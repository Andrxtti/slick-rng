// Your place, in 3D: walls, a wood floor, the view out the window, furniture and the things you can use. Built
// from Resources/Dream/Homes/<tier>.json (made by unity/tools/art/layouts.py) and the furniture meshes in
// Resources/Dream/Props. A pro-art home can replace any tier: a prefab at Resources/Homes/<tier> with children
// named "use:<id>" (laptop, bed, closet, tv, beer, bong, fish) is used instead.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CFDream.Character
{
    public class HomeUse
    {
        public string id, label;
        public Vector3 spot; public float face;     // where you stand to use it (room space), and which way you face
        public Transform tr;                         // the thing itself
        public Vector3 Anchor => tr != null ? tr.position + Vector3.up * top : Vector3.zero;   // where its label floats (world)
        public float top = 0.3f;
    }

    public class HomeRoom
    {
        public GameObject root; public string tier;
        public float W, D, H; public Vector3 spawn; public float spawnFace;
        public readonly List<HomeUse> uses = new List<HomeUse>();
        public Renderer tvScreen; public Material tvOff; public Material[] tvOn;
        public Transform tank; public Vector3 tankMin, tankMax;  // water volume in room space
        public readonly List<Transform> fish = new List<Transform>();
        public Transform can; public Vector3 canHome; public Transform bong; public Transform shadow;
        bool[,] blocked; const float Cell = 0.2f; int gx, gz;

        static Dictionary<string, object> propMeta;
        static readonly Dictionary<string, Texture2D> tex = new Dictionary<string, Texture2D>();

        public HomeUse Use(string id) => uses.Find(u => u.id == id);

        public static HomeRoom Build(string tier, Transform parent, int layer)
        {
            var ta = Resources.Load<TextAsset>("Dream/Homes/" + tier) ?? Resources.Load<TextAsset>("Dream/Homes/studio");
            var L = ta != null ? Json.Parse(ta.text) as Dictionary<string, object> : null;
            if (L == null) return null;
            if (propMeta == null) { var pm = Resources.Load<TextAsset>("Dream/Homes/props"); propMeta = pm != null ? Json.Parse(pm.text) as Dictionary<string, object> : new Dictionary<string, object>(); }
            var r = new HomeRoom { tier = tier };
            r.W = F(L, "w", 5); r.D = F(L, "d", 4.5f); r.H = F(L, "h", 2.6f);
            r.root = new GameObject("Home " + tier); r.root.transform.SetParent(parent, false); r.root.layer = layer;
            Color wall = Hex(J.S(L, "wall", "#5a5262")), wall2 = Hex(J.S(L, "wall2", "#4f4858")), floor = Hex(J.S(L, "floor", "#c9b8a2")), trim = Hex(J.S(L, "trim", "#2a2430"));
            string scheme = tier;

            // floor (it also catches taps), walls and skirting
            var fl = Quad(r.root.transform, "Floor", new Vector3(-r.W / 2, 0, r.D / 2), new Vector3(r.W / 2, 0, r.D / 2), new Vector3(r.W / 2, 0, -r.D / 2), new Vector3(-r.W / 2, 0, -r.D / 2),
                Mats.Textured(Tex("floor_wood"), floor, 0.35f, new Vector2(r.W / 1.8f, r.D / 1.8f)), layer, new Vector2(1, 1));
            var bc = fl.AddComponent<BoxCollider>(); bc.center = new Vector3(0, -0.05f, 0); bc.size = new Vector3(r.W, 0.1f, r.D);
            Quad(r.root.transform, "Back wall", new Vector3(-r.W / 2, 0, -r.D / 2), new Vector3(r.W / 2, 0, -r.D / 2), new Vector3(r.W / 2, r.H, -r.D / 2), new Vector3(-r.W / 2, r.H, -r.D / 2), Mats.Get(wall, 0, 0.15f), layer, Vector2.one);
            Quad(r.root.transform, "Left wall", new Vector3(-r.W / 2, 0, r.D / 2), new Vector3(-r.W / 2, 0, -r.D / 2), new Vector3(-r.W / 2, r.H, -r.D / 2), new Vector3(-r.W / 2, r.H, r.D / 2), Mats.Get(wall2, 0, 0.15f), layer, Vector2.one);
            Box(r.root.transform, new Vector3(0, 0.05f, -r.D / 2 + 0.01f), new Vector3(r.W, 0.1f, 0.02f), Mats.Get(trim, 0, 0.4f), layer);
            Box(r.root.transform, new Vector3(-r.W / 2 + 0.01f, 0.05f, 0), new Vector3(0.02f, 0.1f, r.D), Mats.Get(trim, 0, 0.4f), layer);
            // a thin lip along the open edges so the cut-away reads as a diorama
            Box(r.root.transform, new Vector3(0, -0.06f, r.D / 2 + 0.03f), new Vector3(r.W + 0.12f, 0.12f, 0.06f), Mats.Get(trim, 0, 0.4f), layer);
            Box(r.root.transform, new Vector3(r.W / 2 + 0.03f, -0.06f, 0), new Vector3(0.06f, 0.12f, r.D + 0.12f), Mats.Get(trim, 0, 0.4f), layer);

            // windows onto the bay
            var viewMat = Mats.Textured(Tex(J.S(L, "view", "window_city")), Color.white, 0.6f, null, 1.1f);
            foreach (var w in List(L, "windows"))
            {
                float x = F(w, "x"), y = F(w, "y", 1.5f), ww = F(w, "w", 1.6f), hh = F(w, "h", 1.1f);
                bool leftWall = J.S(w, "wall") == "left"; float z = F(w, "z");
                var g = new GameObject("Window"); g.layer = layer; g.transform.SetParent(r.root.transform, false);
                if (leftWall) { g.transform.localPosition = new Vector3(-r.W / 2 + 0.012f, y, z); g.transform.localRotation = Quaternion.Euler(0, 90, 0); }
                else g.transform.localPosition = new Vector3(x, y, -r.D / 2 + 0.012f);
                Quad(g.transform, "View", new Vector3(-ww / 2, -hh / 2, 0), new Vector3(ww / 2, -hh / 2, 0), new Vector3(ww / 2, hh / 2, 0), new Vector3(-ww / 2, hh / 2, 0), viewMat, layer, Vector2.one);
                var fm = Mats.Get(trim, 0, 0.5f); float t = 0.06f;
                Box(g.transform, new Vector3(0, hh / 2 + t / 2, 0.02f), new Vector3(ww + 2 * t, t, 0.06f), fm, layer);
                Box(g.transform, new Vector3(0, -hh / 2 - t / 2, 0.03f), new Vector3(ww + 2 * t, t, 0.1f), fm, layer);
                Box(g.transform, new Vector3(-ww / 2 - t / 2, 0, 0.02f), new Vector3(t, hh, 0.06f), fm, layer);
                Box(g.transform, new Vector3(ww / 2 + t / 2, 0, 0.02f), new Vector3(t, hh, 0.06f), fm, layer);
                Box(g.transform, new Vector3(0, 0, 0.02f), new Vector3(0.03f, hh, 0.04f), fm, layer);
            }
            foreach (var g in List(L, "rugs"))
            {
                float rw = F(g, "w", 2), rd = F(g, "d", 1.4f); var c = new Vector3(F(g, "x"), 0.006f, F(g, "z"));
                Quad(r.root.transform, "Rug", c + new Vector3(-rw / 2, 0, rd / 2), c + new Vector3(rw / 2, 0, rd / 2), c + new Vector3(rw / 2, 0, -rd / 2), c + new Vector3(-rw / 2, 0, -rd / 2),
                    Mats.Textured(Tex("rug"), Color.Lerp(Color.white, Hex(J.S(g, "c", "#e8c46a")), 0.35f), 0.1f), layer, Vector2.one);
            }
            foreach (var g in List(L, "lights"))
            {
                var lg = new GameObject("Light"); lg.layer = layer; lg.transform.SetParent(r.root.transform, false);
                lg.transform.localPosition = new Vector3(F(g, "x"), F(g, "y", 1.5f), F(g, "z"));
                var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = Hex(J.S(g, "c", "#ffcf9a")); li.intensity = F(g, "i", 1); li.range = F(g, "r", 4);
                li.cullingMask = 1 << layer; li.shadows = LightShadows.None;
            }

            // the furniture
            r.gx = Mathf.CeilToInt(r.W / Cell); r.gz = Mathf.CeilToInt(r.D / Cell); r.blocked = new bool[r.gx, r.gz];
            foreach (var p in List(L, "props"))
            {
                string pid = J.S(p, "p"); if (string.IsNullOrEmpty(pid)) continue;
                var over = J.O(p, "c");
                Material Mat(string name)
                {
                    var hex = over != null ? J.S(over, name) : null;
                    return PropMat(name, hex != null ? Hex(hex) : (Color?)null, scheme);
                }
                var go = DrmMesh.Spawn("Dream/Props/" + pid, r.root.transform, layer, Mat, pid);
                if (go == null) continue;
                go.transform.localPosition = new Vector3(F(p, "x"), F(p, "y"), F(p, "z"));
                go.transform.localRotation = Quaternion.Euler(0, F(p, "ry"), 0);
                float s = F(p, "s", 1); go.transform.localScale = Vector3.one * s;
                var meta = J.O(propMeta, pid);
                Vector3 mn = V3(meta, "min", new Vector3(-0.3f, 0, -0.3f)), mx = V3(meta, "max", new Vector3(0.3f, 0.8f, 0.3f));
                // screens get their picture as a textured quad over the mesh's screen
                var sc = J.L(meta, "screen");
                if (sc != null && sc.Count == 4)
                {
                    var q = new Vector3[4]; for (int i = 0; i < 4; i++) q[i] = V3(sc[i]);
                    bool tv = pid != "laptop";
                    var m = tv ? Mats.Get(Hex("#0b0d10"), 0f, 0.92f) : Mats.Textured(Tex("laptop_screen"), Color.white, 0.8f, null, 1.3f);
                    var sq = Quad(go.transform, "Screen", q[0], q[1], q[2], q[3], m, layer, Vector2.one);
                    if (tv)
                    {
                        r.tvScreen = sq.GetComponent<Renderer>(); r.tvOff = m;
                        r.tvOn = new[] { Mats.Textured(Tex("tv_poker_0"), Color.white, 0.8f, null, 1.4f), Mats.Textured(Tex("tv_poker_1"), Color.white, 0.8f, null, 1.4f), Mats.Textured(Tex("tv_poker_2"), Color.white, 0.8f, null, 1.4f) };
                    }
                }
                bool walkable = pid == "rug" || pid == "can" || pid == "bong" || pid == "laptop" || (pid == "crt" && F(p, "y") > 0.1f) || pid.Contains("tv") && F(p, "y") > 0.1f;
                if (!walkable) r.Block(go.transform, mn * s, mx * s);
                string use = J.S(p, "use");
                if (!string.IsNullOrEmpty(use))
                {
                    var sp = J.L(p, "spot");
                    var u = new HomeUse { id = use, label = J.S(p, "label", use), tr = go.transform, top = (mx.y - mn.y) * s + 0.15f };
                    if (sp != null && sp.Count >= 3) { u.spot = new Vector3(N(sp[0]), 0, N(sp[1])); u.face = N(sp[2]); }
                    else u.spot = go.transform.localPosition + go.transform.localRotation * Vector3.forward * 0.8f;
                    r.uses.Add(u);
                    var col = go.AddComponent<BoxCollider>(); col.center = (mn + mx) / 2; col.size = Vector3.Max(mx - mn, Vector3.one * 0.25f);
                    if (use == "beer") { r.can = go.transform; r.canHome = go.transform.localPosition; }
                    if (use == "bong") r.bong = go.transform;
                    if (use == "fish")
                    {
                        r.tank = go.transform;
                        // water volume (tank local -> room space) and a few fish swimming in it
                        Vector3 a = new Vector3(mn.x + 0.08f, 0.86f, mn.z + 0.08f), b = new Vector3(mx.x - 0.08f, mx.y - 0.12f, mx.z - 0.08f);
                        r.tankMin = a; r.tankMax = b;
                        Color[] cols = { Hex("#ff8a1f"), Hex("#3fb6ff"), Hex("#ffd23f"), Hex("#ff4f8b") };
                        int nfish = pid.Contains("big") ? 6 : 3;
                        for (int i = 0; i < nfish; i++)
                        {
                            var fc = cols[i % cols.Length];
                            var f = DrmMesh.Spawn("Dream/Props/fish", go.transform, layer, nm => Mats.Get(fc, 0.2f, 0.85f), "Fish " + i);
                            if (f != null) r.fish.Add(f.transform);
                        }
                    }
                }
            }
            r.BlockEdges();
            var spn = J.L(L, "spawn");
            if (spn != null && spn.Count >= 3) { r.spawn = new Vector3(N(spn[0]), 0, N(spn[1])); r.spawnFace = N(spn[2]); }

            // a soft shadow under you
            var sh = new GameObject("Shadow"); sh.layer = layer; sh.transform.SetParent(r.root.transform, false);
            Quad(sh.transform, "Blob", new Vector3(-0.45f, 0.012f, 0.3f), new Vector3(0.45f, 0.012f, 0.3f), new Vector3(0.45f, 0.012f, -0.3f), new Vector3(-0.45f, 0.012f, -0.3f),
                Mats.Textured(Resources.Load<Texture2D>("Dream/Drive/shadow"), new Color(0, 0, 0, 0.75f), 0f, null, 0, true), layer, Vector2.one);
            r.shadow = sh.transform;
            return r;
        }

        // ------------------------------------------------------------ walking: a grid over the floor, A* between cells
        void Block(Transform t, Vector3 mn, Vector3 mx)
        {
            const float pad = 0.24f;
            var corners = new[] { new Vector3(mn.x - pad, 0, mn.z - pad), new Vector3(mx.x + pad, 0, mn.z - pad), new Vector3(mx.x + pad, 0, mx.z + pad), new Vector3(mn.x - pad, 0, mx.z + pad) };
            for (int i = 0; i < 4; i++) corners[i] = t.localPosition + t.localRotation * corners[i];
            float x0 = Mathf.Min(corners[0].x, Mathf.Min(corners[1].x, Mathf.Min(corners[2].x, corners[3].x))), x1 = Mathf.Max(corners[0].x, Mathf.Max(corners[1].x, Mathf.Max(corners[2].x, corners[3].x)));
            float z0 = Mathf.Min(corners[0].z, Mathf.Min(corners[1].z, Mathf.Min(corners[2].z, corners[3].z))), z1 = Mathf.Max(corners[0].z, Mathf.Max(corners[1].z, Mathf.Max(corners[2].z, corners[3].z)));
            for (int i = 0; i < gx; i++) for (int k = 0; k < gz; k++)
                {
                    var c = CellPos(i, k);
                    if (c.x >= x0 && c.x <= x1 && c.z >= z0 && c.z <= z1) blocked[i, k] = true;
                }
        }
        void BlockEdges()
        {
            for (int i = 0; i < gx; i++) for (int k = 0; k < gz; k++)
                {
                    var c = CellPos(i, k);
                    if (c.x < -W / 2 + 0.3f || c.x > W / 2 - 0.25f || c.z < -D / 2 + 0.3f || c.z > D / 2 - 0.25f) blocked[i, k] = true;
                }
        }
        Vector3 CellPos(int i, int k) => new Vector3(-W / 2 + (i + 0.5f) * Cell, 0, -D / 2 + (k + 0.5f) * Cell);
        (int, int) CellOf(Vector3 p) => (Mathf.Clamp(Mathf.FloorToInt((p.x + W / 2) / Cell), 0, gx - 1), Mathf.Clamp(Mathf.FloorToInt((p.z + D / 2) / Cell), 0, gz - 1));
        public bool Free(Vector3 p) { var (i, k) = CellOf(p); return !blocked[i, k]; }

        (int, int) NearestFree(int i, int k)
        {
            if (!blocked[i, k]) return (i, k);
            for (int r = 1; r < Mathf.Max(gx, gz); r++)
                for (int a = -r; a <= r; a++) for (int b = -r; b <= r; b++)
                    {
                        if (Mathf.Abs(a) != r && Mathf.Abs(b) != r) continue;
                        int x = i + a, z = k + b; if (x < 0 || z < 0 || x >= gx || z >= gz) continue;
                        if (!blocked[x, z]) return (x, z);
                    }
            return (i, k);
        }

        // a walkable route in room space; the last point is `to` itself when it's free, else the nearest free spot
        public List<Vector3> Path(Vector3 from, Vector3 to)
        {
            var (si, sk) = NearestFree(CellOf(from).Item1, CellOf(from).Item2);
            var (ti, tk) = CellOf(to); bool exact = !blocked[ti, tk]; (ti, tk) = NearestFree(ti, tk);
            var open = new SortedSet<(float f, int i, int k)>(Comparer<(float, int, int)>.Create((a, b) => a.Item1 != b.Item1 ? a.Item1.CompareTo(b.Item1) : a.Item2 != b.Item2 ? a.Item2.CompareTo(b.Item2) : a.Item3.CompareTo(b.Item3)));
            var g = new float[gx, gz]; var from_ = new (int, int)[gx, gz]; var seen = new bool[gx, gz];
            for (int i = 0; i < gx; i++) for (int k = 0; k < gz; k++) g[i, k] = float.MaxValue;
            float Hh(int i, int k) => Mathf.Sqrt((i - ti) * (i - ti) + (k - tk) * (k - tk));
            g[si, sk] = 0; open.Add((Hh(si, sk), si, sk)); from_[si, sk] = (-1, -1);
            bool found = false;
            while (open.Count > 0)
            {
                var cur = open.Min; open.Remove(cur); int ci = cur.i, ck = cur.k;
                if (seen[ci, ck]) continue; seen[ci, ck] = true;
                if (ci == ti && ck == tk) { found = true; break; }
                for (int a = -1; a <= 1; a++) for (int b = -1; b <= 1; b++)
                    {
                        if (a == 0 && b == 0) continue; int x = ci + a, z = ck + b;
                        if (x < 0 || z < 0 || x >= gx || z >= gz || blocked[x, z] || seen[x, z]) continue;
                        if (a != 0 && b != 0 && (blocked[ci + a, ck] || blocked[ci, ck + b])) continue;   // no cutting corners
                        float ng = g[ci, ck] + (a != 0 && b != 0 ? 1.414f : 1f);
                        if (ng < g[x, z]) { g[x, z] = ng; from_[x, z] = (ci, ck); open.Add((ng + Hh(x, z), x, z)); }
                    }
            }
            var pts = new List<Vector3>();
            if (!found) { pts.Add(CellPos(ti, tk)); return pts; }
            var cells = new List<(int, int)>(); for (var c = (ti, tk); c.Item1 >= 0; c = from_[c.Item1, c.Item2]) cells.Add(c);
            cells.Reverse();
            // keep only the turns we need: skip cells while there's a clear straight line
            var keep = new List<(int, int)> { cells[0] }; int anchor = 0;
            for (int n = 2; n < cells.Count; n++) if (!Clear(cells[anchor], cells[n])) { keep.Add(cells[n - 1]); anchor = n - 1; }
            keep.Add(cells[cells.Count - 1]);
            for (int n = 1; n < keep.Count; n++) pts.Add(CellPos(keep[n].Item1, keep[n].Item2));
            if (exact && pts.Count > 0) pts[pts.Count - 1] = new Vector3(to.x, 0, to.z);
            if (pts.Count == 0) pts.Add(exact ? new Vector3(to.x, 0, to.z) : CellPos(ti, tk));
            return pts;
        }
        bool Clear((int, int) a, (int, int) b)
        {
            int steps = Mathf.Max(Mathf.Abs(b.Item1 - a.Item1), Mathf.Abs(b.Item2 - a.Item2)) * 2;
            for (int s = 0; s <= steps; s++)
            {
                float t = steps == 0 ? 0 : s / (float)steps;
                int i = Mathf.RoundToInt(Mathf.Lerp(a.Item1, b.Item1, t)), k = Mathf.RoundToInt(Mathf.Lerp(a.Item2, b.Item2, t));
                if (blocked[i, k]) return false;
            }
            return true;
        }

        // ------------------------------------------------------------ materials
        static Material PropMat(string name, Color? over, string tier)
        {
            bool dark = tier == "penthouse";
            Color C(string h) => over ?? Hex(h);
            switch (name)
            {
                case "wood": return Mats.Get(C(dark ? "#3a2a20" : tier == "island" ? "#b98a5a" : "#8a5f3c"), 0f, 0.35f);
                case "wood_dark": return Mats.Get(C("#4a3222"), 0f, 0.35f);
                case "black": return Mats.Get(C("#141416"), 0.1f, 0.45f);
                case "metal": return Mats.Metal(C("#b9bcc6"));
                case "sheet": return Mats.Fabric(C("#ece9e4"));
                case "pillow": return Mats.Fabric(C("#f4f2ee"));
                case "blanket": return Mats.Fabric(C("#34507a"));
                case "chair": return Mats.Get(C("#1c1c20"), 0f, 0.4f);
                case "laptop": return Mats.Get(C("#9a9ca4"), 0.8f, 0.75f);
                case "screen": return Mats.Get(C("#2a6fd1"), 0f, 0.9f, 0.6f);
                case "plastic_dk": return Mats.Get(C("#1e1c1a"), 0f, 0.5f);
                case "screen_tv": return Mats.Get(C("#0b0d10"), 0f, 0.92f);
                case "fabric": return Mats.Fabric(C("#5b4a6a"));
                case "pillow2": return Mats.Fabric(C("#e8a23a"));
                case "appliance": return Mats.Get(C("#e8e8ea"), 0.2f, 0.8f);
                case "can": return Mats.Metal(C("#d4b13a"));
                case "glass_tint": return Mats.Glass(over ?? new Color(0.5f, 0.85f, 0.78f, 0.45f));
                case "water": return Mats.Glass(over ?? new Color(0.2f, 0.5f, 0.85f, 0.35f));
                case "tank_glass": return Mats.Glass(over ?? new Color(0.8f, 0.92f, 0.97f, 0.15f));
                case "gravel": return Mats.Get(C("#9a8b72"), 0f, 0.2f);
                case "leaf": return Mats.Get(C("#2f7a3a"), 0f, 0.35f);
                case "fish": return Mats.Get(C("#ff8a1f"), 0.2f, 0.85f);
                case "shirt_a": return Mats.Fabric(C("#b8262c"));
                case "shirt_b": return Mats.Fabric(C("#2f5fb0"));
                case "shirt_c": return Mats.Fabric(C("#e9e7e1"));
                case "shirt_d": return Mats.Fabric(C("#1d1d22"));
                case "lampshade": return Mats.Get(C("#ffe2b0"), 0f, 0.3f, 1.4f);
                case "pot": return Mats.Get(C("#b5653a"), 0f, 0.3f);
                default: return Mats.Plastic(C("#808080"));
            }
        }

        // ------------------------------------------------------------ helpers
        static GameObject Quad(Transform parent, string name, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Material m, int layer, Vector2 uv)
        {
            var go = new GameObject(name); go.layer = layer; go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name };
            mesh.vertices = new[] { a, b, c, d };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(uv.x, 0), new Vector2(uv.x, uv.y), new Vector2(0, uv.y) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };   // corners go bottom-left, bottom-right, top-right, top-left as seen from the lit side
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            // face the side we want lit: towards the room's open front and up
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = m; mr.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }
        static void Box(Transform parent, Vector3 c, Vector3 s, Material m, int layer)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.layer = layer; UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false); go.transform.localPosition = c; go.transform.localScale = s;
            var mr = go.GetComponent<MeshRenderer>(); mr.sharedMaterial = m; mr.shadowCastingMode = ShadowCastingMode.Off;
        }
        static Texture2D Tex(string n)
        {
            if (tex.TryGetValue(n, out var t) && t != null) return t;
            t = Resources.Load<Texture2D>("Dream/Homes/" + n); if (t != null) t.wrapMode = n == "floor_wood" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            tex[n] = t; return t;
        }
        static List<object> List(object o, string k) => J.L(o, k) ?? new List<object>();
        static float F(object o, string k, float def = 0) => (float)J.N(o, k, def);
        static float N(object o) => o is double d ? (float)d : 0f;
        static Vector3 V3(object o) => o is List<object> l && l.Count >= 3 ? new Vector3(N(l[0]), N(l[1]), N(l[2])) : Vector3.zero;
        static Vector3 V3(object o, string k, Vector3 def) { var l = J.L(o, k); return l != null && l.Count >= 3 ? V3(l) : def; }
        static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.gray;
    }
}
