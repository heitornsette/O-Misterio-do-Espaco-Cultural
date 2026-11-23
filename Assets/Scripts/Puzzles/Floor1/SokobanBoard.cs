using System.Collections.Generic;
using UnityEngine;

namespace Misterio.Puzzles.Floor1
{
    public enum SokobanMoveResult
    {
        /// <summary>Nothing moved: wall, statue in the way, or out of bounds.</summary>
        Blocked,
        /// <summary>The guard walked into an empty cell.</summary>
        Moved,
        /// <summary>The guard pushed a statue that still has a way out.</summary>
        Pushed,
        /// <summary>The push worked but the statue is now stuck in a corner.</summary>
        PushedIntoDeadlock
    }

    /// <summary>One reversible step, kept so the player can undo any number of pushes.</summary>
    public struct SokobanMove
    {
        public Vector2Int PlayerFrom;
        public int StatueIndex;        // -1 when the step only moved the guard
        public Vector2Int StatueFrom;
    }

    /// <summary>
    /// Grid logic for the ground floor: the guard pushes wooden statues back onto the floor marks.
    /// Pure C# so the rules can be tested without a scene. The puzzle never locks the player out:
    /// undo and reset are always available, and a corner trap is reported as a warning.
    /// </summary>
    public sealed class SokobanBoard
    {
        public int Width { get; }
        public int Height { get; }

        readonly bool[] walls;
        readonly Vector2Int[] statuesStart;
        readonly Vector2Int[] targets;
        readonly Vector2Int playerStart;

        readonly Stack<SokobanMove> history = new Stack<SokobanMove>();

        public Vector2Int[] Statues { get; private set; }
        public Vector2Int Player { get; private set; }

        public IReadOnlyList<Vector2Int> Targets => targets;
        public int MoveCount => history.Count;
        public bool CanUndo => history.Count > 0;
        public int StatueCount => Statues.Length;

        public SokobanBoard(int width, int height, Vector2Int player, Vector2Int[] statues,
                            Vector2Int[] targets, bool[] walls)
        {
            Width = width;
            Height = height;
            this.walls = walls;
            playerStart = player;
            statuesStart = (Vector2Int[])statues.Clone();
            this.targets = (Vector2Int[])targets.Clone();
            Statues = (Vector2Int[])statues.Clone();
            Player = player;
        }

        public bool InBounds(Vector2Int cell) =>
            cell.x >= 0 && cell.y >= 0 && cell.x < Width && cell.y < Height;

        public bool IsWall(Vector2Int cell) =>
            !InBounds(cell) || walls[cell.y * Width + cell.x];

        public bool IsFree(Vector2Int cell) => !IsWall(cell) && StatueAt(cell) < 0;

        public int StatueAt(Vector2Int cell)
        {
            for (int i = 0; i < Statues.Length; i++)
                if (Statues[i] == cell) return i;
            return -1;
        }

        public bool IsTarget(Vector2Int cell)
        {
            for (int i = 0; i < targets.Length; i++)
                if (targets[i] == cell) return true;
            return false;
        }

        public bool IsSolved()
        {
            for (int i = 0; i < targets.Length; i++)
                if (StatueAt(targets[i]) < 0) return false;
            return true;
        }

        /// <summary>True when the statue can no longer be pushed out of its cell.</summary>
        public bool IsStuck(Vector2Int statueCell)
        {
            if (IsTarget(statueCell)) return false;
            bool blockedHorizontal = IsWall(statueCell + Vector2Int.left) || IsWall(statueCell + Vector2Int.right);
            bool blockedVertical = IsWall(statueCell + Vector2Int.up) || IsWall(statueCell + Vector2Int.down);
            return blockedHorizontal && blockedVertical;
        }

        public SokobanMoveResult TryMove(Vector2Int direction)
        {
            if (direction == Vector2Int.zero) return SokobanMoveResult.Blocked;
            if (Mathf.Abs(direction.x) + Mathf.Abs(direction.y) != 1) return SokobanMoveResult.Blocked;

            Vector2Int next = Player + direction;
            if (IsWall(next)) return SokobanMoveResult.Blocked;

            int statue = StatueAt(next);
            if (statue < 0)
            {
                history.Push(new SokobanMove { PlayerFrom = Player, StatueIndex = -1, StatueFrom = next });
                Player = next;
                return SokobanMoveResult.Moved;
            }

            Vector2Int beyond = next + direction;
            if (!IsFree(beyond)) return SokobanMoveResult.Blocked;

            history.Push(new SokobanMove { PlayerFrom = Player, StatueIndex = statue, StatueFrom = next });
            Player = next;
            Statues[statue] = beyond;
            return IsStuck(beyond) ? SokobanMoveResult.PushedIntoDeadlock : SokobanMoveResult.Pushed;
        }

        /// <summary>
        /// Keeps the board in step with the guard's cell. The guard walks freely, so he can be
        /// between cells when a push starts; aligning first keeps the push on the grid.
        /// </summary>
        public void AlignTo(Vector2Int cell)
        {
            if (IsWall(cell)) return;
            if (StatueAt(cell) >= 0) return;
            Player = cell;
        }

        public bool Undo()        {
            if (history.Count == 0) return false;
            SokobanMove move = history.Pop();
            Player = move.PlayerFrom;
            if (move.StatueIndex >= 0) Statues[move.StatueIndex] = move.StatueFrom;
            return true;
        }

        public void Reset()
        {
            history.Clear();
            Player = playerStart;
            for (int i = 0; i < Statues.Length; i++) Statues[i] = statuesStart[i];
        }

        /// <summary>Statues currently off their marks, used by the HUD objective line.</summary>
        public int PendingStatues()
        {
            int pending = 0;
            for (int i = 0; i < targets.Length; i++)
                if (StatueAt(targets[i]) < 0) pending++;
            return pending;
        }
    }
}
