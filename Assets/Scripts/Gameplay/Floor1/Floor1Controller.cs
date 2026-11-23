using System.Collections.Generic;
using Misterio.Puzzles.Floor1;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Misterio.Gameplay
{
    /// <summary>
    /// Ground floor: the guard pushes the three wooden statues back onto the floor marks.
    /// Movement stays free in 8 directions; when he walks into a statue the board takes over,
    /// snaps the push to the grid and keeps undo/reset available at all times.
    /// </summary>
    public sealed class Floor1Controller : PuzzleFloor
    {
        [Header("Grid")]
        [SerializeField] Vector2 gridOrigin = new Vector2(-1.75f, -0.88f);
        [SerializeField] Vector2 cellSize = new Vector2(0.24f, 0.2f);
        [SerializeField, Min(1)] int width = 12;
        [SerializeField, Min(1)] int height = 8;

        [Header("Pieces")]
        [SerializeField] Transform[] statueViews;
        [SerializeField] Transform[] markViews;
        [SerializeField, Min(0.05f)] float pushCooldown = 0.24f;

        SokobanBoard board;
        readonly List<Rect> blockers = new List<Rect>();
        float cooldown;
        float warnCooldown;

        static readonly Vector2Int PlayerStart = new Vector2Int(2, 2);
        static readonly Vector2Int[] StatueCells = { new Vector2Int(4, 2), new Vector2Int(6, 4), new Vector2Int(3, 5) };
        static readonly Vector2Int[] TargetCells = { new Vector2Int(7, 2), new Vector2Int(8, 4), new Vector2Int(5, 5) };

        public SokobanBoard Board => board;

        protected override void Awake()
        {
            base.Awake();
            floorNumber = 1;
            objectiveText = "Devolva as estátuas às marcações no piso.";
            solvedPause = 1.5f;
        }

        protected override void OnFloorReady()
        {
            board = new SokobanBoard(width, height, PlayerStart, StatueCells, TargetCells, BuildWalls());
            Guard.Blocked += OnBlocked;
            Guard.Teleport(WorldOf(PlayerStart));
            RefreshBlockers();
            SyncViews();
            UpdateCounter();
            Hud.ShowHint("Ande contra uma estátua para empurrá-la. Z desfaz, R reinicia.", 7f);
        }

        protected override void OnDestroy()
        {
            if (Guard != null) Guard.Blocked -= OnBlocked;
            base.OnDestroy();
        }

        bool[] BuildWalls()
        {
            var walls = new bool[width * height];
            for (int x = 0; x < width; x++)
            {
                walls[x] = true;
                walls[(height - 1) * width + x] = true;
            }
            for (int y = 0; y < height; y++)
            {
                walls[y * width] = true;
                walls[y * width + width - 1] = true;
            }
            return walls;
        }

        protected override void OnFloorTick(float deltaTime)
        {
            cooldown = Mathf.Max(0f, cooldown - deltaTime);
            warnCooldown = Mathf.Max(0f, warnCooldown - deltaTime);
            HandleEditKeys();

            if (board != null && board.IsSolved() && !Solved) CompleteFloor();
        }

        void HandleEditKeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.zKey.wasPressedThisFrame && board.CanUndo)
            {
                board.Undo();
                Guard.Teleport(WorldOf(board.Player));
                RefreshBlockers();
                SyncViews();
                UpdateCounter();
                NoteProgress();
            }
            if (keyboard.rKey.wasPressedThisFrame)
            {
                board.Reset();
                Guard.Teleport(WorldOf(board.Player));
                RefreshBlockers();
                SyncViews();
                UpdateCounter();
                NoteProgress();
            }
        }

        void OnBlocked(Vector2 direction)
        {
            if (board == null || cooldown > 0f || Solved) return;

            Vector2Int step = ToCardinal(direction);
            if (step == Vector2Int.zero) return;

            Vector2Int guardCell = CellOf(Guard.Position);
            board.AlignTo(guardCell);
            int statue = board.StatueAt(guardCell + step);
            if (statue < 0) return;

            var result = board.TryMove(step);
            if (result == SokobanMoveResult.Blocked)
            {
                FloorSfx.Instance?.Error();
                return;
            }

            cooldown = pushCooldown;
            FloorSfx.Instance?.Push();
            Guard.Teleport(WorldOf(board.Player));
            RefreshBlockers();
            SyncViews();
            UpdateCounter();
            NoteProgress();

            if (result == SokobanMoveResult.PushedIntoDeadlock && warnCooldown <= 0f)
            {
                warnCooldown = 3f;
                Hud.ShowHint("Essa estátua ficou sem saída. Z desfaz o empurrão.", 5f);
            }
            if (board.IsSolved()) FloorSfx.Instance?.Confirm();
        }

        void RefreshBlockers()
        {
            blockers.Clear();
            for (int i = 0; i < board.Statues.Length; i++)
                blockers.Add(CellRect(board.Statues[i], 0.82f));
            Guard.Configure(WalkableRect(), blockers);
        }

        Rect WalkableRect()
        {
            Vector2 min = WorldOf(Vector2Int.one);
            Vector2 max = WorldOf(new Vector2Int(width - 1, height - 1));
            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        Rect CellRect(Vector2Int cell, float coverage)
        {
            Vector2 center = WorldOf(cell);
            float halfWidth = cellSize.x * coverage * 0.5f;
            float halfHeight = cellSize.y * coverage * 0.5f;
            return new Rect(center.x - halfWidth, center.y - halfHeight, halfWidth * 2f, halfHeight * 2f);
        }

        public Vector2 WorldOf(Vector2Int cell) =>
            new Vector2(gridOrigin.x + cell.x * cellSize.x, gridOrigin.y + cell.y * cellSize.y);

        public Vector2Int CellOf(Vector2 world)
        {
            int x = Mathf.RoundToInt((world.x - gridOrigin.x) / cellSize.x);
            int y = Mathf.RoundToInt((world.y - gridOrigin.y) / cellSize.y);
            return new Vector2Int(Mathf.Clamp(x, 0, width - 1), Mathf.Clamp(y, 0, height - 1));
        }

        static Vector2Int ToCardinal(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
                return direction.x >= 0f ? Vector2Int.right : Vector2Int.left;
            return direction.y >= 0f ? Vector2Int.up : Vector2Int.down;
        }

        void SyncViews()
        {
            if (statueViews != null)
            {
                for (int i = 0; i < statueViews.Length && i < board.Statues.Length; i++)
                {
                    if (statueViews[i] == null) continue;
                    Vector2 position = WorldOf(board.Statues[i]);
                    statueViews[i].position = new Vector3(position.x, position.y, statueViews[i].position.z);
                }
            }

            if (markViews != null)
            {
                for (int i = 0; i < markViews.Length && i < TargetCells.Length; i++)
                {
                    if (markViews[i] == null) continue;
                    Vector2 position = WorldOf(TargetCells[i]);
                    markViews[i].position = new Vector3(position.x, position.y, markViews[i].position.z);
                    bool filled = board.StatueAt(TargetCells[i]) >= 0;
                    var renderer = markViews[i].GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        var color = renderer.color;
                        color.a = filled ? 0.35f : 0.9f;
                        renderer.color = color;
                    }
                }
            }
        }

        void UpdateCounter()
        {
            int pending = board.PendingStatues();
            int total = TargetCells.Length;
            Hud.SetCounter("Estátuas no lugar: " + (total - pending) + "/" + total);
            Hud.SetObjective(pending == 0
                ? "Escadaria liberada."
                : "Devolva as estátuas às marcações no piso.");
        }

        /// <summary>Development only: solves the statues so the route capture can move on.</summary>
        protected override void AutoCompleteForCapture()
        {
            var solution = SokobanSolver.Solve(board);
            if (solution != null)
                foreach (var step in solution) board.TryMove(step);
            RefreshBlockers();
            SyncViews();
            UpdateCounter();
            CompleteFloor();
        }

        protected override string HintForFloor()
        {
            if (board == null) return "Empurre as estátuas até as marcações.";
            var hint = SokobanSolver.NextHint(board);
            if (hint.HasValue)
            {
                Vector2Int step = hint.Value;
                string direction = step == Vector2Int.up ? "cima"
                    : step == Vector2Int.down ? "baixo"
                    : step == Vector2Int.left ? "esquerda" : "direita";
                return "Dica: tente um passo para a " + direction + ". Z desfaz qualquer engano.";
            }
            return "Empurre pela face oposta ao destino e evite cantos. Z desfaz, R reinicia.";
        }
    }
}
