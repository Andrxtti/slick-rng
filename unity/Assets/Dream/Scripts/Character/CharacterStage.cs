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
        public RenderTexture Texture => framing == "room" && roomRt != null ? roomRt : rt;
        // your place: the room, life in it (walking, using things) and its own render texture shaped like the view
        HomeRoom room; HomeLife life; PetLife pets; RenderTexture roomRt; Vector3 roomPos; float roomYaw; bool hasRoomPos;
        public bool InRoom => framing == "room";
        public HomeLife Life => life;
        public PetLife Pets => pets;
        public HomeRoom Room => room;
        public Camera Cam => cam;
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
            rt = new RenderTexture(768, 960, 24, RenderTextureFormat.ARGB32) { name = "Character", antiAliasing = 1, useMipMap = false }; // single-sample: UI Toolkit can't show a multisampled texture
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
            if (framing == "room") { FrameRoom(); return; }
            float lookY, dist;
            switch (framing)
            {
                case "full": lookY = height * 0.52f; dist = height * 3.3f; break;
                case "face": lookY = height * 0.925f; dist = height * 0.44f; break;
                default: lookY = height * 0.78f; dist = height * 1.55f; break;
            }
            cam.transform.localPosition = new Vector3(0, lookY, dist);
            cam.transform.LookAt(transform.TransformPoint(new Vector3(0, lookY - 0.04f * (framing == "face" ? 0.3f : 1), 0)));
        }
        public void Framing(string mode, string tier = null, float aspect = 0, string pet = null)
        {
            if (mode == "room") { EnterRoom(tier ?? room?.tier ?? "studio", aspect, pet); return; }
            if (framing == "room") ExitRoom();
            if (mode != framing) Frame(bodyH, mode);
        }

        // ------------------------------------------------------------ your place
        void EnterRoom(string tier, float aspect, string pet)
        {
            if (room == null || room.tier != tier)
            {
                if (room != null) Destroy(room.root);
                room = HomeRoom.Build(tier, transform, Layer); hasRoomPos = false; pivot.localPosition = Vector3.zero;
                if (room == null) return;
            }
            room.root.SetActive(true);
            if (hasRoomPos) { pivot.localPosition = roomPos; pivot.localRotation = Quaternion.Euler(0, roomYaw, 0); }
            if (life == null) life = gameObject.AddComponent<HomeLife>();
            life.enabled = true;
            if (framing != "room") life.Init(this, room);
            if (pets == null) pets = gameObject.AddComponent<PetLife>();
            pets.enabled = true; pets.Set(this, room, pet); life.Pets = pets;
            RoomTexture(aspect > 0 ? aspect : (roomRt != null ? roomRt.width / (float)roomRt.height : 0.62f));
            framing = "room"; cam.targetTexture = roomRt; FrameRoom();
            cam.enabled = views.Count > 0 && HasBody;
        }
        void ExitRoom()
        {
            if (room != null) { roomPos = pivot.localPosition; roomYaw = pivot.localEulerAngles.y; hasRoomPos = true; room.root.SetActive(false); }
            if (life != null) life.enabled = false;
            if (pets != null) pets.enabled = false;
            pivot.localPosition = Vector3.zero; cam.targetTexture = rt; cam.fieldOfView = 22; framing = "bust";
        }
        public void RoomTexture(float aspect)
        {
            int h = 1280, w = Mathf.Clamp(Mathf.RoundToInt(h * aspect), 400, 2560);
            if (roomRt != null && roomRt.width == w && roomRt.height == h) return;
            if (roomRt != null) { if (cam.targetTexture == roomRt) cam.targetTexture = null; roomRt.Release(); }
            roomRt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { name = "Room", antiAliasing = 1, useMipMap = false };
            roomRt.Create();
            if (framing == "room") { cam.targetTexture = roomRt; FrameRoom(); }
        }
        // a three-quarter view from the open front corner, as close as it can get with the whole room in shot
        void FrameRoom()
        {
            if (room == null) return;
            float aspect = cam.targetTexture != null ? cam.targetTexture.width / (float)cam.targetTexture.height : 0.62f;
            cam.fieldOfView = Mathf.Clamp(2 * Mathf.Atan(Mathf.Tan(29f * Mathf.Deg2Rad) / aspect) * Mathf.Rad2Deg, 34f, 80f);
            var target = new Vector3(room.W * 0.04f, 0.7f, -room.D * 0.06f);
            // a phone held upright looks down more steeply, so the floor fills the tall screen
            float tall = Mathf.Clamp01((1f - aspect) / 0.5f);
            var dir = Vector3.Lerp(new Vector3(0.45f, 0.9f, 1f), new Vector3(0.3f, 1.55f, 1f), tall).normalized;
            float W = room.W / 2, D = room.D / 2, H = room.H;
            var pts = new[] { new Vector3(-W, 0, -D), new Vector3(W, 0, -D), new Vector3(-W, 0, D), new Vector3(W, 0, D), new Vector3(-W, H, -D), new Vector3(W, H, -D), new Vector3(-W, H, D) };
            bool Fits(float dist)
            {
                cam.transform.localPosition = target + dir * dist; cam.transform.LookAt(transform.TransformPoint(target));
                foreach (var p in pts) { var v = cam.WorldToViewportPoint(transform.TransformPoint(p)); if (v.z <= 0 || v.x < 0.02f || v.x > 0.98f || v.y < 0.02f || v.y > 0.97f) return false; }
                return true;
            }
            float lo = 1.5f, hi = 40f;
            for (int i = 0; i < 22; i++) { float mid = (lo + hi) / 2; if (Fits(mid)) hi = mid; else lo = mid; }
            Fits(hi);
            cam.farClipPlane = hi + 30;
        }
        // a tap on the room view (viewport coordinates): use what's there, or walk to the spot
        public void TapRoom(Vector2 uv)
        {
            if (room == null || life == null) return;
            var ray = cam.ViewportPointToRay(new Vector3(uv.x, uv.y, 0));
            if (!Physics.Raycast(ray, out var hit, 200f, 1 << Layer)) return;
            foreach (var u in room.uses) if (u.tr != null && (hit.transform == u.tr || hit.transform.IsChildOf(u.tr))) { life.Use(u.id); return; }
            life.WalkTo(transform.InverseTransformPoint(hit.point));
        }
        public Vector3 Viewport(Vector3 world) => cam.WorldToViewportPoint(world);
        float bodyH = 1.7f;
        // frame the body actually built: eye height plus the crown of the head (UMA's default man is about 1.95 m)
        void Measure()
        {
            var e = Bone(HumanBodyBones.LeftEye) ?? Bone(HumanBodyBones.Head);
            if (e == null) return;
            float h = Mathf.Clamp(transform.InverseTransformPoint(e.position).y + 0.13f, 1.2f, 2.4f);
            if (Mathf.Abs(h - bodyH) > 0.01f) { bodyH = h; Frame(bodyH); }
        }

        // ------------------------------------------------------------ views
        public void Attach(VisualElement v) { views.Add(v); cam.enabled = HasBody; }
        public void Detach(VisualElement v) { views.Remove(v); if (views.Count == 0) cam.enabled = false; }
        public void Drag(float dx) { yaw -= dx * 0.45f; spin = -dx * 0.45f; dragging = true; }
        public void Release() { dragging = false; }

        void LateUpdate()
        {
            if (views.Count == 0 || pivot == null) return;
            if (framing != "room")   // in your place, walking decides which way you face
            {
                if (!dragging) { spin *= 0.92f; yaw += spin; yaw = Mathf.LerpAngle(yaw, -12 + Mathf.Sin(Time.time * 0.6f) * 4, 0.01f); }
                pivot.localRotation = Quaternion.Euler(0, yaw, 0);
            }
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
                Measure();
                Accessories.Dress(this, spec, accessories);
                pivot.localRotation = keep;
                foreach (var a in accessories) SetLayer(a, Layer);
                cam.enabled = views.Count > 0;
#if UNITY_EDITOR
                Report();
#endif
                Changed?.Invoke();
            });
        }
