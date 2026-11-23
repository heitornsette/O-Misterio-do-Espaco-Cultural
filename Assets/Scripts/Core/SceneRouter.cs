using Misterio.Puzzles;
using UnityEngine;
using UnityEngine.Events;

namespace Misterio.Core
{
    /// <summary>
    /// Connects each cutscene controller to the route without editing every scene by hand:
    /// the finish event of the controller found in the scene is appended with a jump to the next
    /// scene, the clock is frozen while the cutscene plays, and the current night time is handed to
    /// the scenes that display it.
    /// </summary>
    public static class SceneRouter
    {
        /// <summary>The very first scene of the game, which starts a fresh run.</summary>
        public static bool IsFirstScene(string sceneName) =>
            sceneName == GameFlow.Exterior || sceneName == GameFlow.Cutscene01;

        /// <summary>
        /// Finds the cutscene controller in the active scene, feeds it the current state and appends
        /// the routing call to its onCutsceneFinished event.
        /// </summary>
        public static bool WireCutscene(string sceneName)
        {
            var controller = FindCutsceneController();
            if (controller == null) return false;

            var session = GameSession.EnsureExists();
            session.SetClockRunning(false);

            FeedState(controller, session);

            var field = controller.GetType().GetField("onCutsceneFinished",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
            var unityEvent = field?.GetValue(controller) as UnityEvent;
            if (unityEvent == null)
            {
                Debug.LogWarning("[Router] no onCutsceneFinished on " + controller.GetType().Name);
                return false;
            }

            string next = ResolveNext(sceneName, session);
            if (string.IsNullOrEmpty(next))
            {
                Debug.Log("[Router] " + sceneName + " is the last scene on the route.");
                return true;
            }

            unityEvent.AddListener(() =>
            {
                Debug.Log("[Router] onCutsceneFinished fired for " + sceneName + " at t=" + Time.timeSinceLevelLoad.ToString("F2"));
                GameSession.Instance?.SetClockRunning(false);
                var current = GameSession.Instance;
                string target = ResolveNext(sceneName, current);
                if (sceneName == GameFlow.Cutscene04A) current?.ConsumeResumeFloor();
                GameFlow.Instance?.Load(target);
            });
            Debug.Log("[Router] " + sceneName + " -> " + next + " (listener armed)");
            return true;
        }

        /// <summary>
        /// Sends the clock (and, for CC09, the floor that ran out of time) to the controller so the
        /// cutscenes show the real night time instead of a placeholder.
        /// </summary>
        static void FeedState(MonoBehaviour controller, GameSession session)
        {
            var clock = controller.GetType().GetMethod("SetGlobalClock");
            if (clock != null && clock.GetParameters().Length == 1)
                clock.Invoke(controller, new object[] { session.ClockText() });

            var floor = controller.GetType().GetMethod("SetIncompleteFloor");
            if (floor != null && floor.GetParameters().Length == 1)
                floor.Invoke(controller, new object[] { Mathf.Clamp(session.TimedOutFloor, 1, 3) });
        }

        /// <summary>
        /// After a failure cutscene the game returns to the floor that was interrupted; otherwise it
        /// follows the success chain. Exposed so the routing rules can be tested without scenes.
        /// </summary>
        public static string ResolveNext(string sceneName, GameSession session)
        {
            if (sceneName == GameFlow.Cutscene04A && session != null)
            {
                string resume = RouteRules.ResumeTarget(session.ResumeFloor);
                if (resume != null) return resume;
            }
            return RouteRules.NextAfter(sceneName);
        }

        static MonoBehaviour FindCutsceneController()
        {
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour == null) continue;
                string name = behaviour.GetType().Name;
                if (name.StartsWith("CC") && name.Contains("CutsceneController")) return behaviour;
                if (name == "CreditsRollController") return behaviour;
            }
            return null;
        }
    }
}
