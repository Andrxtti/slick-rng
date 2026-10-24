// Checks on the UMA install, written to Logs/dream_uma.txt so they can be read without the Console:
//   Dream > 3D characters > Diagnose       what UMA's library holds (races, base recipes, wardrobe for our race)
//   Dream > 3D characters > Rebuild UMA library   re-indexes UMA's content for this project, then diagnoses
//   Dream > 3D characters > Trim library ...    keeps only the race and wardrobe the game uses, so builds (the web
//                                                one above all) don't carry every race and costume UMA ships with.
//                                                Rebuild UMA library undoes it.
#if DREAM_UMA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UMA;
using UMA.CharacterSystem;
using UnityEditor;
using UnityEngine;

namespace CFDream.EditorTools
{
    public static class DreamUMATools
    {
        const string LogPath = "Logs/dream_uma.txt";

        [MenuItem("Dream/3D characters/Diagnose", priority = 4)]
        public static void Diagnose()
        {
            var sb = new StringBuilder();
            var idx = UMAAssetIndexer.Instance;
            sb.AppendLine("indexer: " + (idx != null ? "ok" : "MISSING"));
            if (idx != null)
            {
                foreach (var r in idx.GetAllAssets<RaceData>())
                    if (r != null) sb.AppendLine($"race asset={r.name} raceName={r.raceName} base={(r.baseRaceRecipe != null ? r.baseRaceRecipe.name : "NULL")} wardrobeSlots={(r.wardrobeSlots != null ? string.Join(",", r.wardrobeSlots) : "")}");
                var male = idx.GetRace("HumanMale30");
                sb.AppendLine("GetRace(HumanMale30): " + (male != null ? male.name : "NULL"));
                var recipes = idx.GetAllAssets<UMAWardrobeRecipe>();
                sb.AppendLine($"wardrobe recipes: {recipes.Count}");
                foreach (var g in recipes.Where(x => x != null).GroupBy(x => x.wardrobeSlot).OrderBy(g => g.Key))
                    sb.AppendLine($"  {g.Key}: {string.Join(", ", g.Select(x => x.name + (x.compatibleRaces != null && x.compatibleRaces.Count > 0 ? "[" + string.Join("/", x.compatibleRaces) + "]" : "")).Take(60))}");
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText(LogPath, sb.ToString());
            Debug.Log("[Dream] UMA diagnosis written to " + LogPath);
        }

        // the bodies the game uses, and wardrobe it never asks for even when the race could wear it
        static readonly string[] KeepRaces = { "Human Male 3.0" };
        static readonly string[] DropWardrobe = { "Challenger", "Freebie", "Anime", "Pigtail", "Tornado", "bb_female", "Wrapped", "underwear", "underpants", "tights", "swimwear", "tanktop", "Robe" };
        static readonly Type[] Trimmable = { typeof(RaceData), typeof(UMAWardrobeRecipe), typeof(UMATextRecipe), typeof(UMAWardrobeCollection), typeof(SlotDataAsset), typeof(OverlayDataAsset) };

        [MenuItem("Dream/3D characters/Trim library (report only)", priority = 6)]
        public static void TrimReport() => Trim(false);

        [MenuItem("Dream/3D characters/Trim library to the game's wardrobe", priority = 7)]
        public static void TrimApply() => Trim(true);

        static void Trim(bool apply)
        {
            var idx = UMAAssetIndexer.Instance;
            if (idx == null) { Debug.LogError("[Dream] UMA's asset indexer is missing"); return; }
            var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);       // asset paths
            var keepNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);  // slot and overlay names the kept recipes ask for
            var sb = new StringBuilder();
            void KeepRecipe(UMARecipeBase r)
            {
                if (r == null) return;
                keep.Add(AssetDatabase.GetAssetPath(r));
                if (r is UMATextRecipe tr && !string.IsNullOrEmpty(tr.recipeString))
                    foreach (System.Text.RegularExpressions.Match mt in System.Text.RegularExpressions.Regex.Matches(tr.recipeString, "\"id\":\"([^\"]+)\""))
                        keepNames.Add(mt.Groups[1].Value);
                if (r is UMAPackedRecipeBase pr)
                    foreach (var ai in idx.GetAssetItems(pr, true)) if (ai != null && !string.IsNullOrEmpty(ai._Path)) keep.Add(ai._Path);
            }
            foreach (var r in idx.GetAllAssets<RaceData>())
                if (r != null && KeepRaces.Contains(r.raceName)) { keep.Add(AssetDatabase.GetAssetPath(r)); KeepRecipe(r.baseRaceRecipe); sb.AppendLine("keep race " + r.raceName); }
            // and whatever the avatar prefab always adds (UMA's capsule collider recipe and the like)
            var refs = Resources.Load<CFDream.Character.DreamUMARefs>("Dream/UMARefs");
            var dcaP = refs != null && refs.avatar != null ? refs.avatar.GetComponent<DynamicCharacterAvatar>() : null;
            if (dcaP != null && dcaP.umaAdditionalRecipes != null)
                foreach (var ar in dcaP.umaAdditionalRecipes) { KeepRecipe(ar); if (ar != null) sb.AppendLine("keep avatar recipe " + ar.name); }
            int wardrobe = 0;
            var wanted = CFDream.Character.UMABody.Wanted().Select(n => n.Replace("_Recipe", "")).Distinct().ToList();
            foreach (var w in idx.GetAllAssets<UMAWardrobeRecipe>())
            {
                if (w == null || w.compatibleRaces == null || !w.compatibleRaces.Any(KeepRaces.Contains)) continue;
                if (DropWardrobe.Any(d => w.name.IndexOf(d, StringComparison.OrdinalIgnoreCase) >= 0)) continue;
                if (!wanted.Any(n => w.name.StartsWith(n, StringComparison.OrdinalIgnoreCase))) continue;   // only what the game asks for
                KeepRecipe(w); wardrobe++;
            }
            sb.AppendLine($"keep {wardrobe} wardrobe recipes");

