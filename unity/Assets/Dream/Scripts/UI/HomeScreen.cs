// Home: your place in 3D with you in it. Tap the floor to walk, tap a label (or the thing) to use it. The laptop
// is the hub (tonight's games, the calendar, money); the closet is your wardrobe; the bed ends the day; the TV,
// beer, bong and fish are just for fun. Upgrading your home (in the closet) changes the room.
using System;
using System.Collections.Generic;
using CFDream.Character;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFDream.UI
{
    public static class HomeScreen
    {
        static readonly Dictionary<string, string> Emoji = new Dictionary<string, string>
        { { "laptop", "💻" }, { "bed", "🛏" }, { "closet", "👕" }, { "tv", "📺" }, { "beer", "🍺" }, { "bong", "💨" }, { "fish", "🐠" } };

        public static VisualElement Build(State s)
        {
            var page = U.Div("home-room");
            if (!Bodies.Available) { page.Add(HubView.Laptop(s, false)); return page; }   // no 3D body: the hub as before

            var view = new CharacterView(CharacterSpec.From(s), null, 0, 0, "room") { Tier = s.Eq("home") };
            view.AddToClassList("room-view");
            page.Add(view);

            var labels = U.Div("room-labels").NoPick(); page.Add(labels);
            var fx = U.Div("room-fx").NoPick(); page.Add(fx);
            var haze = U.Div("room-haze").NoPick(); page.Add(haze);
            var hud = U.Col().Cls("room-hud"); hud.Add(HubView.Header(s)); page.Add(hud);
            page.Add(Tonight(s));
            var bubble = U.Div("room-bubble").NoPick(); var bt = U.T("", "room-bubble-t", Fonts.Semi).NoPick(); bubble.Add(bt); bubble.style.display = DisplayStyle.None; page.Add(bubble);
            if (Game.Morning != null && Game.Morning.Count > 0) page.Add(Morning(s));

            // labels over the things you can use
            var chips = new Dictionary<string, VisualElement>();
            string sayWho = null; float sayUntil = 0;
            var puffs = new List<(VisualElement e, float t0, Vector2 v)>(); float hazeUntil = 0;

            void Said(string who, string line) { sayWho = who; sayUntil = Time.realtimeSinceStartup + 3.2f; bt.text = line; bubble.style.display = DisplayStyle.Flex; }
            void Done(string what)
            {
                switch (what)
                {
                    case "laptop": Game.SetTab("calendar"); break;
                    case "closet": Game.SetTab("closet"); break;
                    case "bed":
                        var sl = Calendar.SlotsLeft(s);
                        DreamApp.Confirm("Call it a night?", sl.left > 0 ? $"You've still got {sl.left} game{(sl.left == 1 ? "" : "s")} you could play tonight." : "Big day tomorrow.",
                            "Sleep", () => Game.Sleep(s.job == "nights" && sl.left == 0 ? 0 : 1), "Not yet");
                        break;
                }
            }
            void Puff() { hazeUntil = Time.realtimeSinceStartup + 7f; for (int i = 0; i < 9; i++) { var e = U.Div("puff").NoPick(); fx.Add(e); puffs.Add((e, Time.realtimeSinceStartup + i * 0.12f, new Vector2(UnityEngine.Random.Range(-28f, 28f), UnityEngine.Random.Range(-70f, -40f)))); } }
            Action<string, string> onSaid = Said; Action<string> onDone = Done; Action onPuff = Puff;
            page.RegisterCallback<AttachToPanelEvent>(e => { HomeLife.Said += onSaid; HomeLife.Done += onDone; HomeLife.Puff += onPuff; });
            page.RegisterCallback<DetachFromPanelEvent>(e => { HomeLife.Said -= onSaid; HomeLife.Done -= onDone; HomeLife.Puff -= onPuff; });

            page.schedule.Execute(() =>
            {
                var st = CharacterStage.Instance; var room = st?.Room; if (st == null || room == null || !st.InRoom) return;
                float now = Time.realtimeSinceStartup;
                foreach (var u in room.uses)
                {
                    if (!chips.TryGetValue(u.id, out var chip))
                    {
                        chip = U.Row().Cls("room-chip");
                        chip.Add(U.T(Emoji.TryGetValue(u.id, out var em) ? em : "•", "room-chip-i", Fonts.Emoji).NoPick());
                        chip.Add(U.T(u.label, "room-chip-t", Fonts.Semi).NoPick());
                        string id = u.id; chip.Tap(() => CharacterStage.Instance?.Life?.Use(id));
                        labels.Add(chip); chips[u.id] = chip; labels.pickingMode = PickingMode.Ignore;
                    }
                    var vp = st.Viewport(u.Anchor);
                    if (vp.z <= 0) { chip.style.display = DisplayStyle.None; continue; }
                    chip.style.display = DisplayStyle.Flex;
                    var p = view.FromViewport(vp); chip.style.left = p.x; chip.style.top = p.y;
                }
                // the speech bubble follows whoever's talking
                if (sayWho != null)
                {
                    if (now > sayUntil) { sayWho = null; bubble.style.display = DisplayStyle.None; }
                    else
                    {
                        Vector3 at;
                        if (sayWho == "me") { var head = st.Bone(HumanBodyBones.Head); at = head != null ? head.position + Vector3.up * 0.42f : st.Pivot.position + Vector3.up * 2.2f; }
                        else { var u = room.Use(sayWho); at = u != null ? u.Anchor + Vector3.up * 0.25f : st.Pivot.position + Vector3.up * 2.2f; }
                        var p = view.FromViewport(st.Viewport(at)); bubble.style.left = p.x; bubble.style.top = p.y;
                    }
                }
                // smoke drifting up from your head, and the room going soft for a bit
                if (puffs.Count > 0)
                {
                    var head = st.Bone(HumanBodyBones.Head); var hp = view.FromViewport(st.Viewport(head != null ? head.position + st.Pivot.forward * 0.15f : st.Pivot.position + Vector3.up * 1.8f));
                    for (int i = puffs.Count - 1; i >= 0; i--)
                    {
                        var (e, t0, v) = puffs[i]; float t = now - t0;
                        if (t < 0) { e.style.opacity = 0; continue; }
                        if (t > 2.6f) { e.RemoveFromHierarchy(); puffs.RemoveAt(i); continue; }
                        float k = t / 2.6f, size = 26 + 90 * k;
                        e.style.width = size; e.style.height = size; e.style.left = hp.x + v.x * t - size / 2; e.style.top = hp.y + v.y * t - size / 2;
                        e.style.opacity = 0.55f * (1 - k);
                    }
                }
                haze.style.opacity = Mathf.Clamp01((hazeUntil - now) / 2f) * 0.28f;
            }).Every(33);
            return page;
        }

        // tonight at a glance, and the way into the laptop
        static VisualElement Tonight(State s)
        {
            var bar = U.Row().Cls("room-tonight");
            var l = U.Col().Grow();
            var sl = Calendar.SlotsLeft(s);
            l.Add(U.T(Calendar.Str(s.cal.d, true), "room-day", Fonts.Semi).NoPick());
            string news = s.cal.inv != null ? "You've got an invite" : s.cal.meet != null ? "Slick wants to meet" : sl.left > 0 ? $"{sl.left} game{(sl.left == 1 ? "" : "s")} left tonight" : "No games left tonight";
            l.Add(U.T(news, "room-news", Fonts.Medium).NoPick().Fg(s.cal.inv != null || s.cal.meet != null ? C.Gold2 : C.Mute));
            bar.Add(l);
            bar.Add(U.Btn("💻 Laptop", () =>
            {
                var life = CharacterStage.Instance != null ? CharacterStage.Instance.Life : null;
                if (life != null && life.isActiveAndEnabled && life.Room != null) life.Use("laptop"); else Game.SetTab("calendar");
            }, "gold"));
            return bar;
        }

        static VisualElement Morning(State s)
        {
            var card = U.Card("room-morning");
            var top = U.Row(); top.Add(U.Kicker("Good morning").Grow());
            var x = U.Div("icon-btn"); x.Add(new Icon("close", 16, C.Mute)); x.Tap(() => { Game.Morning = null; card.RemoveFromHierarchy(); }); top.Add(x);
            card.Add(top);
            int n = 0;
            foreach (var (icon, text) in Game.Morning) { if (n++ >= 4) break; var r = U.Row().Mt(4); r.Add(U.T(icon, "morning-i", Fonts.Emoji)); r.Add(U.P(text).Grow()); card.Add(r); }
            return card;
        }
    }
}
