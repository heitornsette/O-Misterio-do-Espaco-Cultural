using System.IO;
using Misterio.Core;
using Misterio.Puzzles.Floor1;
using Misterio.Puzzles.Floor2;
using Misterio.Puzzles.Floor3;
using Misterio.Puzzles.Floor4;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Misterio.EditorTools
{
    /// <summary>
    /// Acceptance gate for the puzzle floors: opens each scene, drives the real controller through a
    /// complete solution and reports whether the floor completes. This proves the puzzles are
    /// solvable with the geometry actually stored in the scene, not just in the unit tests.
    /// </summary>
    public static class PuzzleSolvabilityProbe
    {
        const System.Reflection.BindingFlags Any =
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic;

        [MenuItem("Misterio/Probe Puzzle Solvability")]
        public static void Probe()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/PuzzleProbe"));
            Directory.CreateDirectory(root);
            var report = new System.Collections.Generic.List<string>();

            report.Add("Floor1: " + SolveFloor1());
            report.Add("Floor2: " + SolveFloor2());
            report.Add("Floor3: " + SolveFloor3());
            report.Add("Floor4: " + SolveFloor4());

            File.WriteAllLines(Path.Combine(root, "report.txt"), report);
            Debug.Log("[PuzzleProbe]\n" + string.Join("\n", report));
            EditorApplication.Exit(0);
        }

        static object Call(object target, string method, params object[] args)
        {
            var info = target.GetType().GetMethod(method, Any);
            if (info == null) return "MISSING " + method;
            return info.Invoke(target, args);
        }

        static object Field(object target, string name)
        {
            var info = target.GetType().GetField(name, Any);
            if (info != null) return info.GetValue(target);
            // Several floor properties (TotalPieces, CollectedPieces) are read-only properties.
            var property = target.GetType().GetProperty(name, Any);
            return property?.GetValue(target);
        }

        static MonoBehaviour OpenController(string sceneName, string typeName)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/" + sceneName + ".unity", OpenSceneMode.Single);
            MonoBehaviour found = null;
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour == null) continue;
                Call(behaviour, "Awake");
                if (behaviour.GetType().Name == typeName) found = behaviour;
            }
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour == null) continue;
                Call(behaviour, "Start");
            }
            return found;
        }

        /// <summary>Uses the BFS solver, which searches pushes instead of individual steps.</summary>
        static string SolveFloor1()
        {
            var controller = OpenController("Floor1_Estatuas", "Floor1Controller");
            if (controller == null) return "controller not found";

            var board = Field(controller, "board") as SokobanBoard;
            if (board == null) return "board not built";

            var solution = SokobanSolver.Solve(board);
            if (solution == null) return "NO SOLUTION EXISTS";

            // Replay the solution through the public board API to prove it is a real solution.
            foreach (var step in solution) board.TryMove(step);
            return board.IsSolved()
                ? "SOLVED with " + solution.Count + " moves"
                : "solver returned a path but the board disagrees";
        }

        static string SolveFloor2()
        {
            var controller = OpenController("Floor2_Galeria", "Floor2Controller");
            if (controller == null) return "controller not found";

            var puzzle = Field(controller, "puzzle") as GalleryPuzzle;
            if (puzzle == null) return "puzzle not built";

            // Try every permutation through the public API: exactly one must solve it.
            var indices = new[] { 0, 1, 2 };
            int solvedBy = 0;
            foreach (var a in indices)
                foreach (var b in indices)
                    foreach (var c in indices)
                    {
                        if (a == b || a == c || b == c) continue;
                        puzzle.Reset();
                        puzzle.Place(a, 0);
                        puzzle.Place(b, 1);
                        puzzle.Place(c, 2);
                        if (puzzle.IsSolved()) solvedBy++;
                    }

            return solvedBy == 1
                ? "SOLVED by exactly 1 of 6 arrangements"
                : "AMBIGUOUS: " + solvedBy + " arrangements solve it";
        }

        static string SolveFloor3()
        {
            var controller = OpenController("Floor3_Acervo", "Floor3Controller");
            if (controller == null) return "controller not found";

            int total = (int)Field(controller, "TotalPieces");
            for (int i = 0; i < total; i++) Call(controller, "Collect", i);

            int collected = (int)Field(controller, "CollectedPieces");
            return collected == total ? "SOLVED with " + total + " pieces" : "NOT SOLVED (" + collected + "/" + total + ")";
        }

        static string SolveFloor4()
        {
            var controller = OpenController("Floor4_Salao", "Floor4Controller");
            if (controller == null) return "controller not found";

            var circuit = Field(controller, "circuit") as ConduitCircuit;
            var solvedMasks = Field(controller, "solvedMasks") as byte[];
            if (circuit == null || solvedMasks == null) return "circuit not built";

            int turns = 0;
            for (int y = 0; y < circuit.Height; y++)
            {
                for (int x = 0; x < circuit.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    byte wanted = solvedMasks[y * circuit.Width + x];
                    if (wanted == 0) continue;
                    int guard = 0;
                    while (circuit.Get(cell).Mask != wanted && guard < 4)
                    {
                        circuit.Turn(cell);
                        turns++;
                        guard++;
                    }
                }
            }
            return circuit.IsSolved() ? "SOLVED with " + turns + " turns" : "NOT SOLVED";
        }
    }
}
