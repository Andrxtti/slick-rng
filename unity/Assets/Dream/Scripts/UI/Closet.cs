// The closet: your fit and its rating, four saved fits, every category, and an inspect panel for what you're
// wearing (grade, maker, its story, where you earned it, how broken in it is, colourways and metal finishes).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFDream.UI
{
    public static class ClosetView
    {
        static readonly HashSet<string> Required = new HashSet<string> { "outfit", "home", "ride" };

        public static VisualElement Build(State s)
        {
            var col = U.Col();
            col.Add(FitCard(s));
            var cats = DB.List("DR_CATS") ?? new List<object>();
            var catRow = U.Row().Cls("wrap cats");
            string cur = Game.ClosetCat; string slot = "outfit";
            foreach (var o in cats)
            {
                var a = o as List<object>; if (a == null || a.Count < 3) continue;
                string key = a[0] as string, label = a[1] as string, sl = a[2] as string;
                if (key == cur) slot = sl;
                bool fresh = s.newItems.Any(k => k.StartsWith(sl + ":") && (sl != "outfit" || CatOf(k.Substring(7)) == key));
                var chip = Screens.Toggle(label + (fresh ? " •" : ""), key == cur, () => { Game.ClosetCat = key; Game.Render(); });
                catRow.Add(chip);
            }
            col.Add(catRow);
            col.Add(Inspect(s, slot, s.Eq(slot)));
            col.Add(Items(s, cur, slot));
            // seen: clear the new badges for this category
            int before = s.newItems.Count;
            s.newItems.RemoveAll(k => k.StartsWith(slot + ":") && (slot != "outfit" || CatOf(k.Substring(7)) == cur));
            if (s.newItems.Count != before) s.Save();
            return col;
        }
        static string CatOf(string outfitId) => Character.Wardrobe.Category(outfitId) ?? J.S(DB.Obj("DR_OUTFIT_CAT"), outfitId, "shirts");

        // ------------------------------------------------------------ your fit
        static VisualElement FitCard(State s)
        {
            var c = U.Card("fit");
            var row = U.Row();
            row.Add(new Portrait(Fig.Of(s), 120));
            var r = U.Col().Grow(); r.style.marginLeft = 12;
            var fs = Fashion.FitScore(s);
            r.Add(U.Kicker("Fit rating"));
            var sr = U.Row(); sr.Add(U.Num(fs.score.ToString(), "fit-score").Fg(fs.score >= 80 ? C.Gold2 : C.Cream)); var nm = U.T(fs.name, "fit-name", Fonts.Disp); nm.style.marginLeft = 8; sr.Add(nm); r.Add(sr);
            r.Add(U.Bar(fs.score / 99.0, fs.score >= 92 ? C.Violet : fs.score >= 80 ? C.Gold : C.Green, 5).Mt(4));
            foreach (var (mk, n) in fs.sets) r.Add(U.Hint($"{J.S(DB.Obj("DR_BRANDS"), mk, mk)} set · {n} pieces").Fg(C.Gold));
            if (fs.sets.Count == 0) r.Add(U.Hint("Wear 3+ pieces from one maker for a set bonus.").Mt(4));
            r.Add(U.Btn("Restyle face and hair", Game.StartRestyle, "ghost small").Mt(8));
            row.Add(r);
            c.Add(row);
            var fits = U.Row().Cls("fits").Mt(12);
            for (int i = 0; i < Fashion.FitSlots; i++)
            {
                int k = i; bool has = i < s.fits.Count && s.fits[i] != null;
                var f = U.Col().Cls("fitslot", has ? "has" : "");
                f.Add(U.T($"Fit {i + 1}", "fitslot-t", Fonts.Semi));
                if (has) f.Add(U.Btn("Wear", () => { Game.FitWear(k); DreamApp.Toast($"Wearing fit {k + 1}"); }, "small"));
                f.Add(U.Btn(has ? "Save over" : "Save", () => { Game.FitSave(k); DreamApp.Toast($"Saved fit {k + 1}"); }, "ghost small"));
                fits.Add(f);
            }
            c.Add(fits);
            return c;
        }

        // ------------------------------------------------------------ what you're wearing in this slot
        static VisualElement Inspect(State s, string slot, string id)
        {
            var c = U.Card("inspect");
            if (id == "none") { c.Add(U.Kicker("Nothing on")); c.Add(U.Hint("Pick something below.")); return c; }
            var it = DB.Item(slot, id); var m = Fashion.MetaOf(slot, id); var g = Fashion.Grade(m.grade); var gc = C.H(g.color);
            string key = slot + ":" + id;
            c.style.borderLeftColor = gc; c.style.borderLeftWidth = 4;
            var hd = U.Row();
            var l = U.Col().Grow();
            l.Add(U.Kicker((m.brand ?? "") + (string.IsNullOrEmpty(m.brand) ? "" : " · ") + g.name).Fg(gc));
            l.Add(U.H3(it?.name ?? id));
            hd.Add(l);
            if (m.fx != null) hd.Add(U.Chip("Animated", C.Violet));
            c.Add(hd);
            if (!string.IsNullOrEmpty(m.story)) c.Add(U.P(m.story).Cls("story-t").Mt(4));
            if (s.got.TryGetValue(key, out var got) && !string.IsNullOrEmpty(got.at)) c.Add(U.Hint($"Earned: {got.at} · {DateTimeOffset.FromUnixTimeMilliseconds((long)got.d).LocalDateTime:MMM d, yyyy}").Mt(6));
            else if (!string.IsNullOrEmpty(it?.req)) c.Add(U.Hint(it.req).Mt(6));
            if (slot == "home" && Calendar.HomeCost.TryGetValue(id, out var hc)) c.Add(U.Hint($"Costs {U.Money(hc)} a month").Fg(C.Gold));
            if (slot == "ride" && Calendar.RideCost.TryGetValue(id, out var rc)) c.Add(U.Hint($"Costs {U.Money(rc)} a month").Fg(C.Gold));

            if (Array.IndexOf(Fashion.Wearable, slot) >= 0)
            {
                int lv = Fashion.WearLv(s, key); s.wear.TryGetValue(key, out var w); w = w ?? new WearRec();
                var wr = U.Row().Mt(10); wr.Add(U.Chip(Fashion.WearN[lv], lv >= 3 ? C.Violet : lv >= 2 ? C.Gold : lv >= 1 ? C.Green : C.Mute)); wr.Add(U.Spring());
                wr.Add(U.Num(lv < 3 ? $"{w.g} / {Fashion.WearAt[lv + 1]}" : $"{w.g}", "wear-n")); c.Add(wr);
                if (lv < 3) { c.Add(U.Bar((w.g - Fashion.WearAt[lv]) / (double)Math.Max(1, Fashion.WearAt[lv + 1] - Fashion.WearAt[lv]), C.Green, 5).Mt(4)); c.Add(U.Hint($"Good decisions while wearing it. Next: {Fashion.WearGives[lv + 1]}.").Mt(2)); }
                var stats = U.Row().Cls("stats").Mt(8);
                stats.Add(U.Stat(w.e.ToString(), "Events")); stats.Add(U.Stat(w.c.ToString(), "Cashes")); stats.Add(U.Stat(w.f.ToString(), "Final tables")); stats.Add(U.Stat(w.w.ToString(), "Wins", w.w > 0 ? C.Gold2 : (Color?)null));
                c.Add(stats);
                if (w.t.Count > 0) c.Add(U.Hint("Won in it: " + string.Join(", ", w.t)).Mt(4).Fg(C.Gold));

                var dyes = Fashion.DyeOpts(s, slot, id);
                if (dyes != null)
                {
                    c.Add(U.Kicker("Colourway" + (lv < 1 && dyes.Any(x => !x.ok) ? " · more unlock when it's Broken in" : "")).Mt(10));
                    var sw = U.Row().Cls("wrap swatches");
                    int curDye = s.dye.TryGetValue(id, out var cd) ? cd : 0;
                    foreach (var x in dyes)
                    {
                        var e = U.Div("swatch").Cls(x.c == null ? "orig" : "", x.prem ? "prem" : "", x.i == curDye ? "on" : "", x.ok ? "" : "locked");
                        if (x.c != null) e.Bg(C.H(x.c)); else e.Add(U.T("✕", "orig-x", Fonts.Sym).NoPick());
                        int v = x.i; if (x.ok) e.Tap(() => Game.Dye(id, v));
                        sw.Add(e);
                    }
                    c.Add(sw);
                }
                if (Fashion.HasFinish(slot, id))
                {
                    c.Add(U.Kicker("Metal finish" + (lv < 1 ? " · unlocks when it's Broken in" : "")).Mt(10));
                    var fr = U.Row().Cls("wrap");
                    int curF = s.fin.TryGetValue(key, out var cf) ? cf : -1;
                    fr.Add(Screens.Toggle("Original", curF < 0, () => Game.SetFinish(slot, id, -1)));
                    for (int i = 0; i < Fashion.Finishes.Length; i++)
                    {
                        int k = i; var t = Screens.Toggle(Fashion.Finishes[i].n, curF == i, () => Game.SetFinish(slot, id, k));
                        t.Add(U.Div("fin-dot").Bg(C.H(Fashion.Finishes[i].c)).NoPick());
                        if (lv < 1) t.AddToClassList("off");
                        fr.Add(t);
                    }
                    c.Add(fr);
                }
                if (lv >= 2) c.Add(U.Hint($"Your monogram ({Fashion.Initials(s.name)}) is on this piece.").Mt(8).Fg(C.Gold));
            }
            return c;
        }

        // ------------------------------------------------------------ everything in the category
        static VisualElement Items(State s, string cat, string slot)
        {
            var list = DB.Items.Where(i => i.slot == slot && (slot != "outfit" || CatOf(i.id) == cat)).ToList();
            list.AddRange(DB.LeagueItems.Where(i => i.slot == slot && s.Owns(i.slot, i.id) && (slot != "outfit" || CatOf(i.id) == cat)));
            list.AddRange(Calendar.LiveItems.Where(i => i.slot == slot && s.Owns(i.slot, i.id)));
            var grid = U.Row().Cls("wrap items");
            if (!Required.Contains(slot) && !list.Any(i => i.id == "none")) grid.Add(ItemCard(s, slot, new ItemDef { slot = slot, id = "none", name = "Nothing" }, true));
            foreach (var it in list.OrderByDescending(i => i.id == "none" || s.Owns(i.slot, i.id)).ThenBy(i => Array.IndexOf(Fashion.GradeOrder, Fashion.MetaOf(i.slot, i.id).grade)))
                grid.Add(ItemCard(s, slot, it, it.id == "none" || s.Owns(it.slot, it.id) || (slot == "outfit" && it.id == "office")));
            return grid;
        }

        static VisualElement ItemCard(State s, string slot, ItemDef it, bool owned)
        {
            var m = Fashion.MetaOf(slot, it.id); var g = Fashion.Grade(m.grade); var gc = C.H(g.color);
            bool on = s.Eq(slot) == it.id, fresh = s.newItems.Contains(slot + ":" + it.id);
            var c = U.Col().Cls("item", on ? "on" : "", owned ? "" : "locked");
            var strip = U.Div("item-strip").Bg(it.id == "none" ? C.Line : gc); c.Add(strip);
            var top = U.Row();
            top.Add(U.T(it.id == "none" ? "" : g.name.ToUpperInvariant(), "item-grade", Fonts.Bold).Fg(gc).Grow());
            if (fresh) top.Add(U.Chip("New", C.Gold, true));
            else if (on) top.Add(U.Chip("On", C.Green));
            c.Add(top);
            c.Add(U.T(it.name, "item-n", Fonts.Semi));
            if (!string.IsNullOrEmpty(m.brand)) c.Add(U.Hint(m.brand));
            if (!owned) c.Add(U.Hint("🔒 " + (it.req ?? "Locked")).Cls("item-req"));
            if (slot == "home" && Calendar.HomeCost.TryGetValue(it.id, out var hc)) c.Add(U.Hint($"{U.Money(hc)}/mo"));
            if (slot == "ride" && Calendar.RideCost.TryGetValue(it.id, out var rc)) c.Add(U.Hint($"{U.Money(rc)}/mo"));
            if (owned && !on)
            {
                if (slot == "home" || slot == "ride")
                {
                    double Cost(string id) { var t = slot == "home" ? Calendar.HomeCost : Calendar.RideCost; return t.TryGetValue(id ?? "", out var v) ? v : 0; }
                    double after = Calendar.Bills(s) - Cost(s.Eq(slot)) + Cost(it.id);
                    c.Tap(() => DreamApp.Confirm(slot == "home" ? "Move in?" : "Switch rides?", $"Your bills become {U.Money(after)} a month, due on the 1st.", "Do it", () => Game.Equip(slot, it.id), "Not yet"));
                }
                else c.Tap(() => Game.Equip(slot, it.id));
            }
            return c;
        }
    }
}
