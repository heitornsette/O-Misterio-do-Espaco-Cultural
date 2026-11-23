using System.Collections.Generic;
using Misterio.Puzzles.Floor1;
using Misterio.Puzzles.Floor2;
using Misterio.Puzzles.Floor3;
using Misterio.Puzzles.Floor4;
using NUnit.Framework;
using UnityEngine;

namespace Misterio.Puzzles.Tests
{
    public sealed class SokobanBoardTests
    {
        /// <summary>Builds an open room with a wall border, so out-of-bounds and walls behave the same.</summary>
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
        public void WalkingIntoAnEmptyCellMovesTheGuard()
        {
            var board = Room(6, 6, new Vector2Int(2, 2), new Vector2Int[0], new Vector2Int[0]);
            Assert.AreEqual(SokobanMoveResult.Moved, board.TryMove(Vector2Int.right));
            Assert.AreEqual(new Vector2Int(3, 2), board.Player);
        }

        [Test]
        public void WalkingIntoAWallIsBlocked()
        {
            var board = Room(6, 6, new Vector2Int(1, 1), new Vector2Int[0], new Vector2Int[0]);
            Assert.AreEqual(SokobanMoveResult.Blocked, board.TryMove(Vector2Int.left));
            Assert.AreEqual(new Vector2Int(1, 1), board.Player);
        }

        [Test]
        public void DiagonalStepsAreRejected()
        {
            var board = Room(6, 6, new Vector2Int(2, 2), new Vector2Int[0], new Vector2Int[0]);
            Assert.AreEqual(SokobanMoveResult.Blocked, board.TryMove(new Vector2Int(1, 1)));
            Assert.AreEqual(SokobanMoveResult.Blocked, board.TryMove(Vector2Int.zero));
        }

        [Test]
        public void PushingMovesTheStatueOneCellForward()
        {
            var board = Room(6, 6, new Vector2Int(2, 2), new[] { new Vector2Int(3, 2) }, new[] { new Vector2Int(4, 2) });
            Assert.AreEqual(SokobanMoveResult.Pushed, board.TryMove(Vector2Int.right));
            Assert.AreEqual(new Vector2Int(3, 2), board.Player);
            Assert.AreEqual(new Vector2Int(4, 2), board.Statues[0]);
        }

        [Test]
        public void PushIsBlockedWhenTheCellBehindTheStatueIsAWall()
        {
            // Room is 5 wide, so the border wall sits at x = 4: the first push lands on (3,1) and the
            // next one would need (4,1), which is wall.
            var board = Room(5, 4, new Vector2Int(1, 1), new[] { new Vector2Int(2, 1) }, new[] { new Vector2Int(3, 1) });
            Assert.AreEqual(SokobanMoveResult.Pushed, board.TryMove(Vector2Int.right));
            Assert.AreEqual(SokobanMoveResult.Blocked, board.TryMove(Vector2Int.right));
            Assert.AreEqual(new Vector2Int(3, 1), board.Statues[0]);
            Assert.AreEqual(new Vector2Int(2, 1), board.Player);
        }

        [Test]
        public void PushIsBlockedWhenAnotherStatueIsInTheWay()
        {
            var board = Room(7, 5, new Vector2Int(2, 2),
                new[] { new Vector2Int(3, 2), new Vector2Int(4, 2) },
                new[] { new Vector2Int(5, 2) });
            Assert.AreEqual(SokobanMoveResult.Blocked, board.TryMove(Vector2Int.right));
            Assert.AreEqual(new Vector2Int(2, 2), board.Player);
        }

        [Test]
        public void UndoRestoresBothGuardAndStatue()
        {
            var board = Room(6, 6, new Vector2Int(2, 2), new[] { new Vector2Int(3, 2) }, new[] { new Vector2Int(4, 2) });
            board.TryMove(Vector2Int.right);
            Assert.IsTrue(board.CanUndo);
            Assert.IsTrue(board.Undo());
            Assert.AreEqual(new Vector2Int(2, 2), board.Player);
            Assert.AreEqual(new Vector2Int(3, 2), board.Statues[0]);
            Assert.IsFalse(board.CanUndo);
        }

