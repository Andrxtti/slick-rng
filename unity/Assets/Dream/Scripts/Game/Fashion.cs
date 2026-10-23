// Every piece has a grade, a maker, a line about what it says, a record of where you earned it and of what you
// did while wearing it. Wear a piece while you play well and it breaks in: new colourways and metal finishes,
// then your monogram, then a finish that catches the light. Nothing is bought.
using System;
using System.Collections.Generic;
using System.Linq;

namespace CFDream
{
    public class Meta { public string grade = "standard", brand = "", story = "", fx, brandKey; }

    public static class Fashion
    {
        public static readonly string[] Wearable = { "outfit", "eyes", "head", "ear", "chain", "ring", "wrist", "watch", "patch", "sleeve" };
        public static readonly int[] WearAt = { 0, 25, 100, 300 }; // good decisions made while wearing it
        public static readonly string[] WearN = { "New", "Broken in", "Signature", "Mastered" };
        public static readonly string[] WearGives = { "", "New colourways and metal finishes", "Your monogram, stitched or engraved", "A finish that catches the light" };
        public static readonly string[] GradeOrder = { "standard", "rare", "elite", "legendary", "iconic" };

        public static Meta MetaOf(string slot, string id)
        {
            var bm = Character.Wardrobe.MetaFor(slot, id); if (bm != null) return bm; // brand packs
            var m = J.L(DB.Obj("DR_META"), slot + ":" + id);
            if (m == null && slot == "patch" && id == "haunt") return new Meta { grade = "rare", brand = "Live event", story = "Live event exclusive: Haunted Riverside. Only during the real Halloween run." };
            if (m == null) return new Meta();
            string bk = m.Count > 1 ? m[1] as string : "";
            return new Meta { grade = m[0] as string, brandKey = bk, brand = J.S(DB.Obj("DR_BRANDS"), bk ?? "", ""), story = m.Count > 2 ? m[2] as string : "", fx = m.Count > 3 ? m[3] as string : null };
        }
        public static (string name, string color, int pts) Grade(string g)
        {
            var a = J.L(DB.Obj("DR_GRADE"), g) ?? J.L(DB.Obj("DR_GRADE"), "standard");
            return (a[0] as string, a[1] as string, (int)J.Num(a[2]));
        }

        // ------------------------------------------------------------ wear: what you did in it, and how broken in it is
        public static WearRec Rec(State s, string key) { if (!s.wear.TryGetValue(key, out var r)) s.wear[key] = r = new WearRec(); return r; }
        public static int WearLv(State s, string key)
        {
            int g = s.wear.TryGetValue(key, out var r) ? r.g : 0, l = 0;
            while (l < 3 && g >= WearAt[l + 1]) l++;
            return l;
        }
        public static List<(string slot, string id)> Worn(State s) => Wearable.Select(sl => (sl, s.Eq(sl))).Where(x => x.Item2 != "none" && !string.IsNullOrEmpty(x.Item2)).ToList();
        // the gear you sat down in counts for the whole event, even if the results unlock and equip something new
        public static void WearDecision(State s, bool good, Prog o, Run run)
        {
            if (run != null && run.worn == null) run.worn = Worn(s);
            if (!good) return;
            foreach (var (slot, id) in run?.worn ?? Worn(s))
            {
                string k = slot + ":" + id; var r = Rec(s, k); int before = WearLv(s, k); r.g++;
                int after = WearLv(s, k); if (after > before && o != null) o.wear.Add((slot, id, after));
            }
        }
        public static void WearEvent(State s, Result res, Run run, bool cashed, bool won, bool ft)
        {
            foreach (var (slot, id) in run.worn ?? Worn(s))
            {
                var r = Rec(s, slot + ":" + id); r.e++;
                if (cashed) r.c++; if (ft) r.f++;
                if (won) { r.w++; if (!string.IsNullOrEmpty(res.ev.title) || res.ev.kind == "mtt") { r.t.Insert(0, res.ev.name); if (r.t.Count > 5) r.t.RemoveAt(r.t.Count - 1); } }
            }
        }
        public static void GotIt(State s, string key, string at) { if (!s.got.ContainsKey(key)) s.got[key] = new Got { d = Career.Now(), at = at ?? "" }; }

