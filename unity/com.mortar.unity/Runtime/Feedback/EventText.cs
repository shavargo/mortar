using Mortar.Game.Events;

namespace Mortar.Unity
{
    public static class EventText
    {
        public static string? Describe(GameEvent gameEvent)
        {
            switch (gameEvent)
            {
                case CommandRejected rejected:
                    return Sentence(rejected.Reason);
                case PickupFailed failed:
                    return $"Cannot pick that up: {failed.Reason}";
                case ItemPickedUp picked:
                    return $"Picked up {picked.Name}";
                case GoldPickedUp gold:
                    return $"Picked up {gold.Amount} gold";
                case PotionDrunk potion:
                    return $"Restored {potion.Amount} {potion.Restores}";
                case LeveledUp leveled:
                    return $"Reached level {leveled.Level}";
                case StatIncreased stat:
                    return $"{Sentence(stat.Stat.ToString().ToLowerInvariant())} is now {stat.Value}";
                case LevelChanged changed:
                    return changed.ToDepth == 0 ? "Entered the town" : $"Entered depth {changed.ToDepth}";
                default:
                    return null;
            }
        }

        public static string Sentence(string text) =>
            text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}
