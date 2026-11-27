using System;
using System.Collections.Generic;
using UnityEngine;

namespace EspacoCultural.Menu
{
    /// <summary>
    /// Configurações do jogo salvas em PlayerPrefs (volume da música, volume dos efeitos,
    /// tela cheia e resolução). As resoluções são múltiplos inteiros de 640×360,
    /// para o pixel art nunca ficar borrado.
    /// </summary>
    public static class GameSettings
    {
        public const int MaxLevel = 10;

        private const string KeyMusic = "cfg_musica";
        private const string KeySfx = "cfg_efeitos";
        private const string KeyFullscreen = "cfg_tela_cheia";
        private const string KeyResolution = "cfg_resolucao";

        /// <summary>Disparado sempre que algum valor muda.</summary>
        public static event Action Changed;

        public static int MusicLevel
        {
            get => PlayerPrefs.GetInt(KeyMusic, 8);
            set { PlayerPrefs.SetInt(KeyMusic, Mathf.Clamp(value, 0, MaxLevel)); Save(); }
        }

        public static int SfxLevel
        {
            get => PlayerPrefs.GetInt(KeySfx, 8);
            set { PlayerPrefs.SetInt(KeySfx, Mathf.Clamp(value, 0, MaxLevel)); Save(); }
        }

        public static float MusicVolume => MusicLevel / (float)MaxLevel;
        public static float SfxVolume => SfxLevel / (float)MaxLevel;

        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;
            set { PlayerPrefs.SetInt(KeyFullscreen, value ? 1 : 0); Save(); ApplyDisplay(); }
        }

        public static int ResolutionIndex
        {
            get
            {
                var list = AvailableResolutions();
                int saved = PlayerPrefs.GetInt(KeyResolution, -1);
                if (saved >= 0 && saved < list.Count) return saved;
                return ClosestToCurrent(list);
            }
            set
            {
                var list = AvailableResolutions();
                PlayerPrefs.SetInt(KeyResolution, Mathf.Clamp(value, 0, list.Count - 1));
                Save();
                ApplyDisplay();
            }
        }

        /// <summary>640×360 multiplicado por 1, 2, 3, 4 e 6, até o tamanho do monitor.</summary>
        public static List<Vector2Int> AvailableResolutions()
        {
            int maxW = Display.main != null && Display.main.systemWidth > 0 ? Display.main.systemWidth : Screen.currentResolution.width;
            int maxH = Display.main != null && Display.main.systemHeight > 0 ? Display.main.systemHeight : Screen.currentResolution.height;
            var list = new List<Vector2Int>();
            foreach (int k in new[] { 1, 2, 3, 4, 6 })
            {
                var r = new Vector2Int(640 * k, 360 * k);
                if (r.x <= maxW && r.y <= maxH) list.Add(r);
            }
            if (list.Count == 0) list.Add(new Vector2Int(640, 360));
            return list;
        }

        public static string ResolutionLabel(Vector2Int r) => $"{r.x}x{r.y}";

        /// <summary>
        /// Aplica tela cheia e resolução no jogo exportado.
        /// No Editor o Unity ignora Screen.SetResolution; ali "tela cheia" maximiza a aba Game.
        /// </summary>
        public static void ApplyDisplay()
        {
#if UNITY_EDITOR
            MaximizeGameView(Fullscreen);
            return;
#else
            var list = AvailableResolutions();
            var r = list[Mathf.Clamp(ResolutionIndex, 0, list.Count - 1)];
            var mode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            Screen.SetResolution(r.x, r.y, mode);
#endif
        }

#if UNITY_EDITOR
        private static void MaximizeGameView(bool on)
        {
            if (!Application.isPlaying) return;
            try
            {
                var type = System.Type.GetType("UnityEditor.GameView,UnityEditor");
                if (type == null) return;
                var win = UnityEditor.EditorWindow.GetWindow(type, false, null, false);
                if (win != null && win.maximized != on) win.maximized = on;
            }
            catch (Exception e) { Debug.LogWarning("[Config] Não deu para maximizar a aba Game: " + e.Message); }
        }
#endif

        private static int ClosestToCurrent(List<Vector2Int> list)
        {
            int best = list.Count - 1;
            int bestDiff = int.MaxValue;
            for (int i = 0; i < list.Count; i++)
            {
                int diff = Mathf.Abs(list[i].x - Screen.width);
                if (diff < bestDiff) { bestDiff = diff; best = i; }
            }
            return best;
        }

        private static void Save()
        {
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
