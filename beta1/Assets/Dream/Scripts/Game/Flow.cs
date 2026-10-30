// Test Beta 1.0's whole game: title -> reel (first launch) -> the Prologue (Yarn: films and the diner question) -> the
// Biscayne Championship -> Monday (the office, then Slick's offer on the phone at home: quit or keep the job) -> Chapter 1
// hub and events -> the table -> results -> Chapter 1's story -> the goal -> the end card. Saves in PlayerPrefs.
// The calendar is Steven's (old Calendar.cs): live games one a day full-time or one a weekend with the day job, online
// games three a day or three a week, the Biscayne Classic on the last Saturday of the month with Day 2 on Sunday.
using System;
using System.Collections.Generic;
using System.Linq;
using Dream.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Dream.Game
{
    [Serializable]
    public class Save
    {
        public string step = "new";           // new, prologue, choice, ch1, done
        public string name = "";              // what you typed in the creator (Yarn: $name); "Rookie" if you typed nothing
        public double br, peak, debt;         // bankroll, best bankroll, what's owed to Slick
        public string job = "";               // "quit" (full-time) or "nights" (kept the day job), as in the old game
        public int day = -1, liveDay = -99, liveWeek = -99;   // calendar day (0 = Mon 5 Jan 2026); last live game day and week
        public int onlineDay = -99, onlineN, onlineWeek = -99, onlineWN;   // online games played today / this week
        public bool reelSeen, roomTitle, day2, classicDone, c2Seen;   // c2Seen: Chapter 2's opening storyboard played (A4)
        public int rings, bracelets, awayUntil = -1; public string ticket = "";   // Chapter 3: bracelets; awayUntil: flown home   // Chapter 2: Circuit rings won; a satellite seat (the event id it gets you into)   // day2: chips bagged for Day 2; classicDone: the Classic is over (Chapter 1 ends)
        public int titles, coolers;
        public List<string> story = new List<string>();   // Yarn's story variables ($job, seen flags), saved as name=value
        public int events, cashes, decisions, perfect;
        public string lastEv = ""; public int lastPlace; public double lastPrize; public string lastMet = "";   // for the films' {keys}
        public List<string> log = new List<string>();
        public List<string> eq = new List<string>();   // what you wear and own, as "slot:id" (Home.cs: the closet, the ride, the home)

        public int rev;   // counts saves: of the copies on the device, the highest valid one is the career

        // hotfix (Steven 11:01: a crash wiped his save): two copies, PlayerPrefs (IndexedDB on the web, synced
        // asynchronously) and, on the web, localStorage (written at once). Load keeps the newest one that parses; nothing
        // but the player's own "New career" ever deletes a career.
        const string Key = "dream_beta1_save";
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void Dream_SaveMirror(string json);
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern string Dream_LoadMirror();
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void Dream_EraseMirror();
#else
        static void Dream_SaveMirror(string json) { }
        static string Dream_LoadMirror() => "";
        static void Dream_EraseMirror() { }
#endif
        static Save Parse(string j) { try { return string.IsNullOrEmpty(j) ? null : JsonUtility.FromJson<Save>(j); } catch (Exception e) { Debug.LogWarning("[Dream] a save copy didn't parse: " + e.Message); return null; } }
        public static Save Load()
        {
            var a = Parse(PlayerPrefs.GetString(Key, "")); Save b = null;
            try { b = Parse(Dream_LoadMirror()); } catch (Exception e) { Debug.LogWarning("[Dream] save mirror: " + e.Message); }
            var s = a == null ? b : b == null ? a : (b.rev > a.rev ? b : a);
            if (s != null) Debug.Log($"[Dream] save loaded: rev {s.rev}, step {s.step}, day {s.day} ({(s == b && b != a ? "from the mirror" : "from PlayerPrefs")})");
            return s;
        }
        public void Write()
        {
            rev++; var j = JsonUtility.ToJson(this);
            PlayerPrefs.SetString(Key, j); PlayerPrefs.Save();
            try { Dream_SaveMirror(j); } catch (Exception e) { Debug.LogWarning("[Dream] save mirror: " + e.Message); }
        }
        public static void Erase() { PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); try { Dream_EraseMirror(); } catch { } }
    }

    public partial class Flow : MonoBehaviour
    {
        public static Flow I;
        public static EventDef LastEvent;   // where the story is now: films and lines are checked against it (Moments.FilmOk)
        public Save S;
        Canvas canvas; RectTransform root;
        // Test Beta 1.1 (Steven, 13:31/13:36): Chapter 1 lasts a month and ends with the Biscayne Classic, played on
        // Saturday 14 February 2026 (day 40; Day 2 on the Sunday). The bankroll no longer ends it: $5,000 before the Classic
        // is a milestone (Slick's coverage off the table), and a weekly bill keeps online grinding from snowballing.
        public const int ClassicDay = 40;
        public const double Goal = 5000;                                          // the Classic stake milestone (also in Chapter1.yarn)
        public const double Bills = 600;                                          // rent and living, every Friday
        public const double Salary = 1250;                                        // every Friday with the day job (Steven, 11:27: $1,250 a week; the old game paid $2,000)
        public const int GamesCap = 3;                                            // online: per day full-time, per week with the day job
        public const double Low = 400;                                            // old Calendar.LowMark for Chapter 1: the freeroll opens
        Story story;
        static readonly DateTime Day0 = new DateTime(2026, 1, 5);                 // old Calendar.Day0: a Monday
        static DateTime Date(int d) => Day0.AddDays(d);
        static int Dow(int d) => (int)Date(d).DayOfWeek;
        static int WeekOf(int d) => (int)Math.Floor((d + 1) / 7.0);               // weeks run Sunday to Saturday
        static bool Weekend(int d) { int w = Dow(d); return w == 0 || w == 6; }
        static bool LastSaturday(int d) => d == ClassicDay;   // the Classic's day in Chapter 1 (was: the last Saturday of the month)
        static string DateStr(int d) => Date(d).ToString("dddd, MMMM d", System.Globalization.CultureInfo.InvariantCulture);
        public string Rep => S.step == "ch3" || S.step == "end4" ? (S.bracelets >= 1 ? "Bracelet Winner" : "Summer Grinder") : S.step == "ch2" || S.step == "end" ? (S.rings >= 2 ? "Two-time Ring Winner" : S.rings == 1 ? "Ring Winner" : "Circuit Grinder") : S.roomTitle ? "Biscayne Champion" : S.job == "quit" ? "Full-time Dreamer" : "Moonlighter";   // old Career.Rep, Chapter 1
        // Steven, 14:54: every build is "Test Beta 1.1.x", x bumped per build, until he says 2.0
        public const string Version = "1.1.5";
        public const string FeedbackUrl = "https://github.com/Andrxtti/slick-rng/issues/new?title=The%20Dream%20Test%20Beta%20" + Version + "%20feedback";

        // ------------------------------------------------------------ the calendar (old Calendar.cs, Chapter 1)
        // what's on today: the nightly and the Global Rush every day, the Classic on the last Saturday of the month,
        // Day 2 the next day if you bagged chips, the freeroll while your roll is low
        IEnumerable<EventDef> Today()
        {
            if (S.step == "ch2") { foreach (var e in TodayCh2()) yield return e; yield break; }
            if (S.step == "ch3") { foreach (var e in TodayCh3()) yield return e; yield break; }
            foreach (var ev in Lib.Events.Where(e => e.ch == 1))
            {
                if (ev.id == "classic" && (!LastSaturday(S.day) || S.day2 || S.classicDone)) continue;   // one Day 1: bagged or out, it's done
                if (ev.id == "classic2" && !(S.day2 && S.day == ClassicDay + 1)) continue;
                if (ev.id == "freeroll" && S.br >= Low) continue;
                yield return ev;
            }
        }
        int OnlineToday => S.onlineDay == S.day ? S.onlineN : 0;
        int OnlineWeek => S.onlineWeek == WeekOf(S.day) ? S.onlineWN : 0;
        // null when you can play it today (old Calendar.CantPlay)
        string CantPlay(EventDef ev)
        {
            if (ev.id == "classic2" || ev.id == "classic") return null;   // the Classic is its own day, whatever else you played that week
            if (ev.online)
            {
                if (S.job == "nights") return OnlineWeek >= GamesCap ? "No online games left this week" : null;
                if (OnlineToday >= GamesCap) return "No online games left today";
                return S.liveDay == S.day && OnlineToday >= 1 ? "A live day is one live game and one online" : null;
            }
            if (S.job == "nights" && !Weekend(S.day)) return "Live games are weekends only while you have the day job";
            if (S.job == "nights" && S.liveWeek == WeekOf(S.day) && StopDay(S.day) < 0 && SeriesDay(S.day) < 0) return "Your live game this weekend is played";   // a Circuit stop: both weekend days
            if (S.liveDay == S.day) return "One live game a day";
            if (S.job != "nights" && OnlineToday >= 2) return "Two online games played: a live day is one live and one online";
            return null;
        }

        void Start()
        {
            I = this; Application.targetFrameRate = 60; LastEvent = null;
            // ground truth for the web build's console: which pipeline and quality level the player is really running
            var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            Debug.Log($"[Dream] pipeline {(urp != null ? urp.name : "none")} · quality {QualitySettings.names[QualitySettings.GetQualityLevel()]} · upscaling {(urp != null ? urp.upscalingFilter.ToString() : "-")} · render scale {(urp != null ? urp.renderScale : 0)}");
            var cam = new GameObject("UiCamera").AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Ui.Night;
            canvas = Ui.Root("Dream"); root = (RectTransform)canvas.transform;
            Lib.Load();
            story = Story.Make(transform);
            Films.Fill = Fill;
            Voice.Preload(this);   // the voice lines stream in the background (StreamingAssets/Voice)
            Table.OnLevel = FinalTable;
            Title();
        }

        RectTransform Screen(Color bg) { Ui.Clear(root); return Ui.Panel(root, "Screen", bg); }

        // the title music (Steven, 11:55): "Retroracing Nightlife" by Bogart VGM, CC-BY 4.0, streamed from
        // StreamingAssets/Music/title.mp3, looped under the title and the creator with a soft fade-in
        AudioSource music; bool musicOn;
        void Music(bool on)
        {
            musicOn = on;
            if (music == null) { music = gameObject.AddComponent<AudioSource>(); music.loop = true; music.volume = 0; StartCoroutine(LoadMusic()); }
            StartCoroutine(FadeMusic());
        }
        System.Collections.IEnumerator LoadMusic()
        {
            var sa = Application.streamingAssetsPath; var url = (sa.Contains("://") ? sa : "file://" + sa) + "/Music/title.mp3";
            using var req = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG);
            yield return req.SendWebRequest();
            if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success) yield break;
            music.clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(req);
            if (musicOn) music.Play();
        }
        System.Collections.IEnumerator FadeMusic()
        {
            if (musicOn && music.clip != null && !music.isPlaying) music.Play();
            float to = musicOn ? 0.55f : 0f, speed = musicOn ? 0.22f : 0.8f;   // in over ~2.5 s, out in under a second
            while (music != null && !Mathf.Approximately(music.volume, to) && musicOn == (to > 0)) { music.volume = Mathf.MoveTowards(music.volume, to, Time.unscaledDeltaTime * speed); yield return null; }
            if (music != null && !musicOn && music.volume <= 0) music.Pause();
        }

        // ------------------------------------------------------------ title
        public void Title()
        {
            S = Save.Load(); Music(true);
            var s = Screen(Ui.Night);
            Ui.At(Ui.Label(s, "CONFIRMED FIRE PRESENTS", 26, Ui.Dim), 0, 0.8f, 1, 0.83f);
            Ui.At(Ui.Title(s, "The Dream", 150, Ui.Ink), 0, 0.64f, 1, 0.8f);
            var ver = Ui.At(Ui.Label(s, "TEST BETA " + Version, 30, Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Bold), 0.3f, 0.6f, 0.7f, 0.635f);
            ver.raycastTarget = true; ver.gameObject.AddComponent<Button>().onClick.AddListener(VersionTap);   // 5 taps: the test menu (DebugMenu.cs)
            Ui.At(Ui.Label(s, "Chapters 1 to 3 · The Grind, the Circuit, the Summer", 34, Ui.Ink), 0, 0.565f, 1, 0.6f);
            Ui.At(Ui.Label(s, "Every decision is a real solver spot.\nCoaching by Confirmed Fire.", 30, Ui.Dim), 0.1f, 0.46f, 0.9f, 0.53f);
            bool can = S != null && S.step != "new";
            Ui.At(Ui.Button(s, can ? "Continue" : "Begin", () => { if (can) Resume(); else NewCareer(); }, Ui.Gold, Ui.Night, 48), 0.18f, 0.3f, 0.82f, 0.36f);
            if (can) Ui.At(Ui.Button(s, "New career", () => { Save.Erase(); NewCareer(); }, Ui.Card, Ui.Ink, 36), 0.18f, 0.22f, 0.82f, 0.27f);
            Ui.At(Ui.Button(s, "Replay the reel", () => { Music(false); Films.Play(new[] { "reel" }, Title); }, new Color(1, 1, 1, 0.06f), Ui.Dim, 30), 0.3f, 0.13f, 0.7f, 0.17f);
            // the one setting (TABLE.md §10): the all-in plays out, or goes straight to its result
            bool skip = PlayerPrefs.GetInt("sd_skip", 0) > 0;
            Ui.At(Ui.Button(s, "All-in animations: " + (skip ? "off" : "on"), () => { PlayerPrefs.SetInt("sd_skip", skip ? 0 : 1); PlayerPrefs.Save(); Title(); }, new Color(1, 1, 1, 0.04f), Ui.Dim, 24), 0.3f, 0.08f, 0.7f, 0.115f);
        }

        void NewCareer()
        {
            bool seen = S != null && S.reelSeen;
            if (!seen) Music(false);   // the reel has its own score
            Films.Play(seen ? new string[0] : new[] { "reel" }, Create);
        }

        // the player's first real menu (Steven, 11:27): the character creator, as in the old game's Screens.Create. Your
        // name, and the dice. The films are rendered with one player, so the dice rolls a name rather than a look.
        static readonly string[] Names = { "Alex", "Sam", "Jordan", "Nico", "Danny", "Marco", "Chris", "Leo", "Ricky", "Tony", "Matt", "Eddie", "Ray", "Vince", "Jay" };
        void Create()
        {
            Music(true); var s = Screen(Ui.Night);
            Ui.At(Ui.Label(s, "CREATE YOUR PLAYER", 26, Ui.Gold, TextAnchor.MiddleLeft, FontStyle.Bold), 0.08f, 0.92f, 0.92f, 0.95f);
            var pic = Ui.At(Ui.Tile(s, "Portrait", Color.white, 36), 0.14f, 0.43f, 0.86f, 0.9f).GetComponent<Image>();
            pic.sprite = null; var tex = Resources.Load<Texture2D>("UI/you");
            if (tex != null) { var raw = new GameObject("You", typeof(RectTransform)).AddComponent<RawImage>(); raw.transform.SetParent(pic.transform, false); raw.texture = tex;
                var fit = raw.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = tex.width / (float)tex.height;
                pic.gameObject.AddComponent<RectMask2D>(); pic.color = Color.clear; }   // the portrait fills the frame, cropped, never stretched
            Ui.At(Ui.Label(s, "Your name", 26, Ui.Dim, TextAnchor.MiddleLeft), 0.08f, 0.37f, 0.92f, 0.4f);
            var field = Ui.Input(s, "Rookie", 72); Ui.At(field, 0.08f, 0.29f, 0.76f, 0.37f); field.characterLimit = 18;
            var dice = Ui.Button(s, "⚄", () => field.text = Names[UnityEngine.Random.Range(0, Names.Length)], Ui.Card, Ui.Gold, 64);
            Ui.At(dice, 0.79f, 0.295f, 0.92f, 0.365f);
            Ui.At(Ui.Label(s, "Everyone at Biscayne will still call you kid.", 26, Ui.Dim, TextAnchor.MiddleLeft), 0.08f, 0.24f, 0.92f, 0.28f);
            Ui.At(Ui.Button(s, "Start the story", () => Begin(field.text), Ui.Gold, Ui.Night, 46), 0.12f, 0.1f, 0.88f, 0.17f);
        }

        void Begin(string name)
        {
            S = new Save { step = "prologue", br = 0, day = 5, reelSeen = true, name = string.IsNullOrWhiteSpace(name) ? "Rookie" : name.Trim() }; S.Write();   // Saturday: the Biscayne Championship (free); the bankroll starts at zero, as in the old game
            LastEvent = null; Music(false);   // a new career is nowhere yet: the prologue's films are story beats
            story.Run("Prologue", () => Play("local"));
        }

        void Resume()
        {
            Music(false);
            if (S.step == "prologue") { Films.Play(new[] { "p_seat" }, () => Play("local")); return; }
            if (S.step == "choice") { story.Run("Monday", AfterStory); return; }
            if (S.step == "done") BeginChapter2();   // a 1.1.3 save that finished Chapter 1: on to the Circuit
            if (S.step == "end") BeginChapter3();    // a 1.1.4 save that finished Chapter 2: on to the Summer
            if (S.step == "end4") { EndCard(); return; }
            Hub();
        }

        // a Yarn choice in the game's style: the card lines on top (first small, second big, the rest small), one button
        // per option with its hint under it
        public void ChoiceCard(List<string> cardLines, List<string> options, List<string> hints, Action<int> pick)
        {
            var s = Screen(Color.black);
            float y = 0.76f;
            for (int i = 0; i < cardLines.Count; i++)
            {
                bool big = i == 1 || cardLines.Count == 1; float h = big ? 0.1f : 0.04f;
                Ui.At(big ? Ui.Title(s, cardLines[i], 84, Ui.Ink) : Ui.Label(s, cardLines[i].ToUpperInvariant(), 26, Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Bold), 0.06f, y - h, 0.94f, y);
                y -= h + 0.01f;
            }
            y -= 0.05f;
            for (int i = 0; i < options.Count; i++)
            {
                int k = i; bool first = i == 0;
                Ui.At(Ui.Button(s, options[i], () => pick(k), first ? Ui.Gold : Ui.Card, first ? Ui.Night : Ui.Ink, 42), 0.1f, y - 0.07f, 0.9f, y);
                if (i < hints.Count) Ui.At(Ui.Label(s, hints[i], 26, Ui.Dim), 0.08f, y - 0.11f, 0.92f, y - 0.075f);
                y -= 0.15f;
            }
        }

        // a plain Yarn line: the narrator on black, tap to go on
        public void Line(string text, Action next)
        {
            var s = Screen(Color.black);
            Ui.At(Ui.Title(s, text, 56, Ui.Ink), 0.1f, 0.35f, 0.9f, 0.65f).lineSpacing = 12;
            var b = s.gameObject.AddComponent<Button>(); b.onClick.AddListener(() => next());
            Ui.At(Ui.Label(s, "tap to continue", 24, new Color(1, 1, 1, 0.4f)), 0, 0.06f, 1, 0.09f);
        }

        // run a story node with a given job and bankroll (the test command "story <node> <job> <bankroll>")
        public void TestStory(string node, string job, double br)
        {
            S ??= new Save(); S.step = "ch1"; S.job = job; S.br = br; S.peak = Math.Max(S.peak, br); S.day = 13;
            S.story.RemoveAll(v => v.StartsWith("$job=")); S.story.Add("$job=" + job);
            story.Run(node, AfterStory, new Story.Outcome { ev = "nightly", bust = true });
        }

        // after a story run: the job decision moves the calendar to Monday night; the end of the chapter is the end card
        void AfterStory()
        {
            if (S.step == "ch1" && S.day < 7) { S.day = 7; S.Write(); }   // Monday night: Chapter 1 starts
            if (S.step == "ch2" && S.day < Ch2Start) BeginChapter2();   // Chapter 1 just ended: Tommy drove you north
            if (S.step == "ch3" && S.day < Ch3Start) BeginChapter3();   // Chapter 2 just ended: Las Vegas in June
            if (S.step == "end4") EndCard(); else Hub();
        }

        // to the next day (old Calendar.SkipTo when nothing is left today); the day job pays every Friday on the way, and
        // bagged chips for Day 2 are lost if Sunday goes by
        void Sleep()
        {
            var news = new List<string>();
            bool stuck = Today().All(e => CantPlay(e) != null);
            do
            {
                S.day++;
                if (Dow(S.day) == 5)   // Friday: the paycheck (with the day job) and the week's rent and living
                {
                    if (S.job == "nights") { S.br += Salary; news.Add($"Payday +{Ui.Money(Salary)}."); }
                    S.br = Math.Max(0, S.br - Bills); news.Add($"Rent and living \u2212{Ui.Money(Bills)}.");
                }
                if (S.day2 && S.day > ClassicDay + 1) { S.day2 = false; news.Add("You missed Day 2 of the Classic. Your chips were blinded out."); }
                if (S.day2 && S.day == ClassicDay + 1) news.Add("Day 2 of the Biscayne Classic. Noon. Your chips are bagged.");
                if (!S.classicDone && S.day > ClassicDay + 1) S.classicDone = true;   // the Classic came and went
            } while (stuck && Today().All(e => CantPlay(e) != null) && S.day < 400);
            S.peak = Math.Max(S.peak, S.br); S.Write();
            story.Run("AfterSleep", () => { if (S.step == "ch2" && S.day < Ch2Start) BeginChapter2(); if (S.step == "ch3" && S.day < Ch3Start) BeginChapter3(); if (S.step == "end4") EndCard(); else Hub(news.Count > 0 ? string.Join("  ", news) : null); });
        }

        // ------------------------------------------------------------ an event
        // before an event: Yarn may have a scene first (week two plays before your first live game from 19 January)
        void Enter(string id)
        {
            var ev = Lib.Event(id);
            story.Run("BeforeEvent", () => Play(id), new Story.Outcome { ev = id, online = ev.online });
        }

        void Play(string id)
        {
            var ev = Lib.Event(id);
            bool ticket = S.ticket == ev.id;   // a satellite seat pays the buy-in
            if (!ev.free && !ticket) S.br -= ev.buyin;
            if (ticket) S.ticket = "";
            if (ev.id == "classic2") S.day2 = false;
            if (ev.id == "classic2") S.classicDone = true;
            S.Write();
            Hands.Chapter = S.step == "prologue" ? 0 : S.step == "ch2" ? 2 : S.step == "ch3" ? 3 : 1;
            var run = Hands.Start(ev, $"{id}|{DateTime.UtcNow.Ticks}");
            Ui.Clear(root);
            Table.Show(root, run, (bad) => Finish(run, bad));
        }

        // whoever is waiting at the final table (old Story.FtIntro): Tommy at the Championship, Mustich on Day 2
        static string Boss(EventDef ev) => ev.boss == "lou" ? "tommy" : ev.boss == "trent" ? "mustich" : null;
        static string BossName(string who) => who == "tommy" ? "Tommy \"The Shark\" Maio" : who == "mustich" ? "Mustich" : who;
        bool FinalTable(Run run, Action go)
        {
            var who = Boss(run.ev);
            if (run.Stage != "final" || who == null) return false;
            S.lastEv = run.ev.name; S.lastMet = who;
            Films.Play(new[] { "ft_intro_" + who }, go);
            return true;
        }

        void Finish(Run run, bool hadMistake)
        {
            var ev = run.ev; int n = run.results.Count; double prize = 0; int place = 0, paid = 0;
            if (ev.kind == "cash") { double net = run.bust ? -ev.buyin : Hands.CashNet(run, (run.rng.Next() - 0.5) * 0.3 + ev.soft * 0.5); prize = ev.buyin + net; }
            else { paid = Hands.Paid(ev); place = Hands.Place(run); prize = ev.kind == "sat" || place > paid ? 0 : Hands.PrizeFor(ev, place); }
            bool qualified = ev.kind == "sat" && !run.bust && place <= ev.seats;
            if (S.debt > 0 && prize - (ev.free ? 0 : ev.buyin) > 0)   // half your profits go back to Slick until the stake is repaid
            {
                double back = Math.Min(S.debt, Math.Round((prize - (ev.free ? 0 : ev.buyin)) / 2)); S.debt -= back; prize -= back;
                S.log.Insert(0, $"Repaid Slick {Ui.Money(back)}" + (S.debt <= 0 ? " (all of it)" : ""));
            }
            S.br += prize; S.peak = Math.Max(S.peak, S.br); S.events++; S.decisions += n; S.perfect += run.results.Count(r => r.tier == "perfect");
            if (ev.online) { if (S.onlineDay != S.day) { S.onlineDay = S.day; S.onlineN = 0; } S.onlineN++; if (S.onlineWeek != WeekOf(S.day)) { S.onlineWeek = WeekOf(S.day); S.onlineWN = 0; } S.onlineWN++; }
            else if (ev.id != "classic2" && ev.id != "local") { S.liveDay = S.day; S.liveWeek = WeekOf(S.day); }
            if (prize > 0) S.cashes++;
            bool won = ev.kind == "mtt" && place == 1;
            if (won) S.titles++;
            if (won && ev.title == "room") S.roomTitle = true;
            if (won && ev.title == "ring") S.rings++;
            if (won && ev.title == "bracelet") S.bracelets++;
            if (qualified && !string.IsNullOrEmpty(ev.target) && ev.id != "classic") S.ticket = ev.target;   // the Mega Satellite: a seat in the Main
            if (ev.id == "classic2" && won) S.roomTitle = true;
            if (qualified && ev.id == "classic") S.day2 = true;
            if (ev.id == "classic" && !qualified) S.classicDone = true;   // out on Day 1: the Classic is over for you
            S.log.Insert(0, ev.kind == "cash" ? $"{ev.name}: {(prize - ev.buyin >= 0 ? "+" : "")}{Ui.Money(prize - ev.buyin)}" : qualified ? $"{ev.name}: bagged for Day 2" : $"{ev.name}: {Ord(place)} of {ev.field}" + (prize > 0 ? $", {Ui.Money(prize)}" : ""));
            bool bust = ev.kind == "cash" ? prize < ev.buyin * 0.5 : ev.kind == "sat" ? !qualified : prize <= 0;
            // who you met at the final table, and how it ended between you (old Play: res.met, Story.FtEnd)
            var who = Boss(ev); bool met = who != null && run.si >= run.stages.Count - 1 && (run.Stage == "final" || !run.bust);
            S.lastEv = ev.name; S.lastPlace = place; S.lastPrize = prize; S.lastMet = met ? who : "";
            string ftEnd = met && ev.id != "classic2" && Data("ft_" + (won ? "won_" : "lost_") + who) ? "ft_" + (won ? "won_" : "lost_") + who : "";
            string classicEnd = ev.id == "classic2" && !won ? ClassicLoss(met ? who : null) : "";
            if (ev.id == "local") S.step = "choice";
            S.Write();
            bool ft = run.si >= run.stages.Count - 1 && (run.Stage == "final" || !run.bust);   // you reached the final table
            bool bubble = ev.kind == "mtt" && paid > 0 && place > paid && place <= paid + Math.Max(3, paid / 10);   // just outside the money
            var o = new Story.Outcome { ev = ev.id, won = won, bust = bust, cooler = run.cooler != null, qualified = qualified, ftEnd = ftEnd, classicEnd = classicEnd, online = ev.online, ft = ft && ev.kind == "mtt", bubble = bubble };
            LastEvent = ev;
            void Res() => Results(run, place, paid, prize, qualified, () => story.Run("AfterEvent", AfterStory, o));
            string outcome = won ? "win" : run.cooler != null ? "cooler" : prize > 0 ? "cash" : "bust";
            if (qualified) Res();   // bagging chips has its own film (classic_day1)
            else { Ui.Clear(root); Moments.WalkAway(root, ev, outcome, place, prize, S.events, Res); }   // the walk away (old after_* scenes)
        }
        static bool Data(string film) => film == "ft_won_tommy" || film == "ft_lost_tommy";   // the final-table endings that are filmed

        // who holds the Classic's trophy when it isn't you (old Story.ClassicLoss): Mustich if he beat you at the final
        // table, otherwise Mustich one time in five, Tommy a little over one in ten, someone else the rest (Slick, while the
        // legends aren't built)
        string ClassicLoss(string metBy)
        {
            if (metBy == "mustich") return "classic_mustich";
            double r = new System.Random(S.day * 7919 + S.events).NextDouble();
            return r < 0.4 ? "classic_mustich" : r < 0.75 ? "classic_shark" : "classic_other";   // Steven: mix it up, Tommy and Mustich often
        }

        // the words the films leave to the game (old Story.Text)
        string Fill(string key)
        {
            string place = Ord(S.lastPlace), cash = Ui.Money(S.lastPrize);
            switch (key)
            {
                case "days": return Math.Max(0, ClassicDay - S.day).ToString();   // days to the Classic (CH1_WEEKS {days})
                case "br": return Ui.Money(S.br);
                case "debt": return Ui.Money(S.debt);
                case "saturday":
                    return S.lastPrize > 0 ? $"You finished {place} on Saturday and took home {cash}.\nYou've replayed every hand in your head since."
                        : "You busted Saturday. And you have never felt more alive.\nYou've replayed every hand in your head since.";
                case "ft_intro": return $"Final table. {S.lastEv}.\nNine players left, and one of them is {BossName(S.lastMet)}.";
                case "ft_won": return $"Final hand. You and {BossName(S.lastMet)}, everything in the middle.\nYour cards hold. {S.lastEv} is yours.";
                case "ft_lost": return $"Final hand. You and {BossName(S.lastMet)}, everything in the middle.\nTheir cards hold. You're out in {place}" + (S.lastPrize > 0 ? $", with {cash}." : ".");
                case "day2": return S.lastPlace > 0 ? $"Sunday night. Day 2 of the Biscayne Classic.\nYou went out in {place}" + (S.lastPrize > 0 ? $" for {cash}." : ", short of the money.") : "Sunday night. Day 2 of the Biscayne Classic.";
                case "day2_you": return S.lastPlace > 0 && S.lastPlace <= 9 ? $"Final table. {place}. Close enough to touch it." : S.lastPrize > 0 ? $"{place}. On the board, not the trophy. Next month." : "Made Day 2 and still went home with nothing. Next month.";
            }
            return "";
        }

        static string Ord(int n) => n + (n % 100 is 11 or 12 or 13 ? "th" : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" });

        void Results(Run run, int place, int paid, double prize, bool qualified, Action next)
        {
            var ev = run.ev; var s = Screen(Ui.Night);
            Ui.At(Ui.Label(s, ev.name.ToUpperInvariant(), 26, Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Bold), 0.05f, 0.89f, 0.95f, 0.92f);
            string head = ev.kind == "cash" ? (prize >= ev.buyin ? "Up " + Ui.Money(prize - ev.buyin) : "Down " + Ui.Money(ev.buyin - prize))
                : qualified ? "Bagged for Day 2." : place == 1 ? "You won it." : prize > 0 ? $"{Ord(place)} place" : run.cooler != null ? "Coolered." : $"Out in {Ord(place)}";
            Ui.At(Ui.Title(s, head, 96, Ui.Ink), 0.05f, 0.77f, 0.95f, 0.88f);
            string sub = qualified ? $"Top {ev.seats} of {ev.field} come back Sunday" : prize > 0 ? "Paid " + Ui.Money(prize) : ev.kind == "cash" ? "" : $"{paid} paid of {ev.field}";
            Ui.At(Ui.Num(s, sub, 36, prize > 0 || qualified ? Ui.Gold : Ui.Dim), 0.05f, 0.72f, 0.95f, 0.77f);
            // the big moments (old Run.cs results), in words
            string met = S.lastMet == "tommy" ? "Tommy" : S.lastMet == "mustich" ? "Mustich" : null, banner = null;
            if (place == 1 && met != null) banner = $"Beat {met} heads-up, for the title";
            else if (place == 1) banner = ev.online ? "Top of the CF Tilt lobby" : "A title for the trophy room";
            else if (met != null && place <= 9) banner = $"Final table with {met}: they'll remember your face";
            if (banner != null) Ui.At(Ui.Label(s, banner.ToUpperInvariant(), 24, Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Bold), 0.05f, 0.665f, 0.95f, 0.695f);
            if (run.cooler != null && banner == null) Ui.At(Ui.Label(s, "You did nothing wrong. " + run.cooler, 26, Ui.Dim), 0.08f, 0.66f, 0.92f, 0.71f);
            Ui.At(Ui.Label(s, $"ACCURACY {Hands.Accuracy(run.results)}%  ·  {run.results.Count} DECISIONS", 26, Ui.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), 0.05f, 0.61f, 0.95f, 0.65f);
            float y = 0.57f;
            foreach (var r in run.results.Take(10))
            {
                var L = Lib.Labels[r.lab];
                var row = Ui.At(Ui.Box(s, "R"), 0.08f, y - 0.035f, 0.92f, y);
                string cards = Hands.Card(r.spot.hand[0]) + " " + Hands.Card(r.spot.hand[1]);
                Ui.At(Ui.Num(row, $"{cards}  {r.spot.seats[r.spot.hero].pos,-4}", 28, Ui.Ink, TextAnchor.MiddleLeft), 0, 0, 0.4f, 1);
                Ui.At(Ui.Label(row, r.mine, 28, Ui.Dim, TextAnchor.MiddleLeft), 0.38f, 0, 0.7f, 1);
                Ui.At(Ui.Label(row, L.n, 28, Table.Hex(L.c), TextAnchor.MiddleRight, FontStyle.Bold), 0.6f, 0, 1, 1);
                y -= 0.04f;
            }
            Ui.At(Ui.Num(s, "Bankroll " + Ui.Money(S.br), 40, Ui.Gold), 0.05f, 0.12f, 0.95f, 0.16f);
            Ui.At(Ui.Button(s, "Continue", next, Ui.Gold, Ui.Night, 44), 0.2f, 0.04f, 0.8f, 0.1f);
        }

        // ------------------------------------------------------------ the end of the beta
        public void EndCard()
        {
            var s = Screen(Color.black);
            Ui.At(Ui.Label(s, "TEST BETA " + Version, 30, Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Bold), 0, 0.68f, 1, 0.71f);
            Ui.At(Ui.Title(s, "Chapter 4 · The Tour", 84, Ui.Ink), 0.05f, 0.58f, 0.95f, 0.67f);
            Ui.At(Ui.Label(s, "coming in the next build", 40, Ui.Dim), 0.05f, 0.535f, 0.95f, 0.575f);
            Ui.At(Ui.Num(s, $"{S.name}, {Rep}  ·  {Ui.Money(S.br)}  ·  {S.events} events  ·  {S.decisions} decisions", 28, Ui.Dim), 0.05f, 0.46f, 0.95f, 0.5f);
            Ui.At(Ui.Button(s, "Send feedback", () => Application.OpenURL(FeedbackUrl), Ui.Gold, Ui.Night, 44), 0.18f, 0.3f, 0.82f, 0.36f);
            Ui.At(Ui.Button(s, "Keep playing Chapter 3", () => { S.step = "ch3"; S.Write(); Hub(); }, Ui.Card, Ui.Ink, 34), 0.18f, 0.21f, 0.82f, 0.26f);
            Ui.At(Ui.Button(s, "Title", Title, new Color(1, 1, 1, 0.06f), Ui.Dim, 30), 0.3f, 0.12f, 0.7f, 0.17f);
            Ui.At(Ui.Label(s, "Music: “Retroracing Nightlife” by Bogart VGM (CC-BY 4.0), facebook.com/BogartVGM", 20, new Color(1, 1, 1, 0.4f)), 0.05f, 0.04f, 0.95f, 0.07f);
        }
    }
}
