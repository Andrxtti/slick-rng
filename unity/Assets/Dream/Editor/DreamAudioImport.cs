// Audio made by tools/art/audio.py imports as compressed Vorbis so the web build stays small; stingers decompress on load.
using UnityEditor;
using UnityEngine;

namespace CFDream.EditorTools
{
    public class DreamAudioImport : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            string path = assetPath.Replace('\\', '/');
            var imp = (AudioImporter)assetImporter; string n = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            var st = imp.defaultSampleSettings;
            if (path.Contains("/Dream/Resources/Dream/Voice/"))   // spoken lines: mono, light, loaded when said
            {
                st.compressionFormat = AudioCompressionFormat.Vorbis; st.quality = 0.3f; st.loadType = AudioClipLoadType.CompressedInMemory;
                imp.defaultSampleSettings = st; imp.forceToMono = true; imp.loadInBackground = false; return;
            }
            if (!path.Contains("/Dream/Resources/Dream/Audio/")) return;
            st.compressionFormat = AudioCompressionFormat.Vorbis; st.quality = n.StartsWith("music_") ? 0.45f : 0.3f;
            st.loadType = n.StartsWith("sfx_") ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.CompressedInMemory;
            imp.defaultSampleSettings = st; imp.forceToMono = false; imp.loadInBackground = true;
        }
    }
}
