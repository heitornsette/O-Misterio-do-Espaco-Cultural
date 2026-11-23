using System;
using UnityEditor;
using UnityEngine;

namespace Misterio.EditorTools
{
    /// <summary>
    /// Art for the outdoor approach: dawn sky, campus skyline, trees and the plaza. Each layer is a
    /// wide strip drawn at a different depth so the camera can scroll them at different speeds.
    /// </summary>
    public static class ExteriorArtFactory
    {
        public static readonly Color SkyTop = new Color(0.05f, 0.07f, 0.16f);
        public static readonly Color SkyMid = new Color(0.14f, 0.18f, 0.30f);
        public static readonly Color SkyLow = new Color(0.36f, 0.32f, 0.38f);
        public static readonly Color SkyGlow = new Color(0.62f, 0.48f, 0.42f);

        /// <summary>The night sky with a warm band at the horizon.</summary>
        public static Sprite Sky(int width = 384, int height = 216)
        {
            return PixelArtFactory.Build("ceu_chegada", width, height, (x, y) =>
            {
                float t = y / (float)(height - 1);
                Color color;
                if (t > 0.78f) color = Color.Lerp(SkyMid, SkyTop, (t - 0.78f) / 0.22f);
                else if (t > 0.30f) color = Color.Lerp(SkyLow, SkyMid, (t - 0.30f) / 0.48f);
                else color = Color.Lerp(SkyGlow, SkyLow, t / 0.30f);

                // A few stars high up, deterministic.
                if (t > 0.82f && (x * 37 + y * 91) % 613 == 0) color = new Color(0.92f, 0.95f, 1f);
                return color;
            }, 108f);
        }

        /// <summary>Distant campus buildings, flat silhouettes for the far parallax layer.</summary>
        public static Sprite Skyline(int width = 512, int height = 96)
        {
            return PixelArtFactory.Build("skyline_campus", width, height, (x, y) =>
            {
                // Deterministic block silhouette with windows.
                int block = x / 64;
                int blockHeight = 34 + (block * 37) % 46;
                if (y > blockHeight) return Transparent;

                var body = new Color(0.10f, 0.12f, 0.20f, 1f);
                if (y == blockHeight) body = new Color(0.16f, 0.19f, 0.28f, 1f);
                // Lit windows.
                if (y > 8 && y < blockHeight - 4 && (x % 9 == 4) && ((y / 7) % 3 == 0))
                    body = new Color(0.72f, 0.66f, 0.42f, 1f);
                return body;
            }, 108f);
        }

        /// <summary>A row of trees for the middle parallax layer.</summary>
        public static Sprite TreeLine(int width = 512, int height = 120)
        {
            return PixelArtFactory.Build("arvores_campus", width, height, (x, y) =>
            {
                // Trunks every 84 px with canopies above.
                int tree = x / 84;
                int centre = tree * 84 + 42;
                int dx = x - centre;
                int trunkTop = 42 + (tree * 23) % 18;

                if (Mathf.Abs(dx) <= 4 && y < trunkTop)
                    return dx == 0 ? new Color(0.20f, 0.15f, 0.12f) : new Color(0.28f, 0.21f, 0.16f);

                float radius = 34f + (tree * 11) % 10;
                float ny = (y - trunkTop) / radius;
                float nx = dx / radius;
                if (nx * nx + ny * ny <= 1f)
                {
                    float shade = 0.5f + 0.5f * Mathf.Sin((x + y) * 0.35f);
                    return Color.Lerp(new Color(0.10f, 0.20f, 0.16f), new Color(0.20f, 0.34f, 0.24f), shade);
                }
                return Transparent;
            }, 108f);
        }

        /// <summary>The plaza paving with lamps, for the near parallax layer.</summary>
        public static Sprite PlazaGround(int width = 512, int height = 72)
        {
            return PixelArtFactory.Build("praca_piso", width, height, (x, y) =>
            {
                if (y > 8) return Transparent;

                // Paving stones with a subtle grid and grout lines.
                bool grout = (x % 32 == 0) || (y == 4);
                var stone = new Color(0.26f, 0.26f, 0.30f);
                if (grout) stone = new Color(0.16f, 0.16f, 0.20f);
                else if ((x / 32 + y / 4) % 2 == 0) stone = new Color(0.30f, 0.30f, 0.34f);
                return stone;
            }, 108f);
        }

        /// <summary>A plaza lamp post with a warm glow.</summary>
        public static Sprite LampPost(int height = 84)
        {
            return PixelArtFactory.Build("poste_luz", 20, height, (x, y) =>
            {
                // Pole.
                if (x >= 9 && x <= 10 && y < height - 10) return new Color(0.12f, 0.12f, 0.16f);
                // Lamp head.
                if (y >= height - 12 && y <= height - 4 && x >= 5 && x <= 14)
                    return y >= height - 8 ? new Color(1f, 0.90f, 0.62f) : new Color(0.22f, 0.22f, 0.26f);
                // Base.
                if (y < 4 && x >= 7 && x <= 12) return new Color(0.14f, 0.14f, 0.18f);
                return Transparent;
            }, 108f);
        }

        /// <summary>The building facade the player walks up to.</summary>
        public static Sprite Facade(int width = 160, int height = 150)
        {
            return PixelArtFactory.Build("fachada_espaco", width, height, (x, y) =>
            {
                var stone = new Color(0.20f, 0.21f, 0.26f);
                var trim = new Color(0.28f, 0.29f, 0.34f);

                // Roof line.
                if (y >= height - 8) return trim;
                // Columns.
                if ((x % 40 < 4) && y > 10) return trim;
                // Glass doors at the centre.
                if (y > 6 && y < 90 && x > 58 && x < 102)
                {
                    if (y > 84) return trim;
                    var glass = new Color(0.42f, 0.52f, 0.62f);
                    if ((x + y) % 17 == 0) glass = new Color(0.62f, 0.74f, 0.82f);
                    return glass;
                }
                // Steps.
                if (y <= 6) return y <= 3 ? trim : stone;
                return stone;
            }, 108f);
        }

        static Color Transparent => new Color(0f, 0f, 0f, 0f);
    }
}
