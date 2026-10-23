// The 3D character lives on a small stage far below the world: its own camera, a three-light studio rig, and a
// render texture that every CharacterView on screen shows. The camera only renders while a view is visible.
//
// The body itself comes from a provider (ICharacterBody). UMA registers one when it's installed; until then the
// views fall back to the 2D portrait. Rigid accessories (chains, watches, rings, earrings, glasses) are built
// here and pinned to the body's bones, so any body that exposes a humanoid skeleton can wear them.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFDream.Character
{
    public interface ICharacterBody
    {
        GameObject Root { get; }
        bool Ready { get; }
        // build or update to match the spec; onReady runs once the body (and its skeleton) is in place
        void Apply(CharacterSpec spec, Action onReady);
        Transform Bone(HumanBodyBones b);
        BodyFit Fit { get; }             // where jewellery and hats sit on this body; null for the defaults
        void Dispose();
    }

    public static class Bodies
    {
        // set by a body provider (UMA, or a future pro-art body) at startup
        public static Func<Transform, ICharacterBody> Create;
        public static string Name = "none";
        public static bool Available => Create != null;
    }

    public class CharacterStage : MonoBehaviour
    {
        public const int Layer = 30; // a layer only the stage camera sees
        static CharacterStage inst;
        public static CharacterStage Instance => inst;

        Camera cam; Transform pivot; RenderTexture rt;
        ICharacterBody body; string builtKey, pendingKey; CharacterSpec pending;
        readonly List<GameObject> accessories = new List<GameObject>();
        readonly HashSet<VisualElement> views = new HashSet<VisualElement>();
        float yaw = -12, spin; bool dragging;
        public bool HasBody => body != null && body.Ready;
        public RenderTexture Texture => rt;
        public Transform Pivot => pivot;
        public BodyFit Fit => body?.Fit;
        public event Action Changed;

        public static CharacterStage Get()
        {
            if (inst != null) return inst;
            var go = new GameObject("Character Stage"); DontDestroyOnLoad(go);
            inst = go.AddComponent<CharacterStage>(); inst.Setup();
            return inst;
        }

        void Setup()
        {
            transform.position = new Vector3(0, -400, 0);
            pivot = new GameObject("Pivot").transform; pivot.SetParent(transform, false);
            var cgo = new GameObject("Stage Camera"); cgo.transform.SetParent(transform, false);
            cam = cgo.AddComponent<Camera>();
            cam.cullingMask = 1 << Layer; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.fieldOfView = 22; cam.nearClipPlane = 0.05f; cam.farClipPlane = 30; cam.allowHDR = false; cam.allowMSAA = true; cam.enabled = false;
            rt = new RenderTexture(768, 960, 24, RenderTextureFormat.ARGB32) { name = "Character", antiAliasing = 4, useMipMap = false };
            rt.Create(); cam.targetTexture = rt;
            Frame(1.7f);
            // a soft studio: warm key, cool fill, gold rim from behind
            // the character faces +Z (towards the camera); light directions are where the light travels
            AddLight("Key", LightType.Directional, new Color(1f, 0.95f, 0.88f), 1.15f, new Vector3(28, 149, 0));
            AddLight("Fill", LightType.Directional, new Color(0.72f, 0.8f, 1f), 0.45f, new Vector3(10, 211, 0));
            AddLight("Rim", LightType.Directional, new Color(0.95f, 0.8f, 0.5f), 0.9f, new Vector3(18, -18, 0));
        }
        void AddLight(string n, LightType t, Color c, float intensity, Vector3 euler)
        {
            var go = new GameObject(n); go.transform.SetParent(transform, false); go.transform.localEulerAngles = euler;
            var l = go.AddComponent<Light>(); l.type = t; l.color = c; l.intensity = intensity; l.cullingMask = 1 << Layer; l.shadows = LightShadows.None;
        }

        // aim at a character about `height` metres tall standing at the pivot: "full" (head to toe), "bust"
        // (chest up) or "face" (for the creator)
        string framing = "bust";
        void Frame(float height, string mode = null)
        {
            framing = mode ?? framing;
            float lookY, dist;
            switch (framing)
            {
                case "full": lookY = height * 0.53f; dist = height * 2.95f; break;
                case "face": lookY = height * 0.925f; dist = height * 0.44f; break;
                default: lookY = height * 0.78f; dist = height * 1.55f; break;
            }
            cam.transform.localPosition = new Vector3(0, lookY, dist);
            cam.transform.LookAt(transform.TransformPoint(new Vector3(0, lookY - 0.04f * (framing == "face" ? 0.3f : 1), 0)));
        }
        public void Framing(string mode) { if (mode != framing) Frame(1.7f, mode); }

        // ------------------------------------------------------------ views
        public void Attach(VisualElement v) { views.Add(v); cam.enabled = HasBody; }
        public void Detach(VisualElement v) { views.Remove(v); if (views.Count == 0) cam.enabled = false; }
        public void Drag(float dx) { yaw -= dx * 0.45f; spin = -dx * 0.45f; dragging = true; }
        public void Release() { dragging = false; }

        void LateUpdate()
        {
            if (views.Count == 0 || pivot == null) return;
            if (!dragging) { spin *= 0.92f; yaw += spin; yaw = Mathf.LerpAngle(yaw, -12 + Mathf.Sin(Time.time * 0.6f) * 4, 0.01f); }
            pivot.localRotation = Quaternion.Euler(0, yaw, 0);
            foreach (var v in views) v.MarkDirtyRepaint();
        }

        // ------------------------------------------------------------ the body
        public void Show(CharacterSpec spec)
        {
            if (!Bodies.Available || spec == null) return;
            string key = spec.Fingerprint();
            if (key == builtKey || key == pendingKey) return;
            pendingKey = key; pending = spec;
            if (body == null)
            {
                try { body = Bodies.Create(pivot); }
                catch (Exception e) { Debug.LogException(e); body = null; return; }
            }
            body.Apply(spec, () =>
            {
                builtKey = key; if (pendingKey == key) pendingKey = null;
                SetLayer(body.Root, Layer);
                var keep = pivot.localRotation; pivot.localRotation = Quaternion.identity; // build pieces facing +Z
                Accessories.Dress(this, spec, accessories);
                pivot.localRotation = keep;
                foreach (var a in accessories) SetLayer(a, Layer);
                cam.enabled = views.Count > 0;
                Changed?.Invoke();
            });
        }
        public Transform Bone(HumanBodyBones b) => body?.Bone(b);

        static void SetLayer(GameObject go, int layer)
        {
            if (go == null) return;
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        void OnDestroy() { body?.Dispose(); if (rt != null) rt.Release(); if (inst == this) inst = null; }
    }

    // ------------------------------------------------------------ the on-screen view
    // Shows the stage's render texture; drag sideways to turn the character. Without a 3D body it shows the 2D
    // portrait instead, so every screen works before UMA is installed.
    public class CharacterView : VisualElement
    {
        readonly CharacterSpec spec; readonly VisualElement fallback;
        bool down; float lastX; int pid = -1;

        readonly string framing;
        public CharacterView(CharacterSpec spec, VisualElement fallback2D, float width, float height, string framing = "bust")
        {
            this.spec = spec; fallback = fallback2D; this.framing = framing;
            style.width = width; style.height = height; style.flexShrink = 0;
            AddToClassList("char-view");
            if (!Bodies.Available) { if (fallback != null) Add(fallback); return; }
            RegisterCallback<AttachToPanelEvent>(e => Bind());
            RegisterCallback<DetachFromPanelEvent>(e => CharacterStage.Instance?.Detach(this));
            RegisterCallback<PointerDownEvent>(e => { down = true; pid = e.pointerId; lastX = e.position.x; this.CapturePointer(pid); });
            RegisterCallback<PointerMoveEvent>(e => { if (!down || e.pointerId != pid) return; CharacterStage.Instance?.Drag(e.position.x - lastX); lastX = e.position.x; });
            RegisterCallback<PointerUpEvent>(e => { if (e.pointerId != pid) return; down = false; this.ReleasePointer(pid); CharacterStage.Instance?.Release(); });
        }

        void Bind()
        {
            var st = CharacterStage.Get();
            st.Framing(framing);
            st.Attach(this);
            style.backgroundImage = new StyleBackground(Background.FromRenderTexture(st.Texture));
            style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            st.Show(spec);
            if (!st.HasBody && fallback != null && fallback.parent == null) Add(fallback);
            Action onChanged = null;
            onChanged = () => { if (fallback != null && fallback.parent == this) Remove(fallback); };
            st.Changed += onChanged;
            RegisterCallback<DetachFromPanelEvent>(e => st.Changed -= onChanged);
            if (st.HasBody && fallback != null && fallback.parent == this) Remove(fallback);
        }
    }
}
