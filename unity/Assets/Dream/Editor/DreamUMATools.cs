// Checks on the UMA install, written to Logs/dream_uma.txt so they can be read without the Console:
//   Dream > 3D characters > Diagnose       what UMA's library holds (races, base recipes, wardrobe for our race)
//   Dream > 3D characters > Rebuild UMA library   re-indexes UMA's content for this project, then diagnoses
#if DREAM_UMA
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
