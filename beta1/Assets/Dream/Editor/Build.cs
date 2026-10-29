// The web beta: remote "build-web" -> C:\Dev\Builds\TheDream-Beta1-Web (staged, not published: Steven decides).
// Gzip with the decompression fallback, so any static host works. Film-only assets (FilmSets, FilmCast, FilmAnim,
// FilmData) are editor-only and never enter the build; the films ship as StreamingAssets/Films/*.mp4.
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

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
            var r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Dream/Scenes/Main.unity" }, locationPathName = Out, target = BuildTarget.WebGL, options = BuildOptions.None,
            });
            long bytes = 0; foreach (var f in Directory.GetFiles(Path.Combine(Out, "Build"))) bytes += new FileInfo(f).Length;
            Remote.Log($"build-web: {r.summary.result}, {r.summary.totalErrors} errors, Build/ {bytes / 1e6:0.0} MB -> {Out}");
        }
    }
}
