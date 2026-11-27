using UnityEditor;
using UnityEngine;

namespace EspacoCultural.Audio.EditorTools
{
    /// <summary>Músicas em Resources/Audio tocam em streaming (Vorbis); efeitos ficam descomprimidos na memória.</summary>
    public class GameAudioImporter : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            string p = assetPath.Replace('\\', '/');
            if (!p.StartsWith("Assets/Resources/Audio/")) return;
            var imp = (AudioImporter)assetImporter;
            var s = imp.defaultSampleSettings;
            bool isMusic = System.IO.Path.GetFileName(p).StartsWith("musica_");
            s.loadType = isMusic ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = isMusic ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
            s.quality = 0.7f;
            imp.defaultSampleSettings = s;
            imp.forceToMono = true;
        }
    }
}
