using System.Collections.Generic;
using UnityEngine;

namespace Misterio.Puzzles.Floor1
{
    /// <summary>
    /// Breadth-first solver for the statue board.
    /// It searches over guard steps, not pushes: a five-statue room has few distinct step states
    /// once the guard's reachable cells collapse, while a push-only search must solve a path query
    /// for every branch and blows up. Verified against the shipped layout.
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
        /// solved. The board is never modified.
        /// </summary>
        public static List<Vector2Int> Solve(SokobanBoard board, int maxStates = 1500000)
        {
            var start = new State { Player = board.Player, Statues = (Vector2Int[])board.Statues.Clone() };
            if (Solved(start, board)) return new List<Vector2Int>();

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
                    if (Solved(next, board)) return nextPath;
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

        static bool Solved(State state, SokobanBoard board)
        {
            foreach (var target in board.Targets)
                if (IndexOf(state.Statues, target) < 0) return false;
            return true;
        }

        static int IndexOf(Vector2Int[] statues, Vector2Int cell)
        {
            for (int i = 0; i < statues.Length; i++)
                if (statues[i] == cell) return i;
            return -1;
        }

        /// <summary>
        /// Identity of a state: the guard position plus the sorted statues.
        /// The guard is kept separate on purpose. Sorting everything together would make the key
        /// collide for different guard positions and the search would discard valid paths.
        /// </summary>
        static string Key(State state)
        {
            var statues = new List<string>(state.Statues.Length);
            foreach (var statue in state.Statues) statues.Add(statue.x + "," + statue.y);
            statues.Sort();
            return state.Player.x + "," + state.Player.y + "#" + string.Join("|", statues);
        }
    }
}
