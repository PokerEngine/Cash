using Application.Connection;
using Infrastructure.Connection;
using System.Net.WebSockets;

namespace Infrastructure.Controller;

public class TableWsController(
    IConnectionRegistry connectionRegistry,
    ILogger<TableWsController> logger
)
{
    public async Task HandleAsync(HttpContext context, Guid uid, string nickname, CancellationToken cancellationToken)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        // Accept socket
        using var socket = await context.WebSockets.AcceptWebSocketAsync();

        IConnection connection = new WebSocketConnection(
            tableUid: uid,
            nickname: nickname,
            socket: socket,
            logger: logger
        );
        connectionRegistry.Connect(uid, nickname, connection);

        // Start listening loop
        try
        {
            await connection.ListenAsync(cancellationToken);
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
            connectionRegistry.Disconnect(uid, nickname, connection);

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
        }
    }
}
