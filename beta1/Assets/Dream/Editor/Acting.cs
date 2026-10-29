// Mixamo acting clips (copied from the old project's Anim/Mixamo into Assets/Dream/FilmAnim, outside git) imported as
// humanoid clips named after the file; looping ones loop. Seated clips keep their own hip height so they sit, standing
// ones stand on their feet. "acting-build" makes FilmAnim/Acting.controller with one state per clip.
// Ported from the old project's Editor/DreamAnim.cs (same settings, proven there).
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Dream.EditorTools
{
    [InitializeOnLoad]
    public class Acting : AssetPostprocessor
    {
        public override uint GetVersion() => 1;
        public const string Folder = "Assets/Dream/FilmAnim";
        public const string ControllerPath = Folder + "/Acting.controller";
        static readonly string[] Loops = { "Idle", "Breathing Idle", "Talking", "Standing Arguing", "Thinking", "Looking Around", "Sad Idle", "Sitting Idle", "Sitting Talking", "Walking", "Texting While Standing" };
        static bool Mine(string path) => path.StartsWith(Folder) && path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase);

        static Acting() { Remote.Extra["acting-build"] = _ => Build(); }

        void OnPreprocessModel()
        {
            if (!Mine(assetPath)) return;
            var mi = (ModelImporter)assetImporter;
            mi.animationType = ModelImporterAnimationType.Human; mi.importAnimation = true;
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
                bool seated = name.StartsWith("Sitting") || name == "Sit To Stand" || name == "Stand To Sit";
                c.keepOriginalOrientation = true; c.keepOriginalPositionXZ = true;
                c.keepOriginalPositionY = seated; c.heightFromFeet = !seated; c.heightOffset = 0;
            }
            mi.clipAnimations = clips;
        }

        public static void Build()
        {
            var clips = new List<AnimationClip>();
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Folder }))
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
                    if (o is AnimationClip ac && !ac.name.StartsWith("__preview__")) { clips.Add(ac); break; }
            if (clips.Count == 0) { Remote.Log("acting-build: no clips in " + Folder); return; }
            AssetDatabase.DeleteAsset(ControllerPath);
            var c = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var sm = c.layers[0].stateMachine; AnimatorState first = null;
            foreach (var clip in clips)
            {
                var st = sm.AddState(clip.name); st.motion = clip;
                if (clip.name == "Sitting Idle" || first == null) first = st;
            }
            sm.defaultState = first;
            EditorUtility.SetDirty(c); AssetDatabase.SaveAssets();
            Remote.Log($"acting-build: {clips.Count} clips -> {ControllerPath}");
        }
    }

    // Characters from tools/blender/character.py: humanoid avatar from the model, blend shapes kept, materials built
    // at run time from the character's manifest.json (Film/Cast.cs).
    public class CastImport : AssetPostprocessor
    {
        public override uint GetVersion() => 1;
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/Dream/FilmCast/")) return;
            var mi = (ModelImporter)assetImporter;
            mi.animationType = ModelImporterAnimationType.Human; mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.importBlendShapes = true; mi.importAnimation = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.importNormals = ModelImporterNormals.Import; mi.importBlendShapeNormals = ModelImporterNormals.Calculate;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Dream/FilmCast/")) return;
            var ti = (TextureImporter)assetImporter;
            if (assetPath.Contains("_normalmap")) ti.textureType = TextureImporterType.NormalMap;
            else ti.alphaIsTransparency = true;
            ti.maxTextureSize = 4096;
        }
    }
}
