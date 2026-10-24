// The movie's post-processing: a URP volume profile the scene camera switches on (depth of field on close-ups,
// bloom, ACES tonemapping, a gentle grade). It lives as an asset so the build keeps the effect shaders — URP strips
// post-processing variants that no profile in the project uses.
//   Dream > Create cinema profile   (or the remote command "cinema-profile")
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CFDream.EditorTools
{
    public static class DreamCinema
    {
        public const string Path = "Assets/Dream/Resources/Dream/Cinema.asset";

        [MenuItem("Dream/Create cinema profile")]
        public static void Create()
        {
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Path);
            if (existing != null) AssetDatabase.DeleteAsset(Path);
            var p = VolumeProfileFactory.CreateVolumeProfileAtPath(Path);

            var dof = VolumeProfileFactory.CreateVolumeComponent<DepthOfField>(p, true, false);
            dof.mode.Override(DepthOfFieldMode.Gaussian); dof.gaussianStart.Override(2.2f); dof.gaussianEnd.Override(5.5f);
            dof.gaussianMaxRadius.Override(1.1f); dof.highQualitySampling.Override(false);

            var bloom = VolumeProfileFactory.CreateVolumeComponent<Bloom>(p, true, false);
            bloom.threshold.Override(0.95f); bloom.intensity.Override(0.45f); bloom.scatter.Override(0.65f);

            var tone = VolumeProfileFactory.CreateVolumeComponent<Tonemapping>(p, true, false);
            tone.mode.Override(TonemappingMode.ACES);

            var ca = VolumeProfileFactory.CreateVolumeComponent<ColorAdjustments>(p, true, false);
            ca.postExposure.Override(0.15f); ca.contrast.Override(10f); ca.saturation.Override(-8f);

            var lgg = VolumeProfileFactory.CreateVolumeComponent<LiftGammaGain>(p, true, false);
            lgg.lift.Override(new Vector4(0.98f, 0.99f, 1.03f, -0.01f));    // cool shadows
            lgg.gain.Override(new Vector4(1.03f, 1.01f, 0.97f, 0.0f));      // warm highlights

            var chroma = VolumeProfileFactory.CreateVolumeComponent<ChromaticAberration>(p, true, false);
            chroma.intensity.Override(0.06f);

            EditorUtility.SetDirty(p); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("[Dream] cinema profile written to " + Path + " with " + p.components.Count + " effects");
        }
    }
}
