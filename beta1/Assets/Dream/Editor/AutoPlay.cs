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
        static string branch, only; static bool lose, broke, cut; static double next; static int steps; static StreamWriter log; static readonly HashSet<string> shot = new HashSet<string>();
        static string lastHub, lastText;

        static AutoPlay()
        {
            Remote.Extra["spot"] = a => { var p = a.Split(' '); var root = (RectTransform)Object.FindAnyObjectByType<Canvas>().transform; Ui.Clear(root); Remote.Log("spot: " + Table.ShowSeat(root, p[0], p.Length > 1 ? p[1] : "shove")); };
            Remote.Extra["story"] = a => { var p = a.Split(' '); Flow.I?.TestStory(p[0], p[1], double.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture)); };
            Remote.Extra["auto"] = b =>
            {
                lose = b.EndsWith(" lose"); broke = b.EndsWith(" broke"); cut = false; only = b.EndsWith(" online") ? "online" : b.EndsWith(" live") ? "live" : null; Moments.Log.Clear(); branch = b.StartsWith("keep") ? "keep" : "quit"; steps = 0; shot.Clear(); lastHub = null; Films.Played.Clear();
                log = new StreamWriter(Path.Combine(Application.dataPath, "..", "Logs", $"auto_{branch}{(lose ? "_lose" : broke ? "_broke" : only != null ? "_" + only : "")}.txt"));
                PlayerPrefs.DeleteAll();
                if (!EditorApplication.isPlaying) { UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Dream/Scenes/Main.unity"); EditorApplication.isPlaying = true; }
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
            next = EditorApplication.timeSinceStartup + 0.35;
            if (++steps > 4000) { Stop("gave up after 4000 steps"); return; }
            var films = Object.FindAnyObjectByType<Films>();
            if (films != null) { films.Skip(); return; }
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
                bool fresh = (Has("THE BUBBLE") && Shot("bubble")) | (Has("HEADS-UP") && Shot("headsup")) | (Has("IN THE MONEY") && Shot("itm")) | (name == "WalkAway" && Shot("walkaway"));
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
            if (Table.Waiting || Has("Coaching by Confirmed Fire")) { Table.AutoStep((lose && f.S.events < 7) || (broke && f.S.debt <= 0 && f.S.events < 3)); return; }   // a spot, or its grade panel (lose: the worst answers for the first games)
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
            var choice = Find("Play poker. For real.") ?? Find("Take Slick's stake");   // the diner question; rock bottom
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
                if (res != null) W($"  result: {res}");
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
