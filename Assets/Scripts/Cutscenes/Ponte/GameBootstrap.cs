using System.Reflection;
using EspacoCultural.Tempo;
using UnityEngine;
using UnityEngine.Events;

namespace Misterio.Core
{
    /// <summary>
    /// PONTE entre as cutscenes do repositório da equipe e o nosso jogo.
    /// As cenas CC01–CC10 e Créditos já trazem este componente (mesmo GUID do original),
    /// então ele roda sozinho ao abrir a cena:
    ///  - congela o nosso relógio (GameClock) enquanto a cutscene toca;
    ///  - passa o horário atual para as cenas que mostram o relógio (SetGlobalClock);
    ///  - na CC09, escolhe a variação do andar em que o tempo acabou (SetIncompleteFloor);
    ///  - liga o fim da cutscene à próxima cena da nossa rota (RouteRules.NextAfter);
    ///  - adiciona o CutsceneSkip (segurar ESPAÇO/ENTER/ESC pula a cutscene).
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] bool startNewRunOnAwake = true;   // mantido só para a cena continuar compatível

        private bool froze;

        private void Awake()
        {
            _ = startNewRunOnAwake;
            GameFlow.EnsureExists();

            var controller = FindController();
            if (controller == null) return;

            var clock = GameClock.Instance;
            if (clock != null && clock.State != GameClock.ClockState.Finished)
            {
                clock.Freeze();
                froze = true;
            }

            Feed(controller, clock);
            Wire(controller, gameObject.scene.name);
            BindVolumes();

            // Segurar ESPAÇO/ENTER/ESC pula a cutscene (os Créditos já têm o pulo próprio).
            if (controller.GetType().Name != "CreditsRollController")
                gameObject.AddComponent<CutsceneSkip>().Init(controller, gameObject.scene.name);
        }

        private void OnDestroy()
        {
            if (froze && GameClock.Instance != null) GameClock.Instance.Unfreeze();
        }

        private static void Wire(MonoBehaviour controller, string sceneName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var fieldName in new[] { "onCutsceneFinished", "onCreditsFinished" })
            {
                var ev = controller.GetType().GetField(fieldName, flags)?.GetValue(controller) as UnityEvent;
                if (ev == null) continue;
                ev.AddListener(() =>
                {
                    string next = Misterio.Puzzles.RouteRules.NextAfter(sceneName);
                    if (!string.IsNullOrEmpty(next)) GameFlow.Instance.Load(next);
                });
                return;
            }
        }

        private void Feed(MonoBehaviour controller, GameClock clock)
        {
            try
            {
                var setClock = controller.GetType().GetMethod("SetGlobalClock");
                if (setClock != null && setClock.GetParameters().Length == 1)
                {
                    string text = clock != null && clock.State != GameClock.ClockState.NotStarted ? clock.CurrentTimeText : "02:00";
                    setClock.Invoke(controller, new object[] { text });
                }

                var setFloor = controller.GetType().GetMethod("SetIncompleteFloor");
                int floor = GameSession.Instance.TimedOutFloor;
                if (setFloor != null && setFloor.GetParameters().Length == 1 && floor > 0)
                    controller.StartCoroutine(NextFrame(controller, setFloor, Mathf.Clamp(floor, 1, 3)));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Cutscene] Não deu para passar o estado para a cena: " + e.Message);
            }
        }

        private static System.Collections.IEnumerator NextFrame(MonoBehaviour controller, MethodInfo method, int floor)
        {
            yield return null;
            if (controller != null) method.Invoke(controller, new object[] { floor });
        }

        /// <summary>As fontes de áudio das cutscenes passam a seguir o volume das Configurações.</summary>
        private static void BindVolumes()
        {
            foreach (var src in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (src == null || src.GetComponent<EspacoCultural.Menu.VolumeBinding>() != null) continue;
                bool music = src.loop || src.gameObject.name.ToLowerInvariant().Contains("music") ||
                             src.gameObject.name.ToLowerInvariant().Contains("musica") ||
                             src.gameObject.name.ToLowerInvariant().Contains("trilha");
                float original = src.volume;
                var vb = src.gameObject.AddComponent<EspacoCultural.Menu.VolumeBinding>();
                vb.category = music ? EspacoCultural.Menu.VolumeBinding.Category.Musica : EspacoCultural.Menu.VolumeBinding.Category.Efeitos;
                vb.baseVolume = original;
                vb.enabled = false; vb.enabled = true;   // reaplica com o volume original
            }
        }

        private static MonoBehaviour FindController()
        {
            foreach (var b in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (b == null) continue;
                string n = b.GetType().Name;
                if ((n.StartsWith("CC") && n.Contains("CutsceneController")) || n == "CreditsRollController") return b;
            }
            return null;
        }
    }
}
