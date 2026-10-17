using Mortar.Game;

namespace Mortar.Unity
{
    public enum ClickKind
    {
        Move,
        Attack,
        PickUp,
        Stairs,
    }

    public readonly struct ClickAction
    {
        public ClickAction(ClickKind kind, Cell cell, int targetId)
        {
            Kind = kind;
            Cell = cell;
            TargetId = targetId;
        }

        public ClickKind Kind { get; }

        public Cell Cell { get; }

        public int TargetId { get; }
    }

    public interface ICellQuery
    {
        int LivingMonsterAt(Cell cell);

        int GroundItemAt(Cell cell);

        bool IsStairs(Cell cell);

        bool IsWalkable(Cell cell);
    }
}
