using System;
using System.Collections.Generic;
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Events;
using Mortar.Game.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mortar.Unity
{
    public sealed class PlayerControls : MonoBehaviour
    {
        public const int HoldRepeatFrames = 6;

        private readonly InputAction[] _belt = new InputAction[Belt.Slots];
        private MortarGame _game = null!;
        private Func<CellSpace?> _space = () => null;
        private Camera? _camera;
        private InputAction _click = null!;
        private InputAction _point = null!;
        private InputAction _inventory = null!;
        private InputAction _character = null!;
        private InputAction _menu = null!;
        private bool _holdingMove;
        private int _framesSinceMove;
        private Cell? _pendingStairs;

        public event Action? InventoryToggled;

        public event Action? CharacterToggled;

        public event Action? MenuRequested;

        public Func<Vector2, bool> IsOverUi { get; set; } = position => false;

        public Func<bool> InterceptWorldClick { get; set; } = () => false;

        public bool HasPendingStairs => _pendingStairs.HasValue;

        public static PlayerControls Create(Transform parent, MortarGame game, Func<CellSpace?> space, Camera? camera)
        {
            var go = new GameObject("Input");
            go.transform.SetParent(parent, false);
            var input = go.AddComponent<PlayerControls>();
            input._game = game;
            input._space = space;
            input._camera = camera;
            game.Ticked += input.OnTicked;
            game.SessionStarted += input.OnSessionStarted;
            return input;
        }

        public ClickKind? Click(Cell cell)
        {
            var session = _game.Session;
            _pendingStairs = null;
            if (session == null)
            {
                return null;
            }
            var action = ClickResolver.Resolve(new WorldCellQuery(session.World), cell);
            if (action == null)
            {
                return null;
            }
            if (action.Value.Kind == ClickKind.Stairs)
            {
                _pendingStairs = cell;
                _game.Enqueue(new MoveCommand(cell));
            }
            else
            {
                _game.Enqueue(ClickResolver.ToCommand(action.Value));
            }
            return action.Value.Kind;
        }

        public Cell? CellUnderPointer()
        {
            var screen = _point.ReadValue<Vector2>();
            return IsOverUi(screen) ? null : CellUnder(screen);
        }

        private void Awake()
        {
            _click = new InputAction("click", InputActionType.Button, "<Mouse>/leftButton");
            _point = new InputAction("point", InputActionType.Value, "<Pointer>/position", expectedControlType: "Vector2");
            _inventory = new InputAction("inventory", InputActionType.Button, "<Keyboard>/i");
            _character = new InputAction("character", InputActionType.Button, "<Keyboard>/c");
            _menu = new InputAction("menu", InputActionType.Button, "<Keyboard>/escape");
            for (var i = 0; i < _belt.Length; i++)
            {
                _belt[i] = new InputAction($"belt{i + 1}", InputActionType.Button, $"<Keyboard>/{i + 1}");
            }
        }

        private IEnumerable<InputAction> Actions()
        {
            yield return _click;
            yield return _point;
            yield return _inventory;
            yield return _character;
            yield return _menu;
            foreach (var action in _belt)
            {
                yield return action;
            }
        }

        private void OnEnable()
        {
            foreach (var action in Actions())
            {
                action.Enable();
            }
        }

        private void OnDisable()
        {
            foreach (var action in Actions())
            {
                action.Disable();
            }
        }

        private void OnDestroy()
        {
            if (_game != null)
            {
                _game.Ticked -= OnTicked;
                _game.SessionStarted -= OnSessionStarted;
            }
            foreach (var action in Actions())
            {
                action.Dispose();
            }
        }

        private void Update()
        {
            if (_menu.WasPressedThisFrame())
            {
                MenuRequested?.Invoke();
            }
            if (_game.Session == null || _game.Paused)
            {
                return;
            }
            if (_inventory.WasPressedThisFrame())
            {
                InventoryToggled?.Invoke();
            }
            if (_character.WasPressedThisFrame())
            {
                CharacterToggled?.Invoke();
            }
            for (var i = 0; i < _belt.Length; i++)
            {
                if (_belt[i].WasPressedThisFrame())
                {
                    _game.Enqueue(new UseBeltCommand(i));
                }
            }
            HandlePointer(_point.ReadValue<Vector2>());
        }

        private void HandlePointer(Vector2 screen)
        {
            if (_click.WasPressedThisFrame())
            {
                _holdingMove = false;
                _framesSinceMove = 0;
                if (IsOverUi(screen) || InterceptWorldClick())
                {
                    return;
                }
                var cell = CellUnder(screen);
                _holdingMove = cell.HasValue && Click(cell.Value) == ClickKind.Move;
                return;
            }
            if (!_holdingMove || !_click.IsPressed())
            {
                _holdingMove = false;
                return;
            }
            if (++_framesSinceMove < HoldRepeatFrames)
            {
                return;
            }
            _framesSinceMove = 0;
            var held = CellUnder(screen);
            if (held.HasValue && _game.Session != null && _game.Session.World.Level.IsWalkable(held.Value))
            {
                _game.Enqueue(new MoveCommand(held.Value));
            }
        }

        private void OnTicked(IReadOnlyList<GameEvent> events)
        {
            var session = _game.Session;
            if (!_pendingStairs.HasValue || session == null || !StairsWalk.IsSettled(session.World.Player))
            {
                return;
            }
            var stairs = _pendingStairs.Value;
            _pendingStairs = null;
            if (StairsWalk.IsWithinReach(session.World.Player, stairs))
            {
                _game.Enqueue(new UseStairsCommand());
            }
        }

        private void OnSessionStarted(GameSession session)
        {
            _pendingStairs = null;
            _holdingMove = false;
        }

        private Cell? CellUnder(Vector2 screen)
        {
            var space = _space();
            if (space == null || _camera == null)
            {
                return null;
            }
            var world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -_camera.transform.position.z));
            return space.CellAt(world);
        }
    }
}
