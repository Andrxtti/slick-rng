using System.Linq;
using Dream.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Dream.Game
{
    // The hidden test menu (Milestone A3): 5 taps on the TEST BETA label on the title. Set the bankroll, the date and the
    // job, go home or to the Classic, and play any scene of the script (its film, or its storyboard).
    public partial class Flow
    {
        int versionTaps; float versionTapAt;
        void VersionTap()
        {
            versionTaps = Time.realtimeSinceStartup - versionTapAt < 2.5f ? versionTaps + 1 : 1; versionTapAt = Time.realtimeSinceStartup;
            if (versionTaps >= 5) { versionTaps = 0; DebugMenu(); }
        }

        int debugPage;
        void DebugMenu()
        {
            if (S == null || S.step == "new" || S.step == "prologue" || S.step == "choice")
            { S = new Save { step = "ch1", job = "nights", day = 7, br = 2000, peak = 2000, name = "Tester" }; S.story.Add("$job=nights"); S.Write(); }
            var s = Screen(Ui.Night); Canvas.ForceUpdateCanvases();
            Ui.At(Ui.Label(s, "TEST MENU", 26, Ui.Neon, TextAnchor.MiddleLeft, FontStyle.Bold), 0.05f, 0.95f, 0.5f, 0.98f);
            Ui.At(Ui.Label(s, $"{DateStr(S.day)} (day {S.day})  ·  {Ui.Money(S.br)}  ·  {(S.job == "nights" ? "day job" : "full-time")}", 24, Ui.Ink, TextAnchor.MiddleLeft), 0.05f, 0.915f, 0.95f, 0.95f);
            void Row(float y, params (string label, System.Action act)[] bs)
            {
                float w = 0.9f / bs.Length;
                for (int i = 0; i < bs.Length; i++) { var a = bs[i].act; Ui.At(Ui.Button(s, bs[i].label, () => { a(); S.peak = System.Math.Max(S.peak, S.br); S.Write(); DebugMenu(); }, Ui.Card, Ui.Ink, 24), 0.05f + i * w + 0.005f, y, 0.05f + (i + 1) * w - 0.005f, y + 0.045f); }
            }
            Row(0.86f, ("−$1,000", () => S.br = System.Math.Max(0, S.br - 1000)), ("+$1,000", () => S.br += 1000), ("$50", () => S.br = 50), ("$5,000", () => S.br = 5000));
            Row(0.81f, ("−1 day", () => S.day = System.Math.Max(7, S.day - 1)), ("+1 day", () => S.day++), ("+7 days", () => S.day += 7), ("Fri 13 Feb", () => S.day = ClassicDay - 1));
            Row(0.76f, ("Classic day", () => { S.day = ClassicDay; S.day2 = false; S.classicDone = false; }), ("Day 2", () => { S.day = ClassicDay + 1; S.day2 = true; }),
                ("Chapter 2", () => { S.step = "ch2"; S.day = Ch2Start; S.classicDone = true; }),
                ("Chapter 3", () => { S.step = "ch3"; S.day = Ch3Start; S.classicDone = true; S.bracelets = 0; S.awayUntil = -1;   // the chapter fresh: no bracelet, its scenes unseen
                    S.story.RemoveAll(v => v.StartsWith("$c3_") || v.StartsWith("$bracelet1_seen") || v.StartsWith("$party_flown")); }),
                ("Job: " + (S.job == "nights" ? "keep" : "quit"), () => { S.job = S.job == "nights" ? "quit" : "nights"; S.story.RemoveAll(v => v.StartsWith("$job=")); S.story.Add("$job=" + S.job); }));
            Ui.At(Ui.Button(s, "Go home", () => Hub(), Ui.Gold, Ui.Night, 28), 0.05f, 0.7f, 0.48f, 0.75f);
            Ui.At(Ui.Button(s, "Title", Title, Ui.Card, Ui.Ink, 28), 0.52f, 0.7f, 0.95f, 0.75f);
            // every scene of the script, a page at a time
            var all = new[] { "reel" }.Concat(Storyboard.All().Select(x => x.id)).Concat(ChapterOneFilms).Distinct().ToList();
            int per = 12, pages = (all.Count + per - 1) / per; debugPage = Mathf.Clamp(debugPage, 0, pages - 1);
            Ui.At(Ui.Label(s, $"SCENES  ·  page {debugPage + 1} of {pages}  ·  film or storyboard", 22, Ui.Dim, TextAnchor.MiddleLeft, FontStyle.Bold), 0.05f, 0.655f, 0.95f, 0.69f);
            var page = all.Skip(debugPage * per).Take(per).ToList();
            for (int i = 0; i < page.Count; i++)
            {
                var id = page[i]; int c = i % 2, r = i / 2; var sc = Storyboard.Get(id);
                string label = id + (sc != null && sc.storyboard ? "  (storyboard)" : "");
                Ui.At(Ui.Button(s, label, () => Films.Play(new[] { id }, DebugMenu), new Color(1, 1, 1, 0.07f), Ui.Ink, 22), 0.05f + c * 0.455f, 0.6f - r * 0.07f, 0.495f + c * 0.455f, 0.65f - r * 0.07f);
            }
            Ui.At(Ui.Button(s, "← page", () => { debugPage = (debugPage + pages - 1) % pages; DebugMenu(); }, Ui.Card, Ui.Ink, 24), 0.05f, 0.12f, 0.48f, 0.17f);
            Ui.At(Ui.Button(s, "page →", () => { debugPage = (debugPage + 1) % pages; DebugMenu(); }, Ui.Card, Ui.Ink, 24), 0.52f, 0.12f, 0.95f, 0.17f);
        }
        // the Chapter 1 films that aren't scenes of SCRIPT_ALL (split or added in this build)
        static readonly string[] ChapterOneFilms = { "p_diner_yes", "p_diner_no", "p_diner2", "badbeat2", "broke_free", "broke_q", "broke_stake", "ft_intro_tommy", "ft_intro_mustich", "ft_won_tommy", "ft_lost_tommy",
            "c1_w2", "c1_w2_a", "c1_w2_b", "c1_w2b", "c1_w3_diner_keep", "c1_w3_diner_quit", "c1_w4_diner", "c1_w4_eve", "c1_w4_eve2" };
    }
}
