// The game's flow: which screen you're on and what happens when you tap. Screens read from here and call these.
using System;
using System.Collections.Generic;
using System.Linq;

namespace CFDream
{
    public enum View { Title, Create, Hub, Scene, Run, Results, Profile, Transit }

    // getting to the game: "drive" (your ride), "fly" (out of town or your jet) or "online" (the CF Poker client)
    public class Trip { public string kind, ride; public EventDef ev; public string kicker, place, sub; public Action then; }   // story trips carry their own caption and what comes after

    public static class Game
    {
        public static State State;
        public static View View = View.Title;
        public static string Tab = "home", ClosetCat = "", ProfileTab = "quests";
        public static Run Run;
        public static Trip Trip;
        public static Result Last;
        public static ScenePlay Scene;
        public static Look Draft = new Look(); public static string DraftName = ""; public static bool Restyle;
        public static List<(string icon, string text)> Morning;
        public static int CalSel = -1, CalWk;
        public static bool AnyDay; // testing: play any event regardless of the calendar
        public static Action Render = () => { };

        public static void Boot()
        {
            // Play mode can start without a script reload, so nothing from the last session may linger here
            Run = null; Last = null; Scene = null; Morning = null; Trip = null; Restyle = false; Draft = new Look(); DraftName = "";
            View = View.Title; Tab = "home"; ClosetCat = ""; CalSel = -1; CalWk = 0;
            DB.Load();
            State = State.Load();
            Calendar.Migrate(State);
            if (State.made) View = View.Hub;
            // a story scene was in progress: pick it up where it was
            if (State.made && !string.IsNullOrEmpty(State.sceneId) && DB.Scenes.ContainsKey(State.sceneId))
            {
                Scene = new ScenePlay { id = State.sceneId, i = Math.Max(0, Math.Min(State.sceneI, DB.Scenes[State.sceneId].beats.Count - 1)) }; View = View.Scene;
            }
        }

        // ------------------------------------------------------------ new career and the creator
        public static void NewCareer() { Draft = new Look(); DraftName = ""; Restyle = false; View = View.Create; Render(); }
        public static void Begin(string name)
        {
            var s = new State { made = true, name = string.IsNullOrWhiteSpace(name) ? "Rookie" : name.Trim(), look = Draft.Clone() };
            State = s; Career.Unlocks(s); s.newItems.Clear(); s.Save();
            PlayScene("p_office");
        }
        public static void StartRestyle() { Draft = State.look.Clone(); DraftName = State.name; Restyle = true; View = View.Create; Render(); }
        public static void SaveRestyle(string name) { State.look = Draft.Clone(); if (!string.IsNullOrWhiteSpace(name)) State.name = name.Trim(); Restyle = false; State.Save(); View = View.Hub; Tab = "closet"; Render(); }
        public static void CancelRestyle() { Restyle = false; View = View.Hub; Tab = "closet"; Render(); }

        // ------------------------------------------------------------ the hub
        public static void Hub()
        {
            View = View.Hub;
            var s = State;
            if (s.queue.Count > 0 && Run == null) { var id = s.queue[0]; s.queue.RemoveAt(0); s.Save(); PlayScene(id); return; }
            if (Run == null && s.FlagN("needjob") > 0) Tab = "home";
            Render();
            if (Run != null) return;
            // home after the office: look around, then Slick's offer lands on your phone
            if (s.FlagN("needjob") > 0) { if (CFDream.UI.Tips.Seen(s, "t10") || CFDream.UI.Tips.Off(s)) OfferSoon(4000); return; }
            // the first week: the Spacepad opens on its own, then the planner
            if (s.FlagN("needplan") > 0)
                CFDream.UI.Tips.Show("t12", "Your Spacepad", "This is your *Spacepad*, your laptop for everything poker: tonight's games, the calendar, your money and your job. The tabs at the bottom take you *Home*, to the *Closet*, *Online*, your *Career* and the *Story*.",
                    CFDream.UI.Tips.OnScreen(".navbar"), () => CFDream.UI.HubView.Planner(State, true));
        }
        // the walking tip is done: give them a moment in the apartment before the phone buzzes
        public static void HomeTipDone() { if (State != null && State.FlagN("needjob") > 0) OfferSoon(2500); }
        static bool offerSet;
        static void OfferSoon(int ms)
        {
            if (offerSet) return; offerSet = true;
            CFDream.UI.Tips.Later(ms, () =>
            {
                offerSet = false;
                if (State == null || State.FlagN("needjob") <= 0 || Run != null || View == View.Scene || View == View.Transit) return;
                CFDream.UI.DreamApp.CloseModal(); Sound.Play("text", 0.8f); PlayScene("p_offer");
            });
        }
        // the first week is planned: quit and you start now, keep the job and the story picks up on the first night you chose
        public static void FirstPlanDone()
        {
            var s = State; s.SetFlag("needplan", 0); s.Save();
            bool keep = s.job == "nights";
            string next = keep
                ? "Since you kept the job, the days go by at work. When you *go to sleep* you wake up on the *first night you picked*. Lou pays you every *Friday*. Every *Sunday* you plan the next week."
                : "Since you quit, your week *starts now*. Play tonight, then *go to sleep* to move to the next night you picked. Every *Sunday* you plan the next week.";
            CFDream.UI.Tips.Show("t14", "What happens next", next, null, () =>
            {
                if (keep)
                {
                    int first = -1; foreach (var k in s.cal.plan.Keys) if (k >= s.cal.d && (first < 0 || k < first)) first = k;
                    if (first > s.cal.d) { Morning = Calendar.Sleep(s, first - s.cal.d); CalSel = -1; CalWk = 0; }
                }
                PlayScene("c1");
            });
        }

