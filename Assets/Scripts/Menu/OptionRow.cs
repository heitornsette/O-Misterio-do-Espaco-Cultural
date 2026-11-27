using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EspacoCultural.Menu
{
    /// <summary>
    /// Linha ajustável do painel de Configurações, em estilo pixel:
    /// - Nivel: barra de segmentos (0–10), ←/→ ajusta.
    /// - Alternar: SIM / NÃO, ←/→ ou Enter alterna.
    /// - Lista: "&lt; valor &gt;", ←/→ ou Enter troca.
    /// Com mouse: clicar nas setas ou na barra; passar o mouse por cima seleciona a linha.
    /// </summary>
    public class OptionRow : Selectable, ISubmitHandler, IPointerClickHandler
    {
        public enum Kind { Nivel, Alternar, Lista }

        [Header("Tipo")]
        public Kind kind = Kind.Nivel;
        public int maxLevel = 10;
        public string[] choices = { "NÃO", "SIM" };

        [Header("Visual")]
        public Text label;
        public Text valueText;          // usado em Alternar e Lista
        public Graphic cursor;
        public Image[] segments;        // usado em Nivel
        public Button leftArrow;
        public Button rightArrow;
        public Color normalColor = new Color32(0xEC, 0xE8, 0xDE, 0xFF);
        public Color selectedColor = new Color32(0x8E, 0xC1, 0xDA, 0xFF);
        public Color segmentOn = new Color32(0x8E, 0xC1, 0xDA, 0xFF);
        public Color segmentOff = new Color32(0x33, 0x4A, 0x63, 0xFF);

        public UnityEvent<int> onValueChanged = new UnityEvent<int>();

        public int Value { get; private set; }
        private bool isSelected;

        protected override void Awake()
        {
            base.Awake();
            transition = Transition.None;
            if (Application.isPlaying && leftArrow != null) leftArrow.onClick.AddListener(() => { Select(); Step(-1); });
            if (Application.isPlaying && rightArrow != null) rightArrow.onClick.AddListener(() => { Select(); Step(+1); });
            Refresh();
        }

        public void SetValue(int v, bool notify)
        {
            Value = Clamp(v);
            Refresh();
            if (notify) onValueChanged.Invoke(Value);
        }

        public void SetChoices(string[] newChoices)
        {
            choices = newChoices;
            Value = Clamp(Value);
            Refresh();
        }

        public void Step(int dir)
        {
            int count = Count;
            int next = Value + dir;
            if (kind == Kind.Nivel) next = Mathf.Clamp(next, 0, maxLevel);
            else next = (next % count + count) % count;   // Alternar e Lista dão a volta
            if (next == Value) return;
            Value = next;
            Refresh();
            onValueChanged.Invoke(Value);
            EspacoCultural.Audio.GameAudio.Sfx("sfx_ajuste");   // depois do Invoke: já toca no volume novo
        }

        // ---------- Navegação ----------

        public override void OnMove(AxisEventData eventData)
        {
            if (eventData.moveDir == MoveDirection.Left) { Step(-1); eventData.Use(); return; }
            if (eventData.moveDir == MoveDirection.Right) { Step(+1); eventData.Use(); return; }
            base.OnMove(eventData);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (kind != Kind.Nivel) Step(+1);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (kind == Kind.Nivel && segments != null && segments.Length > 0)
            {
                // Clique na barra: escolhe o segmento clicado (clicar no primeiro aceso zera).
                for (int i = 0; i < segments.Length; i++)
                {
                    var rt = segments[i].rectTransform;
                    if (RectTransformUtility.RectangleContainsScreenPoint(rt, eventData.position, eventData.pressEventCamera))
                    {
                        int target = (i + 1 == Value) ? i : i + 1;
                        SetValue(target, true);
                        EspacoCultural.Audio.GameAudio.Sfx("sfx_ajuste");
                        return;
                    }
                }
            }
            else if (kind != Kind.Nivel && eventData.pointerPress == gameObject)
            {
                Step(+1);
            }
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            if (IsInteractable() && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            isSelected = true;
            MenuController.NotifySelected(gameObject);
            EspacoCultural.Audio.GameAudio.Sfx("sfx_menu_mover", 0.6f);
            Refresh();
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            isSelected = false;
            Refresh();
        }

        // ---------- Visual ----------

        private int Count => kind == Kind.Nivel ? maxLevel + 1 : Mathf.Max(1, choices != null ? choices.Length : 1);
        private int Clamp(int v) => Mathf.Clamp(v, 0, Count - 1);

        private void Refresh()
        {
            var c = isSelected ? selectedColor : normalColor;
            if (label != null) label.color = c;
            if (cursor != null) cursor.enabled = isSelected;

            if (kind == Kind.Nivel)
            {
                if (segments != null)
                    for (int i = 0; i < segments.Length; i++)
                        if (segments[i] != null) segments[i].color = i < Value ? segmentOn : segmentOff;
                if (valueText != null) valueText.text = "";
            }
            else if (valueText != null && choices != null && choices.Length > 0)
            {
                valueText.text = choices[Clamp(Value)];
                valueText.color = c;
            }

            SetArrowColor(leftArrow, c);
            SetArrowColor(rightArrow, c);
        }

        private static void SetArrowColor(Button b, Color c)
        {
            if (b == null) return;
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.color = c;
        }
    }
}
