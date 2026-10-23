// Reads the DRM1 meshes the art tools write (unity/tools/art): named parts, each with a pivot, positions,
// normals and one index list per material. Rides and furniture both use it.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace CFDream.Character
{
    public static class DrmMesh
    {
        public struct Part { public string name; public Vector3 pivot; public Mesh mesh; public string[] mats; }
        static readonly Dictionary<string, Part[]> cache = new Dictionary<string, Part[]>();

        public static Part[] Load(string resource)
        {
            if (cache.TryGetValue(resource, out var c)) return c;
            var ta = Resources.Load<TextAsset>(resource);
            if (ta == null) return null;
            try
            {
                using (var br = new BinaryReader(new MemoryStream(ta.bytes)))
                {
                    if (Encoding.ASCII.GetString(br.ReadBytes(4)) != "DRM1") throw new Exception("not a DRM1 mesh");
                    int n = br.ReadInt32(); var parts = new Part[n];
                    for (int k = 0; k < n; k++)
                    {
                        var p = new Part { name = Str(br), pivot = V3(br) };
                        int vc = br.ReadInt32();
                        var pos = new Vector3[vc]; var nor = new Vector3[vc];
                        for (int i = 0; i < vc; i++) pos[i] = V3(br);
                        for (int i = 0; i < vc; i++) nor[i] = V3(br);
                        int sc = br.ReadInt32(); p.mats = new string[sc];
                        var mesh = new Mesh { name = resource + " " + p.name, indexFormat = vc > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                        mesh.vertices = pos; mesh.normals = nor; mesh.subMeshCount = sc;
                        for (int s = 0; s < sc; s++)
                        {
                            p.mats[s] = Str(br); int ic = br.ReadInt32(); var idx = new int[ic];
                            for (int i = 0; i < ic; i++) idx[i] = br.ReadInt32();
                            mesh.SetTriangles(idx, s, false);
                        }
                        mesh.RecalculateBounds();
                        p.mesh = mesh; parts[k] = p;
                    }
                    cache[resource] = parts; return parts;
                }
            }
            catch (Exception e) { Debug.LogWarning($"[Dream] mesh {resource}: {e.Message}"); return null; }
        }

        // a GameObject per part under `parent`, materials chosen by name
        public static GameObject Spawn(string resource, Transform parent, int layer, Func<string, Material> mat, string name = null)
        {
            var parts = Load(resource); if (parts == null) return null;
            var root = new GameObject(name ?? resource); root.transform.SetParent(parent, false); root.layer = layer;
            foreach (var p in parts)
            {
                var go = new GameObject(p.name); go.layer = layer; go.transform.SetParent(root.transform, false); go.transform.localPosition = p.pivot;
                go.AddComponent<MeshFilter>().sharedMesh = p.mesh;
                var mr = go.AddComponent<MeshRenderer>(); mr.shadowCastingMode = ShadowCastingMode.Off;
                var ms = new Material[p.mats.Length]; for (int i = 0; i < ms.Length; i++) ms[i] = mat(p.mats[i]);
                mr.sharedMaterials = ms;
            }
            return root;
        }

        static string Str(BinaryReader br) { int l = br.ReadInt32(); return Encoding.UTF8.GetString(br.ReadBytes(l)); }
        static Vector3 V3(BinaryReader br) => new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
    }
}
