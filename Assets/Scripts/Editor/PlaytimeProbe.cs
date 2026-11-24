using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Misterio.Gameplay;
using Misterio.Puzzles;
using Misterio.Puzzles.Floor1;
using Misterio.Puzzles.Floor4;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Misterio.EditorTools
{
    /// <summary>
    /// Measures how long a competent player needs, from real scene data instead of guesswork.
    /// The number is built from three measured parts: the authored cutscene durations, the distance
    /// the guard must physically walk to finish each puzzle, and a fixed interaction overhead per
    /// action. Reading and thinking are reported separately so the result stays honest.
    /// </summary>
    public static class PlaytimeProbe
    {
        const BindingFlags Any =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>Seconds of walking per world unit, taken from the guard's own move speed.</summary>
        const float GuardSpeed = 1.15f;

        /// <summary>Seconds an interaction costs even when the guard is already in range.</summary>
        const float InteractionCost = 0.45f;

        [MenuItem("Misterio/Probe Playtime")]
        public static void Probe()
        {
            var report = new List<string>();

            float cutscenes = CutsceneSeconds();
            report.Add("cutscenes (rota de sucesso): " + cutscenes.ToString("F1") + "s");

            var floor1 = MeasureFloor1();
            var floor2 = MeasureFloor2();
            var floor3 = MeasureFloor3();
            var floor4 = MeasureFloor4();
            float exterior = MeasureExterior();
            float epilogue = MeasureEpilogue();

            report.Add("exterior (chegada): " + exterior.ToString("F1") + "s");
            report.Add("exterior (epílogo): " + epilogue.ToString("F1") + "s");
            report.Add("andar 1: " + floor1.ToString("F1") + "s");
            report.Add("andar 2: " + floor2.ToString("F1") + "s");
            report.Add("andar 3: " + floor3.ToString("F1") + "s");
            report.Add("andar 4: " + floor4.ToString("F1") + "s");

            float total = cutscenes + exterior + epilogue + floor1 + floor2 + floor3 + floor4;
            report.Add("---");
            report.Add("PISO MECANICO (sem pensar): " + total.ToString("F1") + "s = " + (total / 60f).ToString("F1") + " min");

            // A first-time player reads the plaques, studies the patrol and retries. Those are
            // reported as ranges rather than folded into the mechanical floor.
            report.Add("leitura das placas: 15-30s");
            report.Add("observacao da patrulha: 20-45s");
            report.Add("erros e novas tentativas: 30-90s");
            float low = total + 65f;
            float high = total + 165f;
            report.Add("PRIMEIRA PARTIDA estimada: " + (low / 60f).ToString("F1") + "-" + (high / 60f).ToString("F1") + " min");

            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/PlaytimeProbe"));
            Directory.CreateDirectory(path);
            File.WriteAllLines(Path.Combine(path, "report.txt"), report);
            Debug.Log("[Playtime]\n" + string.Join("\n", report));
            EditorApplication.Exit(0);
        }

        /// <summary>Sum of the authored durations of the cutscenes on the success route.</summary>
        static float CutsceneSeconds()
        {
            float total = 0f;
            foreach (string scene in new[]
            {
                RouteRules.Cutscene01, RouteRules.Cutscene02, RouteRules.Cutscene03,
                RouteRules.Cutscene04, RouteRules.Cutscene05, RouteRules.Cutscene06,
                RouteRules.Cutscene07, RouteRules.Cutscene08
            })
            {
                var duration = ReadDuration(scene);
                total += duration;
            }
            return total + 31f; // credits
        }

        static float ReadDuration(string sceneName)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/" + sceneName + ".unity", OpenSceneMode.Single);
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour == null) continue;
                var field = behaviour.GetType().GetField("Duration",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (field?.GetValue(null) is float value && value > 0f) return value;
            }
            return 0f;
        }

        static void Open(string sceneName)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/" + sceneName + ".unity", OpenSceneMode.Single);
            var behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var behaviour in behaviours)
                if (behaviour != null && behaviour.isActiveAndEnabled)
                    behaviour.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);
            foreach (var behaviour in behaviours)
                if (behaviour != null) Call(behaviour, "Awake");
            foreach (var behaviour in behaviours)
                if (behaviour != null) Call(behaviour, "Start");
        }

        static object Call(object target, string method, params object[] args)
        {
            var info = target.GetType().GetMethod(method, Any);
            return info?.Invoke(target, args);
        }

        static object Field(object target, string name)
        {
            var info = target.GetType().GetField(name, Any);
            if (info != null) return info.GetValue(target);
            return target.GetType().GetProperty(name, Any)?.GetValue(target);
        }

        /// <summary>Walking distance of the solver's solution, divided by the guard's speed.</summary>
        static float MeasureFloor1()
        {
            Open("Floor1_Estatuas");
            var controller = Object.FindAnyObjectByType<Floor1Controller>();
            if (controller == null) return 0f;
            var board = Field(controller, "board") as SokobanBoard;
            if (board == null) return 0f;

            var solution = SokobanSolver.Solve(board);
            if (solution == null) return 0f;

            // Each solver step moves one cell; the cell size comes from the controller.
            Vector2 cell = (Vector2)Field(controller, "cellSize");
            float stepDistance = (cell.x + cell.y) * 0.5f;
            float walking = solution.Count * stepDistance / GuardSpeed;
            return walking + solution.Count * InteractionCost;
        }

        /// <summary>Two round trips: painting to hook, three times.</summary>
        static float MeasureFloor2()
        {
            Open("Floor2_Galeria");
            var controller = Object.FindAnyObjectByType<Floor2Controller>();
            if (controller == null) return 0f;

            var paintings = Object.FindObjectsByType<PaintingInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var hooks = Object.FindObjectsByType<HookInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (paintings.Length == 0 || hooks.Length == 0) return 0f;

            // Worst case: each painting is carried from its spot to the farthest hook.
            float total = 0f;
            foreach (var painting in paintings)
            {
                float farthest = 0f;
                foreach (var hook in hooks)
                    farthest = Mathf.Max(farthest, Vector2.Distance(painting.InteractPoint, hook.InteractPoint));
                total += farthest / GuardSpeed * 2f;   // there and back for the next painting
                total += InteractionCost * 2f;         // pick up, hang
            }
            return total;
        }

        /// <summary>Collecting every piece: a route that visits all of them plus the patrol waits.</summary>
        static float MeasureFloor3()
        {
            Open("Floor3_Acervo");
            var controller = Object.FindAnyObjectByType<Floor3Controller>();
            if (controller == null) return 0f;

            var pieces = Object.FindObjectsByType<PieceInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (pieces.Length == 0) return 0f;

            var points = new List<Vector2>();
            foreach (var piece in pieces) points.Add(piece.InteractPoint);
            var guard = Object.FindAnyObjectByType<GuardActor>();
            Vector2 start = guard != null ? guard.Position : Vector2.zero;

            // Greedy nearest-neighbour route from the guard's start.
            float distance = 0f;
            Vector2 current = start;
            var remaining = new List<Vector2>(points);
            while (remaining.Count > 0)
            {
                int best = 0;
                float bestDistance = float.MaxValue;
                for (int i = 0; i < remaining.Count; i++)
                {
                    float d = Vector2.Distance(current, remaining[i]);
                    if (d < bestDistance) { bestDistance = d; best = i; }
                }
                distance += bestDistance;
                current = remaining[best];
                remaining.RemoveAt(best);
            }

            // Waiting for the patrol is part of the puzzle: two crossings, each with a wait.
            float patrolWait = 24f;
            return distance / GuardSpeed + pieces.Length * InteractionCost + patrolWait;
        }

        /// <summary>Cursor movement and turns at a human pace.</summary>
        static float MeasureFloor4()
        {
            Open("Floor4_Salao");
            var controller = Object.FindAnyObjectByType<Floor4Controller>();
            if (controller == null) return 0f;
            var circuit = Field(controller, "circuit") as ConduitCircuit;
            var solved = Field(controller, "solvedMasks") as byte[];
            if (circuit == null || solved == null) return 0f;

            // Count the turns needed, then price cursor travel between the tiles.
            int turns = 0;
            float cursorTravel = 0f;
            var cursor = circuit.Source;
            for (int y = 0; y < circuit.Height; y++)
            {
                for (int x = 0; x < circuit.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    byte wanted = solved[y * circuit.Width + x];
                    if (wanted == 0) continue;
                    int guard = 0;
                    int needed = 0;
                    var probe = circuit.Get(cell).Mask;
                    while (probe != wanted && guard < 4)
                    {
                        probe = ConduitPiece.Rotate(probe, 1);
                        needed++;
                        guard++;
                    }
                    turns += needed;
                    cursorTravel += Mathf.Abs(cell.x - cursor.x) + Mathf.Abs(cell.y - cursor.y);
                    cursor = cell;
                }
            }

            const float cursorStep = 0.12f;
            const float turnCost = 0.35f;
            return cursorTravel * cursorStep + turns * turnCost;
        }

        /// <summary>Crossing the dawn lawn to the tree where Moema rests.</summary>
        static float MeasureEpilogue()
        {
            Open("Epilogue_Gramado");
            var epilogue = Object.FindAnyObjectByType<EpilogueScene>();
            if (epilogue == null) return 0f;

            float left = (float)Field(epilogue, "worldLeft");
            float tree = (float)Field(epilogue, "treeX");
            float distance = Mathf.Abs(tree - left);
            return distance / GuardSpeed + 1.6f; // plus the closing beat
        }

        /// <summary>Crossing the campus at walking speed.</summary>
        static float MeasureExterior()
        {
            Open("Exterior_Chegada");
            var exterior = Object.FindAnyObjectByType<ExteriorScene>();
            if (exterior == null) return 0f;

            float left = (float)Field(exterior, "worldLeft");
            float right = (float)Field(exterior, "entranceX");
            float distance = Mathf.Abs(right - left);
            return distance / GuardSpeed + 1f; // plus the enter transition
        }
    }
}
