using System.Runtime.InteropServices;
using UnityEngine;

namespace Dream.Game
{
    // B3: coming back to the game after the page was hidden (another app, another tab, the lock screen). iOS pauses the
    // film's <video> and suspends the audio while hidden; nothing may wait on them when the page comes back. On hidden:
    // save. On shown: unpause the audio, restart a paused film, and give the film watchdogs a fresh clock (they measure
    // real time, so a long absence used to look like a stalled film). A film that still won't play is ended by the
    // watchdog, so no screen is ever left waiting on a video.
    public class Resume : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void Dream_HookPage(string go);
#endif
        public static int Count;   // how many times the page came back (test driver)
        public static bool Hidden; // the page is hidden: nothing judges a film while the browser has it paused

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            var go = new GameObject("DreamResume"); DontDestroyOnLoad(go); go.AddComponent<Resume>();
#if UNITY_WEBGL && !UNITY_EDITOR
            Dream_HookPage(go.name);
#endif
        }

        public void OnPageHidden(string _) { Hidden = true; PlayerPrefs.Save(); }

        public void OnPageShown(string _)
        {
            bool was = Hidden; Hidden = false;   // focus and pageshow arrive as well: only a page that was hidden wakes the film
            AudioListener.pause = false;
            if (was) { Count++; Films.I?.Wake(); }
        }

        void OnApplicationPause(bool paused) { if (paused) OnPageHidden(""); else OnPageShown(""); }
        void OnApplicationFocus(bool focus) { if (focus) OnPageShown(""); }
    }
}
