using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EspacoCultural.Gameplay
{
    /// <summary>
    /// Utilitários de UI em coordenadas de pixel (tela de 640×360, origem no canto superior esquerdo,
    /// y crescendo para baixo — igual ao Photoshop/Aseprite e às medidas das provas de cenário).
    /// A arte dos andares fica em Assets/Resources/Jogo e é carregada por nome.
    /// </summary>
    public static class Px
    {
        public const int W = 640, H = 360;
        public const string ResFolder = "Jogo/";

        private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        public static readonly Color Navy = new Color32(0x1E, 0x23, 0x4D, 0xFF);
        public static readonly Color LightBlue = new Color32(0x8E, 0xC1, 0xDA, 0xFF);
        public static readonly Color Warm = new Color32(0xFF, 0xD3, 0x7A, 0xFF);

        /// <summary>Carrega Resources/Jogo/&lt;nome&gt; (sem extensão).</summary>
        public static Texture2D Tex(string name)
        {
            if (cache.TryGetValue(name, out var t) && t != null) return t;
            t = Resources.Load<Texture2D>(ResFolder + name);
            if (t == null) Debug.LogWarning($"[Px] Textura não encontrada: Resources/{ResFolder}{name}");
            else { t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Clamp; }
            cache[name] = t;
            return t;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            return rt;
        }

        /// <summary>Posiciona pelo canto superior esquerdo (x, y em pixels da tela 640×360).</summary>
        public static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(Mathf.Round(x), -Mathf.Round(y));
            rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>Posiciona pelo centro (útil para peças que giram).</summary>
        public static void PlaceCenter(RectTransform rt, float cx, float cy, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(cx, -cy);
            rt.sizeDelta = new Vector2(w, h);
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public static RawImage Raw(string name, Transform parent, Texture tex, float x, float y, float w = -1, float h = -1)
        {
            var rt = Rect(name, parent);
            var ri = rt.gameObject.AddComponent<RawImage>();
            ri.texture = tex;
            ri.raycastTarget = false;
            if (w < 0) w = tex != null ? tex.width : 16;
            if (h < 0) h = tex != null ? tex.height : 16;
            Place(rt, x, y, w, h);
            return ri;
        }

        public static Image Box(string name, Transform parent, Color c, float x, float y, float w, float h)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            Place(rt, x, y, w, h);
            return img;
        }

        /// <summary>Painel no estilo das placas do museu: borda branca, filete azul-claro e fundo azul-marinho.</summary>
        public static RectTransform Panel(string name, Transform parent, float x, float y, float w, float h)
        {
            var outer = Box(name, parent, Color.white, x, y, w, h).rectTransform;
            Box("Filete", outer, LightBlue, 2, 2, w - 4, h - 4);
            Box("Fundo", outer, Navy, 3, 3, w - 6, h - 6);
            return outer;
        }

        public static Text Label(string name, Transform parent, Font font, int size, Color color,
                                 float x, float y, float w, float h, TextAnchor align = TextAnchor.UpperLeft)
        {
            var rt = Rect(name, parent);
            Place(rt, x, y, w, h);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.lineSpacing = 1.25f;
            t.raycastTarget = false;
            return t;
        }
    }
}