#if UNITY_EDITOR
        // what the stage camera should be seeing, for Logs/dream_play.txt
        void Report()
        {
            var rs = body.Root != null ? body.Root.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
            var sb = new System.Text.StringBuilder($"[Dream] stage: {rs.Length} renderers, {accessories.Count} accessories, views {views.Count}, cam {cam.enabled} at {cam.transform.position} looking {cam.transform.forward}, mask {cam.cullingMask}, pipeline {UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.name}");
            foreach (var r in rs)
                sb.Append($"\n  {r.name}: layer {r.gameObject.layer}, on {r.enabled && r.gameObject.activeInHierarchy}, bounds {r.bounds.center} size {r.bounds.size}, mats {r.sharedMaterials.Length}");
            foreach (var r in rs)
                foreach (var m in r.sharedMaterials)
                    if (m != null)
                    {
                        sb.Append($"\n    {m.name} [{m.shader.name}]");
                        for (int i = 0; i < m.shader.GetPropertyCount(); i++)
                        {
                            var pn = m.shader.GetPropertyName(i); var pt = m.shader.GetPropertyType(i);
                            if (pt == UnityEngine.Rendering.ShaderPropertyType.Color) sb.Append($" {pn}={ColorUtility.ToHtmlStringRGBA(m.GetColor(pn))}");
                            else if (pt == UnityEngine.Rendering.ShaderPropertyType.Texture) { var t = m.GetTexture(pn); if (t != null) sb.Append($" {pn}:{t.name}"); }
                        }
                    }
            string P(HumanBodyBones b) { var t = Bone(b); return t ? t.position.ToString("F3") : "none"; }
            sb.Append($"\n  hips {P(HumanBodyBones.Hips)}, neck {P(HumanBodyBones.Neck)}, head {P(HumanBodyBones.Head)}, eyes {P(HumanBodyBones.LeftEye)} {P(HumanBodyBones.RightEye)}, upper arms {P(HumanBodyBones.LeftUpperArm)} {P(HumanBodyBones.RightUpperArm)}, feet {P(HumanBodyBones.LeftFoot)}");
            Debug.Log(sb.ToString());
            StartCoroutine(Snap());
        }
        public void SnapNow() { if (gameObject.activeInHierarchy) StartCoroutine(Snap()); }
        // save what the stage camera renders to Logs/stage.png, with a count of the pixels that aren't see-through
        System.Collections.IEnumerator Snap()
        {
            for (int i = 0; i < 3; i++) yield return new WaitForEndOfFrame();
            var src = Texture; var prev = RenderTexture.active; RenderTexture.active = src;
            var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0); tex.Apply();
            RenderTexture.active = prev;
            var px = tex.GetPixels32(); int solid = 0, coloured = 0;
            foreach (var p in px) { if (p.a > 25) solid++; if (p.r + p.g + p.b > 30) coloured++; }
            Debug.Log($"[Dream] stage render: {solid} of {px.Length} pixels solid, {coloured} coloured");
            foreach (var v in views)
            {
                var bg = v.resolvedStyle.backgroundImage;
                Debug.Log($"[Dream] view {v.GetType().Name}: world {v.worldBound}, rt {(bg.renderTexture != null ? bg.renderTexture.name : "none")}, tex {(bg.texture != null)}, display {v.resolvedStyle.display}, vis {v.resolvedStyle.visibility}, opacity {v.resolvedStyle.opacity}, tint {v.resolvedStyle.unityBackgroundImageTintColor}, children {v.childCount}, panel {(v.panel != null)}");
                for (var p = v.parent; p != null; p = p.parent)
                    if (p.resolvedStyle.opacity < 0.99f || p.resolvedStyle.display == DisplayStyle.None || p.resolvedStyle.visibility == Visibility.Hidden)
                        Debug.Log($"[Dream]   hidden by {p.name}/{string.Join(".", p.GetClasses())}: opacity {p.resolvedStyle.opacity}, display {p.resolvedStyle.display}");
            }
            try { System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "..", "Logs", "stage.png"), tex.EncodeToPNG()); } catch (Exception) { }
            Destroy(tex);
        }
