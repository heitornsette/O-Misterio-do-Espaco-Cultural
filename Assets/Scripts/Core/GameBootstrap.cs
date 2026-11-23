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
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
