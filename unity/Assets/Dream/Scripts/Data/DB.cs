// The Dream's content, loaded from Resources/Dream/dream_data.json (exported from the web build by
// unity/tools/export_dream.py) plus the pre-solved hand library in spots.json.
using System.Collections.Generic;
using UnityEngine;

namespace CFDream
{
    public class EventDef
    {
        public string id, kind, name, venue, title, boss, blurb, target, host, live;
        public int ch, field, seats, req;
        public double buyin, prizeBase, soft;
        public bool free, online, daily, turbo, reentry, invite;
        public List<string> legends = new List<string>();
        public List<string> stages; public List<int> perS;   // quick online games carry their own plan
        public EventDef Copy(string newName = null)
        {
            var e = (EventDef)MemberwiseClone();
            if (newName != null) e.name = newName;
            return e;
        }
        public static EventDef From(Dictionary<string, object> d)
        {
            var e = new EventDef
            {
                id = J.S(d, "id"), kind = J.S(d, "kind", "mtt"), name = J.S(d, "name", ""), venue = J.S(d, "venue", "cardroom"), title = J.S(d, "title"),
                boss = J.S(d, "boss"), blurb = J.S(d, "blurb", ""), target = J.S(d, "target"), ch = J.I(d, "ch"), field = J.I(d, "field"), seats = J.I(d, "seats"),
                req = J.I(d, "req"), buyin = J.N(d, "buyin"), prizeBase = J.N(d, "prizeBase"), free = J.B(d, "free"), online = J.B(d, "online"),
                daily = J.B(d, "daily"), turbo = J.B(d, "turbo"), reentry = J.B(d, "reentry"),
            };
            var lg = J.L(d, "legends"); if (lg != null) e.legends = J.Strs(lg);
            return e;
        }
    }

    public class ItemDef
    {
        public string slot, id, name, req, season, tier, live;
        public bool league;
        public string Key => slot + ":" + id;
    }

    public class LabelDef { public string id, n, icon, c, tier, line; public int score, life; }

    public class Beat
    {
        public string sp, t, who, mood;
        public bool big, text, goal, fn;
        public List<Choice> choice;
    }
    public class Choice { public string t, go; public Beat line; }
    public class Scene { public string id, place, title, sub, end; public List<Beat> beats = new List<Beat>(); }

    public static class DB
    {
        static Dictionary<string, object> root, data;
        public static List<EventDef> Events = new List<EventDef>();
        public static EventDef Daily;
        public static List<ItemDef> Items = new List<ItemDef>(), LeagueItems = new List<ItemDef>();
        public static Dictionary<string, LabelDef> Labels = new Dictionary<string, LabelDef>();
        public static Dictionary<string, Scene> Scenes = new Dictionary<string, Scene>();
        public static List<List<int[]>> ClassCombos = new List<List<int[]>>();
        public static List<string> ClassNames = new List<string>();
        static Dictionary<string, object> spots;

        public static bool Loaded => data != null;

        public static void Load()
        {
            if (data != null) return;
            var ta = Resources.Load<TextAsset>("Dream/dream_data");
            root = Json.Parse(ta.text) as Dictionary<string, object>;
            data = J.O(root, "data");
            foreach (var o in J.L(data, "DR_EVENTS")) Events.Add(EventDef.From((Dictionary<string, object>)o));
            Daily = EventDef.From(J.O(data, "DR_DAILY"));
            foreach (var o in J.L(data, "DR_ITEMS"))
                Items.Add(new ItemDef { slot = J.S(o, "slot"), id = J.S(o, "id"), name = J.S(o, "name"), req = J.S(o, "req", "") });
            foreach (var o in J.L(data, "DR_LG_ITEMS"))
                LeagueItems.Add(new ItemDef { slot = J.S(o, "slot"), id = J.S(o, "id"), name = J.S(o, "name"), req = "", season = J.S(o, "season"), tier = J.S(o, "tier"), league = true });
            foreach (var kv in J.O(data, "DR_LABELS"))
            {
                var o = kv.Value;
                Labels[kv.Key] = new LabelDef { id = kv.Key, n = J.S(o, "n"), icon = J.S(o, "icon"), c = J.S(o, "c"), tier = J.S(o, "tier"), line = J.S(o, "line"), score = J.I(o, "score"), life = J.I(o, "life") };
            }
            foreach (var kv in J.O(data, "DR_SCENES")) Scenes[kv.Key] = ParseScene(kv.Key, (Dictionary<string, object>)kv.Value);
            foreach (var o in J.L(root, "classes"))
            {
                ClassNames.Add(J.S(o, "name"));
                var combos = new List<int[]>();
                foreach (var c in J.L(o, "combos")) { var p = J.Nums(c); combos.Add(new[] { (int)p[0], (int)p[1] }); }
                ClassCombos.Add(combos);
            }
            Story.ApplyOverrides();
            ApplyScriptEdits();
            Character.Wardrobe.Load(); // built-in 3D pieces and brand packs (Resources/Brands/*/brand.json)
        }

