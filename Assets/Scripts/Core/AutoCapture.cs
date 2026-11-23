using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Misterio.Core
{
    /// <summary>
    /// Development helper: when the game is launched with -autoshot, it captures the game view every
    /// few seconds into a folder and records which scene was running. This is how the built player is
    /// validated without depending on an external screen grabber.
    /// </summary>
    public sealed class AutoCapture : MonoBehaviour
    {
        [SerializeField] float interval = 2f;
        [SerializeField] int maxShots = 140;

        string outputDirectory;
        static int shot;
        bool capturing;
        bool autoPlay;
        static bool started;

        public static AutoCapture Instance { get; private set; }

        void Awake()
        {
            var args = Environment.GetCommandLineArgs();
            string folder = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-autoshot")
                {
                    folder = i + 1 < args.Length && !args[i + 1].StartsWith("-") ? args[i + 1] : "autoshots";
                }
            }
            if (folder == null)
            {
                enabled = false;
                return;
            }
            autoPlay = Array.IndexOf(args, "-autoplay") >= 0;
            outputDirectory = Path.IsPathRooted(folder) ? folder : Path.Combine(Application.persistentDataPath, folder);
            Directory.CreateDirectory(outputDirectory);

            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            // The helper must survive scene loads, otherwise the capture stops at the first
            // transition and the route can never be validated end to end.
            DontDestroyOnLoad(gameObject);
            if (started) return;
            started = true;
            StartCoroutine(Run());
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        IEnumerator Run()
        {
            yield return new WaitForSeconds(1f);
            while (shot < maxShots)
            {
                yield return new WaitForSeconds(interval);
                if (autoPlay) AutoPlay();
                yield return Capture();
            }
            Application.Quit();
        }

        /// <summary>Dev-only: solves the current floor so the whole route can be captured.</summary>
        static void AutoPlay()
        {
            // PuzzleFloor is abstract, so the check is by base type rather than by name.
            foreach (var floor in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (floor == null) continue;
                if (!(floor is Misterio.Gameplay.PuzzleFloor)) continue;
                var solve = floor.GetType().GetMethod("AutoCompleteForCapture",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);
                if (solve != null)
                {
                    Debug.Log("[AutoPlay] solving " + floor.GetType().Name);
                    solve.Invoke(floor, null);
                }
                else
                {
                    Debug.LogWarning("[AutoPlay] no AutoCompleteForCapture on " + floor.GetType().Name);
                }
            }
        }

        IEnumerator Capture()
        {
            capturing = true;
            string name = "shot_" + shot.ToString("00") + "_" + SceneManager.GetActiveScene().name + ".png";
            shot++;
            string path = Path.Combine(outputDirectory, name);
            ScreenCapture.CaptureScreenshot(path);
            // The screenshot is written at the end of the frame.
            yield return null;
            yield return null;
            if (File.Exists(path))
                Debug.Log("[AutoShot] " + path);
            capturing = false;
        }

        void OnGUI()
        {
            if (!capturing) return;
        }
    }
}
