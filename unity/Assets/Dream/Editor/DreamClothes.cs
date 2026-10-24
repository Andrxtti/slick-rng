// Turns the painted closet clothes (unity/tools/art/clothes.py -> Assets/Dream/Clothes/<slot>_<id>.png plus
// clothes.json) into UMA wearables: for each item, a copy of the base garment's overlay with our diffuse in the
// first texture slot, and a copy of the base wardrobe recipe that points at that overlay. Assets land in
// Assets/Dream/Clothes/UMA and the UMA library is told about them, so UMABody can dress by recipe name
// "dream_<slot>_<id>". Re-running updates in place.
#if DREAM_UMA
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UMA;
using UMA.CharacterSystem;
using UnityEditor;
using UnityEngine;

namespace CFDream.EditorTools
{
    public static class DreamClothes
    {
        const string Src = "Assets/Dream/Clothes", Dst = "Assets/Dream/Clothes/UMA";

        [MenuItem("Dream/3D characters/Build closet clothes", priority = 10)]
        public static void Build()
        {
            var idx = UMAAssetIndexer.Instance; if (idx == null) { Debug.LogError("[Dream] UMA's asset indexer is missing"); return; }
            var metaTa = AssetDatabase.LoadAssetAtPath<TextAsset>(Src + "/clothes.json");
            if (metaTa == null) { Debug.LogError("[Dream] " + Src + "/clothes.json not found: run unity/tools/art/clothes.py"); return; }
            var meta = Json.Parse(metaTa.text) as Dictionary<string, object>;
            if (!AssetDatabase.IsValidFolder(Dst)) AssetDatabase.CreateFolder(Src, "UMA");
            int made = 0, skipped = 0; var log = new System.Text.StringBuilder();
            foreach (var kv in meta)
            {
                string key = kv.Key; var m = kv.Value as Dictionary<string, object>; if (m == null) continue;
                string slot = key.Split(':')[0], id = key.Split(':')[1], name = "dream_" + slot + "_" + id;
                string texPath = $"{Src}/{slot}_{id}.png";
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                if (tex == null) { skipped++; continue; }
                string baseRecipeName = J.S(m, "recipe"), baseSlotName = J.S(m, "slot");
                var baseRecipe = idx.GetAssetItem<UMAWardrobeRecipe>(baseRecipeName)?.Item as UMAWardrobeRecipe ?? FindAsset<UMAWardrobeRecipe>(baseRecipeName);
                if (baseRecipe == null) { log.AppendLine($"{key}: base recipe {baseRecipeName} not found"); skipped++; continue; }
                // the overlay the base uses on its slot
                string baseOverlayName = BaseOverlay(baseRecipe.recipeString, baseSlotName) ?? BaseOverlay(baseRecipe.recipeString, baseSlotName + "_slot");
                if (string.IsNullOrEmpty(baseOverlayName)) { log.AppendLine($"{key}: no overlay on slot {baseSlotName} in {baseRecipeName}"); skipped++; continue; }
                var baseOverlay = idx.GetAssetItem<OverlayDataAsset>(baseOverlayName)?.Item as OverlayDataAsset ?? FindAsset<OverlayDataAsset>(baseOverlayName);
                if (baseOverlay == null) { log.AppendLine($"{key}: base overlay {baseOverlayName} not found"); skipped++; continue; }

                // our overlay: the base's textures with our diffuse first
                string ovPath = $"{Dst}/{name}_overlay.asset";
                var ov = AssetDatabase.LoadAssetAtPath<OverlayDataAsset>(ovPath);
                if (ov == null) { ov = Object.Instantiate(baseOverlay); AssetDatabase.CreateAsset(ov, ovPath); }
                ov.name = name; ov._oldOverlayName = name; ov.material = baseOverlay.material; ov.alphaMask = null;
                var texs = (Texture[])baseOverlay.textureList.Clone(); if (texs.Length == 0) texs = new Texture[1];
                texs[0] = tex; ov.textureList = texs;
                ov.textureNames = texs.Select(t => t != null ? t.name : "").ToArray();
                EditorUtility.SetDirty(ov);

                // our recipe: the base's, pointing at our overlay
                string rcPath = $"{Dst}/{name}_Recipe.asset";
                var rc = AssetDatabase.LoadAssetAtPath<UMAWardrobeRecipe>(rcPath);
                if (rc == null) { rc = Object.Instantiate(baseRecipe); AssetDatabase.CreateAsset(rc, rcPath); }
                rc.name = name; rc.DisplayValue = id;
                rc.recipeString = baseRecipe.recipeString.Replace("\"id\":\"" + baseOverlayName + "\"", "\"id\":\"" + name + "\"");
                rc.wardrobeRecipeThumbs = new List<WardrobeRecipeThumb>();
                EditorUtility.SetDirty(rc);
                made++;
            }
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            // tell UMA's library about them
            foreach (var p in Directory.GetFiles(Dst, "*.asset"))
            {
                var o = AssetDatabase.LoadMainAssetAtPath(p.Replace('\\', '/'));
                if (o is OverlayDataAsset || o is UMAWardrobeRecipe) idx.EvilAddAsset(o.GetType(), o);
            }
            idx.ForceSave();
            Debug.Log($"[Dream] closet clothes: {made} built, {skipped} skipped\n{log}");
        }

        static string BaseOverlay(string recipeString, string slotName)
        {
            int at = recipeString.IndexOf("\"id\":\"" + slotName + "\"");
            if (at < 0) return null;
            int o = recipeString.IndexOf("\"overlays\":[{\"id\":\"", at); if (o < 0) return null;
            o += "\"overlays\":[{\"id\":\"".Length; int e = recipeString.IndexOf('"', o);
            return recipeString.Substring(o, e - o);
        }
        static T FindAsset<T>(string name) where T : Object
        {
            foreach (var g in AssetDatabase.FindAssets(name + " t:" + typeof(T).Name))
            {
                var a = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)); if (a != null && a.name == name) return a;
            }
            return null;
        }
    }
}
#endif
