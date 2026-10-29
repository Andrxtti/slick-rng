// The test driver: plays the game start to finish in Play mode with no hands on the mouse, taking one branch of the
// job choice (on the phone). Hands get the solver's best answer; films are skipped once they start; texts are tapped
// through; at the hub it enters the Classic when it can, then the nightly, the Global Rush and the freeroll, and sleeps
// when nothing is playable today.
//   remote: auto quit | auto keep   -> Logs/auto_<branch>.txt and screenshots Logs/<branch>_choice.png, _hub.png, _end.png
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dream.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Dream.UI;

namespace Dream.EditorTools
{
    [InitializeOnLoad]
    public static class AutoPlay
    {
        static string branch, only; static bool lose, broke, cut; static double acc = 1; static int stakeDay = -1, accSum, accN; static double next; static int steps; static StreamWriter log; static readonly HashSet<string> shot = new HashSet<string>();
        static string lastHub, lastText;

        static AutoPlay()
        {
            Remote.Extra["spot"] = a => { var p = a.Split(' '); var root = (RectTransform)Object.FindAnyObjectByType<Canvas>().transform; Ui.Clear(root); Remote.Log("spot: " + Table.ShowSeat(root, p[0], p.Length > 1 ? p[1] : "shove")); };
            Remote.Extra["films"] = ids => Films.Play(ids.Split(' '), () => Remote.Log("films: queue done"));
            Remote.Extra["films-state"] = _ =>
            {
                var vp = Films.I != null ? Films.I.Player : null;
                Remote.Log($"films-state: paused {EditorApplication.isPaused} frame {Time.frameCount} t {Time.unscaledTime:0.0} focus {Application.isFocused} runInBackground {Application.runInBackground} | {Object.FindObjectsByType<UnityEngine.Video.VideoPlayer>(FindObjectsSortMode.None).Length} VideoPlayer(s), playing {Films.Playing}"
                    + (vp == null ? "" : $", vp.isPlaying {vp.isPlaying}, time {vp.time:0.0}, muted {vp.GetDirectAudioMute(0)}, volume {vp.GetDirectAudioVolume(0):0.00}, {System.IO.Path.GetFileName(vp.url)}"));
            };
            Remote.Extra["voice-key"] = a => { var i = a.IndexOf(' '); Remote.Log($"voice-key: {Dream.Game.Voice.Key(a.Substring(0, i), a.Substring(i + 1))}"); };
            Remote.Extra["voice-last"] = _ => Remote.Log("voice-last: " + Dream.Game.Voice.Last);
            Remote.Extra["story"] = a => { var p = a.Split(' '); Flow.I?.TestStory(p[0], p[1], double.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture)); };
            Remote.Extra["auto"] = b =>
            {
                var am = System.Text.RegularExpressions.Regex.Match(b, @" acc ([\d.]+)"); acc = am.Success ? double.Parse(am.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 1; stakeDay = -1; accSum = accN = 0;
                lose = b.EndsWith(" lose"); broke = b.EndsWith(" broke"); cut = false; only = b.EndsWith(" online") ? "online" : b.EndsWith(" live") ? "live" : null; Moments.Log.Clear(); branch = b.StartsWith("keep") ? "keep" : "quit"; steps = 0; shot.Clear(); lastHub = null; Films.Played.Clear();
                log = new StreamWriter(Path.Combine(Application.dataPath, "..", "Logs", $"auto_{branch}{(acc < 1 ? $"_acc{(int)(acc * 100)}" : lose ? "_lose" : broke ? "_broke" : only != null ? "_" + only : "")}.txt"));
                PlayerPrefs.DeleteAll();
                if (!EditorApplication.isPlaying) { UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Dream/Scenes/Main.unity"); EditorApplication.isPlaying = true; }
                EditorApplication.delayCall += () => Application.runInBackground = true;   // driven unfocused: keep the game loop ticking
                EditorApplication.update -= Tick; EditorApplication.update += Tick;
            };
        }

        static void W(string m) { log?.WriteLine(m); log?.Flush(); }

        static Button Find(string label)
        {
            foreach (var b in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (!b.interactable || !b.isActiveAndEnabled) continue;
                var t = b.transform.childCount > 0 ? b.transform.GetChild(0).GetComponent<TMPro.TMP_Text>() : null;
                if (b.name == label || (t != null && t.text.StartsWith(label))) return b;
            }
            return null;
        }
        static bool Has(string text) => Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Any(t => t.text.StartsWith(text));
        static bool Shot(string name) { if (!shot.Add(name)) return false; ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath, "..", "Logs", $"{branch}_{name}.png")); return true; }

