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
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            // The night must keep running even when the window loses focus; without this the
            // clock and the cutscenes freeze as soon as the player alt-tabs.
            Application.runInBackground = true;
            Application.targetFrameRate = 60;

            // Development only: -startscene <name> opens any scene directly, which makes it possible
            // to validate the time-out endings and the credits in the built player.
            string requested = ReadArgument("-startscene");
            if (!string.IsNullOrEmpty(requested) && gameObject.scene.name != requested)
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
                SceneRouter.IsFirstScene(gameObject.scene.name))
                GameSession.Instance.ResetRun();

            SceneRouter.WireCutscene(gameObject.scene.name);

            // The development capture helper only activates when the player is launched with
            // -autoshot; it stays inert in a normal run.
            if (gameObject.GetComponent<AutoCapture>() == null)
                gameObject.AddComponent<AutoCapture>();
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
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
