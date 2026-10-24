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
        static readonly HashSet<string> Required = new HashSet<string> { "outfit", "bottom", "shoes", "home", "ride" };

        public static bool ShowInfo; // the details panel for what's worn in this category
        static readonly Dictionary<string, string> CatIcon = new Dictionary<string, string>
        {
            { "shirts", "shirts" }, { "street", "street" }, { "jackets", "jacket" }, { "formal", "formal" }, { "bottom", "bottom" }, { "shoes", "shoes" }, { "head", "head" }, { "eyes", "eyes" }, { "chain", "chain" },
            { "ear", "ear" }, { "ring", "ring" }, { "wrist", "wrist" }, { "watch", "watch" }, { "patch", "patch" }, { "sleeve", "sleeve" }, { "home", "home" }, { "ride", "ride" }, { "pet", "pet" },
        };

        // the closet, the way a career-mode locker works: you on top, Look 1 / Look 2, then a grid of slots each showing
        // what's worn there; tap a slot for everything you own in it (with sub-tabs for tops) and an Equip.
        // Game.ClosetCat: "" = the slot grid, else a category key from DR_CATS.
        static readonly string[] SlotOrder = { "outfit", "bottom", "shoes", "head", "eyes", "chain", "watch", "ring", "wrist", "ear", "patch", "sleeve", "pet", "home", "ride" };
        static readonly Dictionary<string, string> SlotLabel = new Dictionary<string, string>
        { { "outfit", "Top" }, { "bottom", "Bottom" }, { "shoes", "Shoes" }, { "head", "Head" }, { "eyes", "Face" }, { "chain", "Chain" }, { "watch", "Watch" }, { "ring", "Ring" }, { "wrist", "Bracelet" }, { "ear", "Earrings" }, { "patch", "Chest patch" }, { "sleeve", "Sleeve patch" }, { "pet", "Pet" }, { "home", "Home" }, { "ride", "Ride" } };
        static readonly string[] TopCats = { "all", "shirts", "street", "jackets", "formal" };
        static readonly Dictionary<string, string> TopCatLabel = new Dictionary<string, string> { { "all", "All" }, { "shirts", "Tops" }, { "street", "Streetwear" }, { "jackets", "Jackets" }, { "formal", "Formal" } };
        public static string Sub = "all";   // the sub-tab inside Top

        public static VisualElement Build(State s)
        {
            bool land = DreamApp.Landscape;
            string cur = Game.ClosetCat;
            string slot = SlotOfCat(cur);

            var head = Bits.PageHead("Closet", s, Bits.FitChip(s), Bits.IconBtn("tag", () => Redeem(s), "Redeem a code"), Bits.IconBtn("face", Game.StartRestyle, "Restyle your look"));
            var stage = Stage(s, land ? -1 : 290);
            var looks = LookTabs(s);
            VisualElement body = string.IsNullOrEmpty(cur) ? SlotGrid(s) : SlotPage(s, cur, slot);

            VisualElement root;
            if (land)
            {
                var row = U.Row().Cls("two-pane grow");
                var left = U.Col().Cls("pane-left"); var hp = U.Div("pad-x"); hp.Add(head); left.Add(hp); left.Add(stage); row.Add(left);
                var sv = Screens.Scroll(); var pad = U.Div("pad"); pad.Add(looks); pad.Add(body); pad.Add(U.Space(24)); sv.Add(pad); row.Add(sv);
                root = row;
            }
            else
            {
                var sv = Screens.Scroll(); var b = U.Div("pad-top");
                var hp = U.Div("pad-x"); hp.Add(head); b.Add(hp); b.Add(stage);
                var pad = U.Div("pad"); pad.Add(looks); pad.Add(body); pad.Add(U.Space(24)); b.Add(pad);
                sv.Add(b); root = sv;
            }

            // seen: clear the new badges for what's on screen
            if (!string.IsNullOrEmpty(slot))
            {
                int before = s.newItems.Count;
                s.newItems.RemoveAll(k => k.StartsWith(slot + ":") && (slot != "outfit" || Sub == "all" || CatOf(k.Substring(7)) == Sub));
                if (s.newItems.Count != before) s.Save();
            }
            return root;
        }
        static string SlotOfCat(string cat)
        {
            if (string.IsNullOrEmpty(cat)) return null;
            foreach (var o in DB.List("DR_CATS") ?? new List<object>()) if (o is List<object> a && a.Count >= 3 && a[0] as string == cat) return a[2] as string;
            return cat;
        }
        static void Open(string slot) { Game.ClosetCat = slot == "outfit" ? "shirts" : slot; if (slot == "outfit") Sub = "all"; ShowInfo = false; Game.Render(); }
        static void Back() { Game.ClosetCat = ""; ShowInfo = false; Game.Render(); }

        // Look 1 / Look 2, plus shuffle
        static VisualElement LookTabs(State s)
        {
            var row = U.Row().Cls("looks");
            int on = Fashion.Look(s);
            for (int i = 0; i < Fashion.Looks; i++)
            {
                int k = i; var t = U.Div("look-tab").Cls(k == on ? "on" : "");
                t.Add(U.T($"Look {i + 1}", "look-t", k == on ? Fonts.Semi : Fonts.Medium).NoPick());
                if (k != on) t.Tap(() => Game.LookSwitch(k));
                row.Add(t);
            }
            row.Add(U.Spring());
            var sh = U.Row().Cls("look-tab", "ghost"); sh.Add(new Icon("sculpt", 15, C.Gold)); sh.Add(U.T("Shuffle", "look-t", Fonts.Medium).NoPick()); sh.Tap(Game.Randomize); row.Add(sh);
            return row;
        }

        // a picture of the piece: the rendered garment when there is one, else the drawn icon
        public static VisualElement Thumb(State s, string slot, string id, float w, float h)
        {
            string key = (slot == "outfit" ? "top" : slot) + "_" + id;
            var tex = id == "none" || string.IsNullOrEmpty(id) ? null : Resources.Load<Texture2D>("Dream/Thumbs/" + key);
            if (tex == null) return Art(s, slot, id, w, h);
            var e = U.Div("thumb").NoPick(); e.style.width = w; e.style.height = h;
            e.style.backgroundImage = new StyleBackground(tex); e.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            return e;
        }

        // ------------------------------------------------------------ every slot at a glance
        static VisualElement SlotGrid(State s)
        {
            var tiles = new List<VisualElement>();
            foreach (var slot in SlotOrder)
            {
                if (!DB.Items.Any(i => i.slot == slot)) continue;
                string id = s.Eq(slot); bool has = id != "none" && !string.IsNullOrEmpty(id);
                bool fresh = s.newItems.Any(k => k.StartsWith(slot + ":"));
                var art = has ? Thumb(s, slot, id, 84, 70) : (VisualElement)new Icon("plus", 26, C.Mute);
                var it = has ? DB.Item(slot, id) : null;
                string sl = slot;
                var t = Bits.Tile(art, SlotLabel.TryGetValue(slot, out var lb) ? lb : slot, false, () => Open(sl), has ? (it?.name ?? id) : "Nothing", has ? (Color?)null : C.Mute, "slot-tile");
                if (fresh) t.Add(U.T("NEW", "tile-new", Fonts.Bold).NoPick());
                tiles.Add(t);
            }
            return Bits.Tiles(tiles).Mt(10);
        }

        // ------------------------------------------------------------ one slot: back, what's on, sub-tabs, the grid
        static VisualElement SlotPage(State s, string cat, string slot)
        {
            var c = U.Col();
            var top = U.Row().Cls("slot-head");
            var back = U.Row().Cls("back-b"); back.Add(new Icon("chev-l", 16, C.Gold)); back.Add(U.T("All slots", "back-t", Fonts.Semi).NoPick()); back.Tap(Back); top.Add(back);
            top.Add(U.Spring());
            top.Add(U.T(SlotLabel.TryGetValue(slot, out var lb) ? lb : slot, "slot-title", Fonts.Semi).NoPick());
            c.Add(top);
            if (slot == "outfit")
            {
                var sv = new ScrollView(ScrollViewMode.Horizontal); sv.AddToClassList("rail"); sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden; sv.verticalScrollerVisibility = ScrollerVisibility.Hidden;
                foreach (var k in TopCats)
                {
                    bool on = Sub == k; string kk = k;
                    var chip = U.Row().Cls("rail-b", on ? "on" : ""); chip.Add(U.T(TopCatLabel[k], "rail-t", on ? Fonts.Semi : Fonts.Medium).NoPick());
                    chip.Tap(() => { Sub = kk; Game.Render(); }); sv.Add(chip);
                }
                c.Add(sv);
            }
            c.Add(Wearing(s, slot, s.Eq(slot)));
            c.Add(Items(s, slot == "outfit" ? Sub : cat, slot));
            return c;
        }
        static string CatOf(string outfitId) => Character.Wardrobe.Category(outfitId) ?? J.S(DB.Obj("DR_OUTFIT_CAT"), outfitId, "shirts");

        static Color? DyeOf(State s, string slot, string id) => (slot == "outfit" || slot == "head") && s.dye.TryGetValue(id, out var di) && di > 0 && di < Fashion.Dyes.Length && Fashion.Dyes[di] != null ? C.H(Fashion.Dyes[di]) : (Color?)null;
        static Color MetalOf(State s, string slot, string id)
        {
            if (s.fin.TryGetValue(slot + ":" + id, out var f) && f >= 0 && f < Fashion.Finishes.Length) return C.H(Fashion.Finishes[f].c);
            return id == "silver" || id == "steel" || id == "iced" || id == "diamond" || id == "tennis" ? C.H("#dfe3ea") : C.H("#e3b94f");
        }
        public static ItemArt Art(State s, string slot, string id, float w, float h) => new ItemArt(slot, id, DyeOf(s, slot, id), MetalOf(s, slot, id), w, h);

        // ------------------------------------------------------------ the stage: the fit on you, what's worn at a glance
        static readonly (string slot, string icon)[] Quick = { ("outfit", "tops"), ("chain", "chain"), ("watch", "watch"), ("head", "head") };
        static VisualElement Stage(State s, float h)
        {
            var st = StageView.Make(Fig.Of(s), () => Character.CharacterSpec.From(s), h, "full", out _);
            var q = U.Col().Cls("quick");
            foreach (var (slot, icon) in Quick)
            {
                string id = s.Eq(slot); bool has = id != "none" && !string.IsNullOrEmpty(id);
                var b = U.Div("quick-b").Cls(has ? "has" : "");
                if (has) b.Add(Art(s, slot, id, 30, 26)); else b.Add(new Icon("plus", 16, C.Mute));
                string sl = slot; b.Tap(() => Open(sl));
                q.Add(b);
            }
            st.Add(q);
            st.Add(SetPanel(s));
            return st;
        }

        // the maker you're closest to a set bonus with
        static VisualElement SetPanel(State s)
        {
            var makers = new Dictionary<string, int>();
            foreach (var (slot, id) in Fashion.Worn(s))
            {
                var m = Fashion.MetaOf(slot, id);
                if (!string.IsNullOrEmpty(m.brandKey) && m.brandKey != "league") makers[m.brandKey] = (makers.TryGetValue(m.brandKey, out var c) ? c : 0) + 1;
            }
            var p = U.Div("stage-panel", "set-panel");
            if (makers.Count == 0) { p.Add(U.T("Wear 3 pieces from one maker for a set bonus", "panel-r")); return p; }
            var best = makers.OrderByDescending(kv => kv.Value).First();
            string name = J.S(DB.Obj("DR_BRANDS"), best.Key, Character.Wardrobe.Brands.TryGetValue(best.Key, out var bi) ? bi.name : best.Key);
            p.Add(U.T(name + " set", "panel-k", Fonts.Semi));
            var r = U.Row().Mt(6);
            for (int i = 0; i < 3; i++) r.Add(U.Div("set-pip").Cls(i < best.Value ? "on" : ""));
            r.Add(U.T(best.Value >= 3 ? $"Set bonus · {best.Value} pieces" : $"{best.Value} of 3 for a bonus", "panel-r").Mt(0));
            p.Add(r);
            return p;
        }

        // ------------------------------------------------------------ what's on in this category, with its details
        static VisualElement Wearing(State s, string slot, string id)
        {
            var c = U.Div("wearing");
            if (id == "none" || string.IsNullOrEmpty(id)) { c.Add(U.Hint("Nothing on. Tap a piece to wear it.")); return c; }
            var it = DB.Item(slot, id); var m = Fashion.MetaOf(slot, id); var g = Fashion.Grade(m.grade); var gc = C.H(g.color);
            var row = U.Row();
            var l = U.Col().Grow();
            l.Add(U.T("Wearing", "wearing-k", Fonts.Semi));
            l.Add(U.T(it?.name ?? id, "wearing-n", Fonts.Semi));
            l.Add(U.T((string.IsNullOrEmpty(m.brand) ? "" : m.brand + " · ") + g.name, "wearing-g", Fonts.Medium).Fg(gc));
            row.Add(l);
            var more = U.Row().Cls("more-b");
            more.Add(U.T(ShowInfo ? "Less" : "Details", "more-t", Fonts.Semi).NoPick());
            more.Add(new Icon(ShowInfo ? "chev-l" : "chev-r", 14, C.Gold));
            more.Tap(() => { ShowInfo = !ShowInfo; Game.Render(); });
            row.Add(more);
            c.Add(row);
            if (ShowInfo) c.Add(Inspect(s, slot, id));
            return c;
        }

        // ------------------------------------------------------------ saved fits and redeem codes (sheets)
        static void Fits(State s)
        {
            DreamApp.Sheet("Saved fits", box =>
            {
                box.Add(U.Hint("Save what you're wearing, then switch back in one tap.").Mt(4));
                var g = U.Col().Mt(12);
                for (int i = 0; i < Fashion.FitSlots; i++)
                {
                    int k = i; bool has = i < s.fits.Count && s.fits[i] != null;
                    var r = U.Row().Cls("fit-row");
                    r.Add(U.T($"Fit {i + 1}", "fit-row-t", Fonts.Semi).Grow());
                    if (has) r.Add(U.Btn("Wear", () => { DreamApp.CloseModal(); Game.FitWear(k); DreamApp.Toast($"Wearing fit {k + 1}"); }, "gold small"));
                    r.Add(U.Btn(has ? "Save over" : "Save", () => { DreamApp.CloseModal(); Game.FitSave(k); DreamApp.Toast($"Saved fit {k + 1}"); }, "small"));
                    g.Add(r);
                }
                box.Add(g);
            });
        }

        static void Redeem(State s)
        {
            DreamApp.Sheet("Redeem a code", box =>
            {
                box.Add(U.Hint("Codes come with real Confirmed Fire merch and partner drops. Each one unlocks the piece in your closet.").Mt(4));
                var tf = new TextField { maxLength = 32 }; tf.AddToClassList("name-field"); tf.AddToClassList("code-field"); Fonts.Use(tf, Fonts.MonoB);
                box.Add(tf);
                var msg = U.Hint("").Mt(6); box.Add(msg);
                box.Add(U.Btn("Redeem", () =>
                {
                    var (ok, text) = Character.Wardrobe.Redeem(s, tf.value);
                    if (ok) { DreamApp.CloseModal(); DreamApp.Toast("Unlocked: " + text); Game.Render(); }
                    else { msg.text = text; msg.style.color = C.Red; }
                }, "gold wide").Mt(10));
                tf.schedule.Execute(() => tf.Focus()).StartingIn(50);
            });
        }

        // ------------------------------------------------------------ what you're wearing in this slot
        static VisualElement Inspect(State s, string slot, string id)
        {
            var c = U.Div("inspect");
            var it = DB.Item(slot, id); var m = Fashion.MetaOf(slot, id);
            string key = slot + ":" + id;
            if (m.fx != null) c.Add(U.Chip("Animated", C.Violet).Mt(8));
            if (!string.IsNullOrEmpty(m.story)) c.Add(U.P(m.story).Cls("story-t").Mt(8));
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

        // ------------------------------------------------------------ everything in the category, as pictures
        static VisualElement Items(State s, string cat, string slot)
        {
            bool all = slot != "outfit" || cat == "all";
            var list = DB.Items.Where(i => i.slot == slot && (all || CatOf(i.id) == cat)).ToList();
            list.AddRange(DB.LeagueItems.Where(i => i.slot == slot && s.Owns(i.slot, i.id) && (all || CatOf(i.id) == cat)));
            list.AddRange(Calendar.LiveItems.Where(i => i.slot == slot && s.Owns(i.slot, i.id)));
            var tiles = new List<VisualElement>();
            if (!Required.Contains(slot) && !list.Any(i => i.id == "none")) tiles.Add(ItemTile(s, slot, new ItemDef { slot = slot, id = "none", name = "Nothing" }, true));
            foreach (var it in list.OrderByDescending(i => i.id == "none" || s.Owns(i.slot, i.id)).ThenBy(i => Array.IndexOf(Fashion.GradeOrder, Fashion.MetaOf(i.slot, i.id).grade)))
                tiles.Add(ItemTile(s, slot, it, it.id == "none" || s.Owns(it.slot, it.id) || (slot == "outfit" && it.id == "office")));
            return Bits.Tiles(tiles).Mt(12);
        }

        static VisualElement ItemTile(State s, string slot, ItemDef it, bool owned)
        {
            var m = Fashion.MetaOf(slot, it.id); var g = Fashion.Grade(m.grade); var gc = C.H(g.color);
            bool on = s.Eq(slot) == it.id, fresh = s.newItems.Contains(slot + ":" + it.id), none = it.id == "none";
            string sub = none ? "" : !owned ? (it.req ?? "Locked") : slot == "home" && Calendar.HomeCost.TryGetValue(it.id, out var hc) ? $"{U.Money(hc)}/mo"
                : slot == "ride" && Calendar.RideCost.TryGetValue(it.id, out var rc) ? $"{U.Money(rc)}/mo" : g.name;
            var art = Thumb(s, slot, it.id, 84, 70);
            if (!owned) art.style.opacity = 0.25f;
            Action tap = null;
            if (on) tap = () => { ShowInfo = !ShowInfo; Game.Render(); };
            else if (owned)
            {
                if (slot == "home" || slot == "ride")
                {
                    double Cost(string iid) { var tbl = slot == "home" ? Calendar.HomeCost : Calendar.RideCost; return tbl.TryGetValue(iid ?? "", out var v) ? v : 0; }
                    double after = Calendar.Bills(s) - Cost(s.Eq(slot)) + Cost(it.id);
                    tap = () => DreamApp.Confirm(slot == "home" ? "Move in?" : "Switch rides?", $"Your bills become {U.Money(after)} a month, due on the 1st.", "Do it", () => Game.Equip(slot, it.id), "Not yet");
                }
                else tap = () => Game.Equip(slot, it.id);
            }
            var t = Bits.Tile(art, it.name, on, tap, sub, !owned ? C.Mute : none ? (Color?)null : gc, owned ? "" : "locked");
            if (!none) { var strip = U.Div("tile-strip").Bg(gc).NoPick(); t.Add(strip); }
            if (!owned) { var lk = U.Div("tile-lock").NoPick(); lk.Add(new Icon("lock", 20, C.Cream)); t.Add(lk); }
            if (fresh) t.Add(U.T("NEW", "tile-new", Fonts.Bold).NoPick());
            return t;
        }
    }
}
