using System;
using UnityEditor;
using UnityEngine;

namespace Misterio.EditorTools
{
    /// <summary>
    /// Light sprites for the floors. The project uses the built-in pipeline, so lighting is painted
    /// with additive sprites exactly the way the cutscenes already do it. That keeps the gameplay
    /// visually consistent with the videos without converting the project to URP.
    /// </summary>
    public static class LightArtFactory
    {
        /// <summary>A downward cone of light, as if from a ceiling fixture.</summary>
        public static Sprite SpotlightCone()
        {
            return PixelArtFactory.Build("luz_facho", 64, 96, (x, y) =>
            {
                // The cone narrows at the top and widens at the floor.
                float t = y / 95f;
                float halfWidth = Mathf.Lerp(3f, 30f, t);
                float centre = 31.5f;
                float distance = Mathf.Abs(x - centre);
                if (distance > halfWidth) return Transparent;

                // Soft edge: fade towards the border and towards the floor.
                float edge = 1f - (distance / halfWidth);
                float fade = Mathf.SmoothStep(0f, 1f, t) * (1f - Mathf.SmoothStep(0.82f, 1f, t));
                float alpha = edge * edge * fade * 0.34f;
                return new Color(0.86f, 0.92f, 1f, alpha);
            }, 96f);
        }

        /// <summary>A wide, very soft pool of light for ambient fill.</summary>
        public static Sprite AmbientPool()
        {
            return PixelArtFactory.Build("luz_poca", 96, 48, (x, y) =>
            {
                float nx = (x - 47.5f) / 47.5f;
                float ny = (y - 23.5f) / 23.5f;
                float distance = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = alpha * alpha * 0.22f;
                return new Color(0.72f, 0.84f, 1f, alpha);
            }, 96f);
        }

        /// <summary>A hard-edged elliptical shadow to ground objects on the floor.</summary>
        public static Sprite ObjectShadow()
        {
            return PixelArtFactory.Build("sombra_objeto", 48, 20, (x, y) =>
            {
                float nx = (x - 23.5f) / 23.5f;
                float ny = (y - 9.5f) / 9.5f;
                float distance = Mathf.Sqrt(nx * nx + ny * ny);
                if (distance > 1f) return Transparent;
                float alpha = Mathf.Clamp01(1f - distance) * 0.45f;
                return new Color(0f, 0f, 0.02f, alpha);
            }, 96f);
        }

        /// <summary>A warm rim light used on wooden pieces and the floor marks.</summary>
        public static Sprite WarmRim()
        {
            return PixelArtFactory.Build("luz_calida", 32, 32, (x, y) =>
            {
                float nx = (x - 15.5f) / 15.5f;
                float ny = (y - 15.5f) / 15.5f;
                float distance = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = alpha * alpha * 0.30f;
                return new Color(1f, 0.86f, 0.62f, alpha);
            }, 64f);
        }

        /// <summary>A single dust mote; scattered copies give the rooms depth.</summary>
        public static Sprite DustMote()
        {
            return PixelArtFactory.Build("poeira", 4, 4, (x, y) =>
            {
                bool lit = x >= 1 && x <= 2 && y >= 1 && y <= 2;
                if (!lit) return Transparent;
                return new Color(0.92f, 0.95f, 1f, 0.55f);
            }, 128f);
        }

        /// <summary>A soft radial halo for the moth and for clue highlights.</summary>
        public static Sprite Halo()
        {
            return PixelArtFactory.Build("halo", 32, 32, (x, y) =>
            {
                float nx = (x - 15.5f) / 15.5f;
                float ny = (y - 15.5f) / 15.5f;
                float distance = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = alpha * alpha * 0.55f;
                return new Color(1f, 0.94f, 0.72f, alpha);
            }, 64f);
        }

        /// <summary>The floating interaction marker shown above usable objects.</summary>
        public static Sprite InteractMarker()
        {
            return PixelArtFactory.Build("marcador_interagir", 14, 20, (x, y) =>
            {
                // A small "E" in a rounded plate, readable at 384x216.
                bool plate = x >= 2 && x <= 11 && y >= 4 && y <= 15;
                if (plate)
                {
                    if (x == 2 || x == 11 || y == 4 || y == 15) return PixelArtFactory.Cream4;
                    return new Color(0.02f, 0.03f, 0.08f, 0.88f);
                }
                // Pointer under the plate.
                if (y >= 1 && y <= 3 && x >= 5 && x <= 8) return PixelArtFactory.Cream4;
                return Transparent;
            }, 128f);
        }

        static Color Transparent => new Color(0f, 0f, 0f, 0f);
    }
}
