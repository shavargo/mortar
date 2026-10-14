namespace Mortar.Game.Content;

public sealed class ClassGrowth
{
    public ClassGrowth(
        int lifeBase,
        int lifePerVitality,
        int lifePerLevel,
        int manaBase,
        int manaPerMagic,
        int manaPerLevel,
        int statPointsPerLevel)
    {
        LifeBase = lifeBase;
        LifePerVitality = lifePerVitality;
        LifePerLevel = lifePerLevel;
        ManaBase = manaBase;
        ManaPerMagic = manaPerMagic;
        ManaPerLevel = manaPerLevel;
        StatPointsPerLevel = statPointsPerLevel;
    }

    public int LifeBase { get; }

    public int LifePerVitality { get; }

    public int LifePerLevel { get; }

    public int ManaBase { get; }

    public int ManaPerMagic { get; }

    public int ManaPerLevel { get; }

    public int StatPointsPerLevel { get; }
}
