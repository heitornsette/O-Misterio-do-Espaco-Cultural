using System;
using System.Collections.Generic;
using EspacoCultural.Audio;
using EspacoCultural.Menu;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EspacoCultural.Gameplay
{
    /// <summary>
    /// Pausa dos andares (Esc) com as mesmas configurações do menu inicial:
    /// CONTINUAR, MÚSICA, EFEITOS, TELA CHEIA, RESOLUÇÃO e MENU INICIAL.
    /// Setas/WASD escolhem e ajustam, E/Espaço/Enter confirmam, Esc fecha.
    /// Os valores são os do GameSettings (PlayerPrefs), então valem para o jogo todo.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        private enum Row { Continuar, Musica, Efeitos, TelaCheia, Resolucao, Menu }

        private RectTransform root;
        private readonly List<Text> labels = new List<Text>();
        private readonly List<Text> values = new List<Text>();
        private readonly List<Image[]> bars = new List<Image[]>();
        private Text cursor;
        private int index;
        private float openedAt;
        private Action onResume, onMenu;

        private static readonly Color Normal = new Color32(0xEC, 0xE8, 0xDE, 0xFF);
        private static readonly Color SegOff = new Color32(0x33, 0x4A, 0x63, 0xFF);
        private const int RowH = 18, Top = 34, W = 280, H = 168;

        public bool Open => root != null && root.gameObject.activeSelf;

        public void Build(RectTransform screen, Font title, Font body)
        {
            root = Px.Panel("Pausa", screen, (Px.W - W) / 2f, 70, W, H);
            Px.Label("Titulo", root, title, 8, Px.Warm, 0, 12, W, 12, TextAnchor.UpperCenter).text = "PAUSA";

            string[] names = { "CONTINUAR", "MÚSICA", "EFEITOS", "TELA CHEIA", "RESOLUÇÃO", "MENU INICIAL" };
            for (int i = 0; i < names.Length; i++)
            {
                float y = Top + i * RowH;
                labels.Add(Px.Label("Item", root, body, 8, Normal, 26, y, 120, 12));
                labels[i].text = names[i];
                values.Add(Px.Label("Valor", root, body, 8, Normal, 140, y, 124, 12, TextAnchor.UpperRight));
                Image[] seg = null;
                if (i == (int)Row.Musica || i == (int)Row.Efeitos)
                {
                    seg = new Image[GameSettings.MaxLevel];
                    for (int k = 0; k < seg.Length; k++)
                        seg[k] = Px.Box("Seg", root, SegOff, 164 + k * 10, y + 1, 8, 8);
                }
                bars.Add(seg);
            }
            cursor = Px.Label("Cursor", root, body, 8, Px.LightBlue, 14, Top, 10, 12);
            cursor.text = ">";

            var note = Px.Label("Nota", root, body, 8, new Color(1, 1, 1, 0.55f), 0, H - 22, W, 12, TextAnchor.UpperCenter);
#if UNITY_EDITOR
            note.text = "No Editor: tela cheia maximiza a aba Game";
#else
            note.text = "SETAS ajustar   E confirmar   ESC voltar";
#endif
            root.gameObject.SetActive(false);
        }

        public void Show(Action resume, Action goToMenu)
        {
            onResume = resume;
            onMenu = goToMenu;
            index = 0;
            openedAt = Time.unscaledTime;
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            Refresh();
        }

        public void Hide()
        {
            if (!Open) return;
            root.gameObject.SetActive(false);
            var cb = onResume; onResume = null;
            cb?.Invoke();
        }

        private void Update()
        {
            if (!Open || Time.unscaledTime - openedAt < 0.15f) return;
            var k = Keyboard.current;
            if (k == null) return;

            if (k.escapeKey.wasPressedThisFrame) { GameAudio.Sfx("sfx_menu_mover", 0.6f); Hide(); return; }
            if (k.mKey.wasPressedThisFrame) { GoMenu(); return; }
            if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) Move(-1);
            if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) Move(1);
            if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) Adjust(-1);
            if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) Adjust(1);
            if (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame) Confirm();
        }

        private void Move(int d)
        {
            index = (index + d + labels.Count) % labels.Count;
            GameAudio.Sfx("sfx_menu_mover", 0.6f);
            Refresh();
        }

        private void Adjust(int d)
        {
            switch ((Row)index)
            {
                case Row.Musica: GameSettings.MusicLevel = Mathf.Clamp(GameSettings.MusicLevel + d, 0, GameSettings.MaxLevel); break;
                case Row.Efeitos: GameSettings.SfxLevel = Mathf.Clamp(GameSettings.SfxLevel + d, 0, GameSettings.MaxLevel); break;
                case Row.TelaCheia: GameSettings.Fullscreen = !GameSettings.Fullscreen; break;
                case Row.Resolucao:
                    int n = GameSettings.AvailableResolutions().Count;
                    GameSettings.ResolutionIndex = (GameSettings.ResolutionIndex + d + n) % n;
                    break;
                default: return;
            }
            GameAudio.Sfx("sfx_ajuste");
            Refresh();
        }

        private void Confirm()
        {
            switch ((Row)index)
            {
                case Row.Continuar: GameAudio.Sfx("sfx_menu_confirmar"); Hide(); break;
                case Row.Menu: GoMenu(); break;
                case Row.TelaCheia:
                case Row.Resolucao: Adjust(1); break;
            }
        }

        private void GoMenu()
        {
            GameAudio.Sfx("sfx_menu_confirmar");
            root.gameObject.SetActive(false);
            onResume = null;
            var cb = onMenu; onMenu = null;
            cb?.Invoke();
        }

        private void Refresh()
        {
            for (int i = 0; i < labels.Count; i++)
            {
                bool sel = i == index;
                var c = sel ? Px.LightBlue : Normal;
                labels[i].color = c;
                values[i].color = c;
                if (bars[i] != null)
                {
                    int lvl = i == (int)Row.Musica ? GameSettings.MusicLevel : GameSettings.SfxLevel;
                    for (int k = 0; k < bars[i].Length; k++) bars[i][k].color = k < lvl ? Px.LightBlue : SegOff;
                }
            }
            values[(int)Row.TelaCheia].text = "< " + (GameSettings.Fullscreen ? "SIM" : "NÃO") + " >";
            var list = GameSettings.AvailableResolutions();
            var r = list[Mathf.Clamp(GameSettings.ResolutionIndex, 0, list.Count - 1)];
            values[(int)Row.Resolucao].text = "< " + GameSettings.ResolutionLabel(r) + " >";
            Px.Place(cursor.rectTransform, 14, Top + index * RowH, 10, 12);
        }
    }
}
