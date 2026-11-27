using System.Collections.Generic;
using UnityEngine;

namespace EspacoCultural.Campus
{
    /// <summary>
    /// Arte das cenas do campus. As imagens ficam em Assets/Resources/Campus (fundos, NPCs) e
    /// Assets/Resources/Jogo (guarda, estátuas, pena, pegada...). Tudo vira Sprite em tempo de execução
    /// com 1 pixel = 1 unidade do mundo (a câmera Pixel Perfect usa PPU 1 e referência 640×360).
    /// </summary>
    public static class CampusArt
    {
        private static readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite[]> sheetCache = new Dictionary<string, Sprite[]>();

        private static Material lit;

        /// <summary>Material "Sprite-Lit-Default" do URP 2D: sem ele os sprites ignoram as Light 2D.</summary>
        public static Material Lit
        {
            get
            {
                if (lit != null) return lit;
                var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
                if (sh != null) lit = new Material(sh) { name = "Sprite-Lit (campus)" };
                else Debug.LogWarning("[Campus] Shader Sprite-Lit-Default não encontrado: os sprites ficarão sem luz.");
                return lit;
            }
        }

        /// <summary>Cria um SpriteRenderer já com o material iluminado.</summary>
        public static SpriteRenderer AddRenderer(GameObject go)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            if (Lit != null) sr.sharedMaterial = Lit;
            return sr;
        }

        public static Texture2D Tex(string name)
        {
            if (texCache.TryGetValue(name, out var t) && t != null) return t;
            t = Resources.Load<Texture2D>("Campus/" + name);
            if (t == null) t = Resources.Load<Texture2D>("Jogo/" + name);
            if (t == null) Debug.LogWarning("[Campus] Textura não encontrada: " + name);
            else { t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Clamp; }
            texCache[name] = t;
            return t;
        }

        /// <summary>Sprite da imagem inteira, com o pivô na posição (px, py) da imagem (y a partir de baixo).</summary>
        public static Sprite Whole(string name, Vector2 pivotPx)
        {
            var t = Tex(name);
            if (t == null) return null;
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height),
                new Vector2(pivotPx.x / t.width, pivotPx.y / t.height), 1f, 0, SpriteMeshType.FullRect);
        }

        public static Sprite FromTexture(Texture2D t, Vector2 pivotPx) =>
            t == null ? null : Sprite.Create(t, new Rect(0, 0, t.width, t.height),
                new Vector2(pivotPx.x / t.width, pivotPx.y / t.height), 1f, 0, SpriteMeshType.FullRect);

        /// <summary>
        /// Quadros de uma sprite sheet horizontal (quadros quadrados, altura = tamanho do quadro).
        /// footRow = linha (a partir do topo) onde o pé encosta no chão; vira o pivô.
        /// </summary>
        public static Sprite[] Sheet(string name, int footRow)
        {
            string key = name + "@" + footRow;
            if (sheetCache.TryGetValue(key, out var s) && s != null && s.Length > 0 && s[0] != null) return s;
            var t = Tex(name);
            if (t == null) return new Sprite[0];
            int size = t.height, n = Mathf.Max(1, t.width / size);
            s = new Sprite[n];
            for (int i = 0; i < n; i++)
                s[i] = Sprite.Create(t, new Rect(i * size, 0, size, size),
                    new Vector2(0.5f, (size - footRow) / (float)size), 1f, 0, SpriteMeshType.FullRect);
            sheetCache[key] = s;
            return s;
        }
    }
}
