using EspacoCultural.Tempo;

namespace Misterio.Core
{
    /// <summary>PONTE: o pouco de estado da partida que as cutscenes CC09/CC10 consultam.</summary>
    public sealed class GameSession
    {
        private static GameSession instance;
        public static GameSession Instance => instance ?? (instance = new GameSession());

        /// <summary>Andar em que o relógio chegou às 06:00 (0 = nenhum). Escolhe a variação da CC09.</summary>
        public int TimedOutFloor { get; set; }

        /// <summary>Recomeça a noite do zero (relógio volta para 02:00, parado).</summary>
        public void ResetRun()
        {
            TimedOutFloor = 0;
            if (GameClock.Instance != null) GameClock.Instance.ResetClock();
        }
    }
}
