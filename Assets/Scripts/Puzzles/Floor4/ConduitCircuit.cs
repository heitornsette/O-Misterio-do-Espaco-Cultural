using System.Collections.Generic;
using UnityEngine;

namespace Misterio.Puzzles.Floor4
{
    /// <summary>
    /// One conduit tile. The connection mask uses four bits (N = 1, E = 2, S = 4, W = 8) and
    /// rotating the tile is just rotating those bits, which keeps the graph model exact.
    /// </summary>
    public struct ConduitPiece
    {
        public byte Mask;       // connections in the *current* orientation
        public byte BaseMask;   // connections when Rotation == 0
        public int Rotation;    // 0..3 clockwise quarter turns

        public bool HasNorth => (Mask & 1) != 0;
        public bool HasEast => (Mask & 2) != 0;
        public bool HasSouth => (Mask & 4) != 0;
        public bool HasWest => (Mask & 8) != 0;

        public static byte Rotate(byte mask, int turns)
        {
            int value = mask & 0xF;
            for (int i = 0; i < ((turns % 4) + 4) % 4; i++)
                value = ((value << 1) | (value >> 3)) & 0xF;
            return (byte)value;
        }

        public void SetRotation(int turns)
        {
            Rotation = ((turns % 4) + 4) % 4;
            Mask = Rotate(BaseMask, Rotation);
        }

        public void TurnClockwise() => SetRotation(Rotation + 1);
    }

    /// <summary>
    /// The main hall panel: rotate conduits until the energy walks from the source to the panel.
    /// Connectivity is a breadth-first search over the 4-bit connection graph, so a solution is
    /// always verified instead of assumed.
    /// </summary>
    public sealed class ConduitCircuit
    {
        public const int North = 1, East = 2, South = 4, West = 8;

        public int Width { get; }
        public int Height { get; }

        readonly ConduitPiece[] pieces;
        readonly bool[] fixedTile;
        readonly Vector2Int source;
        readonly Vector2Int target;

        public ConduitCircuit(int width, int height, Vector2Int source, Vector2Int target,
                              byte[] baseMasks, bool[] locked = null)
        {
            Width = width;
            Height = height;
            this.source = source;
            this.target = target;
            pieces = new ConduitPiece[width * height];
            fixedTile = locked ?? new bool[width * height];
            for (int i = 0; i < pieces.Length; i++)
            {
                pieces[i] = new ConduitPiece { BaseMask = (byte)(baseMasks[i] & 0xF), Rotation = 0 };
                pieces[i].SetRotation(0);
            }
            if (!IsValid(source) || !IsValid(target))
                throw new System.ArgumentException("Source and target must be inside the panel.");
        }

        public bool IsValid(Vector2Int cell) =>
            cell.x >= 0 && cell.y >= 0 && cell.x < Width && cell.y < Height;

        int Index(Vector2Int cell) => cell.y * Width + cell.x;

        public ConduitPiece Get(Vector2Int cell) => pieces[Index(cell)];

        public bool IsLocked(Vector2Int cell) => fixedTile[Index(cell)];

        public Vector2Int Source => source;
        public Vector2Int Target => target;

        /// <summary>Rotates one tile clockwise. Locked tiles and out-of-range cells are ignored.</summary>
        public bool Turn(Vector2Int cell)
        {
            if (!IsValid(cell) || IsLocked(cell)) return false;
            pieces[Index(cell)].TurnClockwise();
            return true;
        }

        static Vector2Int Neighbour(Vector2Int cell, int direction)
        {
            switch (direction)
            {
                case North: return cell + Vector2Int.up;
                case East: return cell + Vector2Int.right;
                case South: return cell + Vector2Int.down;
                default: return cell + Vector2Int.left;
            }
        }

        static int Opposite(int direction)
        {
            switch (direction)
            {
                case North: return South;
                case East: return West;
                case South: return North;
                default: return East;
            }
        }

