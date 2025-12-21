using Application.Connection;
using Application.IntegrationEvent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Connection;

public class WebSocketConnection(
    Guid tableUid,
    string nickname,
    WebSocket socket,
    ILogger logger
) : IConnection
{
    private const int ReceiveBufferSize = 4 * 1024;

    public async Task ListenAsync(CancellationToken cancellationToken)
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
                logger.LogWarning(ex, "Connection to the table {TableUid} failed", tableUid);
                break;
            }
            catch (OperationCanceledException)
            {
                // Graceful cancellation
                break;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                logger.LogInformation("Connection to the table {TableUid} is closed by the client", tableUid);
                break;
            }

            logger.LogDebug(
                "Received {Result} to the table {TableUid} from {Nickname}, ignoring",
                result,
                tableUid,
                nickname
            );
        }
    }

    public async Task SendIntegrationEventAsync(IIntegrationEvent integrationEvent)
    {
        await SendDataAsync("Event", integrationEvent);
    }

    private async Task SendDataAsync(string type, object data)
    {
        var json = JsonSerializer.Serialize(new Response
        {
            Type = type,
            Data = JsonSerializer.SerializeToElement(data)
        });

        var buffer = Encoding.UTF8.GetBytes(json);
        await socket.SendAsync(
            buffer,
            WebSocketMessageType.Text,
            true,
            CancellationToken.None
        );
    }
}

internal record Response
{
    public required string Type;
    public required JsonElement Data;
}
