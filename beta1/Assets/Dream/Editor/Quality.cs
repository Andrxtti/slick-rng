// Film quality (DAY2 priority 3): the editor pipeline that renders the films gets screen-space ambient occlusion on top
// of its 4K soft shadows; the phone pipeline (Build.cs, URP_Mobile) gets its own renderer without it, since the live
// game is UI. Safe to re-run.   remote: film-quality
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Dream.EditorTools
{
    [InitializeOnLoad]
    public static class Quality
    {
        const string Dir = "Assets/Dream/Settings";
        static Quality() { Remote.Extra["film-quality"] = _ => Run(); }

        public static void Run()
        {
            // the phone renderer: a copy of the film renderer taken before SSAO goes on, used by URP_Mobile
            if (AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Dir + "/URP_Renderer_Mobile.asset") == null)
                AssetDatabase.CopyAsset(Dir + "/URP_Renderer.asset", Dir + "/URP_Renderer_Mobile.asset");
            var phoneR = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Dir + "/URP_Renderer_Mobile.asset");
            foreach (var f in phoneR.rendererFeatures.Where(f => f != null && f.GetType().Name == "ScreenSpaceAmbientOcclusion").ToList())
            { phoneR.rendererFeatures.Remove(f); AssetDatabase.RemoveObjectFromAsset(f); }
            EditorUtility.SetDirty(phoneR);
            var mobile = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Dir + "/URP_Mobile.asset");
            if (mobile != null)
            {
                var so = new SerializedObject(mobile);
                so.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue = phoneR;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // the film renderer: SSAO (the type is internal to URP, so it is made by name)
            var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Dir + "/URP_Renderer.asset");
            var type = Type.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion, Unity.RenderPipelines.Universal.Runtime");
            var ssao = rd.rendererFeatures.FirstOrDefault(f => f != null && f.GetType() == type);
            if (ssao == null)
            {
                ssao = (ScriptableRendererFeature)ScriptableObject.CreateInstance(type); ssao.name = "SSAO";
                AssetDatabase.AddObjectToAsset(ssao, rd); rd.rendererFeatures.Add(ssao);
                EditorUtility.SetDirty(rd); AssetDatabase.SaveAssets();
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ssao, out string _, out long local);   // the renderer's feature map holds file ids
                var rso = new SerializedObject(rd); var map = rso.FindProperty("m_RendererFeatureMap");
                map.arraySize = rd.rendererFeatures.Count; map.GetArrayElementAtIndex(rd.rendererFeatures.Count - 1).longValue = local;
                rso.ApplyModifiedPropertiesWithoutUndo();
            }
            var s = new SerializedObject(ssao);
            void F(string k, float v) { var p = s.FindProperty("m_Settings." + k); if (p != null) p.floatValue = v; }
            void I(string k, int v) { var p = s.FindProperty("m_Settings." + k); if (p != null) { if (p.propertyType == SerializedPropertyType.Enum) p.enumValueIndex = v; else if (p.propertyType == SerializedPropertyType.Boolean) p.boolValue = v != 0; else p.intValue = v; } }
            F("Intensity", 1.4f); F("Radius", 0.28f); F("DirectLightingStrength", 0.35f); F("Falloff", 40f);
            I("Downsample", 0); I("AfterOpaque", 0); I("Source", 1); I("NormalSamples", 1); I("AOMethod", 1); I("BlurQuality", 0); I("Samples", 0);
            s.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rd); AssetDatabase.SaveAssets();
            Pores();
            Remote.Log($"film-quality: SSAO on the film renderer ({rd.rendererFeatures.Count} features); phone renderer without it; pore map");
        }

        // FilmData/Tex/pores_n.png: a tileable normal map of skin pores and fine creases for Dream/Skin (made here, CC0:
        // ours). Height = small round dimples scattered at random (wrapping at the edges) plus faint cross-hatched micro
        // lines; the normal is its gradient.
        public const string PorePath = "Assets/Dream/FilmData/Tex/pores_n.png";
        static void Pores()
        {
            const int N = 512; var h = new float[N * N]; var rng = new System.Random(7);
            for (int k = 0; k < 2600; k++)
            {
                int cx = rng.Next(N), cy = rng.Next(N); float r = 1.2f + (float)rng.NextDouble() * 1.8f, d = 0.6f + (float)rng.NextDouble() * 0.4f;
                int R = (int)Mathf.Ceil(r * 2.5f);
                for (int y = -R; y <= R; y++) for (int x = -R; x <= R; x++)
                {
                    float q = (x * x + y * y) / (r * r); if (q > 6) continue;
                    h[((cy + y + N) % N) * N + (cx + x + N) % N] -= d * Mathf.Exp(-q);
                }
            }
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                h[y * N + x] += 0.08f * Mathf.Sin((x + y) * 0.9f + Mathf.Sin(y * 0.05f) * 3f) * Mathf.Sin((x - y) * 0.7f) + 0.05f * (float)(rng.NextDouble() - 0.5);
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float dx = h[y * N + (x + 1) % N] - h[y * N + (x - 1 + N) % N], dy = h[((y + 1) % N) * N + x] - h[((y - 1 + N) % N) * N + x];
                var n = new Vector3(-dx * 1.5f, -dy * 1.5f, 1).normalized;
                px[y * N + x] = new Color32((byte)((n.x * 0.5f + 0.5f) * 255), (byte)((n.y * 0.5f + 0.5f) * 255), (byte)((n.z * 0.5f + 0.5f) * 255), 255);
            }
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false); tex.SetPixels32(px); tex.Apply();
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PorePath));
            System.IO.File.WriteAllBytes(PorePath, tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(PorePath);
            var imp = (TextureImporter)AssetImporter.GetAtPath(PorePath);
            imp.textureType = TextureImporterType.NormalMap; imp.wrapMode = TextureWrapMode.Repeat; imp.mipmapEnabled = true; imp.SaveAndReimport();
        }
    }
}