        [Test]
        public void UndoWithoutHistoryReturnsFalse()
        {
            var board = Room(6, 6, new Vector2Int(2, 2), new Vector2Int[0], new Vector2Int[0]);
            Assert.IsFalse(board.Undo());
        }

        [Test]
        public void UndoReversesEveryStepInReverseOrder()
        {
            var board = Room(8, 6, new Vector2Int(2, 2), new[] { new Vector2Int(3, 2) }, new[] { new Vector2Int(6, 2) });
            board.TryMove(Vector2Int.right);
            board.TryMove(Vector2Int.right);
            board.TryMove(Vector2Int.right);
            Assert.AreEqual(new Vector2Int(6, 2), board.Statues[0]);
            while (board.Undo()) { }
            Assert.AreEqual(new Vector2Int(2, 2), board.Player);
            Assert.AreEqual(new Vector2Int(3, 2), board.Statues[0]);
        }

        [Test]
        public void ResetRestoresTheInitialLayoutAndClearsHistory()
        {
            var board = Room(6, 6, new Vector2Int(2, 2), new[] { new Vector2Int(3, 2) }, new[] { new Vector2Int(4, 2) });
            board.TryMove(Vector2Int.right);
            board.TryMove(Vector2Int.right);
            board.Reset();
            Assert.AreEqual(new Vector2Int(2, 2), board.Player);
            Assert.AreEqual(new Vector2Int(3, 2), board.Statues[0]);
            Assert.IsFalse(board.CanUndo);
            Assert.AreEqual(0, board.MoveCount);
        }

        [Test]
        public void PuzzleIsSolvedOnlyWhenEveryTargetIsCovered()
        {
            var board = Room(8, 6, new Vector2Int(2, 2),
                new[] { new Vector2Int(3, 2), new Vector2Int(3, 3) },
                new[] { new Vector2Int(4, 2), new Vector2Int(4, 3) });
            Assert.IsFalse(board.IsSolved());
            // Push the upper statue right onto its mark, walk around, then push the lower one right.
            Assert.AreEqual(SokobanMoveResult.Pushed, board.TryMove(Vector2Int.right));
            Assert.IsFalse(board.IsSolved());
            board.TryMove(Vector2Int.left);
            board.TryMove(Vector2Int.up);
            Assert.AreEqual(SokobanMoveResult.Pushed, board.TryMove(Vector2Int.right));
            Assert.IsTrue(board.IsSolved());
        }

        [Test]
        public void StatuePushedIntoACornerIsReportedAsDeadlock()
        {
            // Corner at (1,1): the statue goes to (1,1) from (2,1) pushed left; walls above and left.
            var board = Room(6, 6, new Vector2Int(3, 1), new[] { new Vector2Int(2, 1) }, new[] { new Vector2Int(4, 4) });
            Assert.AreEqual(SokobanMoveResult.PushedIntoDeadlock, board.TryMove(Vector2Int.left));
            Assert.IsTrue(board.IsStuck(new Vector2Int(1, 1)));
        }

        [Test]
        public void StatueOnATargetIsNeverReportedAsStuck()
        {
            var board = Room(6, 6, new Vector2Int(3, 1), new[] { new Vector2Int(2, 1) }, new[] { new Vector2Int(1, 1) });
            Assert.AreEqual(SokobanMoveResult.Pushed, board.TryMove(Vector2Int.left));
            Assert.IsTrue(board.IsSolved());
            Assert.IsFalse(board.IsStuck(new Vector2Int(1, 1)));
        }

        [Test]
        public void PendingStatuesCountsWhatIsLeft()
        {
            var board = Room(8, 6, new Vector2Int(2, 2),
                new[] { new Vector2Int(3, 2), new Vector2Int(3, 3) },
                new[] { new Vector2Int(4, 2), new Vector2Int(4, 3) });
            Assert.AreEqual(2, board.PendingStatues());
            board.TryMove(Vector2Int.right);
            Assert.AreEqual(1, board.PendingStatues());
        }

