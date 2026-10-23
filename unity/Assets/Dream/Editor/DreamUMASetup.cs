// Turns the UMA body on when UMA is in the project: sets the DREAM_UMA define on every platform (so the game
// still compiles without UMA) and makes Resources/Dream/UMARefs, which points at UMA's character prefab and a
// standing idle. Runs by itself after scripts load; also Dream > Set up 3D characters.
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace CFDream.EditorTools
{
    [InitializeOnLoad]
    public static class DreamUMASetup
    {
        const string Define = "DREAM_UMA", RefsPath = "Assets/Dream/Resources/Dream/UMARefs.asset";
        const string AvatarPrefab = "Assets/UMA/Core/Defaults/UMADynamicCharacterAvatar.prefab", Idle = "Assets/UMA/UMA3/Animation/IdleController.controller";
        static readonly NamedBuildTarget[] Targets = { NamedBuildTarget.Standalone, NamedBuildTarget.WebGL, NamedBuildTarget.iOS, NamedBuildTarget.Android };

        static DreamUMASetup() { EditorApplication.delayCall += () => Sync(false); }

        static bool UmaPresent() => AppDomain.CurrentDomain.GetAssemblies().Any(a => { try { return a.GetType("UMA.CharacterSystem.DynamicCharacterAvatar", false) != null; } catch { return false; } });

        [MenuItem("Dream/Set up 3D characters", priority = 3)]
        public static void Setup() => Sync(true);

        static void Sync(bool log)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            bool has = UmaPresent();
            foreach (var t in Targets)
            {
                string cur;
                try { cur = PlayerSettings.GetScriptingDefineSymbols(t); } catch { continue; }
                var list = cur.Split(';').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
                bool on = list.Contains(Define);
                if (has == on) continue;
                if (has) list.Add(Define); else list.Remove(Define);
                PlayerSettings.SetScriptingDefineSymbols(t, string.Join(";", list));
            }
            if (has) EnsureRefs(log);
            else if (log) Debug.LogWarning("[Dream] UMA isn't in the project. Import UMA 3 from the Package Manager (My Assets) first.");
        }

        static void EnsureRefs(bool log)
        {
            var refs = AssetDatabase.LoadAssetAtPath<CFDream.Character.DreamUMARefs>(RefsPath);
            bool fresh = refs == null, changed = fresh;
            if (fresh) refs = ScriptableObject.CreateInstance<CFDream.Character.DreamUMARefs>();
            if (refs.avatar == null) { refs.avatar = AssetDatabase.LoadAssetAtPath<GameObject>(AvatarPrefab); changed |= refs.avatar != null; }
            if (refs.idle == null) { refs.idle = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Idle); changed |= refs.idle != null; }
            if (fresh) AssetDatabase.CreateAsset(refs, RefsPath);
            if (changed) { EditorUtility.SetDirty(refs); AssetDatabase.SaveAssets(); }
            if (log || fresh) Debug.Log($"[Dream] 3D characters set up: UMA avatar {(refs.avatar != null ? "found" : "MISSING")}, idle {(refs.idle != null ? "found" : "MISSING")}");
        }
    }
}
