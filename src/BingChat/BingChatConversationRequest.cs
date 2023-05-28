using System.Text.Json.Serialization;

// ReSharper disable MemberCanBeInternal
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

#pragma warning disable CS8618

namespace BingChat;

internal sealed class BingChatConversationRequest
{
    public int Type { get; set; }
    public string InvocationId { get; set; }
    public string Target { get; set; }
    public RequestArgument[] Arguments { get; set; }
}

internal sealed class RequestArgument
{
    public string Source { get; set; }
    public string[] OptionsSets { get; set; }
    public string[] AllowedMessageTypes { get; set; }
    public string[] SliceIds { get; set; }
    public string TraceId { get; set; }
    public bool IsStartOfSession { get; set; }
    public RequestMessage Message { get; set; }
    public string Tone { get; set; }
    public string ConversationSignature { get; set; }
    public Participant Participant { get; set; }
    public string ConversationId { get; set; }
}

internal sealed class RequestMessage
{
    // Are these needed?
    // locale = ,
    // market = ,
    // region = ,
    // location = ,
    // locationHints: [],
    public DateTime Timestamp { get; set; }
    public string Author { get; set; }
    public string InputMethod { get; set; }
    public string MessageType { get; set; }
    public string Text { get; set; }
}

internal struct Participant
{
    public string Id { get; set; }
}
