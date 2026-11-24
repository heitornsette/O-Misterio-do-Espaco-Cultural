using Misterio.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Misterio.Gameplay
{
    /// <summary>
    /// Pause on ESC. It freezes the night clock, dims the scene and offers continue or restart.
    /// Built from code so every floor gets the same overlay without per-scene wiring.
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] Font font;

        Canvas canvas;
        Text titleText;
        bool paused;

        public static PauseMenu Create(Font font)
        {
            var host = new GameObject("Pause");
            var menu = host.AddComponent<PauseMenu>();
            menu.font = font;
            menu.Build();
            return menu;
        }

        void Build()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            var backdrop = MakeImage("Fundo", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                                     new Color(0.008f, 0.012f, 0.028f, 0.78f));
            backdrop.raycastTarget = true;

            var panel = MakeImage("Painel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                  Vector2.zero, new Vector2(760f, 340f),
                                  new Color(0.03f, 0.05f, 0.10f, 0.96f));

            titleText = MakeText("Título", panel.transform, new Vector2(0.5f, 1f), new Vector2(0f, -46f),
                                 new Vector2(700f, 70f), 46, "NOITE PAUSADA");
            MakeText("Dica", panel.transform, new Vector2(0.5f, 1f), new Vector2(0f, -112f),
                     new Vector2(700f, 48f), 26, "ESC continua  ·  R recomeça a noite");

            MakeButton(panel.transform, "CONTINUAR", new Vector2(0f, -150f), Continue);
            MakeButton(panel.transform, "RECOMEÇAR", new Vector2(0f, -230f), Restart);

            canvas.enabled = false;
        }

        Image MakeImage(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin,
                        Vector2 offsetMax, Color color)
        {
            var host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(transform, false);
            var image = host.AddComponent<Image>();
            image.color = color;
            var rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return image;
        }

        Text MakeText(string name, Transform parent, Vector2 anchor, Vector2 offset, Vector2 size,
                      int fontSize, string content)
        {
            var host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(parent, false);
            var text = host.AddComponent<Text>();
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.text = content;
            text.color = new Color(0.93f, 0.95f, 1f, 1f);
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            var rect = text.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
            return text;
        }

        void MakeButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            var host = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            host.transform.SetParent(parent, false);
            var image = host.GetComponent<Image>();
            image.color = new Color(0.10f, 0.16f, 0.28f, 1f);
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(420f, 62f);

            var button = host.GetComponent<Button>();
            button.onClick.AddListener(action);
            var colors = button.colors;
            colors.highlightedColor = new Color(0.18f, 0.26f, 0.42f, 1f);
            colors.pressedColor = new Color(0.24f, 0.34f, 0.52f, 1f);
            button.colors = colors;

            var text = MakeText("Texto", host.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
                                new Vector2(400f, 56f), 30, label);
            text.alignment = TextAnchor.MiddleCenter;
        }

        void Update()
        {
            if (PlaytestInput.Active)
            {
                if (PlaytestInput.PausePressed) Toggle();
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame) Toggle();

            if (paused && keyboard.rKey.wasPressedThisFrame) Restart();
        }

        public void Toggle()
        {
            paused = !paused;
            canvas.enabled = paused;
            Time.timeScale = paused ? 0f : 1f;

            var session = GameSession.Instance != null ? GameSession.Instance : GameSession.EnsureExists();
            session.SetClockRunning(!paused);

            // The floor stops reading input while paused.
            foreach (var actor in Object.FindObjectsByType<GuardActor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                actor.CanMove = !paused;
                actor.CanInteract = !paused;
            }
        }

        void Continue()
        {
            if (paused) Toggle();
        }

        void OnDisable()
        {
            // A paused game must not survive the object: leaving timeScale at zero would freeze the
            // next scene as well.
            if (!paused) return;
            paused = false;
            Time.timeScale = 1f;
            var session = GameSession.Instance;
            if (session != null) session.SetClockRunning(false);
        }

        void Restart()
        {
            Time.timeScale = 1f;
            GameSession.Instance?.ResetRun();
            GameFlow.EnsureExists().Load(GameFlow.Exterior);
        }
    }
}
