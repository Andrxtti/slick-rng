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
        public BodyFit Fit => null;

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
            var cb = pending; pending = null;
            try { cb?.Invoke(); } catch (Exception e) { Debug.LogException(e); }
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
        static readonly Dictionary<string, string[]> HairMap = new Dictionary<string, string[]>
        {
            // UMA 3's own card hair first: it takes the hair colour. The older bb_ cuts stay as a fallback for the short styles.
            { "short", new[] { "Hair_MessyRightPart_Recipe", "Hair_LeftPart_Recipe" } }, { "fade", new[] { "Hair_PulledBack_Recipe", "bb_Male_Military_Hair_Recipe" } },
            { "waves", new[] { "Hair_PulledBack_Recipe" } }, { "buzz", new[] { "Hair_PulledBack_Recipe", "bb_Male_Military_Hair_Recipe" } },
            { "quiff", new[] { "Hair_MessyPomp_Recipe" } }, { "slick", new[] { "Hair_StraigntPulledBack_Recipe", "Hair_PulledBack_Recipe" } },
            { "curly", new[] { "Hair_Poofy" } }, { "afro", new[] { "Hair_Poofy" } }, { "twists", new[] { "HairMessyUp_Recipe", "Hair_Pointy_Recipe" } },
            { "cornrows", new[] { "Hair_StraigntPulledBack_Recipe" } }, { "locs", new[] { "Hair_LongSwept_Recipe" } }, { "mohawk", new[] { "Hair_Pointy_Recipe" } },
            { "wavy", new[] { "Hair_LeftPart_Recipe", "HairMessyUp_Recipe" } }, { "curtains", new[] { "Hair_Straight_Recipe", "Hair_CurveUnder_Recipe" } },
            { "mullet", new[] { "HairMessyUp_Recipe" } }, { "long", new[] { "Hair_LongSwept_Recipe", "Hair_Straight_Recipe" } },
            { "ponytail", new[] { "HairPonytail_Recipe" } }, { "bun", new[] { "Hair_Bun_Recipe", "Hair_UpwardBun_Recipe" } },
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
            return $"{s.hairStyle}|{s.facial}|{s.brows}|{s.age}|{s.freckles}|{o?.id}|{(h != null && h.id != "phones" ? "hat" : "")}";
        }

        void Dress(CharacterSpec s)
        {
            var avail = dca.AvailableRecipes;
            bool hat = s.Get("head") is Worn hw && hw.id != "none" && hw.id != "phones";
            // a hat sits over short hair; long styles keep their length
            string hairStyle = hat && s.hairStyle != "long" && s.hairStyle != "locs" && s.hairStyle != "ponytail" ? "buzz" : s.hairStyle;
            Put(avail, "Hair", hairStyle == "bald" ? null : Pick(avail, "Hair", HairMap.TryGetValue(hairStyle ?? "", out var hs) ? hs : null, true));
            Put(avail, "Beard", s.facial == "none" ? null : Pick(avail, "Beard", BeardMap.TryGetValue(s.facial ?? "", out var bs) ? bs : null, false));
            Put(avail, "Eyebrows", Pick(avail, "Eyebrows", BrowMap.TryGetValue(s.brows ?? "", out var br) ? br : null, true));
            string skinLook = s.age == "old" ? "Senior_U_Wardrobe" : s.age == "adult" ? "Adult_M_Wardrobe" : s.freckles ? "Freckled_Light_U_Wardrobe" : null;
            foreach (var slot in new[] { "Complexion", "Face" })
                if (skinLook != null && Has(avail, slot, skinLook)) { Put(avail, slot, skinLook); skinLook = null; }

            // clothes: what the closet item says to wear, else our pick from UMA's built-in wardrobe
            var o = s.Get("outfit"); string id = o?.id ?? "office";
            var def = Wardrobe.For("outfit", id);
            bool formal = Tailored.Contains(id);
            string top = def != null && !string.IsNullOrEmpty(def.asset) ? def.asset
                : Pick(avail, def != null && !string.IsNullOrEmpty(def.umaSlot) ? def.umaSlot : "Chest", TopMap.TryGetValue(id, out var tm) ? tm : new[] { formal ? "male_jacket_hive_Recipe" : "male_hoodie_grey_Recipe" }, true);
            Put(avail, def != null && !string.IsNullOrEmpty(def.umaSlot) ? def.umaSlot : "Chest", top);
            Put(avail, "Legs", Pick(avail, "Legs", formal ? new[] { "male_sweatpants_black_Recipe", "male_sportpants_alt_black_Recipe" } : new[] { "male_sportpants_alt_black_Recipe", "male_sweatpants_black_Recipe" }, true));
            Put(avail, "Feet", Pick(avail, "Feet", formal ? new[] { "male_shoes_tall_Recipe", "male_shoe_low_white.001_Recipe" } : new[] { "male_shoe_low_white.001_Recipe", "shoe_low_white_Recipe" }, true));
            if (def != null) foreach (var extra in def.with)
                {
                    var p = extra.Split(':'); if (p.Length != 2) continue;
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
