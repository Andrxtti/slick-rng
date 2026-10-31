// The ride's own little studio, far from everything else: a camera that renders only the vehicle into a
// transparent texture, with sunset lighting (hot pink from behind, cyan fill, warm top). Cutscenes lay that
// texture over their painted backgrounds. Wheels turn at whatever speed the cutscene sets.
using System.Collections.Generic;
using UnityEngine;

namespace CFDream.Character
{
    public class RideStage : MonoBehaviour
    {
        public const int Layer = 29;
        static RideStage inst;
        Camera cam; RenderTexture rt; Transform holder; RideRig rig; string rigId;
        readonly Dictionary<string, RideRig> built = new Dictionary<string, RideRig>();
        public float Speed;          // metres per second the ground moves under the wheels
        public float Bank;           // planes: roll in degrees
        public RenderTexture Texture => rt;
        public RideRig Rig => rig;
        public float GroundV { get; private set; }   // where the near side touches the ground, as a 0..1 height in the texture (0 = bottom)
        public float LengthU { get; private set; }   // the ride's length as a fraction of the texture's width

        public static RideStage Get()
        {
            if (inst != null) return inst;
            var go = new GameObject("Ride Stage"); DontDestroyOnLoad(go);
            inst = go.AddComponent<RideStage>(); inst.Setup(); return inst;
        }

        void Setup()
        {
            transform.position = new Vector3(0, -800, 0);
            holder = new GameObject("Holder").transform; holder.SetParent(transform, false);
            var cgo = new GameObject("Ride Camera"); cgo.transform.SetParent(transform, false);
            cam = cgo.AddComponent<Camera>();
            cam.cullingMask = 1 << Layer; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.fieldOfView = 16; cam.nearClipPlane = 0.5f; cam.farClipPlane = 200; cam.allowHDR = false; cam.enabled = false;
            rt = new RenderTexture(1536, 768, 24, RenderTextureFormat.ARGB32) { name = "Ride", antiAliasing = 1, useMipMap = false };
            rt.Create(); cam.targetTexture = rt;
            Light(new Color(1f, 0.36f, 0.62f), 1.6f, new Vector3(18, 120, 0));    // the sunset behind and to the left
            Light(new Color(0.35f, 0.85f, 1f), 0.75f, new Vector3(12, -30, 0));    // cyan fill from the front
            Light(new Color(1f, 0.85f, 0.7f), 0.55f, new Vector3(75, 10, 0));      // warm sky from above
        }
        void Light(Color c, float i, Vector3 euler)
        {
            var go = new GameObject("Light"); go.transform.SetParent(transform, false); go.transform.localEulerAngles = euler;
            var l = go.AddComponent<Light>(); l.type = LightType.Directional; l.color = c; l.intensity = i; l.cullingMask = 1 << Layer; l.shadows = LightShadows.None;
        }

        // put a ride on the stage and frame it side-on, three-quarters from the front, filling `fill` of the width
        public void Show(string id, float fill = 0.82f)
        {
            if (rig != null) rig.root.SetActive(false);
            if (!built.TryGetValue(id, out rig)) { rig = RideModel.Build(id, holder, Layer); built[id] = rig; }
            rig.root.SetActive(true); rigId = id;
            var b = rig.bounds; float L = b.size.x;
            rig.root.transform.localPosition = new Vector3(-b.center.x, rig.plane ? -b.center.y : 0, 0);
            float yaw = rig.plane ? 12f : 18f, pitch = rig.plane ? -6f : 5f;
            float dist = L / fill / (2 * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * cam.aspect) * 1.05f;
            var look = new Vector3(0, rig.plane ? 0 : b.size.y * 0.42f, 0);
            var rot = Quaternion.Euler(pitch, -yaw, 0);
            cam.transform.localPosition = look + rot * new Vector3(0, 0, -dist);
            cam.transform.LookAt(transform.TransformPoint(look));
            // where the ground line lands, for placing the texture on a painted road
            var ground = transform.TransformPoint(new Vector3(0, 0, -b.extents.z));
            GroundV = cam.WorldToViewportPoint(ground).y;
            var nose = cam.WorldToViewportPoint(transform.TransformPoint(new Vector3(b.extents.x, b.size.y * 0.3f, 0)));
            var tail = cam.WorldToViewportPoint(transform.TransformPoint(new Vector3(-b.extents.x, b.size.y * 0.3f, 0)));
            LengthU = Mathf.Abs(nose.x - tail.x);
            Speed = 0; Bank = 0;
        }

        public void Begin() { cam.enabled = true; }
        public void End() { cam.enabled = false; Speed = 0; }

        void LateUpdate()
        {
            if (rig == null || !cam.enabled) return;
            float dt = Time.deltaTime;
            if (!rig.plane)
            {
                float deg = Speed / Mathf.Max(0.1f, rig.wheelR) * Mathf.Rad2Deg * dt;
                foreach (var w in rig.wheels) w.Rotate(0, 0, -deg, Space.Self);
                // a little life in the suspension
                var p = rig.root.transform.localPosition; p.y = Mathf.Sin(Time.time * 9.1f) * 0.006f + Mathf.Sin(Time.time * 3.3f) * 0.004f;
                rig.root.transform.localPosition = p;
            }
            else
            {
                var p = rig.root.transform.localPosition; p.y = -rig.bounds.center.y + Mathf.Sin(Time.time * 1.3f) * 0.08f;
                rig.root.transform.localPosition = p;
                rig.root.transform.localRotation = Quaternion.Euler(Bank, 0, Mathf.Sin(Time.time * 0.9f) * 1.5f + 2f);
            }
        }

        void OnDestroy() { if (rt != null) rt.Release(); if (inst == this) inst = null; }
    }
}
