// The UMA body: a realistic, fully adjustable character dressed from UMA's wardrobe. When UMA is in the project
// (the DREAM_UMA define, set automatically by Dream's editor tools) and Resources/Dream/UMARefs exists
// (Dream > Set up 3D characters), it registers itself as the 3D body and every CharacterView shows it.
//
// What maps where:
//   sculpt sliders → UMA face DNA (0.5 is neutral; our -1..1 nudges each by about a quarter)
//   skin, hair and eye colours → UMA shared colours
//   hair, beard, brows, age/freckles → UMA wardrobe recipes (best match per style)
//   outfit → wardrobe.json "asset" when set, else a built-in pick from UMA's clothes until the pro art lands
using System;
using System.Collections.Generic;
using UnityEngine;
#if DREAM_UMA
using System.Linq;
using UMA;
using UMA.CharacterSystem;
using UMA.PoseTools;
#endif

namespace CFDream.Character
{
#if DREAM_UMA
    public class UMABody : ICharacterBody
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            var refs = Resources.Load<DreamUMARefs>("Dream/UMARefs");
            if (refs == null) { Debug.Log("[Dream] UMA is installed but Resources/Dream/UMARefs is missing: run Dream > Set up 3D characters"); return; }
            Bodies.Create = pivot => new UMABody(pivot, refs);
            Bodies.Name = "UMA";
        }

        readonly DreamUMARefs refs; readonly GameObject go; readonly DynamicCharacterAvatar dca;
        CharacterSpec queued; Action pending; bool first = true, built, scheduled; string dressedKey;
        static bool loggedDna;

        public GameObject Root => go;
        public bool Ready => built;
        BodyFit fit;
        public BodyFit Fit => fit;

        public UMABody(Transform pivot, DreamUMARefs refs)
        {
            this.refs = refs;
            go = refs.avatar != null ? UnityEngine.Object.Instantiate(refs.avatar, pivot, false) : new GameObject();
            go.name = "UMA Character";
            go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity;
            dca = go.GetComponent<DynamicCharacterAvatar>();
            if (dca == null) dca = go.AddComponent<DynamicCharacterAvatar>();
            dca.RacePreset = Race(refs.maleRace, "Human Male 3.0");
            if (refs.idle != null)
            {
                if (dca.raceAnimationControllers == null) dca.raceAnimationControllers = new DynamicCharacterAvatar.RaceAnimatorList();
                dca.raceAnimationControllers.defaultAnimationController = refs.idle;
            }
            if (dca.CharacterCreated == null) dca.CharacterCreated = new UMADataEvent();
            if (dca.CharacterUpdated == null) dca.CharacterUpdated = new UMADataEvent();
            dca.CharacterCreated.AddListener(OnBuilt);
            dca.CharacterUpdated.AddListener(OnBuilt);
        }

        // UMA looks races up by their display name ("Human Male 3.0"), not the asset name ("HumanMale30"); accept either
        static string Race(string want, string fallback)
        {
            var idx = UMAAssetIndexer.Instance;
            if (idx == null) return want;
            if (!string.IsNullOrEmpty(want) && idx.GetRace(want) != null) return want;
            foreach (var r in idx.GetAllAssets<RaceData>())
                if (r != null && (r.name == want || r.raceName == want)) return r.raceName;
            return idx.GetRace(fallback) != null ? fallback : want;
        }

        public void Apply(CharacterSpec spec, Action onReady)
        {
            queued = spec; pending = onReady;
            if (!first) Push();
        }

        void OnBuilt(UMAData d)
        {
            Pin(); Tint();
            if (first)
            {
                first = false;
                LogDna(dca);
                // dress the default body in the player's look, then report ready. Next frame: UMA is still finishing
                // this build when it calls us, and starting another one inside the callback pulls the skeleton out from under it.
                if (queued != null) { scheduled = true; dca.StartCoroutine(PushNextFrame()); return; }
            }
            if (scheduled) return; // UMA sends a second event for the same build; the dressed build is still to come
#if UNITY_EDITOR
            if (!built)
            {
                var sc = dca.umaData?.umaRecipe?.sharedColors;
                Debug.Log("[Dream] UMA shared colours: " + (sc == null ? "none" : string.Join(", ", sc.Where(c => c != null).Select(c => $"{c.name}={(c.channelMask.Length > 0 ? ColorUtility.ToHtmlStringRGB(c.channelMask[0]) : "-")}"))));
            }
#endif
            built = true;
            try { fit = Measure(); } catch (Exception e) { Debug.LogWarning("[Dream] body measure: " + e.Message); fit = null; }
            try { Face(); } catch (Exception e) { Debug.LogWarning("[Dream] face: " + e.Message); }
            var cb = pending; pending = null;
            try { cb?.Invoke(); } catch (Exception e) { Debug.LogException(e); }
        }

        // Size the jewellery to this body: bake the skin and measure the neck, wrists, ring finger, ears and crown
        // around their bones, in the pivot's frame (x right, y up, z forward). Clothes are skipped by name so a
        // hoodie collar doesn't read as a thick neck.
        BodyFit Measure()
        {
            var f = new BodyFit { scale = 1 };
            var piv = go.transform.parent != null ? go.transform.parent : go.transform;
            var pts = new List<Vector3>(); var baked = new Mesh();
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (smr.sharedMesh == null) continue;
                smr.BakeMesh(baked, true);
                var v = baked.vertices; var l2w = smr.transform.localToWorldMatrix; var mats = smr.sharedMaterials;
                // UMA packs everything into one renderer, one submesh per material: keep the skin's submeshes only
                for (int sm = 0; sm < baked.subMeshCount && sm < mats.Length; sm++)
                {
                    string mn = mats[sm] != null ? mats[sm].name.ToLowerInvariant() : "";
                    bool skin = mn.Contains("body") || mn.Contains("skin") || mn.Contains("head") || mn.Contains("face") || mn.Contains("hand") || mn.Contains("human");
                    bool cloth = mn.Contains("hair") || mn.Contains("beard") || mn.Contains("brow") || mn.Contains("hoodie") || mn.Contains("shirt") || mn.Contains("jacket") || mn.Contains("pants") || mn.Contains("shoe") || mn.Contains("short") || mn.Contains("dream_") || mn.Contains("eye");
                    if (cloth || !skin) continue;
                    var tris = baked.GetTriangles(sm);
                    for (int i = 0; i < tris.Length; i += 6) pts.Add(piv.InverseTransformPoint(l2w.MultiplyPoint3x4(v[tris[i]])));
                }
#if UNITY_EDITOR
                Debug.Log("[Dream] body materials: " + string.Join(", ", mats.Select(m => m != null ? m.name : "null")));
#endif
            }
            UnityEngine.Object.Destroy(baked);
            if (pts.Count < 100) return null;
            Vector3 L(HumanBodyBones b) { var t = Bone(b); return t != null ? piv.InverseTransformPoint(t.position) : Vector3.zero; }
            // neck: a slice just above the neck bone
            var neck = L(HumanBodyBones.Neck); var head = L(HumanBodyBones.Head);
            float ny = Mathf.Lerp(neck.y, head.y, 0.35f);
            float rx = 0, zf = 0, zb = 0; int cnt = 0;
            foreach (var q in pts) if (Mathf.Abs(q.y - ny) < 0.012f && Mathf.Abs(q.x - neck.x) < 0.12f && Mathf.Abs(q.z - neck.z) < 0.12f) { rx = Mathf.Max(rx, Mathf.Abs(q.x - neck.x)); zf = Mathf.Max(zf, q.z - neck.z); zb = Mathf.Max(zb, neck.z - q.z); cnt++; }
            if (cnt > 10) { f.neckR = rx + 0.004f; f.neckDepth = (zf + zb) / 2 + 0.004f; f.neckFwd = (zf - zb) / 2; f.neckUp = ny - neck.y; }
            // wrists: radius around the forearm axis near the hand
            foreach (var left in new[] { true, false })
            {
                var hand = L(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand); var fore = L(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
                var axis = (hand - fore).normalized; var at = hand - axis * 0.035f;
                float rFlat = 0, rSide = 0; int c2 = 0;
                var o = Vector3.ProjectOnPlane(left ? Vector3.left : Vector3.right, axis).normalized; var side = Vector3.Cross(axis, o).normalized;
                foreach (var q in pts)
                {
                    var d = q - at; float along = Vector3.Dot(d, axis); if (Mathf.Abs(along) > 0.01f) continue;
                    var r = d - axis * along; if (r.magnitude > 0.08f) continue;
                    rFlat = Mathf.Max(rFlat, Mathf.Abs(Vector3.Dot(r, o))); rSide = Mathf.Max(rSide, Mathf.Abs(Vector3.Dot(r, side))); c2++;
                }
                if (c2 > 6 && left) { f.wristFlat = rFlat; f.wristR = rSide; }
            }
            // ring finger: radius around the proximal phalanx
            var fa = L(HumanBodyBones.RightRingProximal); var fb = L(HumanBodyBones.RightRingIntermediate);
            if (fa != Vector3.zero && fb != Vector3.zero)
            {
                var axis = (fb - fa).normalized; var at = Vector3.Lerp(fa, fb, 0.5f); float rr = 0; int c3 = 0;
                var rs = new List<float>();
                foreach (var q in pts) { var d = q - at; float along = Vector3.Dot(d, axis); if (Mathf.Abs(along) > 0.005f) continue; var r = (d - axis * along).magnitude; if (r < 0.016f) rs.Add(r); }
                rs.Sort(); if (rs.Count > 4) { rr = rs[(int)(rs.Count * 0.8f)]; f.fingerR = rr + 0.0008f; c3 = rs.Count; }
            }
            // ears: the skin's widest points in the band behind the eyes (the ear sits 7-12 cm behind the eye plane); the crown: the top of the skull
            var le = L(HumanBodyBones.LeftEye); var re = L(HumanBodyBones.RightEye); var eyes = (le + re) / 2;
            float ex = 0, ey = 0, ez = 0; int c4 = 0; float top = head.y;
            foreach (var q in pts)
            {
                if (q.y > head.y - 0.02f) top = Mathf.Max(top, q.y);
                if (q.y > eyes.y - 0.05f && q.y < eyes.y + 0.015f && q.z < eyes.z - 0.06f && q.z > eyes.z - 0.14f) ex = Mathf.Max(ex, Mathf.Abs(q.x - eyes.x));
            }
            foreach (var q in pts)
                if (q.y > eyes.y - 0.05f && q.y < eyes.y + 0.015f && q.z < eyes.z - 0.06f && q.z > eyes.z - 0.14f && Mathf.Abs(q.x - eyes.x) > ex - 0.005f) { ey += q.y; ez += q.z; c4++; }
            var centre = eyes + Vector3.forward * f.lensFwd - Vector3.up * f.eyeUp - Vector3.forward * f.eyeFwd;   // what HeadCentre computes
            if (c4 > 2) { f.earSide = ex + 0.002f; f.earUp = (ey / c4) - 0.03f - centre.y; f.earBack = centre.z - ez / c4 + 0.02f; }   // the lobe hangs below and behind the widest point
            f.headUp = top - head.y; f.crownUp = top - centre.y + 0.004f;
            // the skull's width a little above the ears, for hats
            float hw = 0;
            foreach (var q in pts) if (q.y > eyes.y + 0.03f && q.y < eyes.y + 0.07f && q.z < eyes.z && q.z > eyes.z - 0.14f) hw = Mathf.Max(hw, Mathf.Abs(q.x - eyes.x));
            f.headR = Mathf.Max(0.07f, hw > 0 ? hw + 0.004f : ex);
#if UNITY_EDITOR
            Debug.Log($"[Dream] body fit: neck r {f.neckR:0.000} d {f.neckDepth:0.000}, wrist {f.wristFlat:0.000}x{f.wristR:0.000}, finger {f.fingerR:0.0000}, ear side {f.earSide:0.000} up {f.earUp:0.000} back {f.earBack:0.000}, crown +{f.crownUp:0.000} headR {f.headR:0.000} ({pts.Count} pts)");
#endif
            return f;
        }

        // UMA's human race adds a rigidbody and capsule for games with physics; on our floorless stage it would just fall
        void Pin()
        {
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) { rb.useGravity = false; rb.isKinematic = true; }
            foreach (var col in go.GetComponentsInChildren<Collider>(true)) col.enabled = false;
            foreach (var an in go.GetComponentsInChildren<Animator>(true)) an.applyRootMotion = false;
            go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity;
        }

        // UMA writes shared colours to _Color, but UMA 3's URP hair shader reads _BaseColor: tint the generated hair
        // materials ourselves so hair and beards take the player's colour on every pipeline
        void Tint()
        {
            var s = queued; if (s == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null || m.shader == null || m.shader.name.IndexOf("Hair", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    // card hair blends root to tip; a lighter tip reads as real hair catching the light
                    if (m.HasProperty("_RootColor")) { m.SetColor("_RootColor", Color.Lerp(s.hair, Color.black, 0.12f)); m.SetColor("_Tip_Color", Color.Lerp(s.hair, Color.white, 0.14f)); }
                    if (m.HasProperty("_BaseColor")) { var c = s.hair; c.a = m.GetColor("_BaseColor").a; m.SetColor("_BaseColor", c); }
                    if (m.HasProperty("_Color")) { var c = s.hair; c.a = m.GetColor("_Color").a; m.SetColor("_Color", c); }
                }
        }

        System.Collections.IEnumerator PushNextFrame() { yield return null; scheduled = false; Push(); }

        // apply the look: colours and DNA always, the wardrobe only when it changed (rebuilding clothes is the slow part)
        void Push()
        {
            var s = queued; if (s == null) return;
            try
            {
                Colours(s); Shape(s);
                string key = DressKey(s);
                if (key != dressedKey) { Dress(s); dressedKey = key; dca.BuildCharacter(true); }
                else dca.ForceUpdate(true, true, false);
            }
            catch (Exception e) { Debug.LogException(e); var cb = pending; pending = null; cb?.Invoke(); }
        }

        // ------------------------------------------------------------ the face
        // UMA's expression player moves brows, mouth, jaw and eyes by bone poses; it blinks and glances on its own.
        UMAExpressionPlayer face; float talkT; string lastMood;
        void Face()
        {
            var set = (refs.expressionSet as UMAExpressionSet) ?? dca.activeRace?.racedata?.expressionSet;
            if (set == null) return;
            if (face == null) { face = go.GetComponent<UMAExpressionPlayer>(); if (face == null) face = go.AddComponent<UMAExpressionPlayer>(); }
            face.expressionSet = set; face.overrideMecanimJaw = true; face.overrideMecanimEyes = true; face.overrideMecanimHead = false; face.overrideMecanimNeck = false; face.overrideMecanimHands = false;
            face.enableBlinking = true; face.enableSaccades = true; face.minBlinkDelay = 2.5f; face.maxBlinkDelay = 7f;
            face.Initialize();
        }
        public void Express(string mood, bool talking, float t)
        {
            if (face == null) return;
            float k = 1 - Mathf.Exp(-Time.deltaTime * 6f);
            float smile = 0.08f, happy = 0, sad = 0, despair = 0, sour = 0, brows = 0, browsIn = 0, midBrow = 0;
            switch (mood)
            {
                case "win": happy = 0.75f; smile = 0.5f; brows = 0.35f; break;
                case "cash": smile = 0.4f; brows = 0.15f; break;
                case "bust": sad = 0.55f; smile = -0.25f; midBrow = -0.3f; break;
                case "cooler": despair = 0.45f; sour = 0.25f; browsIn = 0.4f; break;
                case "think": browsIn = 0.35f; brows = -0.15f; smile = 0; break;
                case "tense": browsIn = 0.5f; midBrow = -0.2f; smile = -0.1f; break;
                default: brows = 0.06f * Mathf.Sin(t * 0.7f); break;
            }
            // talking: the jaw works, the brows lift on the stresses
            float jaw = 0;
            if (talking) { talkT += Time.deltaTime; float s1 = Mathf.Sin(talkT * 11f), s2 = Mathf.Sin(talkT * 17.3f + 1f); jaw = Mathf.Clamp01(0.12f + 0.38f * Mathf.Abs(s1) * (0.6f + 0.4f * s2 * s2)); brows += 0.08f * Mathf.Max(0, Mathf.Sin(talkT * 2.3f)); }
            else talkT = 0;
            face.jawOpen_Close = Mathf.Lerp(face.jawOpen_Close, jaw, talking ? 0.6f : k);
            face.leftMouthSmile_Frown = Mathf.Lerp(face.leftMouthSmile_Frown, smile, k); face.rightMouthSmile_Frown = Mathf.Lerp(face.rightMouthSmile_Frown, smile * 0.9f, k);
            face.leftBrowUp_Down = Mathf.Lerp(face.leftBrowUp_Down, brows, k); face.rightBrowUp_Down = Mathf.Lerp(face.rightBrowUp_Down, brows * 0.85f, k);
            face.midBrowUp_Down = Mathf.Lerp(face.midBrowUp_Down, midBrow, k); face.browsIn = Mathf.Lerp(face.browsIn, browsIn, k);
            face.HappyFace = Mathf.Lerp(face.HappyFace, happy, k); face.SadFace = Mathf.Lerp(face.SadFace, sad, k); face.DespairFace = Mathf.Lerp(face.DespairFace, despair, k); face.SourFace = Mathf.Lerp(face.SourFace, sour, k);
            lastMood = mood;
        }

        public Transform Bone(HumanBodyBones b)
        {
            var anim = go != null ? go.GetComponentInChildren<Animator>() : null;
            return anim != null && anim.isHuman ? anim.GetBoneTransform(b) : null;
        }

        public void Dispose() { if (go != null) UnityEngine.Object.Destroy(go); }

        // ------------------------------------------------------------ colours
        // UMA multiplies its own skin texture, which is already a light, pinkish skin tone, by the colour we pass.
        // Dividing that tone out means the palette colour is the colour you see (measured on our stage: 1 : 0.81 : 0.89).
        static readonly Vector3 SkinBase = new Vector3(1f, 0.811f, 0.89f);
        static Color Relative(Color target, Vector3 b)
        {
            var c = new Color(target.r / b.x, target.g / b.y, target.b / b.z);
            float m = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            if (m > 1) c = new Color(c.r / m, c.g / m, c.b / m);
            c.a = 1; return c;
        }
        void Colours(CharacterSpec s)
        {
            dca.SetColor("Skin", Relative(s.skin, SkinBase));
            dca.SetColor("Hair", s.hair);
            dca.SetColor("Eyebrows", s.hair);
            dca.SetColor("Beard", s.hair);
            dca.SetColor("Eyes", s.eyes);
        }

        // ------------------------------------------------------------ face shape: our sliders onto UMA's face DNA
        static readonly (string key, string dna, float amt)[] Map =
        {
            ("fw", "headWidth", 0.22f), ("jw", "jawsSize", 0.22f), ("jw", "mandibleSize", 0.12f),
            ("ch", "chinSize", 0.22f), ("ch", "chinPronounced", 0.12f), ("cb", "cheekPronounced", 0.22f), ("cb", "cheekPosition", 0.1f),
            ("cf", "cheekSize", 0.22f), ("cf", "lowCheekPronounced", 0.12f), ("nw", "noseWidth", 0.25f), ("nl", "noseSize", 0.22f),
            ("nb", "nosePronounced", 0.22f), ("nb", "noseCurve", 0.1f), ("lp", "lipsSize", 0.25f), ("mw", "mouthSize", 0.22f),
            ("es", "eyeSize", 0.22f), ("ep", "eyeSpacing", 0.22f), ("et", "eyeRotation", 0.25f), ("bh", "browPosition", 0.2f), ("er", "earsSize", 0.25f),
        };
        void Shape(CharacterSpec s)
        {
            var dna = dca.GetDNA(); if (dna == null || dna.Count == 0) return;
            var vals = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            void Add(string n, float v) => vals[n] = (vals.TryGetValue(n, out var cur) ? cur : 0.5f) + v;
            foreach (var (k, n, a) in Map) Add(n, s.Sculpt(k) * a);
            if (s.face == "round") { Add("cheekSize", 0.08f); Add("headWidth", 0.05f); Add("jawsSize", 0.04f); }
            else if (s.face == "angular") { Add("mandibleSize", 0.1f); Add("chinPronounced", 0.08f); Add("cheekSize", -0.05f); }
            foreach (var kv in dna) if (vals.TryGetValue(kv.Key, out var v)) kv.Value.Set(Mathf.Clamp01(v));
        }
        // once per run: which DNA this race offers, so the slider mapping can be checked against it
        static void LogDna(DynamicCharacterAvatar a)
        {
            if (loggedDna) return; loggedDna = true;
            try
            {
                Debug.Log("[Dream] UMA body ready. DNA: " + string.Join(", ", a.GetDNA().Keys.OrderBy(k => k)));
            }
            catch (Exception e) { Debug.LogWarning("[Dream] UMA DNA list: " + e.Message); }
        }

        // ------------------------------------------------------------ the wardrobe
        // every wardrobe recipe the game can ask UMA for (the editor's library trim keeps these and drops the rest)
        public static IEnumerable<string> Wanted()
        {
            foreach (var m in new[] { HairMap, BeardMap, BrowMap, TopMap }) foreach (var a in m.Values) foreach (var n in a) yield return n;
            foreach (var n in new[] { "Senior_U_Wardrobe", "Adult_M_Wardrobe", "Freckled_Light_U_Wardrobe", "male_sweatpants_black_Recipe", "male_sportpants_alt_black_Recipe",
                                      "male_shoes_tall_Recipe", "male_shoe_low_white.001_Recipe", "shoe_low_white_Recipe", "male_jacket_hive_Recipe", "male_hoodie_grey_Recipe",
                                      // base garments the closet paints its own looks onto (Dream > 3D characters > Build closet clothes)
                                      "male_tshirt_white_Recipe", "male_tanktop_yellow_Recipe", "male_shorts_black_cotton_Recipe", "male_shorts_white_Recipe", "male_shorts_hive_Recipe",
                                      "male_sportpants_grey_Recipe", "male_sportpants_blueWhite_Recipe", "male_tights_black_Recipe", "sportswear_sweater_granit_Recipe", "sportwear_pants_granit_Recipe",
                                      "shoes_tall_white_Recipe", "male_shoes_tall_turquoise_Recipe", "male_hoodie_blue_Recipe", "male_hoodie_blueWhite_Recipe" }) yield return n;
        }

        public static string HairOverride;   // editor tests: force a hair recipe by name
        static readonly Dictionary<string, string[]> HairMap = new Dictionary<string, string[]>
        {
            // one UMA 3 card-hair recipe per style, chosen by looking at each one on the body (Oct 2026)
            { "short", new[] { "Hair_MessyRightPart_Recipe" } }, { "sidepart", new[] { "Hair_LeftPart_Recipe" } },
            { "buzz", new[] { "bb_Male_Military_Hair_Recipe" } }, { "quiff", new[] { "Hair_MessyPomp_Recipe" } },
            { "spiky", new[] { "HairMessyUp_Recipe" } }, { "fauxhawk", new[] { "Hair_Pointy_Recipe" } },
            { "slick", new[] { "Hair_UpwardBun_Recipe" } }, { "bun", new[] { "Hair_Bun_Recipe" } }, { "ponytail", new[] { "HairPonytail_Recipe" } },
            { "long", new[] { "Hair_StraigntPulledBack_Recipe" } }, { "longswept", new[] { "Hair_LongSwept_Recipe" } },
            // styles from older saves (no longer offered) land on the nearest cut
            { "fade", new[] { "Hair_LeftPart_Recipe" } }, { "waves", new[] { "bb_Male_Military_Hair_Recipe" } }, { "curly", new[] { "Hair_MessyRightPart_Recipe" } },
            { "afro", new[] { "Hair_MessyRightPart_Recipe" } }, { "twists", new[] { "HairMessyUp_Recipe" } }, { "cornrows", new[] { "Hair_UpwardBun_Recipe" } },
            { "locs", new[] { "Hair_LongSwept_Recipe" } }, { "mohawk", new[] { "Hair_Pointy_Recipe" } }, { "wavy", new[] { "HairMessyUp_Recipe" } },
            { "curtains", new[] { "Hair_StraigntPulledBack_Recipe" } }, { "mullet", new[] { "HairMessyUp_Recipe" } },
        };
        static readonly Dictionary<string, string[]> BeardMap = new Dictionary<string, string[]>
        {
            { "stubble", new[] { "Beard_Trimmed", "Beard_Trimmed_Recipe" } }, { "mustache", new[] { "Mustache_Small", "Mustache_Shaped" } },
            { "goatee", new[] { "Beard_Goatee", "Beard_Goatee_Recipe" } }, { "chin", new[] { "Beard_Curtain", "Beard_Circle" } },
            { "beard", new[] { "Beard_Trimmed_Bushy", "Beard_Gunslinger", "Beard_Drifter" } },
        };
        static readonly Dictionary<string, string[]> BrowMap = new Dictionary<string, string[]>
        {
            { "natural", new[] { "Eyebrows_Average_Average", "HDEyeBrows_Wardrobe" } }, { "thick", new[] { "Eyebrows_Bushy_Bushy", "Eyebrows_Average_Bushy" } },
            { "slit", new[] { "Eyebrows_Thin_Average", "HDEyeBrows_Thin_Wardrobe" } }, { "arched", new[] { "Eyebrows_Arched_Average", "HDEyeBrowsArched_Wardrobe" } },
        };
        static readonly HashSet<string> Tailored = new HashSet<string> { "suit", "designer", "tux", "blazer", "vampire", "vest", "office" };
        static readonly Dictionary<string, string[]> TopMap = new Dictionary<string, string[]>
        {
            { "hoodie", new[] { "male_hoodie_grey_Recipe" } }, { "fleece", new[] { "male_hoodie_grey_Recipe" } }, { "tee", new[] { "male_tshirt_white_Recipe" } },
            { "jersey", new[] { "male_tshirt_white_Recipe" } }, { "track", new[] { "male_jacket_hive_Recipe" } }, { "polo", new[] { "male_hoodie_blue_Recipe" } },
            { "cardigan", new[] { "male_hoodie_blueWhite_Recipe" } }, { "flannel", new[] { "male_hoodie_blue_Recipe" } }, { "ugly", new[] { "male_hoodie_blueWhite_Recipe" } },
        };

        static string DressKey(CharacterSpec s)
        {
            var o = s.Get("outfit"); var h = s.Get("head");
            return $"{HairOverride}|{s.hairStyle}|{s.facial}|{s.brows}|{s.age}|{s.freckles}|{o?.id}|{s.Get("bottom")?.id}|{s.Get("shoes")?.id}|{(h != null && h.id != "phones" ? "hat" : "")}";
        }

        void Dress(CharacterSpec s)
        {
            var avail = dca.AvailableRecipes;
            bool hat = s.Get("head") is Worn hw && hw.id != "none" && hw.id != "phones";
            // a hat sits over short hair; long styles keep their length
            // a hat covers short hair completely (UMA's card hair would poke through the crown); long styles hang out under it
            bool longHair = s.hairStyle == "long" || s.hairStyle == "longswept" || s.hairStyle == "locs" || s.hairStyle == "ponytail" || s.hairStyle == "curtains";
            string hairStyle = hat && !longHair ? "bald" : s.hairStyle;
            Put(avail, "Hair", hairStyle == "bald" ? null : Pick(avail, "Hair", !string.IsNullOrEmpty(HairOverride) ? new[] { HairOverride } : HairMap.TryGetValue(hairStyle ?? "", out var hs) ? hs : null, true));
            Put(avail, "Beard", s.facial == "none" ? null : Pick(avail, "Beard", BeardMap.TryGetValue(s.facial ?? "", out var bs) ? bs : null, false));
            Put(avail, "Eyebrows", Pick(avail, "Eyebrows", BrowMap.TryGetValue(s.brows ?? "", out var br) ? br : null, true));
            string skinLook = s.age == "old" ? "Senior_U_Wardrobe" : s.age == "adult" ? "Adult_M_Wardrobe" : s.freckles ? "Freckled_Light_U_Wardrobe" : null;
            foreach (var slot in new[] { "Complexion", "Face" })
                if (skinLook != null && Has(avail, slot, skinLook)) { Put(avail, slot, skinLook); skinLook = null; }

            // clothes: the painted closet piece when it's built (dream_<slot>_<id>), else the item's own asset, else a stand-in
            var o = s.Get("outfit"); string id = o?.id ?? "office";
            var def = Wardrobe.For("outfit", id);
            bool formal = Tailored.Contains(id);
            string chest = def != null && !string.IsNullOrEmpty(def.umaSlot) ? def.umaSlot : "Chest";
            string top = Has(avail, chest, "dream_top_" + id) ? "dream_top_" + id
                : def != null && !string.IsNullOrEmpty(def.asset) ? def.asset
                : Pick(avail, chest, TopMap.TryGetValue(id, out var tm) ? tm : new[] { formal ? "male_jacket_hive_Recipe" : "male_hoodie_grey_Recipe" }, true);
            Put(avail, chest, top);
            var b = s.Get("bottom"); string bid = b?.id;
            string legs = bid != null && Has(avail, "Legs", "dream_bottom_" + bid) ? "dream_bottom_" + bid
                : Pick(avail, "Legs", formal ? new[] { "dream_bottom_slacks", "male_sportpants_alt_black_Recipe", "male_sweatpants_black_Recipe" } : new[] { "dream_bottom_jeans", "male_sportpants_alt_black_Recipe", "male_sweatpants_black_Recipe" }, true);
            Put(avail, "Legs", legs);
            var sh = s.Get("shoes"); string sid = sh?.id;
            string feet = sid != null && Has(avail, "Feet", "dream_shoes_" + sid) ? "dream_shoes_" + sid
                : Pick(avail, "Feet", formal ? new[] { "dream_shoes_dress", "male_shoes_tall_Recipe", "male_shoe_low_white.001_Recipe" } : new[] { "dream_shoes_sneakers", "male_shoe_low_white.001_Recipe", "shoe_low_white_Recipe" }, true);
            Put(avail, "Feet", feet);
            if (def != null) foreach (var extra in def.with)
                {
                    var p = extra.Split(':'); if (p.Length != 2 || p[0] == "Legs" || p[0] == "Feet") continue;
                    if (Has(avail, p[0], p[1])) Put(avail, p[0], p[1]);
                }
        }

        static bool Has(Dictionary<string, List<UMATextRecipe>> avail, string slot, string name) =>
            avail != null && avail.TryGetValue(slot, out var l) && l.Any(r => r != null && string.Equals(r.name, name, StringComparison.OrdinalIgnoreCase));

        // the first wanted recipe this race can wear in the slot; optionally any recipe in the slot as a last resort
        static string Pick(Dictionary<string, List<UMATextRecipe>> avail, string slot, string[] wanted, bool anyIfNone)
        {
            if (avail == null || !avail.TryGetValue(slot, out var list) || list == null || list.Count == 0) return null;
            if (wanted != null)
                foreach (var w in wanted)
                {
                    var hit = list.FirstOrDefault(r => r != null && string.Equals(r.name, w, StringComparison.OrdinalIgnoreCase));
                    if (hit != null) return hit.name;
                }
            if (wanted != null)
                foreach (var w in wanted)
                {
                    var stem = w.Replace("_Recipe", "").ToLowerInvariant();
                    var hit = list.FirstOrDefault(r => r != null && r.name.ToLowerInvariant().Contains(stem));
                    if (hit != null) return hit.name;
                }
            return anyIfNone ? list.FirstOrDefault(r => r != null)?.name : null;
        }

        void Put(Dictionary<string, List<UMATextRecipe>> avail, string slot, string recipe)
        {
#if UNITY_EDITOR
            Debug.Log($"[Dream] UMA {slot}: {(string.IsNullOrEmpty(recipe) ? "(none)" : recipe)} of {(avail != null && avail.TryGetValue(slot, out var l) ? l.Count : 0)}");
#endif
            if (string.IsNullOrEmpty(recipe)) { dca.ClearSlot(slot); return; }
            dca.SetSlot(slot, recipe);
        }
    }
#endif
}
