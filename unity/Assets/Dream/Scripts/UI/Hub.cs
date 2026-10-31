// The hub: who you are and what you have, the chapter goal, and four tabs. The calendar tab is the heart of
// the game: tonight's events and what they risk, Slick's meetups, private game invites, rent, the job, the
// majors coming up, and going to sleep.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFDream.UI
{
    public static class HubView
    {
        // Home is your place; its laptop ("calendar") is the hub: tonight's games, the calendar, money
        static readonly (string id, string n, string icon)[] Tabs = { ("home", "Home", "home"), ("closet", "Closet", "closet"), ("online", "Online", "online"), ("career", "Career", "career"), ("story", "Story", "story") };

        public static VisualElement Build()
        {
            var s = Game.State;
            var page = Screens.Page("hub");
            var body = U.Div("hub-body");
            switch (Game.Tab)
            {
                case "closet": body.Add(ClosetView.Build(s)); break;
                case "career": body.Add(Simple("Career", s, CareerView.Build(s))); break;
                case "story": body.Add(Simple("Story", s, StoryTab(s))); break;
                case "online": body.Add(Simple("Online", s, OnlineTab(s))); break;
                case "calendar": body.Add(Laptop(s, true)); break;
                default: body.Add(HomeScreen.Build(s)); break;
            }
            page.Add(body);
            page.Add(NavBar(s));
            return page;
        }

        // CF Tilt online: quick games on the same engine as the story, by tier; the rest is marked as coming
        static VisualElement OnlineTab(State s)
        {
            var col = U.Col();
            var sl = Calendar.SlotsLeft(s);
            col.Add(U.Hint(s.ch == 0 ? "Finish the prologue first." : s.job == "nights" ? $"Online games count against your week: {sl.left} of {sl.cap} left." : $"Online games count against today: {sl.left} of {sl.cap} left.").Mt(2));
            foreach (var tier in new[] { "Casual", "Ranked", "League" })
            {
                col.Add(U.Kicker(tier).Mt(16));
                foreach (var g in Career.Online(s).Where(x => x.tier == tier))
                {
                    var c = U.Card("event" + (g.soon ? " soon" : ""));
                    var hd = U.Row(); hd.Add(U.H3(g.name).Grow()); if (g.soon) hd.Add(U.Chip(g.note, C.Mute)); else hd.Add(U.Chip(g.ev.turbo ? "Turbo" : "Online", C.Blue));
                    c.Add(hd); c.Add(U.P(g.blurb).Mt(4));
                    if (!g.soon)
                    {
                        var ev = g.ev; var risk = Career.Risk(s, ev);
                        c.Add(U.Hint($"Buy-in {U.Money(ev.buyin)} · {ev.field} players · {ev.seats} paid · {string.Join(" / ", ev.perS)} hands").Mt(4));
                        string why = s.ch == 0 ? "After the prologue" : risk.k == "no" ? risk.label : Calendar.CantPlay(s, ev);
                        c.Add(U.Btn("Play", () => Game.StartEvent(ev.id), why == null ? "gold wide" : "wide", why == null, why).Mt(8));
                    }
                    col.Add(c);
                }
            }
            col.Add(U.Hint("Every online game opens with a player intro: your look, your gear, your titles. Ranked ratings and the League season land with the online update.").Mt(16));
            return col;
        }

        // a plain scrolling page with a title (career, story)
        static VisualElement Simple(string title, State s, VisualElement content)
        {
            var sv = Screens.Scroll(); var pad = U.Div("pad");
            if (DreamApp.Landscape) pad.AddToClassList("narrow-col");
            pad.Add(Bits.PageHead(title, s, Bankroll(s, true)));
            pad.Add(content); pad.Add(U.Space(24));
            sv.Add(pad);
            return sv;
        }

        // ------------------------------------------------------------ the tab bar, always at the bottom
        static VisualElement NavBar(State s)
        {
            var bar = U.Row().Cls("navbar");
            foreach (var (id, n, icon) in Tabs)
            {
                bool on = Game.Tab == id || (id == "home" && Game.Tab == "calendar");
                var t = U.Col().Cls("nav-b", on ? "on" : "");
                t.Add(new Icon(icon, 24, on ? C.Gold : C.Mute));
                t.Add(U.T(n, "nav-t", on ? Fonts.Semi : Fonts.Medium).NoPick());
                if (id == "closet" && s.newItems.Count > 0) t.Add(U.Div("nav-dot").NoPick());
                string k = id; t.Tap(() => { if (Game.Tab != k) Game.SetTab(k); });
                bar.Add(t);
            }
            return bar;
        }

        // ------------------------------------------------------------ home: you, your stage, tonight
        static VisualElement Home(State s)
        {
            bool land = DreamApp.Landscape;
            var head = Header(s);
            var stage = HomeStage(s, land ? -1 : 300);
            var list = CalendarTab(s);
            if (land)
            {
                var row = U.Row().Cls("two-pane grow");
                var left = U.Col().Cls("pane-left"); left.Add(head); left.Add(stage); row.Add(left);
                var sv = Screens.Scroll(); var pad = U.Div("pad"); pad.Add(Chapter(s)); pad.Add(list); pad.Add(U.Space(24)); sv.Add(pad); row.Add(sv);
                return row;
            }
            var main = Screens.Scroll(); var b = U.Div("pad-top");
            b.Add(head); b.Add(stage);
            var rest = U.Div("pad"); rest.Add(Chapter(s)); rest.Add(list); rest.Add(U.Space(24));
            b.Add(rest); main.Add(b);
            return main;
        }

        // the laptop: tonight, the calendar, money and the job, in a browser window with a way back home
        public static VisualElement Laptop(State s, bool chrome)
        {
            var main = Screens.Scroll(); var pad = U.Div("pad-top");
            if (DreamApp.Landscape) pad.AddToClassList("narrow-col");
            if (chrome)
            {
                var bar = U.Row().Cls("laptop-bar");
                var back = U.Row().Cls("laptop-back"); back.Add(new Icon("chev-l", 16, C.Gold)); back.Add(U.T("Home", "laptop-back-t", Fonts.Semi).NoPick());
                back.Tap(() => Game.SetTab("home")); bar.Add(back);
                var url = U.Row().Cls("laptop-url").Grow(); url.Add(U.T("Spacepad \u00b7 cfdream.app", "laptop-url-t", Fonts.Medium).NoPick()); bar.Add(url);
                pad.Add(bar);
            }
            pad.Add(Header(s));
            var rest = U.Div("pad"); rest.Add(Chapter(s)); rest.Add(Study(s)); rest.Add(CalendarTab(s)); rest.Add(U.Space(24));
            pad.Add(rest); main.Add(pad);
            return main;
        }

        // the study tool: where your game leaks, and the spots you misplayed dealt again until you get them right
        static VisualElement Study(State s)
        {
            var c = U.Card("study"); var misses = Play.Misses(s);
            var top = U.Row(); top.Add(U.Kicker("Study").Grow()); if (s.FlagN("fixed") > 0) top.Add(U.Hint($"{(int)s.FlagN("fixed")} fixed").Fg(C.Green)); c.Add(top);
            var ranked = Progression.Skills.Where(k => k != "short").Select(k => (k, acc: Progression.SkillAcc(s.pg, k))).Where(x => x.acc.HasValue).OrderBy(x => x.acc.Value).Take(3).ToList();
            if (ranked.Count == 0) c.Add(U.Hint("Play a few hands and your leaks show up here, graded by the solver."));
            else
            {
                c.Add(U.T("Where it leaks", "kh-t", Fonts.Semi).Mt(2));
                foreach (var (k, acc) in ranked)
                {
                    var (n, d) = Progression.SkillNames[k]; int stars = Progression.SkillStars(s.pg, k);
                    var r = U.Row().Cls("skill").Mt(4);
                    var l = U.Col().Grow(); l.Add(U.T(n, "skill-n", Fonts.Semi)); l.Add(U.Hint(d)); r.Add(l);
                    var rr = U.Col().Cls("skill-r"); rr.Add(U.Sym(new string('★', stars) + new string('☆', 3 - stars), "stars").Fg(stars > 0 ? C.Gold : C.Faint)); rr.Add(U.Hint(acc + "%").Fg(acc < 62 ? C.Red : acc < 78 ? C.Gold : C.Green));
                    r.Add(rr); c.Add(r);
                }
            }
            if (misses.Count > 0)
            {
                c.Add(U.Btn($"Drill your misses · {misses.Count}", Game.StartDrill, "gold wide").Mt(10));
                c.Add(U.Hint("Same seat, same stacks, same action. A spot leaves the list when you get it right.").Mt(4));
            }
            else if (s.dec > 0) c.Add(U.Hint("No misplayed spots waiting. Every mistake you make at the tables lands here to be drilled.").Mt(6));
            return c;
        }

        public static VisualElement Header(State s)
        {
            var row = U.Row().Cls("home-head");
            var lv = Progression.LevelOf(s.pg.xp);
            var ring = Bits.LevelRing(lv.lvl, lv.into / Math.Max(1, lv.need)); ring.Tap(() => Game.SetTab("career")); row.Add(ring);
            var who = U.Col().Grow(); who.style.marginLeft = 12;
            who.Add(U.T(s.name, "hub-name", Fonts.Disp));
            var title = J.S(DB.Obj("DR_TITLES_COS"), s.pg.eq.TryGetValue("title", out var tt) ? tt : "none", "");
            who.Add(U.Hint(Career.Rep(s) + (tt != null && tt != "none" && !string.IsNullOrEmpty(title) ? " · " + title : "")));
            row.Add(who);
            row.Add(Bankroll(s, false));
            var col = U.Col(); col.Add(row);
            if (s.debt > 0) col.Add(U.Hint($"You owe Slick {U.Money(s.debt)}. Half of every profit goes to him until it's paid.").Cls("debt").Fg(C.Red));
            return col;
        }

        static VisualElement Bankroll(State s, bool small)
        {
            var money = U.Col().Cls("hub-money", small ? "small" : "");
            money.Add(U.Num(U.Money(s.br), "hub-br").Fg(s.br < 100 ? C.Red : C.Gold2));
            money.Add(U.T("Bankroll", "hub-br-l", Fonts.Medium));
            return money;
        }

        static VisualElement HomeStage(State s, float h)
        {
            var st = StageView.Make(Fig.Of(s), () => Character.CharacterSpec.From(s), h, "full", out _);
            var chip = Bits.FitChip(s, () => Game.SetTab("closet")); chip.AddToClassList("stage-tl"); st.Add(chip);
            return st;
        }

        static VisualElement Chapter(State s)
        {
            var ch = DB.Chapter(s.ch);
            var c = U.Card("chapter");
            var pr = Career.ChapterProg(s);
            var top = U.Row();
            top.Add(U.T($"{J.S(ch, "n")} · {J.S(ch, "t")}", "panel-k", Fonts.Semi).Grow());
            if (s.ch >= 1 && !string.IsNullOrEmpty(pr.text)) top.Add(U.T(pr.text, "panel-r"));
            c.Add(top);
            c.Add(U.P(J.S(ch, "goal", "")).Cls("goal-t"));
            if (s.ch >= 1) c.Add(U.Bar(pr.frac, C.Gold, 4).Mt(8));
            return c;
        }

        // ------------------------------------------------------------ the calendar
        static VisualElement CalendarTab(State s)
        {
            var col = U.Col(); var c = s.cal;
            if (Game.Morning != null && Game.Morning.Count > 0) col.Add(Morning());
            if (c.meet != null && c.meet.d == c.d) col.Add(Meetup(s));
            if (c.inv != null) col.Add(Invite(s));

            col.Add(Tonight(s));
            var today = Calendar.Today(s).Where(x => x.why != "invite").ToList();
            string pick = Career.Pick(s);
            if (today.Count == 0) col.Add(U.Card().Kids(U.P("Nothing on the schedule tonight.")));
            foreach (var de in today.OrderByDescending(x => x.ev.id == pick).ThenByDescending(x => x.ev.kind == "mtt" ? x.ev.buyin : 0)) col.Add(EventCard(s, de.ev, de.why, de.ev.id == pick));

            if (s.ch >= 1)
            {
                foreach (var L in Calendar.LiveNow()) col.Add(LiveCard(s, L));
                col.Add(DailyCard(s));
                col.Add(U.T("Plan ahead", "sec-h", Fonts.Disp).Mt(18).Mb(10));
                col.Add(Week(s));
                if (Game.CalSel > c.d) col.Add(DayPreview(s, Game.CalSel));
                col.Add(Money(s));
                col.Add(Job(s));
                col.Add(Majors(s));
                col.Add(SleepBar(s));
            }
            return col;
        }

        // "Tonight" with today's date and how many games are left
        static VisualElement Tonight(State s)
        {
            var D = Calendar.Date(s.cal.d); var sl = Calendar.SlotsLeft(s);
            var r = U.Row().Cls("sec-row");
            var l = U.Col().Grow();
            l.Add(U.T(s.ch == 0 ? "Saturday" : "Tonight", "sec-h", Fonts.Disp));
            l.Add(U.Hint($"{Calendar.Str(s.cal.d, true)} · Year {D.y}"));
            r.Add(l);
            var right = U.Col().Cls("sec-right");
            var pips = U.Row().Cls("pips");
            for (int i = 0; i < sl.cap; i++) pips.Add(U.Div("pip").Cls(i < sl.used ? "used" : ""));
            right.Add(pips);
            int lv = Calendar.LiveLeft(s);
            string status = s.ch == 0 ? "Prologue" : s.job == "nights" ? $"{sl.left} of {sl.cap} online this week · live {(lv > 0 ? (Calendar.Weekend(s) ? "tonight" : "this weekend") : "played")}" : $"{sl.left} of {sl.cap} online today · {(lv > 0 ? "1 live game" : "live game played")}";
            right.Add(U.Hint(status));
            r.Add(right);
            return r;
        }

        static VisualElement Morning()
        {
            var c = U.Card("morning");
            var hd = U.Row(); hd.Add(U.Kicker("This morning").Grow()); hd.Add(U.Btn("Got it", () => { Game.Morning = null; Game.Render(); }, "ghost small")); c.Add(hd);
            foreach (var (icon, text) in Game.Morning)
            {
                var r = U.Row().Cls("news"); r.Add(U.T(icon, "news-i")); r.Add(U.P(text).Grow()); c.Add(r);
            }
            Screens.Enter(c);
            return c;
        }

        static VisualElement Week(State s)
        {
            var c = s.cal; int ws = c.d - Calendar.Date(c.d).dow + 7 * Game.CalWk;
            var w = U.Card("week");
            var hd = U.Row();
            hd.Add(U.Btn("‹", () => { Game.CalWk = Math.Max(0, Game.CalWk - 1); Game.Render(); }, "ghost small", Game.CalWk > 0));
            hd.Add(U.T(Game.CalWk == 0 ? "This week" : Game.CalWk == 1 ? "Next week" : $"{Calendar.Str(ws)} week", "week-t", Fonts.Semi).Grow());
            hd.Add(U.Btn("›", () => { Game.CalWk = Math.Min(12, Game.CalWk + 1); Game.Render(); }, "ghost small"));
            w.Add(hd);
            var row = U.Row().Cls("days");
            for (int i = 0; i < 7; i++)
            {
                int d = ws + i; var D = Calendar.Date(d);
                var cell = U.Col().Cls("day", d < c.d ? "past" : "", d == c.d ? "today" : "", d == Game.CalSel ? "sel" : "");
                cell.Add(U.T(Calendar.Dow[D.dow].Substring(0, 1), "day-w", Fonts.Bold).NoPick());
                cell.Add(U.Num(D.dd.ToString(), "day-n").NoPick());
                cell.Add(U.T(Calendar.DayIcons(s, d), "day-i").NoPick());
                if (d > c.d) cell.Tap(() => { Game.CalSel = Game.CalSel == d ? -1 : d; Game.Render(); });
                row.Add(cell);
            }
            w.Add(row);
            return w;
        }

        static VisualElement DayPreview(State s, int d)
        {
            var c = U.Card("preview");
            c.Add(U.Kicker(Calendar.Str(d, true)));
            var evs = Calendar.DayEvents(s, d).Where(x => x.why != "invite" && x.why != "freeroll").ToList();
            var series = Calendar.SeriesOnDay(d);
            foreach (var x in series) c.Add(U.P($"{x.sr.icon} {x.sr.n} · day {x.o + 1} of {x.sr.days}"));
            foreach (var x in evs.Where(e => e.ev.kind != "cash").Take(8)) c.Add(U.Hint($"{x.ev.name} · {(x.ev.free ? "free" : U.Money(x.ev.buyin))}"));
            int n = d - s.cal.d; bool rent = false; for (int k = s.cal.d + 1; k <= d; k++) if (Calendar.Date(k).dd == 1) rent = true;
            if (rent) c.Add(U.Hint($"Rent and bills come out on the 1st: {U.Money(Calendar.Bills(s))}.").Fg(C.Gold));
            c.Add(U.Btn($"Sleep until {Calendar.Dow[Calendar.Date(d).dow]}", () => Game.SleepTo(d), "wide", true, n == 1 ? "Skips the rest of tonight" : $"Skips {n} days").Mt(8));
            return c;
        }

        static VisualElement Meetup(State s)
        {
            var m = s.cal.meet; var c = U.Card("meetup");
            var r = U.Row(); r.Add(new Portrait(Fig.Slick(), 58));
            var t = U.Col().Grow(); t.style.marginLeft = 10;
            t.Add(U.Kicker("Lucky Seven Diner · 2 AM")); t.Add(U.H3("Slick wants to meet"));
            string offer = m.kind == "stake" ? $"He'll stake you {U.Money(m.amt)}. Half your profits go back to him until it's repaid."
                : m.kind == "seat" ? $"He'll pay your seat into the {DB.Event(m.id)?.name}. No strings."
                : $"{U.Money(m.amt)} for groceries. \"Don't play with it.\"";
            t.Add(U.P(offer));
            r.Add(t); c.Add(r);
            var b = U.Row().Mt(10); b.Add(U.Btn("Not tonight", Game.MeetSkip, "ghost grow")); b.Add(U.Btn("Meet him", Game.MeetAccept, "gold grow")); c.Add(b);
            return c;
        }

        static VisualElement Invite(State s)
        {
            var inv = s.cal.inv; var ev = inv.ev; bool tonight = inv.d == s.cal.d;
            var c = U.Card("invite");
            c.Add(U.Kicker(tonight ? "Private tournament · tonight" : "Private tournament · tomorrow night").Fg(C.Violet));
            c.Add(U.H2(ev.name));
            c.Add(U.P(ev.blurb).Mt(4));
            double pct = ev.buyin / Math.Max(1, s.br) * 100;
            var risk = Career.Risk(s, ev);
            var chips = U.Row().Cls("wrap").Mt(8);
            chips.Add(U.Chip($"Buy-in {U.Money(ev.buyin)}", C.Cream));
            chips.Add(U.Chip(s.br > 0 ? $"{Math.Round(pct)}% of your roll" : "You're broke", pct <= 5 ? C.Green : pct <= 20 ? C.Gold : C.Red));
            if (Calendar.TravelFor(s, ev) > 0) chips.Add(U.Chip($"Travel {U.Money(Calendar.TravelFor(s, ev))}", C.Mute));
            c.Add(chips);
            c.Add(U.Hint(pct > 20 ? "A real shot. Bust and you're rebuilding for weeks." : pct > 5 ? "Bigger than you usually play. The field is soft." : "Comfortable. Soft field, good money.").Mt(6));
            var b = U.Row().Mt(10);
            b.Add(U.Btn("Pass", () => DreamApp.Confirm("Pass on the game?", "Invites like this don't come around often.", "Pass", Game.DeclineInvite, "Keep it"), "ghost grow"));
            var sl = Calendar.SlotsLeft(s);
            if (tonight) b.Add(U.Btn("Register", () => Game.StartEvent("inv"), "gold grow", risk.k != "no" && sl.left > 0, risk.k == "no" ? risk.label : sl.left <= 0 ? "No games left" : null));
            else b.Add(U.Btn("See you tomorrow", null, "grow", false));
            c.Add(b);
            return c;
        }

        static readonly Dictionary<string, Color> RiskCol = new Dictionary<string, Color> { { "safe", C.Green }, { "shot", C.Gold }, { "yolo", C.Red }, { "no", C.Faint }, { "free", C.Blue } };

        public static VisualElement EventCard(State s, EventDef ev, string why, bool pick)
        {
            var c = U.Card("event").Cls(pick ? "pick" : "");
            var risk = Career.Risk(s, ev); var sl = Calendar.SlotsLeft(s);
            var top = U.Row();
            var l = U.Col().Grow();
            l.Add(U.Kicker(pick ? "★ Slick's pick" : why ?? "").Fg(pick ? C.Gold2 : C.Gold));
            l.Add(U.H3(ev.name));
            string kind = ev.id == "classic" ? $"Day 1 of 2 · {ev.field:#,0} players · top {ev.seats} return Sunday" : ev.id == "classic2" ? $"Day 2 · {ev.field} players · 32 paid" : ev.kind == "cash" ? "Cash game" : ev.kind == "sat" ? $"Satellite · {ev.seats} seats" : ev.field > 0 ? $"{ev.field:#,0} players" : "Tournament";
            l.Add(U.Hint($"{DB.Place(ev.venue)} · {kind}"));
            top.Add(l);
            var r = U.Col().Cls("event-price");
            bool ticket = s.Ticket(ev.id) > 0;
            r.Add(U.Num(ev.free ? "FREE" : ticket ? "SEAT" : U.Money(ev.buyin), "event-buyin").Fg(ev.free || ticket ? C.Blue : C.Cream));
            r.Add(U.T(ev.kind == "cash" ? "BRING" : "BUY-IN", "stat-l", Fonts.Bold));
            top.Add(r);
            c.Add(top);
            var chips = U.Row().Cls("wrap").Mt(8);
            chips.Add(U.Chip(risk.label, RiskCol.TryGetValue(risk.k, out var rc) ? rc : C.Mute, risk.k == "no"));
            if (ev.turbo) chips.Add(U.Chip("Turbo", C.Red));
            if (ev.reentry) chips.Add(U.Chip("Re-entry", C.Mute));
            if (ev.id == "classic") chips.Add(U.Chip("🏆 Biscayne Classic", C.Gold));
            else if (ev.kind == "sat" && !string.IsNullOrEmpty(ev.target)) chips.Add(U.Chip("Wins a seat: " + (DB.Event(ev.target)?.name ?? ev.target), C.Blue));
            var tr = Calendar.TravelFor(s, ev); if (tr > 0) chips.Add(U.Chip($"Hotel {U.Money(tr)}", C.Mute));
            if (!string.IsNullOrEmpty(ev.title)) chips.Add(U.Chip(DB.TitleIcon(ev.title) + " " + DB.TitleName(ev.title), C.Gold));
            if (ev.legends != null && ev.legends.Count > 0) chips.Add(U.Chip("A legend is in the field", C.Violet));
            if (ev.online) chips.Add(U.Chip("Online", C.Blue));
            c.Add(chips);
            if (!string.IsNullOrEmpty(ev.blurb)) c.Add(U.Hint(ev.blurb).Mt(6));
            string why2 = risk.k == "no" ? risk.label : Calendar.CantPlay(s, ev);
            c.Add(U.Btn(ev.kind == "cash" ? "Sit down" : "Register", () => Game.Register(ev.id), pick ? "gold wide" : "wide", why2 == null, why2).Mt(10));
            return c;
        }

        static VisualElement LiveCard(State s, LiveDef L)
        {
            var c = U.Card("live");
            c.Add(U.Kicker(L.icon + " Live event · real world, limited time").Fg(C.H("#ff7a1a")));
            c.Add(U.H3(L.ev.name));
            c.Add(U.Hint(L.ev.blurb));
            bool played = Calendar.LivePlayed(s, L.id), own = s.Owns(L.item.slot, L.item.id);
            c.Add(U.Hint(own ? $"You own the {L.item.name}." : $"Reward: {L.item.name}").Mt(4).Fg(own ? C.Green : C.Gold));
            var sl = Calendar.SlotsLeft(s);
            c.Add(U.Btn(played ? "Played today" : "Play", () => Game.StartEvent(L.ev.id), "wide", !played && sl.left > 0, played ? "Come back tomorrow" : sl.left <= 0 ? "No games left" : null).Mt(8));
            return c;
        }

        static VisualElement DailyCard(State s)
        {
            var ev = Career.DailyEvent(s); bool done = Career.DailyDone(s);
            var c = U.Card("daily");
            c.Add(U.Kicker("CF Tilt Poker · online · resets at midnight"));
            c.Add(U.H3("Daily Freeroll"));
            string res = "";
            if (done && s.daily.TryGetValue(Calendar.RealToday(), out var r) && r.Count >= 2) res = r[1] > 0 ? $"Today: {Fmt.Ord((int)r[0])}, won {U.Money(r[1])}." : $"Today: {Fmt.Ord((int)r[0])}.";
            c.Add(U.Hint(done ? res + " Same hands for everyone today." : $"Free to play. Doesn't use a game slot. Streak: {s.streak} day{(s.streak == 1 ? "" : "s")}."));
            c.Add(U.Btn(done ? "Played today" : "Play the daily", () => Game.StartEvent("daily"), "wide", !done).Mt(8));
            return c;
        }

        static VisualElement Money(State s)
        {
            var c = U.Card("bills");
            int d = s.cal.d + 1; while (Calendar.Date(d).dd != 1) d++;
            double due = Calendar.Bills(s);
            var r = U.Row();
            var l = U.Col().Grow(); l.Add(U.Kicker("Rent and bills")); l.Add(U.P($"{U.Money(due)} due {Calendar.Str(d)} ({d - s.cal.d} days)"));
            l.Add(U.Hint($"{DB.Item("home", s.Eq("home"))?.name ?? "Home"} · {DB.Item("ride", s.Eq("ride"))?.name ?? "Ride"} · living costs"));
            r.Add(l);
            r.Add(U.Chip(s.br >= due * 3 ? "Covered" : s.br >= due ? "Tight" : "Short", s.br >= due * 3 ? C.Green : s.br >= due ? C.Gold : C.Red));
            c.Add(r);
            if (Calendar.Low(s)) c.Add(U.Hint("Your roll is low for your level. Play smaller, grind the freerolls, or wait for Slick.").Mt(6).Fg(C.Red));
            return c;
        }

        // Sunday on the Spacepad: pick the nights you'll play this week (3 online, 1 live on the weekend)
        public static void Planner(State s, bool first = false)
        {
            DreamApp.Sheet("Plan the week", box =>
            {
                bool keep = s.job == "nights";
                box.Add(U.Hint(keep ? $"Pick your nights. {Calendar.GamesCap} online games and one live game (Saturday or Sunday). Sleeping skips straight to the next night you picked."
                                    : $"Pick the nights you'll play. Full-time, any night works: {Calendar.GamesCap} online games, or a live game plus 1 online. Sleeping skips straight to the next night you picked.").Mt(4));
                int start = Calendar.WeekStart(s.cal.d);
                VisualElement rows = null;
                void Draw()
                {
                    rows.Clear();
                    int on = 0, lv = 0; for (int i = 0; i < 7; i++) { var w = Calendar.PlanFor(s, start + i); if (w == "online") on++; else if (w == "live") lv++; }
                    for (int i = 0; i < 7; i++)
                    {
                        int d = start + i; var D = Calendar.Date(d); bool past = d < s.cal.d, wkend = D.dow == 0 || D.dow == 6; string w = Calendar.PlanFor(s, d);
                        var r = U.Row().Cls("plan-row"); if (past) r.AddToClassList("past");
                        r.Add(U.T(Calendar.DowL[D.dow] + (d == s.cal.d ? " · today" : ""), "plan-day").Grow());
                        if (!past)
                        {
                            r.Add(U.Btn("Online", () => { Calendar.SetPlan(s, d, w == "online" ? null : "online"); Draw(); }, w == "online" ? "gold sm" : "ghost sm", w == "online" || !keep || on < Calendar.GamesCap));
                            if (wkend || !keep) r.Add(U.Btn("Live", () => { Calendar.SetPlan(s, d, w == "live" ? null : "live"); Draw(); }, w == "live" ? "gold sm" : "ghost sm", w == "live" || !keep || lv < Calendar.LiveCap).Ml(6));
                        }
                        else r.Add(U.Hint(w == null ? "—" : w == "live" ? "live" : "online"));
                        rows.Add(r);
                    }
                }
                rows = U.Col().Cls("plan-rows").Mt(10); box.Add(rows); Draw();
                if (first) Tips.Later(300, () => Tips.Show("t13", "Plan your week", keep
                    ? "Pick your *3 online nights* and *1 weekend live night*. Those are the only nights you can play while you work for Lou. Tap *Done* when you're set."
                    : "Pick the nights you'll play. Full-time, *any night* works: *3 online games*, or a *live game plus 1 online*. Planning tells the game *which nights to take you to*. Tap *Done* when you're set.", rows));
                box.Add(U.Btn("Done", () => { Calendar.PlanDone(s); DreamApp.CloseModal(); if (first) Game.FirstPlanDone(); else Game.Hub(); }, "gold wide").Mt(12));
            });
            // the very first plan can't be skipped: no X, so the week always gets picked
            if (first) DreamApp.I.Modal.Query(className: "icon-btn").ForEach(e => e.style.display = DisplayStyle.None);
        }

        static VisualElement Job(State s)
        {
            var c = U.Card("job");
            if (s.job == "nights")
            {
                c.Add(U.Kicker("Day job · Floor 34"));
                c.Add(U.P($"{U.Money(Calendar.Salary)} every Friday. {Calendar.GamesCap} online games a week, plus one live game on the weekend."));
                var wk = Enumerable.Range(0, 7).Select(i => Calendar.WeekStart(s.cal.d) + i).Where(d => Calendar.PlanFor(s, d) != null).Select(d => Calendar.Dow[Calendar.Date(d).dow] + (Calendar.PlanFor(s, d) == "live" ? " live" : "")).ToList();
                c.Add(U.Hint(wk.Count > 0 ? "This week on the Spacepad: " + string.Join(" · ", wk) : "No plan this week: play whenever you have games left.").Mt(4));
                c.Add(U.Btn("Plan the week on the Spacepad", () => Planner(s), "wide").Mt(8));
                c.Add(U.Btn("Quit the day job", () => DreamApp.Confirm("Go full-time?", $"No more paycheck. Every day is yours: {Calendar.GamesCap} online games, or a live one plus online. Rent still comes due on the 1st.", "Quit", Game.QuitJob, "Not yet"), "ghost wide").Mt(8));
            }
            else
            {
                c.Add(U.Kicker("Full-time"));
                c.Add(U.P($"No paycheck. {Calendar.GamesCap} online games a day, or a live game plus 1 online."));
                if (s.FlagN("slick_off") > 0) c.Add(U.Hint("Slick isn't backing you right now. Get the roll back to $10,000.").Fg(C.Red).Mt(4));
                var wk2 = Enumerable.Range(0, 7).Select(i => Calendar.WeekStart(s.cal.d) + i).Where(d => Calendar.PlanFor(s, d) != null).Select(d => Calendar.Dow[Calendar.Date(d).dow] + (Calendar.PlanFor(s, d) == "live" ? " live" : "")).ToList();
                c.Add(U.Hint(wk2.Count > 0 ? "This week on the Spacepad: " + string.Join(" · ", wk2) : "No plan this week: every night is yours.").Mt(4));
                c.Add(U.Btn("Plan the week on the Spacepad", () => Planner(s), "wide").Mt(8));
                if (Calendar.Low(s)) c.Add(U.Btn("Ask Lou for the job back", () => DreamApp.Confirm("Back to Floor 34?", "Swallow your pride and ask Lou for your old desk?", "Ask him", Game.Rehire, "Not yet"), "ghost wide").Mt(8));
            }
            return c;
        }

        static VisualElement Majors(State s)
        {
            var c = U.Card("majors");
            c.Add(U.Kicker("Majors coming up"));
            foreach (var x in Calendar.Ahead(s, 6))
            {
                var r = U.Row().Cls("major");
                r.Add(U.T(x.sr.icon, "major-i"));
                var l = U.Col().Grow();
                l.Add(U.T(x.sr.n, "major-n", Fonts.Semi));
                int left = x.s0 - s.cal.d;
                string when = left <= 0 ? $"Running now · day {s.cal.d - x.s0 + 1} of {x.sr.days}" : $"{Calendar.Str(x.s0)} – {Calendar.Str(x.s0 + x.sr.days - 1)} · in {left} day{(left == 1 ? "" : "s")}";
                l.Add(U.Hint($"{DB.Place(x.sr.venue)} · {when}"));
                r.Add(l);
                if (left > 0 && left <= 60) { int s0 = x.s0; r.Add(U.Btn("Skip to it", () => DreamApp.Confirm($"Sleep until {Calendar.Str(s0)}?", $"You'll skip {left} days of games." + (Enumerable.Range(s.cal.d + 1, left).Any(d => Calendar.Date(d).dd == 1) ? $" Rent ({U.Money(Calendar.Bills(s))}) comes out on the way." : ""), "Skip ahead", () => Game.SleepTo(s0), "Stay"), "ghost small")); }
                c.Add(r);
            }
            return c;
        }

        static VisualElement SleepBar(State s)
        {
            var sl = Calendar.SlotsLeft(s); var c = U.Col().Cls("sleep").Mt(6);
            string next = Calendar.Str(s.cal.d + 1, true);
            int skip = Calendar.SkipTo(s);
            if (skip > 1)
            {
                // the plan decides the nights: one sleep goes straight to the next one
                bool week = Calendar.AllPlayed(s) || Calendar.Date(s.cal.d + skip).dow == 0; string day = Calendar.DowL[Calendar.Date(s.cal.d + skip).dow];
                c.Add(U.Btn("Go to sleep · wake up " + day, () => Game.Sleep(0), "gold wide", true, (week ? $"New week: {Calendar.GamesCap} online games and a live one" : "Your next night on the Spacepad") + $" · {skip} nights"));
                return c;
            }
            else c.Add(U.Btn("Go to sleep", () => Game.Sleep(1), sl.left == 0 ? "gold wide" : "wide", true, sl.left > 0 ? $"{sl.left} game{(sl.left == 1 ? "" : "s")} left unplayed · wake up {next}" : $"Wake up {next}"));
            return c;
        }

        // ------------------------------------------------------------ story
        static VisualElement StoryTab(State s)
        {
            var col = U.Col();
            var seen = DB.Scenes.Values.Where(x => s.story.Contains(x.id)).ToList();
            var c = U.Card();
            c.Add(U.Kicker("Replay a scene"));
            if (seen.Count == 0) c.Add(U.Hint("Scenes you've seen show up here."));
            foreach (var sc in seen)
            {
                var r = U.Row().Cls("scene-row");
                var dot = U.Div("scene-dot").Bg(Screens.Tint(sc.place)); r.Add(dot);
                var l = U.Col().Grow(); l.Add(U.T(sc.title ?? DB.Place(sc.place), "scene-n", Fonts.Semi)); l.Add(U.Hint(sc.sub ?? DB.Place(sc.place, "sub"))); r.Add(l);
                string id = sc.id; r.Tap(() => Game.PlayScene(id, true));
                c.Add(r);
            }
            col.Add(c);
            var tc = U.Card(); tc.Add(U.Kicker("Tutorial"));
            tc.Add(U.Hint("The tips that explain each part of the game show once. Bring them all back to see them again."));
            tc.Add(U.Btn("Show the tips again", () => { Tips.Reset(s); DreamApp.Toast("Tips are back on"); }, "ghost wide").Mt(8));
            col.Add(tc);
            var t = U.Card(); t.Add(U.Kicker("Your story so far"));
            if (s.timeline.Count == 0) t.Add(U.Hint("The big moments of your career land here."));
            foreach (var m in s.timeline.Take(40))
            {
                var r = U.Row().Cls("moment"); r.Add(U.T(m.icon, "moment-i")); var l = U.Col().Grow(); l.Add(U.P(m.text)); l.Add(U.Hint(DateTimeOffset.FromUnixTimeMilliseconds((long)m.d).LocalDateTime.ToString("MMM d, yyyy"))); r.Add(l); t.Add(r);
            }
            col.Add(t);
            var dev = U.Row().Mt(10);
            dev.Add(U.Btn(Sound.On ? "Sound: on" : "Sound: off", () => { Sound.On = !Sound.On; Game.Render(); }, "ghost grow"));
            bool sdOff = s.FlagN("sd_skip") > 0;
            dev.Add(U.Btn(sdOff ? "All-in scenes: off" : "All-in scenes: on", () => { if (sdOff) s.flags.Remove("sd_skip"); else s.SetFlag("sd_skip"); s.Save(); Game.Render(); }, "ghost grow"));
            dev.Add(U.Btn("Restyle your look", Game.StartRestyle, "ghost grow"));
            dev.Add(U.Btn("Title screen", () => { Game.View = View.Title; Game.Render(); }, "ghost grow"));
            col.Add(dev);
            return col;
        }
    }
}
