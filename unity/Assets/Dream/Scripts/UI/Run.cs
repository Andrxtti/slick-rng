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
                case "showdown": body.Add(ShowdownView(run)); break;
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
            bool drill = run.drill != null;
            l.Add(U.Kicker(run.ev.name)); l.Add(U.H3(drill ? "Spots you misplayed" : DB.StageLong(run.Stage) + (run.turbo ? " · Turbo" : "")));
            r.Add(l);
            h.Add(r);
            int per = Play.Per(run);
            if (drill)
            {
                var dm = U.Row().Mt(8); var di = U.Col().Cls("run-info"); di.Add(U.Num($"{Math.Min(run.h + 1, per)}/{per}", "")); di.Add(U.T("SPOT", "stat-l", Fonts.Bold)); dm.Add(di);
                int okN = run.results.Count(x => x.tier == "perfect" || x.tier == "good"); var dj = U.Col().Cls("run-info"); dj.Add(U.Num(okN.ToString(), "").Fg(C.Green)); dj.Add(U.T("FIXED", "stat-l", Fonts.Bold)); dm.Add(dj);
                h.Add(dm); if (run.combo >= 3) h.Add(U.Chip($"🔥 {run.combo} in a row", C.H("#ff8a3c")).Mt(6));
                return h;
            }
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
            if (run.drill != null)
            {
                c.Add(U.Kicker("Study session")); c.Add(U.H2("The spots you misplayed"));
                c.Add(U.Hint($"{run.drill.Count} hand{(run.drill.Count == 1 ? "" : "s")}. Same seat, same stacks, same action in front of you. Get one right and it leaves the list."));
                var t0 = U.Row().Cls("tip").Mt(12); t0.Add(new Portrait(Fig.Slick(), 40, false));
                var tl0 = U.Col().Grow(); tl0.style.marginLeft = 8; tl0.Add(U.T("Slick", "tip-who", Fonts.Semi).Fg(C.Blue)); tl0.Add(U.P("Nobody remembers the hands they won. Fix the ones you didn't."));
                t0.Add(tl0); c.Add(t0);
                c.Add(U.Btn("Deal", Game.Deal, "gold wide").Mt(14));
                Screens.Enter(c); return c;
            }
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
            var table = new TableView(run.spot, vs: run.Stage == "final" && run.ev.kind != "cash" && run.drill == null ? run.boss : null); table.AddToClassList("table-wide"); left.Add(table);
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
            // at the final table, whoever from the story is there is the one you're playing against
            string vs = run.Stage == "final" && run.ev.kind != "cash" && run.drill == null ? run.boss : null;
            if (vs != null)
            {
                bool lastHand = run.si >= run.stages.Count - 1 && run.h >= Play.Per(run) - 1;
                var bar = U.Row().Cls("rival-bar"); var fig = Fig.For(vs); if (fig != null) bar.Add(new Portrait(fig, 40, false));
                var bl = U.Col().Grow(); bl.style.marginLeft = 10;
                bl.Add(U.Kicker(lastHand ? "Final hand" : "Final table")); bl.Add(U.H3("vs " + Story.NameOf(vs)));
                bar.Add(bl); col.Add(bar);
            }
            if (withTable) col.Add(new TableView(sp, vs: vs));

            var hero = sp.seats[sp.hero];
            var hr = U.Row().Cls("hero-row");
            var cards = U.Row().Cls("hero-cards"); var a = CardView.Face(sp.hand[0], 62); var b = CardView.Face(sp.hand[1], 62); b.style.marginLeft = 6; cards.Add(a); cards.Add(b);
            hr.Add(cards);
            var hi = U.Col().Grow(); hi.style.marginLeft = 12;
            hi.Add(U.Kicker(sp.cat));
            hi.Add(U.T($"{hero.pos} · {Fmt.Bb(hero.stack - hero.put)} bb", "hero-pos", Fonts.Semi));
            var odds = Odds(sp); if (odds != null) { var oh = U.Hint(odds); oh.name = "odds"; hi.Add(oh); }
            hr.Add(hi);
            col.Add(hr);

            if (!answered)
            {
                // actions side by side, like a real table: fold, call, raise
                var opts = U.Row().Cls("options act-row").Mt(14);
                for (int i = 0; i < sp.acts.Count; i++)
                {
                    int k = i; string lab = sp.Label(i);
                    var btn = U.Btn(lab, () => Game.Answer(k), "opt-btn " + (i % 2 == 0 ? "pick-blue" : "pick-green"));   // decision tabs alternate Carolina blue / neon lime
                    opts.Add(btn);
                }
                col.Add(opts);
            }
            else if (last != null) col.Add(Verdict(run, sp, last));
            return col;
        }

        // colour by what the button says: fold, passive (call, limp, check), raise, all in
        static string ActCls(string a, string label = null)
        {
            var l = (label ?? "").Trim().ToLowerInvariant();
            if (l.StartsWith("fold") || a == "fold") return "act-fold";
            if (l.StartsWith("call") || l.StartsWith("limp") || l.StartsWith("check")) return "act-call";
            if (l.Contains("all in") || l.Contains("all-in") || l.StartsWith("jam") || l.StartsWith("shove")) return "act-jam";
            if (l.StartsWith("raise") || l.StartsWith("bet") || l.StartsWith("3-bet") || l.StartsWith("4-bet") || l.StartsWith("iso")) return "act-raise";
            return a == "call" ? "act-call" : a == "allin" ? "act-jam" : "act-raise";
        }

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

        // all in: both hands face up, the board one street at a time, then who holds
        static VisualElement ShowdownView(Run run)
        {
            var sd = run.show; var c = U.Card("showdown");
            if (sd == null) { c.schedule.Execute(Game.ShowdownDone).StartingIn(10); return c; }
            c.Add(U.Kicker(sd.oppName != sd.oppSeat ? "Final table · all in vs " + sd.oppName : "All in"));
            var head = U.T("ALL IN", "sd-allin", Fonts.Disp); head.style.fontSize = 44; head.style.color = C.H("#b8ff2e");
            head.style.textShadow = new TextShadow { offset = Vector2.zero, blurRadius = 18, color = new Color(0.72f, 1f, 0.18f, 0.8f) };
            head.style.unityTextAlign = TextAnchor.MiddleCenter; c.Add(head);
            if (sd.folded)
            {
                c.Add(U.P($"{sd.oppSeat} thinks it over. Then the cards go in the muck.").Mt(8));
                c.Add(U.T("They fold. The pot is yours.", "sd-result", Fonts.Semi).Fg(C.Green).Mt(8));
                Sound.Play("chips", 0.9f);
                c.Add(U.Btn("See the verdict", Game.ShowdownDone, "gold wide").Mt(14));
                Screens.Enter(c); return c;
            }
            float w = 46;
            VisualElement SdHand(string who, int[] cards, bool faceUp, out VisualElement backs, out VisualElement faces, Color col)
            {
                var col1 = U.Col().Cls("sd-hand"); col1.style.alignItems = Align.Center; col1.style.flexGrow = 1;
                col1.Add(U.T(who, "sd-who", Fonts.Semi).Fg(col));
                var stack = new VisualElement(); stack.style.width = w * 2 + 6; stack.style.height = w * 1.4f;
                faces = U.Row(); faces.style.position = Position.Absolute; faces.Add(CardView.Face(cards[0], w)); var f2 = CardView.Face(cards[1], w); f2.style.marginLeft = 6; faces.Add(f2);
                backs = U.Row(); backs.style.position = Position.Absolute; backs.Add(CardView.Back(w)); var b2 = CardView.Back(w); b2.style.marginLeft = 6; backs.Add(b2);
                stack.Add(faces); stack.Add(backs); faces.style.opacity = faceUp ? 1 : 0; backs.style.opacity = faceUp ? 0 : 1;
                col1.Add(stack); return col1;
            }
            var row = U.Row().Mt(10);
            row.Add(SdHand("You", sd.hero, true, out _, out _, C.Blue));
            var vsl = U.T("vs", "sd-vs", Fonts.Semi).Fg(C.Mute); vsl.style.alignSelf = Align.Center; row.Add(vsl);
            row.Add(SdHand(sd.oppName, sd.opp, false, out var oppBack, out var oppFace, C.Gold));
            c.Add(row);
            // the equity bar, shown when their cards turn over
            var eq = U.Col().Mt(10); eq.style.opacity = 0;
            var bar = U.Row(); bar.style.height = 8; bar.style.borderTopLeftRadius = bar.style.borderBottomLeftRadius = bar.style.borderTopRightRadius = bar.style.borderBottomRightRadius = 4; bar.style.overflow = Overflow.Hidden;
            var mine = new VisualElement(); mine.style.width = Length.Percent((float)(sd.eq * 100)); mine.style.backgroundColor = C.Blue; bar.Add(mine);
            var theirs = new VisualElement(); theirs.style.flexGrow = 1; theirs.style.backgroundColor = C.Gold; bar.Add(theirs);
            eq.Add(bar);
            var eql = U.Row().Mt(4); eql.Add(U.T($"{Math.Round(sd.eq * 100)}% to win", "sd-eq", Fonts.Semi).Fg(C.Blue).Grow()); eql.Add(U.T($"{Math.Round((1 - sd.eq) * 100)}%", "sd-eq", Fonts.Semi).Fg(C.Gold)); eq.Add(eql);
            c.Add(eq);
            // the board
            var board = U.Row().Mt(14); board.style.justifyContent = Justify.Center; board.style.minHeight = w * 1.4f;
            var bc = new List<VisualElement>();
            for (int i = 0; i < 5; i++) { var f = CardView.Face(sd.board[i], w); if (i > 0) f.style.marginLeft = i == 3 || i == 4 ? 12 : 6; f.style.opacity = 0; f.style.translate = new Translate(0, -14); board.Add(f); bc.Add(f); }
            c.Add(board);
            var street = U.T("", "sd-street", Fonts.Semi).Fg(C.Mute); street.style.unityTextAlign = TextAnchor.MiddleCenter; street.style.marginTop = 6; c.Add(street);
            var result = U.Col().Mt(10); result.style.opacity = 0; result.style.alignItems = Align.Center;
            string big = sd.chop ? "Chopped. Split pot." : sd.win ? "You hold!" : sd.oppName != sd.oppSeat ? $"{sd.oppName} holds." : "They hold.";
            var rl = U.T(big, "sd-result", Fonts.Disp).Fg(sd.chop ? C.Cream : sd.win ? C.Green : C.Red); rl.style.fontSize = 30; result.Add(rl);
            result.Add(U.Hint(sd.win || sd.chop ? $"{sd.heroHand} beats {Lower(sd.oppHand)}." : $"{sd.oppHand} beats {Lower(sd.heroHand)}.").Mt(2));
            if (sd.after != sd.before) result.Add(U.Hint($"Stack {sd.before} → {sd.after} bb").Fg(sd.after > sd.before ? C.Green : C.Red));
            var go = U.Btn("See the verdict", Game.ShowdownDone, "gold wide").Mt(12);
            result.Add(go);
            c.Add(result);
            var hint = U.Hint("Tap to skip").Mt(6); hint.style.unityTextAlign = TextAnchor.MiddleCenter; c.Add(hint);

            // the timeline: flip, flop, turn, a long breath, the river
            float t0 = Time.realtimeSinceStartup, skip = -1; int shown = 0; bool flipped = false, ended = false;
            float[] at = { 1.6f, 1.75f, 1.9f, 3.0f, 4.6f };
            c.RegisterCallback<PointerUpEvent>(e => { if (!ended && Time.realtimeSinceStartup - t0 > 0.4f) skip = 99; });
            Sound.Play("swell", 0.8f);
            c.schedule.Execute(() =>
            {
                if (ended) return;
                float t = Math.Max(Time.realtimeSinceStartup - t0, skip);
                if (!flipped && t >= 0.8f) { flipped = true; oppBack.style.opacity = 0; oppFace.style.opacity = 1; eq.style.opacity = 1; Sound.Play("sting", 0.7f); }
                while (shown < 5 && t >= at[shown])
                {
                    var f = bc[shown]; f.style.opacity = 1; f.style.translate = new Translate(0, 0); shown++;
                    if (skip < 0) Sound.Play(shown == 5 ? "hush" : "tick", 0.8f);
                    street.text = shown == 3 ? "The flop" : shown == 4 ? "The turn" : shown == 5 ? "The river" : "";
                }
                if (shown == 3 && t < at[3]) street.text = "The flop";
                if (shown == 4 && t > 3.6f && t < at[4]) street.text = "The river...";
                if (t >= 5.3f || skip > 0 && shown == 5)
                {
                    ended = true; result.style.opacity = 1; street.text = ""; hint.style.display = DisplayStyle.None;
                    Sound.Play(sd.win ? "win" : sd.chop ? "chips" : "bust", 0.9f);
                }
            }).Every(16);
            Screens.Enter(c);
            return c;
        }
        static string Lower(string s) => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s.Substring(1);

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
            if (ev.kind == "drill")
            {
                hero.Add(U.T($"{res.fixedN}/{res.drillN}", "res-big", Fonts.Disp).Fg(res.fixedN == res.drillN ? C.Gold2 : res.fixedN > 0 ? C.Green : C.Cream));
                hero.Add(U.P(res.fixedN == res.drillN ? "Every spot put right. They're off the list." : res.fixedN > 0 ? "spots put right and off the list. The rest come back next session." : "None put right yet. They'll be back; read the solver's reasons below."));
                var dm = U.Row().Cls("stats").Mt(10); dm.Add(U.Stat(res.acc + "%", "Accuracy")); dm.Add(U.Stat(res.bestCombo.ToString(), "Best run")); dm.Add(U.Stat(Play.Misses(s).Count.ToString(), "Still waiting")); hero.Add(dm);
                Screens.Enter(hero); body.Add(hero);
                if (res.prog != null && res.prog.xp > 0) { var cx = U.Card(); var xr = U.Row(); xr.Add(U.Kicker("Career").Grow()); xr.Add(U.Num($"+{Math.Round(res.prog.xp)} XP", "").Fg(C.Gold)); cx.Add(xr); body.Add(cx); }
                body.Add(Decisions(res));
                var da = U.Col().Mt(6);
                if (Play.Misses(s).Count > 0) da.Add(U.Btn("Drill the rest", Game.StartDrill, "gold wide"));
                da.Add(U.Btn("Back to the Spacepad", () => Game.SetTab("calendar"), (Play.Misses(s).Count > 0 ? "ghost" : "gold") + " wide").Mt(8));
                body.Add(da); body.Add(U.Space(30)); sv.Add(body); page.Add(sv);
                return page;
            }
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
            foreach (var it in res.unlocked) { var m = Fashion.MetaOf(it.slot, it.id); var g = Fashion.Grade(m.grade); moments.Add(Banner("✦", "Unlocked: " + it.name, (string.IsNullOrEmpty(m.brand) ? "" : m.brand + " · ") + g.name, C.H(g.color), ClosetView.Art(s, it.slot, it.id, 56, 48))); }
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
            if (res.all.Count > 0) body.Add(Decisions(res));

            var acts = U.Col().Mt(6);
            if (Calendar.CanReenter(s, res)) acts.Add(U.Btn($"Re-enter · {U.Money(ev.buyin)}", () => DreamApp.Confirm("Fire another bullet?", $"{U.Money(ev.buyin)} from a {U.Money(s.br)} roll. Late registration is still open.", "Re-enter", Game.Reenter, "Not this time"), "ghost wide").Named("reenter"));
            var sl = Calendar.SlotsLeft(s);
            acts.Add(U.Btn("Continue", () => Game.Hub(), "gold wide", true, s.ch == 0 ? null : sl.left > 0 ? $"{sl.left} game{(sl.left == 1 ? "" : "s")} left {(sl.week ? "this week" : "today")}" : sl.week ? "No games left this week" : "No games left today").Mt(8));
            body.Add(acts);
            body.Add(U.Space(30));
            sv.Add(body); page.Add(sv);
            return page;
        }

        static VisualElement Decisions(Result res)
        {
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
                return c;
            }
        }

        static VisualElement Banner(string icon, string title, string sub, Color c, VisualElement art = null)
        {
            var b = U.Row().Cls("banner").Bg(C.A(c, 0.12f));
            if (art != null) { var pic = U.Div("banner-pic").NoPick(); pic.Add(art); b.Add(pic); }
            else b.Add(U.T(icon, "banner-i").Fg(c));
            var l = U.Col().Grow(); l.style.marginLeft = 10; l.Add(U.T(title, "banner-t", Fonts.Semi).Fg(c)); if (!string.IsNullOrEmpty(sub)) l.Add(U.Hint(sub)); b.Add(l);
            Screens.Enter(b, 120);
            return b;
        }
    }
}