        [Test]
        public void EveryMoveIsReversible()
        {
            var board = Room(8, 8, new Vector2Int(3, 3), new[] { new Vector2Int(4, 3) }, new[] { new Vector2Int(6, 6) });
            var random = new System.Random(1234);
            var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            for (int i = 0; i < 60; i++)
            {
                board.TryMove(directions[random.Next(directions.Length)]);
                Assert.GreaterOrEqual(board.Player.x, 1);
                Assert.LessOrEqual(board.Player.x, board.Width - 2);
                Assert.GreaterOrEqual(board.Player.y, 1);
                Assert.LessOrEqual(board.Player.y, board.Height - 2);
            }
            while (board.Undo()) { }
            Assert.AreEqual(new Vector2Int(3, 3), board.Player);
            Assert.AreEqual(new Vector2Int(4, 3), board.Statues[0]);
        }
    }

    public sealed class ConduitCircuitTests
    {
        [Test]
        public void RotatingBitsTurnsNorthIntoEast()
        {
            Assert.AreEqual(ConduitCircuit.East, ConduitPiece.Rotate(ConduitCircuit.North, 1));
            Assert.AreEqual(ConduitCircuit.South, ConduitPiece.Rotate(ConduitCircuit.North, 2));
            Assert.AreEqual(ConduitCircuit.West, ConduitPiece.Rotate(ConduitCircuit.North, 3));
            Assert.AreEqual(ConduitCircuit.North, ConduitPiece.Rotate(ConduitCircuit.North, 4));
        }

        [Test]
        public void RotatingAStraightPipeKeepsTwoSockets()
        {
            byte horizontal = ConduitCircuit.East | ConduitCircuit.West;
            byte vertical = ConduitPiece.Rotate(horizontal, 1);
            Assert.AreEqual(ConduitCircuit.North | ConduitCircuit.South, vertical);
        }

        [Test]
        public void SocketsMustFaceEachOtherToConnect()
        {
            // 1x2 panel: left tile has East, right tile has East too (so it does not face West).
            var masks = new byte[] { ConduitCircuit.East, ConduitCircuit.East };
            var circuit = new ConduitCircuit(2, 1, new Vector2Int(0, 0), new Vector2Int(1, 0), masks);
            Assert.IsFalse(circuit.AreConnected(new Vector2Int(0, 0), new Vector2Int(1, 0)));
        }

        [Test]
        public void FacingSocketsConnect()
        {
            var masks = new byte[] { ConduitCircuit.East, ConduitCircuit.West };
            var circuit = new ConduitCircuit(2, 1, new Vector2Int(0, 0), new Vector2Int(1, 0), masks);
            Assert.IsTrue(circuit.AreConnected(new Vector2Int(0, 0), new Vector2Int(1, 0)));
        }

        [Test]
        public void StraightLineReachesTheTarget()
        {
            var masks = new byte[]
            {
                ConduitCircuit.East, ConduitCircuit.East | ConduitCircuit.West, ConduitCircuit.West
            };
            var circuit = new ConduitCircuit(3, 1, new Vector2Int(0, 0), new Vector2Int(2, 0), masks);
            Assert.IsTrue(circuit.IsSolved());
        }

        [Test]
        public void BrokenLineDoesNotReachTheTarget()
        {
            var masks = new byte[]
            {
                ConduitCircuit.East, ConduitCircuit.North | ConduitCircuit.South, ConduitCircuit.West
            };
            var circuit = new ConduitCircuit(3, 1, new Vector2Int(0, 0), new Vector2Int(2, 0), masks);
            Assert.IsFalse(circuit.IsSolved());
        }

        [Test]
        public void TurningTheMiddleTileFixesTheBrokenLine()
        {
            var masks = new byte[]
            {
                ConduitCircuit.East, ConduitCircuit.North | ConduitCircuit.South, ConduitCircuit.West
            };
            var circuit = new ConduitCircuit(3, 1, new Vector2Int(0, 0), new Vector2Int(2, 0), masks);
            Assert.IsFalse(circuit.IsSolved());
            Assert.IsTrue(circuit.Turn(new Vector2Int(1, 0)));
            Assert.IsTrue(circuit.IsSolved());
        }

