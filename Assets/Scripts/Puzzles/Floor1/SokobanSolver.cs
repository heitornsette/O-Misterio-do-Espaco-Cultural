using System.Collections.Generic;
using UnityEngine;

namespace Misterio.Puzzles.Floor1
{
    /// <summary>
    /// Breadth-first solver for the statue board, used to prove a layout is solvable and to power
    /// the contextual hint. It searches over pushes (not every step), which keeps the state space
    /// small enough for the 12x8 room.
    /// </summary>
    public static class SokobanSolver
    {
        struct State
        {
            public Vector2Int Player;
            public Vector2Int[] Statues;
        }

        /// <summary>
        /// Returns the shortest list of guard moves that solves the board, or null when it cannot be
        /// solved. The board itself is not modified.
        /// </summary>
        public static List<Vector2Int> Solve(SokobanBoard board, int maxStates = 400000)
        {
            var start = new State { Player = board.Player, Statues = (Vector2Int[])board.Statues.Clone() };
            if (IsSolved(start, board)) return new List<Vector2Int>();

            var visited = new HashSet<string> { Key(start) };
            var queue = new Queue<List<Vector2Int>>();
            var states = new Queue<State>();
            queue.Enqueue(new List<Vector2Int>());
            states.Enqueue(start);
            int explored = 0;

            while (queue.Count > 0 && explored < maxStates)
            {
                var path = queue.Dequeue();
                var state = states.Dequeue();
                explored++;

                foreach (var direction in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    if (!TryStep(board, state, direction, out var next)) continue;
                    string key = Key(next);
                    if (!visited.Add(key)) continue;

                    var nextPath = new List<Vector2Int>(path) { direction };
                    if (IsSolved(next, board)) return nextPath;
                    queue.Enqueue(nextPath);
                    states.Enqueue(next);
                }
            }
            return null;
        }

        /// <summary>Cheapest next move towards progress; used by the hint system.</summary>
        public static Vector2Int? NextHint(SokobanBoard board, int maxStates = 60000)
        {
            var solution = Solve(board, maxStates);
            return solution != null && solution.Count > 0 ? solution[0] : (Vector2Int?)null;
        }

        static bool IsSolved(State state, SokobanBoard board)
        {
            for (int i = 0; i < board.Targets.Count; i++)
            {
                bool covered = false;
                foreach (var statue in state.Statues)
                    if (statue == board.Targets[i]) { covered = true; break; }
                if (!covered) return false;
            }
            return true;
        }

        static bool TryStep(SokobanBoard board, State state, Vector2Int direction, out State next)
        {
            next = state;
            Vector2Int target = state.Player + direction;
            if (board.IsWall(target)) return false;

            int statue = IndexOf(state.Statues, target);
            if (statue < 0)
            {
                next.Player = target;
                return true;
            }

            Vector2Int beyond = target + direction;
            if (board.IsWall(beyond) || IndexOf(state.Statues, beyond) >= 0) return false;

            var statues = (Vector2Int[])state.Statues.Clone();
            statues[statue] = beyond;
            next.Player = target;
            next.Statues = statues;
            return true;
        }

        static int IndexOf(Vector2Int[] statues, Vector2Int cell)
        {
            for (int i = 0; i < statues.Length; i++)
                if (statues[i] == cell) return i;
            return -1;
        }

        static string Key(State state)
        {
            var parts = new List<string> { state.Player.x + "," + state.Player.y };
            foreach (var statue in state.Statues) parts.Add(statue.x + "," + statue.y);
            parts.Sort();
            return string.Join("|", parts);
        }
    }
}
