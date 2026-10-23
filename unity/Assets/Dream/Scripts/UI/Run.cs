// Playing an event: a level intro with Slick's tip, the table and your hand, your options, then the verdict
// (Chess.com-style label, the solver's mix, pot odds and a one-line lesson), coolers, and the results.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFDream.UI
{
    public static class RunView
    {
        public static VisualElement Build()
        {
            var run = Game.Run; if (run == null) { Game.View = View.Hub; return HubView.Build(); }
            if (DreamApp.Landscape && run.spot != null && (run.state == "play" || run.state == "answered")) return Wide(run);
            var page = Screens.Page("run");
            if (DreamApp.Landscape) page.AddToClassList("narrow");
            var sv = Screens.Scroll(); var body = U.Div("pad");
            body.Add(Header(run));
            switch (run.state)
            {
                case "ready": body.Add(Intro(run)); break;
                case "cooler": body.Add(Cooler(run)); break;
                default: body.Add(Hand(run)); break;
            }
            body.Add(U.Space(30));
            sv.Add(body); page.Add(sv);
            return page;
        }

        static VisualElement Header(Run run)
        {
            var h = U.Div("run-head");
            var r = U.Row();
            var l = U.Col().Grow(); l.style.marginRight = 52;
            l.Add(U.Kicker(run.ev.name)); l.Add(U.H3(DB.StageLong(run.Stage) + (run.turbo ? " · Turbo" : "")));
            r.Add(l);
            h.Add(r);
            int per = Play.Per(run);
            var m = U.Row().Mt(8);
            var stack = U.Col().Grow();
            var sl = U.Row(); sl.Add(U.T("STACK", "stat-l", Fonts.Bold).Grow()); sl.Add(U.Num(run.stack.ToString(), "stack-n").Fg(StackCol(run.stack))); stack.Add(sl);
            stack.Add(U.Bar(run.stack / 200.0, StackCol(run.stack), 7).Mt(3));
            m.Add(stack);
            var info = U.Col().Cls("run-info");
            info.Add(U.Num($"{Math.Min(run.h + 1, per)}/{per}", "")); info.Add(U.T("HAND", "stat-l", Fonts.Bold));
            m.Add(info);
            var lvl = U.Col().Cls("run-info");
            lvl.Add(U.Num($"{Math.Min(run.si + 1, run.stages.Count)}/{run.stages.Count}", "")); lvl.Add(U.T("LEVEL", "stat-l", Fonts.Bold));
            m.Add(lvl);
            h.Add(m);
            if (run.combo >= 3) h.Add(U.Chip($"🔥 {run.combo} in a row", C.H("#ff8a3c")).Mt(6));
            return h;
        }
        static Color StackCol(int st) => st >= 100 ? C.Green : st >= 50 ? C.Gold : C.Red;

        static VisualElement Intro(Run run)
        {
            var c = U.Card("intro");
            string stage = run.Stage; var tips = DB.Tips(stage);
            c.Add(U.Kicker(run.si == 0 ? DB.Place(run.ev.venue) : "Next level"));
            c.Add(U.H2(DB.StageLong(stage)));
            int per = Play.Per(run);
            c.Add(U.Hint($"{per} hand{(per == 1 ? "" : "s")} at this level" + (run.turbo ? " · turbo blinds: stacks get short fast" : "")));
            bool bossTime = run.boss != null && (stage == "final" || (run.ev.kind == "cash" && run.si == 0));
            if (bossTime)
            {
                var (name, role, col, fig) = Screens.Speaker(DB.Legend(run.boss) != null ? "L:" + run.boss : run.boss, null);
                var b = U.Row().Cls("boss").Mt(10);
                if (fig != null) b.Add(new Portrait(fig, 64));
                var bl = U.Col().Grow(); bl.style.marginLeft = 10;
                bl.Add(U.Kicker(stage == "final" ? "At the final table" : "At your table")); bl.Add(U.T(name, "speech-name", Fonts.Semi).Fg(col)); if (!string.IsNullOrEmpty(role)) bl.Add(U.Hint(role));
                var line = J.S(DB.Legend(run.boss), "line"); if (!string.IsNullOrEmpty(line)) bl.Add(U.P("“" + line + "”").Mt(4));
                b.Add(bl); c.Add(b);
            }
            if (tips.Count > 0)
            {
                var t = U.Row().Cls("tip").Mt(12);
                t.Add(new Portrait(Fig.Slick(), 40, false));
                var tl = U.Col().Grow(); tl.style.marginLeft = 8; tl.Add(U.T("Slick", "tip-who", Fonts.Semi).Fg(C.Blue)); tl.Add(U.P(tips[(run.si + run.results.Count + run.ev.name.Length) % tips.Count])); t.Add(tl);
                c.Add(t);
            }
            c.Add(U.Btn(run.si == 0 && run.results.Count == 0 ? "Take your seat" : "Deal", Game.Deal, "gold wide").Mt(14));
            Screens.Enter(c);
            return c;
        }

        static VisualElement Wide(Run run)
        {
            var page = Screens.Page("run two-pane");
            var left = U.Col().Cls("pane-left table-pane");
            left.Add(Header(run));
            var table = new TableView(run.spot); table.AddToClassList("table-wide"); left.Add(table);
            page.Add(left);
            var sv = Screens.Scroll(); sv.AddToClassList("pane-right");
            var body = U.Div("pad"); body.Add(Hand(run, false)); body.Add(U.Space(24));
            sv.Add(body); page.Add(sv);
            return page;
        }

        static VisualElement Hand(Run run, bool withTable = true)
        {
            var sp = run.spot; var col = U.Col(); if (sp == null) return col;
            bool answered = run.state == "answered"; var last = answered ? run.results.LastOrDefault() : null;
            if (withTable) col.Add(new TableView(sp));

            var hero = sp.seats[sp.hero];
            var hr = U.Row().Cls("hero-row");
            var cards = U.Row().Cls("hero-cards"); var a = CardView.Face(sp.hand[0], 62); var b = CardView.Face(sp.hand[1], 62); b.style.marginLeft = 6; cards.Add(a); cards.Add(b);
            hr.Add(cards);
            var hi = U.Col().Grow(); hi.style.marginLeft = 12;
            hi.Add(U.Kicker(sp.cat));
            hi.Add(U.T($"{hero.pos} · {Fmt.Bb(hero.stack - hero.put)} bb", "hero-pos", Fonts.Semi));
            var odds = Odds(sp); if (odds != null) hi.Add(U.Hint(odds));
            hr.Add(hi);
            col.Add(hr);

            if (!answered)
            {
                // actions side by side, like a real table: fold, call, raise
                var opts = U.Row().Cls("options act-row").Mt(14);
                for (int i = 0; i < sp.acts.Count; i++)
                {
                    int k = i; string lab = sp.Label(i);
                    var btn = U.Btn(lab, () => Game.Answer(k), "opt-btn " + ActCls(sp.acts[i]));
                    opts.Add(btn);
                }
                col.Add(opts);
            }
            else if (last != null) col.Add(Verdict(run, sp, last));
            return col;
        }

        static string ActCls(string a) => a == "fold" ? "act-fold" : a == "call" ? "act-call" : a == "allin" ? "act-jam" : "act-raise";

        static string Odds(Spot sp)
        {
            int ci = sp.acts.IndexOf("call"); if (ci < 0) return null;
            var m = System.Text.RegularExpressions.Regex.Match(sp.Label(ci), @"([\d.]+)");
            if (!m.Success || !double.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var amt0)) return null;
            double amt = amt0 - sp.seats[sp.hero].put; if (amt <= 0 || sp.pot <= 0) return null;
            return $"Call {Fmt.Bb(amt)} to win {Fmt.Bb(sp.pot)} · need {Math.Round(amt / (sp.pot + amt) * 100)}%";
        }

        static VisualElement Verdict(Run run, Spot sp, Decision r)
        {
            var L = DB.Labels[r.lab]; var lc = C.H(L.c);
            var c = U.Card("verdict");
            c.style.borderTopColor = lc; c.style.borderTopWidth = 3;
            var top = U.Row();
            var badge = U.Div("badge").Bg(lc); badge.Add(U.T(L.icon, "badge-t", Fonts.Sym).NoPick()); top.Add(badge);
            var tl = U.Col().Grow(); tl.style.marginLeft = 10;
            tl.Add(U.T(L.n, "verdict-n", Fonts.Disp).Fg(lc)); tl.Add(U.Hint(L.line));
            top.Add(tl);
            int delta = r.after - r.before;
            top.Add(U.Num((delta >= 0 ? "+" : "") + delta, "verdict-d").Fg(delta >= 0 ? C.Green : C.Red));
            c.Add(top);
            var h = r.hand;
            c.Add(U.P(h.mineK == h.bestK ? $"You: <b>{h.mine}</b>. That's the solver's top play here ({h.bestF}%)." : $"You: <b>{h.mine}</b> ({h.myF}%). Solver's top play: <b>{h.best}</b> ({h.bestF}%).").Mt(10));
            if (h.loss.HasValue && h.loss.Value > 0.005) c.Add(U.Hint($"Cost: {h.loss.Value:0.00} {(h.unit == "bb" ? "bb" : "bb of ICM equity")} per hand, on average.").Fg(r.tier == "bad" ? C.Red : C.Mute));

            c.Add(U.Kicker("The solver's mix").Mt(12));
            for (int i = 0; i < sp.acts.Count; i++)
            {
                double f = sp.strat[i];
                var row = U.Row().Cls("mix-row", i == r.j ? "mine" : "");
                row.Add(U.T(sp.Label(i), "mix-l", i == r.j ? Fonts.Semi : Fonts.Body).Grow());
                var bar = U.Bar(f, i == r.j ? lc : C.A(C.Cream, 0.55f), 8); bar.style.width = 110; bar.style.marginLeft = 8; bar.style.marginRight = 8;
                row.Add(bar); row.Add(U.Num(Math.Round(f * 100) + "%", "mix-p"));
                c.Add(row);
            }
            var tip = Play.Tip(r);
            if (!string.IsNullOrEmpty(tip))
            {
                var t = U.Row().Cls("tip").Mt(12);
                t.Add(new Portrait(Fig.Slick(), 36, false));
                var tt = U.Col().Grow(); tt.style.marginLeft = 8; tt.Add(U.T("Slick", "tip-who", Fonts.Semi).Fg(C.Blue)); tt.Add(U.P(tip)); t.Add(tt);
                c.Add(t);
            }
            int per = Play.Per(run);
            string next = run.bust ? "See where you finished" : run.h + 1 < per ? "Next hand" : run.si + 1 < run.stages.Count ? "Next level" : "Finish";
            c.Add(U.Btn(next, Game.NextHand, "gold wide").Mt(14));
            Screens.Enter(c);
            return c;
        }

        static VisualElement Cooler(Run run)
        {
            var c = U.Card("cooler");
            c.Add(U.Kicker("Cooler"));
            c.Add(U.H2("Nothing you could do"));
            c.Add(U.P(run.cooler ?? "").Mt(6));
            var lines = DB.Strings("DR_COOLER_LINES");
            if (lines.Count > 0)
            {
                var t = U.Row().Cls("tip").Mt(12); t.Add(new Portrait(Fig.Slick(), 36, false));
                var tt = U.Col().Grow(); tt.style.marginLeft = 8; tt.Add(U.T("Slick", "tip-who", Fonts.Semi).Fg(C.Blue)); tt.Add(U.P(lines[(run.results.Count + run.si) % lines.Count])); t.Add(tt);
                c.Add(t);
            }
            c.Add(U.Btn("See where you finished", () => Game.Finish(false), "gold wide").Mt(14));
            Screens.Enter(c);
            return c;
        }
    }

    public static class ResultsView
    {
        public static VisualElement Build()
        {
            var res = Game.Last; var s = Game.State;
            if (res == null) { Game.View = View.Hub; return HubView.Build(); }
            var ev = res.ev;
            var page = Screens.Narrow("results");
            var sv = Screens.Scroll(); var body = U.Div("pad");
            body.Add(U.Space(46)); // room for the Home button

            var hero = U.Card("res-hero");
            hero.Add(U.Kicker(ev.name));
            bool won = res.place == 1 && ev.kind != "cash";
            if (ev.kind == "cash")
            {
                hero.Add(U.T(U.Signed(res.net), "res-big", Fonts.Disp).Fg(res.net >= 0 ? C.Green : C.Red));
                hero.Add(U.P(res.net >= 0 ? "Booked a win." : res.bust ? "Felted." : "Booked a loss."));
            }
            else if (ev.kind == "sat")
            {
                hero.Add(U.T(res.ticket != null ? "Seat won" : Fmt.Ord(res.place), "res-big", Fonts.Disp).Fg(res.ticket != null ? C.Gold2 : C.Cream));
                hero.Add(U.P(res.ticket != null ? $"You're in the {DB.Event(res.ticket)?.name}." : $"of {ev.field:#,0}. {ev.seats} seats were up for grabs."));
            }
            else
            {
                hero.Add(U.T(Fmt.Ord(res.place), "res-big", Fonts.Disp).Fg(won ? C.Gold2 : res.prize > 0 ? C.Green : C.Cream));
                hero.Add(U.P($"of {ev.field:#,0}" + (res.prize > 0 ? $" · won {U.Money(res.prize)}" : res.place <= res.paid ? "" : $" · {res.paid} paid")));
            }
            if (!string.IsNullOrEmpty(res.cooler)) hero.Add(U.Hint("Cooler: " + res.cooler).Mt(4));
            var money = U.Row().Cls("stats").Mt(10);
            money.Add(U.Stat(U.Money(res.before), "Before")); money.Add(U.Stat(U.Money(res.after), "Bankroll now", res.after >= res.before ? C.Green : C.Red)); money.Add(U.Stat(res.acc + "%", "Accuracy"));
            hero.Add(money);
            if (res.repaid > 0) hero.Add(U.Hint($"Paid Slick back {U.Money(res.repaid)}." + (s.debt > 0 ? $" {U.Money(s.debt)} to go." : " Debt cleared.")).Mt(4));
            if (res.travel > 0) hero.Add(U.Hint($"Hotel and travel: {U.Money(res.travel)}"));
            if (ev.daily && res.streak > 0) hero.Add(U.Hint($"Daily streak: {res.streak} day{(res.streak == 1 ? "" : "s")}").Fg(C.Gold));
            Screens.Enter(hero);
            body.Add(hero);

            // big moments
            var moments = U.Col();
            foreach (var t in res.titles) moments.Add(Banner(DB.TitleIcon(t), DB.TitleName(t), "A title for the trophy room.", C.Gold));
            if (res.beat != null) moments.Add(Banner("🂡", "Beat " + J.S(DB.Legend(res.beat), "name", res.beat), "Heads-up, for the title.", C.Violet));
            else if (res.met != null && DB.Legend(res.met) != null) moments.Add(Banner("🤝", "Final table with " + J.S(DB.Legend(res.met), "name", res.met), "They'll remember your face.", C.Violet));
            foreach (var it in res.unlocked) { var m = Fashion.MetaOf(it.slot, it.id); var g = Fashion.Grade(m.grade); moments.Add(Banner("✦", "Unlocked: " + it.name, (m.brand ?? "") + " · " + g.name, C.H(g.color))); }
            body.Add(moments);

            var p = res.prog;
            if (p != null)
            {
                var c = U.Card();
                var xr = U.Row(); xr.Add(U.Kicker("Career").Grow()); xr.Add(U.Num($"+{Math.Round(p.xp)} XP", "").Fg(C.Gold)); c.Add(xr);
                foreach (var l in p.levels) c.Add(U.P($"⬆ Career level {l}").Fg(C.Gold2));
                foreach (var (kind, id, why) in p.cos) c.Add(U.Hint($"New: {CareerView.CosName(kind, id)} · {why}"));
                foreach (var (t, xp) in p.quests) c.Add(U.Hint($"Quest done: {t} (+{xp} XP)").Fg(C.Green));
                if (p.bonus) c.Add(U.Hint("All three daily quests: +100 XP").Fg(C.Green));
                var achs = DB.List("DR_ACH") ?? new List<object>(); string[] tierN = { "Bronze", "Silver", "Gold" };
                foreach (var (id, tier) in p.ach)
                {
                    string an = J.S(achs.FirstOrDefault(a => J.S(a, "id") == id), "n", id);
                    c.Add(U.Hint("Achievement: " + an + " · " + tierN[Math.Min(2, tier)]).Fg(C.Gold));
                }
                foreach (var (slot, id, lv) in p.wear) c.Add(U.Hint($"{DB.Item(slot, id)?.name ?? id} is now {Fashion.WearN[lv]}: {Fashion.WearGives[lv]}").Fg(C.Violet));
                body.Add(c);
            }

            // how you played
            if (res.all.Count > 0)
            {
                var c = U.Card(); c.Add(U.Kicker($"Your decisions · {res.n}"));
                var counts = U.Row().Cls("wrap").Mt(4);
                foreach (var g in res.all.GroupBy(d => d.lab).OrderByDescending(g => DB.Labels[g.Key].score))
                { var L = DB.Labels[g.Key]; counts.Add(U.Chip($"{L.icon} {L.n} ×{g.Count()}", C.H(L.c))); }
                c.Add(counts);
                foreach (var d in res.keys)
                {
                    var L = DB.Labels[d.lab]; var r = U.Row().Cls("keyhand");
                    r.Add(U.Sym(CardView.Short(d.hand.cards), "kh-cards"));
                    var l = U.Col().Grow(); l.style.marginLeft = 8;
                    l.Add(U.T($"{d.hand.pos} · {d.hand.cat}", "kh-t", Fonts.Semi));
                    l.Add(U.Hint(d.hand.mineK == d.hand.bestK ? $"{d.hand.mine}: the solver's play" : $"You: {d.hand.mine} · Best: {d.hand.best} ({d.hand.bestF}%)"));
                    r.Add(l); r.Add(U.Chip(L.n, C.H(L.c)));
                    c.Add(r);
                }
                body.Add(c);
            }

            var acts = U.Col().Mt(6);
            if (Calendar.CanReenter(s, res)) acts.Add(U.Btn($"Re-enter · {U.Money(ev.buyin)}", () => DreamApp.Confirm("Fire another bullet?", $"{U.Money(ev.buyin)} from a {U.Money(s.br)} roll. Late registration is still open.", "Re-enter", Game.Reenter, "No"), "wide", true, "Same event, fresh stack"));
            var sl = Calendar.SlotsLeft(s);
            acts.Add(U.Btn("Continue", () => Game.Hub(), "gold wide", true, s.ch == 0 ? null : sl.left > 0 ? $"{sl.left} game{(sl.left == 1 ? "" : "s")} left {(sl.week ? "this week" : "today")}" : sl.week ? "No sessions left this week" : "No games left today"));
            body.Add(acts);
            body.Add(U.Space(30));
            sv.Add(body); page.Add(sv);
            return page;
        }

        static VisualElement Banner(string icon, string title, string sub, Color c)
        {
            var b = U.Row().Cls("banner").Border(C.A(c, 0.5f)).Bg(C.A(c, 0.1f));
            b.Add(U.T(icon, "banner-i").Fg(c));
            var l = U.Col().Grow(); l.style.marginLeft = 10; l.Add(U.T(title, "banner-t", Fonts.Semi).Fg(c)); if (!string.IsNullOrEmpty(sub)) l.Add(U.Hint(sub)); b.Add(l);
            Screens.Enter(b, 120);
            return b;
        }
    }
}
