// The saved career. Everything a player has done lives here and is written to PlayerPrefs as JSON.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CFDream
{
    public class Look
    {
        public int skin = 2, hairC = 1, eyeC;
        public string hair = "short", facial = "none", brows = "natural", face = "oval", age = "young";
        public bool freck;
        public Dictionary<string, double> sc = new Dictionary<string, double>();
        public Look Clone() { var l = (Look)MemberwiseClone(); l.sc = new Dictionary<string, double>(sc); return l; }
    }
    public class EventRec { public int played, cashes, wins, best; public double cash, net; }
    public class WearRec { public int g, e, c, f, w; public List<string> t = new List<string>(); }
    public class Got { public double d; public string at; }
    public class Moment { public double d; public string icon, text; }
    public class Trophy { public string t, ev; public double prize, d; }
    public class Invite { public EventDef ev; public int d; }
    public class Meetup { public string kind, id; public double amt; public int d; }
    public class Fit { public Dictionary<string, string> eq = new Dictionary<string, string>(); public Dictionary<string, int> dye = new Dictionary<string, int>(), fin = new Dictionary<string, int>(); }
    public class Quest { public string id; public int v; public bool done; }
    public class MisSpot { public string g, p, lab; public int t, n, c; public double d; }

    public class Progress
    {
        public double xp;
        public Dictionary<string, int> st = new Dictionary<string, int>();
        public List<string> days = new List<string>();
        public Dictionary<string, List<int>> sk = new Dictionary<string, List<int>>();
        public Dictionary<string, int> ach = new Dictionary<string, int>();
        public HashSet<string> cos = new HashSet<string> { "back:classic", "felt:green", "frame:none", "title:none" };
        public Dictionary<string, string> eq = new Dictionary<string, string> { { "back", "classic" }, { "felt", "green" }, { "frame", "none" }, { "title", "none" } };
        public string qDay; public bool qBonus;
        public List<Quest> q = new List<Quest>();
        public List<MisSpot> mis = new List<MisSpot>();
        public int St(string k) => st.TryGetValue(k, out var v) ? v : 0;
        public void Bump(string k, int by = 1) { st[k] = St(k) + by; }
    }

    public class CalState
    {
        public int d;
        public Dictionary<int, int> used = new Dictionary<int, int>(), wk = new Dictionary<int, int>();
        public Dictionary<int, List<string>> trav = new Dictionary<int, List<string>>();
        public Dictionary<string, int> re = new Dictionary<string, int>();
        public Invite inv; public Meetup meet;
        public string seed = Guid.NewGuid().ToString("N").Substring(0, 10), paid = "";
    }

    public class State
    {
        public bool made;
        public string name = "", job, lastEvent;
        public Look look = new Look();
        public double br, peak, debt;
        public int ch, dec, perfect, good, close, mistakes, played, cashes, ft, streak, bestStreak;
        public HashSet<string> story = new HashSet<string>(), legends = new HashSet<string>(), met = new HashSet<string>(), owned = new HashSet<string>();
        public List<string> queue = new List<string>(), newItems = new List<string>();
        public Dictionary<string, int> titles = new Dictionary<string, int> { { "local", 0 }, { "room", 0 }, { "ring", 0 }, { "bracelet", 0 }, { "tour", 0 }, { "glo", 0 }, { "titan", 0 }, { "me", 0 } };
        public Dictionary<string, EventRec> events = new Dictionary<string, EventRec>();
        public Dictionary<string, double> flags = new Dictionary<string, double>();
        public Dictionary<string, string> eq = new Dictionary<string, string> { { "outfit", "office" }, { "bottom", "khakis" }, { "shoes", "sneakers" }, { "eyes", "readers" }, { "head", "none" }, { "ear", "none" }, { "ring", "none" }, { "wrist", "none" }, { "watch", "digital" }, { "chain", "none" }, { "home", "studio" }, { "ride", "bus" }, { "pet", "none" }, { "patch", "none" }, { "sleeve", "none" } };
        public Dictionary<string, int> dye = new Dictionary<string, int>(), fin = new Dictionary<string, int>(), tickets = new Dictionary<string, int>();
        public Dictionary<string, WearRec> wear = new Dictionary<string, WearRec>();
        public Dictionary<string, Got> got = new Dictionary<string, Got>();
        public List<Fit> fits = new List<Fit>();
        public List<Moment> timeline = new List<Moment>();
        public List<Trophy> trophies = new List<Trophy>();
        public Dictionary<string, List<double>> daily = new Dictionary<string, List<double>>(); // real date -> [place, prize]
        public Dictionary<string, List<string>> live = new Dictionary<string, List<string>>();
        public Progress pg = new Progress();
        public CalState cal = new CalState();

        public int Title(string t) => titles.TryGetValue(t, out var v) ? v : 0;
        public int TitleCount => titles.Values.Sum();
        public bool Flag(string k) => flags.TryGetValue(k, out var v) && v != 0;
        public double FlagN(string k) => flags.TryGetValue(k, out var v) ? v : 0;
        public void SetFlag(string k, double v = 1) { flags[k] = v; }
        public bool Owns(string slot, string id) => owned.Contains(slot + ":" + id);
        public string Eq(string slot) => eq.TryGetValue(slot, out var v) ? v : "none";
        public int Ticket(string id) => tickets.TryGetValue(id, out var v) ? v : 0;

        // ------------------------------------------------------------ save and load
        const string Key = "cfdream.save.v1";
        public static State Load()
        {
            try
            {
                var txt = PlayerPrefs.GetString(Key, "");
                if (string.IsNullOrEmpty(txt)) return new State();
                return FromDict(Json.Parse(txt) as Dictionary<string, object>) ?? new State();
            }
            catch (Exception e) { Debug.LogWarning("Dream save could not be read: " + e.Message); return new State(); }
        }
        public void Save() { PlayerPrefs.SetString(Key, Json.Write(ToDict())); PlayerPrefs.Save(); }
        public static void Erase() { PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); }

        static Dictionary<string, object> D() => new Dictionary<string, object>();
        static List<object> Ls<T>(IEnumerable<T> xs) => xs.Select(x => (object)x).ToList();
        static Dictionary<string, object> Map<T>(Dictionary<string, T> m) { var d = D(); foreach (var kv in m) d[kv.Key] = kv.Value; return d; }
        static Dictionary<string, object> MapI(Dictionary<int, int> m) { var d = D(); foreach (var kv in m) d[kv.Key.ToString()] = kv.Value; return d; }

        public Dictionary<string, object> ToDict()
        {
            var d = D();
            d["v"] = 1; d["made"] = made; d["name"] = name; d["job"] = job; d["lastEvent"] = lastEvent;
            d["look"] = LookDict(look);
            d["br"] = br; d["peak"] = peak; d["debt"] = debt; d["ch"] = ch;
            foreach (var (k, v) in new[] { ("dec", dec), ("perfect", perfect), ("good", good), ("close", close), ("mistakes", mistakes), ("played", played), ("cashes", cashes), ("ft", ft), ("streak", streak), ("bestStreak", bestStreak) }) d[k] = v;
            d["story"] = Ls(story); d["legends"] = Ls(legends); d["met"] = Ls(met); d["owned"] = Ls(owned); d["queue"] = Ls(queue); d["newItems"] = Ls(newItems);
            d["titles"] = Map(titles); d["flags"] = Map(flags); d["eq"] = Map(eq); d["dye"] = Map(dye); d["fin"] = Map(fin); d["tickets"] = Map(tickets);
            var ev = D(); foreach (var kv in events) ev[kv.Key] = new Dictionary<string, object> { { "played", kv.Value.played }, { "cashes", kv.Value.cashes }, { "wins", kv.Value.wins }, { "best", kv.Value.best }, { "cash", kv.Value.cash }, { "net", kv.Value.net } }; d["events"] = ev;
            var w = D(); foreach (var kv in wear) w[kv.Key] = new Dictionary<string, object> { { "g", kv.Value.g }, { "e", kv.Value.e }, { "c", kv.Value.c }, { "f", kv.Value.f }, { "w", kv.Value.w }, { "t", Ls(kv.Value.t) } }; d["wear"] = w;
            var g = D(); foreach (var kv in got) g[kv.Key] = new Dictionary<string, object> { { "d", kv.Value.d }, { "at", kv.Value.at } }; d["got"] = g;
            d["fits"] = fits.Select(f => (object)(f == null ? null : new Dictionary<string, object> { { "eq", Map(f.eq) }, { "dye", Map(f.dye) }, { "fin", Map(f.fin) } })).ToList();
            d["timeline"] = timeline.Select(m => (object)new Dictionary<string, object> { { "d", m.d }, { "icon", m.icon }, { "text", m.text } }).ToList();
            d["trophies"] = trophies.Select(t => (object)new Dictionary<string, object> { { "t", t.t }, { "ev", t.ev }, { "prize", t.prize }, { "d", t.d } }).ToList();
            var dl = D(); foreach (var kv in daily) dl[kv.Key] = Ls(kv.Value); d["daily"] = dl;
            var lv = D(); foreach (var kv in live) lv[kv.Key] = Ls(kv.Value); d["live"] = lv;
            // progression
            var p = D(); p["xp"] = pg.xp; p["st"] = Map(pg.st); p["days"] = Ls(pg.days); p["ach"] = Map(pg.ach); p["cos"] = Ls(pg.cos); p["eq"] = Map(pg.eq); p["qDay"] = pg.qDay; p["qBonus"] = pg.qBonus;
            var sk = D(); foreach (var kv in pg.sk) sk[kv.Key] = Ls(kv.Value); p["sk"] = sk;
            p["q"] = pg.q.Select(q => (object)new Dictionary<string, object> { { "id", q.id }, { "v", q.v }, { "done", q.done } }).ToList();
            p["mis"] = pg.mis.Select(m => (object)new Dictionary<string, object> { { "g", m.g }, { "t", m.t }, { "n", m.n }, { "c", m.c }, { "p", m.p }, { "lab", m.lab }, { "d", m.d } }).ToList();
            d["pg"] = p;
            // calendar
            var c = D(); c["d"] = cal.d; c["used"] = MapI(cal.used); c["wk"] = MapI(cal.wk); c["re"] = Map(cal.re); c["seed"] = cal.seed; c["paid"] = cal.paid;
            var tr = D(); foreach (var kv in cal.trav) tr[kv.Key.ToString()] = Ls(kv.Value); c["trav"] = tr;
            if (cal.inv != null) c["inv"] = new Dictionary<string, object> { { "d", cal.inv.d }, { "id", cal.inv.ev.id }, { "name", cal.inv.ev.name }, { "venue", cal.inv.ev.venue }, { "buyin", cal.inv.ev.buyin }, { "ch", cal.inv.ev.ch }, { "soft", cal.inv.ev.soft }, { "host", cal.inv.ev.host }, { "blurb", cal.inv.ev.blurb } };
            if (cal.meet != null) c["meet"] = new Dictionary<string, object> { { "kind", cal.meet.kind }, { "id", cal.meet.id }, { "amt", cal.meet.amt }, { "d", cal.meet.d } };
            d["cal"] = c;
            return d;
        }

        static Dictionary<string, object> LookDict(Look l)
        {
            var sc = D(); foreach (var kv in l.sc) sc[kv.Key] = kv.Value;
            return new Dictionary<string, object> { { "skin", l.skin }, { "hair", l.hair }, { "hairC", l.hairC }, { "facial", l.facial }, { "eyeC", l.eyeC }, { "brows", l.brows }, { "face", l.face }, { "age", l.age }, { "freck", l.freck }, { "sc", sc } };
        }
        public static Look LookFrom(object o)
        {
            var l = new Look(); if (!(o is Dictionary<string, object>)) return l;
            l.skin = J.I(o, "skin", 2); l.hair = J.S(o, "hair", "short"); l.hairC = J.I(o, "hairC", 1); l.facial = J.S(o, "facial", "none"); l.eyeC = J.I(o, "eyeC");
            l.brows = J.S(o, "brows", "natural"); l.face = J.S(o, "face", "oval"); l.age = J.S(o, "age", "young"); l.freck = J.B(o, "freck");
            var sc = J.O(o, "sc"); if (sc != null) foreach (var kv in sc) l.sc[kv.Key] = J.Num(kv.Value);
            return l;
        }

        static HashSet<string> Set(object o) => new HashSet<string>(J.Strs(o));
        static Dictionary<string, int> IntMap(object o, Dictionary<string, int> def = null)
        {
            var m = def ?? new Dictionary<string, int>();
            if (o is Dictionary<string, object> d) foreach (var kv in d) m[kv.Key] = (int)Math.Round(J.Num(kv.Value));
            return m;
        }
        static Dictionary<string, string> StrMap(object o, Dictionary<string, string> def = null)
        {
            var m = def ?? new Dictionary<string, string>();
            if (o is Dictionary<string, object> d) foreach (var kv in d) if (kv.Value is string s) m[kv.Key] = s;
            return m;
        }

        public static State FromDict(Dictionary<string, object> d)
        {
            if (d == null) return null;
            var s = new State();
            s.made = J.B(d, "made"); s.name = J.S(d, "name", ""); s.job = J.S(d, "job"); s.lastEvent = J.S(d, "lastEvent");
            s.look = LookFrom(d.TryGetValue("look", out var lk) ? lk : null);
            s.br = J.N(d, "br"); s.peak = J.N(d, "peak"); s.debt = J.N(d, "debt"); s.ch = J.I(d, "ch");
            s.dec = J.I(d, "dec"); s.perfect = J.I(d, "perfect"); s.good = J.I(d, "good"); s.close = J.I(d, "close"); s.mistakes = J.I(d, "mistakes");
            s.played = J.I(d, "played"); s.cashes = J.I(d, "cashes"); s.ft = J.I(d, "ft"); s.streak = J.I(d, "streak"); s.bestStreak = J.I(d, "bestStreak");
            s.story = Set(d.Val("story")); s.legends = Set(d.Val("legends")); s.met = Set(d.Val("met")); s.owned = Set(d.Val("owned"));
            s.queue = J.Strs(d.Val("queue")); s.newItems = J.Strs(d.Val("newItems"));
            s.titles = IntMap(d.Val("titles"), s.titles); s.eq = StrMap(d.Val("eq"), s.eq);
            s.dye = IntMap(d.Val("dye")); s.fin = IntMap(d.Val("fin")); s.tickets = IntMap(d.Val("tickets"));
            if (d.Val("flags") is Dictionary<string, object> fl) foreach (var kv in fl) s.flags[kv.Key] = kv.Value is bool b ? (b ? 1 : 0) : J.Num(kv.Value);
            if (d.Val("events") is Dictionary<string, object> ev) foreach (var kv in ev) s.events[kv.Key] = new EventRec { played = J.I(kv.Value, "played"), cashes = J.I(kv.Value, "cashes"), wins = J.I(kv.Value, "wins"), best = J.I(kv.Value, "best"), cash = J.N(kv.Value, "cash"), net = J.N(kv.Value, "net") };
            if (d.Val("wear") is Dictionary<string, object> w) foreach (var kv in w) s.wear[kv.Key] = new WearRec { g = J.I(kv.Value, "g"), e = J.I(kv.Value, "e"), c = J.I(kv.Value, "c"), f = J.I(kv.Value, "f"), w = J.I(kv.Value, "w"), t = J.Strs(J.L(kv.Value, "t")) };
            if (d.Val("got") is Dictionary<string, object> g) foreach (var kv in g) s.got[kv.Key] = new Got { d = J.N(kv.Value, "d"), at = J.S(kv.Value, "at", "") };
            if (d.Val("fits") is List<object> fits) foreach (var f in fits) s.fits.Add(f == null ? null : new Fit { eq = StrMap(J.O(f, "eq")), dye = IntMap(J.O(f, "dye")), fin = IntMap(J.O(f, "fin")) });
            if (d.Val("timeline") is List<object> tl) foreach (var m in tl) s.timeline.Add(new Moment { d = J.N(m, "d"), icon = J.S(m, "icon", ""), text = J.S(m, "text", "") });
            if (d.Val("trophies") is List<object> tp) foreach (var t in tp) s.trophies.Add(new Trophy { t = J.S(t, "t"), ev = J.S(t, "ev"), prize = J.N(t, "prize"), d = J.N(t, "d") });
            if (d.Val("daily") is Dictionary<string, object> dl) foreach (var kv in dl) s.daily[kv.Key] = J.Nums(kv.Value);
            if (d.Val("live") is Dictionary<string, object> lv) foreach (var kv in lv) s.live[kv.Key] = J.Strs(kv.Value);
            if (d.Val("pg") is Dictionary<string, object> p)
            {
                var pg = s.pg; pg.xp = J.N(p, "xp"); pg.st = IntMap(p.Val("st")); pg.days = J.Strs(p.Val("days")); pg.ach = IntMap(p.Val("ach"));
                var cos = Set(p.Val("cos")); if (cos.Count > 0) pg.cos = cos;
                pg.eq = StrMap(p.Val("eq"), pg.eq); pg.qDay = J.S(p, "qDay"); pg.qBonus = J.B(p, "qBonus");
                if (p.Val("sk") is Dictionary<string, object> sk) foreach (var kv in sk) pg.sk[kv.Key] = J.Nums(kv.Value).Select(x => (int)x).ToList();
                if (p.Val("q") is List<object> q) foreach (var x in q) pg.q.Add(new Quest { id = J.S(x, "id"), v = J.I(x, "v"), done = J.B(x, "done") });
                if (p.Val("mis") is List<object> mis) foreach (var x in mis) pg.mis.Add(new MisSpot { g = J.S(x, "g"), t = J.I(x, "t"), n = J.I(x, "n"), c = J.I(x, "c"), p = J.S(x, "p"), lab = J.S(x, "lab"), d = J.N(x, "d") });
            }
            if (d.Val("cal") is Dictionary<string, object> c)
            {
                var cal = s.cal; cal.d = J.I(c, "d"); cal.seed = J.S(c, "seed", cal.seed); cal.paid = J.S(c, "paid", "");
                if (c.Val("used") is Dictionary<string, object> u) foreach (var kv in u) cal.used[int.Parse(kv.Key)] = (int)J.Num(kv.Value);
                if (c.Val("wk") is Dictionary<string, object> wk) foreach (var kv in wk) cal.wk[int.Parse(kv.Key)] = (int)J.Num(kv.Value);
                if (c.Val("trav") is Dictionary<string, object> tr) foreach (var kv in tr) cal.trav[int.Parse(kv.Key)] = J.Strs(kv.Value);
                cal.re = IntMap(c.Val("re"));
                if (c.Val("inv") is Dictionary<string, object> iv)
                    cal.inv = new Invite { d = J.I(iv, "d"), ev = new EventDef { id = "inv", kind = "cash", invite = true, name = J.S(iv, "name", ""), venue = J.S(iv, "venue", "cardroom"), buyin = J.N(iv, "buyin"), ch = J.I(iv, "ch", 1), soft = J.N(iv, "soft"), host = J.S(iv, "host", ""), blurb = J.S(iv, "blurb", "") } };
                if (c.Val("meet") is Dictionary<string, object> mt) cal.meet = new Meetup { kind = J.S(mt, "kind"), id = J.S(mt, "id"), amt = J.N(mt, "amt"), d = J.I(mt, "d") };
            }
            return s;
        }
    }

    static class DictExt
    {
        public static object Val(this Dictionary<string, object> d, string k) => d.TryGetValue(k, out var v) ? v : null;
    }
}
