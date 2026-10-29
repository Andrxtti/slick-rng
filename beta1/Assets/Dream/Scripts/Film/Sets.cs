using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dream.Film
{
    // Film sets: made in Blender by tools/blender/<place>.py, exported to Assets/Dream/FilmSets/<place>/<place>.gltf
    // (generated, outside git, editor-only: films are rendered here and shipped as video). The Blender empties carry
    // names Unity acts on: L_<kind>_n becomes a light from the set's look, MARK_<who> is where a person sits or
    // stands, CAM_<shot> is a fixed camera spot.
    public static class Sets
    {
        // A light per mark prefix: colour, intensity, range, spot angle (0 = point), shadows.
        public struct Lamp
        {
            public Color c; public float i, range, angle; public bool shadow; public Vector3 aim;
            public Lamp(Color c, float i, float range, float angle = 0, bool shadow = false, Vector3 aim = default) { this.c = c; this.i = i; this.range = range; this.angle = angle; this.shadow = shadow; this.aim = aim; }
        }

        public class Look
        {
            public Dictionary<string, Lamp> lamps = new Dictionary<string, Lamp>();
            public Color ambient = new Color(0.05f, 0.045f, 0.04f);
            public Color fog = new Color(0.03f, 0.03f, 0.05f); public float fogDensity = 0.012f;
            public string sky; public float skyExposure = 0.2f; public Color skyTint = Color.white; public float skyRotation;
            public float exposure, temperature, contrast = 12f, saturation;
            public float key = 1f;   // film key strength in this set (the dark diner needs more than a sunlit office)
        }

        static readonly Color Tungsten = new Color(1f, 0.72f, 0.45f);

        public static readonly Dictionary<string, Look> Looks = new Dictionary<string, Look>
        {
            ["diner"] = new Look
            {
                lamps =
                {
                    ["L_pendant"] = new Lamp(Tungsten, 5.5f, 4.5f, 110, true, Vector3.down),
                    ["L_booth"] = new Lamp(Tungsten, 1.6f, 3.5f),
                    ["L_case"] = new Lamp(new Color(1f, 0.85f, 0.6f), 1.2f, 1.6f),
                    ["L_neon_out"] = new Lamp(new Color(1f, 0.08f, 0.1f), 9f, 11f),
                    ["L_streetlamp"] = new Lamp(new Color(0.55f, 0.7f, 1f), 14f, 18f, 0, true),
                    ["L_back"] = new Lamp(new Color(1f, 0.6f, 0.4f), 0.8f, 6f),
                },
                ambient = new Color(0.035f, 0.03f, 0.035f),
                sky = "Assets/Dream/FilmSets/diner/sky.hdr", skyExposure = 0.08f, skyTint = new Color(0.55f, 0.6f, 1f), skyRotation = 90,
                exposure = 0.55f, temperature = 0f, contrast = 14f, saturation = 4f,
            },
        };

        static Sets()
        {
            Looks["office"] = new Look
            {
                lamps =
                {
                    // angle -1: a directional light, the low sun through the glass
                    ["L_sun"] = new Lamp(new Color(1f, 0.55f, 0.42f), 1.6f, 0, -1, true, new Vector3(0.25f, -0.18f, 1f)),
                    ["L_monitor"] = new Lamp(new Color(0.7f, 0.85f, 1f), 0.5f, 2.2f),
                    ["L_desklamp"] = new Lamp(Tungsten, 0.45f, 2.0f),
                    ["L_exit"] = new Lamp(new Color(0.3f, 1f, 0.45f), 0.5f, 3f),
                },
                ambient = new Color(0.16f, 0.13f, 0.15f), key = 0.35f, exposure = 0.1f,
                fog = new Color(0.5f, 0.35f, 0.35f), fogDensity = 0.004f,
                sky = "Assets/Dream/FilmSets/office/sky.hdr", skyExposure = 0.5f, skyTint = new Color(1f, 0.7f, 0.66f), skyRotation = 0,
            };
        }

        static readonly Look CardRoom = Looks["cardroom"] = new Look
        {
            lamps =
            {
                ["L_table"] = new Lamp(new Color(1f, 0.74f, 0.4f), 7f, 5f, 130, true, Vector3.down),   // sodium over each felt
                ["L_window"] = new Lamp(new Color(0.45f, 0.6f, 1f), 3f, 9f),
                ["L_cage"] = new Lamp(new Color(1f, 0.85f, 0.6f), 1.5f, 4f),
            },
            ambient = new Color(0.22f, 0.17f, 0.12f), fog = new Color(0.12f, 0.09f, 0.05f), fogDensity = 0.01f, key = 1.3f, exposure = 1.5f,
        };

        static Look Reel(Color key, Color rim, float exposure = 0f) => new Look
        {
            lamps = { ["L_key"] = new Lamp(key, 6f, 6f, 45, true, Vector3.zero), ["L_rim"] = new Lamp(rim, 5f, 6f), ["L_spot"] = new Lamp(new Color(1f, 0.8f, 0.5f), 6f, 4f, 50, true, Vector3.down) },
            ambient = new Color(0.02f, 0.02f, 0.025f), fogDensity = 0f, key = 1.2f, exposure = exposure,
        };
        static readonly bool reels = InitReels();
        static bool InitReels()
        {
            Looks["reel_casino"] = Reel(new Color(1f, 0.8f, 0.45f), new Color(0.7f, 0.35f, 1f));
            Looks["reel_vegas"] = Reel(new Color(1f, 0.98f, 0.94f), new Color(1f, 0.85f, 0.6f), 0.2f);
            Looks["reel_tv"] = Reel(new Color(1f, 0.96f, 0.9f), new Color(0.4f, 0.7f, 1f));
            Looks["reel_titan"] = Reel(new Color(0.85f, 0.92f, 1f), new Color(0.3f, 0.5f, 1f), 0.2f);
            Looks["reel_main"] = Reel(new Color(1f, 0.92f, 0.8f), new Color(1f, 0.85f, 0.6f));
            Looks["reel_hall"] = Reel(new Color(1f, 0.82f, 0.5f), new Color(1f, 0.8f, 0.45f));
            return true;
        }

        public static GameObject Load(string place)
        {
#if UNITY_EDITOR
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Dream/FilmSets/{place}/{place}.gltf");
            if (src == null) { Debug.LogWarning($"[Dream] no film set {place}"); return null; }
            var go = Object.Instantiate(src); go.name = "Set_" + place;
            Dress(go, place);
            return go;
#else
            return null;
#endif
        }

        public static Transform Find(GameObject set, string name)
        {
            foreach (var t in set.GetComponentsInChildren<Transform>()) if (t.name == name) return t;
            return null;
        }

        static void Dress(GameObject set, string place)
        {
            Looks.TryGetValue(place, out var look); look ??= new Look();
            foreach (var t in set.GetComponentsInChildren<Transform>())
            {
                if (!t.name.StartsWith("L_")) continue;
                foreach (var kv in look.lamps)
                {
                    if (!t.name.StartsWith(kv.Key)) continue;
                    var p = kv.Value; var l = t.gameObject.AddComponent<Light>();
                    l.type = p.angle < 0 ? LightType.Directional : p.angle > 0 ? LightType.Spot : LightType.Point;
                    if (p.angle < 0)   // a sun: shine from its mark toward the people, whatever axes the export used
                    {
                        var who = Find(set, "MARK_you") ?? set.transform;
                        t.rotation = Quaternion.LookRotation((who.position + Vector3.up * 0.8f - t.position).normalized);
                    }
                    l.color = p.c; l.intensity = p.i; l.range = p.range;
                    if (p.angle > 0)
                    {
                        l.spotAngle = p.angle; l.innerSpotAngle = p.angle * 0.5f;
                        var hero = Find(set, "AIM_hero");   // a vignette's key looks at its hero prop
                        t.rotation = p.aim == default && hero != null ? Quaternion.LookRotation(hero.position - t.position) : Quaternion.LookRotation(p.aim == default ? Vector3.down : p.aim);
                    }
                    l.shadows = p.shadow ? LightShadows.Soft : LightShadows.None;
                    l.shadowBias = 0.02f; l.shadowNormalBias = 0.3f;
                }
            }
            // colliders so the camera can tell when a wall or a desk is between it and a face (Shot.Clear)
            foreach (var mf in set.GetComponentsInChildren<MeshFilter>())
                if (mf.sharedMesh != null && !mf.name.StartsWith("Glass") && !mf.name.StartsWith("Window") && !mf.name.StartsWith("Door"))
                    mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            // glTF emissive strength doesn't survive the import: give glowing surfaces (neon, lamps, screens) HDR again
            foreach (var r in set.GetComponentsInChildren<Renderer>())
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    foreach (var prop in new[] { "emissiveFactor", "_EmissionColor" })
                        if (m.HasProperty(prop)) { var e = m.GetColor(prop); if (e.maxColorComponent > 0.05f && e.maxColorComponent <= 1.01f) m.SetColor(prop, e * 6f); }
                }
            // glTF glass comes in as "blend" alpha; keep it, but make it reflect like glass
            foreach (var r in set.GetComponentsInChildren<Renderer>())
                foreach (var m in r.sharedMaterials)
                    if (m != null && m.name.StartsWith("M_Glass") && m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.98f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = look.ambient;
            RenderSettings.fog = look.fogDensity > 0; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = look.fog; RenderSettings.fogDensity = look.fogDensity;
#if UNITY_EDITOR
            if (look.sky != null)
            {
                var hdr = AssetDatabase.LoadAssetAtPath<Texture>(look.sky);
                if (hdr != null)
                {
                    var sky = new Material(Shader.Find("Skybox/Panoramic"));
                    sky.SetTexture("_MainTex", hdr); sky.SetFloat("_Exposure", look.skyExposure);
                    sky.SetColor("_Tint", look.skyTint); sky.SetFloat("_Rotation", look.skyRotation);
                    RenderSettings.skybox = sky;
                }
            }
#endif
            // one realtime reflection probe, rendered once: chrome and glass pick up the neon and the lamps
            var probe = new GameObject("Reflections").AddComponent<ReflectionProbe>();
            probe.transform.SetParent(set.transform, false);
            var b = new Bounds(set.transform.position, Vector3.zero);
            foreach (var r in set.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            probe.transform.position = b.center; probe.size = b.size; probe.boxProjection = true;
            probe.mode = ReflectionProbeMode.Realtime; probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.resolution = 256; probe.hdr = true; probe.intensity = 1f;
            DynamicGI.UpdateEnvironment();
            probe.RenderProbe();
            Current = look;
        }

        public static Look Current;
    }
}
