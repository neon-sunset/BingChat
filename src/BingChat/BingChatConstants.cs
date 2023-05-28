namespace BingChat;

internal static class BingChatConstants
{
    internal const byte ChatHubTerminalChar = (byte)'\u001e';

    internal static readonly ReadOnlyMemory<byte> ChatHubInfo =
        "{\"protocol\":\"json\",\"version\":1}\u001e"u8.ToArray();

    internal static readonly string[] OptionSets = new[]
    {
        "nlu_direct_response_filter",
        "deepleo",
        "disable_emoji_spoken_text",
        "responsible_ai_policy_235",
        "enablemm",
        "osbsdusgrec",
        "objopinion",
        "dsblhlthcrd",
        "chatgptretry",
        "oaimxcnk1024",
        "knowimgv2",
        "dv3sugg",
        "autosave"
    };

    internal static readonly string[] CreativeOptionSets = OptionSets
        .Concat(new[] { "h3imaginative", "clgalileo", "gencontentv3" })
        .ToArray();

    internal static readonly string[] PreciseOptionSets = OptionSets
        .Concat(new[] { "h3precise", "clgalileo", "gencontentv3" })
        .ToArray();

    internal static readonly string[] BalancedOptionSets = OptionSets
        .Concat(new[] { "galileo" })
        .ToArray();

    // GPT-4?
    internal static readonly string[] BalancedOldOptionSets = OptionSets
        .Concat(new[] { "h3balanced", "harmonyv3", "gencontentv3" })
        .ToArray();

    internal static readonly string[] AllowedMessageTypes = new[]
    {
        "ActionRequest",
        "Chat",
        "Context",
        "InternalSearchQuery",
        "InternalSearchResult",
        // "Disengaged",
        "InternalLoaderMessage",
        "Progress",
        "RenderCardRequest",
        "AdsQuery",
        "SemanticSerp",
        "GenerateContentQuery",
        "SearchQuery"
    };
}