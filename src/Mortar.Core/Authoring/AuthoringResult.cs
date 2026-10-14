namespace Mortar.Core.Authoring;

public sealed class AuthoringResult<T>
    where T : class
{
    private AuthoringResult(T? value, IReadOnlyList<AuthoringError> errors)
    {
        Value = value;
        Errors = errors;
    }

    public T? Value { get; }

    public IReadOnlyList<AuthoringError> Errors { get; }

    public bool Succeeded => Value is not null;

    public static AuthoringResult<T> Success(T value) =>
        new AuthoringResult<T>(value, Array.Empty<AuthoringError>());

    public static AuthoringResult<T> Failure(IReadOnlyList<AuthoringError> errors) =>
        new AuthoringResult<T>(null, errors);

    public static AuthoringResult<T> Failure(string source, string message) =>
        Failure(new[] { new AuthoringError(source, message) });
}
