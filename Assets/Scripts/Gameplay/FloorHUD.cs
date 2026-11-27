using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EspacoCultural.Gameplay
{
    /// <summary>
    /// HUD dos andares: objetivo (canto superior esquerdo), barra de dicas (embaixo),
    /// avisos rápidos (toast), janela de leitura (popup) e o fade preto das transições.
    /// O relógio fica no HUD_Relogio separado (centralizado no topo).
    /// </summary>
    public class FloorHUD : MonoBehaviour
    {
        private Text objective;
        private Text hint;
        private RectTransform toastRoot;
        private Text toastText;
        private float toastUntil;
        private RectTransform popupRoot;
        private Text popupTitle, popupBody, popupFooter;
        private Action popupClosed;
        private float popupOpenedAt;
        private Image fade;

        public bool PopupOpen => popupRoot != null && popupRoot.gameObject.activeSelf;
        public bool Fading { get; private set; }
        /// <summary>Quadro em que a janela foi fechada (evita que o mesmo E feche e reabra).</summary>
        public int ClosedFrame { get; private set; } = -1;

        public void Build(RectTransform screen, Font title, Font body)
        {
            // Objetivo
            var obj = Px.Rect("Objetivo", screen);
            Px.Place(obj, 6, 6, 230, 40);
            var bg = obj.gameObject.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.07f, 0.16f, 0.78f);
            bg.raycastTarget = false;
            objective = Px.Label("Texto", obj, body, 8, Color.white, 5, 4, 220, 34);

            // Dicas de controle
            var hb = Px.Box("Dicas", screen, new Color(0.04f, 0.07f, 0.16f, 0.78f), 0, 346, 640, 14);
            hint = Px.Label("Texto", hb.rectTransform, body, 8, Px.LightBlue, 4, 2, 632, 12, TextAnchor.UpperCenter);
            hint.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Toast
            toastRoot = Px.Panel("Aviso", screen, 170, 58, 300, 30);
            toastText = Px.Label("Texto", toastRoot, body, 8, Color.white, 8, 6, 284, 20, TextAnchor.MiddleCenter);
            toastRoot.gameObject.SetActive(false);

            // Popup
            popupRoot = Px.Panel("Janela", screen, 130, 80, 380, 190);
            popupTitle = Px.Label("Titulo", popupRoot, title, 8, Px.Warm, 12, 12, 356, 16);
            popupBody = Px.Label("Corpo", popupRoot, body, 8, Color.white, 12, 34, 356, 130);
            popupFooter = Px.Label("Rodape", popupRoot, body, 8, Px.LightBlue, 12, 170, 356, 12, TextAnchor.UpperRight);
            popupRoot.gameObject.SetActive(false);

            // Fade
            fade = Px.Box("Fade", screen, Color.black, 0, 0, 640, 360);
            Px.Stretch(fade.rectTransform);
        }

        public void SetObjective(string text) { if (objective != null) objective.text = text; }
        public void SetHint(string text) { if (hint != null && hint.text != text) hint.text = text; }

        public void Toast(string text, float seconds = 2.5f)
        {
            toastText.text = text;
            toastRoot.gameObject.SetActive(true);
            toastUntil = Time.unscaledTime + seconds;
        }

        public void HideToast() => toastRoot.gameObject.SetActive(false);

        public void Popup(string title, string body, Action closed = null, string footer = "[E] continuar")
        {
            popupTitle.text = title;
            popupBody.text = body;
            popupFooter.text = footer;
            popupClosed = closed;
            popupOpenedAt = Time.unscaledTime;
            popupRoot.gameObject.SetActive(true);
            popupRoot.SetAsLastSibling();
            fade.rectTransform.SetAsLastSibling();
        }

        public void ClosePopup()
        {
            if (!PopupOpen) return;
            popupRoot.gameObject.SetActive(false);
            ClosedFrame = Time.frameCount;
            var cb = popupClosed; popupClosed = null;
            cb?.Invoke();
        }

        public IEnumerator FadeTo(float target, float duration)
        {
            Fading = true;
            fade.raycastTarget = false;
            float start = fade.color.a, t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var c = fade.color; c.a = Mathf.Lerp(start, target, t / duration); fade.color = c;
                yield return null;
            }
            var e = fade.color; e.a = target; fade.color = e;
            Fading = false;
        }

        private void Update()
        {
            if (toastRoot != null && toastRoot.gameObject.activeSelf && Time.unscaledTime > toastUntil)
                toastRoot.gameObject.SetActive(false);

            if (PopupOpen && Time.unscaledTime - popupOpenedAt > 0.2f)
            {
                var k = Keyboard.current;
                if (k != null && (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame ||
                                  k.enterKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame))
                    ClosePopup();
            }
        }
    }
}
