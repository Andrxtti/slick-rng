// Living in your place: walking where you tap (around the furniture), and using things: the laptop (your hub),
// the bed, the closet, the TV (late-night poker), a beer, the bong, feeding the fish. The UI listens for what
// happens (Said, Done, Puff) and shows captions and screens.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CFDream.Character
{
    public class HomeLife : MonoBehaviour
    {
        public static event Action<string, string> Said;   // who (a use id, or "me") and the line
        public static event Action<string> Done;           // laptop, bed, closet: the UI takes it from here
        public static event Action Puff;

        CharacterStage st; HomeRoom room;
        List<Vector3> path; int pi; float? faceAt; Action arrived; Coroutine acting;
        const float Speed = 1.25f;
        float yaw;
        bool tvOn; float tvT, sayT; int tvF, tvLine;
        readonly List<Vector3> fishGo = new List<Vector3>(); readonly List<Transform> flakes = new List<Transform>(); float feedT;
        public bool Busy => acting != null;
        public HomeRoom Room => room;

        static readonly string[] TvLines = {
            "\"He's got the nuts and he knows it.\"", "\"Rivered! You can't make this up.\"", "\"A bold shove from the button...\"",
            "\"The chip leader is applying the pressure.\"", "\"That's the hand of the night, folks.\"", "\"He tanks... and folds the winner.\"" };
        static readonly string[] BeerLines = { "Ahh. Liquid courage.", "Cold one. Earned it.", "That hit the spot.", "One more and I'm calling everything." };

        public void Init(CharacterStage stage, HomeRoom r)
        {
            st = stage; room = r;
            if (st.Pivot.localPosition == Vector3.zero || !room.Free(st.Pivot.localPosition)) { st.Pivot.localPosition = room.spawn; yaw = room.spawnFace; }
            else yaw = st.Pivot.localEulerAngles.y;
            st.Pivot.localRotation = Quaternion.Euler(0, yaw, 0);
            path = null; arrived = null; faceAt = null;
            fishGo.Clear(); foreach (var f in room.fish) { f.localPosition = Rand(); fishGo.Add(Rand()); }
            SetTv(false);
        }

        // ------------------------------------------------------------ walking
        public void WalkTo(Vector3 to, Action onArrive = null, float? face = null)
        {
            if (room == null) return;
            if (acting != null) { StopCoroutine(acting); acting = null; Release(); }
            path = room.Path(st.Pivot.localPosition, to); pi = 0; arrived = onArrive; faceAt = face;
        }

        public void Use(string id)
        {
            var u = room?.Use(id); if (u == null) return;
            WalkTo(u.spot, () => Act(u), u.face);
        }

        void Update()
        {
            if (room == null || st == null) return;
            var an = Anim();
            float dt = Time.deltaTime; bool moving = false;
            if (path != null && pi < path.Count)
            {
                var p = st.Pivot.localPosition; var target = path[pi]; var d = target - p; d.y = 0;
                float step = Speed * dt;
                if (d.magnitude <= step) { st.Pivot.localPosition = target; pi++; }
                else { st.Pivot.localPosition = p + d.normalized * step; moving = true; }
                if (d.sqrMagnitude > 1e-4f) yaw = Mathf.MoveTowardsAngle(yaw, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 540 * dt);
                if (pi >= path.Count) { path = null; }
            }
            else if (faceAt.HasValue)
            {
                yaw = Mathf.MoveTowardsAngle(yaw, faceAt.Value, 360 * dt);
                if (Mathf.Abs(Mathf.DeltaAngle(yaw, faceAt.Value)) < 2) { faceAt = null; var cb = arrived; arrived = null; cb?.Invoke(); }
            }
            else if (arrived != null) { var cb = arrived; arrived = null; cb.Invoke(); }
            st.Pivot.localRotation = Quaternion.Euler(0, yaw, 0);
            if (an != null) an.SetFloat("Speed", moving ? 1f : 0f);
            if (room.shadow != null) room.shadow.localPosition = new Vector3(st.Pivot.localPosition.x, 0, st.Pivot.localPosition.z);
            Fish(dt); Tv(dt);
        }

        Animator Anim() { var r = st.BodyRoot; return r != null ? r.GetComponentInChildren<Animator>() : null; }
        Gestures Hands()
        {
            var an = Anim(); if (an == null) return null;
            var g = an.GetComponent<Gestures>(); if (g == null) g = an.gameObject.AddComponent<Gestures>();   // not ??: Unity's missing components aren't plain null
            return g;
        }
        void Release() { var g = Hands(); g?.Clear(); if (room.can != null && room.can.parent != room.root.transform) PutCanBack(); }

        // ------------------------------------------------------------ using things
        void Act(HomeUse u)
        {
            switch (u.id)
            {
                case "laptop": acting = StartCoroutine(Laptop(u)); break;
                case "bed": Done?.Invoke("bed"); break;
                case "closet": acting = StartCoroutine(Reach(u, 1.2f, () => Done?.Invoke("closet"))); break;
                case "tv": acting = StartCoroutine(Reach(u, 0.9f, () => { SetTv(!tvOn); if (tvOn) Said?.Invoke("tv", TvLines[tvLine++ % TvLines.Length]); })); break;
                case "beer": acting = StartCoroutine(Beer(u)); break;
                case "bong": acting = StartCoroutine(Bong(u)); break;
                case "fish": acting = StartCoroutine(Feed(u)); break;
            }
        }

        IEnumerator Laptop(HomeUse u)
        {
            var g = Hands(); var kb = u.tr.position + u.tr.rotation * new Vector3(0, 0.03f, 0.04f);
            g?.Both(kb + u.tr.right * -0.1f, kb + u.tr.right * 0.1f, 0.8f); g?.Look(u.tr.position + Vector3.up * 0.15f, 0.8f, 0.2f);
            yield return new WaitForSeconds(0.7f);
            g?.Clear(); acting = null; Done?.Invoke("laptop");
        }

        IEnumerator Reach(HomeUse u, float height, Action then)
        {
            var g = Hands(); var target = u.tr.position + Vector3.up * height * 0.5f + (st.Pivot.position - u.tr.position).normalized * 0.25f;
            g?.Right(target, 0.7f); g?.Look(u.Anchor, 0.7f, 0.1f);
            yield return new WaitForSeconds(0.45f);
            then?.Invoke();
            yield return new WaitForSeconds(0.35f);
            g?.Clear(); acting = null;
        }

        IEnumerator Beer(HomeUse u)
        {
            var g = Hands(); var hand = st.Bone(HumanBodyBones.RightHand); var head = st.Bone(HumanBodyBones.Head);
            if (room.can == null || hand == null || head == null) { acting = null; yield break; }
            g?.Right(room.can.position + Vector3.up * 0.06f, 1f); g?.Look(room.can.position, 0.8f, 0.3f);
            yield return new WaitForSeconds(0.45f);
            room.can.SetParent(hand, true); room.can.localPosition = new Vector3(0, -0.04f, 0.06f); room.can.localRotation = Quaternion.Euler(90, 0, 0);
            Said?.Invoke("me", "Glug glug glug...");
            float t = 0;
            while (t < 1.6f)
            {
                t += Time.deltaTime;
                var mouth = head.position + st.Pivot.forward * 0.1f - Vector3.up * 0.05f;
                g?.Right(mouth, 1f); g?.Look(head.position + st.Pivot.forward * 0.4f + Vector3.up * 0.5f, 0.9f, 0.15f);
                yield return null;
            }
            g?.Clear();
            yield return new WaitForSeconds(0.3f);
            Said?.Invoke("me", BeerLines[UnityEngine.Random.Range(0, BeerLines.Length)]);
            PutCanBack(); room.can.gameObject.SetActive(false);
            acting = null;
            yield return new WaitForSeconds(2.5f);
            if (room != null && room.can != null) room.can.gameObject.SetActive(true);   // there's always another in the fridge
        }
        void PutCanBack() { if (room.can == null) return; room.can.SetParent(room.root.transform, false); room.can.localPosition = room.canHome; room.can.localRotation = Quaternion.identity; }

        IEnumerator Bong(HomeUse u)
        {
            var g = Hands(); if (room.bong == null) { acting = null; yield break; }
            var top = room.bong.position + Vector3.up * 0.42f; var bowl = room.bong.position + room.bong.right * 0.11f + Vector3.up * 0.16f;
            g?.Both(top + Vector3.up * 0.02f, bowl, 1f); g?.Look(top, 1f, 0.6f);
            yield return new WaitForSeconds(2.2f);
            g?.Clear();
            yield return new WaitForSeconds(0.25f);
            Puff?.Invoke(); Said?.Invoke("me", "...duuude.");
            acting = null;
        }

        IEnumerator Feed(HomeUse u)
        {
            var g = Hands(); if (room.tank == null) { acting = null; yield break; }
            var lid = room.tank.TransformPoint(new Vector3(0, room.tankMax.y + 0.12f, 0));
            g?.Right(lid, 0.9f); g?.Look(room.tank.TransformPoint((room.tankMin + room.tankMax) / 2), 0.9f, 0.3f);
            yield return new WaitForSeconds(0.5f);
            // a pinch of flakes drifting down; the fish come up for them
            for (int i = 0; i < 9; i++)
            {
                var f = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(f.GetComponent<Collider>());
                f.layer = CharacterStage.Layer; f.transform.SetParent(room.tank, false); f.transform.localScale = Vector3.one * 0.012f;
                f.transform.localPosition = new Vector3(UnityEngine.Random.Range(room.tankMin.x * 0.5f, room.tankMax.x * 0.5f), room.tankMax.y, UnityEngine.Random.Range(room.tankMin.z * 0.4f, room.tankMax.z * 0.4f));
                f.GetComponent<Renderer>().sharedMaterial = Mats.Get(new Color(0.85f, 0.55f, 0.3f), 0f, 0.3f);
                flakes.Add(f.transform);
            }
            feedT = 5f;
            g?.Clear();
            yield return new WaitForSeconds(0.6f);
            Said?.Invoke("fish", room.fish.Count > 3 ? "Feeding frenzy." : "Bubbles approves.");
            acting = null;
        }

        // ------------------------------------------------------------ the fish and the TV
        Vector3 Rand() => new Vector3(UnityEngine.Random.Range(room.tankMin.x, room.tankMax.x), UnityEngine.Random.Range(room.tankMin.y, room.tankMax.y), UnityEngine.Random.Range(room.tankMin.z, room.tankMax.z));
        void Fish(float dt)
        {
            if (room.fish.Count == 0) return;
            if (feedT > 0)
            {
                feedT -= dt;
                for (int i = flakes.Count - 1; i >= 0; i--)
                {
                    var f = flakes[i]; var p = f.localPosition; p.y = Mathf.Max(room.tankMin.y, p.y - dt * 0.03f); f.localPosition = p;
                    if (feedT <= 0) { Destroy(f.gameObject); flakes.RemoveAt(i); }
                }
            }
            for (int i = 0; i < room.fish.Count; i++)
            {
                var fsh = room.fish[i]; if (fsh == null) continue;
                var goal = feedT > 0 && flakes.Count > 0 ? flakes[i % flakes.Count].localPosition : fishGo[i];
                var d = goal - fsh.localPosition;
                float sp = feedT > 0 ? 0.16f : 0.06f;
                if (d.magnitude < 0.03f) { if (feedT <= 0) fishGo[i] = Rand(); continue; }
                fsh.localPosition += d.normalized * Mathf.Min(d.magnitude, sp * dt);
                var dir = new Vector3(d.x, d.y * 0.3f, d.z); if (dir.sqrMagnitude > 1e-5f)
                    fsh.localRotation = Quaternion.Slerp(fsh.localRotation, Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(0, -90, 0), dt * 4);
            }
        }

        void SetTv(bool on)
        {
            tvOn = on; if (room.tvScreen == null) return;
            room.tvScreen.sharedMaterial = on && room.tvOn != null ? room.tvOn[0] : room.tvOff; tvT = 0; tvF = 0; sayT = 0;
        }
        void Tv(float dt)
        {
            if (!tvOn || room.tvScreen == null || room.tvOn == null) return;
            tvT += dt; sayT += dt;
            if (tvT > 2.2f) { tvT = 0; tvF = (tvF + 1) % room.tvOn.Length; room.tvScreen.sharedMaterial = room.tvOn[tvF]; }
            if (sayT > 4.5f) { sayT = 0; Said?.Invoke("tv", TvLines[tvLine++ % TvLines.Length]); }
        }

        void OnDisable() { if (acting != null) { StopCoroutine(acting); acting = null; } }
    }
}
