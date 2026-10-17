using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;

namespace Mortar.Game.Simulation;

internal static class CommandSystem
{
    public static void Apply(World world, IReadOnlyList<Command> commands, EventSink events)
    {
        foreach (var command in commands)
        {
            var reason = command is null ? CommandRejected.MissingCommand : Execute(world, command, events);
            if (reason is not null) events.Reject(command, reason);
        }
    }

    private static string? Execute(World world, Command command, EventSink events)
    {
        switch (command)
        {
            case MoveCommand move:
                return Move(world, move);
            case AttackCommand attack:
                return Attack(world, attack);
            case CastCommand cast:
                return Cast(world, cast);
            case UseItemCommand useItem:
                return SpellItemActions.Use(world, useItem, events);
            case SpendStatPointCommand spend:
                return PlayerGrowth.SpendPoint(world, spend.Stat, events);
            case UseStairsCommand _:
                return Stairs.Use(world, events);
            case UsePortalCommand _:
                return PortalSystem.Use(world, events);
            case TalkCommand talk:
                return VendorSession.Talk(world, talk.VendorKind);
            case LeaveVendorCommand _:
                return VendorSession.Leave(world);
            case ClassSkillCommand _:
                return ClassSkillSystem.Use(world, events);
            case PickUpCommand pickUp:
                return PickupSystem.Start(world, pickUp);
            case EquipCommand equip:
                return EquipActions.Equip(world, equip.ItemId, events);
            case UnequipCommand unequip:
                return EquipActions.Unequip(world, unequip.Slot, events);
            case DropItemCommand drop:
                return InventoryActions.Drop(world, drop.ItemId, events);
            case MoveItemCommand moveItem:
                return InventoryActions.Move(world, moveItem.ItemId, moveItem.To, events);
            case PutInBeltCommand putInBelt:
                return InventoryActions.PutInBelt(world, putInBelt.ItemId, putInBelt.Slot, events);
            case UseBeltCommand useBelt:
                return InventoryActions.UseBelt(world, useBelt.Slot, events);
            default:
                return "unsupported command";
        }
    }

    private static string? Attack(World world, AttackCommand attack)
    {
        var target = world.Level.Find(attack.TargetId);
        if (target is null || target.Kind != ActorKind.Monster)
        {
            return "no such monster on this level";
        }
        if (!target.IsAlive)
        {
            return "target is already dead";
        }
        var weapon = CombatSystem.EquippedWeapon(world);
        if (CombatSystem.IsRangedWeapon(weapon))
        {
            var ammoId = weapon!.Definition.AmmoId!;
            if (!Ammo.Has(world.Belongings, ammoId))
            {
                return "no ammo";
            }
        }
        world.Player.SetAttackIntent(target.Id);
        return null;
    }

    private static string? Cast(World world, CastCommand cast)
    {
        if (string.IsNullOrWhiteSpace(cast.SpellId) || !world.Content.TryGetSpell(cast.SpellId, out var spell))
        {
            return "unknown spell";
        }
        var sourceError = CastSource(world, spell!, out var fromStaff);
        if (sourceError is not null) return sourceError;
        var aimError = CastAim(world, cast, spell!, out var aim, out var targetId);
        if (aimError is not null) return aimError;
        var specialError = CastSpecial(world, cast, spell!, out var itemId);
        if (specialError is not null) return specialError;
        world.Player.SetCastIntent(spell!.Id, targetId, aim, fromStaff, itemId);
        return null;
    }

    private static string? CastSource(World world, SpellDefinition spell, out bool fromStaff)
    {
        var staff = CombatSystem.EquippedWeapon(world);
        fromStaff = staff is not null
            && staff.Definition.SpellId == spell.Id
            && staff.Charges > 0
            && staff.Definition.MaxCharges > 0;
        var knows = world.Progress.Knows(spell.Id);
        if (knows && world.Player.Mana >= spell.ManaCost)
        {
            fromStaff = false;
            return null;
        }
        if (fromStaff) return null;
        return knows ? "not enough mana" : "spell not learned";
    }

    private static string? CastAim(World world, CastCommand cast, SpellDefinition spell, out Cell aim, out int targetId)
    {
        aim = world.Player.Cell;
        targetId = 0;
        if (spell.Shape != SpellShapes.Projectile && spell.Shape != SpellShapes.Area) return null;
        if (cast.TargetActorId != 0)
        {
            var target = world.Level.Find(cast.TargetActorId);
            if (target is null || !target.IsAlive || target.Kind != ActorKind.Monster)
            {
                return "no such monster on this level";
            }
            aim = target.Cell;
            targetId = target.Id;
            return null;
        }
        if (cast.TargetCell is { } cell)
        {
            if (!world.Level.InBounds(cell)) return "invalid target";
            aim = cell;
            return null;
        }
        return "spell needs a target";
    }

    private static string? CastSpecial(World world, CastCommand cast, SpellDefinition spell, out int itemId)
    {
        itemId = 0;
        if (spell.Id == "identify")
        {
            if (cast.ItemId == 0) return "identify needs an item";
            var item = world.Belongings.Find(cast.ItemId);
            if (item is null) return "no such item";
            if (item.Identified) return "item is already identified";
            itemId = cast.ItemId;
        }
        if (spell.Id == "town-portal")
        {
            if (world.Content.ShallowestLevel > 0) return "there is no town";
            if (world.Depth <= 0) return "cannot open a portal in town";
        }
        return null;
    }

    private static string? Move(World world, MoveCommand move)
    {
        if (!world.Level.IsWalkable(move.Destination))
        {
            return "destination is not walkable";
        }
        var occupant = world.Level.OccupantAt(move.Destination);
        if ((occupant != 0 && occupant != world.Player.Id) || world.Level.HasTownNpc(move.Destination))
        {
            return "destination is blocked";
        }
        if (!Movement.IsReachable(world.Level, world.Player.Cell, move.Destination))
        {
            return "destination is unreachable";
        }
        world.Player.SetMoveIntent(move.Destination);
        return null;
    }
}