        // the edited script (Resources/Dream/script_edits.json from unity/tools/script_import.py): words, display
        // names and choice labels laid over the scenes by scene id and line order
        static void ApplyScriptEdits()
        {
            var ta = Resources.Load<TextAsset>("Dream/script_edits"); if (ta == null) return;
            var ed = Json.Parse(ta.text) as Dictionary<string, object>; if (ed == null) return;
            int changed = 0;
            foreach (var kv in ed)
            {
                if (!Scenes.TryGetValue(kv.Key, out var sc)) continue;
                var lines = kv.Value as List<object>; if (lines == null) continue;
                for (int i = 0; i < lines.Count && i < sc.beats.Count; i++)
                {
                    var L = lines[i] as Dictionary<string, object>; var b = sc.beats[i]; if (L == null || b.fn) continue;
                    if (J.S(L, "sp") != b.sp) continue;   // the order drifted (a line was added or removed): leave the rest alone
                    string t = J.S(L, "t"); if (!string.IsNullOrEmpty(t) && t != b.t) { b.t = t; changed++; }
                    string who = J.S(L, "who"); if (!string.IsNullOrEmpty(who) && !string.Equals(who, DefaultName(b.sp), System.StringComparison.OrdinalIgnoreCase) && who != b.who) b.who = who;
                    var ch = J.L(L, "choice");
                    if (ch != null && b.choice != null)
                        for (int c = 0; c < ch.Count && c < b.choice.Count; c++)
                        {
                            var C = ch[c] as Dictionary<string, object>; if (C == null) continue;
                            string ct = J.S(C, "t"); if (!string.IsNullOrEmpty(ct)) b.choice[c].t = ct;
                            string cl = J.S(C, "line"); if (!string.IsNullOrEmpty(cl) && b.choice[c].line != null) b.choice[c].line.t = cl;
                        }
                }
            }
            if (changed > 0) UnityEngine.Debug.Log($"[Dream] script edits: {changed} lines changed");
        }
        static string DefaultName(string sp)
        {
            if (sp != null && sp.StartsWith("L:")) { var lg = Legend(sp.Substring(2)); return lg != null ? J.S(lg, "name", sp) : sp; }
            var p = Person(sp); return p != null ? J.S(p, "name", sp) : sp;
        }

        static Scene ParseScene(string id, Dictionary<string, object> d)
        {
            var sc = new Scene { id = id, place = J.S(d, "place", "diner"), title = J.S(d, "title"), sub = J.S(d, "sub"), end = J.S(d, "end", "hub") };
            foreach (var b in J.L(d, "beats")) sc.beats.Add(ParseBeat(b as Dictionary<string, object>));
            return sc;
        }
        public static Beat ParseBeat(Dictionary<string, object> b)
        {
            if (b == null) return null;
            var t = J.S(b, "t");
            var beat = new Beat { sp = J.S(b, "sp", "n"), t = t == "__fn" ? null : t, fn = t == "__fn", who = J.S(b, "who"), mood = J.S(b, "mood"), big = J.B(b, "big"), text = J.B(b, "text"), goal = J.B(b, "goal") };
            var ch = J.L(b, "choice");
            if (ch != null)
            {
                beat.choice = new List<Choice>();
                foreach (var c in ch) beat.choice.Add(new Choice { t = J.S(c, "t"), go = J.S(c, "go"), line = ParseBeat(J.O(c, "line")) });
            }
            return beat;
        }

        // ------------------------------------------------------------ lookups
        public static Dictionary<string, object> Data => data;
        public static Dictionary<string, object> Obj(string key) => J.O(data, key);
        public static List<object> List(string key) => J.L(data, key);
        public static EventDef Event(string id)
        {
            if (id == "daily") return Daily;
            foreach (var e in Events) if (e.id == id) return e;
            return null;
        }
        public static ItemDef Item(string slot, string id)
        {
            foreach (var it in Items) if (it.slot == slot && it.id == id) return it;
            foreach (var it in LeagueItems) if (it.slot == slot && it.id == id) return it;
            foreach (var it in Calendar.LiveItems) if (it.slot == slot && it.id == id) return it;
            return null;
        }
        public static string Place(string venue, string field = "name") => J.S(J.O(Obj("DR_PLACES"), venue), field, venue);
        public static Dictionary<string, object> Person(string id) => J.O(Obj("DR_PEOPLE"), id);
        public static Dictionary<string, object> Legend(string id)
        {
            foreach (var o in List("DR_LEGENDS")) if (J.S(o, "id") == id) return o as Dictionary<string, object>;
            return null;
        }
        public static string TitleName(string t) => J.S(J.O(Obj("DR_TITLES"), t), "n", t);
        public static string TitleIcon(string t) => J.S(J.O(Obj("DR_TITLES"), t), "icon", "🏆");
        public static string TitlePlural(string t) => J.S(J.O(Obj("DR_TITLES"), t), "plural", t);
        public static Dictionary<string, object> Chapter(int ch) { var l = List("DR_CHAPTERS"); return l[Mathf.Clamp(ch, 0, l.Count - 1)] as Dictionary<string, object>; }
        public static string StageLong(string stage) => J.S(J.O(Obj("DR_STAGE"), stage), "long", stage);
        public static string StageGid(string stage) => J.S(J.O(Obj("DR_STAGE"), stage), "gid", "ring");
        public static List<string> Tips(string stage) { var l = J.L(Obj("DR_TIPS"), stage) ?? J.L(Obj("DR_TIPS"), "early"); return J.Strs(l); }
        public static List<string> Strings(string key) => J.Strs(List(key));
        public static int Int(string key) => (int)J.Num(data[key]);
        public static Dictionary<string, object> Payouts(int field) => J.O(J.O(root, "payouts"), field.ToString());

        // ------------------------------------------------------------ the pre-solved library
        public static Dictionary<string, object> Spots
        {
            get
            {
                if (spots == null) spots = Json.Parse(Resources.Load<TextAsset>("Dream/spots").text) as Dictionary<string, object>;
                return spots;
            }
        }
        public static List<object> Tables(string gid) => J.L(Spots, gid) ?? new List<object>();
    }
}
