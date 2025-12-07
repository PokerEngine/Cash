using Application.Command;
using Infrastructure.Command;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Controller;

public class TableWsController(
    CommandDispatcher commandDispatcher,
    ILogger<TableWsController> logger
)
{
    private const int ReceiveBufferSize = 4 * 1024;
    private static readonly Dictionary<string, (Type TCommandRequest, Type TCommandResponse)> CommandMapping = new()
    {
        {"SitDownAtTableCommand", (typeof(SitDownAtTableCommand), typeof(SitDownAtTableResult))},
        {"StandUpFromTableCommand", (typeof(StandUpFromTableCommand), typeof(StandUpFromTableResult))}
    };

    public async Task HandleAsync(HttpContext context, Guid uid, CancellationToken cancellationToken)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        // Accept socket
        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        logger.LogInformation("Connection established to table {Uid}", uid);

        // Start listening loop
        try
        {
            await ReceiveLoopAsync(socket, uid, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Graceful cancellation
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on websocket receive loop");
        }
        finally
        {
            // Ensure cleanup
            if (socket.State != WebSocketState.Closed)
            {
                try
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None);
                }
                catch
                {
                    // Ignore
                }
            }

            logger.LogInformation("Connection closed to table {Uid}", uid);
        }
    }

    private async Task ReceiveLoopAsync(WebSocket socket, Guid uid, CancellationToken cancellationToken)
    {
        var buffer = new byte[ReceiveBufferSize];
        var seg = new ArraySegment<byte>(buffer);

        while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result;
            try
            {
                result = await socket.ReceiveAsync(seg, cancellationToken);
            }
            catch (WebSocketException ex)
            {
                logger.LogWarning(ex, "Connection to the table {Uid} failed", uid);
                break;
            }
            catch (OperationCanceledException)
            {
                // Graceful cancellation
                break;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                logger.LogInformation("Connection to the table {Uid} closed by the client", uid);
                break;
            }

            // Handle text messages only
            if (result.MessageType != WebSocketMessageType.Text)
            {
                continue;
            }

            // Read whole text
            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);

            ICommandResponse response;

            try
            {
                response = await DispatchCommandAsync(json);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling command");
                await SendExceptionAsync(socket, ex, cancellationToken);
                break;
            }

            await SendResponseAsync(socket, response, cancellationToken);
        }
    }

    private async Task<ICommandResponse> DispatchCommandAsync(string json)
    {
        var request = JsonSerializer.Deserialize<Request>(json) ?? throw new JsonException("Invalid message");

        if (!CommandMapping.TryGetValue(request.Type, out var entry))
        {
            throw new JsonException("Unknown command: " + request.Type);
        }

        var (commandType, resultType) = entry;

        // Deserialize payload
        var rawPayload = request.Data.GetRawText();
        object command = JsonSerializer.Deserialize(rawPayload, commandType) ?? throw new JsonException("Invalid command payload");

        logger.LogInformation("Dispatching " + commandType.Name);

        // Reflect dynamic call to commandDispatcher.DispatchAsync<TCommandRequest, TCommandResponse>()
        var method = typeof(CommandDispatcher).GetMethod(nameof(CommandDispatcher.DispatchAsync))!;
        var genericMethod = method.MakeGenericMethod(commandType, resultType);

        var task = (Task)genericMethod.Invoke(commandDispatcher, new[] { command })!;

        await task.ConfigureAwait(false);

        // Get TCommandResponse from Task<TCommandResponse>
        var resultProperty = task.GetType().GetProperty("Result")!;
        object result = resultProperty.GetValue(task)!;

        return (ICommandResponse)result;
    }

    private async Task SendResponseAsync(
        WebSocket socket,
        ICommandResponse commandResponse,
        CancellationToken cancellationToken
    )
    {
        var response = new Response
        {
            Type = "Response",
            Data = JsonSerializer.SerializeToElement((dynamic)commandResponse)
        };
        var json = JsonSerializer.Serialize(response);
        var buffer = Encoding.UTF8.GetBytes(json);
        var seg = new ArraySegment<byte>(buffer);

        await socket.SendAsync(seg, WebSocketMessageType.Text, true, cancellationToken);
    }

    private async Task SendExceptionAsync(
        WebSocket socket,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var response = new Response
        {
            Type = "Error",
            Data = JsonSerializer.SerializeToElement(new {
                message = exception.Message
            })
        };
        var json = JsonSerializer.Serialize(response);
        var buffer = Encoding.UTF8.GetBytes(json);
        var seg = new ArraySegment<byte>(buffer);

        await socket.SendAsync(seg, WebSocketMessageType.Text, true, cancellationToken);
    }
}

internal record Request
{
    public required string Type { init; get; }
    public required JsonElement Data { init; get; }
}

internal record Response
{
    public required string Type { init; get; }
    public required JsonElement Data { init; get; }
}
