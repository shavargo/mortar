using Mortar.Core.Layouts;
using Mortar.Game.Items;

namespace Mortar.Game.Simulation;

public sealed class LevelState
{
    public const int DropRadius = 3;

    private readonly int[] _occupants;
    private readonly List<GroundItem> _groundItems = new List<GroundItem>();
    private readonly List<Actor> _actors = new List<Actor>();
    private readonly Dictionary<int, Actor> _byId = new Dictionary<int, Actor>();

    internal LevelState(int depth, uint seed, Layout layout)
    {
        Depth = depth;
        Seed = seed;
        Layout = layout;
        _occupants = new int[layout.Width * layout.Height];
    }

    public int Depth { get; }

    public uint Seed { get; }

    public Layout Layout { get; }

    public IReadOnlyList<Actor> Actors => _actors;

    public bool InBounds(Cell cell) =>
        cell.X >= 0 && cell.Y >= 0 && cell.X < Layout.Width && cell.Y < Layout.Height;

    public bool IsWalkable(Cell cell) => InBounds(cell) && Layout.Cells.IsWalkable(cell.X, cell.Y);

    public bool IsFree(Cell cell) => IsWalkable(cell) && _occupants[Index(cell)] == 0;

    public int OccupantAt(Cell cell) => InBounds(cell) ? _occupants[Index(cell)] : 0;

    public Actor? Find(int id) => _byId.TryGetValue(id, out var actor) ? actor : null;

    public Cell? FindMarker(string kind)
    {
        foreach (var marker in Layout.Markers)
        {
            if (marker.Kind == kind) return new Cell(marker.X, marker.Y);
        }
        return null;
    }

    public IReadOnlyList<GroundItem> GroundItems => _groundItems;

    public GroundItem? FindGroundItem(int id) => _groundItems.Find(ground => ground.Id == id);

    internal Cell? NearestFree(Cell origin, int maxRadius) => FirstInRings(origin, maxRadius, IsFree);

    internal Cell DropCell(Cell origin) =>
        FirstInRings(origin, DropRadius, cell => IsWalkable(cell) && !_groundItems.Exists(ground => ground.Cell == cell)) ?? origin;

    internal void AddGroundItem(GroundItem ground)
    {
        var index = _groundItems.FindIndex(existing => existing.Id > ground.Id);
        _groundItems.Insert(index < 0 ? _groundItems.Count : index, ground);
    }

    internal void RemoveGroundItem(GroundItem ground) => _groundItems.Remove(ground);

    internal void Add(Actor actor, Cell cell)
    {
        if (!IsFree(cell))
        {
            throw new InvalidOperationException($"cell {cell} is not free");
        }
        actor.Cell = cell;
        _occupants[Index(cell)] = actor.Id;
        var index = _actors.FindIndex(existing => existing.Id > actor.Id);
        _actors.Insert(index < 0 ? _actors.Count : index, actor);
        _byId[actor.Id] = actor;
    }

    internal void Move(Actor actor, Cell to)
    {
        Vacate(actor);
        _occupants[Index(to)] = actor.Id;
        actor.Cell = to;
    }

    internal void Vacate(Actor actor)
    {
        var index = Index(actor.Cell);
        if (_occupants[index] == actor.Id) _occupants[index] = 0;
    }

    internal void Remove(Actor actor)
    {
        Vacate(actor);
        _actors.Remove(actor);
        _byId.Remove(actor.Id);
    }

    private static Cell? FirstInRings(Cell origin, int maxRadius, Func<Cell, bool> accept)
    {
        for (var radius = 0; radius <= maxRadius; radius++)
        {
            for (var y = origin.Y - radius; y <= origin.Y + radius; y++)
            {
                for (var x = origin.X - radius; x <= origin.X + radius; x++)
                {
                    var cell = new Cell(x, y);
                    if (cell.DistanceTo(origin) == radius && accept(cell)) return cell;
                }
            }
        }
        return null;
    }

    private int Index(Cell cell) => cell.Y * Layout.Width + cell.X;
}
