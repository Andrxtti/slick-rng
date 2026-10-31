// One-click mobile builds: the "Dream" menu in Unity's top bar.
//   Dream > Apply mobile settings         app name, ID, portrait lock, 64-bit, icon
//   Dream > Build Android APK             a phone-installable APK in C:\Dev\Builds
//   Dream > Build Android APK (dev)       same, with logs you can read over USB
//   Dream > Build iOS Xcode project       for a Mac (or a cloud build) to sign and ship
//   Dream > Build Web (iPhone Safari)     a web version to play on any phone; Add to Home Screen
// Also works from the command line: Unity.exe -batchmode -quit -projectPath "<project>" -executeMethod CFDream.EditorTools.DreamBuild.BuildAndroid
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CFDream.EditorTools
{
    public static class DreamBuild
    {
        public const string AppId = "com.confirmedfire.dream", Company = "Confirmed Fire", Product = "The Dream", Version = "0.1.0";
        const string IconDir = "Assets/Dream/Art/Icon/";
        public static bool Quiet;   // set by DreamRemote: no Explorer window popping up after a build
        static string OutDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Builds"));

        [MenuItem("Dream/Apply mobile settings", priority = 1)]
        public static void ApplySettings()
        {
            PlayerSettings.companyName = Company;
            PlayerSettings.productName = Product;
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, AppId);

            // portrait or landscape: the screens switch to two-pane layouts when the phone is turned sideways
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // Android: 64-bit IL2CPP (what the Play Store requires), Android 10 and newer
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            if (PlayerSettings.Android.bundleVersionCode < 1) PlayerSettings.Android.bundleVersionCode = 1;

            // iOS 15 and newer
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.buildNumber = PlayerSettings.Android.bundleVersionCode.ToString();

            PlayerSettings.SplashScreen.backgroundColor = new Color32(11, 10, 8, 255);
            Icons();
            Materials3D();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Dream] Mobile settings applied: {Product} ({AppId}) {Version}, portrait + landscape, ARM64, Android 10+, iOS 15+");
        }

        static void Icons()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconDir + "icon.png");
            if (icon == null) { Debug.LogWarning("[Dream] App icon not found at " + IconDir + "icon.png"); return; }
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            AdaptiveIcons(icon, AssetDatabase.LoadAssetAtPath<Texture2D>(IconDir + "icon_bg.png"), AssetDatabase.LoadAssetAtPath<Texture2D>(IconDir + "icon_fg.png"));
        }

        // Android 8+ launchers mask icons into their own shape; give them a background and a foreground layer.
        // Looked up by name so this file compiles whether or not the Android module is installed.
        static void AdaptiveIcons(Texture2D icon, Texture2D bg, Texture2D fg)
        {
            if (bg == null || fg == null) return;
            try
            {
                var t = typeof(PlayerSettings); var nbt = NamedBuildTarget.Android;
                var getKinds = t.GetMethod("GetSupportedIconKinds", new[] { typeof(NamedBuildTarget) });
                var getIcons = t.GetMethod("GetPlatformIcons", new[] { typeof(NamedBuildTarget), typeof(PlatformIconKind) });
                var setIcons = t.GetMethod("SetPlatformIcons", new[] { typeof(NamedBuildTarget), typeof(PlatformIconKind), typeof(PlatformIcon[]) });
                if (getKinds == null || getIcons == null || setIcons == null) return;
                var kinds = getKinds.Invoke(null, new object[] { nbt }) as PlatformIconKind[];
                if (kinds == null || kinds.Length == 0) return;
                foreach (var k in kinds)
                {
                    var icons = getIcons.Invoke(null, new object[] { nbt, k }) as PlatformIcon[]; if (icons == null) continue;
                    bool adaptive = k.ToString().IndexOf("Adaptive", StringComparison.OrdinalIgnoreCase) >= 0;
                    foreach (var ic in icons)
                    {
                        if (adaptive && ic.maxLayerCount >= 2) ic.SetTextures(bg, fg);
                        else ic.SetTextures(icon);
                    }
                    setIcons.Invoke(null, new object[] { nbt, k, icons });
                }
            }
            catch (Exception e) { Debug.LogWarning("[Dream] Adaptive icons skipped: " + e.GetBaseException().Message); }
        }

        // The 3D character's jewellery, glasses and hats tint copies of two materials. Keeping them as assets in
        // Resources means the URP Lit shader (opaque and see-through) ships in every build.
        [MenuItem("Dream/Set up 3D materials", priority = 2)]
        public static void Materials3D()
        {
            const string dir = "Assets/Dream/Resources/Dream/Materials";
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) { Debug.LogWarning("[Dream] URP Lit shader not found; 3D materials skipped"); return; }
            if (!AssetDatabase.IsValidFolder(dir)) { Directory.CreateDirectory(dir); AssetDatabase.Refresh(); }
            Material Make(string name, bool glass)
            {
                string path = dir + "/" + name + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
                m.shader = sh;
                m.SetColor("_BaseColor", glass ? new Color(1, 1, 1, 0.15f) : Color.white);
                m.SetFloat("_Smoothness", glass ? 0.95f : 0.5f);
                if (glass) CFDream.Character.Mats.MakeTransparent(m);
                m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black); // keep the glow variant
                EditorUtility.SetDirty(m);
                return m;
            }
            Make("Lit", false); Make("Glass", true);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Dream/Build Android APK", priority = 20)]
        public static void BuildAndroid() => Build(BuildTarget.Android, false);

        [MenuItem("Dream/Build Android APK (dev, with logs)", priority = 21)]
        public static void BuildAndroidDev() => Build(BuildTarget.Android, true);

        [MenuItem("Dream/Build iOS Xcode project", priority = 22)]
        public static void BuildIOS() => Build(BuildTarget.iOS, false);

        [MenuItem("Dream/Build Web (iPhone Safari)", priority = 23)]
        public static void BuildWeb() => Build(BuildTarget.WebGL, false);

        // the web version: our own phone-first page (safe area, home-screen icon), gzip that works on any static host
        static void WebSettings()
        {
            PlayerSettings.SplashScreen.show = false;   // the game has its own title screen; saves a few MB on the web
            PlayerSettings.SplashScreen.showUnityLogo = false;   // otherwise the 2.7 MB logo texture still ships
            PlayerSettings.WebGL.template = "PROJECT:Dream";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            WebTextures();
        }

        // iPhones read ASTC textures straight from the GPU; DXT (Unity's web default) has to be unpacked to full-size
        // RGBA on an iPhone, several times the memory. ASTC is also smaller to download. Only the textures that go into
        // the build are switched (everything under a Resources folder, the scenes, the UMA library), so the hundreds
        // of unused UMA textures aren't re-encoded.
        static readonly string[] Detail = { "_NM", "_SG", "Normal", "Gloss", "Detail", "_AO", ".AO", "_N_", "Mask" };
        [MenuItem("Dream/Web textures to ASTC (iPhone)", priority = 30)]
        public static void WebTextures()
        {
            var roots = new List<string>();
            roots.AddRange(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path));
            roots.AddRange(AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && p.Contains("/Resources/") && !p.Contains("/Editor/") && !AssetDatabase.IsValidFolder(p)));
