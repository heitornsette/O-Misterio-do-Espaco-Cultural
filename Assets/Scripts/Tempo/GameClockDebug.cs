using UnityEngine;
using UnityEngine.InputSystem;

namespace EspacoCultural.Tempo
{
    /// <summary>
    /// Atalhos de teste do relógio (só no Editor / Development Build).
    ///   Enter : inicia o relógio (como se a CC01 tivesse acabado)
    ///   1..4  : começa o puzzle do andar
    ///   C     : conclui o puzzle ativo
    ///   F     : liga/desliga "cutscene" (congela)
    ///   T     : alterna velocidade 1x / 10x / 60x
    ///   R     : reinicia a partida
    /// Mostra um painel com o estado centralizado na parte de baixo.
    /// </summary>
    public class GameClockDebug : MonoBehaviour
    {
        public bool showOverlay = true;
        private bool fakeCutscene;
        private readonly float[] speeds = { 1f, 10f, 60f };
        private int speedIndex;

        private void Update()
        {
#if UNITY_EDITOR || DEBUG
            var c = GameClock.Instance;
            var k = Keyboard.current;
            if (c == null || k == null) return;

            if (k.enterKey.wasPressedThisFrame) c.StartClock();
            if (k.digit1Key.wasPressedThisFrame) c.BeginPuzzle(1);
            if (k.digit2Key.wasPressedThisFrame) c.BeginPuzzle(2);
            if (k.digit3Key.wasPressedThisFrame) c.BeginPuzzle(3);
            if (k.digit4Key.wasPressedThisFrame) c.BeginPuzzle(4);
            if (k.cKey.wasPressedThisFrame && c.ActiveFloor != 0)
            {
                int f = c.ActiveFloor;
                float g = c.CompletePuzzle(f);
                Debug.Log($"[Relógio] Andar {f} resolvido. Crédito ganho: {g:0.0}s");
            }
            if (k.fKey.wasPressedThisFrame)
            {
                fakeCutscene = !fakeCutscene;
                if (fakeCutscene) c.Freeze(); else c.Unfreeze();
            }
            if (k.tKey.wasPressedThisFrame)
            {
                speedIndex = (speedIndex + 1) % speeds.Length;
                c.debugSpeed = speeds[speedIndex];
            }
            if (k.rKey.wasPressedThisFrame) { fakeCutscene = false; c.ResetClock(); }
#endif
        }

#if UNITY_EDITOR || DEBUG
        private GUIStyle style;
        private void OnGUI()
        {
            var c = GameClock.Instance;
            if (!showOverlay || c == null) return;
            style ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
            string txt =
                $"Estado: {c.State}   Velocidade: {c.debugSpeed}x\n" +
                $"Horário: {c.CurrentTimeText}   Ativo: {c.ActivePlaySeconds:0.0}s\n" +
                $"Puzzle: {(c.ActiveFloor == 0 ? "-" : "Andar " + c.ActiveFloor)}   Meta restante: {c.ActivePuzzleRemaining:0.0}s\n" +
                $"Crédito: {c.CreditSeconds:0.0}s   Total concedido: {c.TotalCreditGranted:0.0}/120s\n" +
                "[Enter] iniciar [1-4] puzzle [C] concluir [F] cutscene [T] velocidade [R] reiniciar";
            const float w = 620f, h = 110f;
            GUI.Box(new Rect((Screen.width - w) / 2f, Screen.height - h - 10f, w, h), txt, style);
        }
#endif
    }
}
