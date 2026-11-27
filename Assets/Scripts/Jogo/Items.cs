using System.Collections.Generic;
using UnityEngine;

namespace EspacoCultural.Jogo
{
    /// <summary>Itens do inventário: nome, descrição e ícone em pixel art (12×12, desenhado aqui).</summary>
    public static class Items
    {
        public class Def
        {
            public string id, name, desc;
            public string[] icon;
            public Dictionary<char, Color32> colors;
        }

        private static readonly Dictionary<char, Color32> Wood = new Dictionary<char, Color32>
        {
            { '#', new Color32(0x5c, 0x3a, 0x2a, 255) }, { 'o', new Color32(0xbb, 0x88, 0x65, 255) },
            { 'w', new Color32(0x9b, 0x65, 0x40, 255) }, { 'k', new Color32(0x26, 0x1a, 0x16, 255) },
        };
        private static readonly Dictionary<char, Color32> Book = new Dictionary<char, Color32>
        {
            { 'b', new Color32(0x14, 0x18, 0x30, 255) }, { 'w', new Color32(0x52, 0x76, 0x9a, 255) },
            { 'k', new Color32(0xde, 0xe1, 0xe9, 255) }, { 'y', new Color32(0xf4, 0xc9, 0x8a, 255) },
        };
        private static readonly Dictionary<char, Color32> Ball = new Dictionary<char, Color32>
        {
            { 'r', new Color32(0xc2, 0x4a, 0x3a, 255) }, { 'w', new Color32(0xfc, 0xf6, 0xde, 255) },
            { 'd', new Color32(0x7e, 0x3b, 0x2a, 255) },
        };
        private static readonly Dictionary<char, Color32> Key = new Dictionary<char, Color32>
        {
            { 'y', new Color32(0xf4, 0xc9, 0x8a, 255) }, { 'd', new Color32(0xc2, 0x7d, 0x46, 255) },
        };
        private static readonly Dictionary<char, Color32> Fuse = new Dictionary<char, Color32>
        {
            { 'm', new Color32(0xae, 0xb7, 0xa4, 255) }, { 'g', new Color32(0xdc, 0xd6, 0xbe, 255) },
            { 'k', new Color32(0x14, 0x18, 0x30, 255) }, { 'o', new Color32(0xff, 0x8a, 0x3d, 255) },
        };

        private static readonly string[] VaqueiroIcon =
        {
            "....####....", "..########..", "....oooo....", "....oooo....", "...wwwwww...", "..ww.ww.ww..",
            "..w.wwww.w..", "....wwww....", "....w..w....", "....w..w....", "..########..", "..########..",
        };
        private static readonly string[] CatalogoIcon =
        {
            "............", ".bbbbbbbbbb.", ".bwwwwwwwwb.", ".bwkkkkkkwb.", ".bwwwwwwwwb.", ".bwkkkkwwwb.",
            ".bwwwwwwwwb.", ".bwyyyyyywb.", ".bwyyyyyywb.", ".bwwwwwwwwb.", ".bbbbbbbbbb.", "............",
        };
        private static readonly string[] BolaIcon =
        {
            "............", "....rrrr....", "..rrrrrrrr..", ".rwwrrrrrrr.", ".rwrrrrrrrr.", "rrrrwwwwrrrr",
            "rrrrrrrrrrrr", ".rrrrrrrrrd.", ".rrrrrrrrdd.", "..rrrrrddd..", "....dddd....", "............",
        };
        private static readonly string[] ChaveIcon =
        {
            "............", "..yyyy......", ".yy..yy.....", ".y....y.....", ".yy..yy.....", "..yyyyyyyyy.",
            "......dy.yd.", "......d..d..", "............", "............", "............", "............",
        };
        private static readonly string[] FusivelIcon =
        {
            "............", "....kkkk....", "...kmmmmk...", "...kggggk...", "...kgoogk...", "...kgoogk...",
            "...kgoogk...", "...kgoogk...", "...kggggk...", "...kmmmmk...", "....kkkk....", "............",
        };

        public static readonly Dictionary<string, Def> All = new Dictionary<string, Def>
        {
            { "vaqueiro", new Def { id = "vaqueiro", name = "Estátua do Vaqueiro",
                desc = "Escultura de madeira tirada da fonte do CC. O lugar dela é a marcação com o chapéu, no térreo.",
                icon = VaqueiroIcon, colors = Wood } },
            { "catalogo", new Def { id = "catalogo", name = "Catálogo \"Mãos do Ceará\"",
                desc = "Catálogo da exposição, da Biblioteca Central.\n" +
                       "Matriz de Aquiraz — C. Bezerra, 1921\nMandacarus ao Meio-Dia — C. Bezerra, 1934\n" +
                       "Jangadas no Mucuripe — R. Lima, 1958\nRendeiras da Prainha — R. Lima, 1972\n" +
                       "Manhã em Guaramiranga — M. Alencar, 1987",
                icon = CatalogoIcon, colors = Book } },
            { "bola", new Def { id = "bola", name = "Bola de borracha",
                desc = "Dos achados e perdidos do ginásio. No acervo, E arremessa: a Moema é curiosa e vai atrás.",
                icon = BolaIcon, colors = Ball } },
            { "chave_registro", new Def { id = "chave_registro", name = "Chave do registro",
                desc = "A zeladora emprestou. Abre o cadeado do registro da fonte do CC.",
                icon = ChaveIcon, colors = Key } },
            { "fusivel_10", FuseDef(10) }, { "fusivel_16", FuseDef(16) },
            { "fusivel_25", FuseDef(25) }, { "fusivel_32", FuseDef(32) },
        };

        private static Def FuseDef(int amps) => new Def
        {
            id = "fusivel_" + amps, name = $"Fusível de {amps} A",
            desc = $"Fusível reserva do quadro do ginásio ({amps} A). Só serve no circuito que pede {amps} A.",
            icon = FusivelIcon, colors = Fuse,
        };

        public static Def Get(string id) => All.TryGetValue(id, out var d) ? d : null;

        private static readonly Dictionary<string, Texture2D> icons = new Dictionary<string, Texture2D>();

        public static Texture2D Icon(string id)
        {
            if (icons.TryGetValue(id, out var t) && t != null) return t;
            var d = Get(id);
            if (d == null) return null;
            t = Draw(d.icon, d.colors);
            icons[id] = t;
            return t;
        }

        /// <summary>Desenha um ícone a partir de linhas de texto (y para baixo), cada caractere uma cor.</summary>
        public static Texture2D Draw(string[] rows, Dictionary<char, Color32> colors)
        {
            int h = rows.Length, w = rows[0].Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    char c = x < rows[y].Length ? rows[y][x] : '.';
                    tex.SetPixel(x, h - 1 - y, colors.TryGetValue(c, out var col) ? (Color)col : Color.clear);
                }
            tex.Apply();
            return tex;
        }
    }
}
