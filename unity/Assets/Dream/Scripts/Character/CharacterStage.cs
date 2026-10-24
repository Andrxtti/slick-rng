// The 3D character lives on a small stage far below the world: its own camera, a three-light studio rig, and a
// render texture that every CharacterView on screen shows. The camera only renders while a view is visible.
//
// The body itself comes from a provider (ICharacterBody). UMA registers one when it's installed; until then the
// views fall back to the 2D portrait. Rigid accessories (chains, watches, rings, earrings, glasses) are built
// here and pinned to the body's bones, so any body that exposes a humanoid skeleton can wear them.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
        void Express(string mood, bool talking, float t);   // the face: a mood, and the mouth while a line plays
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
        public RenderTexture Texture => (framing == "room" || inScene) && roomRt != null ? roomRt : rt;
        // your place: the room, life in it (walking, using things) and its own render texture shaped like the view
        HomeRoom room; HomeLife life; PetLife pets; RenderTexture roomRt; Vector3 roomPos; float roomYaw; bool hasRoomPos;
        // the movie: a set, the other character, and a camera that moves
        ICharacterBody npc; Transform npcPivot; string npcKey, npcPending; bool inScene;
        Vector3 camA, camB, lookA, lookB; float shotT = 1, shotDur = 6; string shotKey;
        float cutFade; bool actYou = true; float actT;
        string shotKind = "wide", baseKind = "wide"; bool shotYou = true, subjYou = true, narr, reacted; float sinceShot;
        // the movie's post-processing (Resources/Dream/Cinema, made by Dream > Create cinema profile): on in scenes only
        Volume cinema; DepthOfField dof; UniversalAdditionalCameraData camData;
        Transform ext; string extView;   // the establishing shot: the view out of the window, painted big, far above the set
        public float CutFade => cutFade;   // 1 right after a cut, fading to 0: the film view dips to black with it
        public static bool Talking;        // a line is typing itself out: the speaker's mouth moves
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
            try
            {
                camData = cam.GetUniversalAdditionalCameraData();
                if (camData != null) { camData.renderPostProcessing = false; camData.volumeLayerMask = 1 << Layer; camData.renderShadows = false; }
                var prof = Resources.Load<VolumeProfile>("Dream/Cinema");
                if (prof != null)
                {
                    var vg = new GameObject("Cinema"); vg.layer = Layer; vg.transform.SetParent(transform, false);
                    cinema = vg.AddComponent<Volume>(); cinema.isGlobal = true; cinema.priority = 100; cinema.sharedProfile = prof; cinema.weight = 0;
                    cinema.profile.TryGet(out dof);   // .profile is this stage's own copy, so the focus can move per shot
                }
            }
            catch (Exception e) { Debug.LogWarning("[Dream] cinema post-processing unavailable: " + e.Message); cinema = null; dof = null; }
        }
        void Cinema(bool on)
        {
            if (camData != null) { camData.renderPostProcessing = on && cinema != null; camData.antialiasing = on ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.None; }
            if (cinema != null) cinema.weight = on ? 1 : 0;
            cam.allowHDR = on && cinema != null;
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
            if (inScene) { Aim(); PlaceCam(shotT); return; }   // the movie camera owns the shot
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
            if (mode == "scene") { EnterScene(tier ?? "set_cardroom", aspect); return; }
            if (inScene) ExitScene();
            if (mode == "room") { EnterRoom(tier ?? room?.tier ?? "studio", aspect, pet); return; }
            if (framing == "room") ExitRoom();
            if (mode != framing) Frame(bodyH, mode);
        }

        // ------------------------------------------------------------ your place
        void EnterRoom(string tier, float aspect, string pet)
        {
            bool fresh = room == null || room.tier != tier;
            if (fresh)
            {
                // out with the old place: everything called "Home ..." under the stage, not just the one we remember
                for (int i = transform.childCount - 1; i >= 0; i--) { var ch = transform.GetChild(i); if (ch.name.StartsWith("Home ")) { ch.gameObject.SetActive(false); Destroy(ch.gameObject); } }
                if (pets != null) pets.Clear();
                room = HomeRoom.Build(tier, transform, Layer); hasRoomPos = false; pivot.localPosition = Vector3.zero;
                if (room == null) return;
            }
            room.root.SetActive(true);
            if (hasRoomPos) { pivot.localPosition = roomPos; pivot.localRotation = Quaternion.Euler(0, roomYaw, 0); }
            if (life == null) life = gameObject.AddComponent<HomeLife>();
            life.enabled = true;
            if (framing != "room" || fresh) life.Init(this, room);   // a new place (or coming back to this one): start from its spawn spot
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
        // ------------------------------------------------------------ the movie
        // a scene is a set (built like a home from Homes/set_<place>.json, or your own place for "home") with you on
        // the "you" mark and whoever you're talking to on the "them" mark; the camera plays shots between them
        void EnterScene(string tier, float aspect)
        {
            if (framing == "room") ExitRoom();
            bool fresh = room == null || room.tier != tier;
            if (fresh)
            {
                for (int i = transform.childCount - 1; i >= 0; i--) { var ch = transform.GetChild(i); if (ch.name.StartsWith("Home ")) { ch.gameObject.SetActive(false); Destroy(ch.gameObject); } }
                if (pets != null) pets.Clear();
                room = HomeRoom.Build(tier, transform, Layer); hasRoomPos = false; confetti = null;
                if (room == null) return;
            }
            room.root.SetActive(true);
            if (life != null) life.enabled = false;
            if (pets != null) pets.enabled = false;
            Backdrop(room.view);
            if (npcPivot == null) { npcPivot = new GameObject("Them").transform; npcPivot.SetParent(transform, false); }
            var you = room.Mark("you", new Vector3(-0.7f, 0, 1.0f), 90f); var them = room.Mark("them", new Vector3(0.7f, 0, 1.0f), -90f);
            pivot.localPosition = you.pos; pivot.localRotation = Quaternion.Euler(0, you.yaw, 0);
            npcPivot.localPosition = them.pos; npcPivot.localRotation = Quaternion.Euler(0, them.yaw, 0);
            if (room.shadow != null) room.shadow.localPosition = new Vector3(you.pos.x, 0, you.pos.z);
            RoomTexture(aspect > 0 ? aspect : (roomRt != null ? roomRt.width / (float)roomRt.height : 0.85f));
            framing = "scene"; inScene = true; cam.targetTexture = roomRt; cam.farClipPlane = 60; Cinema(true);
            if (shotKey == null) Shot("wide", true, 7f);
            cam.enabled = views.Count > 0;   // the set shows even while the people are still being built
        }
        // a 16:9 painting of the city, 32 m wide, hung far above the set for the opening shot
        void Backdrop(string view)
        {
            view = view ?? "window_city";
            if (ext != null && extView == view) { ext.gameObject.SetActive(true); return; }
            if (ext != null) Destroy(ext.gameObject);
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad); go.name = "Backdrop"; go.layer = Layer; Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false); go.transform.localPosition = new Vector3(0, 40f, 0); go.transform.localRotation = Quaternion.Euler(0, 180f, 0);   // faces +z
            go.transform.localScale = new Vector3(32f, 18f, 1f);
            var mr = go.GetComponent<MeshRenderer>(); mr.sharedMaterial = Mats.Textured(HomeRoom.ViewTex(view), Color.white, 0.6f, null, 1.1f); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ext = go.transform; extView = view;
        }
        void ExitScene()
        {
            inScene = false; shotKey = null; Cinema(false);
            if (ext != null) ext.gameObject.SetActive(false);
            confetti = null;   // it lives in the set
            if (room != null && room.tier.StartsWith("set_")) { room.root.SetActive(false); Destroy(room.root); room = null; }
            else if (room != null) room.root.SetActive(false);
            if (npc != null) npc.Root.SetActive(false);
            pivot.localPosition = Vector3.zero; cam.targetTexture = rt; cam.fieldOfView = 22; framing = "bust";
        }
        // the other character: built once per look, kept between beats
        public void ShowNpc(CharacterSpec spec)
        {
            if (!Bodies.Available || npcPivot == null) return;
            if (spec == null) { if (npc != null) npc.Root.SetActive(false); return; }
            string key = spec.Fingerprint() + "|" + string.Join(",", spec.worn.ConvertAll(w => w.slot + ":" + w.id));
            if (npc != null) npc.Root.SetActive(true);
            if (key == npcKey || key == npcPending) return;
            npcPending = key;
            if (npc == null) { try { npc = Bodies.Create(npcPivot); } catch (Exception e) { Debug.LogException(e); npc = null; return; } }
            npc.Apply(spec, () =>
            {
                npcKey = key; if (npcPending == key) npcPending = null;
                SetLayer(npc.Root, Layer);
                npc.Root.transform.localRotation = Quaternion.identity;
                if (inScene) { Aim(); PlaceCam(shotT); }
            });
        }
        // where the heads are, for the camera (a mark plus 1.6 m until a body is built)
        Vector3 HeadOf(bool you)
        {
            var piv = you ? pivot : npcPivot; var b = you ? (HasBody ? Bone(HumanBodyBones.Head) : null) : (npc != null && npc.Ready ? npc.Bone(HumanBodyBones.Head) : null);
            if (b != null) return transform.InverseTransformPoint(b.position) + Vector3.up * 0.12f;   // the head bone sits at the skull base; the eyes are a hand above
            return piv.localPosition + Vector3.up * 1.66f;
        }
        // shots: "wide" (both of you, a slow push in), "cu" (close on the speaker, off the axis), "ots" (over the
        // listener's shoulder at the speaker). speakerIsYou says who is talking; narration means nobody gestures.
        public void Shot(string kind, bool speakerIsYou, float dur = 6f, bool narration = false)
        {
            string key = kind + (speakerIsYou ? "|you" : "|them");
            if (key == shotKey && shotT < 1) return;
            if (shotKey != null && key != shotKey) { Sound.Play("cut", 0.5f); cutFade = 1f; }   // a cut: a soft whoosh and a dip to black
            shotKey = key; baseKind = shotKind = kind; shotYou = subjYou = speakerIsYou; narr = narration; shotDur = dur; shotT = 0; actYou = speakerIsYou; actT = 0; sinceShot = 0; reacted = false;
            Aim(); PlaceCam(0);
        }
        // coverage: a long line cuts to the listener for a moment (a reaction), then back to the speaker
        void Reaction(bool back)
        {
            shotKind = back ? baseKind : "cu"; subjYou = back ? shotYou : !shotYou; shotT = 0; shotDur = back ? 6f : 4f; sinceShot = 0; if (back) reacted = true;
            Sound.Play("cut", 0.3f); cutFade = 0.8f; Aim(); PlaceCam(0);
        }
        bool Reacting => subjYou != shotYou;
        // where the camera goes for the current shot, from where the heads are right now (called again when a body finishes building)
        public void Aim()
        {
            if (!inScene || npcPivot == null) return;
            Vector3 P = HeadOf(true), Q = HeadOf(false), M = (P + Q) / 2; M.y = pivot.localPosition.y;   // M sits on the floor between you
            Vector3 d = Q - P; d.y = 0; if (d.sqrMagnitude < 1e-4f) d = Vector3.right; d.Normalize();
            Vector3 side = new Vector3(d.z, 0, -d.x); if (side.z < 0) side = -side;   // the camera lives on the open side of the set
            Vector3 S = subjYou ? P : Q, L = subjYou ? Q : P, toL = L - S; toL.y = 0; toL.Normalize();
            switch (shotKind)
            {
                case "ext":
                    {
                        // the city, slowly pushing in and tilting down, cropped tight enough that the painting's edges never show
                        Vector3 c = ext != null ? ext.localPosition : new Vector3(0, 40f, 0); float dist = 9f / Mathf.Tan(15f * Mathf.Deg2Rad) * 0.74f;
                        camA = c + Vector3.forward * dist * 1.05f + Vector3.right * 0.8f + Vector3.up * 0.9f; camB = c + Vector3.forward * dist * 0.93f - Vector3.right * 0.6f + Vector3.up * 0.2f;
                        lookA = c + Vector3.up * 1.2f; lookB = c - Vector3.up * 0.2f; cam.fieldOfView = 30; break;
                    }
                case "cu":
                    {
                        var off = (toL * 1.0f + side * 0.75f).normalized;   // about forty degrees off the eye-line, the set behind them
                        camA = S + off * 1.35f + Vector3.up * 0.04f; camB = S + off * 1.08f + Vector3.up * 0.02f; lookA = lookB = S - Vector3.up * 0.08f; cam.fieldOfView = 30; break;   // eyes on the upper third
                    }
                case "ots":
                    {
                        // behind the listener's shoulder (toL points from the speaker past the listener), looking back at the speaker
                        var sideL = Vector3.Cross(Vector3.up, toL).normalized; if (Vector3.Dot(sideL, side) < 0) sideL = -sideL;
                        camA = L + toL * 0.55f + sideL * 0.40f + Vector3.up * 0.12f; camB = L + toL * 0.45f + sideL * 0.34f + Vector3.up * 0.10f; lookA = lookB = S - Vector3.up * 0.11f; cam.fieldOfView = 34; break;
                    }
                default:
                    {
                        var front = (side * 0.5f + Vector3.forward * 0.85f).normalized;   // from out front, both of you in the frame
                        // eye height, looking a touch down at chest height, so the frame holds the room and not the void above the walls
                        camA = M + front * 3.9f + Vector3.up * 1.55f; camB = M + front * 3.3f + Vector3.up * 1.45f; lookA = lookB = M + Vector3.up * 1.05f; cam.fieldOfView = 40; break;
                    }
            }
            // focus: sharp on the subject, the set going soft behind them; the wide keeps the room readable
            if (dof != null)
            {
                float dist = (lookA - (camA + camB) / 2).magnitude;
                bool close = shotKind == "cu" || shotKind == "ots";
                dof.gaussianStart.value = close ? dist + 0.45f : shotKind == "ext" ? 200f : 7f; dof.gaussianEnd.value = close ? dist + 2.6f : shotKind == "ext" ? 400f : 16f; dof.gaussianMaxRadius.value = close ? 1.1f : 0.6f;
            }
        }
        // a win: paper falling from the ceiling of the set, gold, cream and blue, tumbling
        ParticleSystem[] confetti; static Mesh paper;
        void Confetti(bool on)
        {
            if (on && confetti == null && room != null && inScene)
            {
                if (paper == null)
                {
                    paper = new Mesh { name = "Paper" };
                    paper.vertices = new[] { new Vector3(-0.5f, -0.3f, 0), new Vector3(0.5f, -0.3f, 0), new Vector3(0.5f, 0.3f, 0), new Vector3(-0.5f, 0.3f, 0) };
                    paper.triangles = new[] { 0, 2, 1, 0, 3, 2 }; paper.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back }; paper.RecalculateBounds();
                }
                var cols = new[] { new Color(1f, 0.78f, 0.25f), new Color(0.95f, 0.93f, 0.85f), new Color(0.35f, 0.6f, 1f) };
                confetti = new ParticleSystem[cols.Length];
                for (int i = 0; i < cols.Length; i++)
                {
                    var go = new GameObject("Confetti " + i); go.layer = Layer; go.transform.SetParent(room.root.transform, false); go.transform.localPosition = new Vector3(0, room.H - 0.08f, 0.1f);
                    var ps = go.AddComponent<ParticleSystem>(); ps.Stop();
                    var m = ps.main; m.loop = true; m.duration = 5; m.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 6.5f); m.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.6f);
                    m.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.055f); m.gravityModifier = 0.1f; m.maxParticles = 220; m.simulationSpace = ParticleSystemSimulationSpace.Local;
                    m.startRotation3D = true; m.startRotationX = new ParticleSystem.MinMaxCurve(0, 6.28f); m.startRotationY = new ParticleSystem.MinMaxCurve(0, 6.28f); m.startRotationZ = new ParticleSystem.MinMaxCurve(0, 6.28f);
                    var em = ps.emission; em.rateOverTime = 22;
                    var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(room.W * 0.8f, 0.05f, room.D * 0.6f);
                    var rot = ps.rotationOverLifetime; rot.enabled = true; rot.separateAxes = true;
                    rot.x = new ParticleSystem.MinMaxCurve(-5f, 5f); rot.y = new ParticleSystem.MinMaxCurve(-5f, 5f); rot.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
                    var nz = ps.noise; nz.enabled = true; nz.strength = 0.3f; nz.frequency = 0.5f; nz.scrollSpeed = 0.25f;
                    var r = go.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Mesh; r.mesh = paper; r.alignment = ParticleSystemRenderSpace.World;
                    r.sharedMaterial = Mats.Get(cols[i], 0.1f, 0.55f); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    confetti[i] = ps;
                }
            }
            if (confetti == null) return;
            foreach (var ps in confetti) { if (ps == null) continue; if (on && !ps.isPlaying) ps.Play(); else if (!on && ps.isPlaying) ps.Stop(); }
        }
        // how you carry yourself in a shot: arms up for a win, head down after a bust or a cooler
        public void Pose(string mood)
        {
            this.mood = mood;
            Confetti(mood == "win");
            var an = body?.Root != null ? body.Root.GetComponentInChildren<Animator>() : null; if (an == null) return;
            var g = an.GetComponent<Gestures>(); if (g == null) g = an.gameObject.AddComponent<Gestures>();
            var head = Bone(HumanBodyBones.Head); if (head == null) { g.Clear(); return; }
            Vector3 fwd = pivot.forward, right = pivot.right, up = Vector3.up, H = head.position;
            switch (mood)
            {
                case "win": g.Both(H + up * 0.42f - right * 0.32f + fwd * 0.12f, H + up * 0.45f + right * 0.3f + fwd * 0.1f, 1f); g.Look(H + fwd * 1.5f + up * 0.6f, 0.8f, 0.2f); break;
                case "bust": case "cooler": g.Clear(); g.Look(H + fwd * 0.8f - up * 0.9f, 0.95f, 0.35f); break;
                case "cash": g.Clear(); g.Look(H + fwd * 1.5f - up * 0.1f, 0.6f, 0.1f); break;
                default: g.Clear(); break;
            }
        }
        void PlaceCam(float t)
        {
            float k = t * t * (3 - 2 * t), tm = Time.time;
            // a hand-held breath on the camera: tiny, slow, never still
            var sway = new Vector3(Mathf.PerlinNoise(tm * 0.35f, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, tm * 0.28f) - 0.5f, 0) * 0.016f;
            var look = Vector3.Lerp(lookA, lookB, k) + new Vector3(Mathf.PerlinNoise(tm * 0.22f, 2.1f) - 0.5f, Mathf.PerlinNoise(4.2f, tm * 0.19f) - 0.5f, 0) * 0.02f;
            cam.transform.localPosition = Vector3.Lerp(camA, camB, k) + sway;
            cam.transform.LookAt(transform.TransformPoint(look));
        }
        // who looks at whom, and the speaker's hands: the listener watches the speaker, the speaker talks with a hand
        void Acting()
        {
            actT += Time.deltaTime;
            var gy = GesturesOf(body); var gn = GesturesOf(npc);
            var hy = Bone(HumanBodyBones.Head); var hn = npc?.Bone(HumanBodyBones.Head);
            if (hy == null && hn == null) return;
            Vector3 P = hy != null ? hy.position : transform.TransformPoint(HeadOf(true)), Q = hn != null ? hn.position : transform.TransformPoint(HeadOf(false));
            if (gy != null && string.IsNullOrEmpty(mood)) gy.Look(Q - Vector3.up * 0.02f, 0.75f, 0.15f);
            if (gn != null) gn.Look(P - Vector3.up * 0.02f, 0.75f, 0.15f);
            // faces: the speaker talks while the line types; moods colour your face, the other person reacts a little
            bool talk = Talking || Sound.Speaking;
            body?.Express(mood ?? "", talk && actYou && !narr, actT);
            npc?.Express(mood == "win" ? "cash" : mood == "bust" || mood == "cooler" ? "think" : "", talk && !actYou && !narr, actT);
            // the speaker's right hand lifts and turns while the line plays, then settles
            var g = actYou ? gy : gn; var piv = actYou ? pivot : npcPivot; var head = actYou ? hy : hn;
            if (g != null && head != null && string.IsNullOrEmpty(mood) && !narr)
            {
                float w = Mathf.Clamp01(1.2f - actT * 0.18f) * (0.45f + 0.25f * Mathf.Sin(actT * 1.7f));
                var chest = head.position - Vector3.up * 0.42f;
                var target = chest + piv.forward * (0.28f + 0.06f * Mathf.Sin(actT * 2.3f)) + piv.right * (0.18f + 0.05f * Mathf.Sin(actT * 1.1f + 1f)) + Vector3.up * (0.05f * Mathf.Sin(actT * 2.9f));
                g.Right(target, w);
            }
        }
        string mood;
        Gestures GesturesOf(ICharacterBody b)
        {
            var an = b?.Root != null ? b.Root.GetComponentInChildren<Animator>() : null; if (an == null) return null;
            var g = an.GetComponent<Gestures>(); if (g == null) g = an.gameObject.AddComponent<Gestures>(); return g;
        }
        public void RoomTexture(float aspect)
        {
            int h = 1280, w = Mathf.Clamp(Mathf.RoundToInt(h * aspect), 400, 2560);
            if (roomRt != null && roomRt.width == w && roomRt.height == h) return;
            if (roomRt != null) { if (cam.targetTexture == roomRt) cam.targetTexture = null; roomRt.Release(); }
            roomRt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { name = "Room", antiAliasing = 1, useMipMap = false };
            roomRt.Create();
            if (framing == "room") { cam.targetTexture = roomRt; FrameRoom(); }
            else if (inScene) cam.targetTexture = roomRt;
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
        public void Attach(VisualElement v) { views.Add(v); cam.enabled = HasBody || inScene; }
        public void Detach(VisualElement v) { views.Remove(v); if (views.Count == 0) cam.enabled = false; }
        public void Drag(float dx) { yaw -= dx * 0.45f; spin = -dx * 0.45f; dragging = true; }
        public void Release() { dragging = false; }

        void LateUpdate()
        {
            if (views.Count == 0 || pivot == null) return;
            if (inScene)
            {
                if (shotT < 1) shotT = Mathf.Min(1, shotT + Time.deltaTime / shotDur);
                sinceShot += Time.deltaTime;
                // coverage on a long line: after a few seconds on the speaker, a beat on the listener, then back
                bool both = HasBody && npc != null && npc.Ready && npc.Root.activeSelf;
                if (baseKind == "ext" && sinceShot > 3.6f) { baseKind = shotKind = "wide"; shotT = 0; shotDur = 7f; sinceShot = 0; Sound.Play("cut", 0.5f); cutFade = 1f; Aim(); PlaceCam(0); }
                if (!reacted && !Reacting && !narr && string.IsNullOrEmpty(mood) && both && baseKind != "wide" && baseKind != "ext" && sinceShot > 4.5f) Reaction(false);
                else if (Reacting && sinceShot > 3.2f) Reaction(true);
                PlaceCam(shotT); Acting();
                if (cutFade > 0) cutFade = Mathf.Max(0, cutFade - Time.deltaTime / 0.35f);
                foreach (var v in views) v.MarkDirtyRepaint(); return;
            }
            body?.Express("", false, Time.time);
            if (framing != "room")   // in your place, walking decides which way you face
            {
                if (!dragging) { spin *= 0.92f; yaw += spin; yaw = Mathf.LerpAngle(yaw, -12 + Mathf.Sin(Time.time * 0.6f) * 4, 0.01f); }
                pivot.localRotation = Quaternion.Euler(0, yaw, 0);
            }
            foreach (var v in views) v.MarkDirtyRepaint();
        }

        // ------------------------------------------------------------ the body
        public void Invalidate() { builtKey = null; }   // editor tests: force the next Show to rebuild
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
        // a picture of the stage right now, rendered on demand (works with the editor's game view hidden): Logs/<name>.png
        public void SnapTo(string name, string framing = null)
        {
            if (!string.IsNullOrEmpty(framing) && framing != this.framing) { if (framing == "room") Framing("room", room?.tier, 0.62f); else Framing(framing); }
            var src = Texture; if (src == null) return;
            bool was = cam.enabled; cam.enabled = true; cam.Render(); cam.enabled = was;
            var prev = RenderTexture.active; RenderTexture.active = src;
            var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0); tex.Apply(); RenderTexture.active = prev;
            try { System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "..", "Logs", name + ".png"), tex.EncodeToPNG()); } catch (Exception) { }
            Destroy(tex);
        }
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
        public CharacterSpec NpcSpec; public string ShotKind = "wide", Mood; public bool SpeakerIsYou = true, Narration;   // "scene" views
        bool Room => framing == "room";
        bool SceneMode => framing == "scene";

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
                if (!down || e.pointerId != pid || Room || SceneMode) return;
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
            if (SceneMode) { st.ShowNpc(NpcSpec); st.Shot(ShotKind, SpeakerIsYou, 6f, Narration); st.Pose(Mood); }
            Label hint = null;
            if (!st.HasBody && fallback != null && fallback.parent == null)
            {
                Add(fallback);
                // the flat stand-in is only there while the 3D player builds (slow the first time on a phone): say so
                hint = new Label("Building your player…"); hint.AddToClassList("build-hint"); hint.pickingMode = PickingMode.Ignore; Add(hint);
            }
            Action onChanged = null;
            onChanged = () => { if (fallback != null && fallback.parent == this) Remove(fallback); if (hint != null && hint.parent == this) Remove(hint); if (SceneMode) st.Pose(Mood); };
            st.Changed += onChanged;
            RegisterCallback<DetachFromPanelEvent>(e => st.Changed -= onChanged);
            if (st.HasBody && fallback != null && fallback.parent == this) { Remove(fallback); if (hint != null && hint.parent == this) Remove(hint); }
            // a room view's texture follows the view's shape
            if (Room || SceneMode) RegisterCallback<GeometryChangedEvent>(e => { if (Aspect() > 0 && (st.InRoom || SceneMode)) { st.RoomTexture(Aspect()); SetTexture(st); if (SceneMode) st.Shot(ShotKind, SpeakerIsYou, 6f, Narration); } });
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
