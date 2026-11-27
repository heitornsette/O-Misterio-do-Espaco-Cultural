using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EspacoCultural.Menu
{
    /// <summary>
    /// Visual de um item do menu (junto de um Button): texto branco-gelo e, quando selecionado,
    /// azul-claro com o cursor triangular. Passar o mouse por cima também seleciona,
    /// para mouse e teclado/controle mostrarem o mesmo destaque.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MenuItem : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
    {
        public Text label;
        public Graphic cursor;
        public Color normalColor = new Color32(0xEC, 0xE8, 0xDE, 0xFF);
        public Color selectedColor = new Color32(0x8E, 0xC1, 0xDA, 0xFF);
        [Tooltip("Quantos pixels o texto anda para a direita quando selecionado.")]
        public int selectedNudge = 2;

        private Vector2 labelBasePos;
        private bool hasBase;

        private void Awake()
        {
            if (label != null) { labelBasePos = label.rectTransform.anchoredPosition; hasBase = true; }
            SetSelected(false);
        }

        private void OnEnable()
        {
            bool sel = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
            SetSelected(sel);
        }

        public void OnSelect(BaseEventData eventData)
        {
            SetSelected(true);
            MenuController.NotifySelected(gameObject);
            EspacoCultural.Audio.GameAudio.Sfx("sfx_menu_mover", 0.6f);
        }

        public void OnDeselect(BaseEventData eventData) => SetSelected(false);

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (EventSystem.current != null && GetComponent<Button>().interactable)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }

        private void SetSelected(bool on)
        {
            if (label != null)
            {
                label.color = on ? selectedColor : normalColor;
                if (hasBase) label.rectTransform.anchoredPosition = labelBasePos + (on ? new Vector2(selectedNudge, 0) : Vector2.zero);
            }
            if (cursor != null) cursor.enabled = on;
        }
    }
}
