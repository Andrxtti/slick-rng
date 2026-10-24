// Audio made by tools/art/audio.py imports as compressed Vorbis so the web build stays small; stingers decompress on load.
using UnityEditor;
using UnityEngine;

namespace CFDream.EditorTools
{
    public class DreamAudioImport : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Dream/Resources/Dream/Audio/")) return;
            var imp = (AudioImporter)assetImporter; string n = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            var st = imp.defaultSampleSettings;
            st.compressionFormat = AudioCompressionFormat.Vorbis; st.quality = n.StartsWith("music_") ? 0.45f : 0.3f;
            st.loadType = n.StartsWith("sfx_") ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.CompressedInMemory;
            imp.defaultSampleSettings = st; imp.forceToMono = false; imp.loadInBackground = true;
        }
    }
}