        // ------------------------------------------------------------ colourways and metal finishes
        public static readonly string[] Dyes = { null, "#17171a", "#efeee9", "#24427e", "#b3262c", "#1e6b45", "#6b3fa0", "#d98a2b", "#8a8f98", "#e8a6c2", "#c9a03e", "#3cbcd0", "#5a3a22", "#efe3c8", "#b4874e", "#24402f", "#5e1b2d", "#46505e", "#0f2a44" };
        public const int DyeBasic = 13;
        public static readonly (string n, string c)[] Finishes = { ("Yellow gold", "#e3b94f"), ("White gold", "#e4e7ec"), ("Rose gold", "#e7a487"), ("Black chrome", "#2e2e34") };
        static readonly HashSet<string> MetalSlots = new HashSet<string> { "chain", "ring", "wrist", "watch", "ear" };
        static readonly HashSet<string> NoFinish = new HashSet<string> { "wrist:beads", "watch:digital", "chain:bones", "chain:lights" };
        static readonly HashSet<string> Dyeable = new HashSet<string> { "hoodie", "tee", "varsity", "bomber", "track", "cardigan", "vest", "fleece", "jersey", "polo", "cap", "snap", "beanie", "bucket" };
        public static bool HasFinish(string slot, string id) => MetalSlots.Contains(slot) && id != "none" && !NoFinish.Contains(slot + ":" + id);
        public static bool IsLeague(string slot, string id) => DB.LeagueItems.Any(x => x.slot == slot && x.id == id);
        public static List<(int i, string c, bool ok, bool prem)> DyeOpts(State s, string slot, string id)
        {
            if ((slot != "outfit" && slot != "head") || id == "none" || IsLeague(slot, id)) return null;
            int lv = WearLv(s, slot + ":" + id); bool basic = Dyeable.Contains(id);
            return Dyes.Select((c, i) => (i, c, i == 0 || (i < DyeBasic ? basic || lv >= 1 : lv >= 1), i >= DyeBasic)).ToList();
        }
        public static string Initials(string n)
        {
            var w = (n ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2).Select(x => char.ToUpperInvariant(x[0]).ToString());
            var s = string.Concat(w); return s.Length > 0 ? s : "CF";
        }

        // ------------------------------------------------------------ fit rating and maker sets
        public static (int score, string name, List<(string mk, int n)> sets) FitScore(State s)
        {
            double pts = 0; var makers = new Dictionary<string, int>();
            foreach (var (slot, id) in Worn(s))
            {
                var m = MetaOf(slot, id); int l = WearLv(s, slot + ":" + id);
                pts += Grade(m.grade).pts + new[] { 0, 1, 2, 4 }[l];
                if (!string.IsNullOrEmpty(m.brandKey) && m.brandKey != "league") makers[m.brandKey] = (makers.TryGetValue(m.brandKey, out var c) ? c : 0) + 1;
            }
            var sets = makers.Where(kv => kv.Value >= 3).Select(kv => (kv.Key, kv.Value)).ToList();
            pts += sets.Sum(x => 3 * x.Item2);
            int score = (int)Math.Max(1, Math.Min(99, Math.Round(pts * 99 / 140)));
            string name = score >= 92 ? "Iconic" : score >= 80 ? "Red carpet" : score >= 65 ? "Dripping" : score >= 45 ? "Sharp" : score >= 25 ? "Clean" : "Off the rack";
            return (score, name, sets);
        }

        // ------------------------------------------------------------ saved fits
        public const int FitSlots = 4;
        public static void FitSave(State s, int i)
        {
            while (s.fits.Count <= i) s.fits.Add(null);
            var f = new Fit(); foreach (var k in Wearable) f.eq[k] = s.Eq(k);
            f.dye = new Dictionary<string, int>(s.dye); f.fin = new Dictionary<string, int>(s.fin); s.fits[i] = f;
        }
        public static void FitWear(State s, int i)
        {
            if (i >= s.fits.Count || s.fits[i] == null) return; var f = s.fits[i];
            foreach (var k in Wearable) if (f.eq.TryGetValue(k, out var id) && (id == "none" || s.Owns(k, id))) s.eq[k] = id;
            foreach (var kv in f.dye) s.dye[kv.Key] = kv.Value; foreach (var kv in f.fin) s.fin[kv.Key] = kv.Value;
        }
    }
}