            var remove = idx.SerializedItems.Where(ai => ai != null && Trimmable.Contains(ai._Type) && !keep.Contains(ai._Path ?? "") && !keepNames.Contains(ai._Name ?? "")).ToList();
            sb.AppendLine($"kept by name from recipes: {keepNames.Count}");
            foreach (var g in remove.GroupBy(ai => ai._Type.Name)) sb.AppendLine($"remove {g.Count()} {g.Key}");

            // what that saves: files only the removed items use
            long Size(IEnumerable<string> paths) => paths.Where(File.Exists).Sum(f => new FileInfo(f).Length);
            var keptDeps = new HashSet<string>(AssetDatabase.GetDependencies(keep.Where(x => !string.IsNullOrEmpty(x)).ToArray(), true));
            var goneDeps = new HashSet<string>(AssetDatabase.GetDependencies(remove.Select(ai => ai._Path).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray(), true));
            goneDeps.ExceptWith(keptDeps);
            sb.AppendLine($"kept content {Size(keptDeps) / 1048576f:F0} MB on disk; removed-only content {Size(goneDeps) / 1048576f:F0} MB");

            if (apply)
            {
                foreach (var ai in remove) idx.RemoveAsset(ai, false);
                idx.RemoveAssetsComplete();
                AssetDatabase.SaveAssets();
                sb.AppendLine("applied: library trimmed (Rebuild UMA library restores everything)");
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/dream_trim.txt", sb.ToString());
            Debug.Log("[Dream] UMA trim " + (apply ? "applied" : "report") + " written to Logs/dream_trim.txt");
            if (apply) Diagnose();
        }

        // UMA ships 4K textures; a phone browser needs a fraction of that. Caps every texture the trimmed library uses
        // at `size` for the web build only (an import override, so desktop and source files keep full resolution).
        [MenuItem("Dream/3D characters/Shrink UMA textures for web", priority = 8)]
        public static void ShrinkForWeb() { Shrink("WebGL", 1024, null); Shrink("WebGL", 512, Detail); }
        // normal, gloss, AO and detail maps carry less you can see: they go smaller
        static readonly string[] Detail = { "_NM", "_SG", "Normal", "Gloss", "Detail", "_AO", ".AO", "_N_", "Mask" };

        // every asset UMA's library will put in a build
        public static string[] LibraryPaths()
        {
            var idx = UMAAssetIndexer.Instance; if (idx == null) return new string[0];
            return idx.SerializedItems.Where(ai => ai != null && !string.IsNullOrEmpty(ai._Path)).Select(ai => ai._Path).Distinct().ToArray();
        }

        static void Shrink(string platform, int size, string[] only)
        {
            var idx = UMAAssetIndexer.Instance; if (idx == null) return;
            var paths = LibraryPaths();
            var deps = AssetDatabase.GetDependencies(paths, true).Where(p => p.StartsWith("Assets/UMA")).ToList();
            int changed = 0, seen = 0; long before = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var p in deps)
                {
                    if (!(AssetImporter.GetAtPath(p) is TextureImporter ti)) continue;
                    if (only != null && !only.Any(k => Path.GetFileName(p).IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)) continue;
                    seen++;
                    var ps = ti.GetPlatformTextureSettings(platform);
                    if (ps.overridden && ps.maxTextureSize <= size) continue;
                    before += File.Exists(p) ? new FileInfo(p).Length : 0;
                    ps.overridden = true; ps.maxTextureSize = size;
                    ps.compressionQuality = 50;
                    ti.SetPlatformTextureSettings(ps); EditorUtility.SetDirty(ti); ti.SaveAndReimport(); changed++;
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            var msg = $"{platform}: {seen} UMA textures in use, {changed} capped at {size}px";
            File.AppendAllText("Logs/dream_trim.txt", msg + "\n");
            Debug.Log("[Dream] " + msg);
        }

        [MenuItem("Dream/3D characters/Rebuild UMA library", priority = 5)]
        public static void Rebuild()
        {
            var idx = UMAAssetIndexer.Instance;
            if (idx == null) { Debug.LogError("[Dream] UMA's asset indexer is missing"); return; }
            idx.RebuildLibrary();
            AssetDatabase.SaveAssets();
            Diagnose();
        }
    }
}
#endif
