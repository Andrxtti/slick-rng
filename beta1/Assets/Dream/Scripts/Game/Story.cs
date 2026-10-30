// The story logic runs in Yarn Spinner (Resources/Story/*.yarn): decision points, story variables, conditions on
// later lines, phone texts, and which film plays. This is the bridge: one DialogueRunner, the game's commands
// (<<film>>, <<money>>, <<stake>>), the variables synced with the save, and a presenter that shows Yarn's lines and
// choices in the game's own UI.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Dream.UI;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

namespace Dream.Game
{
    public class Story : MonoBehaviour
    {
        DialogueRunner runner; InMemoryVariableStorage vars; Action done;
        Flow F => Flow.I;

        public static Story Make(Transform parent)
        {
            var go = new GameObject("Story"); go.SetActive(false); go.transform.SetParent(parent);
            var st = go.AddComponent<Story>();
            st.vars = go.AddComponent<InMemoryVariableStorage>();
            st.runner = go.AddComponent<DialogueRunner>();
            st.runner.VariableStorage = st.vars;
            st.runner.DialoguePresenters = new DialoguePresenterBase[] { go.AddComponent<StoryPresenter>() };
            st.runner.SetProject(Resources.Load<YarnProject>("Story/Dream"));
            st.runner.onDialogueComplete ??= new UnityEngine.Events.UnityEvent();
            st.runner.onDialogueComplete.AddListener(st.Complete);
            st.runner.AddCommandHandler<string>("film", st.Film);
            st.runner.AddCommandHandler<float, string>("money", st.Money);
            st.runner.AddCommandHandler<float>("stake", st.Stake);
            st.runner.AddCommandHandler<float>("away", n => { st.F.S.awayUntil = st.F.S.day + (int)n; st.F.S.Write(); });   // flown home for n days (Chapter 3)
            st.runner.AddCommandHandler<string>("clock", t => Phone.Clock = t);   // the time on the phone's status bar
            go.SetActive(true);
            return st;
        }

        // run a node with the game state it needs; then is called when the node (and anything it jumps to) is over
        // what just happened at the table, for the story to react to
        public class Outcome { public string ev = "", ftEnd = "", classicEnd = ""; public bool won, bust, cooler, qualified, online, ft, bubble; }

        public void Run(string node, Action then, Outcome o = null)
        {
            var S = F.S; o ??= new Outcome();
            Load(S);
            vars.SetValue("$name", S.name ?? ""); vars.SetValue("$classic_done", S.classicDone); vars.SetValue("$bankroll", (float)S.br); vars.SetValue("$peak", (float)S.peak); vars.SetValue("$room_title", S.roomTitle);
            vars.SetValue("$chapter", S.step == "prologue" || S.step == "choice" ? 0f : S.step == "ch2" || S.step == "done" ? 2f : S.step == "ch3" || S.step == "end" ? 3f : S.step == "end4" ? 4f : 1f);
            // Chapter 2: rings, the final table, the venue, where you are in a Circuit stop, and the options Steven wrote
            var ev = string.IsNullOrEmpty(o.ev) ? null : Lib.Event(o.ev);
            vars.SetValue("$bracelets", (float)S.bracelets); vars.SetValue("$bubble", o.bubble); vars.SetValue("$seriesday", (float)Flow.SeriesDay(S.day));
            vars.SetValue("$rings", (float)S.rings); vars.SetValue("$ft", o.ft); vars.SetValue("$ring", ev != null && ev.title == "ring");
            vars.SetValue("$venue", ev?.venue ?? ""); vars.SetValue("$stop", (float)Flow.StopIndex(S.day)); vars.SetValue("$stopday", (float)Flow.StopDay(S.day));
            foreach (var sc in new[] { "c2_smokes", "c2_cowboy", "c3_party" }) for (int n = 1; n <= 2; n++) vars.SetValue($"${sc}_{n}", Storyboard.OptionText(sc, n));
            vars.SetValue("$day", (float)S.day); vars.SetValue("$debt", (float)S.debt); vars.SetValue("$debt_s", Ui.Money(S.debt));
            vars.SetValue("$event", o.ev); vars.SetValue("$won", o.won); vars.SetValue("$bust", o.bust); vars.SetValue("$cooler", o.cooler);
            vars.SetValue("$qualified", o.qualified); vars.SetValue("$online", o.online); vars.SetValue("$ft_end", o.ftEnd); vars.SetValue("$classic_end", o.classicEnd);
            done = then;
            _ = runner.StartDialogue(node);
        }

        void Complete()
        {
            var S = F.S;
            Keep(S);
            var (floats, strings, _) = vars.GetAllVariables();
            if (strings.TryGetValue("$job", out var job) && !string.IsNullOrEmpty(job)) { S.job = job; if (S.step == "choice") S.step = "ch1"; }
            if (floats.TryGetValue("$chapter", out var ch))
            {
                if (ch >= 4) S.step = "end4";                             // Chapter 3's goal: Chapter 4's opening and the end card
                else if (ch >= 3 && S.step == "ch2") S.step = "ch3";      // Chapter 2's goal: the Summer (Flow.BeginChapter3 moves the calendar)
                else if (ch >= 2 && S.step == "ch1") S.step = "ch2";      // Chapter 1's end: the Circuit (Flow.BeginChapter2 moves the calendar)
            }
            S.Write();
            var d = done; done = null; d?.Invoke();
        }

