using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Dream.Film
{
    // Plays one scene of the script as film: loads the set and the cast, then for each line cuts to its shot, plays the
    // voice and the acting, and holds for the length of the voice plus a beat. Cards are captions over the shot (or on
    // black when there is no set). The render command (Editor/Render.cs) records it and stops on Take.Done.
    public class Player : MonoBehaviour
    {
        public string sceneId = "p_diner";   // played when no render is asking for one
        public const float Beat = 0.45f, CardHold = 2.8f;

        Scene sc; GameObject set; Camera cam; AudioSource voice; Volume volume; DepthOfField dof;
        readonly Dictionary<string, Actor> actors = new Dictionary<string, Actor>();
        Text caption; Image black; CanvasGroup capGroup;
        AudioSource sfx, room;
        Shot shot; Light key, fill; System.IO.StreamWriter cue;

        IEnumerator Start()
        {
            var id = string.IsNullOrEmpty(Take.Id) || Take.Id == "test" ? sceneId : Take.Id;
            sc = Scene.Load(id);
            if (sc == null) { Debug.LogWarning("[Dream] no film " + id); Take.Done = true; yield break; }
            BuildCamera(); BuildCaption();
            if (!string.IsNullOrEmpty(sc.place)) set = Sets.Load(sc.place);
            if (set != null)
                foreach (var who in sc.cast)
                {
                    var mark = Sets.Find(set, "MARK_" + who);
                    if (mark != null) actors[who] = Actor.Spawn(who, mark);
                }
            if (actors.Count == 2)   // stools swivel: two people talking turn a little toward each other
            {
                var a = new List<Actor>(actors.Values);
                for (int i = 0; i < 2; i++)
                {
                    var to = a[1 - i].transform.position - a[i].transform.position; to.y = 0;
                    var side = Vector3.Dot(to, a[i].transform.right) > 0 ? 1f : -1f;
                    a[i].transform.Rotate(0, 25f * side, 0);
                }
            }
            black.color = set == null ? Color.black : Color.clear;
            var amb = Data.Load<AudioClip>($"Sfx/amb_{sc.place}.wav");   // room tone under everything
            if (amb != null) { room = gameObject.AddComponent<AudioSource>(); room.clip = amb; room.loop = true; room.volume = 0.55f; room.Play(); }
            yield return null;   // one frame for the probe and the bodies to settle
            // the cue sheet: when each line starts in the film (video time), so stills can be pulled per line
            try { cue = new System.IO.StreamWriter(System.IO.Path.Combine(Application.dataPath, "..", "Logs", $"film_{id}.txt")); } catch { }
            foreach (var l in sc.lines) yield return PlayLine(l);
            cue?.Close();
            yield return Fade(1f, 0.6f);
            Take.Done = true;
        }

        IEnumerator PlayLine(Line l)
        {
            Business.Before(sc.id, l.n, set, actors);
            shot = set == null ? null : Shot.Make(l.shot, l.subject, l.who, set, actors, sc);
            if (shot != null) { shot.Apply(cam, dof, 0f); LightFor(shot, l.shot); }
            cue?.WriteLine($"{l.n}	{Time.time:0.00}	{l.shot} {l.subject}	{l.who}: {l.text}"); cue?.Flush();
            AudioClip clip = string.IsNullOrEmpty(l.voice) ? null : Data.Load<AudioClip>($"Voice/{l.voice}.wav");
            foreach (var d in sc.directions)   // sounds the stage directions call for
                if (d.after == l.n - 1 && d.text.Contains("bell")) sfx.PlayOneShot(Data.Load<AudioClip>("Sfx/bell.wav"), 0.7f);
            float len = clip != null ? clip.length + Beat : CardHold;
            if (l.who == "card") { caption.text = l.text; StartCoroutine(FadeCaption(len)); }
            if (actors.TryGetValue(l.who, out var speaker))
            {
                speaker.Act(l.act, l.next, len);
                speaker.Say(clip);
                foreach (var a in actors.Values) if (a != speaker) a.LookAt(speaker.Eyes);
                var other = Other(l.who); if (other != null) speaker.LookAt(other.Eyes);
            }
            else if (clip != null) { voice.clip = clip; voice.Play(); }
            for (float t = 0; t < len; t += Time.deltaTime)
            {
                shot?.Apply(cam, dof, t / len);
                yield return null;
            }
        }

        // Film lighting cheats per shot, as a gaffer would: a warm key from above and to the side of the camera
        // (motivated by the pendants), a dim cool fill from the other side (the window). Wides keep the set's light.
        void LightFor(Shot s, string kind)
        {
            if (key == null)
            {
                key = new GameObject("FilmKey").AddComponent<Light>(); key.type = LightType.Spot; key.shadows = LightShadows.Soft;
                key.color = new Color(1f, 0.76f, 0.52f); key.spotAngle = 42; key.innerSpotAngle = 20; key.range = 5;
                key.shadowBias = 0.01f; key.shadowNormalBias = 0.2f;
                fill = new GameObject("FilmFill").AddComponent<Light>(); fill.type = LightType.Point;
                fill.color = new Color(0.55f, 0.65f, 1f); fill.range = 3.5f; fill.shadows = LightShadows.None;
            }
            bool close = kind == "cu" || kind == "ecu" || kind == "push" || kind == "ots" || kind == "two" || kind == "insert";
            key.enabled = fill.enabled = close && set != null;
            if (!key.enabled) return;
            var toCam = s.to - s.focus; toCam.y = 0; toCam = toCam.sqrMagnitude < 1e-4f ? Vector3.back : toCam.normalized;
            key.transform.position = s.focus + Quaternion.AngleAxis(38, Vector3.up) * toCam * 1.5f + Vector3.up * 0.75f;
            key.transform.LookAt(s.focus);
            key.intensity = kind == "two" ? 3f : kind == "insert" ? 1.5f : 3.2f;
            fill.transform.position = s.focus + Quaternion.AngleAxis(-70, Vector3.up) * toCam * 1.4f + Vector3.up * 0.1f;
            fill.intensity = 0.5f;
        }

        Actor Other(string who) { foreach (var kv in actors) if (kv.Key != who) return kv.Value; return null; }

        void BuildCamera()
        {
            cam = new GameObject("FilmCamera").AddComponent<Camera>(); cam.tag = "MainCamera";
            cam.usePhysicalProperties = true; cam.sensorSize = new Vector2(36f, 20.25f); cam.gateFit = Camera.GateFitMode.Horizontal;
            cam.nearClipPlane = 0.05f; cam.farClipPlane = 200f; cam.allowHDR = true; cam.allowMSAA = true;
            cam.clearFlags = CameraClearFlags.Skybox;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true; data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High; data.stopNaN = true;
            cam.gameObject.AddComponent<AudioListener>();
            voice = gameObject.AddComponent<AudioSource>(); voice.spatialBlend = 0;
            sfx = gameObject.AddComponent<AudioSource>(); sfx.spatialBlend = 0;

            // the cinema look: ACES, a gentle grade, bloom on the neon, shallow focus, grain, vignette
            volume = new GameObject("Look").AddComponent<Volume>(); volume.isGlobal = true;
            var p = ScriptableObject.CreateInstance<VolumeProfile>(); volume.sharedProfile = p;
            var look = Sets.Current ?? new Sets.Look();
            p.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
            var bloom = p.Add<Bloom>(); bloom.threshold.Override(0.95f); bloom.intensity.Override(0.9f); bloom.scatter.Override(0.72f);
            var ca = p.Add<ColorAdjustments>(); ca.postExposure.Override(0.2f); ca.contrast.Override(14f); ca.saturation.Override(4f);
            var wb = p.Add<WhiteBalance>(); wb.temperature.Override(4f);
            var smh = p.Add<ShadowsMidtonesHighlights>(); smh.shadows.Override(new Vector4(0.92f, 0.95f, 1.08f, 0f)); smh.highlights.Override(new Vector4(1.06f, 1.0f, 0.92f, 0f));
            var vig = p.Add<Vignette>(); vig.intensity.Override(0.28f); vig.smoothness.Override(0.45f);
            var grain = p.Add<FilmGrain>(); grain.type.Override(FilmGrainLookup.Thin2); grain.intensity.Override(0.22f);
            p.Add<ChromaticAberration>().intensity.Override(0.04f);
            dof = p.Add<DepthOfField>(); dof.mode.Override(DepthOfFieldMode.Bokeh); dof.focusDistance.Override(2f);
            dof.focalLength.Override(50f); dof.aperture.Override(2.4f); dof.bladeCount.Override(7);
        }

        void BuildCaption()
        {
            var canvas = new GameObject("Captions").AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var scaler = canvas.gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
            black = new GameObject("Black").AddComponent<Image>(); black.transform.SetParent(canvas.transform, false);
            Stretch(black.rectTransform); black.color = Color.clear; black.raycastTarget = false;
            caption = new GameObject("Caption").AddComponent<Text>(); caption.transform.SetParent(canvas.transform, false);
            Stretch(caption.rectTransform); caption.rectTransform.offsetMin = new Vector2(160, 110); caption.rectTransform.offsetMax = new Vector2(-160, -110);
            caption.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); caption.fontSize = 44; caption.color = new Color(0.96f, 0.94f, 0.9f);
            caption.alignment = TextAnchor.LowerCenter; caption.horizontalOverflow = HorizontalWrapMode.Wrap;
            var sh = caption.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.8f); sh.effectDistance = new Vector2(2, -2);
            capGroup = caption.gameObject.AddComponent<CanvasGroup>(); capGroup.alpha = 0;
        }

        static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }

        IEnumerator FadeCaption(float hold)
        {
            for (float t = 0; t < 0.5f; t += Time.deltaTime) { capGroup.alpha = t / 0.5f; yield return null; }
            capGroup.alpha = 1; yield return new WaitForSeconds(Mathf.Max(0, hold - 1.0f));
            for (float t = 0; t < 0.5f; t += Time.deltaTime) { capGroup.alpha = 1 - t / 0.5f; yield return null; }
            capGroup.alpha = 0;
        }

        IEnumerator Fade(float to, float time)
        {
            var from = black.color.a;
            for (float t = 0; t < time; t += Time.deltaTime) { black.color = new Color(0, 0, 0, Mathf.Lerp(from, to, t / time)); yield return null; }
            black.color = new Color(0, 0, 0, to);
        }
    }
}