        [Test]
        public void LockedTilesCannotBeTurned()
        {
            var masks = new byte[] { ConduitCircuit.East, ConduitCircuit.West };
            var locked = new[] { true, false };
            var circuit = new ConduitCircuit(2, 1, new Vector2Int(0, 0), new Vector2Int(1, 0), masks, locked);
            Assert.IsFalse(circuit.Turn(new Vector2Int(0, 0)));
            Assert.AreEqual(ConduitCircuit.East, circuit.Get(new Vector2Int(0, 0)).Mask);
        }

        [Test]
        public void TurningOutsideThePanelIsIgnored()
        {
            var circuit = new ConduitCircuit(2, 2, Vector2Int.zero, new Vector2Int(1, 1),
                new byte[] { 1, 2, 4, 8 });
            Assert.IsFalse(circuit.Turn(new Vector2Int(5, 5)));
            Assert.IsFalse(circuit.Turn(new Vector2Int(-1, 0)));
        }

        [Test]
        public void ScrambledPanelIsAlwaysSolvable()
        {
            var solved = new byte[]
            {
                ConduitCircuit.East, ConduitCircuit.East | ConduitCircuit.West, ConduitCircuit.West
            };
            var scrambled = ConduitCircuit.ScrambleFrom(solved, new[] { 1, 3 }, 3, 1);
            var circuit = new ConduitCircuit(3, 1, new Vector2Int(0, 0), new Vector2Int(2, 0), scrambled);
            Assert.IsFalse(circuit.IsSolved(), "The scrambled panel should not start solved.");

            // Rotating every tile back to the solved mask must solve it.
            for (int x = 0; x < 3; x++)
            {
                var cell = new Vector2Int(x, 0);
                int guard = 0;
                while (circuit.Get(cell).Mask != solved[x] && guard < 4)
                {
                    circuit.Turn(cell);
                    guard++;
                }
            }
            Assert.IsTrue(circuit.IsSolved());
        }

        [Test]
        public void PoweredTilesOnlyFollowsConnectedNeighbours()
        {
            var masks = new byte[]
            {
                ConduitCircuit.East, ConduitCircuit.West,
                ConduitCircuit.North | ConduitCircuit.South, ConduitCircuit.North | ConduitCircuit.South
            };
            // 2x2: top row connected, bottom row isolated.
            var circuit = new ConduitCircuit(2, 2, new Vector2Int(0, 0), new Vector2Int(1, 1), masks);
            var powered = circuit.PoweredTiles();
            Assert.IsTrue(powered.Contains(new Vector2Int(0, 0)));
            Assert.IsTrue(powered.Contains(new Vector2Int(1, 0)));
            Assert.IsFalse(powered.Contains(new Vector2Int(0, 1)));
            Assert.IsFalse(powered.Contains(new Vector2Int(1, 1)));
        }
    }

    public sealed class GalleryPuzzleTests
    {
        static GalleryPuzzle Build()
        {
            var paintings = new[]
            {
                new GalleryPainting { Id = "mare", Title = "Maré de Mucuripe", Year = 1998, Palette = PaletteFamily.Cool, Theme = "litoral", Symbol = "onda" },
                new GalleryPainting { Id = "nevoa", Title = "Névoa de Baturité", Year = 2004, Palette = PaletteFamily.Cool, Theme = "serra", Symbol = "montanha" },
                new GalleryPainting { Id = "sertao", Title = "Luz do Sertão", Year = 1972, Palette = PaletteFamily.Warm, Theme = "sertão", Symbol = "sol" }
            };
            // Clue 1: the warm painting hangs furthest right.
            // Clue 2: the coastal canvas hangs next to the oldest work.
            return new GalleryPuzzle(paintings, new[] { 1, 0, 2 }, new[]
            {
                "A obra de paleta quente fica à direita das demais.",
                "A tela do litoral fica ao lado da obra mais antiga."
            });
        }

        [Test]
        public void StartsEmpty()
        {
            var puzzle = Build();
            Assert.AreEqual(0, puzzle.PlacedCount());
            for (int i = 0; i < puzzle.SlotCount; i++) Assert.IsTrue(puzzle.IsSlotEmpty(i));
        }