        static void Tick()
        {
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next) return;
            if (!Application.runInBackground) Application.runInBackground = true;   // driven unfocused: keep the game loop ticking
            next = EditorApplication.timeSinceStartup + 0.2;
            if (++steps > 30000) { Stop("gave up after 30000 steps"); return; }   // a month of Chapter 1 is a lot of hands
            if (Films.Playing) { Films.I.Skip(); return; }
            // the table's moments: the all-in, the cooler, the level card, the bubble, heads-up, the walk away
            var verdict = Find("See the verdict") ?? Find("See where you finished");
            if (verdict != null)
            {
                bool fresh = Has("ALL IN") && Shot(Has("You hold!") ? "allin_hold" : Has("holds.") ? "allin_lost" : "allin_fold");
                fresh |= Has("Nothing you could do") && Shot("cooler");
                if (!fresh) verdict.onClick.Invoke();   // a new screenshot waits a tick (it's taken at the end of the frame)
                return;
            }
            foreach (var name in new[] { "Moment", "WalkAway" })
            {
                var m = GameObject.Find(name);
                if (m == null || m.GetComponent<Button>() == null) continue;
                string at = Flow.LastEvent == null ? "" : "_" + (Has("CF TILT") || Has("HAND-FOR-HAND") || Has("CHAT") || (Flow.I != null && Table.Online) ? "online" : "live");   // stills say where they were taken
                bool fresh = (Has("THE BUBBLE") && Shot("bubble" + at)) | (Has("HEADS-UP") && Shot("headsup" + at)) | (Has("IN THE MONEY") && Shot("itm" + at)) | (name == "WalkAway" && Shot("walkaway" + at));
                if (fresh || Has("ALL IN")) return;   // a screenshot waits a tick; an all-in is still running
                m.GetComponent<Button>().onClick.Invoke(); return;
            }
            if (GameObject.Find("Talk") != null && Shot("talk")) return;
            var start = Find("Start the story");   // the creator: roll the dice once, then go
            if (start != null)
            {
                if (!shot.Contains("creator")) { Find("⚄")?.onClick.Invoke(); Shot("creator"); return; }
                W("creator: " + Object.FindAnyObjectByType<TMPro.TMP_InputField>()?.text); start.onClick.Invoke(); return;
            }
            var f = Flow.I; if (f == null || f.S == null) { Find("Begin")?.onClick.Invoke(); return; }
            if (Table.Waiting || Has("Coaching by Confirmed Fire")) { Table.AutoStep((lose && f.S.events < 7) || (broke && f.S.debt <= 0 && f.S.events < 3), acc); return; }   // a spot, or its grade panel (lose: the worst answers for the first games)
            if (Phone.I != null)   // the phone: take the branch when the decision is up, otherwise tap through the texts
            {
                var pick = Find(branch == "keep" ? "Keep the job" : "Quit. Text Lou");
                if (pick != null)
                {
                    Shot("phone_choice");
                    if (shot.Contains("choice_wait")) { W($"choice on the phone: {branch}"); pick.onClick.Invoke(); }
                    else shot.Add("choice_wait");   // one tick for the screenshot
                    return;
                }
                var texts = Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Where(t => t.transform.parent != null && t.transform.parent.name == "Bubble").Select(t => t.text).ToList();
                var said = string.Join(" | ", texts); if (said != lastText) { W("  phone: " + said); lastText = said; }
                if (Has("BRO. YOU QUIT") || Has("Smart. A paycheck")) Shot("phone_" + branch);
                if (Has("TEN THOUSAND")) Shot("david");
                Find("Dim")?.onClick.Invoke(); return;
            }
            var choice = Find("Play poker. For real.") ?? Find("Take Slick's stake") ?? Find("\"Does it fold");   // the diner question; rock bottom; week two (Mustich)
            if (choice != null) { Shot(choice.name == "Button" && Has("Rock bottom") ? "broke" : "diner_choice"); W("  choose: " + choice.GetComponentInChildren<TMPro.TMP_Text>().text); choice.onClick.Invoke(); return; }
            if (Find("Send feedback") != null)
            {
                Shot("end");
                if (shot.Contains("end_wait")) Stop($"end card: {f.Rep}, bankroll {f.S.br:0}, day {f.S.day}, events {f.S.events}, debt {f.S.debt:0}");
                else shot.Add("end_wait");
                return;
            }
            var cont = Find("Continue");
            if (cont != null)
            {
                if (Has("Busted?")) { W($"  texts: Slick, broke ({f.S.job}): {Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Select(t => t.text).FirstOrDefault(t => t.Contains("paycheck") || t.Contains("No paycheck"))}"); Shot("broke"); }
                if (Has("TEN THOUSAND")) { W("  texts: David, ten thousand"); Shot("david"); }
                var res = Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Select(t => t.text).FirstOrDefault(t => t.StartsWith("You won") || t.StartsWith("Out in") || t.Contains(" place") || t.StartsWith("Up ") || t.StartsWith("Down ") || t.StartsWith("Coolered"));
                var accLine = Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Select(t => t.text).FirstOrDefault(t => t.StartsWith("ACCURACY"));
                if (accLine != null && res != null)   // running accuracy over the career, weighted by decisions
                {
                    var m = System.Text.RegularExpressions.Regex.Match(accLine, @"ACCURACY (\d+)%\s*·\s*(\d+)");
                    if (m.Success) { accSum += int.Parse(m.Groups[1].Value) * int.Parse(m.Groups[2].Value); accN += int.Parse(m.Groups[2].Value); }
                }
                if (res != null) W($"  result: {res}{(accLine != null ? "  (" + accLine.Split('·')[0].Trim() + (accN > 0 ? $", career {accSum / accN}%" : "") + ")" : "")}");
                cont.onClick.Invoke(); return;
            }
            if (Has("tap to continue") && Find("Screen") != null) { Find("Screen").onClick.Invoke(); return; }   // a narrator card
            if (Has("BISCAYNE CARD ROOM"))
            {
                if (broke && !cut && Find("nightly") != null) { cut = true; f.S.br = 70; f.S.Write(); W("  (test: bankroll cut to $70)"); f.Hub(); return; }   // on a day you can play
                Shot("hub");
                var texts = Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Select(t => t.text).ToList();
                string date = texts.FirstOrDefault(t => t.Contains(", ") && (t.StartsWith("Mon") || t.StartsWith("Tue") || t.StartsWith("Wed") || t.StartsWith("Thu") || t.StartsWith("Fri") || t.StartsWith("Sat") || t.StartsWith("Sun")));
                string why = string.Join("; ", texts.Where(t => t.StartsWith("Live games") || t.StartsWith("Your live game") || t.StartsWith("One live game") || t.StartsWith("No online") || t.StartsWith("A live day") || t.StartsWith("Two online")).Distinct());
                string note = texts.FirstOrDefault(t => t.StartsWith("Payday"));
                if (stakeDay < 0 && f.S.br >= 5000) { stakeDay = f.S.day; W($"  STAKE: $5,000 reached on day {f.S.day} ({date})"); }
                string line = $"hub {date} | ${f.S.br:0} | {f.Rep} | {(why == "" ? "can play" : why)}{(note != null ? " | " + note : "")}";
                if (line != lastHub) { W(line); lastHub = line; }
                // three smaller games first, so the calendar rules (one live game a day, weekends with the job) get played
                var order = only == "online" ? new[] { "glob55", "freeroll" } : only == "live" ? new[] { "classic2", "classic", "nightly" } : new[] { "classic2", "classic", "nightly", "glob55", "freeroll" };
                foreach (var id in order) { var b = Find(id); if (b != null) { W($"  play {id} (${f.S.br:0} before buy-in)"); b.onClick.Invoke(); return; } }
                var sleep = Find("Sleep") ?? Find("Call it a night"); if (sleep != null) { sleep.onClick.Invoke(); return; }
                Stop("hub with nothing to do");
                return;
            }
            Find("Begin")?.onClick.Invoke();
        }

        static void Stop(string why)
        {
            W(why); W("films: " + string.Join(", ", Films.Played));
            W("where (TABLE.md 11):"); foreach (var l in Moments.Log) W("  " + l);
            int bad = Moments.Log.Count(l => l.StartsWith("MISMATCH")); W(bad == 0 ? "PASS: no live/online mismatch" : $"FAIL: {bad} mismatches");
            log?.Close(); log = null; EditorApplication.update -= Tick;
            Remote.Log($"auto {branch}: {why}");
        }
    }
}
