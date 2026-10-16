namespace Mortar.Game.Content;

public static class SpellShapes
{
    public const string Self = "self";
    public const string Projectile = "projectile";
    public const string Area = "area";

    public static bool IsKnown(string shape) =>
        shape == Self || shape == Projectile || shape == Area;
}