        [Test]
        public void PlacingMovesThePaintingBetweenSlots()
        {
            var puzzle = Build();
            Assert.IsTrue(puzzle.Place(0, 0));
            Assert.AreEqual(0, puzzle.PaintingAt(0));
            Assert.IsTrue(puzzle.Place(0, 2));
            Assert.IsTrue(puzzle.IsSlotEmpty(0));
            Assert.AreEqual(0, puzzle.PaintingAt(2));
            Assert.AreEqual(1, puzzle.PlacedCount());
        }

        [Test]
        public void RemovingClearsTheSlot()
        {
            var puzzle = Build();
            puzzle.Place(1, 1);
            puzzle.Remove(1);
            Assert.IsTrue(puzzle.IsSlotEmpty(1));
        }

        [Test]
        public void PuzzleIsSolvedOnlyOnTheExactArrangement()
        {
            var puzzle = Build();
            puzzle.Place(1, 0);
            puzzle.Place(0, 1);
            Assert.IsFalse(puzzle.IsSolved());
            puzzle.Place(2, 2);
            Assert.IsTrue(puzzle.IsSolved());
        }

        [Test]
        public void WrongOrderIsNotSolved()
        {
            var puzzle = Build();
            puzzle.Place(0, 0);
            puzzle.Place(1, 1);
            puzzle.Place(2, 2);
            Assert.IsFalse(puzzle.IsSolved());
            Assert.AreEqual(1, puzzle.CorrectCount()); // only the sertão painting is right
        }

        [Test]
        public void ResetClearsEverySlot()
        {
            var puzzle = Build();
            puzzle.Place(0, 0);
            puzzle.Place(1, 1);
            puzzle.Reset();
            Assert.AreEqual(0, puzzle.PlacedCount());
            Assert.IsFalse(puzzle.IsSolved());
        }

        [Test]
        public void InvalidInputIsRejected()
        {
            var puzzle = Build();
            Assert.IsFalse(puzzle.Place(-1, 0));
            Assert.IsFalse(puzzle.Place(0, -1));
            Assert.IsFalse(puzzle.Place(99, 0));
            Assert.IsFalse(puzzle.Place(0, 99));
        }

        [Test]
        public void CluesLeadToExactlyOneArrangement()
        {
            var puzzle = Build();
            var paintings = puzzle.Paintings;

            int warmIndex = -1, coastalIndex = -1, oldestIndex = -1;
            int oldestYear = int.MaxValue;
            for (int i = 0; i < paintings.Count; i++)
            {
                if (paintings[i].Palette == PaletteFamily.Warm) warmIndex = i;
                if (paintings[i].Theme == "litoral") coastalIndex = i;
                if (paintings[i].Year < oldestYear) { oldestYear = paintings[i].Year; oldestIndex = i; }
            }

            var solutions = new List<int[]>();
            var indices = new[] { 0, 1, 2 };
            foreach (var a in indices)
                foreach (var b in indices)
                    foreach (var c in indices)
                    {
                        if (a == b || a == c || b == c) continue;
                        var arrangement = new[] { a, b, c };
                        int slotOfWarm = System.Array.IndexOf(arrangement, warmIndex);
                        int slotOfCoastal = System.Array.IndexOf(arrangement, coastalIndex);
                        int slotOfOldest = System.Array.IndexOf(arrangement, oldestIndex);
                        bool clueOne = slotOfWarm == 2;
                        bool clueTwo = Mathf.Abs(slotOfCoastal - slotOfOldest) == 1;
                        if (clueOne && clueTwo) solutions.Add(arrangement);
                    }

            Assert.AreEqual(1, solutions.Count, "The clues must determine a single arrangement.");
            var only = solutions[0];
            var expected = new[] { 1, 0, 2 };
            CollectionAssert.AreEqual(expected, only);
        }
        [Test]
        public void LayoutFromPathProducesASolvablePanel()
        {
            var route = new[]
            {
                new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
                new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(4, 2)
            };
            var masks = ConduitCircuit.LayoutFromPath(5, 4, route);
            var circuit = new ConduitCircuit(5, 4, new Vector2Int(0, 1), new Vector2Int(4, 2), masks);
            Assert.IsTrue(circuit.IsSolved(), "A panel derived from the route must already be solved.");
            Assert.AreEqual(6, circuit.PieceCount(masks));
        }

