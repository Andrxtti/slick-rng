// One-click mobile builds: the "Dream" menu in Unity's top bar.
//   Dream > Apply mobile settings         app name, ID, portrait lock, 64-bit, icon
//   Dream > Build Android APK             a phone-installable APK in C:\Dev\Builds
//   Dream > Build Android APK (dev)       same, with logs you can read over USB
//   Dream > Build iOS Xcode project       for a Mac (or a cloud build) to sign and ship
// Also works from the command line: Unity.exe -batchmode -quit -projectPath "<project>" -executeMethod CFDream.EditorTools.DreamBuild.BuildAndroid
using System;
using System.IO;
using System.Linq;
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
        static string OutDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Builds"));

        [MenuItem("Dream/Apply mobile settings", priority = 1)]
        public static void ApplySettings()
        {
            PlayerSettings.companyName = Company;
            PlayerSettings.productName = Product;
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, AppId);

            // portrait only: the whole UI is laid out for a phone held upright
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

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
            AssetDatabase.SaveAssets();
            Debug.Log($"[Dream] Mobile settings applied: {Product} ({AppId}) {Version}, portrait, ARM64, Android 10+, iOS 15+");
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

        [MenuItem("Dream/Build Android APK", priority = 20)]
        public static void BuildAndroid() => Build(BuildTarget.Android, false);

        [MenuItem("Dream/Build Android APK (dev, with logs)", priority = 21)]
        public static void BuildAndroidDev() => Build(BuildTarget.Android, true);

        [MenuItem("Dream/Build iOS Xcode project", priority = 22)]
        public static void BuildIOS() => Build(BuildTarget.iOS, false);

        [MenuItem("Dream/Open builds folder", priority = 40)]
        public static void OpenBuilds() { Directory.CreateDirectory(OutDir); EditorUtility.RevealInFinder(OutDir + Path.DirectorySeparatorChar); }

        static void Build(BuildTarget target, bool dev)
        {
            var group = BuildPipeline.GetBuildTargetGroup(target);
            string name = target == BuildTarget.Android ? "Android" : "iOS";
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

            Directory.CreateDirectory(OutDir);
            string path = target == BuildTarget.Android ? Path.Combine(OutDir, $"TheDream-{Version}{(dev ? "-dev" : "")}.apk") : Path.Combine(OutDir, "TheDream-iOS");
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) scenes = new[] { "Assets/Scenes/SampleScene.unity" };
            var opts = new BuildPlayerOptions
            {
                scenes = scenes, locationPathName = path, target = target, targetGroup = group,
                options = dev ? BuildOptions.Development | BuildOptions.AllowDebugging : BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var sum = report.summary;
            if (sum.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Dream] Build succeeded: {path} ({sum.totalSize / 1048576.0:0.0} MB in {sum.totalTime.TotalMinutes:0.0} min)");
                if (!Application.isBatchMode) EditorUtility.RevealInFinder(path);
            }
            else Debug.LogError($"[Dream] {name} build {sum.result} with {sum.totalErrors} error(s). The details are in the Console above this line.");
        }
    }
}
