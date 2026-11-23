using System.Collections.Generic;
using Misterio.Puzzles.Floor1;
using NUnit.Framework;
using UnityEngine;

namespace Misterio.Puzzles.Tests
{
    /// <summary>
    /// The solver is what proves a statue layout is solvable, and it powers the hint. If it reports
    /// "no solution" for a solvable room the player gets stuck forever, so it is tested directly.
    /// </summary>
    public sealed class SokobanSolverTests
    {
        static SokobanBoard Room(int width, int height, Vector2Int player, Vector2Int[] statues, Vector2Int[] targets)
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
            return new SokobanBoard(width, height, player, statues, targets, walls);
        }

        [Test]
        public void AlreadySolvedBoardNeedsNoMoves()
        {
            var board = Room(6, 6, new Vector2Int(2, 2), new[] { new Vector2Int(3, 3) }, new[] { new Vector2Int(3, 3) });
            var solution = SokobanSolver.Solve(board);
            Assert.IsNotNull(solution);
            Assert.AreEqual(0, solution.Count);
        }

        [Test]
        public void OnePushAwayIsSolved()
        {
            var board = Room(7, 5, new Vector2Int(2, 2), new[] { new Vector2Int(3, 2) }, new[] { new Vector2Int(4, 2) });
            var solution = SokobanSolver.Solve(board);
            Assert.IsNotNull(solution, "A single push must be found.");
            foreach (var step in solution) board.TryMove(step);
            Assert.IsTrue(board.IsSolved());
        }

        [Test]
        public void SolverPathActuallySolvesTheBoard()
        {
            var board = Room(8, 6, new Vector2Int(2, 2),
                new[] { new Vector2Int(3, 2), new Vector2Int(3, 4) },
                new[] { new Vector2Int(5, 2), new Vector2Int(5, 4) });
            var solution = SokobanSolver.Solve(board);
            Assert.IsNotNull(solution, "This room is solvable and must be found.");
            foreach (var step in solution) board.TryMove(step);
            Assert.IsTrue(board.IsSolved());
        }

        [Test]
        public void UnreachableTargetReportsNoSolution()
        {
            // The statue is boxed in by walls on three sides and the target is far away.
            var walls = new bool[5 * 5];
            for (int i = 0; i < 25; i++) walls[i] = false;
            // Enclose the statue at (2,2) except from below.
            walls[2 * 5 + 1] = true; // left
            walls[2 * 5 + 3] = true; // right
            walls[3 * 5 + 2] = true; // above
            var board = new SokobanBoard(5, 5, new Vector2Int(2, 1), new[] { new Vector2Int(2, 2) },
                                         new[] { new Vector2Int(3, 4) }, walls);
            Assert.IsNull(SokobanSolver.Solve(board));
        }

        [Test]
        public void FiveStatuesAreSolvedOnTheShippedLayout()
        {
            // Mirrors Floor1Controller.StatueCells / TargetCells so the shipped puzzle is covered.
            var board = Room(12, 8, new Vector2Int(2, 2),
                new[]
                {
                    new Vector2Int(7, 2), new Vector2Int(9, 4), new Vector2Int(8, 6),
                    new Vector2Int(3, 4), new Vector2Int(6, 5)
                },
                new[]
                {
                    new Vector2Int(8, 2), new Vector2Int(9, 5), new Vector2Int(6, 6),
                    new Vector2Int(4, 5), new Vector2Int(7, 4)
                });

            var solution = SokobanSolver.Solve(board);
            Assert.IsNotNull(solution, "The shipped ground-floor layout must be solvable.");
            foreach (var step in solution) board.TryMove(step);
            Assert.IsTrue(board.IsSolved(), "The solver path must actually finish the puzzle.");
        }

        [Test]
        public void HintPointsAtARealMove()
        {
            var board = Room(7, 5, new Vector2Int(2, 2), new[] { new Vector2Int(3, 2) }, new[] { new Vector2Int(4, 2) });
            var hint = SokobanSolver.NextHint(board);
            Assert.IsTrue(hint.HasValue);
            Assert.AreNotEqual(SokobanMoveResult.Blocked, board.TryMove(hint.Value));
        }

        [Test]
        public void SolverDoesNotModifyTheBoard()
        {
            var board = Room(7, 5, new Vector2Int(2, 2), new[] { new Vector2Int(3, 2) }, new[] { new Vector2Int(4, 2) });
            var before = (Vector2Int)board.Player;
            SokobanSolver.Solve(board);
            Assert.AreEqual(before, board.Player);
            Assert.AreEqual(new Vector2Int(3, 2), board.Statues[0]);
        }
    }
}
