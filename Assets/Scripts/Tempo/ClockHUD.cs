using UnityEngine;
using UnityEngine.UI;

namespace EspacoCultural.Tempo
{
    /// <summary>
    /// HUD do relógio: horário "HH:MM" e, quando houver crédito, a ampulheta com o saldo.
    /// Pisca os dois-pontos enquanto o tempo corre; fica fixo quando congelado ou com crédito.
    /// Muda de cor nos últimos 30 minutos ficcionais (05:30+).
    /// </summary>
    public class ClockHUD : MonoBehaviour
    {
        public Text timeText;
        public GameObject creditGroup;     // ícone de ampulheta + texto (some quando não há crédito)
        public Text creditText;

        [Header("Cores (paleta do projeto)")]
        public Color normalColor = new Color(0.90f, 0.95f, 1f);
        public Color urgentColor = new Color(1f, 0.55f, 0.35f);
        public Color creditColor = new Color(0.55f, 0.85f, 1f);
        [Tooltip("A partir deste horário (minutos) o relógio fica na cor de urgência. 330 = 05:30")]
        public float urgentFromMinutes = 330f;

        public bool blinkColon = true;

        private GameClock clock;

        private void OnEnable() => TryBind();

        private void OnDisable()
        {
            if (clock == null) return;
            clock.TimeChanged -= OnTime;
            clock.CreditChanged -= OnCredit;
            clock = null;
        }

        private void TryBind()
        {
            if (clock != null || GameClock.Instance == null) return;
            clock = GameClock.Instance;
            clock.TimeChanged += OnTime;
            clock.CreditChanged += OnCredit;
            OnTime(clock.CurrentMinutes);
            OnCredit(clock.CreditSeconds);
        }

        private void Update()
        {
            if (clock == null) { TryBind(); return; }
            if (timeText == null) return;

            // Dois-pontos piscando só quando o relógio está avançando de fato.
            bool advancing = clock.State == GameClock.ClockState.Running && !clock.IsCrediting;
            string t = clock.CurrentTimeText;
            if (blinkColon && advancing && (Time.unscaledTime % 1f) > 0.5f) t = t.Replace(':', ' ');
            timeText.text = t;
        }

        private void OnTime(float minutes)
        {
            if (timeText != null)
                timeText.color = minutes >= urgentFromMinutes ? urgentColor : normalColor;
        }

        private void OnCredit(float seconds)
        {
            bool show = seconds > 0.01f;
            if (creditGroup != null && creditGroup.activeSelf != show) creditGroup.SetActive(show);
            if (creditText != null && show)
            {
                int s = Mathf.CeilToInt(seconds);
                creditText.text = $"{s / 60}:{s % 60:00}";
                creditText.color = creditColor;
            }
        }
    }
}
