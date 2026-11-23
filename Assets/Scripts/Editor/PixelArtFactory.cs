using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Misterio.EditorTools
{
    /// <summary>
    /// Draws the gameplay sprites with the same palette as the cutscenes, so the floors read as part
    /// of the same art direction instead of coloured blocks. Every sprite is generated pixel by
    /// pixel, which keeps the result crisp under the pixel-perfect camera and easy to revise.
    /// </summary>
    public static class PixelArtFactory
    {
        // Palette taken from Assets/Art/Palette/paleta_misterio.hex
        public static readonly Color Black = Hex(0x000000);
        public static readonly Color Ink = Hex(0x141830);
        public static readonly Color Grey1 = Hex(0x33302e);
        public static readonly Color Grey2 = Hex(0x3c3b39);
        public static readonly Color Grey3 = Hex(0x55524f);
        public static readonly Color Grey4 = Hex(0x746f68);
        public static readonly Color Grey5 = Hex(0x9e978c);
        public static readonly Color Grey6 = Hex(0xaeb7a4);
        public static readonly Color Brown1 = Hex(0x261a16);
        public static readonly Color Brown2 = Hex(0x5c3a2a);
        public static readonly Color Brown3 = Hex(0x7a4e36);
        public static readonly Color Brown4 = Hex(0x7c3901);
        public static readonly Color Brown5 = Hex(0x9b6540);
        public static readonly Color Brown6 = Hex(0xbb8865);
        public static readonly Color Cream1 = Hex(0xcec8bc);
        public static readonly Color Cream2 = Hex(0xdad1b6);
        public static readonly Color Cream3 = Hex(0xece8de);
        public static readonly Color Cream4 = Hex(0xfcf6de);
        public static readonly Color Blue1 = Hex(0x8ec1da);
        public static readonly Color Blue2 = Hex(0xe9f8fd);

        static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

        /// <summary>Creates a PNG-backed sprite asset from a per-pixel painter function.</summary>
        public static Sprite Build(string assetName, int width, int height, Func<int, int, Color> painter,
                                   float pixelsPerUnit = 128f)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = painter(x, y);
            texture.SetPixels(pixels);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();

            string path = "Assets/Art/Gameplay/" + assetName + ".png";
            FloorSceneKit.CreateFolder("Assets/Art/Gameplay");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePixelsPerUnit = pixelsPerUnit;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---------------------------------------------------------------- statues

        /// <summary>A carved wooden sculpture on a base, with visible grain and a lit left edge.</summary>
        public static Sprite Statue(string name, int seed)
        {
            return Build(name, 28, 56, (x, y) =>
            {
                int baseTop = 6;
                // Base
                if (y < baseTop)
                {
                    if (x < 2 || x > 25) return Transparent;
                    if (y == 0) return Brown1;
                    if (y == baseTop - 1) return Brown5;
                    return x < 6 ? Brown3 : Brown2;
                }

                // Torso: a tapered column that widens at the shoulders.
                float t = (y - baseTop) / (float)(55 - baseTop);
                int halfWidth = Mathf.RoundToInt(Mathf.Lerp(5f, 9f, Mathf.Sin(t * Mathf.PI * 0.85f)));
                int centre = 14;
                int left = centre - halfWidth;
                int right = centre + halfWidth;

                if (x < left || x > right) return Transparent;

                // Grain: vertical streaks with a deterministic wobble per seed.
                int grain = ((x * 7 + seed * 13) % 11);
                if (x == left) return Brown1;                       // contour
                if (x == left + 1) return Brown5;                    // lit edge
                if (x == right) return Brown1;
                if (grain == 0) return Brown2;
                if (grain == 3) return Brown4;
                if (grain == 6) return Brown5;
                return Brown3;
            });
        }

        /// <summary>White floor tape marking where a statue belongs.</summary>
        public static Sprite PlacementMark()
        {
            return Build("marcacao_piso", 26, 16, (x, y) =>
            {
                bool border = x <= 1 || y <= 1 || x >= 24 || y >= 14;
                if (!border) return Transparent;
                // Dashed tape: every third pixel is a gap.
                if ((x + y) % 6 == 0) return Transparent;
                return y <= 1 ? Cream4 : Cream2;
            });
        }

        // ---------------------------------------------------------------- paintings

        public enum Motif { Coast, Mountain, Backlands }

        /// <summary>A framed canvas with a fictional regional motif.</summary>
        public static Sprite Painting(string name, Motif motif)
        {
            return Build(name, 32, 24, (x, y) =>
            {
                // Frame
                bool frame = x <= 1 || y <= 1 || x >= 30 || y >= 22;
                if (frame)
                {
                    if (x == 0 || y == 0 || x == 31 || y == 23) return Brown1;
                    return (x + y) % 5 == 0 ? Brown4 : Brown3;
                }

                int cx = x - 2, cy = y - 2;   // 0..27, 0..19 inside the canvas
                switch (motif)
                {
                    case Motif.Coast:
                        // Sky band, sea band with wave crests, sand at the bottom.
                        if (cy > 13) return cy > 16 ? Cream2 : Blue1;
                        if (cy > 6) return (cx % 7 == 0 && cy % 3 == 0) ? Blue2 : Blue1;
                        if (cy > 4) return Cream1;
                        return (cx + cy) % 9 == 0 ? Cream3 : Cream2;

                    case Motif.Mountain:
                        // Layered ridges fading into mist.
                        int ridge = 8 + Mathf.RoundToInt(3f * Mathf.Sin(cx * 0.42f));
                        if (cy > ridge + 4) return Grey3;
                        if (cy > ridge) return Grey4;
                        if (cy > ridge - 4) return Grey5;
                        return (cx + cy * 2) % 11 == 0 ? Cream1 : Grey6;

                    case Motif.Backlands:
                        // Warm ground, low sun, dry brush strokes.
                        if (cy < 6) return Brown4;
                        if (cy < 10) return (cx % 5 == 0) ? Brown5 : Brown6;
                        if (cy < 13) return Cream2;
                        return (cx % 4 == 0) ? Brown3 : Brown2;
                }
                return Brown2;
            });
        }

        // ---------------------------------------------------------------- conduits

        /// <summary>A metal conduit tile with rivets; the sockets are drawn by separate sprites.</summary>
        public static Sprite ConduitBody()
        {
            return Build("conduite_corpo", 24, 24, (x, y) =>
            {
                if (x <= 1 || y <= 1 || x >= 22 || y >= 22) return Grey1;
                bool inner = x >= 4 && y >= 4 && x <= 19 && y <= 19;
                if (!inner) return Grey3;
                // Brushed metal highlight along the top-left.
                if (x <= 6 && y >= 18) return Grey5;
                if (y >= 20) return Grey4;
                if ((x * 3 + y * 5) % 17 == 0) return Grey4;
                return Grey2;
            });
        }

        /// <summary>A glowing connector stub that marks a socket direction.</summary>
        public static Sprite ConduitSocket()
        {
            return Build("conduite_bocal", 8, 12, (x, y) =>
            {
                if (x <= 0 || x >= 7 || y <= 0 || y >= 11) return Grey1;
                if (y >= 9) return Blue2;
                if (y >= 6) return Blue1;
                return Grey4;
            });
        }

        /// <summary>Corner brackets used as the panel cursor.</summary>
        public static Sprite Cursor()
        {
            return Build("cursor_conduite", 28, 28, (x, y) =>
            {
                const int arm = 7;
                bool horizontal = (y <= 2 || y >= 25) && (x <= arm || x >= 25 - arm);
                bool vertical = (x <= 2 || x >= 25) && (y <= arm || y >= 25 - arm);
                if (!horizontal && !vertical) return Transparent;
                return Cream4;
            });
        }

        // ---------------------------------------------------------------- archive

        /// <summary>A ceramic shard from the collection.</summary>
        public static Sprite ArchivePiece()
        {
            return Build("peca_acervo", 18, 18, (x, y) =>
            {
                float dx = x - 8.5f, dy = y - 8.5f;
                float radius = 7f + Mathf.Sin(Mathf.Atan2(dy, dx) * 3f) * 1.2f;
                if (dx * dx + dy * dy > radius * radius) return Transparent;
                if (dx * dx + dy * dy > (radius - 1.4f) * (radius - 1.4f)) return Brown3;
                if (dx + dy > 4f) return Cream1;
                if (dx + dy > 1f) return Cream2;
                return Cream3;
            });
        }

        /// <summary>A loose floorboard that creaks when stepped on.</summary>
        public static Sprite LooseBoard()
        {
            return Build("tabua_solta", 28, 14, (x, y) =>
            {
                if (y <= 1 || y >= 12) return Brown1;
                if (x <= 1 || x >= 26) return Brown1;
                if (y == 2) return Brown5;
                if ((x * 5 + y * 3) % 13 == 0) return Brown4;
                return Brown3;
            });
        }

        /// <summary>A glass display case with a highlight and a dark base.</summary>
        public static Sprite DisplayCase()
        {
            return Build("vitrine", 44, 26, (x, y) =>
            {
                if (y <= 2) return Brown1;                                  // base
                if (x <= 1 || x >= 42) return Brown1;                       // side rails
                if (y >= 24) return Brown2;                                 // top rail
                // Glass with a diagonal reflection.
                bool reflection = (x - y) > 4 && (x - y) < 9;
                bool reflection2 = (x - y) > 20 && (x - y) < 23;
                if (reflection) return new Color(Blue2.r, Blue2.g, Blue2.b, 0.55f);
                if (reflection2) return new Color(Blue1.r, Blue1.g, Blue1.b, 0.35f);
                return new Color(Blue1.r, Blue1.g, Blue1.b, 0.16f);
            });
        }

        // ---------------------------------------------------------------- effects

        /// <summary>Soft vignette that matches the cutscene framing.</summary>
        public static Sprite Vignette()
        {
            return Build("vinheta_gameplay", 128, 72, (x, y) =>
            {
                float nx = (x - 63.5f) / 63.5f;
                float ny = (y - 35.5f) / 35.5f;
                float distance = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.15f, distance)) * 0.72f;
                return new Color(0.012f, 0.018f, 0.045f, alpha);
            }, 64f);
        }

        /// <summary>A soft radial glow used under lamps and around the panel.</summary>
        public static Sprite Glow()
        {
            return Build("brilho_suave", 64, 64, (x, y) =>
            {
                float nx = (x - 31.5f) / 31.5f;
                float ny = (y - 31.5f) / 31.5f;
                float distance = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = alpha * alpha * 0.85f;
                return new Color(Blue1.r, Blue1.g, Blue1.b, alpha);
            }, 64f);
        }

        static Color Transparent => new Color(0f, 0f, 0f, 0f);
    }
}
