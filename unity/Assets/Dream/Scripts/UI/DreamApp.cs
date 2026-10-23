// Boots The Dream in any scene: press Play and it builds its own UI Toolkit panel, loads the game and draws
// whichever screen the game is on. No scene setup, prefabs or inspector wiring needed.
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFDream.UI
{
    public class DreamApp : MonoBehaviour
    {
        public static DreamApp I;
        public static bool Landscape; // wider than tall: screens switch to two-pane layouts
        PanelSettings ps;
        UIDocument doc;
        VisualElement root, column, home, modal, toast;
        Rect lastSafe; int lastW, lastH;
        string lastKey; Vector2 lastScroll;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (I != null || FindAnyObjectByType<DreamApp>() != null) return;
            Application.targetFrameRate = 60;
            // turn with the phone: portrait or either landscape, never upside down
            Screen.autorotateToPortrait = true; Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true; Screen.autorotateToLandscapeRight = true;
            if (!Application.isEditor || Screen.orientation != ScreenOrientation.AutoRotation) Screen.orientation = ScreenOrientation.AutoRotation;
            var go = new GameObject("The Dream"); go.SetActive(false); DontDestroyOnLoad(go);
            var ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.name = "Dream Panel";
            ps.themeStyleSheet = Resources.Load<ThemeStyleSheet>("Dream/DreamTheme");
            ps.scaleMode = PanelScaleMode.ConstantPixelSize;
            ps.scale = UiScale();
            ps.sortingOrder = 10;
            var d = go.AddComponent<UIDocument>(); d.panelSettings = ps;
            var app = go.AddComponent<DreamApp>(); app.ps = ps;
            go.SetActive(true);
        }

        void Awake() { I = this; doc = GetComponent<UIDocument>(); }

        void Start()
        {
            Fonts.Load();
            EnsureEventSystem();
            QuietCamera();
            root = doc.rootVisualElement;
            root.style.position = Position.Absolute; root.style.left = 0; root.style.right = 0; root.style.top = 0; root.style.bottom = 0;
            var sheet = Resources.Load<StyleSheet>("Dream/dream"); if (sheet != null) root.styleSheets.Add(sheet); else Debug.LogWarning("[Dream] dream.uss not found in Resources/Dream");
            root.AddToClassList("app"); Fonts.Use(root, Fonts.Body);
            column = U.Div("column"); root.Add(column);
            home = HomeButton(); root.Add(home);
            modal = U.Div("modal").Show(false); modal.style.backgroundColor = new Color(0, 0, 0, 0.66f); root.Add(modal);
            toast = U.Div("toast").Show(false); root.Add(toast);
            ApplyScreen(true);
            Game.Render = RenderNow;
            try { Game.Boot(); }
            catch (Exception e) { Debug.LogException(e); column.Add(Screens.ErrorView(e)); return; }
            RenderNow();
        }

        void Update() => ApplyScreen(false);

        // One scale for both orientations: a phone is 430 units on its short side and 932 on its long side, whichever way
        // it's held, so text and buttons stay the same size when you rotate. Desktops and tablets get the smaller fit.
        static float UiScale()
        {
            float w = Mathf.Max(1, Screen.width), h = Mathf.Max(1, Screen.height);
            return Mathf.Max(0.5f, Mathf.Min(Mathf.Min(w, h) / 430f, Mathf.Max(w, h) / 932f));
        }

        void ApplyScreen(bool force)
        {
            if (root == null) return;
            if (!force && Screen.width == lastW && Screen.height == lastH && Screen.safeArea == lastSafe) return;
            lastW = Screen.width; lastH = Screen.height; lastSafe = Screen.safeArea;
            if (ps != null) ps.scale = UiScale();
            bool land = Screen.width > Screen.height * 1.15f;
            root.EnableInClassList("landscape", land);
            if (land != Landscape) { Landscape = land; if (!force && Game.State != null) RenderNow(); }
            if (root.panel == null) return;
            var sa = Screen.safeArea;
            var lt = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(sa.xMin, Screen.height - sa.yMax));
            var rb = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(Screen.width - sa.xMax, sa.yMin));
            root.style.paddingLeft = lt.x; root.style.paddingTop = lt.y; root.style.paddingRight = rb.x; root.style.paddingBottom = rb.y;
            home.style.top = 10 + lt.y; home.style.right = 12 + rb.x;
        }

        // The UI covers the whole screen, so the 3D camera behind it is only burning battery. Turn it off until the 3D
        // character arrives; the panel clears the screen itself.
        void QuietCamera()
        {
            if (ps != null) { ps.clearColor = true; ps.colorClearValue = C.Ink; }
            foreach (var cam in Camera.allCameras) cam.enabled = false;
        }

        static void EnsureEventSystem()
        {
#if ENABLE_INPUT_SYSTEM
            try
            {
                Type es = Fonts.FindType("UnityEngine.EventSystems.EventSystem"), mod = Fonts.FindType("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
                if (es == null || mod == null || FindAnyObjectByType(es) != null) return;
                var go = new GameObject("EventSystem", es, mod); DontDestroyOnLoad(go);
            }
            catch (Exception e) { Debug.LogWarning("[Dream] EventSystem: " + e.Message); }
#endif
        }

        // ------------------------------------------------------------ drawing the current screen
        string Key() => Game.View == View.Hub ? $"hub:{Game.Tab}:{(Game.Tab == "closet" ? Game.ClosetCat : "")}" : Game.View == View.Scene ? $"scene:{Game.Scene?.id}" : Game.View.ToString();

        public void RenderNow()
        {
            if (column == null) return;
            var sv = column.Q<ScrollView>("main"); if (sv != null) lastScroll = sv.scrollOffset;
            string key = Key(); bool same = key == lastKey; lastKey = key;
            column.Clear();
            VisualElement view;
            try { view = Screens.Build(); }
            catch (Exception e) { Debug.LogException(e); view = Screens.ErrorView(e); }
            column.Add(view);
            root.style.backgroundColor = Screens.Backdrop;
            var v = Game.View;
            bool showHome = v == View.Run || v == View.Results || v == View.Profile || (v == View.Create && Game.Restyle);
            home.Show(showHome); root.EnableInClassList("has-home", showHome);
            if (same)
            {
                var nsv = column.Q<ScrollView>("main"); var off = lastScroll;
                if (nsv != null && off.y > 0)
                {
                    EventCallback<GeometryChangedEvent> cb = null;
                    cb = e => { nsv.contentContainer.UnregisterCallback(cb); nsv.scrollOffset = off; };
                    nsv.contentContainer.RegisterCallback(cb);
                }
            }
        }

        VisualElement HomeButton()
        {
            var b = U.Div("home-btn");
            var icon = new VisualElement().NoPick(); icon.style.width = 20; icon.style.height = 20;
            icon.generateVisualContent += mgc =>
            {
                var p = mgc.painter2D; p.strokeColor = C.A(C.Cream, 0.9f); p.lineWidth = 1.8f; p.lineJoin = LineJoin.Round; p.lineCap = LineCap.Round;
                p.BeginPath(); p.MoveTo(new Vector2(2, 10)); p.LineTo(new Vector2(10, 2.5f)); p.LineTo(new Vector2(18, 10)); p.Stroke();
                p.BeginPath(); p.MoveTo(new Vector2(4.5f, 8.5f)); p.LineTo(new Vector2(4.5f, 18)); p.LineTo(new Vector2(15.5f, 18)); p.LineTo(new Vector2(15.5f, 8.5f)); p.Stroke();
                p.BeginPath(); p.MoveTo(new Vector2(8.5f, 18)); p.LineTo(new Vector2(8.5f, 13)); p.LineTo(new Vector2(11.5f, 13)); p.LineTo(new Vector2(11.5f, 18)); p.Stroke();
            };
            b.Add(icon);
            b.Tap(GoHome);
            return b;
        }

        void GoHome()
        {
            switch (Game.View)
            {
                case View.Run:
                    Confirm("Leave the table?", Game.Run != null && Game.Run.ev.kind == "cash" ? "You'll cash out where your stack is right now." : "Walking away counts as busting out of this event.",
                        "Leave", () => { Game.Finish(true); });
                    return;
                case View.Create: Game.CancelRestyle(); return;
                default: Game.Hub(); return;
            }
        }

        // ------------------------------------------------------------ modal and toast
        public static void Confirm(string title, string text, string yes, Action onYes, string no = "Stay")
        {
            var m = I.modal; m.Clear(); m.Show(true);
            var box = U.Card("modal-box");
            box.Add(U.H2(title)); box.Add(U.P(text).Mt(6));
            var row = U.Row().Mt(14);
            row.Add(U.Btn(no, () => m.Show(false), "ghost grow"));
            row.Add(U.Btn(yes, () => { m.Show(false); onYes?.Invoke(); }, "gold grow"));
            box.Add(row);
            m.Add(box);
        }
        // a modal panel with a title, a close button and whatever `fill` puts in it
        public static void Sheet(string title, Action<VisualElement> fill)
        {
            var m = I.modal; m.Clear(); m.Show(true);
            var box = U.Card("modal-box");
            var hd = U.Row(); hd.Add(U.H2(title).Grow());
            var x = U.Div("icon-btn"); x.Add(new Icon("close", 18, C.Mute)); x.Tap(CloseModal); hd.Add(x);
            box.Add(hd);
            fill?.Invoke(box);
            m.Add(box);
        }
        public static void CloseModal() { if (I != null) { I.modal.Clear(); I.modal.Show(false); } }

        public static void Toast(string text)
        {
            var t = I.toast; t.Clear(); t.Add(U.T(text, "toast-t", Fonts.Semi)); t.Show(true); t.RemoveFromClassList("gone");
            t.schedule.Execute(() => t.AddToClassList("gone")).StartingIn(1600);
            t.schedule.Execute(() => t.Show(false)).StartingIn(2000);
        }
    }
}
