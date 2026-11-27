using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace EspacoCultural.Tempo
{
    /// <summary>
    /// Decide o que acontece quando o relógio chega às 06:00 sem o circuito final resolvido.
    /// Narrativa: CC09 e CC10 são os finais de tempo; se acabar durante o Andar 4, há uma variação.
    /// Preencha os nomes das cenas ou use os UnityEvents para disparar a Timeline correspondente.
    /// </summary>
    public class TimeUpHandler : MonoBehaviour
    {
        [Header("Cenas (opcional — deixe vazio para usar só os eventos)")]
        public string timeUpSceneDefault = "";   // CC09
        public string timeUpSceneFloor4 = "";    // CC10 (variação do Andar 4)

        [Header("Eventos")]
        public UnityEvent onTimeUpDefault;
        public UnityEvent onTimeUpFloor4;

        private GameClock bound;

        private void Update()
        {
            if (bound == null && GameClock.Instance != null)
            {
                bound = GameClock.Instance;
                bound.onTimeUp.AddListener(HandleTimeUp);
            }
        }

        private void OnDestroy()
        {
            if (bound != null) bound.onTimeUp.RemoveListener(HandleTimeUp);
        }

        private void HandleTimeUp(int floor)
        {
            Debug.Log($"[Relógio] 06:00 — tempo esgotado (andar {floor}).");
            if (floor == 4)
            {
                onTimeUpFloor4?.Invoke();
                if (!string.IsNullOrEmpty(timeUpSceneFloor4)) SceneManager.LoadScene(timeUpSceneFloor4);
            }
            else
            {
                onTimeUpDefault?.Invoke();
                if (!string.IsNullOrEmpty(timeUpSceneDefault)) SceneManager.LoadScene(timeUpSceneDefault);
            }
        }
    }
}
