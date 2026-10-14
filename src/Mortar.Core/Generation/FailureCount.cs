namespace Mortar.Core.Generation;

public sealed class FailureCount
{
    public FailureCount(string reason, int count)
    {
        Reason = reason;
        Count = count;
    }

    public string Reason { get; }

    public int Count { get; }
}
