// Rigid pieces built in code and pinned to the skeleton: chains, watches, bracelets, rings, earrings, glasses and
// hats. Any body that exposes a humanoid skeleton can wear them, so they work on UMA today and on pro-art bodies
// later. Sizes are for an average adult and scale with the body (shoulder width), and a body can fine-tune them
// through its BodyFit. A brand piece with its own model uses kind "prefab" in its brand.json instead.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CFDream.Character
{
    // Where things sit on a body, in metres for an average adult (scale 1). A body provider can supply its own.
    public class BodyFit
    {
        public float scale = 0;                 // 0: work it out from shoulder width
        public float neckUp = 0.012f, neckR = 0.062f, neckDepth = 0.058f, neckFwd = 0.008f, chainDrop = 0.075f, chestLean = 0.045f;
        public float headUp = 0.085f, headFwd = 0.0f, headR = 0.098f, crownUp = 0.112f;   // crownUp: top of the skull above the head centre
        public float eyeUp = 0.012f, eyeFwd = 0.094f, eyeGap = 0.032f, lensFwd = 0.024f; // lensFwd: eyeball centre to lens, with eye bones
        public BodyFit Copy() => (BodyFit)MemberwiseClone();
        public float earSide = 0.077f, earUp = -0.032f, earBack = 0.004f;
        public float wristR = 0.030f, wristFlat = 0.022f, wristBack = 0.035f;
        public float fingerR = 0.0095f;
    }

    public static class Accessories
    {
        public static readonly HashSet<string> Slots = new HashSet<string> { "chain", "watch", "wrist", "ring", "ear", "eyes", "head" };

        class Ctx
        {
            public CharacterStage st; public BodyFit f; public float s; public Vector3 up, fwd, right; public CharacterSpec spec;
            public Transform B(HumanBodyBones b) => st.Bone(b);
            public float M(float metres) => metres * s;
        }

        public static void Dress(CharacterStage st, CharacterSpec spec, List<GameObject> list)
        {
            foreach (var g in list) if (g != null) UnityEngine.Object.Destroy(g);
            list.Clear();
            if (st == null || spec == null || st.Bone(HumanBodyBones.Head) == null) return;
            var c = new Ctx { st = st, spec = spec, f = st.Fit ?? new BodyFit() };
            c.s = c.f.scale;
            if (c.s <= 0)
            {
                Transform l = c.B(HumanBodyBones.LeftUpperArm), r = c.B(HumanBodyBones.RightUpperArm);
                c.s = l != null && r != null ? Mathf.Clamp(Vector3.Distance(l.position, r.position) / 0.33f, 0.6f, 1.6f) : 1;
            }
            c.fwd = st.Pivot.forward; c.up = st.Pivot.up; c.right = Vector3.Cross(c.up, c.fwd);
            Transform le = c.B(HumanBodyBones.LeftEye), re = c.B(HumanBodyBones.RightEye);
            if (le != null && re != null) { c.f = c.f.Copy(); c.f.eyeGap = Vector3.Distance(le.position, re.position) / 2 / c.s; } // lenses over the real eyes

            foreach (var w in spec.worn)
            {
                var def = Wardrobe.For(w.slot, w.id);
                if (def != null && def.kind == "uma") continue;          // the body wears these
                if (def == null && !Slots.Contains(w.slot)) continue;    // outfits and patches belong to the body too
                try
                {
                    GameObject go = def != null && def.kind == "prefab" ? Prefab(c, def, w)
                        : Build(c, w, def != null && !string.IsNullOrEmpty(def.asset) && def.kind == "proc" ? def.asset : w.id);
                    if (go == null) continue;
                    if (def != null && def.kind == "proc" && (def.offset != Vector3.zero || def.euler != Vector3.zero || Math.Abs(def.scale - 1) > 1e-4f))
                    {
                        go.transform.position += c.right * def.offset.x + c.up * def.offset.y + c.fwd * def.offset.z;
                        go.transform.rotation = Quaternion.Euler(def.euler) * go.transform.rotation; go.transform.localScale *= def.scale;
                    }
                    go.GetComponent<BoneFollow>()?.Pin();
                    list.Add(go);
                }
                catch (Exception e) { Debug.LogWarning($"[Dream] Couldn't build {w.Key}: {e.Message}"); }
            }
        }

        static GameObject Build(Ctx c, Worn w, string kind)
        {
            switch (w.slot)
            {
                case "chain": return Chain(c, w, kind);
                case "watch": return Watch(c, w, kind);
                case "wrist": return Bracelet(c, w, kind);
                case "ring": return Ring(c, w, kind);
                case "ear": return Earrings(c, w, kind);
                case "eyes": return Glasses(c, w, kind);
                case "head": return Hat(c, w, kind);
            }
            return null;
        }

        // ------------------------------------------------------------ helpers
        static GameObject Root(Ctx c, string name, Transform bone)
        {
            // beside the body, not inside its skeleton: UMA rebuilds its bones and wants their names unique
            var go = new GameObject(name);
            go.transform.SetParent(c.st.Pivot, false);
            go.transform.position = bone.position; go.transform.rotation = Quaternion.identity;
            go.AddComponent<BoneFollow>().Init(c.st, bone);
            return go;
        }
        static GameObject Part(GameObject root, Mesh mesh, Material mat, Vector3 pos, Quaternion rot, Vector3? scale = null)
        {
            var go = new GameObject(mesh.name);
            go.transform.SetParent(root.transform, false);
            go.transform.position = pos; go.transform.rotation = rot;
            if (scale.HasValue) go.transform.localScale = scale.Value;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false; mr.lightProbeUsage = LightProbeUsage.Off; mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go;
        }
        // world-space path → a tube parented to root (the mesh is built relative to the root so it follows the bone)
        static GameObject TubePart(GameObject root, string key, List<Vector3> world, float radius, Material mat, bool closed = true, int sides = 8)
        {
            var local = new List<Vector3>(world.Count); var o = root.transform.position;
            foreach (var p in world) local.Add(p - o);
            var mesh = MeshKit.Tube(key + Hash(local), local, radius, sides, closed);
            return Part(root, mesh, mat, o, Quaternion.identity);
        }
        static string Hash(List<Vector3> l)
        {
            unchecked { int h = 17; foreach (var p in l) { h = h * 31 + Mathf.RoundToInt(p.x * 10000); h = h * 31 + Mathf.RoundToInt(p.y * 10000); h = h * 31 + Mathf.RoundToInt(p.z * 10000); } return h.ToString("x"); }
        }
        // rotation that maps local +Y to `axis` and local +Z towards `front`
        static Quaternion Up(Vector3 axis, Vector3 front)
        {
            var f = Vector3.ProjectOnPlane(front, axis); if (f.sqrMagnitude < 1e-6f) f = Vector3.ProjectOnPlane(Vector3.forward, axis);
            return Quaternion.LookRotation(f.normalized, axis);
        }
        static Color Hex(string h) => Palette.Hex(h);
        static Color Dyed(Worn w, string fallback) => w.dye ?? Hex(fallback);
        static float Glow(Worn w, float base_) => base_ + (w.level >= 3 ? 0.25f : 0);
        static Material MetalOf(Worn w) { var m = Palette.Metal(w); return w.finish == 3 ? Mats.Get(m, 0.9f, 0.7f) : Mats.Metal(m, Glow(w, 0)); }

        // ------------------------------------------------------------ chains
        static List<Vector3> NeckLoop(Ctx c, Vector3 centre, float rx, float rz, float drop, int n = 56)
        {
            var l = new List<Vector3>(n);
            for (int i = 0; i < n; i++)
            {
                float t = i * Mathf.PI * 2 / n, front = Mathf.Pow((1 + Mathf.Cos(t)) / 2, 2.2f);
                float d = drop * front;
                l.Add(centre + c.right * (Mathf.Sin(t) * rx) + c.fwd * (Mathf.Cos(t) * rz + c.M(c.f.chestLean) * front) - c.up * d);
            }
            return l;
        }

        static GameObject Chain(Ctx c, Worn w, string id)
        {
            var neck = c.B(HumanBodyBones.Neck) ?? c.B(HumanBodyBones.Head);
            var root = Root(c, "Chain " + w.id, neck);
            Vector3 centre = neck.position + c.up * c.M(c.f.neckUp) + c.fwd * c.M(c.f.neckFwd);
            float rx = c.M(c.f.neckR), rz = c.M(c.f.neckDepth), drop = c.M(c.f.chainDrop);
            var metal = MetalOf(w);
            switch (id)
            {
                case "bones":
                case "lights":
                    {
                        var path = NeckLoop(c, centre, rx, rz, drop, 22);
                        TubePart(root, "cord", NeckLoop(c, centre, rx, rz, drop), c.M(0.0012f), Mats.Fabric(Hex("#1b1b1f")));
                        Color[] bulbs = { Hex("#e8433a"), Hex("#3cbc5a"), Hex("#f2c230"), Hex("#3a7fe0") };
                        for (int i = 0; i < path.Count; i++)
                        {
                            if (id == "bones") Part(root, MeshKit.Box(new Vector3(c.M(0.012f), c.M(0.005f), c.M(0.005f))), Mats.Plastic(Hex("#ece6d6"), 0.35f), path[i], Quaternion.LookRotation(c.fwd, c.up));
                            else Part(root, MeshKit.Sphere(c.M(0.0045f), 8), Mats.Gem(bulbs[i % 4], 0.9f), path[i], Quaternion.identity);
                        }
                        break;
                    }
                case "layered":
                    TubePart(root, "a", NeckLoop(c, centre, rx, rz, drop * 0.7f), c.M(0.0024f), metal);
                    TubePart(root, "b", NeckLoop(c, centre, rx * 1.04f, rz * 1.04f, drop * 1.45f), c.M(0.0028f), metal);
                    break;
                default:
                    {
                        float th = id == "cuban" ? 0.0062f : id == "iced" ? 0.0066f : id == "rope" ? 0.0046f : id == "wishbone" ? 0.0022f : 0.0028f;
                        var loop = NeckLoop(c, centre, rx, rz, drop);
                        TubePart(root, "chain", loop, c.M(th), id == "iced" ? Mats.Gem(Hex("#eef6ff"), Glow(w, 0.35f)) : metal, true, id == "cuban" || id == "iced" ? 10 : 8);
                        if (id == "iced") for (int i = 0; i < loop.Count; i += 2) Part(root, MeshKit.Sphere(c.M(0.0042f), 6), Mats.Gem(Hex("#ffffff"), Glow(w, 0.45f)), loop[i], Quaternion.identity);
                        if (id == "wishbone")
                        {
                            Vector3 tip = loop[0] - c.up * c.M(0.026f) + c.fwd * c.M(0.004f), top = loop[0];
                            TubePart(root, "wl", new List<Vector3> { top - c.right * c.M(0.010f), tip }, c.M(0.0022f), metal, false);
                            TubePart(root, "wr", new List<Vector3> { top + c.right * c.M(0.010f), tip }, c.M(0.0022f), metal, false);
                        }
                        break;
                    }
            }
            return root;
        }

        // ------------------------------------------------------------ wrist: watches and bracelets
        // the wrist's frame: axis along the forearm, `o` pointing away from the body (the back of the wrist when the arm hangs)
        static bool Wrist(Ctx c, bool left, float back, out Transform bone, out Vector3 at, out Vector3 axis, out Vector3 o, out Vector3 side)
        {
            var hand = c.B(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            bone = c.B(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
            at = axis = o = side = Vector3.zero;
            if (hand == null || bone == null) return false;
            axis = (hand.position - bone.position).normalized;
            var core = c.B(HumanBodyBones.Chest) ?? c.B(HumanBodyBones.Spine) ?? c.B(HumanBodyBones.Hips);
            o = core != null ? Vector3.ProjectOnPlane(hand.position - core.position, axis) : Vector3.zero;
            if (o.sqrMagnitude < 1e-6f) o = left ? -c.right : c.right;
            o = o.normalized; side = Vector3.Cross(axis, o).normalized;
            at = hand.position - axis * c.M(back);
            return true;
        }

        static GameObject Watch(Ctx c, Worn w, string id)
        {
            if (!Wrist(c, true, c.f.wristBack, out var bone, out var at, out var axis, out var o, out var side)) return null;
            var root = Root(c, "Watch " + w.id, bone);
            float ro = c.M(c.f.wristFlat) + c.M(0.003f), rs = c.M(c.f.wristR) + c.M(0.003f);
            bool digital = id == "digital";
            var strap = digital ? Mats.Plastic(Hex("#18181b"), 0.35f) : id == "hyper" ? Mats.Plastic(Hex("#121214"), 0.5f) : MetalOf(w);
            TubePart(root, "band", MeshKit.Ellipse(at, o, side, ro, rs, 32), c.M(digital ? 0.0042f : 0.0036f), strap, true, 8);
            Vector3 faceAt = at + o * (ro + c.M(0.002f));
            var rot = Up(o, axis);
            if (digital)
            {
                Part(root, MeshKit.Box(new Vector3(c.M(0.034f), c.M(0.009f), c.M(0.038f))), Mats.Plastic(Hex("#202024"), 0.5f), faceAt + o * c.M(0.0045f), rot);
                Part(root, MeshKit.Box(new Vector3(c.M(0.024f), c.M(0.001f), c.M(0.022f))), Mats.Gem(Hex("#5ad2c8"), 0.6f), faceAt + o * c.M(0.0092f), rot);
            }
            else
            {
                var caseMat = id == "hyper" ? Mats.Get(Hex("#2e2e34"), 1, 0.75f) : id == "iced" ? Mats.Gem(Hex("#eef6ff"), Glow(w, 0.3f)) : MetalOf(w);
                Part(root, MeshKit.Disc(c.M(0.0195f), c.M(0.009f), 28, c.M(0.002f)), caseMat, faceAt, rot);
                var dial = id == "steel" ? Hex("#1d2a3d") : id == "gold" ? Hex("#f1e6c8") : id == "hyper" ? Hex("#0d0d0f") : Hex("#e9eef5");
                Part(root, MeshKit.Disc(c.M(0.0158f), c.M(0.0012f), 28), Mats.Get(dial, 0.2f, 0.9f, id == "hyper" ? 0.05f : 0), faceAt + o * c.M(0.0088f), rot);
                // hands
                var hm = id == "gold" ? Mats.Plastic(Hex("#1a1a1a")) : MetalOf(w);
                Part(root, MeshKit.Box(new Vector3(c.M(0.0016f), c.M(0.0008f), c.M(0.011f))), hm, faceAt + o * c.M(0.0104f) + axis * c.M(0.004f), rot);
                Part(root, MeshKit.Box(new Vector3(c.M(0.008f), c.M(0.0008f), c.M(0.0016f))), hm, faceAt + o * c.M(0.0104f) + side * c.M(0.003f), rot);
                if (id == "hyper") Part(root, MeshKit.Box(new Vector3(c.M(0.0012f), c.M(0.0008f), c.M(0.012f))), Mats.Gem(Hex("#e8433a"), 0.6f), faceAt + o * c.M(0.0108f) - axis * c.M(0.003f), rot);
            }
            return root;
        }

        static GameObject Bracelet(Ctx c, Worn w, string id)
        {
            if (!Wrist(c, false, c.f.wristBack + 0.006f, out var bone, out var at, out var axis, out var o, out var side)) return null;
            var root = Root(c, "Bracelet " + w.id, bone);
            float ro = c.M(c.f.wristFlat) + c.M(0.006f), rs = c.M(c.f.wristR) + c.M(0.005f);
            var loop = MeshKit.Ellipse(at, o, side, ro, rs, id == "beads" ? 16 : 32);
            switch (id)
            {
                case "beads":
                    foreach (var p in loop) Part(root, MeshKit.Sphere(c.M(0.0058f), 10), Mats.Plastic(Hex("#6b4a2e"), 0.4f), p, Quaternion.identity);
                    break;
                case "tennis":
                    TubePart(root, "band", loop, c.M(0.0026f), MetalOf(w));
                    for (int i = 0; i < loop.Count; i += 2) Part(root, MeshKit.Sphere(c.M(0.0034f), 6), Mats.Gem(Hex("#ffffff"), Glow(w, 0.4f)), loop[i] + (loop[i] - at).normalized * c.M(0.0015f), Quaternion.identity);
                    break;
                default:
                    TubePart(root, "band", loop, c.M(0.0032f), MetalOf(w), true, 10);
                    break;
            }
            return root;
        }

        // ------------------------------------------------------------ rings
        static GameObject Ring(Ctx c, Worn w, string id)
        {
            bool pinky = id == "pinky";
            Transform prox = c.B(pinky ? HumanBodyBones.RightLittleProximal : HumanBodyBones.RightRingProximal);
            Transform mid = c.B(pinky ? HumanBodyBones.RightLittleIntermediate : HumanBodyBones.RightRingIntermediate);
            Transform parent; Vector3 at, axis;
            if (prox != null && mid != null) { parent = prox; axis = (mid.position - prox.position).normalized; at = prox.position + (mid.position - prox.position) * 0.4f; }
            else
            {
                var hand = c.B(HumanBodyBones.RightHand); var fore = c.B(HumanBodyBones.RightLowerArm); if (hand == null || fore == null) return null;
                parent = hand; axis = (hand.position - fore.position).normalized; at = hand.position + axis * c.M(0.075f);
            }
            var root = Root(c, "Ring " + w.id, parent);
            Vector3 a = Vector3.Cross(axis, Mathf.Abs(Vector3.Dot(axis, c.fwd)) < 0.9f ? c.fwd : c.up).normalized, b = Vector3.Cross(axis, a);
            float R = c.M(c.f.fingerR) * (pinky ? 0.85f : 1);
            var metal = MetalOf(w);
            TubePart(root, "band", MeshKit.Ellipse(at, a, b, R, R, 20), c.M(id == "champ" ? 0.0034f : 0.0022f), metal, true, 8);
            // the top of the ring faces away from the body, like the back of the hand
            var core = c.B(HumanBodyBones.Chest) ?? c.B(HumanBodyBones.Hips);
            Vector3 o = core != null ? Vector3.ProjectOnPlane(at - core.position, axis).normalized : c.right;
            if (id == "signet") Part(root, MeshKit.Disc(c.M(0.0062f), c.M(0.0028f), 20, c.M(0.0006f)), metal, at + o * (R + c.M(0.0012f)), Up(o, axis));
            if (id == "champ")
            {
                Part(root, MeshKit.Disc(c.M(0.0085f), c.M(0.0045f), 20, c.M(0.001f)), metal, at + o * (R + c.M(0.0015f)), Up(o, axis));
                Part(root, MeshKit.Sphere(c.M(0.0042f), 10), Mats.Gem(Hex("#5aa9de"), Glow(w, 0.4f)), at + o * (R + c.M(0.0068f)), Quaternion.identity);
            }
            return root;
        }

        // ------------------------------------------------------------ head: earrings, glasses, hats
        static Vector3 HeadCentre(Ctx c, out Transform head)
        {
            head = c.B(HumanBodyBones.Head);
            // a body with eye bones pins the face exactly: measure the rest of the head back from the eyes
            Transform l = c.B(HumanBodyBones.LeftEye), r = c.B(HumanBodyBones.RightEye);
            if (l != null && r != null)
            {
                var eyes = (l.position + r.position) / 2;
                return eyes + c.fwd * c.M(c.f.lensFwd) - c.up * c.M(c.f.eyeUp) - c.fwd * c.M(c.f.eyeFwd);
            }
            return head.position + c.up * c.M(c.f.headUp) + c.fwd * c.M(c.f.headFwd);
        }

        static GameObject Earrings(Ctx c, Worn w, string id)
        {
            var C = HeadCentre(c, out var head);
            var root = Root(c, "Earrings " + w.id, head);
            foreach (int sgn in new[] { -1, 1 })
            {
                Vector3 lobe = C + c.right * (sgn * c.M(c.f.earSide)) + c.up * c.M(c.f.earUp) - c.fwd * c.M(c.f.earBack);
                switch (id)
                {
                    case "hoop":
                        TubePart(root, "hoop" + sgn, MeshKit.Ellipse(lobe - c.up * c.M(0.009f), c.up, c.fwd, c.M(0.009f), c.M(0.009f), 20), c.M(0.0011f), MetalOf(w), true, 6);
                        break;
                    case "diamond":
                        Part(root, MeshKit.Sphere(c.M(0.0052f), 10), Mats.Gem(Hex("#eaf6ff"), Glow(w, 0.45f)), lobe + c.right * (sgn * c.M(0.002f)), Quaternion.identity);
                        break;
                    default:
                        Part(root, MeshKit.Sphere(c.M(0.0036f), 10), MetalOf(w), lobe + c.right * (sgn * c.M(0.0018f)), Quaternion.identity);
                        break;
                }
            }
            return root;
        }

        static GameObject Glasses(Ctx c, Worn w, string id)
        {
            var C = HeadCentre(c, out var head);
            var root = Root(c, "Glasses " + w.id, head);
            Vector3 E = C + c.up * c.M(c.f.eyeUp) + c.fwd * c.M(c.f.eyeFwd);
            float gap = c.M(c.f.eyeGap);
            string fr = "#151515", lens = "#ffffff1f"; float frT = 0.0026f, rx = 0.024f, ry = 0.019f; bool wide = false, metalFrame = false;
            switch (id)
            {
                case "readers": fr = "#6b5a45"; frT = 0.0013f; rx = 0.021f; ry = 0.014f; break;
                case "round": metalFrame = true; frT = 0.0013f; rx = ry = 0.021f; break;
                case "thick": frT = 0.0036f; rx = 0.025f; ry = 0.018f; break;
                case "sport": fr = "#e9e7e1"; frT = 0.003f; wide = true; lens = "#1d3a78c8"; break;
                case "tinted": fr = "#3a3d44"; frT = 0.0014f; lens = "#b65a2a8c"; break;
                case "aviator": fr = "#d8c27a"; metalFrame = true; frT = 0.0013f; rx = 0.026f; ry = 0.022f; lens = "#1f2a26b8"; break;
                case "shades": frT = 0.0032f; lens = "#0b0b0ee0"; break;
                case "masq": fr = "#3a1458"; frT = 0.0055f; rx = 0.027f; ry = 0.018f; lens = "#00000000"; break;
                case "amber": fr = "#5a3a22"; frT = 0.003f; lens = "#d98a2b9c"; break;
                case "snow": fr = "#efeee9"; frT = 0.0042f; wide = true; lens = "#e07a2cc0"; break;
            }
            var frame = metalFrame ? MetalOf(w) : Mats.Plastic(Hex(fr), id == "masq" ? 0.85f : 0.6f);
            var lensC = Hex(lens);
            var face = Up(c.fwd, c.up); // lens discs: local Y → forward
            if (wide)
            {
                float wx = 0.072f, wy = id == "snow" ? 0.03f : 0.024f;
                TubePart(root, "frame", MeshKit.Ellipse(E, c.right, c.up, c.M(wx), c.M(wy), 40), c.M(frT), frame);
                if (lensC.a > 0.01f) Part(root, MeshKit.Disc(1, 0.02f, 32), Mats.Glass(lensC), E - c.fwd * c.M(0.001f), face, new Vector3(c.M(wx), c.M(0.03f), c.M(wy)));
                if (id == "snow") TubePart(root, "strap", MeshKit.Ellipse(C + c.up * c.M(c.f.eyeUp) - c.fwd * c.M(0.005f), c.right, c.fwd, c.M(c.f.headR + 0.006f), c.M(c.f.eyeFwd + 0.006f), 40, Mathf.PI * 1.08f, Mathf.PI * 1.92f), c.M(0.004f), Mats.Fabric(Hex("#24427e")), false);
                else Temples(c, root, C, E, c.M(wx), frT, frame);
                return root;
            }
            foreach (int sgn in new[] { -1, 1 })
            {
                Vector3 L = E + c.right * (sgn * gap);
                TubePart(root, "rim" + sgn, MeshKit.Ellipse(L, c.right, c.up, c.M(rx), c.M(ry), 32), c.M(frT), frame);
                if (lensC.a > 0.01f) Part(root, MeshKit.Disc(1, 0.02f, 28), Mats.Glass(lensC), L - c.fwd * c.M(0.0008f), face, new Vector3(c.M(rx), c.M(0.03f), c.M(ry)));
            }
            // bridge
            TubePart(root, "bridge", MeshKit.Ellipse(E + c.up * c.M(0.004f), c.right, c.up, gap - c.M(rx), c.M(0.004f), 10, 0, Mathf.PI), c.M(frT * 0.8f), frame, false);
            Temples(c, root, C, E, gap + c.M(rx), frT, frame);
            if (id == "masq") TubePart(root, "ribbon", MeshKit.Ellipse(C + c.up * c.M(c.f.eyeUp), c.right, c.fwd, c.M(c.f.headR + 0.003f), c.M(c.f.eyeFwd), 30, Mathf.PI * 1.1f, Mathf.PI * 1.9f), c.M(0.0018f), Mats.Fabric(Hex("#3a1458")), false);
            return root;
        }
        static void Temples(Ctx c, GameObject root, Vector3 C, Vector3 E, float halfW, float frT, Material frame)
        {
            foreach (int sgn in new[] { -1, 1 })
            {
                Vector3 a = E + c.right * (sgn * halfW), b = C + c.right * (sgn * c.M(c.f.earSide + 0.004f)) + c.up * c.M(c.f.eyeUp - 0.004f) - c.fwd * c.M(0.01f);
                TubePart(root, "temple" + sgn, new List<Vector3> { a, Vector3.Lerp(a, b, 0.5f) + c.right * (sgn * c.M(0.004f)), b, b - c.up * c.M(0.012f) - c.fwd * c.M(0.008f) }, c.M(frT * 0.75f), frame, false, 6);
            }
        }

        static GameObject Hat(Ctx c, Worn w, string id)
        {
            var C = HeadCentre(c, out var head);
            var root = Root(c, "Hat " + w.id, head);
            float R = c.M(c.f.headR + 0.006f);
            Vector3 crown = C + c.up * c.M(c.f.crownUp);           // where the skull ends; hats sit on it, not float above it
            Quaternion Tilt(float deg) => Up(c.up, c.fwd) * Quaternion.Euler(-deg, 0, 0);
            Material Cloth(string hex) => Mats.Fabric(Dyed(w, hex));
            switch (id)
            {
                case "cap":
                case "snap":
                    {
                        var rot = Tilt(12); Vector3 at = crown - c.up * (R * (id == "snap" ? 1.0f : 0.9f) - c.M(0.012f));
                        var mat = Cloth(id == "cap" ? "#17171a" : "#24427e");
                        Part(root, MeshKit.Dome(R, id == "snap" ? 1.0f : 0.9f, -0.05f), mat, at, rot);
                        Part(root, MeshKit.Sphere(c.M(0.006f), 8), mat, at + rot * Vector3.up * R * (id == "snap" ? 1.0f : 0.9f), rot);
                        Vector3 front = at + rot * (Vector3.forward * R * 0.97f);
                        var brimRot = rot * Quaternion.Euler(id == "snap" ? 2 : 10, 0, 0);
                        Part(root, MeshKit.Disc(1, 0.05f, 28), mat, front + brimRot * (Vector3.forward * c.M(0.035f)), brimRot, new Vector3(c.M(0.078f), c.M(0.004f) / 0.05f, c.M(0.06f)));
                        break;
                    }
                case "beanie":
                    {
                        var rot = Tilt(6); Vector3 at = crown - c.up * ((R + c.M(0.004f)) * 1.12f - c.M(0.02f)); var mat = Cloth("#b3262c");
                        Part(root, MeshKit.Dome(R + c.M(0.004f), 1.12f, -0.32f), mat, at, rot);
                        float br = R + c.M(0.004f);
                        Vector3 rim = at + rot * (Vector3.up * (Mathf.Sin(-0.3f) * br * 1.12f));
                        float cr = Mathf.Cos(-0.3f) * br + c.M(0.004f);
                        TubePart(root, "cuff", MeshKit.Ellipse(rim, rot * Vector3.right, rot * Vector3.forward, cr, cr, 36), c.M(0.012f), mat);
                        break;
                    }
                case "bucket":
                    {
                        var rot = Tilt(4); Vector3 at = crown - c.up * c.M(0.062f); var mat = Cloth("#d8cfb4");
                        Part(root, MeshKit.Lathe("bucketcrown" + R.ToString("0.0000"), new[] { new Vector2(R, 0), new Vector2(R * 0.92f, c.M(0.075f)), new Vector2(R * 0.92f, c.M(0.075f)), new Vector2(0, c.M(0.08f)) }, 32, true), mat, at, rot);
                        Part(root, MeshKit.Lathe("bucketbrim" + R.ToString("0.0000"), new[] { new Vector2(R * 0.98f, 0), new Vector2(R + c.M(0.05f), -c.M(0.032f)) }, 32, true), mat, at, rot);
                        break;
                    }
                case "cowboy":
                case "fedora":
                    {
                        bool cow = id == "cowboy"; var rot = Tilt(cow ? 6 : 8); Vector3 at = crown - c.up * c.M(0.055f);
                        var mat = Cloth(cow ? "#8a5a32" : "#2a2522");
                        float h = c.M(cow ? 0.11f : 0.095f);
                        Part(root, MeshKit.Lathe("crown" + id + R.ToString("0.0000"), new[] { new Vector2(R * 0.97f, 0), new Vector2(R * 0.93f, h * 0.8f), new Vector2(R * 0.6f, h), new Vector2(0, h * 0.9f) }, 32, true), mat, at, rot);
                        float bw = c.M(cow ? 0.09f : 0.06f);
                        Part(root, MeshKit.Lathe("brim" + id + R.ToString("0.0000"), new[] { new Vector2(R * 0.9f, 0), new Vector2(R + bw * 0.75f, c.M(0.002f)), new Vector2(R + bw, c.M(cow ? 0.025f : 0.008f)) }, 36, true), mat, at, rot);
                        TubePart(root, "band", MeshKit.Ellipse(at + rot * (Vector3.up * c.M(0.012f)), rot * Vector3.right, rot * Vector3.forward, R * 0.965f, R * 0.965f, 36), c.M(0.0045f), Mats.Fabric(Hex(cow ? "#3a2416" : "#101010")));
                        break;
                    }
                case "visor":
                    {
                        var rot = Tilt(8); Vector3 at = crown - c.up * c.M(0.075f); var mat = Cloth("#1e8f4a");
                        TubePart(root, "band", MeshKit.Ellipse(at, rot * Vector3.right, rot * Vector3.forward, R * 0.97f, R * 0.97f, 36), c.M(0.007f), mat);
                        var brimRot = rot * Quaternion.Euler(10, 0, 0);
                        Part(root, MeshKit.Disc(1, 0.05f, 28), mat, at + rot * (Vector3.forward * R) + brimRot * (Vector3.forward * c.M(0.03f)), brimRot, new Vector3(c.M(0.075f), c.M(0.004f) / 0.05f, c.M(0.055f)));
                        break;
                    }
                case "phones":
                    {
                        var dark = Mats.Plastic(Hex("#202024"), 0.55f); float side = c.M(c.f.earSide + 0.016f);
                        Vector3 mid = C + c.up * c.M(c.f.earUp + 0.012f);
                        TubePart(root, "band", MeshKit.Ellipse(mid, c.right, c.up, side, R + c.M(0.02f) - c.M(c.f.earUp), 30, 0, Mathf.PI), c.M(0.006f), dark, false);
                        foreach (int sgn in new[] { -1, 1 })
                        {
                            var cupRot = Up(c.right * sgn, c.fwd);
                            Part(root, MeshKit.Disc(c.M(0.036f), c.M(0.026f), 28, c.M(0.006f)), dark, mid + c.right * (sgn * (side - c.M(0.012f))), cupRot);
                            Part(root, MeshKit.Disc(c.M(0.02f), c.M(0.002f), 24), MetalOf(w), mid + c.right * (sgn * (side + c.M(0.0145f))), cupRot);
                        }
                        break;
                    }
                case "pumpkin":
                    {
                        var rot = Tilt(4); Vector3 at = C + c.up * c.M(0.02f);
                        Part(root, MeshKit.Dome(R + c.M(0.01f), 0.95f, -0.25f), Mats.Get(Hex("#d98a2b"), 0, 0.35f), at, rot);
                        TubePart(root, "stem", new List<Vector3> { at + c.up * (R * 0.9f), at + c.up * (R * 0.9f + c.M(0.03f)) + c.fwd * c.M(0.006f) }, c.M(0.007f), Mats.Fabric(Hex("#3c6b2a")), false);
                        break;
                    }
                case "pilgrim":
                    {
                        var rot = Tilt(4); Vector3 at = C + c.up * c.M(0.035f); var mat = Mats.Fabric(Hex("#141414"));
                        float h = c.M(0.14f);
                        Part(root, MeshKit.Lathe("pilgrim" + R.ToString("0.0000"), new[] { new Vector2(R * 0.98f, 0), new Vector2(R * 0.86f, h), new Vector2(R * 0.86f, h), new Vector2(0, h) }, 32, true), mat, at, rot);
                        Part(root, MeshKit.Lathe("pilgrimbrim" + R.ToString("0.0000"), new[] { new Vector2(R * 0.9f, 0), new Vector2(R + c.M(0.06f), 0) }, 32, true), mat, at, rot);
                        Part(root, MeshKit.Box(new Vector3(c.M(0.03f), c.M(0.025f), c.M(0.004f))), Mats.Metal(Hex("#e3b94f")), at + rot * (Vector3.forward * R * 0.97f + Vector3.up * c.M(0.02f)), rot);
                        break;
                    }
                case "santa":
                    {
                        var rot = Tilt(-18); Vector3 at = C + c.up * c.M(0.02f);
                        float h = c.M(0.2f);
                        Part(root, MeshKit.Lathe("santa" + R.ToString("0.0000"), new[] { new Vector2(R, 0), new Vector2(R * 0.55f, h * 0.45f), new Vector2(c.M(0.01f), h), new Vector2(0, h) }, 32, true), Mats.Fabric(Hex("#c8242c")), at, rot);
                        TubePart(root, "cuff", MeshKit.Ellipse(at, rot * Vector3.right, rot * Vector3.forward, R + c.M(0.006f), R + c.M(0.006f), 36), c.M(0.016f), Mats.Fabric(Hex("#f3f1ea")));
                        Part(root, MeshKit.Sphere(c.M(0.022f), 12), Mats.Fabric(Hex("#f3f1ea")), at + rot * (Vector3.up * h), Quaternion.identity);
                        break;
                    }
                default:
                    UnityEngine.Object.Destroy(root); return null;
            }
            return root;
        }

        // ------------------------------------------------------------ brand pieces with their own model
        static GameObject Prefab(Ctx c, WearableDef def, Worn w)
        {
            var src = string.IsNullOrEmpty(def.asset) ? null : Resources.Load<GameObject>(def.asset);
            if (src == null) { Debug.LogWarning($"[Dream] {w.Key}: prefab \"{def.asset}\" not found in Resources"); return null; }
            HumanBodyBones hb = HumanBodyBones.Head;
            if (!string.IsNullOrEmpty(def.bone) && !Enum.TryParse(def.bone, true, out hb)) hb = HumanBodyBones.Head;
            var bone = c.B(hb) ?? c.B(HumanBodyBones.Head);
            var go = UnityEngine.Object.Instantiate(src, c.st.Pivot, false);
            go.name = "Brand " + w.Key;
            go.transform.position = bone.TransformPoint(def.offset * c.s); go.transform.rotation = bone.rotation * Quaternion.Euler(def.euler);
            go.transform.localScale = Vector3.one * def.scale * c.s;
            go.AddComponent<BoneFollow>().Init(c.st, bone);
            if (def.dyeable && w.dye.HasValue)
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    var mpb = new MaterialPropertyBlock(); r.GetPropertyBlock(mpb);
                    mpb.SetColor("_BaseColor", w.dye.Value); r.SetPropertyBlock(mpb);
                }
            return go;
        }
    }
}