        // the story variables live in the save (job, seen flags); game state comes from the game each run
        static readonly string[] Saved = { "$job", "$honest", "$david_seen", "$lesson_seen", "$badbeat_seen", "$firstwin_seen", "$stake_seen", "$wk2", "$wk3", "$wk3d", "$wk4a", "$wk4b",
            "$c2_fatts_seen", "$c2_mouth_seen", "$ring1_seen", "$lesson2_seen", "$smokes_seen", "$cowboy_seen", "$ring2_seen",
            "$c3_gg_seen", "$c3_steak_seen", "$c3_bubble_seen", "$c3_chance_seen", "$c3_party_seen", "$party_flown", "$c3_parm_seen", "$bracelet1_seen" };
        void Load(Save S)
        {
            foreach (var kv in S.story)
            {
                var p = kv.Split(new[] { '=' }, 2); if (p.Length < 2) continue;
                if (p[1] == "true" || p[1] == "false") vars.SetValue(p[0], p[1] == "true"); else vars.SetValue(p[0], p[1]);
            }
        }
        void Keep(Save S)
        {
            S.story.Clear();
            var (_, strings, bools) = vars.GetAllVariables();   // by type: TryGetValue<bool> throws on a string variable
            foreach (var name in Saved)
            {
                if (bools.TryGetValue(name, out var b)) S.story.Add($"{name}={(b ? "true" : "false")}");
                else if (strings.TryGetValue(name, out var t)) S.story.Add($"{name}={t}");
            }
        }

        // ------------------------------------------------------------ commands
        YarnTask Film(string id)
        {
            bool over = false;
            if (!Moments.FilmOk(id, Flow.LastEvent)) return YarnTask.CompletedTask;   // never a live film after an online event
            StoryPresenter.AfterPhone(() => Films.Play(new[] { id }, () => over = true));   // finish reading the texts first
            return YarnTask.WaitUntil(() => over);
        }

        void Money(float amount, string why)
        {
            var S = F.S; S.br += amount; S.peak = Math.Max(S.peak, S.br); S.log.Insert(0, $"{why}: {Ui.Money(amount)}");
            vars.SetValue("$bankroll", (float)S.br); vars.SetValue("$peak", (float)S.peak); S.Write();
        }

        void Stake(float amount)
        {
            var S = F.S; S.br += amount; S.debt += amount; S.log.Insert(0, $"Slick's stake: {Ui.Money(amount)}");
            vars.SetValue("$bankroll", (float)S.br); S.Write();
        }
    }

    // Yarn's lines and choices in the game's UI. #text lines go to the phone (Phone.cs): the speaker is the sender, "You"
    // sends to the contact in #to:<name>. #card and #hint lines set up the next choice: on the phone if it's up (reply
    // chips under the heading), otherwise a full-screen choice card. Any other line is a narrator card on black. When
    // something that isn't a text comes next (a film, a card, the end), the phone waits for a tap and puts itself away.
    public class StoryPresenter : DialoguePresenterBase
    {
        readonly List<string> card = new List<string>(), hints = new List<string>();

        public static void AfterPhone(Action next) { if (Phone.I != null) Phone.I.Done(next); else next(); }

        public override YarnTask OnDialogueStartedAsync() => YarnTask.CompletedTask;
        public override YarnTask OnDialogueCompleteAsync()
        {
            bool go = false; AfterPhone(() => go = true);
            return YarnTask.WaitUntil(() => go);
        }

        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            var text = line.TextWithoutCharacterName.Text; var tags = line.Metadata ?? Array.Empty<string>();
            if (tags.Contains("card")) { card.Add(text); return YarnTask.CompletedTask; }
            if (tags.Contains("hint")) { hints.Add(text); return YarnTask.CompletedTask; }
            bool go = false;
            if (tags.Contains("text"))
            {
                var to = tags.FirstOrDefault(t => t.StartsWith("to:"))?.Substring(3);
                Phone.Open().Text(line.CharacterName ?? "", to, text, () => go = true);
            }
            else AfterPhone(() => { Voice.Say("narrator", text); Flow.I.Line(text, () => { Voice.Hush(); go = true; }); });
            return YarnTask.WaitUntil(() => go);
        }

        public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] options, CancellationToken token)
        {
            DialogueOption? picked = null;
            var shown = options.Where(o => o.IsAvailable).ToList();
            var labels = shown.Select(o => o.Line.TextWithoutCharacterName.Text).ToList();
            if (Phone.I != null) Phone.I.Choose(card.Count > 0 ? string.Join(" ", card) : null, labels, hints.ToList(), i => picked = shown[i]);
            else Flow.I.ChoiceCard(card.ToList(), labels, hints.ToList(), i => picked = shown[i]);
            card.Clear(); hints.Clear();
            return WaitFor(() => picked);
        }

        static async YarnTask<DialogueOption?> WaitFor(Func<DialogueOption?> pick)
        {
            await YarnTask.WaitUntil(() => pick() != null);
            return pick();
        }
    }
}
