// The game's flow: which screen you're on and what happens when you tap. Screens read from here and call these.
using System;
using System.Collections.Generic;
using System.Linq;

namespace CFDream
{
    public enum View { Title, Create, Hub, Scene, Run, Results, Profile, Transit }

    // getting to the game: "drive" (your ride), "fly" (out of town or your jet) or "online" (the CF Poker client)
    public class Trip { public string kind, ride; public EventDef ev; }

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
            Render();
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
            var s = State; var sc = Scene; var def = DB.Scenes[sc.id];
            if (sc.replay) { Scene = null; View = View.Hub; Tab = "story"; Render(); return; }
            s.sceneId = null; s.sceneI = 0;
            s.story.Add(sc.id); Story.OnEnd(sc.id, s);
            Career.Unlocks(s); Career.Progress(s); s.Save();
            Scene = null;
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
            if (!reentry) { Trip = new Trip { kind = TripKind(run.ev), ride = State.Eq("ride"), ev = run.ev }; View = View.Transit; Render(); return; }
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
            Trip = null;
            if (Run == null) { Hub(); return; }
            View = View.Run; NextTable();
        }
        public static void NextTable() { var run = Run; if (run == null) return; Play.PickTable(run); run.state = "ready"; Render(); }
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
        public static void Rehire() { if (State.job == "quit") PlayScene("rehire"); }
    }
}
