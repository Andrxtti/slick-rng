using System.Collections.Generic;
using UnityEngine;

namespace Dream.Film
{
    // The life in a face, on top of the body clips: blinks every 3-6 s, the head and eyes turning to whoever matters,
    // small involuntary head drift, and the mouth driven by the voice (MakeHuman expression units as blend shapes).
    // Lip-sync reads the clip's samples once: loudness opens the jaw, brightness (zero crossings) spreads the lips for
    // E/S sounds, dull voiced sound rounds them for O/U, and a quiet gap between loud ones closes them for M/B/P.
    public class Face : MonoBehaviour
    {
        readonly List<SkinnedMeshRenderer> smrs = new List<SkinnedMeshRenderer>();
        Animator anim; Transform body, head, neck; Quaternion headFix = Quaternion.identity;
        Vector3 look; bool hasLook; float lookW;
        float nextBlink, blinkT = -1;
        AudioSource src; float[] env, bright; float rate = 60f; float speakStart; bool speaking;
        float open, spread, round; float nodT = -1; float rest;   // rest: the resting expression (Slick's half smile)

        public void Init(Animator a, Transform bodyRoot, string who)
        {
            anim = a; body = bodyRoot;
            head = a.GetBoneTransform(HumanBodyBones.Head); neck = a.GetBoneTransform(HumanBodyBones.Neck);
            foreach (var r in GetComponentsInChildren<SkinnedMeshRenderer>()) if (r.sharedMesh != null && r.sharedMesh.blendShapeCount > 0) smrs.Add(r);
            src = gameObject.AddComponent<AudioSource>(); src.spatialBlend = 0f; src.playOnAwake = false;
            nextBlink = Time.time + Random.Range(1f, 3f);
            rest = who == "slick" ? 0.14f : who == "mustich" ? 0.22f : 0.05f;
        }

        void Set(string shape, float w)
        {
            foreach (var r in smrs)
            {
                int i = r.sharedMesh.GetBlendShapeIndex(shape);
                if (i >= 0) r.SetBlendShapeWeight(i, Mathf.Clamp01(w) * 100f);
            }
        }

        public void Look(Vector3 target) { look = target; hasLook = true; }
        public void Nod() { nodT = 0; }

        public void Speak(AudioClip clip)
        {
            if (clip == null) return;
            var data = new float[clip.samples * clip.channels];
            clip.GetData(data, 0);
            int n = Mathf.CeilToInt(clip.length * rate), step = Mathf.Max(1, clip.frequency * clip.channels / (int)rate);
            env = new float[n]; bright = new float[n];
            for (int f = 0; f < n; f++)
            {
                double sum = 0; int zc = 0, a = f * step, b = Mathf.Min(data.Length, a + step * 2);
                for (int k = a; k < b; k++) { sum += data[k] * data[k]; if (k > a && (data[k] >= 0) != (data[k - 1] >= 0)) zc++; }
                env[f] = b > a ? Mathf.Sqrt((float)(sum / (b - a))) : 0; bright[f] = b > a ? zc / (float)(b - a) : 0;
            }
            float peak = 0.0001f; foreach (var x in env) peak = Mathf.Max(peak, x);
            for (int f = 0; f < n; f++) env[f] /= peak;
            src.clip = clip; src.Play(); speakStart = Time.time; speaking = true;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime, t = Time.time;
            // blink
            if (blinkT < 0 && t >= nextBlink) { blinkT = 0; nextBlink = t + Random.Range(3f, 6f); }
            float blink = 0;
            if (blinkT >= 0) { blinkT += dt; float p = blinkT / 0.16f; blink = p < 0.5f ? p * 2 : Mathf.Max(0, 2 - p * 2); if (p >= 1) blinkT = -1; }
            Set("eye-left-closure", blink); Set("eye-right-closure", blink);

            // mouth
            float o = 0, s = 0, r = 0;
            if (speaking && env != null)
            {
                int f = Mathf.FloorToInt((t - speakStart) * rate);
                if (f >= env.Length) speaking = false;
                else
                {
                    float e = env[f], br = bright[f];
                    float prev = f > 2 ? env[f - 3] : 0;
                    o = Mathf.Clamp01((e - 0.12f) * 0.75f);
                    s = Mathf.Clamp01((br - 0.12f) * 4f) * Mathf.Clamp01(e * 2f);
                    r = Mathf.Clamp01((0.1f - br) * 8f) * Mathf.Clamp01(e * 1.5f);
                    if (e < 0.08f && prev > 0.3f) o = 0;   // a closure between syllables
                }
            }
            open = Mathf.Lerp(open, o, 1 - Mathf.Exp(-dt * (o > open ? 28f : 16f)));
            spread = Mathf.Lerp(spread, s, 1 - Mathf.Exp(-dt * 18f));
            round = Mathf.Lerp(round, r, 1 - Mathf.Exp(-dt * 14f));
            Set("mouth-open", open * 0.3f); Set("mouth-retraction", spread * 0.35f); Set("mouth-pursing", round * 0.35f);
            Set("mouth-corner-puller", rest * (1 - open));
            float brow = speaking ? Mathf.Clamp01(open * 1.4f - 0.25f) * 0.35f : 0;
            Set("eyebrows-left-inner-up", brow); Set("eyebrows-right-inner-up", brow);

            // head: turn toward the look target (on top of the clip), a slow drift, the nod
            if (head == null) return;
            lookW = Mathf.MoveTowards(lookW, hasLook ? 1 : 0, dt * 1.5f);
            if (lookW > 0)
            {
                var want = Quaternion.LookRotation((look - head.position).normalized, Vector3.up);
                var have = Quaternion.LookRotation(Flat(body.forward), Vector3.up);
                var delta = want * Quaternion.Inverse(have);
                delta.ToAngleAxis(out var ang, out var axis); if (ang > 180) ang -= 360;
                ang = Mathf.Clamp(ang, -55, 55) * lookW;
                var turn = Quaternion.AngleAxis(ang, axis);
                if (neck != null) neck.rotation = Quaternion.Slerp(Quaternion.identity, turn, 0.4f) * neck.rotation;
                head.rotation = Quaternion.Slerp(Quaternion.identity, turn, 0.6f) * head.rotation;
            }
            float dx = (Mathf.PerlinNoise(t * 0.23f, 3.3f) - 0.5f) * 3f, dy = (Mathf.PerlinNoise(7.1f, t * 0.19f) - 0.5f) * 4f;
            if (nodT >= 0) { nodT += dt; dx += Mathf.Sin(nodT * 9f) * 7f * Mathf.Clamp01(1 - nodT / 1.1f); if (nodT > 1.1f) nodT = -1; }
            head.rotation = Quaternion.AngleAxis(dy, Vector3.up) * Quaternion.AngleAxis(dx, body.right) * head.rotation;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude < 1e-6f ? Vector3.forward : v.normalized; }
    }
}