        // ------------------------------------------------------------ story scenes
        public static void PlayScene(string id, bool replay = false)
        {
            id = Story.Resolve(id, State);
            if (!DB.Scenes.ContainsKey(id)) { Hub(); return; }
            Scene = new ScenePlay { id = id, replay = replay }; View = View.Scene;
            if (!replay) { State.sceneId = id; State.sceneI = 0; State.Save(); }
            Render();
        }
        static void Bookmark() { var sc = Scene; if (sc != null && !sc.replay && State != null) { State.sceneId = sc.id; State.sceneI = sc.i; State.Save(); } }
        public static Beat CurrentBeat()
        {
            var sc = Scene; if (sc == null) return null;
            if (sc.extra.Count > 0) return sc.extra[0];
            var beats = DB.Scenes[sc.id].beats; return sc.i < beats.Count ? beats[sc.i] : null;
        }
        public static void Advance()
        {
            var sc = Scene; if (sc == null) return;
            var b = CurrentBeat(); if (sc.extra.Count == 0 && b != null && b.choice != null) return; // pick an option first
            if (sc.extra.Count > 0) sc.extra.RemoveAt(0); else sc.i++;
            if (sc.extra.Count == 0 && sc.i >= DB.Scenes[sc.id].beats.Count) { SceneEnd(); return; }
            Bookmark(); Render();
        }
        public static void Choose(int k)
        {
            var sc = Scene; var b = CurrentBeat(); if (b == null || b.choice == null || k >= b.choice.Count) return;
            var c = b.choice[k];
            if (!sc.replay) Story.Act(sc.id, k, State);
            State.Save();
            if (c.go != null)
            {
                if (sc.replay) { Scene = new ScenePlay { id = c.go, replay = true }; Render(); return; }
                State.story.Add(sc.id); State.Save(); PlayScene(c.go); return;
            }
            sc.i++;
            if (c.line != null) sc.extra.Add(c.line);
            if (sc.extra.Count == 0 && sc.i >= DB.Scenes[sc.id].beats.Count) { SceneEnd(); return; }
            Bookmark(); Render();
        }
        public static void Skip()
        {
            var sc = Scene; if (sc == null) return;
            var beats = DB.Scenes[sc.id].beats; sc.extra.Clear();
            int k = beats.FindIndex(sc.i, b => b.choice != null);
            if (k >= 0) { sc.i = k; Bookmark(); Render(); return; }
            SceneEnd();
        }
        public static void SceneEnd()
        {
            Sound.Hush();
            var s = State; var sc = Scene; var def = DB.Scenes[sc.id];
            if (sc.replay) { Scene = null; View = View.Hub; Tab = "story"; Render(); return; }
            s.sceneId = null; s.sceneI = 0;
            if (!s.story.Contains(sc.id)) s.story.Add(sc.id);
            Story.OnEnd(sc.id, s);
            Career.Unlocks(s); Career.Progress(s); s.Save();
            Scene = null;
            if (def.end == "run") { if (Run != null) { View = View.Run; Render(); } else Hub(); return; }   // back to the felt
            // the prologue's road: office, a ride across town, the diner, Saturday at the card room, Monday, home
            if (sc.id == "p_office") { Go(new Trip { kind = "drive", ride = s.Eq("ride"), kicker = "2:13 AM", place = "Calle Ocho", sub = "Lucky Seven Diner", then = () => PlayScene("p_diner") }); return; }
            if (sc.id == "p_diner") { Go(new Trip { kind = "drive", ride = s.Eq("ride"), kicker = "Saturday · 11:40 AM", place = "Biscayne Card Room", sub = "The Biscayne Championship · your free seat", then = () => PlayScene("p_seat") }); return; }
            if (sc.id == "p_seat") { NoTrip = true; View = View.Hub; StartEvent("local"); NoTrip = false; if (Run == null) Render(); return; }
            if (sc.id == "p_after" && !s.story.Contains("p_offer"))
            {
                s.cal.d = Math.Max(s.cal.d, 7); s.SetFlag("needjob"); s.Save(); Tab = "home";
                Go(new Trip { kind = "drive", ride = s.Eq("ride"), kicker = "Monday · 7:10 PM", place = "Home", sub = "The long way back from Floor 34", then = Hub }); return;
            }
            if ((sc.id == "p_quit" || sc.id == "p_keep") && !s.story.Contains("c1"))   // home to the apartment: the Spacepad opens and the first week gets planned
            { s.SetFlag("needplan"); s.Save(); Tab = "calendar"; Hub(); return; }
            string end = def.end ?? "hub";
            if (DB.Scenes.ContainsKey(end)) { PlayScene(end); return; }
            if (DB.Event(end) != null) { View = View.Hub; StartEvent(end); if (Run == null) Render(); return; }
            Hub();
        }

