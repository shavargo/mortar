namespace Mortar.Game.Commands;

public abstract class Command
{
    public abstract string Describe();

    public override string ToString() => Describe();
}
