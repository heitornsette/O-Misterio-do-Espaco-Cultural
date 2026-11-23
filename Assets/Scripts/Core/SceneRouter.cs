using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Misterio.Core
{
    /// <summary>
    /// Connects each cutscene controller to the route without editing every scene by hand:
    /// the finish event of the controller found in the scene is appended with a jump to the next
    /// scene, and the clock is frozen while the cutscene plays.
    /// </summary>
    public static class SceneRouter
    {
        /// <summary>The very first scene of the game, which starts a fresh run.</summary>
        public static bool IsFirstScene(string sceneName) => sceneName == GameFlow.Cutscene01;

        /// <summary>
        /// Finds the cutscene controller in the active scene and appends the routing call to its
        /// onCutsceneFinished event. Returns false when the scene has no controller.
        /// </summary>
        public static bool WireCutscene(string sceneName)
        {
            var controller = FindCutsceneController();
            if (controller == null) return false;

            var session = GameSession.EnsureExists();
            session.SetClockRunning(false);

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

            string next = GameFlow.NextInChain(sceneName);
            if (string.IsNullOrEmpty(next))
            {
                Debug.Log("[Router] " + sceneName + " is the last scene on the route.");
                return true;
            }

            unityEvent.AddListener(() =>
            {
                GameSession.Instance?.SetClockRunning(false);
                GameFlow.Instance?.Load(next);
            });
            Debug.Log("[Router] " + sceneName + " -> " + next);
            return true;
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
