using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Dream.Film
{
    // A camera set-up computed from where the people are: lens (ART_DIRECTION §2: 35mm wide/two, 65mm cu/ots, 85mm
    // ecu), position, aim with the eyes on the upper third, focus on the subject's eyes, a slow handheld drift, and for
    // `push` a dolly-in across the line.
    public class Shot
    {
        public Vector3 from, to, aim, focus; public float mm = 50, fstop = 2.4f, shake = 1f; public float seed;

        public static Shot Make(string kind, string subject, string speaker, GameObject set, Dictionary<string, Actor> actors, Scene sc)
        {
            Actor S = null, O = null;
            if (!string.IsNullOrEmpty(subject)) actors.TryGetValue(subject, out S);
            if (S == null) actors.TryGetValue(speaker, out S);
            foreach (var kv in actors) { if (S == null) S = kv.Value; else if (kv.Value != S && O == null) O = kv.Value; }
            if (S != null && S.Who != "you" && actors.TryGetValue("you", out var y)) O = y;   // in a crowd, people talk to you
            var s = new Shot { seed = Random.value * 100f };
            Vector3 up = Vector3.up;
            Vector3 mid = S != null && O != null ? (S.Eyes + O.Eyes) * 0.5f : S != null ? S.Eyes : set.transform.position + up * 1.5f;
            Vector3 front = S != null ? Flat(O != null ? (S.Forward + O.Forward) : S.Forward) : Vector3.forward;
            Vector3 toO = S != null && O != null ? Flat(O.Eyes - S.Eyes) : Vector3.Cross(up, front);

            switch (kind)
            {
                case "cu": case "push": case "ecu":
                {
                    bool e = kind == "ecu";
                    s.mm = e ? 85 : 65; s.fstop = e ? 1.8f : 2.0f;
                    float frame = e ? 0.24f : 0.48f;                       // metres of subject the frame holds, top to bottom
                    float d = frame * 0.5f / Mathf.Tan(VFov(s.mm) * 0.5f * Mathf.Deg2Rad);
                    var dir = (Flat(S.Forward) * 0.75f + toO * (e ? 0.6f : 0.7f)).normalized;   // in front of them, near the eyeline of the person they talk to
                    s.focus = S.Eyes; s.aim = S.Eyes - up * frame * (1f / 2f - 1f / 3f);   // eyes on the upper third
                    s.to = S.Eyes + dir * d + up * 0.02f;
                    s.from = kind == "push" ? S.Eyes + dir * d * 1.45f + up * 0.03f : s.to;
                    s.shake = kind == "push" ? 0.4f : 0.8f;
                    break;
                }
                case "ots":
                {
                    s.mm = 50; s.fstop = 2.2f;
                    var back = O != null ? (O.Eyes - S.Eyes).normalized : -front;
                    s.to = s.from = (O != null ? O.Eyes : S.Eyes + front) + back * 0.55f + front * 0.32f + up * 0.03f;
                    s.focus = S.Eyes; s.aim = S.Eyes - up * 0.06f - back * 0.0f;
                    break;
                }
                case "wide":
                {
                    var m = Sets.Find(set, "CAM_wide");
                    s.mm = 32; s.fstop = 4f; s.shake = 0.5f;
                    s.to = s.from = m != null ? m.position : mid + front * 4f + up * 0.2f;
                    s.aim = mid - up * 0.15f; s.focus = mid;
                    break;
                }
                case "ext":
                {
                    var m = Sets.Find(set, "CAM_ext");
                    s.mm = 30; s.fstop = 5.6f; s.shake = 0.3f;
                    s.to = m != null ? m.position : mid + front * 10f;
                    s.from = s.to - Flat(mid - s.to) * 0.6f;              // a slow creep toward the window
                    s.aim = mid + up * 0.55f; s.focus = mid;   // the window, with the sign above it in frame
                    break;
                }
                case "cam":   // a vignette's own camera: CAM_<subject> looking at AIM_<subject>, a slow push in
                {
                    var c = Sets.Find(set, "CAM_" + subject); var a = Sets.Find(set, "AIM_" + subject);
                    if (c == null || a == null) goto default;
                    s.mm = 50; s.fstop = 2.0f; s.shake = 0.5f;
                    s.from = c.position; s.to = Vector3.Lerp(c.position, a.position, 0.18f);
                    s.aim = a.position; s.focus = a.position;
                    return s;
                }
                case "insert":
                {
                    var p = FindProp(set, subject) ?? FindProp(set, "envelope");
                    var pr = p != null ? p.GetComponentInChildren<Renderer>() : null;
                    var at = pr != null ? pr.bounds.center : p != null ? p.position : mid - up * 0.5f;
                    if (O != null) at = Vector3.Lerp(at, new Vector3(O.transform.position.x, at.y, at.z), 0.18f);   // a little toward where it is going
                    s.mm = 40; s.fstop = 2.8f; s.shake = 0.6f;
                    s.to = s.from = at + up * 0.55f - front * 0.32f;
                    s.aim = at; s.focus = at;
                    break;
                }
                default:   // two
                {
                    s.mm = 35; s.fstop = 2.8f; s.shake = 0.6f;
                    s.to = s.from = mid + front * 2.3f + up * 0.12f;
                    s.aim = mid - up * 0.12f; s.focus = mid;
                    break;
                }
            }
            if (S != null && kind != "ext" && kind != "wide") s.Clear(S.Eyes, kind == "ots" ? ~(1 << Actor.Layer) : ~0);
            return s;
        }

        // If something solid sits between the camera and the face, swing around the subject (keeping distance and
        // height) until the line of sight is clear.
        void Clear(Vector3 subject, int mask)
        {
            if (!Blocked(subject, to, mask)) return;
            for (int step = 1; step <= 8; step++)
                foreach (var sign in new[] { 1f, -1f })
                {
                    var rot = Quaternion.AngleAxis(sign * step * 12f, Vector3.up);
                    Vector3 nt = subject + rot * (to - subject), nf = subject + rot * (from - subject);
                    if (Blocked(subject, nt, mask)) continue;
                    to = nt; from = nf;
                    return;
                }
        }

        static bool Blocked(Vector3 subject, Vector3 cam, int mask)
        {
            var d = cam - subject; float len = d.magnitude;
            return Physics.SphereCast(subject + d / len * 0.25f, 0.12f, d / len, out _, Mathf.Max(0, len - 0.25f), mask);
        }

        static Transform FindProp(GameObject set, string what)
        {
            if (string.IsNullOrEmpty(what)) return null;
            foreach (var t in set.GetComponentsInChildren<Transform>()) if (t.name.ToLowerInvariant() == "prop_" + what.ToLowerInvariant()) return t;
            return null;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude < 1e-6f ? Vector3.forward : v.normalized; }
        public static float VFov(float mm) => 2f * Mathf.Atan(20.25f * 0.5f / mm) * Mathf.Rad2Deg;

        public void Apply(Camera cam, DepthOfField dof, float t)
        {
            float k = t * t * (3f - 2f * t);
            var pos = Vector3.Lerp(from, to, k);
            float time = Time.time + seed;
            var drift = new Vector3(Mathf.PerlinNoise(time * 0.31f, 1.7f) - 0.5f, Mathf.PerlinNoise(2.3f, time * 0.27f) - 0.5f, 0) * shake;
            cam.transform.position = pos + cam.transform.right * drift.x * 0.006f + cam.transform.up * drift.y * 0.004f;
            cam.transform.rotation = Quaternion.LookRotation(aim - pos) * Quaternion.Euler(drift.y * 0.35f, drift.x * 0.45f, (Mathf.PerlinNoise(time * 0.2f, 5.1f) - 0.5f) * shake * 0.4f);
            cam.focalLength = mm;
            if (dof != null)
            {
                dof.focusDistance.value = Vector3.Distance(cam.transform.position, focus);
                dof.focalLength.value = mm; dof.aperture.value = fstop;
            }
        }
    }
}
