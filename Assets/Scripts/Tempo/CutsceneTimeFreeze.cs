using UnityEngine;
using UnityEngine.Playables;

namespace EspacoCultural.Tempo
{
    /// <summary>
    /// Congela relógio, meta e crédito enquanto uma cutscene roda.
    /// Duas formas de usar:
    /// 1) Coloque no mesmo objeto de um PlayableDirector (Timeline): congela no Play e libera ao terminar/parar.
    /// 2) Sem PlayableDirector: congela enquanto o objeto estiver ativo (OnEnable/OnDisable).
    /// Também dá para chamar Begin()/End() por UnityEvent ou por código.
    /// </summary>
    public class CutsceneTimeFreeze : MonoBehaviour
    {
        [Tooltip("Na CC01: ao terminar a cutscene, inicia o relógio (controle liberado).")]
        public bool startClockWhenDone = false;

        private PlayableDirector director;
        private bool frozen;

        private void Awake()
        {
            director = GetComponent<PlayableDirector>();
            if (director != null)
            {
                director.played += _ => Begin();
                director.stopped += _ => End();
            }
        }

        private void OnEnable()
        {
            if (director == null) Begin();
            else if (director.state == PlayState.Playing) Begin();
        }

        private void OnDisable() => End();

        public void Begin()
        {
            if (frozen || GameClock.Instance == null) return;
            frozen = true;
            GameClock.Instance.Freeze();
        }

        public void End()
        {
            if (!frozen || GameClock.Instance == null) return;
            frozen = false;
            GameClock.Instance.Unfreeze();
            if (startClockWhenDone) GameClock.Instance.StartClock();
        }
    }
}
