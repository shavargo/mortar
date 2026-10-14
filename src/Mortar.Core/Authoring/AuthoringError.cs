namespace Mortar.Core.Authoring;

public sealed class AuthoringError
{
    public AuthoringError(string source, string message)
    {
        Source = source;
        Message = message;
    }

    public string Source { get; }

    public string Message { get; }

    public override string ToString() => $"{Source}: {Message}";
}