        // ------------------------------------------------------------ an event
        public static void StartEvent(string id, bool reentry = false)
        {
            if (Run != null) return;
            var run = Play.Start(id, reentry); if (run == null) { Render(); return; }
            Run = run; Morning = null;
            // a short cutscene on the way, except for another bullet in an event you're already at
            if (!reentry && !NoTrip) { Trip = new Trip { kind = TripKind(run.ev), ride = State.Eq("ride"), ev = run.ev }; View = View.Transit; Render(); return; }
            View = View.Run; NextTable();
        }
        static string TripKind(EventDef ev)
        {
            if (!Play.LiveEv(ev)) return "online";
            return Calendar.TravelFor(State, ev) > 0 || State.Eq("ride") == "jet" ? "fly" : "drive";
        }
        // the cutscene is over (or skipped): take your seat
        public static void Arrive()
        {
            if (View != View.Transit) return;
            var then = Trip?.then; Trip = null;
            if (then != null) { then(); return; }
            if (Run == null) { Hub(); return; }
            View = View.Run; NextTable();
        }
        public static bool NoTrip;   // the story already drove you there
        static void Go(Trip t) { Trip = t; View = View.Transit; Render(); }
        public static void NextTable()
        {
            var run = Run; if (run == null) return; Play.PickTable(run); run.state = "ready";
            // the final table: first a scene with whoever's waiting there
            if (run.Stage == "final" && !run.ftIntro && Story.FtIntro(run)) { run.ftIntro = true; PlayScene("ft_intro"); return; }
            Render();
        }
        // a study session on the Spacepad: your misplayed spots, again
        public static void StartDrill()
        {
            if (Run != null) return;
            var run = Play.StartDrill(State); if (run == null) { Render(); return; }
            Run = run; Morning = null; run.state = "ready"; View = View.Run; Render();
        }
        public static void Deal() { var run = Run; if (run == null) return; run.spot = Play.Deal(run); run.state = "play"; Render(); }
        public static void Answer(int j) { var run = Run; if (run == null || run.state != "play") return; Play.Answer(run, j); Render(); }
        public static void NextHand()
        {
            var run = Run; if (run == null || run.state != "answered") return;
            var what = Play.Next(run);
            if (what == "hand") { Deal(); return; }
            if (what == "level") { NextTable(); return; }
            if (what == "cooler") { Render(); return; }
            Finish(false);
        }
        public static void Finish(bool quit)
        {
            var run = Run; if (run == null) return; Run = null;
            Last = Play.Finish(run, quit); View = View.Results;
            var L = Last; if (L != null) Sound.Play(L.bust ? "bust" : L.place == 1 && L.ev.kind != "cash" ? "win" : L.prize > 0 || L.net > 0 ? "chips" : "bust", L.bust ? 0.9f : 1f);
            Render();
        }
        public static void Reenter() { var ev = Last?.ev; if (ev == null) return; StartEvent(ev.id, true); }

