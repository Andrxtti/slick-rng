// One-time project setup, safe to re-run: URP pipeline asset, linear colour, the film test scene.
//   batch:  Unity.exe -batchmode -projectPath beta1 -executeMethod Dream.EditorTools.Setup.Run -quit
//   remote: setup
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Dream.EditorTools
{
    public static class Setup
    {
        const string Dir = "Assets/Dream/Settings";

        public static void Run()
        {
            Directory.CreateDirectory(Dir);
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Dir + "/URP.asset");
            if (asset == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, Dir + "/URP_Renderer.asset");
                asset = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(asset, Dir + "/URP.asset");
            }
            asset.msaaSampleCount = 4;
            // Forward+: no per-object light cap (the diner alone has a dozen practicals plus the film key and fill)
            var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Dir + "/URP_Renderer.asset");
            if (rd != null) { rd.renderingMode = RenderingMode.ForwardPlus; EditorUtility.SetDirty(rd); }
            var so = new SerializedObject(asset);
            foreach (var (k, v) in new[] { ("m_MainLightShadowmapResolution", 4096), ("m_AdditionalLightsShadowmapResolution", 4096), ("m_ShadowCascadeCount", 4), ("m_SoftShadowQuality", 3) })
            { var pr = so.FindProperty(k); if (pr != null) pr.intValue = v; }
            var adds = so.FindProperty("m_AdditionalLightShadowsSupported"); if (adds != null) adds.boolValue = true;
            var soft = so.FindProperty("m_SoftShadowsSupported"); if (soft != null) soft.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            asset.supportsHDR = true;
            asset.shadowDistance = 30f;
            EditorUtility.SetDirty(asset);
            GraphicsSettings.defaultRenderPipeline = asset;
            for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = asset; }
            PlayerSettings.colorSpace = ColorSpace.Linear;
            // no domain reload on Play: faster, and the render queue (static) survives entering Play mode
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
            PlayerSettings.companyName = "Confirmed Fire"; PlayerSettings.productName = "The Dream";
            AssetDatabase.SaveAssets();
            TestScene();
            FilmScene();
            Debug.Log("[Dream] setup done");
        }

        // The film stage: an empty scene with the player; the render command opens it and sets Take.Id.
        static void FilmScene()
        {
            const string path = "Assets/Dream/Scenes/Film.unity";
            if (File.Exists(path)) return;
            var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Film").AddComponent<Dream.Film.Player>();
            EditorSceneManager.SaveScene(s, path);
        }

        // A lit test scene: floor, sphere, warm key, cool rim, a slowly orbiting camera.
        static void TestScene()
        {
            const string path = "Assets/Dream/Scenes/Test.unity";
            if (File.Exists(path)) return;
            var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var lit = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); floor.transform.localScale = Vector3.one * 2;
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere); ball.transform.position = Vector3.up * 0.5f;
            var m = new Material(lit) { color = new Color(0.8f, 0.15f, 0.15f) }; m.SetFloat("_Smoothness", 0.8f);
            AssetDatabase.CreateAsset(m, Dir + "/TestRed.mat"); ball.GetComponent<Renderer>().sharedMaterial = m;
            var key = new GameObject("Key").AddComponent<Light>(); key.type = LightType.Directional; key.color = new Color(1f, 0.78f, 0.55f);
            key.intensity = 1.6f; key.shadows = LightShadows.Soft; key.transform.rotation = Quaternion.Euler(40, -30, 0);
            var rim = new GameObject("Rim").AddComponent<Light>(); rim.type = LightType.Point; rim.color = new Color(0.3f, 0.8f, 1f);
            rim.intensity = 4f; rim.range = 6f; rim.transform.position = new Vector3(-1.5f, 1.5f, 1.5f);
            var cam = new GameObject("Camera").AddComponent<Camera>(); cam.tag = "MainCamera";
            cam.transform.position = new Vector3(0, 1.2f, -3); cam.gameObject.AddComponent<Orbit>();
            EditorSceneManager.SaveScene(s, path);
        }
    }
}
