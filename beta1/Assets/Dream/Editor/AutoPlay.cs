// The test driver: plays the game start to finish in Play mode with no hands on the mouse, taking one branch of the
// job choice. Hands get the solver's best answer; films are skipped once they start; at the hub it enters the Classic
// when it can, otherwise the biggest event it can afford, and sleeps when nothing is playable today.
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
        static string branch; static bool lose, broke, cut; static double next; static int steps; static StreamWriter log; static readonly HashSet<string> shot = new HashSet<string>();
        static string lastHub;

        static AutoPlay()
        {
            Remote.Extra["spot"] = a => { var p = a.Split(' '); var root = (RectTransform)Object.FindAnyObjectByType<Canvas>().transform; Ui.Clear(root); Remote.Log("spot: " + Table.ShowSeat(root, p[0], p.Length > 1 ? p[1] : "shove")); };
            Remote.Extra["story"] = a => { var p = a.Split(' '); Flow.I?.TestStory(p[0], p[1], double.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture)); };
            Remote.Extra["auto"] = b =>
            {
                lose = b.EndsWith(" lose"); broke = b.EndsWith(" broke"); cut = false; branch = b.StartsWith("keep") ? "keep" : "quit"; steps = 0; shot.Clear(); lastHub = null; Films.Played.Clear();
                log = new StreamWriter(Path.Combine(Application.dataPath, "..", "Logs", $"auto_{branch}{(lose ? "_lose" : broke ? "_broke" : "")}.txt"));
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
                var t = b.transform.childCount > 0 ? b.transform.GetChild(0).GetComponent<Text>() : null;
                if (b.name == label || (t != null && t.text.StartsWith(label))) return b;
            }
            return null;
        }
        static bool Has(string text) => Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.text.StartsWith(text));
        static void Shot(string name) { if (shot.Add(name)) ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath, "..", "Logs", $"{branch}_{name}.png")); }

        static void Tick()
        {
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next) return;
            next = EditorApplication.timeSinceStartup + 0.35;
            if (++steps > 4000) { Stop("gave up after 4000 steps"); return; }
            var films = Object.FindAnyObjectByType<Films>();
            if (films != null) { films.Skip(); return; }
            var f = Flow.I; if (f == null || f.S == null) { Find("Begin")?.onClick.Invoke(); return; }
            if (Table.Waiting || Has("Coaching by Confirmed Fire")) { Table.AutoStep((lose && f.S.events < 7) || (broke && f.S.debt <= 0 && f.S.events < 3)); return; }   // a spot, or its grade panel (lose: the worst answers for the first games)
            if (Has("Decision time."))
            {
                Shot("choice");
                if (shot.Contains("choice_wait")) { W($"choice: {branch}"); Find(branch == "keep" ? "Keep the job" : "Quit. Today.")?.onClick.Invoke(); }
                else shot.Add("choice_wait");   // one tick for the screenshot
                return;
            }
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
                if (Has("Busted?")) { W($"  texts: Slick, broke ({f.S.job}): {Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Select(t => t.text).FirstOrDefault(t => t.Contains("paycheck") || t.Contains("No paycheck"))}"); Shot("broke"); }
                if (Has("TEN THOUSAND")) { W("  texts: David, ten thousand"); Shot("david"); }
                var res = Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Select(t => t.text).FirstOrDefault(t => t.StartsWith("You won") || t.StartsWith("Out in") || t.Contains(" place") || t.StartsWith("Up ") || t.StartsWith("Down ") || t.StartsWith("Coolered"));
                if (res != null) W($"  result: {res}");
                cont.onClick.Invoke(); return;
            }
            if (Has("BISCAYNE CARD ROOM"))
            {
                if (broke && !cut && Find("nightly") != null) { cut = true; f.S.br = 70; f.S.Write(); W("  (test: bankroll cut to $70)"); f.Hub(); return; }   // on a day you can play
                Shot("hub");
                var texts = Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Select(t => t.text).ToList();
                string date = texts.FirstOrDefault(t => t.Contains(", ") && (t.StartsWith("Mon") || t.StartsWith("Tue") || t.StartsWith("Wed") || t.StartsWith("Thu") || t.StartsWith("Fri") || t.StartsWith("Sat") || t.StartsWith("Sun")));
                string why = texts.FirstOrDefault(t => t.StartsWith("Live games") || t.StartsWith("Your live game") || t.StartsWith("One live game"));
                string note = texts.FirstOrDefault(t => t.StartsWith("Payday"));
                string line = $"hub {date} | ${f.S.br:0} | {f.Rep} | {why ?? "can play"}{(note != null ? " | " + note : "")}";
                if (line != lastHub) { W(line); lastHub = line; }
                // three smaller games first, so the calendar rules (one live game a day, weekends with the job) get played
                var order = f.S.events < 4 ? new[] { "nightly", "c12", "deep", "classic" } : new[] { "classic", "deep", "nightly", "c12" };
                foreach (var id in order) { var b = Find(id); if (b != null) { W($"  play {id} (${f.S.br:0} before buy-in)"); b.onClick.Invoke(); return; } }
                var sleep = Find("Sleep"); if (sleep != null) { sleep.onClick.Invoke(); return; }
                Stop("hub with nothing to do");
                return;
            }
            Find("Begin")?.onClick.Invoke();
        }

        static void Stop(string why)
        {
            W(why); W("films: " + string.Join(", ", Films.Played));
            log?.Close(); log = null; EditorApplication.update -= Tick;
            Remote.Log($"auto {branch}: {why}");
        }
    }
}
