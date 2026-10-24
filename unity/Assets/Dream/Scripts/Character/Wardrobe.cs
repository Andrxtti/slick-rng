// The wardrobe: which 3D piece each closet item wears as, plus brand packs.
//
// Built-in pieces map in Resources/Dream/wardrobe.json. A brand (ours, a partner, a merch drop) is a folder
// Resources/Brands/<brand>/ with a brand.json and its art. Drop the folder in and its items show up in the
// closet with their grade, maker, story and unlock rule; no code changes. See unity/BRAND_KIT.md.
//
// How a piece is worn ("kind"):
//   uma     a UMA wardrobe recipe (clothes and hair fitted to the body; asset = recipe name, umaSlot = Chest, Legs, ...)
//   prefab  a rigged or rigid prefab in Resources (asset = path), attached to a bone when rigid
//   proc    a built-in generated piece (chains, watches, rings, earrings, glasses, caps); asset = generator id
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace CFDream.Character
{
    public class WearableDef
    {
        public string key, kind = "none", asset, umaSlot, bone;
        public bool dyeable;
        public string[] with = new string[0];   // extra recipes worn together (the trousers that come with a suit)
        public string[] match = new string[0];  // uma with no asset yet: words to look for in the installed recipe names
        public Vector3 offset, euler; public float scale = 1;
    }

    public class BrandInfo { public string key, name, about, site; public bool partner; public int items; }

    public static class Wardrobe
    {
        static readonly Dictionary<string, WearableDef> defs = new Dictionary<string, WearableDef>();
        static readonly Dictionary<string, Meta> metas = new Dictionary<string, Meta>();
        static readonly Dictionary<string, string> cats = new Dictionary<string, string>();
        static readonly Dictionary<string, string> codes = new Dictionary<string, string>();
        public static readonly Dictionary<string, BrandInfo> Brands = new Dictionary<string, BrandInfo>();
        public static readonly List<string> Problems = new List<string>();
        static bool loaded;

        public static void Load()
        {
            if (loaded) return; loaded = true;
            var core = Resources.Load<TextAsset>("Dream/wardrobe");
            if (core != null)
            {
                var d = Json.Parse(core.text) as Dictionary<string, object>;
                if (d != null) foreach (var kv in d) if (kv.Value is Dictionary<string, object> w) defs[kv.Key] = ReadDef(kv.Key, w);
            }
            foreach (var ta in Resources.LoadAll<TextAsset>("Brands")) if (ta.name == "brand") LoadBrand(ta);
            if (Brands.Count > 0) Debug.Log($"[Dream] Brand packs: {string.Join(", ", Brands.Values.Select(b => $"{b.name} ({b.items})"))}");
            foreach (var p in Problems) Debug.LogWarning("[Dream] Brand pack: " + p);
        }

        public static WearableDef For(string slot, string id) => defs.TryGetValue(slot + ":" + id, out var d) ? d : null;
        public static Meta MetaFor(string slot, string id) => metas.TryGetValue(slot + ":" + id, out var m) ? m : null;
        public static string Category(string outfitId) => cats.TryGetValue(outfitId, out var c) ? c : null;
        public static IEnumerable<WearableDef> All => defs.Values;

        static WearableDef ReadDef(string key, Dictionary<string, object> w)
        {
            Vector3 V(string k, Vector3 def) { var l = J.Nums(J.L(w, k)); return l.Count >= 3 ? new Vector3((float)l[0], (float)l[1], (float)l[2]) : def; }
            return new WearableDef
            {
                key = key, kind = J.S(w, "kind", "none"), asset = J.S(w, "asset"), umaSlot = J.S(w, "umaSlot"), bone = J.S(w, "bone"),
                dyeable = J.B(w, "dyeable"), with = J.Strs(J.L(w, "with")).ToArray(), match = J.Strs(J.L(w, "match")).ToArray(),
                offset = V("offset", Vector3.zero), euler = V("euler", Vector3.zero), scale = (float)J.N(w, "scale", 1),
            };
        }

        static void LoadBrand(TextAsset ta)
        {
            Dictionary<string, object> d;
            try { d = Json.Parse(ta.text) as Dictionary<string, object>; }
            catch (Exception e) { Problems.Add($"a brand.json couldn't be read: {e.Message}"); return; }
            string key = J.S(d, "key");
            if (string.IsNullOrEmpty(key)) { Problems.Add("a brand.json has no \"key\""); return; }
            var info = new BrandInfo { key = key, name = J.S(d, "name", key), about = J.S(d, "about", ""), site = J.S(d, "site"), partner = J.B(d, "partner") };
            Brands[key] = info;
            foreach (var o in J.L(d, "items") ?? new List<object>())
            {
                string slot = J.S(o, "slot"), id = J.S(o, "id");
                if (string.IsNullOrEmpty(slot) || string.IsNullOrEmpty(id) || Array.IndexOf(Fashion.Wearable, slot) < 0 && slot != "home" && slot != "ride" && slot != "pet")
                { Problems.Add($"{key}: an item needs a known \"slot\" and an \"id\""); continue; }
                if (DB.Item(slot, id) != null) { Problems.Add($"{key}: {slot}:{id} already exists, pick another id"); continue; }
                var it = new ItemDef { slot = slot, id = id, name = J.S(o, "name", id), req = J.S(o, "req", "") };
                DB.Items.Add(it); info.items++;
                metas[it.Key] = new Meta { grade = J.S(o, "grade", "standard"), brandKey = key, brand = info.name, story = J.S(o, "story", ""), fx = J.S(o, "fx") };
                if (slot == "outfit") cats[id] = J.S(o, "category", "street");
                Career.AddRule(it.Key, J.S(o, "unlock", "redeem"));
                var w = J.O(o, "wear"); if (w != null) defs[it.Key] = ReadDef(it.Key, w);
            }
            var cs = J.O(d, "codes");
            if (cs != null) foreach (var kv in cs) if (kv.Value is string target) codes[kv.Key.ToLowerInvariant()] = target;
        }

        // ------------------------------------------------------------ redeem codes (the virtual twin of real merch)
        // brand.json stores only SHA-256 hashes of codes, so the codes can't be read out of the app. Codes are
        // case-insensitive and ignore spaces and dashes. A shipping game should confirm codes on a server so one
        // code works once; this offline check is for the beta.
        public static string Normalize(string code) => new string((code ?? "").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        public static string Hash(string code)
        {
            using (var sha = SHA256.Create())
            {
                var b = sha.ComputeHash(Encoding.UTF8.GetBytes(Normalize(code)));
                var sb = new StringBuilder(b.Length * 2); foreach (var x in b) sb.Append(x.ToString("x2")); return sb.ToString();
            }
        }
        public static (bool ok, string msg) Redeem(State s, string code)
        {
            if (Normalize(code).Length < 4) return (false, "That code is too short.");
            if (!codes.TryGetValue(Hash(code), out var key)) return (false, "That code didn't match anything.");
            if (s.owned.Contains(key)) return (false, "You already own that piece.");
            var p = key.Split(':'); var it = p.Length == 2 ? DB.Item(p[0], p[1]) : null;
            if (it == null) return (false, "That piece isn't in this version of the game yet. Update and try again.");
            var m = MetaFor(p[0], p[1]);
            s.owned.Add(key); Fashion.GotIt(s, key, "Redeemed" + (m != null && !string.IsNullOrEmpty(m.brand) ? ": " + m.brand : "")); s.newItems.Add(key);
            Career.Moment(s, "🏷️", $"Redeemed {it.name}");
            s.Save();
            return (true, it.name);
        }
    }
}
