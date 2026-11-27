using System.Collections.Generic;
using EspacoCultural.Jogo;
using UnityEngine;
using UnityEngine.UI;

namespace EspacoCultural.Gameplay
{
    /// <summary>
    /// Inventário (fileira de ícones abaixo do objetivo) e barra de cansaço (canto superior direito).
    /// I abre a lista de itens com as descrições (pela janela do FloorHUD).
    /// Usado nos andares e nas cenas do campus.
    /// </summary>
    public class InventoryHUD : MonoBehaviour
    {
        private RectTransform row;
        private readonly List<RawImage> slots = new List<RawImage>();
        private Image fatigueFill;
        private Text fatigueLabel;
        private Text cluesText;
        private const int Slot = 22;

        public void Build(RectTransform screen, Font body)
        {
            row = Px.Rect("Inventario", screen);
            Px.Place(row, 6, 50, 6 * (Slot + 3), Slot);

            var fb = Px.Rect("Cansaco", screen);
            Px.Place(fb, 548, 6, 86, 26);
            var bg = fb.gameObject.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.07f, 0.16f, 0.78f);
            bg.raycastTarget = false;
            fatigueLabel = Px.Label("Rotulo", fb, body, 8, Px.LightBlue, 5, 2, 80, 10);
            fatigueLabel.text = "CANSAÇO";
            Px.Box("Trilho", fb, new Color(0.08f, 0.1f, 0.2f, 1f), 5, 15, 76, 6);
            fatigueFill = Px.Box("Nivel", fb, Color.white, 5, 15, 0, 6);

            cluesText = Px.Label("Pistas", screen, body, 8, Px.Warm, 548, 34, 86, 10, TextAnchor.UpperRight);

            GameState.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy() => GameState.Changed -= Refresh;

        private void Refresh()
        {
            if (row == null) return;
            var ids = GameState.ItemOrder;
            while (slots.Count < ids.Count)
            {
                int i = slots.Count;
                var frame = Px.Box("Slot", row, new Color(0.04f, 0.07f, 0.16f, 0.85f), i * (Slot + 3), 0, Slot, Slot);
                var img = Px.Raw("Icone", frame.rectTransform, null, 3, 3, 16, 16);
                slots.Add(img);
            }
            for (int i = 0; i < slots.Count; i++)
            {
                bool on = i < ids.Count;
                slots[i].transform.parent.gameObject.SetActive(on);
                if (on) slots[i].texture = Items.Icon(ids[i]);
            }
            if (cluesText != null)
                cluesText.text = GameState.CluesFound > 0 ? $"Pistas da Moema {GameState.CluesFound}/{GameState.CluesTotal}" : "";
        }

        private void Update()
        {
            if (fatigueFill == null) return;
            float f = GameState.Fatigue;
            Px.Place(fatigueFill.rectTransform, 5, 15, Mathf.Round(76 * f), 6);
            fatigueFill.color = f < 0.4f ? new Color32(0x9c, 0xbc, 0x5a, 255)
                              : f < 0.7f ? new Color32(0xf4, 0xc9, 0x8a, 255)
                              : new Color32(0xe8, 0x9a, 0x6a, 255);
            fatigueLabel.text = GameState.Tired ? "CANSADO!" : "CANSAÇO";
        }

        /// <summary>Texto da lista de itens para a janela do inventário.</summary>
        public static string ListText()
        {
            if (GameState.ItemOrder.Count == 0) return "Nada por enquanto.";
            var sb = new System.Text.StringBuilder();
            foreach (var id in GameState.ItemOrder)
            {
                var d = Items.Get(id);
                if (d == null) continue;
                sb.Append("<color=#F4C98A>").Append(d.name).Append("</color>\n").Append(d.desc).Append("\n\n");
            }
            return sb.ToString().TrimEnd();
        }
    }
}
