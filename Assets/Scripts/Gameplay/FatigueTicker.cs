using EspacoCultural.Jogo;
using EspacoCultural.Tempo;
using UnityEngine;

namespace EspacoCultural.Gameplay
{
    /// <summary>
    /// Cansaço do guarda: sobe enquanto o relógio corre (dentro do museu). Cheio em ~7 min de jogo ativo.
    /// Acima de 70% o guarda fica mais lento (GameState.SlowFactor). O café da máquina do CC zera.
    /// </summary>
    public class FatigueTicker : MonoBehaviour
    {
        public const float SecondsToFull = 420f;
        private bool warned;
        private FloorHUD hud;

        private void Start() => hud = GetComponentInChildren<FloorHUD>() ?? FindFirstObjectByType<FloorHUD>();

        private void Update()
        {
            var c = GameClock.Instance;
            if (c == null || c.State != GameClock.ClockState.Running) return;
            GameState.AddFatigue(Time.deltaTime / SecondsToFull);
            if (GameState.Tired && !warned && !GameState.Flag("aviso_cansaco"))
            {
                warned = true;
                GameState.Set("aviso_cansaco");
                if (hud != null) hud.Toast("O guarda está cansado e anda mais devagar.\nUm café da máquina do CC resolveria...", 4f);
            }
        }
    }
}
