using System.Buffers;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using Websocket.Client;

namespace BingChat;

/// <summary>
/// A chat conversation, enables us to chat multiple times in the same context.
/// </summary>
internal sealed class BingChatConversation : IBingChattable
{
    private const char TerminalChar = '\u001e';
    private static readonly ReadOnlyMemory<byte> ProtocolMsg = "{\"protocol\":\"json\",\"version\":1}\u001e"u8.ToArray();
    private static readonly ReadOnlyMemory<byte> KeepAliveMsg = "{\"type\":6}\u001e"u8.ToArray();

    private readonly BingChatRequest _request;

    internal BingChatConversation(
        string clientId, string conversationId, string conversationSignature, BingChatTone tone)
    {
        _request = new BingChatRequest(clientId, conversationId, conversationSignature, tone);
    }

    // This will be completely rewritten
    public async Task<string> AskAsync2(string message, CancellationToken ct = default)
    {
        var buffer = new ArrayBufferWriter<byte>(256);
        using var writer = new Utf8JsonWriter(buffer);

        JsonSerializer.Serialize(
            writer,
            _request.ConstructInitialPayload(message),
            SerializerContext.Default.BingChatConversationRequest);
        buffer.Write(stackalloc[] { (byte)TerminalChar });

        var initialPayload = buffer.WrittenMemory;

        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri("wss://sydney.bing.com/sydney/ChatHub"), ct);

        // Start the chat response session by sending protocol version and type
        await ws.SendAsync(ProtocolMsg, WebSocketMessageType.Text, endOfMessage: true, ct);
        // Schedule keep alive messages
        using var keepAlive = new Timer(
            async _ => await ws.SendAsync(KeepAliveMsg, WebSocketMessageType.Text, endOfMessage: true, ct),
            null,
            TimeSpan.FromMilliseconds(0),
            TimeSpan.FromSeconds(16));
        // I have no idea what this is for, but Edge sends it
        await ws.SendAsync(KeepAliveMsg, WebSocketMessageType.Text, endOfMessage: true, ct);
        // Send the initial payload with user prompt
        await ws.SendAsync(initialPayload, WebSocketMessageType.Text, endOfMessage: true, ct);

        var response = await ws
            .ReadAllMessages(ct)
            .Select(BingChatConversationResponse.FromJson)
            .Where(msg => msg is { Type: 2 })
            .FirstAsync(ct);

        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, ct);
        return BuildAnswer(response!) ?? "<empty answer>";
    }

    /// <inheritdoc/>
    public Task<string> AskAsync(string message)
    {
        var wsClient = new WebsocketClient(new Uri("wss://sydney.bing.com/sydney/ChatHub"));
        var tcs = new TaskCompletionSource<string>();

        void OnMessageReceived(string text)
        {
            try
            {
                foreach (var part in text.Split(TerminalChar, StringSplitOptions.RemoveEmptyEntries))
                {
                    var json = JsonSerializer.Deserialize(part, SerializerContext.Default.BingChatConversationResponse);

                    if (json is not { Type: 2 }) continue;

                    Cleanup();

                    tcs.SetResult(BuildAnswer(json) ?? "<empty answer>");
                    return;
                }
            }
            catch (Exception e)
            {
                Cleanup();
                tcs.SetException(e);
            }
        }

        void Cleanup()
        {
            wsClient.Stop(WebSocketCloseStatus.Empty, string.Empty).ContinueWith(t =>
            {
                if (t.IsFaulted) tcs.SetException(t.Exception!);
                wsClient.Dispose();
            });
        }

        wsClient.MessageReceived
                .Where(msg => msg.MessageType == WebSocketMessageType.Text)
                .Select(msg => msg.Text)
                .Subscribe(OnMessageReceived);

        // Start the WebSocket client
        wsClient.Start().ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                Cleanup();
                tcs.SetException(t.Exception!);
                return;
            }

            var initialPayload = JsonSerializer.Serialize(
                _request.ConstructInitialPayload(message), SerializerContext.Default.BingChatConversationRequest);

            // Send initial messages
            wsClient.Send("{\"protocol\":\"json\",\"version\":1}" + TerminalChar);
            wsClient.Send(initialPayload + TerminalChar);
        });

        return tcs.Task;
    }

    private static string? BuildAnswer(BingChatConversationResponse response)
    {
        //Check status
        if (!response.Item.Result.Value.Equals("Success", StringComparison.OrdinalIgnoreCase))
        {
            throw new BingChatException($"{response.Item.Result.Value}: {response.Item.Result.Message}");
        }

        //Collect messages, including of types: Chat, SearchQuery, LoaderMessage, Disengaged
        var messages = new List<string>();
        foreach (var itemMessage in response.Item.Messages)
        {
            //Not needed
            if (itemMessage.Author != "bot") continue;
            if (itemMessage.MessageType is "InternalSearchResult" or "RenderCardRequest")
                continue;

            //Not supported
            if (itemMessage.MessageType is "GenerateContentQuery")
                continue;

            //From Text
            var text = itemMessage.Text;

            //From AdaptiveCards
            var adaptiveCards = itemMessage.AdaptiveCards;
            if (text is null && adaptiveCards?.Length > 0)
            {
                var bodies = new List<string>();
                foreach (var body in adaptiveCards[0].Body)
                {
                    if (body.Type != "TextBlock" || body.Text is null) continue;
                    bodies.Add(body.Text);
                }
                text = bodies.Count > 0 ? string.Join("\n", bodies) : null;
            }

            //From MessageType
            text ??= $"<{itemMessage.MessageType}>";

            //From SourceAttributions
            var sourceAttributions = itemMessage.SourceAttributions;
            if (sourceAttributions?.Length > 0)
            {
                text += "\n";
                for (var nIndex = 0; nIndex < sourceAttributions.Length; nIndex++)
                {
                    var sourceAttribution = sourceAttributions[nIndex];
                    text += $"\n[{nIndex + 1}]: {sourceAttribution.SeeMoreUrl} \"{sourceAttribution.ProviderDisplayName}\"";
                }
            }

            messages.Add(text);
        }

        return messages.Count > 0 ? string.Join("\n\n", messages) : null;
    }
}