#if DREAM_UMA
            roots.AddRange(DreamUMATools.LibraryPaths());
#endif
            var deps = AssetDatabase.GetDependencies(roots.ToArray(), true).Distinct().ToList();
            int seen = 0, changed = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var p in deps)
                {
                    if (!(AssetImporter.GetAtPath(p) is TextureImporter ti)) continue;
                    if (ti.textureCompression == TextureImporterCompression.Uncompressed || ti.textureType == TextureImporterType.SingleChannel) continue;
                    seen++;
                    var ps = ti.GetPlatformTextureSettings("WebGL");
                    bool detail = ti.textureType == TextureImporterType.NormalMap || Detail.Any(k => Path.GetFileName(p).IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
                    var want = detail ? TextureImporterFormat.ASTC_8x8 : TextureImporterFormat.ASTC_6x6;
                    if (ps.overridden && ps.format == want && !(p.StartsWith("Assets/Dream/Clothes/") && ps.maxTextureSize > 512)) continue;
                    if (ps.overridden && ps.format != TextureImporterFormat.Automatic && !ps.format.ToString().StartsWith("DXT") && !ps.format.ToString().StartsWith("BC")) continue;   // a deliberate format: keep it
                    if (!ps.overridden) ps.maxTextureSize = ti.maxTextureSize;
                    if (p.StartsWith("Assets/Dream/Clothes/")) ps.maxTextureSize = Mathf.Min(ps.maxTextureSize, 512);   // painted clothes: plenty on a phone
                    ps.overridden = true; ps.format = want; ps.compressionQuality = 50;
                    ti.SetPlatformTextureSettings(ps); EditorUtility.SetDirty(ti); ti.SaveAndReimport(); changed++;
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            Debug.Log($"[Dream] Web textures: {seen} in the build, {changed} switched to ASTC");
        }

        // Colour emoji can't be drawn in the web build (the text drops them), so the 10 MB emoji font stays home.
        // It's moved out of Resources for the build and put straight back afterwards.
        static readonly string[] WebSkip = { "Assets/Dream/Resources/Dream/Fonts/NotoColorEmoji.ttf" };
        const string Parked = "Assets/Dream/Parked";
        static List<(string src, string dst)> Park()
        {
            var moved = new List<(string, string)>();
            if (!AssetDatabase.IsValidFolder(Parked)) AssetDatabase.CreateFolder("Assets/Dream", "Parked");
            foreach (var p in WebSkip)
            {
                if (AssetDatabase.LoadMainAssetAtPath(p) == null) continue;
                string to = Parked + "/" + Path.GetFileName(p);
                string err = AssetDatabase.MoveAsset(p, to);
                if (string.IsNullOrEmpty(err)) moved.Add((p, to)); else Debug.LogWarning($"[Dream] Couldn't leave {p} out of the web build: {err}");
            }
            return moved;
        }
        static void Unpark(List<(string src, string dst)> moved)
        {
            foreach (var (src, dst) in moved)
            {
                string err = AssetDatabase.MoveAsset(dst, src);
                if (!string.IsNullOrEmpty(err)) Debug.LogError($"[Dream] Put {dst} back at {src} by hand: {err}");
            }
            AssetDatabase.Refresh();
        }
        // after a build that ran from the menu or a parked file was left behind by a crash: put it back
        [InitializeOnLoadMethod]
        static void UnparkLeftovers()
        {
            EditorApplication.delayCall += () =>
            {
                if (BuildPipeline.isBuildingPlayer || !AssetDatabase.IsValidFolder(Parked)) return;
                var back = new List<(string, string)>();
                foreach (var p in WebSkip) { string to = Parked + "/" + Path.GetFileName(p); if (AssetDatabase.LoadMainAssetAtPath(to) != null && AssetDatabase.LoadMainAssetAtPath(p) == null) back.Add((p, to)); }
                if (back.Count > 0) { Debug.Log("[Dream] Putting back files parked for a web build"); Unpark(back); }
            };
        }

        // what took the space: Logs/build_sizes.txt, biggest assets and folders first
        static void SizeReport(BuildReport report, string name)
        {
            try
            {
                var items = new List<(string path, string type, ulong size)>();
                foreach (var pa in report.packedAssets ?? new PackedAssets[0])
                    foreach (var c in pa.contents) items.Add((string.IsNullOrEmpty(c.sourceAssetPath) ? "(built-in) " + pa.shortPath : c.sourceAssetPath, c.type != null ? c.type.Name : "?", c.packedSize));
                var sb = new StringBuilder();
                sb.AppendLine($"{name} build {DateTime.Now:yyyy-MM-dd HH:mm}  total {report.summary.totalSize / 1048576.0:0.0} MB (uncompressed data: {items.Sum(i => (double)i.size) / 1048576.0:0.0} MB)");
                if (items.Count == 0) sb.AppendLine("No asset sizes: player data wasn't rebuilt this time.");
                sb.AppendLine("\nBy type:");
                foreach (var g in items.GroupBy(i => i.type).OrderByDescending(g => g.Sum(i => (double)i.size)).Take(15))
                    sb.AppendLine($"{g.Sum(i => (double)i.size) / 1048576.0,8:0.00} MB  {g.Key} ({g.Count()})");
                sb.AppendLine("\nBy folder:");
                foreach (var g in items.GroupBy(i => Path.GetDirectoryName(i.path)?.Replace('\\', '/') ?? "").OrderByDescending(g => g.Sum(i => (double)i.size)).Take(40))
                    sb.AppendLine($"{g.Sum(i => (double)i.size) / 1048576.0,8:0.00} MB  {g.Key} ({g.Count()})");
                sb.AppendLine("\nBiggest assets:");
                foreach (var g in items.GroupBy(i => i.path).OrderByDescending(g => g.Sum(i => (double)i.size)).Take(80))
                    sb.AppendLine($"{g.Sum(i => (double)i.size) / 1048576.0,8:0.00} MB  {g.Key}  [{string.Join(",", g.Select(i => i.type).Distinct())}]");
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "..", "Logs"));
                File.WriteAllText(Path.Combine(Application.dataPath, "..", "Logs", "build_sizes.txt"), sb.ToString());
            }
            catch (Exception e) { Debug.LogWarning("[Dream] Size report skipped: " + e.Message); }
        }

        [MenuItem("Dream/Open builds folder", priority = 40)]
        public static void OpenBuilds() { Directory.CreateDirectory(OutDir); EditorUtility.RevealInFinder(OutDir + Path.DirectorySeparatorChar); }

        static void Build(BuildTarget target, bool dev)
        {
            var group = BuildPipeline.GetBuildTargetGroup(target);
            string name = target == BuildTarget.Android ? "Android" : target == BuildTarget.WebGL ? "Web" : "iOS";
            if (!BuildPipeline.IsBuildTargetSupported(group, target))
            {
                string msg = $"{name} Build Support isn't installed for this Unity version.\n\nUnity Hub → Installs → ⚙ on {Application.unityVersion} → Add modules → {name} Build Support" + (target == BuildTarget.Android ? " (with OpenJDK and Android SDK & NDK Tools)." : ".");
                if (Application.isBatchMode) Debug.LogError("[Dream] " + msg); else EditorUtility.DisplayDialog("The Dream", msg, "OK");
                return;
            }
            ApplySettings();
            if (EditorUserBuildSettings.activeBuildTarget != target && !EditorUserBuildSettings.SwitchActiveBuildTarget(group, target))
            {
                Debug.LogError($"[Dream] Couldn't switch the project to {name}.");
                return;
            }
            if (target == BuildTarget.Android) EditorUserBuildSettings.buildAppBundle = false;
            if (target == BuildTarget.WebGL) WebSettings();

            Directory.CreateDirectory(OutDir);
            string path = target == BuildTarget.Android ? Path.Combine(OutDir, $"TheDream-{Version}{(dev ? "-dev" : "")}.apk")
                : target == BuildTarget.WebGL ? Path.Combine(OutDir, "TheDream-Web") : Path.Combine(OutDir, "TheDream-iOS");
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) scenes = new[] { "Assets/Scenes/SampleScene.unity" };
            var opts = new BuildPlayerOptions
            {
                scenes = scenes, locationPathName = path, target = target, targetGroup = group,
                options = dev ? BuildOptions.Development | BuildOptions.AllowDebugging : BuildOptions.None,
            };
            var parked = target == BuildTarget.WebGL ? Park() : new List<(string, string)>();
            BuildReport report;
            try { report = BuildPipeline.BuildPlayer(opts); }
            finally { Unpark(parked); }
            SizeReport(report, name);
            var sum = report.summary;
            if (sum.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Dream] Build succeeded: {path} ({sum.totalSize / 1048576.0:0.0} MB in {sum.totalTime.TotalMinutes:0.0} min)");
                if (!Application.isBatchMode && !Quiet) EditorUtility.RevealInFinder(path);
            }
            else Debug.LogError($"[Dream] {name} build {sum.result} with {sum.totalErrors} error(s). The details are in the Console above this line.");
        }
    }
}
