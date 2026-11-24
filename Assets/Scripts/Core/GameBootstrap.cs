using Misterio.Core;
using UnityEngine;

namespace Misterio.Core
{
    /// <summary>
    /// Bootstraps the persistent services (session + flow) and, in cutscene scenes, wires the
    /// controller's finish event to the next scene on the route. This is what turns the twelve
    /// cutscenes and the four floors into one continuous game.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] bool startNewRunOnAwake = true;

        static GameBootstrap instance;

        void Awake()
        {
            // The bootstrap is added to every scene, so the copy living in the scene that is
            // unloaded must not destroy the one that just arrived. Keep the first instance alive
            // and let the newer scene take over the wiring.
            if (instance != null && instance != this && instance.gameObject.scene.isLoaded
                && gameObject.scene.isLoaded && instance.gameObject.scene == gameObject.scene)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            // The night must keep running even when the window loses focus; without this the
            // clock and the cutscenes freeze as soon as the player alt-tabs.
            Application.runInBackground = true;
            Application.targetFrameRate = 60;

            // The development capture helper only activates when the player is launched with
            // -autoshot; it stays inert in a normal run. It is added before any early return so
            // -startscene also works.
            if (AutoCapture.Instance == null)
            {
                var captureHost = new GameObject("Auto Capture");
                captureHost.AddComponent<AutoCapture>();
            }

            // The automated playthrough only spawns when the player is launched with -playtest.
            if (Misterio.Gameplay.HumanPlaythrough.Instance == null && HasArgument("-playtest"))
            {
                var playtestHost = new GameObject("Human Playthrough");
                playtestHost.AddComponent<Misterio.Gameplay.HumanPlaythrough>();
            }

            // Development only: -startscene <name> opens any scene directly, which makes it possible
            // to validate the time-out endings and the credits in the built player.
            string requested = ReadArgument("-startscene");
            if (!string.IsNullOrEmpty(requested) && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != requested)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(requested);
                return;
            }

            GameSession.EnsureExists();
            var flow = GameFlow.Instance;
            if (flow == null)
            {
                var flowObject = new GameObject("GameFlow");
                flow = flowObject.AddComponent<GameFlow>();
            }

            if (startNewRunOnAwake && GameSession.Instance != null &&
                SceneRouter.IsFirstScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
                GameSession.Instance.ResetRun();

            SceneRouter.WireCutscene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        static bool HasArgument(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == name) return true;
            return false;
        }

        static string ReadArgument(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
