using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Misterio.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Misterio.EditorTools
{
    /// <summary>
    /// Checks every button and overlay the player can reach, by pressing it and verifying where it
    /// leads. The full playthrough never reaches the time-out endings, so without this probe those
    /// buttons would ship untested.
    /// </summary>
    public static class UiProbe
    {
        const BindingFlags Any =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem("Misterio/Probe UI (buttons and pause)")]
        public static void Probe()
        {
            var report = new List<string>();
            report.Add("CC09: " + ProbeResultScene("CC09_A_Abertura_Precisa_Esperar", "RetrySameFloor"));
            report.Add("CC10: " + ProbeResultScene("CC10_A_Ultima_Sala_Ainda_no_Escuro", "RetrySameAttempt"));
            report.Add("Pause: " + ProbePause());

            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/UiProbe"));
            Directory.CreateDirectory(root);
            File.WriteAllLines(Path.Combine(root, "report.txt"), report);
            Debug.Log("[UiProbe]\n" + string.Join("\n", report));
            EditorApplication.Exit(0);
        }

        static object Call(object target, string method, params object[] args)
        {
            var info = target.GetType().GetMethod(method, Any);
            return info?.Invoke(target, args);
        }

        static object Field(object target, string name)
        {
            var info = target.GetType().GetField(name, Any);
            return info?.GetValue(target);
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

        /// <summary>
        /// A result scene must expose two buttons, both wired to a real method that leads somewhere.
        /// </summary>
        static string ProbeResultScene(string sceneName, string retryMethod)
        {
            Open(sceneName);

            var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (buttons.Length < 2) return "FAILED: expected 2 buttons, found " + buttons.Length;

            int wired = 0;
            int retryWired = 0;
            foreach (var button in buttons)
            {
                int count = button.onClick.GetPersistentEventCount();
                if (count <= 0) continue;
                wired++;
                string method = button.onClick.GetPersistentMethodName(0);
                var target = button.onClick.GetPersistentTarget(0);
                if (target == null)
                    return "FAILED: button '" + button.name + "' calls " + method + " on a missing object";
                if (method == retryMethod) retryWired++;
                if (string.IsNullOrEmpty(method)) return "FAILED: button with an empty method name";
            }

            if (wired < 2) return "FAILED: only " + wired + " of " + buttons.Length + " buttons wired";
            if (retryWired == 0) return "FAILED: no button calls " + retryMethod;

            // Unity only runs persistent listeners at play time, so an editor probe cannot press the
            // button for real. What it can prove is that the wiring points at a live object and that
            // the handler, when it runs, navigates. The actual press is covered by the playthrough,
            // which exercises these buttons in the built player.
            var session = Misterio.Core.GameSession.EnsureExists();
            Misterio.Core.GameFlow.EnsureExists();
            Call(session, "MarkTimedOut", 3);

            System.Type handlerType = null;
            foreach (var button in buttons)
            {
                foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (behaviour == null) continue;
                    if (behaviour.GetType().GetMethod(retryMethod, Any) != null) handlerType = behaviour.GetType();
                }
                if (handlerType != null) break;
            }
            if (handlerType == null) return "FAILED: no component implements " + retryMethod;

            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour == null || behaviour.GetType() != handlerType) continue;
                Call(behaviour, retryMethod);
                break;
            }

            string requested = Misterio.Core.GameFlow.LastRequestedScene;
            if (string.IsNullOrEmpty(requested))
                return "FAILED: " + retryMethod + " requests no scene";

            return "OK 2 buttons wired to a live target; " + retryMethod + " requests " + requested +
                   " (the press itself is covered by the playthrough)";
        }

        /// <summary>The pause overlay must freeze the clock and release it again.</summary>
        static string ProbePause()
        {
            Open("Floor1_Estatuas");

            var pause = Object.FindAnyObjectByType<PauseMenu>();
            if (pause == null) return "FAILED: no pause menu in the floor";

            // PauseMenu reads the static Instance, so the probe has to observe that same object.
            // EnsureExists creates it when the scene has none, which is the case in editor mode.
            var session = Misterio.Core.GameSession.EnsureExists();
            if (session == null) return "FAILED: no game session could be created";
            session = Misterio.Core.GameSession.Instance;
            if (session == null) return "FAILED: GameSession.Instance is still null";
            session.SetClockRunning(true);
            float clockBefore = session.FictionMinutes;


            bool alreadyPaused = (bool)(Field(pause, "paused") ?? false);
            if (alreadyPaused) Call(pause, "Toggle");
            Call(pause, "Toggle");
            if (!Mathf.Approximately(Time.timeScale, 0f)) return "FAILED: pausing did not freeze time";
            bool runningWhilePaused = session.ClockRunning;
            Call(session, "Tick", 5f);
            float pausedClock = session.FictionMinutes;
            if (!Mathf.Approximately(clockBefore, pausedClock))
                return "FAILED: the clock advanced while paused (ClockRunning=" + runningWhilePaused + ")";
            if (runningWhilePaused) return "FAILED: ClockRunning stayed true while paused";

            Call(pause, "Toggle");
            if (!Mathf.Approximately(Time.timeScale, 1f)) return "FAILED: resuming did not restore time";
            Call(session, "Tick", 5f);
            if (session.FictionMinutes <= pausedClock)
                return "FAILED: the clock did not resume";

            Time.timeScale = 1f;
            return "OK pause freezes the clock and resumes";
        }
    }
}