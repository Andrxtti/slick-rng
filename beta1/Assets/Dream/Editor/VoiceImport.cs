// The voices that ship in the first download (Resources/Voice): speech, so compressed hard (quality 0.35, mono). At the
// default (quality 1) Chapter 2's storyboard voices pushed Build/ to 31 MB, past the 25 MB budget (Milestone B).
using UnityEditor;
using UnityEngine;

namespace Dream.EditorTools
{
    public class VoiceImport : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/Dream/Resources/Voice")) return;
            var imp = (AudioImporter)assetImporter;
            imp.forceToMono = true;
            var s = imp.defaultSampleSettings;
            s.compressionFormat = AudioCompressionFormat.Vorbis; s.quality = 0.35f; s.loadType = AudioClipLoadType.CompressedInMemory;
            s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            imp.defaultSampleSettings = s;
        }
    }
}