#endif
        public Transform Bone(HumanBodyBones b) => body?.Bone(b);
        public GameObject BodyRoot => body?.Root;

        static void SetLayer(GameObject go, int layer)
        {
            if (go == null) return;
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        void OnDestroy() { body?.Dispose(); if (rt != null) rt.Release(); if (roomRt != null) roomRt.Release(); if (inst == this) inst = null; }
    }

    // ------------------------------------------------------------ the on-screen view
    // Shows the stage's render texture; drag sideways to turn the character. Without a 3D body it shows the 2D
    // portrait instead, so every screen works before UMA is installed.
    public class CharacterView : VisualElement
    {
        readonly CharacterSpec spec; readonly VisualElement fallback;
        bool down; float lastX; Vector2 downAt; int pid = -1;
        readonly string framing;
        public string Tier, Pet;            // "room" views: which home, and who lives there with you
        bool Room => framing == "room";

        public CharacterView(CharacterSpec spec, VisualElement fallback2D, float width, float height, string framing = "bust")
        {
            this.spec = spec; fallback = fallback2D; this.framing = framing;
            if (width > 0) style.width = width; if (height > 0) style.height = height; style.flexShrink = 0;
            AddToClassList("char-view");
            if (!Bodies.Available) { if (fallback != null) Add(fallback); return; }
            RegisterCallback<AttachToPanelEvent>(e => Bind());
            RegisterCallback<DetachFromPanelEvent>(e => CharacterStage.Instance?.Detach(this));
            RegisterCallback<PointerDownEvent>(e => { down = true; pid = e.pointerId; lastX = e.position.x; downAt = e.position; this.CapturePointer(pid); });
            RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!down || e.pointerId != pid || Room) return;
                CharacterStage.Instance?.Drag(e.position.x - lastX); lastX = e.position.x;
            });
            RegisterCallback<PointerUpEvent>(e =>
            {
                if (e.pointerId != pid) return; down = false; this.ReleasePointer(pid); CharacterStage.Instance?.Release();
                if (Room && Vector2.Distance(e.position, downAt) < 14) CharacterStage.Instance?.TapRoom(ToUV(this.WorldToLocal(e.position)));
            });
        }

        void Bind()
        {
            var st = CharacterStage.Get();
            st.Framing(framing, Tier, Aspect(), Pet);
            st.Attach(this);
            SetTexture(st);
            st.Show(spec);
            if (!st.HasBody && fallback != null && fallback.parent == null) Add(fallback);
            Action onChanged = null;
            onChanged = () => { if (fallback != null && fallback.parent == this) Remove(fallback); };
            st.Changed += onChanged;
            RegisterCallback<DetachFromPanelEvent>(e => st.Changed -= onChanged);
            if (st.HasBody && fallback != null && fallback.parent == this) Remove(fallback);
            // a room view's texture follows the view's shape
            if (Room) RegisterCallback<GeometryChangedEvent>(e => { if (Aspect() > 0 && st.InRoom) { st.RoomTexture(Aspect()); SetTexture(st); } });
        }
        void SetTexture(CharacterStage st)
        {
            style.backgroundImage = new StyleBackground(Background.FromRenderTexture(st.Texture));
            style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        }
        float Aspect() { var r = layout; return r.width > 10 && r.height > 10 ? r.width / r.height : 0; }

        // the room texture matches the view's shape, so view and viewport map straight across
        public Vector2 ToUV(Vector2 local) { var r = contentRect; return new Vector2(local.x / Mathf.Max(1, r.width), 1 - local.y / Mathf.Max(1, r.height)); }
        public Vector2 FromViewport(Vector3 vp) { var r = contentRect; return new Vector2(vp.x * r.width, (1 - vp.y) * r.height); }
    }
}
