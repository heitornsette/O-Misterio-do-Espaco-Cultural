using UnityEngine;
using UnityEngine.Events;

namespace EspacoCultural.Tempo
{
    /// <summary>
    /// Coloque um em cada cena de andar. Liga o puzzle do andar ao relógio global.
    /// - Quando o controle é devolvido ao jogador (fim da cutscene de entrada), chame BeginPuzzle()
    ///   — ou marque "Begin On Start" se o andar não tiver cutscene de entrada.
    /// - Quando o puzzle for resolvido, o script do puzzle chama Complete().
    /// </summary>
    public class PuzzleTimer : MonoBehaviour
    {
        [Range(1, 4)] public int floor = 1;
        [Tooltip("Começa a meta automaticamente no Start da cena.")]
        public bool beginOnStart = true;

        [Header("Eventos")]
        public UnityEvent<float> onCompleted;        // crédito ganho (s), 0 se passou da meta
        public UnityEvent onCompletedWithinGoal;

        private void Start()
        {
            if (beginOnStart) BeginPuzzle();
        }

        public void BeginPuzzle()
        {
            if (GameClock.Instance == null) { Debug.LogWarning("[PuzzleTimer] Nenhum GameClock na cena."); return; }
            GameClock.Instance.BeginPuzzle(floor);
        }

        public void Complete()
        {
            var clock = GameClock.Instance;
            if (clock == null) return;
            float gained = clock.CompletePuzzle(floor);
            onCompleted?.Invoke(gained);
            if (clock.WasSolvedWithinGoal(floor)) onCompletedWithinGoal?.Invoke();
            if (floor == 4) clock.FinishSuccess();
        }

        /// <summary>Segundos restantes da meta (para mostrar num HUD do puzzle, se quiser).</summary>
        public float Remaining
        {
            get
            {
                var c = GameClock.Instance;
                return (c != null && c.ActiveFloor == floor) ? c.ActivePuzzleRemaining : 0f;
            }
        }
    }
}
