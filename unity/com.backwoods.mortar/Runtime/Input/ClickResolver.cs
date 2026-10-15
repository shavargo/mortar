using Mortar.Game;
using Mortar.Game.Commands;

namespace Mortar.Unity
{
    public static class ClickResolver
    {
        public static ClickAction? Resolve(ICellQuery query, Cell cell)
        {
            var monster = query.LivingMonsterAt(cell);
            if (monster != 0)
            {
                return new ClickAction(ClickKind.Attack, cell, monster);
            }
            var item = query.GroundItemAt(cell);
            if (item != 0)
            {
                return new ClickAction(ClickKind.PickUp, cell, item);
            }
            if (query.IsStairs(cell))
            {
                return new ClickAction(ClickKind.Stairs, cell, 0);
            }
            return query.IsWalkable(cell) ? new ClickAction(ClickKind.Move, cell, 0) : (ClickAction?)null;
        }

        public static Command ToCommand(ClickAction action)
        {
            switch (action.Kind)
            {
                case ClickKind.Attack:
                    return new AttackCommand(action.TargetId);
                case ClickKind.PickUp:
                    return new PickUpCommand(action.TargetId);
                default:
                    return new MoveCommand(action.Cell);
            }
        }
    }
}
