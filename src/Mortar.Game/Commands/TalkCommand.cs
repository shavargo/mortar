namespace Mortar.Game.Commands;

public sealed class TalkCommand : Command
{
    public TalkCommand(string vendorKind)
    {
        VendorKind = vendorKind;
    }

    public string VendorKind { get; }

    public override string Describe() => $"talk to {VendorKind}";
}
