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
            var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);   // asset paths
            var sb = new StringBuilder();
            void KeepRecipe(UMARecipeBase r)
            {
                if (r == null) return;
                keep.Add(AssetDatabase.GetAssetPath(r));
                if (r is UMAPackedRecipeBase pr)
                    foreach (var ai in idx.GetAssetItems(pr, true)) if (ai != null && !string.IsNullOrEmpty(ai._Path)) keep.Add(ai._Path);
            }
            foreach (var r in idx.GetAllAssets<RaceData>())
                if (r != null && KeepRaces.Contains(r.raceName)) { keep.Add(AssetDatabase.GetAssetPath(r)); KeepRecipe(r.baseRaceRecipe); sb.AppendLine("keep race " + r.raceName); }
            int wardrobe = 0;
            foreach (var w in idx.GetAllAssets<UMAWardrobeRecipe>())
            {
                if (w == null || w.compatibleRaces == null || !w.compatibleRaces.Any(KeepRaces.Contains)) continue;
                if (DropWardrobe.Any(d => w.name.IndexOf(d, StringComparison.OrdinalIgnoreCase) >= 0)) continue;
                KeepRecipe(w); wardrobe++;
            }
            sb.AppendLine($"keep {wardrobe} wardrobe recipes");

            var remove = idx.SerializedItems.Where(ai => ai != null && Trimmable.Contains(ai._Type) && !keep.Contains(ai._Path ?? "")).ToList();
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
