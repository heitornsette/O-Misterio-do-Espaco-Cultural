using System.Collections.Generic;
using Misterio.Puzzles.Floor4;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Misterio.Gameplay
{
    /// <summary>
    /// Main hall: rotate the conduits on the panel until the energy walks from the source to the
    /// panel. Every tile carries a symbol as well as a colour, so the puzzle never depends on hue.
    /// </summary>
    public sealed class Floor4Controller : PuzzleFloor
    {
        [Header("Panel")]
        [SerializeField] Vector2 panelOrigin = new Vector2(-0.72f, -0.18f);
        [SerializeField] Vector2 cellSize = new Vector2(0.28f, 0.24f);
        [SerializeField, Min(2)] int width = 5;
        [SerializeField, Min(2)] int height = 4;
        [SerializeField] Vector2Int source = new Vector2Int(0, 1);
        [SerializeField] Vector2Int target = new Vector2Int(4, 2);

        [Header("Views")]
        [SerializeField] ConduitTileView[] tileViews;
        [SerializeField] Transform cursorView;

        ConduitCircuit circuit;
        Vector2Int cursor;
        readonly List<Rect> blockers = new List<Rect>();
        byte[] solvedMasks;

        /// <summary>The energy route; the solved layout is derived from it, never hand-written.</summary>
        static readonly Vector2Int[] Route =
        {
            new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
            new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(4, 2)
        };

        /// <summary>Dead-end branches make the panel read like a real circuit without extra rules.</summary>
        static readonly Vector2Int[][] Branches =
        {
            new[] { new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(1, 3) },
            new[] { new Vector2Int(3, 2), new Vector2Int(3, 3) }
        };

        /// <summary>Fixed scramble, so the panel starts identical on every run.</summary>
        static readonly int[] StartTurns = { 2, 1, 3, 1, 2, 1, 2, 3, 1, 2, 3, 1, 2, 3, 1, 2, 3, 1, 2, 3 };

        public ConduitCircuit Circuit => circuit;

        protected override void Awake()
        {
            base.Awake();
            floorNumber = 4;
            objectiveText = "Gire os conduítes até a energia chegar ao painel.";
            solvedPause = 1.8f;
        }

        protected override void OnFloorReady()
        {
            solvedMasks = ConduitCircuit.LayoutFromPath(width, height, Route, Branches);
            var masks = ConduitCircuit.ScrambleFrom(solvedMasks, StartTurns, width, height);
            circuit = new ConduitCircuit(width, height, source, target, masks);
            cursor = source;

            var panelRect = new Rect(WorldOf(new Vector2Int(0, 0)).x - cellSize.x * 0.6f,
                                     WorldOf(new Vector2Int(0, 0)).y - cellSize.y * 0.6f,
                                     cellSize.x * (width + 0.2f), cellSize.y * (height + 0.2f));
            blockers.Add(panelRect);
            Guard.Configure(GuardWalkable(), blockers);
            Guard.Teleport(new Vector2(WorldOf(source).x - 1.1f, panelRect.center.y));

            SyncViews();
            UpdateCursorView();
            UpdateCounter();
            Hud.ShowHint("Setas movem o cursor, E gira o conduíte.", 7f);
        }

        Rect GuardWalkable()
        {
            Vector2 min = WorldOf(Vector2Int.zero);
            Vector2 max = WorldOf(new Vector2Int(width - 1, height - 1));
            return new Rect(min.x - 1.5f, min.y - 1.3f, (max.x - min.x) + 3f, (max.y - min.y) + 2.6f);
        }

        protected override void OnFloorTick(float deltaTime)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) MoveCursor(Vector2Int.left);
            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) MoveCursor(Vector2Int.right);
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) MoveCursor(Vector2Int.up);
            if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) MoveCursor(Vector2Int.down);

            if (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
                TurnCurrent();

            if (circuit.IsSolved() && !Solved) CompleteFloor();
        }

        void MoveCursor(Vector2Int delta)
        {
            Vector2Int next = cursor + delta;
            if (!circuit.IsValid(next)) return;
            cursor = next;
            UpdateCursorView();
            NoteProgress();
        }

        void TurnCurrent()
        {
            if (!circuit.Turn(cursor)) return;
            SyncViews();
            UpdateCounter();
            NoteProgress();
        }

        public Vector2 WorldOf(Vector2Int cell) =>
            new Vector2(panelOrigin.x + cell.x * cellSize.x, panelOrigin.y + cell.y * cellSize.y);

        void UpdateCursorView()
        {
            if (cursorView == null) return;
            Vector2 position = WorldOf(cursor);
            cursorView.position = new Vector3(position.x, position.y, cursorView.position.z);
        }

        void SyncViews()
        {
            if (tileViews == null) return;
            var powered = circuit.PoweredTiles();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    if (index >= tileViews.Length || tileViews[index] == null) continue;
                    var cell = new Vector2Int(x, y);
                    tileViews[index].Apply(circuit.Get(cell), powered.Contains(cell), cell == source, cell == target);
                }
            }
        }

        void UpdateCounter()
        {
            int total = circuit.PieceCount(solvedMasks);
            int misaligned = circuit.MisalignedCount(solvedMasks);
            Hud.SetCounter("Conduítes alinhados: " + (total - misaligned) + "/" + total);
            Hud.SetObjective(circuit.IsSolved()
                ? "Circuito fechado."
                : "Gire os conduítes até a energia chegar ao painel.");
        }

        protected override string HintForFloor() =>
            "Siga o caminho da fonte até o painel: cada peça precisa apontar para a vizinha.";
    }
}