        // ------------------------------------------------------------ the calendar
        public static void Sleep(int days = 1)
        {
            var s = State;
            if (days <= 0) { days = Math.Max(1, Calendar.SkipTo(s)); }
            Morning = Calendar.Sleep(s, Math.Max(1, days)); CalSel = -1; CalWk = 0; Hub();
            if (Calendar.PlanDue(s)) CFDream.UI.HubView.Planner(s);   // Sunday with the day job: the week gets planned on the Spacepad first
        }
        public static void SleepTo(int d) { int n = d - State.cal.d; if (n > 0) Sleep(n); }
        public static void QuitJob() { if (State.job == "nights") PlayScene("quit2"); }
        public static void DeclineInvite() { var c = State.cal; if (c.inv == null) return; Career.Moment(State, "📭", $"Passed on the {c.inv.ev.name}"); c.inv = null; State.Save(); Render(); }
        public static void MeetAccept() { Calendar.MeetTake(State); State.Save(); Render(); }
        public static void MeetSkip() { State.cal.meet = null; State.Save(); Render(); }

        // ------------------------------------------------------------ the closet
        public static void Equip(string slot, string id)
        {
            var s = State; if (id != "none" && !s.Owns(slot, id) && !(slot == "outfit" && id == "office")) return;
            s.eq[slot] = id; s.newItems.Remove(slot + ":" + id); Fashion.LookSync(s); s.Save(); Render();
        }
        public static void Dye(string id, int v) { if (v > 0) State.dye[id] = v; else State.dye.Remove(id); Fashion.LookSync(State); State.Save(); Render(); }
        public static void SetFinish(string slot, string id, int v) { string k = slot + ":" + id; if (v >= 0) State.fin[k] = v; else State.fin.Remove(k); Fashion.LookSync(State); State.Save(); Render(); }
        // Look 1 / Look 2: two outfits you flip between; whatever you change goes into the one that's on
        public static void LookSwitch(int k) { Fashion.LookSwitch(State, k); State.Save(); Render(); }
        public static void Randomize()
        {
            var s = State; var rnd = new Random();
            foreach (var slot in Fashion.Wearable)
            {
                var pool = DB.Items.Where(i => i.slot == slot && (s.Owns(slot, i.id) || (slot == "outfit" && i.id == "office"))).Select(i => i.id).ToList();
                bool required = slot == "outfit" || slot == "bottom" || slot == "shoes";
                if (!required) { pool.Add("none"); if (rnd.NextDouble() < 0.35) { s.eq[slot] = "none"; continue; } }
                if (pool.Count > 0) s.eq[slot] = pool[rnd.Next(pool.Count)];
            }
            Fashion.LookSync(s); s.Save(); Render();
        }
        public static void FitSave(int i) { Fashion.FitSave(State, i); State.Save(); Render(); }
        public static void FitWear(int i) { Fashion.FitWear(State, i); State.Save(); Render(); }
        public static void SetTab(string t) { Tab = t; View = View.Hub; Render(); }
        // full-time and asking for the old desk: Lou says no; Slick covers you once a month (not while he's walked away)
        public static void Rehire()
        {
            var s = State; if (s.job != "quit") return;
            int last = (int)s.FlagN("lou500") - 1; bool gift = s.FlagN("slick_off") <= 0 && (last < 0 || s.cal.d - last >= 30);
            PlayScene(gift ? "lou_no" : "lou_no2");
        }
        // registering: a buy-in that's a shot for your roll gets a text from Slick first (three shots and a bust, and he walks)
        public static void Register(string id)
        {
            var s = State; var ev = Calendar.OnToday(s, id)?.ev ?? DB.Event(id);
            if (ev == null || s.ch < 1 || s.FlagN("slick_off") > 0) { StartEvent(id); return; }
            var risk = Career.Risk(s, ev);
            if (risk.k != "shot" && risk.k != "yolo") { StartEvent(id); return; }
            int n = (int)s.FlagN("shots"); double pct = Math.Round(Career.CostToday(s, ev) / Math.Max(1, s.br) * 100);
            string warn = n == 0 ? "Take three shots above your roll and go broke, and I stop bailing you out." : n == 1 ? "That'd be your second shot. Remember what I said." : "That's three. Go broke after this and you're on your own.";
            Sound.Play("text", 0.8f);
            CFDream.UI.DreamApp.Confirm("Slick", $"\"{ev.name} is {pct}% of your roll. That's a shot, kid. You sure?\"\n\n{warn}", "Take the shot",
                () => { s.SetFlag("shots", n + 1); Career.Moment(s, "🎯", $"Took a shot: {ev.name} ({pct}% of the roll)"); s.Save(); StartEvent(id); }, "Play smaller");
        }
    }
}
