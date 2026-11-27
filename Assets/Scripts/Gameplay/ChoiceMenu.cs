using System;
using EspacoCultural.Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EspacoCultural.Gameplay
{
    /// <summary>
    /// Janela de escolha no estilo das placas do museu: título, um texto de apoio e uma lista de opções.
    /// SETAS escolhem, E/ENTER confirmam, ESC cancela (devolve -1).
    /// Usada no fichário e nas estantes da biblioteca, no quadro de luz do ginásio etc.
    /// </summary>
    public class ChoiceMenu : MonoBehaviour
    {
        private RectTransform root;
        private Text title, header, footer;
        private Text[] lines;
        private string[] options;
        private int index;
        private Action<int> picked;
        private float openedAt;
        private const int MaxLines = 8;

        public bool Open => root != null && root.gameObject.activeSelf;
        public int ClosedFrame { get; private set; } = -1;

        public void Build(RectTransform screen, Font titleFont, Font body)
        {
            root = Px.Panel("Escolha", screen, 110, 60, 420, 230);
            title = Px.Label("Titulo", root, titleFont, 8, Px.Warm, 12, 12, 396, 14);
            header = Px.Label("Texto", root, body, 8, Color.white, 12, 30, 396, 60);
            lines = new Text[MaxLines];
            for (int i = 0; i < MaxLines; i++)
                lines[i] = Px.Label("Opcao" + i, root, body, 8, Color.white, 20, 96 + i * 14, 390, 12);
            footer = Px.Label("Rodape", root, body, 8, Px.LightBlue, 12, 212, 396, 12, TextAnchor.UpperRight);
            footer.text = "SETAS escolher   E confirmar   ESC voltar";
            root.gameObject.SetActive(false);
        }

        public void Show(string titleText, string headerText, string[] opts, Action<int> onPick, int start = 0)
        {
            options = opts;
            picked = onPick;
            index = Mathf.Clamp(start, 0, opts.Length - 1);
            title.text = titleText;
            header.text = headerText;
            openedAt = Time.unscaledTime;
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            Draw();
        }

        private void Draw()
        {
            for (int i = 0; i < MaxLines; i++)
            {
                bool on = options != null && i < options.Length;
                lines[i].gameObject.SetActive(on);
                if (!on) continue;
                bool sel = i == index;
                lines[i].text = (sel ? "▶ " : "   ") + options[i];
                lines[i].color = sel ? Px.LightBlue : Color.white;
            }
        }

        private void Close(int result)
        {
            root.gameObject.SetActive(false);
            ClosedFrame = Time.frameCount;
            var cb = picked; picked = null;
            cb?.Invoke(result);
        }

        private void Update()
        {
            if (!Open || Time.unscaledTime - openedAt < 0.15f) return;
            var k = Keyboard.current;
            if (k == null) return;
            if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame)
            { index = (index + options.Length - 1) % options.Length; GameAudio.Sfx("sfx_menu_mover", 0.6f); Draw(); }
            else if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame)
            { index = (index + 1) % options.Length; GameAudio.Sfx("sfx_menu_mover", 0.6f); Draw(); }
            else if (k.eKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)
            { GameAudio.Sfx("sfx_menu_confirmar", 0.7f); Close(index); }
            else if (k.escapeKey.wasPressedThisFrame) Close(-1);
        }
    }
}
