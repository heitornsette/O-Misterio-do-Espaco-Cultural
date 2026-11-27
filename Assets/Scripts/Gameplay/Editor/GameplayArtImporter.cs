using UnityEditor;
using UnityEngine;

namespace EspacoCultural.Gameplay.EditorTools
{
    /// <summary>
    /// Configura automaticamente toda imagem colocada em Assets/Resources/Jogo:
    /// Sprite, filtro Point, sem compressão, sem mipmap, PPU 100, sem redimensionar (pixel art nítida).
    /// </summary>
    public class GameplayArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            var p = assetPath.Replace('\\', '/');
            if (!p.StartsWith("Assets/Resources/Jogo/") && !p.StartsWith("Assets/Resources/Campus/")) return;
            var imp = (TextureImporter)assetImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.alphaIsTransparency = true;
            imp.maxTextureSize = 4096;
        }
    }
}