        static bool Has(byte mask, int direction) => (mask & direction) != 0;

        /// <summary>True when the two neighbouring tiles expose sockets that face each other.</summary>
        public bool AreConnected(Vector2Int a, Vector2Int b)
        {
            if (!IsValid(a) || !IsValid(b)) return false;
            Vector2Int delta = b - a;
            int direction;
            if (delta == Vector2Int.up) direction = North;
            else if (delta == Vector2Int.right) direction = East;
            else if (delta == Vector2Int.down) direction = South;
            else if (delta == Vector2Int.left) direction = West;
            else return false;

            return Has(Get(a).Mask, direction) && Has(Get(b).Mask, Opposite(direction));
        }

        /// <summary>Breadth-first search from the source; returns every tile the energy reaches.</summary>
        public HashSet<Vector2Int> PoweredTiles()
        {
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(source);
            visited.Add(source);
            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                foreach (int direction in new[] { North, East, South, West })
                {
                    if (!Has(Get(cell).Mask, direction)) continue;
                    Vector2Int next = Neighbour(cell, direction);
                    if (!IsValid(next) || visited.Contains(next)) continue;
                    if (!AreConnected(cell, next)) continue;
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
            return visited;
        }

        public bool IsSolved() => PoweredTiles().Contains(target);

        /// <summary>
        /// Builds a solvable panel by starting from the solved layout and scrambling it.
        /// Random layouts can be unsolvable, so the game never generates one that way.
        /// </summary>
        public static byte[] ScrambleFrom(byte[] solvedMasks, int[] turns, int width, int height)
        {
            var result = new byte[solvedMasks.Length];
            for (int i = 0; i < solvedMasks.Length; i++)
                result[i] = ConduitPiece.Rotate(solvedMasks[i], turns[i % turns.Length]);
            return result;
        }

        /// <summary>Number of tiles that still need at least one turn (used by hints).</summary>
        public int MisalignedCount(byte[] solvedMasks)
        {
            int count = 0;
            for (int i = 0; i < pieces.Length; i++)
            {
                if ((solvedMasks[i] & 0xF) == 0) continue; // empty panel slot
                if (pieces[i].Mask != (solvedMasks[i] & 0xF)) count++;
            }
            return count;
        }

        /// <summary>Number of conduit pieces on the panel (empty slots are not counted).</summary>
        public int PieceCount(byte[] solvedMasks)
        {
            int count = 0;
            for (int i = 0; i < solvedMasks.Length; i++)
                if ((solvedMasks[i] & 0xF) != 0) count++;
            return count;
        }

        /// <summary>
        /// Derives the solved masks from a declared path plus optional branches. Building the panel
        /// from the route guarantees the layout is solvable; a hard-coded mask table does not.
        /// </summary>
        public static byte[] LayoutFromPath(int width, int height, Vector2Int[] route, Vector2Int[][] branches = null)
        {
            var masks = new byte[width * height];

            void Link(Vector2Int a, Vector2Int b)
            {
                Vector2Int delta = b - a;
                int fromA, fromB;
                if (delta == Vector2Int.up) { fromA = North; fromB = South; }
                else if (delta == Vector2Int.right) { fromA = East; fromB = West; }
                else if (delta == Vector2Int.down) { fromA = South; fromB = North; }
                else if (delta == Vector2Int.left) { fromA = West; fromB = East; }
                else throw new System.ArgumentException("Path steps must be orthogonal neighbours.");

                masks[a.y * width + a.x] |= (byte)fromA;
                masks[b.y * width + b.x] |= (byte)fromB;
            }

            if (route != null)
                for (int i = 0; i + 1 < route.Length; i++) Link(route[i], route[i + 1]);

            if (branches != null)
                foreach (var branch in branches)
                    for (int i = 0; i + 1 < branch.Length; i++) Link(branch[i], branch[i + 1]);

            return masks;
        }
    }
}
