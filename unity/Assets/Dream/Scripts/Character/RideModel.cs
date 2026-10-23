// Your ride in 3D. Each ride is a small binary mesh in Resources/Dream/Rides/<id>.bytes, made by the art
// tools (unity/tools/art): a painted body, glass, trim, lights and four wheels with their own pivots so they
// can turn. A pro-art or brand model can replace any ride: put a prefab at Resources/Rides/<id> with wheels
// named "wheel..." and it's used instead.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CFDream.Character
{
    public class RideRig
    {
        public GameObject root;
        public readonly List<Transform> wheels = new List<Transform>();
        public Bounds bounds;          // in the root's space
        public float wheelR = 0.35f;
        public bool plane;
    }

    public static class RideModel
    {
        public static readonly Dictionary<string, string> Paint = new Dictionary<string, string>
        { { "hatch", "#8a8f98" }, { "coupe", "#c0222b" }, { "sedan", "#141b33" }, { "super", "#f08a1c" }, { "bus", "#2f6fb0" }, { "jet", "#ecebef" } };


        public static RideRig Build(string id, Transform parent, int layer, Color? paint = null)
        {
            var rig = new RideRig { plane = id == "jet" };
            var pro = Resources.Load<GameObject>("Rides/" + id);
            if (pro != null)
            {
                rig.root = UnityEngine.Object.Instantiate(pro, parent, false);
                foreach (var t in rig.root.GetComponentsInChildren<Transform>(true))
                    if (t.name.StartsWith("wheel", StringComparison.OrdinalIgnoreCase)) rig.wheels.Add(t);
            }
            else
            {
                var parts = DrmMesh.Load("Dream/Rides/" + id) ?? DrmMesh.Load("Dream/Rides/hatch");
                rig.root = new GameObject("Ride " + id); rig.root.transform.SetParent(parent, false);
                var col = paint ?? Hex(Paint.TryGetValue(id, out var h) ? h : "#8a8f98");
                foreach (var p in parts)
                {
                    var go = new GameObject(p.name); go.transform.SetParent(rig.root.transform, false);
                    go.transform.localPosition = p.pivot;
                    go.AddComponent<MeshFilter>().sharedMesh = p.mesh;
                    var mr = go.AddComponent<MeshRenderer>(); mr.shadowCastingMode = ShadowCastingMode.Off;
                    var ms = new Material[p.mats.Length];
                    for (int i = 0; i < ms.Length; i++) ms[i] = Mat(p.mats[i], col);
                    mr.sharedMaterials = ms;
                    if (p.name.StartsWith("wheel")) { rig.wheels.Add(go.transform); rig.wheelR = p.mesh.bounds.extents.y; }
                }
            }
            foreach (var t in rig.root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            bool first = true;
            foreach (var r in rig.root.GetComponentsInChildren<Renderer>())
            {
                var b = r.bounds; var lb = new Bounds(rig.root.transform.InverseTransformPoint(b.center), b.size);
                if (first) { rig.bounds = lb; first = false; } else rig.bounds.Encapsulate(lb);
            }
            return rig;
        }

        static Material Mat(string name, Color paint)
        {
            switch (name)
            {
                case "paint": return Mats.Get(paint, 0.45f, 0.9f);
                case "glass": return Mats.Get(Hex("#0a0c16"), 0.3f, 0.97f);
                case "pillar": case "trim": case "grille": return Mats.Get(Hex("#0b0b0e"), 0.2f, 0.45f);
                case "tire": return Mats.Get(Hex("#111114"), 0f, 0.28f);
                case "rim": return Mats.Metal(Hex("#cfd2da"));
                case "rim_dark": return Mats.Get(Hex("#1d1d22"), 0.7f, 0.6f);
                case "light_f": return Mats.Get(Hex("#fff4dc"), 0f, 0.9f, 5f);
                case "light_r": return Mats.Get(Hex("#ff1430"), 0f, 0.8f, 5f);
                case "stripe": return Mats.Metal(Hex("#d9a54a"));
                case "engine": return Mats.Metal(Hex("#b9bcc6"));
                default: return Mats.Plastic(Hex("#808080"));
            }
        }

        static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.gray;
    }
}
