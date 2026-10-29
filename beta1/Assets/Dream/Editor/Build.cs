// The web beta: remote "build-web" -> C:\Dev\Builds\TheDream-Beta1-Web (staged, not published: Steven decides).
// Gzip with the decompression fallback, so any static host works. Film-only assets (FilmSets, FilmCast, FilmAnim,
// FilmData) are editor-only and never enter the build; the films ship as StreamingAssets/Films/*.mp4.
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Dream.EditorTools
{
    [InitializeOnLoad]
    public static class Build
    {
        public const string Out = @"C:\Dev\Builds\TheDream-Beta1-Web";
        static Build() { Remote.Extra["build-web"] = _ => Web(); }

        public static void Web()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
            PlayerSettings.defaultWebScreenWidth = 540; PlayerSettings.defaultWebScreenHeight = 960;
            // phones get their own light pipeline; the films keep URP.asset (4K shadows, MSAA) in the editor
            var film = GraphicsSettings.defaultRenderPipeline; var phone = Mobile(); Quality.Run();
            SetPipeline(phone);
            BuildReport r;
            try
            {
                r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/Dream/Scenes/Main.unity" }, locationPathName = Out, target = BuildTarget.WebGL, options = BuildOptions.None,
                });
            }
            finally { SetPipeline(film); AssetDatabase.SaveAssets(); }   // write the film pipeline back to ProjectSettings
            long bytes = 0; foreach (var f in Directory.GetFiles(Path.Combine(Out, "Build"))) bytes += new FileInfo(f).Length;
            Remote.Log($"build-web: {r.summary.result}, {r.summary.totalErrors} errors, Build/ {bytes / 1e6:0.0} MB -> {Out}");
        }

        static void SetPipeline(RenderPipelineAsset a)
        {
            GraphicsSettings.defaultRenderPipeline = a;
            int was = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = a; }
            QualitySettings.SetQualityLevel(was, false);
        }

        // URP_Mobile.asset: the phone pipeline. Linear upscaling (FSR's EASU shader is stripped on the web, and with it
        // went every post-processing pass), no MSAA (the live game is UI), small soft shadows, 2 cascades, LDR-safe grade.
        static UniversalRenderPipelineAsset Mobile()
        {
            const string path = "Assets/Dream/Settings/URP_Mobile.asset";
            if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path) == null) AssetDatabase.CopyAsset("Assets/Dream/Settings/URP.asset", path);
            var a = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            var so = new SerializedObject(a);
            foreach (var (k, v) in new[] { ("m_UpscalingFilter", 1), ("m_MSAA", 1), ("m_MainLightShadowmapResolution", 1024), ("m_AdditionalLightsShadowmapResolution", 512),
                                           ("m_ShadowCascadeCount", 2), ("m_SoftShadowQuality", 1) })
                so.FindProperty(k).intValue = v;
            so.FindProperty("m_ShadowDistance").floatValue = 15f; so.FindProperty("m_RenderScale").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets();
            return a;
        }
    }
}