        [Test]
        public void LayoutFromPathRejectsDiagonalSteps()
        {
            var route = new[] { Vector2Int.zero, new Vector2Int(1, 1) };
            Assert.Throws<System.ArgumentException>(() => ConduitCircuit.LayoutFromPath(3, 3, route));
        }

        [Test]
        public void EmptySlotsAreIgnoredByTheAlignmentCounter()
        {
            var route = new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) };
            var masks = ConduitCircuit.LayoutFromPath(4, 1, route);
            var circuit = new ConduitCircuit(4, 1, new Vector2Int(0, 0), new Vector2Int(1, 0), masks);
            Assert.AreEqual(2, circuit.PieceCount(masks));
            Assert.AreEqual(0, circuit.MisalignedCount(masks));
        }
    }

    public sealed class StealthPatrolTests
    {
        static StealthPatrol Patrol()
        {
            return new StealthPatrol(
                new[] { new Vector2(0f, 0f), new Vector2(2f, 0f) },
                speed: 1f, viewDistance: 2f, halfAngleDegrees: 45f, waitAtWaypoint: 0f);
        }

        [Test]
        public void PatrolStartsAtTheFirstWaypoint()
        {
            var patrol = Patrol();
            Assert.AreEqual(Vector2.zero, patrol.Position);
        }

        [Test]
        public void PatrolMovesTowardsTheNextWaypoint()
        {
            var patrol = Patrol();
            patrol.Tick(0.5f);
            Assert.Greater(patrol.Position.x, 0f);
            Assert.AreEqual(0f, patrol.Position.y, 0.001f);
        }

        [Test]
        public void PatrolLoopsBackToTheStart()
        {
            var patrol = Patrol();
            for (int i = 0; i < 40; i++) patrol.Tick(0.1f);
            Assert.LessOrEqual(patrol.Position.x, 2.001f);
            Assert.GreaterOrEqual(patrol.Position.x, -0.001f);
        }

        [Test]
        public void SeesSomethingDirectlyAhead()
        {
            var patrol = Patrol();
            patrol.Tick(0.1f); // facing +x
            Assert.IsTrue(patrol.CanSee(new Vector2(1f, 0f)));
        }

        [Test]
        public void DoesNotSeeBehind()
        {
            var patrol = Patrol();
            patrol.Tick(0.1f);
            Assert.IsFalse(patrol.CanSee(new Vector2(-1f, 0f)));
        }

        [Test]
        public void DoesNotSeeBeyondRange()
        {
            var patrol = Patrol();
            patrol.Tick(0.1f);
            Assert.IsFalse(patrol.CanSee(new Vector2(10f, 0f)));
        }

        [Test]
        public void DoesNotSeeOutsideTheCone()
        {
            var patrol = Patrol();
            patrol.Tick(0.1f);
            // 90 degrees off to the side: outside a 45 degree half-angle cone.
            Assert.IsFalse(patrol.CanSee(new Vector2(0.2f, 1.5f)));
        }

        [Test]
        public void OccluderBlocksTheView()
        {
            var patrol = Patrol();
            patrol.Tick(0.1f);
            var occluder = new[] { new Vector2(0.5f, -1f), new Vector2(0.5f, 1f) };
            Assert.IsTrue(patrol.CanSee(new Vector2(1.5f, 0f)));
            Assert.IsFalse(patrol.CanSee(new Vector2(1.5f, 0f), occluder));
        }

        [Test]
        public void OccluderThatDoesNotCrossTheSightLineStillSees()
        {
            var patrol = Patrol();
            patrol.Tick(0.1f);
            var occluder = new[] { new Vector2(0.5f, 2f), new Vector2(0.5f, 3f) };
            Assert.IsTrue(patrol.CanSee(new Vector2(1.5f, 0f), occluder));
        }

        [Test]
        public void DeterministicTicksProduceIdenticalPositions()
        {
            var first = Patrol();
            var second = Patrol();
            for (int i = 0; i < 25; i++)
            {
                first.Tick(0.05f);
                second.Tick(0.05f);
            }
            Assert.AreEqual(first.Position.x, second.Position.x, 0.0001f);
            Assert.AreEqual(first.Position.y, second.Position.y, 0.0001f);
        }
    }
}
