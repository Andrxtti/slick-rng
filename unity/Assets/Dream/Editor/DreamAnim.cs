// Mixamo clips -> acting. Drop the FBX files (FBX for Unity, without skin) into Assets/Dream/Anim/Mixamo and they are
// imported as humanoid animations named after the file, the looping ones set to loop. "Dream > Build acting set"
// (remote command "acting-build") then makes Resources/Dream/Acting.controller with one state per clip, which the
// character body plays by name (UMABody.Act).
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CFDream.EditorTools
{
    public class DreamAnim : AssetPostprocessor
    {
        public const string Folder = "Assets/Dream/Anim/Mixamo";
        const string ControllerPath = "Assets/Dream/Resources/Dream/Acting.controller";
        // everything else plays once and holds
        static readonly string[] Loops = { "Idle", "Breathing Idle", "Talking", "Standing Arguing", "Thinking", "Looking Around", "Sad Idle", "Sitting Idle", "Sitting Talking", "Walking", "Texting While Standing" };
        static bool Mine(string path) => path.StartsWith(Folder) && path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase);

        void OnPreprocessModel()
        {
            if (!Mine(assetPath)) return;
            var mi = (ModelImporter)assetImporter;
            mi.animationType = ModelImporterAnimationType.Human;
            mi.importAnimation = true;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
        }

        void OnPreprocessAnimation()
        {
            if (!Mine(assetPath)) return;
            var mi = (ModelImporter)assetImporter;
            string name = Path.GetFileNameWithoutExtension(assetPath);
            bool loop = System.Array.IndexOf(Loops, name) >= 0;
            var clips = mi.defaultClipAnimations;
            if (clips == null || clips.Length == 0) return;
            foreach (var c in clips)
            {
                c.name = name; c.loopTime = loop; c.loopPose = loop;
                c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true;
                c.keepOriginalOrientation = true; c.keepOriginalPositionY = true; c.keepOriginalPositionXZ = true;
            }
            mi.clipAnimations = clips;
        }

        [MenuItem("Dream/Build acting set")]
        public static void Build()
        {
            var clips = new List<AnimationClip>();
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (o is AnimationClip ac && !ac.name.StartsWith("__preview__")) { clips.Add(ac); break; }
            }
            if (clips.Count == 0) { Debug.LogWarning("[Dream] no Mixamo clips found in " + Folder); return; }
            Directory.CreateDirectory(Path.GetDirectoryName(ControllerPath));
            AssetDatabase.DeleteAsset(ControllerPath);
            var c = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var sm = c.layers[0].stateMachine;
            AnimatorState first = null;
            foreach (var clip in clips)
            {
                var st = sm.AddState(clip.name); st.motion = clip;
                if (clip.name == "Breathing Idle" || first == null) first = st;
            }
            sm.defaultState = first;
            EditorUtility.SetDirty(c); AssetDatabase.SaveAssets();
            Debug.Log($"[Dream] acting set built from {clips.Count} clips ({ControllerPath})");
        }
    }
}
