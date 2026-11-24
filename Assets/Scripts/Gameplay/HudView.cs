using Misterio.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Misterio.Gameplay
{
    /// <summary>
    /// Shared HUD for the playable floors: the night clock, the hourglass credit, the floor
    /// objective and a piece counter. Built from code so every floor looks identical.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] Font font;
        [SerializeField] Color textColor = new Color(0.93f, 0.95f, 1f, 1f);
        [SerializeField] Color dimColor = new Color(0.62f, 0.70f, 0.85f, 1f);

        Text clockText;
        Text creditText;
        Text objectiveText;
        Text counterText;
        Text promptText;
        Text hintText;
        Canvas canvas;
        float hintTimer;

        public static HudView Create(Font font, string objective)
        {
            var host = new GameObject("HUD");
            var view = host.AddComponent<HudView>();
            view.font = font;
            view.Build(objective);
            return view;
        }

        void Build(string objective)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            // Soft panels behind the text keep it readable over any background, the way the
            // cutscene subtitles already do. They are created first so they stay behind.
            MakePanel(new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(1000f, 118f), new Color(0.008f, 0.014f, 0.032f, 0.52f));
            MakePanel(new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(392f, 118f), new Color(0.008f, 0.014f, 0.032f, 0.52f));

            clockText = MakeText("Clock", new Vector2(1f, 1f), new Vector2(-40f, -36f),
                                 new Vector2(320f, 56f), 40, TextAnchor.UpperRight, textColor);
            creditText = MakeText("Credit", new Vector2(1f, 1f), new Vector2(-40f, -92f),
                                  new Vector2(320f, 44f), 28, TextAnchor.UpperRight, new Color(0.95f, 0.82f, 0.45f));
            objectiveText = MakeText("Objective", new Vector2(0f, 1f), new Vector2(40f, -36f),
                                     new Vector2(900f, 56f), 30, TextAnchor.UpperLeft, textColor);
            counterText = MakeText("Counter", new Vector2(0f, 1f), new Vector2(40f, -92f),
                                   new Vector2(600f, 44f), 26, TextAnchor.UpperLeft, dimColor);
            promptText = MakeText("Prompt", new Vector2(0.5f, 0f), new Vector2(0f, 150f),
                                  new Vector2(900f, 48f), 30, TextAnchor.LowerCenter, textColor);
            hintText = MakeText("Hint", new Vector2(0.5f, 0f), new Vector2(0f, 96f),
                                new Vector2(1200f, 44f), 26, TextAnchor.LowerCenter, new Color(0.85f, 0.88f, 0.95f, 0.9f));

            objectiveText.text = objective;
            creditText.text = string.Empty;
            counterText.text = string.Empty;
            promptText.text = string.Empty;
            hintText.text = string.Empty;
        }

        /// <summary>A flat translucent panel used to keep HUD text readable.</summary>
        void MakePanel(Vector2 anchor, Vector2 offset, Vector2 size, Color color)
        {
            var host = new GameObject("Painel", typeof(RectTransform));
            host.transform.SetParent(transform, false);
            var image = host.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
        }

        Text MakeText(string name, Vector2 anchor, Vector2 offset, Vector2 size, int fontSize,
                      TextAnchor alignment, Color color)
        {
            var host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(transform, false);
            var text = host.AddComponent<Text>();
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var rect = text.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
            return text;
        }

        void Update()
        {
            var session = GameSession.Instance;
            if (session == null) return;

            clockText.text = session.ClockText();

            if (session.CreditSeconds > 0.05f)
            {
                int seconds = Mathf.CeilToInt(session.CreditSeconds);
                creditText.text = "⌛ " + seconds + "s";
            }
            else
            {
                creditText.text = string.Empty;
            }

            if (hintTimer > 0f)
            {
                hintTimer -= Time.deltaTime;
                if (hintTimer <= 0f) hintText.text = string.Empty;
            }
        }

        /// <summary>Current hint text; used by the automated playthrough to record what was shown.</summary>
        public string VisibleHint => hintText != null ? hintText.text : string.Empty;

        public void SetObjective(string text)
        {
            if (objectiveText != null) objectiveText.text = text;
        }

        public void SetCounter(string text)
        {
            if (counterText != null) counterText.text = text;
        }

        public void SetPrompt(string text)
        {
            if (promptText != null) promptText.text = string.IsNullOrEmpty(text) ? string.Empty : "[E] " + text;
        }

        /// <summary>Shows a short contextual hint for a few seconds; never reveals the solution.</summary>
        public void ShowHint(string text, float duration = 6f)
        {
            if (hintText == null) return;
            hintText.text = text;
            hintTimer = duration;
        }

        public void SetVisible(bool visible)
        {
            if (canvas != null) canvas.enabled = visible;
        }
    }
}
