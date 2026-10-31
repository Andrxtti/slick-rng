// Small mesh builders for the rigid pieces (jewellery, glasses, hats): solids of revolution, tubes along a path,
// spheres and boxes, plus a few lit materials. Everything is generated once and cached, so a rebuild is cheap.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CFDream.Character
{
    public static class MeshKit
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        static Mesh Cached(string key, System.Func<Mesh> make)
        {
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            if (cache.Count > 400) { foreach (var x in cache.Values) if (x != null) Object.Destroy(x); cache.Clear(); }
            m = make(); m.name = key; cache[key] = m; return m;
        }
        static string K(float v) => Mathf.RoundToInt(v * 100000).ToString();

        // A solid of revolution around +Y. Each profile point is (radius, height). Walk the outline the way a
        // pen would trace the right half of the solid's silhouette (bottom centre, out, up, back in to top centre)
        // and the faces point outwards. Repeat a point to make a hard edge there. twoSided for thin sheets (brims, lenses).
        public static Mesh Lathe(string key, IList<Vector2> prof, int seg = 28, bool twoSided = false)
        {
            return Cached("lathe:" + key + ":" + seg + (twoSided ? "2" : "1"), () =>
            {
                var v = new List<Vector3>(); var t = new List<int>(); int n = prof.Count;
                for (int i = 0; i <= seg; i++)
                {
                    float a = i * Mathf.PI * 2 / seg, c = Mathf.Cos(a), s = Mathf.Sin(a);
                    foreach (var p in prof) v.Add(new Vector3(p.x * s, p.y, p.x * c));
                }
                for (int i = 0; i < seg; i++)
                    for (int j = 0; j < n - 1; j++)
                    {
                        int a0 = i * n + j, a1 = a0 + 1, b0 = (i + 1) * n + j, b1 = b0 + 1;
                        t.Add(a0); t.Add(b0); t.Add(a1); t.Add(a1); t.Add(b0); t.Add(b1);
                    }
                return Finish(v, t, twoSided);
            });
        }

        // A tube along a path (closed for loops like chains and bands)
        public static Mesh Tube(string key, IList<Vector3> path, float radius, int sides = 8, bool closed = true)
        {
            return Cached("tube:" + key + ":" + K(radius) + ":" + sides, () =>
            {
                int n = path.Count; var v = new List<Vector3>(); var t = new List<int>();
                Vector3 prevN = Vector3.zero;
                for (int i = 0; i < n; i++)
                {
                    Vector3 a = path[closed ? (i - 1 + n) % n : Mathf.Max(0, i - 1)], b = path[closed ? (i + 1) % n : Mathf.Min(n - 1, i + 1)];
                    Vector3 T = (b - a).normalized; if (T.sqrMagnitude < 1e-8f) T = Vector3.forward;
                    Vector3 N = i == 0 ? Vector3.Cross(T, Mathf.Abs(T.y) < 0.9f ? Vector3.up : Vector3.right).normalized : (prevN - T * Vector3.Dot(prevN, T)).normalized;
                    if (N.sqrMagnitude < 1e-8f) N = Vector3.Cross(T, Vector3.up).normalized;
                    prevN = N; Vector3 B = Vector3.Cross(T, N);
                    for (int k = 0; k < sides; k++)
                    {
                        float th = k * Mathf.PI * 2 / sides;
                        v.Add(path[i] + (N * Mathf.Cos(th) + B * Mathf.Sin(th)) * radius);
                    }
                }
                int rings = closed ? n : n - 1;
                for (int i = 0; i < rings; i++)
                {
                    int i2 = (i + 1) % n;
                    for (int k = 0; k < sides; k++)
                    {
                        int k2 = (k + 1) % sides;
                        int a0 = i * sides + k, a1 = i * sides + k2, b0 = i2 * sides + k, b1 = i2 * sides + k2;
                        t.Add(a0); t.Add(a1); t.Add(b0); t.Add(b0); t.Add(a1); t.Add(b1);
                    }
                }
                if (!closed)
                {
                    // flat end caps
                    for (int e = 0; e < 2; e++)
                    {
                        int ring = e == 0 ? 0 : n - 1; int c = v.Count; v.Add(path[ring]);
                        for (int k = 0; k < sides; k++)
                        {
                            int k2 = (k + 1) % sides;
                            if (e == 0) { t.Add(c); t.Add(ring * sides + k2); t.Add(ring * sides + k); }
                            else { t.Add(c); t.Add(ring * sides + k); t.Add(ring * sides + k2); }
                        }
                    }
                }
                return Finish(v, t, false);
            });
        }

        public static Mesh Sphere(float r, int seg = 14)
        {
            var prof = new List<Vector2>();
            for (int i = 0; i <= seg; i++) { float p = -Mathf.PI / 2 + Mathf.PI * i / seg; prof.Add(new Vector2(Mathf.Cos(p) * r, Mathf.Sin(p) * r)); }
            prof[0] = new Vector2(0, -r); prof[seg] = new Vector2(0, r);
            return Lathe("sphere" + K(r), prof, seg * 2);
        }

        // a short capped cylinder along +Y, hard-edged (a watch case, an ear cup, a stud)
        public static Mesh Disc(float r, float h, int seg = 28, float bevel = 0)
        {
            float b = Mathf.Min(bevel, r * 0.4f, h * 0.4f);
            var prof = b > 0
                ? new[] { new Vector2(0, 0), new Vector2(r - b, 0), new Vector2(r - b, 0), new Vector2(r, b), new Vector2(r, h - b), new Vector2(r - b, h), new Vector2(r - b, h), new Vector2(0, h) }
                : new[] { new Vector2(0, 0), new Vector2(r, 0), new Vector2(r, 0), new Vector2(r, h), new Vector2(r, h), new Vector2(0, h) };
            return Lathe("disc" + K(r) + "x" + K(h) + "b" + K(b), prof, seg);
        }

        // a dome: a sphere cut below the equator at `from` radians (negative goes lower), squashed by `hScale`
        public static Mesh Dome(float r, float hScale, float from, int seg = 32, bool twoSided = true)
        {
            var prof = new List<Vector2>(); int steps = 12;
            for (int i = 0; i <= steps; i++) { float p = Mathf.Lerp(from, Mathf.PI / 2, i / (float)steps); prof.Add(new Vector2(Mathf.Cos(p) * r, Mathf.Sin(p) * r * hScale)); }
            prof[steps] = new Vector2(0, r * hScale);
            return Lathe("dome" + K(r) + "h" + K(hScale) + "f" + K(from), prof, seg, twoSided);
        }

        public static Mesh Box(Vector3 size)
        {
            return Cached("box:" + K(size.x) + K(size.y) + K(size.z), () =>
            {
                var h = size * 0.5f; var v = new List<Vector3>(); var t = new List<int>();
                void Face(Vector3 n, Vector3 u, Vector3 w)
                {
                    int c = v.Count; Vector3 o = Vector3.Scale(n, h), du = Vector3.Scale(u, h), dw = Vector3.Scale(w, h);
                    v.Add(o - du - dw); v.Add(o + du - dw); v.Add(o + du + dw); v.Add(o - du + dw);
                    t.Add(c); t.Add(c + 2); t.Add(c + 1); t.Add(c); t.Add(c + 3); t.Add(c + 2);
                }
                Face(Vector3.forward, Vector3.right, Vector3.up); Face(Vector3.back, Vector3.left, Vector3.up);
                Face(Vector3.right, Vector3.back, Vector3.up); Face(Vector3.left, Vector3.forward, Vector3.up);
                Face(Vector3.up, Vector3.right, Vector3.back); Face(Vector3.down, Vector3.right, Vector3.forward);
                var m = Finish(v, t, false);
                // make sure faces point out whatever the winding above turned out to be
                var nm = m.normals; bool flip = false;
                for (int i = 0; i < 4; i++) if (Vector3.Dot(nm[i], Vector3.forward) < 0) flip = true;
                if (flip) { var tr = m.triangles; for (int i = 0; i < tr.Length; i += 3) { int x = tr[i + 1]; tr[i + 1] = tr[i + 2]; tr[i + 2] = x; } m.triangles = tr; m.RecalculateNormals(); }
                return m;
            });
        }

        // points around an ellipse in the plane spanned by a and b
        public static List<Vector3> Ellipse(Vector3 c, Vector3 a, Vector3 b, float ra, float rb, int n = 40, float from = 0, float to = Mathf.PI * 2)
        {
            var l = new List<Vector3>(n); bool full = Mathf.Abs(to - from - Mathf.PI * 2) < 1e-4f;
            for (int i = 0; i < n; i++) { float t = Mathf.Lerp(from, to, i / (float)(full ? n : n - 1)); l.Add(c + a * (Mathf.Cos(t) * ra) + b * (Mathf.Sin(t) * rb)); }
            return l;
        }

        static Mesh Finish(List<Vector3> v, List<int> t, bool twoSided)
        {
            if (twoSided)
            {
                int n = v.Count; v.AddRange(v.GetRange(0, n)); int tn = t.Count;
                for (int i = 0; i < tn; i += 3) { t.Add(t[i] + n); t.Add(t[i + 2] + n); t.Add(t[i + 1] + n); }
            }
            var m = new Mesh { indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
    }

    // A handful of lit materials, made from two templates in Resources/Dream/Materials (Lit and Glass, created by
    // Dream > Set up 3D materials) so the shader variants ship in every build.
    public static class Mats
    {
        static Material lit, glass;
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        public static Material Get(Color c, float metal, float smooth, float glow = 0)
        {
            string key = ColorUtility.ToHtmlStringRGBA(c) + metal.ToString("0.00") + smooth.ToString("0.00") + glow.ToString("0.00");
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Template(false)) { name = "Dream " + key };
            Set(m, c, metal, smooth);
            if (glow > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * glow); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
            cache[key] = m; return m;
        }
        public static Material Metal(Color c, float glow = 0) => Get(c, 1f, 0.86f, glow);
        public static Material Plastic(Color c, float smooth = 0.55f) => Get(c, 0f, smooth);
        public static Material Fabric(Color c) => Get(c, 0f, 0.18f);
        public static Material Gem(Color c, float glow = 0.35f) => Get(c, 0.3f, 0.97f, glow);

        public static Material Glass(Color c)
        {
            string key = "glass" + ColorUtility.ToHtmlStringRGBA(c);
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Template(true)) { name = "Dream " + key };
            Set(m, c, 0.1f, 0.95f);
            cache[key] = m; return m;
        }

        // a picture on a surface: floors, rugs, screens and the view out the window. glow > 0 makes it light itself.
        public static Material Textured(Texture tex, Color tint, float smooth = 0.4f, Vector2? tiling = null, float glow = 0, bool transparent = false)
        {
            var m = new Material(Template(transparent)) { name = "Dream tex " + (tex != null ? tex.name : "none") };
            Set(m, tint, 0f, smooth);
            if (tex != null)
            {
                m.SetTexture("_BaseMap", tex); if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
                if (tiling.HasValue) { m.SetTextureScale("_BaseMap", tiling.Value); if (m.HasProperty("_MainTex")) m.SetTextureScale("_MainTex", tiling.Value); }
            }
            if (glow > 0 && tex != null) { m.EnableKeyword("_EMISSION"); m.SetTexture("_EmissionMap", tex); m.SetColor("_EmissionColor", Color.white * glow); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
            else if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
            return m;
        }

        static void Set(Material m, Color c, float metal, float smooth)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        }

        static Material Template(bool transparent)
        {
            if (!transparent && lit != null) return lit;
            if (transparent && glass != null) return glass;
            var m = Resources.Load<Material>(transparent ? "Dream/Materials/Glass" : "Dream/Materials/Lit");
            if (m == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                m = new Material(sh);
                if (transparent) MakeTransparent(m);
            }
            if (transparent) glass = m; else lit = m;
            return m;
        }

        public static void MakeTransparent(Material m)
        {
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
        }
    }
}
