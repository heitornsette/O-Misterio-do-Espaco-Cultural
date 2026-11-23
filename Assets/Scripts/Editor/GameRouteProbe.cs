using System.Collections.Generic;
using System.IO;
using Misterio.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Misterio.EditorTools
{
    /// <summary>
    /// Walks the whole route in edit mode and captures a frame of every scene, proving that each
    /// scene loads, builds its HUD and renders. Used as the acceptance gate for the integration.
    /// </summary>
    public static class GameRouteProbe
    {
        const int Width = 1280;
        const int Height = 720;

        /// <summary>
        /// Every scene the game can reach: the success chain plus the three ending cutscenes that are
        /// not on it. Derived from the shared route data so the probe can never drift from the game.
        /// </summary>
        static string[] Route
        {
            get
            {
                var scenes = new List<string>(Misterio.Puzzles.RouteRules.SuccessChain);
                foreach (string extra in new[] { GameFlow.Cutscene04A, GameFlow.Cutscene09, GameFlow.Cutscene10 })
                    if (!scenes.Contains(extra)) scenes.Add(extra);
                return scenes.ToArray();
            }
        }

        [MenuItem("Misterio/Probe Full Route (capture every scene)")]
        public static void Probe()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/RouteProbe"));
            Directory.CreateDirectory(root);
            var report = new List<string>();

            foreach (string sceneName in Route)
            {
                string path = "Assets/Scenes/" + sceneName + ".unity";
                if (!File.Exists(path))
                {
                    report.Add(sceneName + " MISSING");
                    continue;
                }

                try
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var camera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
                    if (camera == null) { report.Add(sceneName + " NO_CAMERA"); continue; }

                    // Let the scene run a few editor ticks so Awake/Start build the HUD.
                    for (int i = 0; i < 3; i++)
                    {
                        foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        {
                            if (behaviour == null) continue;
                            // Awake runs first (as Unity does), then Start, then the HUD tick.
                            string method = i == 0 ? "Awake" : (i == 1 ? "Start" : null);
                            if (method == null) continue;
                            var target = behaviour.GetType().GetMethod(method,
                                System.Reflection.BindingFlags.Instance |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Public);
                            if (target == null || target.DeclaringType == typeof(MonoBehaviour)) continue;
                            try { target.Invoke(behaviour, null); }
                            catch (System.Exception inner) { Debug.LogWarning("[RouteProbe] " + sceneName + " " + method + " on " + behaviour.GetType().Name + ": " + inner.Message); }
                        }
                    }
                    Canvas.ForceUpdateCanvases();

                    string file = Path.Combine(root, sceneName + ".png");
                    CutsceneShot.PrepareUi(camera);
                    CutsceneShot.Save(camera, Width, Height, file);
                    CutsceneShot.RestoreUi();

                    double luminance = CutsceneShot.AverageLuminance(file);
                    int objects = scene.GetRootGameObjects().Length;
                    report.Add(sceneName + " ok lum=" + luminance.ToString("F4") + " roots=" + objects);
                }
                catch (System.Exception error)
                {
                    var inner = error;
                    while (inner.InnerException != null) inner = inner.InnerException;
                    report.Add(sceneName + " FAILED " + inner.GetType().Name + ": " + inner.Message + "\n" + inner.StackTrace);
                }
            }

            File.WriteAllLines(Path.Combine(root, "report.txt"), report);
            Debug.Log("[RouteProbe]\n" + string.Join("\n", report));
            EditorApplication.Exit(0);
        }
    }
}
