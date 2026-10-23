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
        static readonly (string id, string n)[] Tabs = { ("calendar", "Calendar"), ("closet", "Closet"), ("career", "Career"), ("story", "Story") };

        public static VisualElement Build()
        {
            var s = Game.State;
            var page = Screens.Page("hub");
            var sv = Screens.Scroll();
            var body = U.Div("pad");
            if (DreamApp.Landscape)
            {
                page.AddToClassList("two-pane");
                var leftSv = new ScrollView(ScrollViewMode.Vertical); leftSv.AddToClassList("pane-left");
                leftSv.verticalScrollerVisibility = ScrollerVisibility.Hidden; leftSv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                var lb = U.Div("pad"); lb.Add(Header(s)); lb.Add(Chapter(s)); lb.Add(TabBar(s)); leftSv.Add(lb);
                page.Add(leftSv);
            }
            else { body.Add(Header(s)); body.Add(Chapter(s)); body.Add(TabBar(s)); }
            switch (Game.Tab)
            {
                case "closet": body.Add(ClosetView.Build(s)); break;
                case "career": body.Add(CareerView.Build(s)); break;
                case "story": body.Add(StoryTab(s)); break;
                default: body.Add(CalendarTab(s)); break;
            }
            body.Add(U.Space(40));
            sv.Add(body); page.Add(sv);
            return page;
        }

        // ------------------------------------------------------------ header
        static VisualElement Header(State s)
        {
            var h = U.Card("hub-head");
            var row = U.Row();
            var pt = new Portrait(Fig.Of(s), 64); pt.Tap(() => Game.SetTab("closet")); row.Add(pt);
            var who = U.Col().Grow(); who.style.marginLeft = 10;
            who.Add(U.T(s.name, "hub-name", Fonts.Disp));
            var title = J.S(DB.Obj("DR_TITLES_COS"), s.pg.eq.TryGetValue("title", out var tt) ? tt : "none", "");
            who.Add(U.Hint(Career.Rep(s) + (tt != null && tt != "none" && !string.IsNullOrEmpty(title) ? " · " + title : "")));
            var lv = Progression.LevelOf(s.pg.xp);
            var lvr = U.Row().Mt(6); lvr.Add(U.Chip("Lv " + lv.lvl, C.Gold)); var xb = U.Bar(lv.into / Math.Max(1, lv.need), C.Gold, 5).Grow(); xb.style.marginLeft = 8; lvr.Add(xb);
            who.Add(lvr);
            row.Add(who);
            var money = U.Col().Cls("hub-money");
            money.Add(U.Num(U.Money(s.br), "hub-br").Fg(s.br < 100 ? C.Red : C.Gold2));
            money.Add(U.T("BANKROLL", "stat-l", Fonts.Bold));
            row.Add(money);
            h.Add(row);
            if (s.debt > 0) h.Add(U.Hint($"You owe Slick {U.Money(s.debt)}. Half of every profit goes to him until it's paid.").Mt(8).Fg(C.Red));
            return h;
        }

        static VisualElement Chapter(State s)
        {
            var ch = DB.Chapter(s.ch);
            var c = U.Card("chapter");
            c.Add(U.Kicker($"{J.S(ch, "n")} · {J.S(ch, "t")}"));
            c.Add(U.P(J.S(ch, "goal", "")).Cls("goal-t"));
            var pr = Career.ChapterProg(s);
            if (s.ch >= 1)
            {
                c.Add(U.Bar(pr.frac, C.Gold, 6).Mt(8));
                if (!string.IsNullOrEmpty(pr.text)) c.Add(U.Hint(pr.text).Mt(4));
            }
            return c;
        }

        static VisualElement TabBar(State s)
        {
            var bar = U.Row().Cls("tabs");
            foreach (var (id, n) in Tabs)
            {
                var t = U.Div("tab").Cls(Game.Tab == id ? "on" : "");
                t.Add(U.T(n, "tab-t", Fonts.Semi).NoPick());
                if (id == "closet" && s.newItems.Count > 0) t.Add(U.Div("tab-dot").NoPick());
                string k = id; t.Tap(() => Game.SetTab(k));
                bar.Add(t);
            }
            return bar;
        }

        // ------------------------------------------------------------ the calendar
        static VisualElement CalendarTab(State s)
        {
            var col = U.Col(); var c = s.cal;
            if (Game.Morning != null && Game.Morning.Count > 0) col.Add(Morning());
            col.Add(Today(s));
            if (s.ch >= 1) col.Add(Week(s));
            if (Game.CalSel > c.d) col.Add(DayPreview(s, Game.CalSel));
            if (c.meet != null && c.meet.d == c.d) col.Add(Meetup(s));
            if (c.inv != null) col.Add(Invite(s));

            var today = Calendar.Today(s).Where(x => x.why != "invite").ToList();
            string pick = Career.Pick(s);
            col.Add(U.Kicker(s.ch == 0 ? "Saturday" : "Tonight").Mt(10));
            if (today.Count == 0) col.Add(U.Card().Kids(U.P("Nothing on the schedule tonight.")));
            foreach (var de in today.OrderByDescending(x => x.ev.id == pick).ThenByDescending(x => x.ev.kind == "mtt" ? x.ev.buyin : 0)) col.Add(EventCard(s, de.ev, de.why, de.ev.id == pick));

            if (s.ch >= 1)
            {
                foreach (var L in Calendar.LiveNow()) col.Add(LiveCard(s, L));
                col.Add(DailyCard(s));
                col.Add(Money(s));
                col.Add(Job(s));
                col.Add(Majors(s));
                col.Add(SleepBar(s));
            }
            return col;
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

        static VisualElement Today(State s)
        {
            var c = U.Card("today"); var D = Calendar.Date(s.cal.d); var sl = Calendar.SlotsLeft(s);
            var r = U.Row();
            var dcol = U.Col().Grow();
            dcol.Add(U.Kicker($"Year {D.y} · {Calendar.MonL[D.m - 1]}"));
            dcol.Add(U.H2(Calendar.Str(s.cal.d, true)));
            string status = s.ch == 0 ? "Prologue" : s.job == "nights" ? $"Day job · {sl.left} of {sl.cap} sessions left this week · payday Friday" : $"Full-time · {sl.left} of {sl.cap} games left today";
            dcol.Add(U.Hint(status));
            r.Add(dcol);
            var pips = U.Row().Cls("pips");
            for (int i = 0; i < sl.cap; i++) pips.Add(U.Div("pip").Cls(i < sl.used ? "used" : ""));
            r.Add(pips);
            c.Add(r);
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
            c.Add(U.Kicker(tonight ? "Private game · tonight" : "Private game · tomorrow night").Fg(C.Violet));
            c.Add(U.H2(ev.name));
            c.Add(U.P(ev.blurb).Mt(4));
            double pct = ev.buyin / Math.Max(1, s.br) * 100;
            var risk = Career.Risk(s, ev);
            var chips = U.Row().Cls("wrap").Mt(8);
            chips.Add(U.Chip($"Bring {U.Money(ev.buyin)}", C.Cream));
            chips.Add(U.Chip(s.br > 0 ? $"{Math.Round(pct)}% of your roll" : "You're broke", pct <= 5 ? C.Green : pct <= 20 ? C.Gold : C.Red));
            if (Calendar.TravelFor(s, ev) > 0) chips.Add(U.Chip($"Travel {U.Money(Calendar.TravelFor(s, ev))}", C.Mute));
            c.Add(chips);
            c.Add(U.Hint(pct > 20 ? "A real shot. Lose it and you're rebuilding for weeks." : pct > 5 ? "Bigger than you usually play. The table is soft." : "Comfortable. Soft table, good money.").Mt(6));
            var b = U.Row().Mt(10);
            b.Add(U.Btn("Pass", () => DreamApp.Confirm("Pass on the game?", "Invites like this don't come around often.", "Pass", Game.DeclineInvite, "Keep it"), "ghost grow"));
            var sl = Calendar.SlotsLeft(s);
            if (tonight) b.Add(U.Btn("Sit down", () => Game.StartEvent("inv"), "gold grow", risk.k != "no" && sl.left > 0, risk.k == "no" ? risk.label : sl.left <= 0 ? "No games left" : null));
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
            string kind = ev.kind == "cash" ? "Cash game" : ev.kind == "sat" ? $"Satellite · {ev.seats} seats" : ev.field > 0 ? $"{ev.field:#,0} players" : "Tournament";
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
            if (ev.kind == "sat" && !string.IsNullOrEmpty(ev.target)) chips.Add(U.Chip("Wins a seat: " + (DB.Event(ev.target)?.name ?? ev.target), C.Blue));
            var tr = Calendar.TravelFor(s, ev); if (tr > 0) chips.Add(U.Chip($"Hotel {U.Money(tr)}", C.Mute));
            if (!string.IsNullOrEmpty(ev.title)) chips.Add(U.Chip(DB.TitleIcon(ev.title) + " " + DB.TitleName(ev.title), C.Gold));
            if (ev.legends != null && ev.legends.Count > 0) chips.Add(U.Chip("A legend is in the field", C.Violet));
            if (ev.online) chips.Add(U.Chip("Online", C.Blue));
            c.Add(chips);
            if (!string.IsNullOrEmpty(ev.blurb)) c.Add(U.Hint(ev.blurb).Mt(6));
            string why2 = risk.k == "no" ? risk.label : sl.left <= 0 && !ev.daily ? (s.job == "nights" ? "No sessions left this week" : "No games left today") : null;
            c.Add(U.Btn(ev.kind == "cash" ? "Sit down" : "Register", () => Game.StartEvent(ev.id), pick ? "gold wide" : "wide", why2 == null, why2).Mt(10));
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

        static VisualElement Job(State s)
        {
            var c = U.Card("job");
            if (s.job == "nights")
            {
                c.Add(U.Kicker("Day job · Floor 34"));
                c.Add(U.P($"{U.Money(Calendar.Salary)} every Friday. {Calendar.GamesCap} poker sessions a week."));
                c.Add(U.Btn("Quit the day job", () => DreamApp.Confirm("Go full-time?", $"No more paycheck. You'll get {Calendar.GamesCap} games a day, and rent still comes due on the 1st.", "Quit", Game.QuitJob, "Not yet"), "ghost wide").Mt(8));
            }
            else
            {
                c.Add(U.Kicker("Full-time"));
                c.Add(U.P($"No paycheck. {Calendar.GamesCap} games a day."));
                if (Calendar.Low(s)) c.Add(U.Btn("Ask Lou for the job back", () => DreamApp.Confirm("Back to Floor 34?", $"{U.Money(Calendar.Salary)} every Friday, but only {Calendar.GamesCap} sessions a week.", "Ask him", Game.Rehire, "Not yet"), "ghost wide").Mt(8));
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
            if (s.job == "nights" && sl.left == 0)
            {
                int toSun = 7 - Calendar.Date(s.cal.d).dow;
                c.Add(U.Btn("Sleep until Sunday", () => Game.Sleep(0), "gold wide", true, $"New week, {Calendar.GamesCap} new sessions · skips {toSun} day{(toSun == 1 ? "" : "s")}"));
                c.Add(U.Btn("Sleep one night", () => Game.Sleep(1), "ghost wide", true, next));
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
            var t = U.Card(); t.Add(U.Kicker("Your story so far"));
            if (s.timeline.Count == 0) t.Add(U.Hint("The big moments of your career land here."));
            foreach (var m in s.timeline.Take(40))
            {
                var r = U.Row().Cls("moment"); r.Add(U.T(m.icon, "moment-i")); var l = U.Col().Grow(); l.Add(U.P(m.text)); l.Add(U.Hint(DateTimeOffset.FromUnixTimeMilliseconds((long)m.d).LocalDateTime.ToString("MMM d, yyyy"))); r.Add(l); t.Add(r);
            }
            col.Add(t);
            var dev = U.Row().Mt(10);
            dev.Add(U.Btn("Restyle your look", Game.StartRestyle, "ghost grow"));
            dev.Add(U.Btn("Title screen", () => { Game.View = View.Title; Game.Render(); }, "ghost grow"));
            col.Add(dev);
            return col;
        }
    }
}
