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
            go.SetActive(true);
            return st;
        }

        // run a node with the game state it needs; then is called when the node (and anything it jumps to) is over
        public void Run(string node, Action then, string ev = "", bool won = false, bool bust = false, bool mistake = false)
        {
            var S = F.S;
            Load(S);
            vars.SetValue("$bankroll", (float)S.br); vars.SetValue("$peak", (float)S.peak); vars.SetValue("$room_title", S.roomTitle);
            vars.SetValue("$chapter", S.step == "prologue" || S.step == "choice" ? 0f : S.step == "done" ? 2f : 1f);
            vars.SetValue("$event", ev); vars.SetValue("$won", won); vars.SetValue("$bust", bust); vars.SetValue("$mistake", mistake);
            done = then;
            _ = runner.StartDialogue(node);
        }

        void Complete()
        {
            var S = F.S;
            Keep(S);
            var (floats, strings, _) = vars.GetAllVariables();
            if (strings.TryGetValue("$job", out var job) && !string.IsNullOrEmpty(job)) { S.job = job; if (S.step == "choice") S.step = "ch1"; }
            if (floats.TryGetValue("$chapter", out var ch) && ch >= 2) S.step = "done";
            S.Write();
            var d = done; done = null; d?.Invoke();
        }

        // the story variables live in the save (job, seen flags); game state comes from the game each run
        static readonly string[] Saved = { "$job", "$david_seen", "$lesson_seen", "$needle_seen", "$classic_seen", "$classic_lose_seen" };
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
            Films.Play(new[] { id }, () => over = true);
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

    // Yarn's lines and choices in the game's UI: #text lines build a phone conversation (tap to go on), #card and #hint
    // lines set up the next choice card, options become its buttons; any other line is a plain card on black.
    public class StoryPresenter : DialoguePresenterBase
    {
        readonly List<string> card = new List<string>(), hints = new List<string>(), thread = new List<string>();
        string threadFrom;

        public override YarnTask OnDialogueStartedAsync() { thread.Clear(); threadFrom = null; return YarnTask.CompletedTask; }
        public override YarnTask OnDialogueCompleteAsync() => YarnTask.CompletedTask;

        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            var text = line.TextWithoutCharacterName.Text; var tags = line.Metadata ?? Array.Empty<string>();
            if (tags.Contains("card")) { card.Add(text); return YarnTask.CompletedTask; }
            if (tags.Contains("hint")) { hints.Add(text); return YarnTask.CompletedTask; }
            bool go = false;
            if (tags.Contains("text"))
            {
                var from = line.CharacterName ?? "";
                if (from != threadFrom) { thread.Clear(); threadFrom = from; }
                thread.Add(text);
                Flow.I.Texts(from, thread, () => go = true);
            }
            else Flow.I.Line(text, () => go = true);
            return YarnTask.WaitUntil(() => go);
        }

        public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] options, CancellationToken token)
        {
            DialogueOption? picked = null;
            var shown = options.Where(o => o.IsAvailable).ToList();
            Flow.I.ChoiceCard(card.ToList(), shown.Select(o => o.Line.TextWithoutCharacterName.Text).ToList(), hints.ToList(), i => picked = shown[i]);
            card.Clear(); hints.Clear(); thread.Clear(); threadFrom = null;
            return WaitFor(() => picked);
        }

        static async YarnTask<DialogueOption?> WaitFor(Func<DialogueOption?> pick)
        {
            await YarnTask.WaitUntil(() => pick() != null);
            return pick();
        }
    }
}
