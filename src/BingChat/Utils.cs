using System.Buffers;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;

namespace BingChat;

internal static class Utils
{
    // !INVALID CODE!
    public static async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadAllMessages(
        this ClientWebSocket client, [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Renting a 64KB buffer is easier than writing proper code :D
        var toReturn = ArrayPool<byte>.Shared.Rent(1024 * 64);
        var buffer = toReturn.AsMemory();

        ValueWebSocketReceiveResult result;
        while ((result = await client.ReceiveAsync(buffer, ct))
            .MessageType != WebSocketMessageType.Close)
        {
            if (result.EndOfMessage)
            {
                var message = buffer[..result.Count];
                Console.WriteLine(Encoding.UTF8.GetChars(message.ToArray()));
                // Remove terminal char and possibly empty json message that may follow it
                message = message[..message.Span.IndexOf(BingChatConstants.ChatHubTerminalChar)];
                if (message.Length > 0)
                {
                    yield return message;
                }
            }
        }
    }
}
