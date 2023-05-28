namespace BingChat;

public interface IBingChattable
{
    /// <summary>
    /// Ask for a answer.
    /// </summary>
    Task<string> AskAsync(string message);

    Task<string> AskAsync2(string message, CancellationToken ct = default);
}