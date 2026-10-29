using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dream.Film
{
    // Characters built by tools/blender/character.py live in Assets/Dream/FilmCast/<who>/ (generated, editor-only).
    public static partial class Cast
    {
        static partial void Build(string who, GameObject go, bool seated, ref Actor actor)
        {
#if UNITY_EDITOR
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Dream/FilmCast/{who}/{who}.fbx");
            if (fbx == null) return;
            var body = go.AddComponent<Body>();
            body.Setup(who, fbx, seated);
            actor = body;
#endif
        }
    }

    // A realistic character: humanoid model playing the Mixamo acting clips, materials from its manifest, a face that
    // blinks, looks, and moves its mouth with the voice.
    public class Body : Actor
    {
        Animator anim; GameObject model; Face face; Transform hips;
        string current; float nextAt = -1; string nextClip;

        // script acting words -> clips (seated / standing)
        static readonly Dictionary<string, string> Sit = new Dictionary<string, string>
        {
            ["idle"] = "Sitting Idle", ["talk"] = "Sitting Talking", ["lean"] = "Sitting Talking", ["point"] = "Sitting Talking",
            ["nod"] = "Sitting Idle", ["shrug"] = "Sitting Talking", ["think"] = "Sitting Idle", ["sad"] = "Sitting Idle",
            ["laugh"] = "Sitting Clap", ["clap"] = "Sitting Clap", ["fist"] = "Sitting Clap",
        };
        static readonly Dictionary<string, string> Stand = new Dictionary<string, string>
        {
            // Mixamo's "Talking" is a seated clip (head at 1.10 m) and "Pointing" half-crouches (1.28 m): standing talk uses Standing Arguing
            ["idle"] = "Breathing Idle", ["talk"] = "Standing Arguing", ["lean"] = "Standing Arguing", ["point"] = "Standing Arguing", ["nod"] = "Head Nod Yes",
            ["shrug"] = "Shrugging", ["think"] = "Thinking", ["sad"] = "Sad Idle", ["laugh"] = "Agreeing", ["clap"] = "Clapping",
            ["fist"] = "Fist Pump", ["stand"] = "Breathing Idle", ["sit"] = "Sitting Idle",
        };

#if UNITY_EDITOR
        public void Setup(string who, GameObject fbx, bool seated)
        {
            Who = who; Seated = seated;
            model = Instantiate(fbx, transform); model.name = "Model";
            model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity;
            anim = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Dream/FilmAnim/Acting.controller");
            anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Head = anim.GetBoneTransform(HumanBodyBones.Head); hips = anim.GetBoneTransform(HumanBodyBones.Hips);
            Dress(who);
            face = model.AddComponent<Face>(); face.Init(anim, transform, who);
            Play(seated ? "Sitting Idle" : "Breathing Idle", 0f);
            if (seated) StartCoroutine(FitSeat());
        }

        void Dress(string who)
        {
            var dir = $"Assets/Dream/FilmCast/{who}";
            var man = MiniJson.Parse(File.ReadAllText(Path.Combine(dir, "manifest.json"))) as Dictionary<string, object>;
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var part = r.name == who ? "skin" : r.name;
                r.sharedMaterial = Materials.Make(dir, part, man);
                r.updateWhenOffscreen = true;
            }
        }
#endif

        // Mixamo's sitting height is the download character's: move the model so the hips rest on the seat.
        IEnumerator FitSeat()
        {
            yield return null; yield return null;
            if (hips == null) yield break;
            float want = transform.position.y + 0.1f;
            model.transform.position += Vector3.up * (want - hips.position.y);
            model.transform.position -= transform.forward * 0.06f;
        }

        void Play(string state, float fade)
        {
            if (anim == null || state == current) return;
            current = state;
            if (fade <= 0) anim.Play(state, 0, Random.value); else anim.CrossFadeInFixedTime(state, fade);
        }

        public override void Act(string clip, string next, float length)
        {
            var map = Seated ? Sit : Stand;
            if (map.TryGetValue(clip ?? "idle", out var st)) Play(st, 0.35f);
            if (!string.IsNullOrEmpty(next) && map.TryGetValue(next, out var nx)) { nextClip = nx; nextAt = Time.time + length * 0.6f; }
            else { nextClip = Seated ? "Sitting Idle" : "Breathing Idle"; nextAt = Time.time + length + 0.2f; }
            if (clip == "nod") face?.Nod();
        }

        public override void Say(AudioClip clip) { face?.Speak(clip); }
        public override void LookAt(Vector3 target) { base.LookAt(target); face?.Look(target); }

        void Update()
        {
            if (nextAt > 0 && Time.time >= nextAt) { Play(nextClip, 0.5f); nextAt = -1; }
        }
    }
}
