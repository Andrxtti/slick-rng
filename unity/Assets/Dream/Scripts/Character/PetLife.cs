// Your pet, living in your place. The dog follows you around, sits when you stop and flops down if you stay put;
// the cat lives on the bed and now and then hops down to wander or stare at the fish. Tap either one to give it
// some love. Models come from Resources/Dream/Pets (unity/tools/art/pets.py): body, head, tail, tongue and four
// legs, each turning where it joins, animated here (trot, sit, lie down, hop, wag).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CFDream.Character
{
    public class PetInfo
    {
        public string id, name, kind; public float leg, len, height; public Dictionary<string, Color> colors = new Dictionary<string, Color>();
        public bool Cat => kind == "cat";
        static Dictionary<string, object> all;
        public static PetInfo Get(string id)
        {
            if (all == null) { var ta = Resources.Load<TextAsset>("Dream/Pets/pets"); all = ta != null ? Json.Parse(ta.text) as Dictionary<string, object> : new Dictionary<string, object>(); }
            var o = J.O(all, id ?? ""); if (o == null) return null;
            var p = new PetInfo { id = id, name = J.S(o, "name", id), kind = J.S(o, "kind", "dog"), leg = (float)J.N(o, "leg", 0.3), len = (float)J.N(o, "len", 0.4), height = (float)J.N(o, "height", 0.6) };
            var c = J.O(o, "colors"); if (c != null) foreach (var kv in c) if (kv.Value is string h && ColorUtility.TryParseHtmlString(h, out var col)) p.colors[kv.Key] = col;
            return p;
        }
        public Material Mat(string n)
        {
            var c = colors.TryGetValue(n, out var v) ? v : Color.gray;
            switch (n)
            {
                case "eye": return Mats.Get(c, 0f, 0.92f);
                case "iris": return Mats.Get(c, 0f, 0.9f, 0.15f);
                case "hi": return Mats.Get(Color.white, 0f, 0.5f, 1.2f);
                case "nose": return Mats.Get(c, 0f, 0.75f);
                case "tongue": return Mats.Get(c, 0f, 0.65f);
                case "collar": return Mats.Plastic(c, 0.5f);
                case "tag": return Mats.Metal(c);
                case "inner": return Mats.Get(c, 0f, 0.3f);
                default: return Mats.Get(c, 0f, 0.22f);   // fur
            }
        }
    }

    public class PetRig
    {
        public Transform root, body, head, tail, tongue; public Transform[] legs = new Transform[4];   // fl, fr, bl, br
        public Vector3 bodyRest, headRest, tailRest; public Quaternion headRot0, tailRot0;
        public static PetRig Build(PetInfo info, Transform parent, int layer)
        {
            var go = DrmMesh.Spawn("Dream/Pets/" + info.id, parent, layer, info.Mat, "Pet " + info.id); if (go == null) return null;
            var r = new PetRig { root = go.transform };
            Transform P(string n) => go.transform.Find(n);
            r.body = P("body"); r.head = P("head"); r.tail = P("tail"); r.tongue = P("tongue");
            string[] ln = { "leg_fl", "leg_fr", "leg_bl", "leg_br" }; for (int i = 0; i < 4; i++) r.legs[i] = P(ln[i]);
            if (r.body == null) return r;
            // everything rides on the body
            foreach (var t in new[] { r.head, r.tail, r.legs[0], r.legs[1], r.legs[2], r.legs[3] }) if (t != null) t.SetParent(r.body, true);
            if (r.tongue != null && r.head != null) r.tongue.SetParent(r.head, true);
            r.bodyRest = r.body.localPosition; r.headRest = r.head != null ? r.head.localPosition : Vector3.zero; r.tailRest = r.tail != null ? r.tail.localPosition : Vector3.zero;
            r.headRot0 = r.head != null ? r.head.localRotation : Quaternion.identity; r.tailRot0 = r.tail != null ? r.tail.localRotation : Quaternion.identity;
            if (r.tongue != null) r.tongue.gameObject.SetActive(false);
            return r;
        }
    }

    public class PetLife : MonoBehaviour
    {
        CharacterStage st; HomeRoom room; PetRig rig; PetInfo info; HomeUse use;
        public string Id { get; private set; }
        public PetInfo Info => info;

        // where it is (room space), where it's going
        Vector3 pos; float yaw; List<Vector3> path; int pi; float speed;
        // pose weights and the gait
        float sitW, lieW, walkW, phase, sitT, lieT, wag, wagAmp, wagT, look, happyT, bounce, sniffT;
        bool onBed, held, hopping; Vector3 hopFrom, hopTo; float hopT; Action hopDone;
        float idleT, thinkT, repath; Vector3 lastPlayer; string doing = "home";
        Vector3 bedSpot; float bedYaw; bool hasBed;

        public void Set(CharacterStage stage, HomeRoom r, string petId)
        {
            st = stage;
            if (r == room && petId == Id && (rig != null || string.IsNullOrEmpty(petId) || petId == "none")) return;
            Clear(); room = r; Id = petId;
            if (room == null || string.IsNullOrEmpty(petId) || petId == "none") return;
            info = PetInfo.Get(petId); if (info == null) return;
            rig = PetRig.Build(info, room.root.transform, CharacterStage.Layer); if (rig == null || rig.body == null) { Clear(); return; }
            var col = rig.root.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(0, info.height * 0.5f, 0); col.size = new Vector3(0.36f, info.height, info.len + 0.4f);
            use = new HomeUse { id = "pet", label = info.name, tr = rig.root, top = info.height + 0.12f };
            room.uses.Add(use);
            FindBed();
            // where it starts: the cat on the bed, the dog beside you
            if (info.Cat && hasBed) { pos = bedSpot; yaw = bedYaw; onBed = true; lieW = 1; doing = "home"; }
            else
            {
                var p = st.Pivot.localPosition; var near = Free(p + Quaternion.Euler(0, st.Pivot.localEulerAngles.y, 0) * new Vector3(0.7f, 0, 0.3f));
                pos = near; yaw = st.Pivot.localEulerAngles.y + 180; sitW = 1; doing = "near";
            }
            thinkT = UnityEngine.Random.Range(12f, 25f); lastPlayer = st.Pivot.localPosition;
            Place(); Pose(0);
        }

        public void Clear()
        {
            if (room != null && use != null) room.uses.Remove(use);
            if (rig != null && rig.root != null) Destroy(rig.root.gameObject);
            rig = null; use = null; info = null; Id = null; path = null; held = false; hopping = false; onBed = false; happyT = 0;
        }

        void FindBed()
        {
            hasBed = false; var bed = room.Use("bed"); if (bed?.tr == null) return;
            var bc = bed.tr.GetComponent<BoxCollider>(); Vector3 c = bc != null ? bc.center : Vector3.zero, s = bc != null ? bc.size : new Vector3(1.4f, 1, 2);
            // on the blanket near the foot of the bed, off to one side
            var local = new Vector3(s.x * 0.2f, 0.6f, c.z + s.z * 0.22f);
            bedSpot = room.root.transform.InverseTransformPoint(bed.tr.TransformPoint(local));
            bedYaw = bed.tr.localEulerAngles.y + 200; hasBed = true;
        }

        Vector3 Free(Vector3 p)
        {
            var pts = room.Path(p, p); return pts.Count > 0 ? pts[pts.Count - 1] : p;
        }

        // ------------------------------------------------------------ being petted (HomeLife walks you over first)
        public Vector3 PetSpot(Vector3 from, out float face)
        {
            var me = new Vector3(pos.x, 0, pos.z); var d = new Vector3(from.x - me.x, 0, from.z - me.z);
            if (d.sqrMagnitude < 1e-4f) d = Vector3.forward;
            float dist = onBed ? 0.55f : 0.5f + info.len * 0.35f;
            Vector3 best = me + d.normalized * dist;
            for (int k = 0; k < 8 && !room.Free(best); k++) best = me + Quaternion.Euler(0, (k + 1) * 45, 0) * d.normalized * dist;
            var to = me - best; face = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            return best;
        }
        public void Hold(bool on) { held = on; if (on) { path = null; if (!onBed && !info.Cat) { sitT = 1; lieT = 0; } } }
        public Vector3 Back => rig != null ? rig.body.position + Vector3.up * 0.06f : Vector3.zero;
        public Vector3 HeadPos => rig?.head != null ? rig.head.position : Back;
        public bool Low => !onBed && info != null && info.height < 0.8f;   // too low to reach without crouching

        // what it does when you're done; returns its line (and maybe wanders off, if it's the cat)
        public string Petted()
        {
            held = false; happyT = info.Cat ? 2.5f : 3.2f;
            if (info.Cat)
            {
                if (UnityEngine.Random.value < 0.25f && !onBed) { thinkT = 0.2f; return "The cat has left the chat."; }
                if (UnityEngine.Random.value < 0.2f) return "*judges you silently*";
                string[] c = { "Purrrrr...", "Mrrp.", "*slow blink*", "Purr purr purr." }; return c[UnityEngine.Random.Range(0, c.Length)];
            }
            string[] d = Id == "frenchie" ? new[] { "*snort snort*", "*happy wheezing*", "Boof!" } : Id == "doberman" ? new[] { "Woof.", "*leans on you*", "*tail goes full helicopter*" } : new[] { "Woof!", "Arf arf!", "*happy tail noises*" };
            return d[UnityEngine.Random.Range(0, d.Length)];
        }
        public string Greeting() => info == null ? "" : info.Cat ? (UnityEngine.Random.value < 0.5f ? "Who's a pretty kitty?" : "Hey, " + info.name + ".") : (UnityEngine.Random.value < 0.5f ? "Who's a good boy?" : "Hey buddy!");

        // the room reacting to you
        void OnEnable() { HomeLife.Puff += OnPuff; }
        void OnDisable() { HomeLife.Puff -= OnPuff; }
        void OnPuff() { if (info == null) return; if (!info.Cat) { sitT = 0; lieT = 1; } }

        // ------------------------------------------------------------ the brain
        void Update()
        {
            if (rig == null || room == null || st == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            var player = st.Pivot.localPosition; bool playerMoving = (player - lastPlayer).sqrMagnitude > 1e-6f; lastPlayer = player;
            idleT = playerMoving ? 0 : idleT + dt;
            if (happyT > 0) happyT -= dt;
            if (sniffT > 0) sniffT -= dt;

            if (hopping) Hop(dt);
            else if (held) { path = null; FaceTo(player, dt); }
            else if (info.Cat) CatBrain(dt, player);
            else DogBrain(dt, player, playerMoving);

            Walk(dt);
            Place(); Pose(dt);
            // the label sits a little lower when it's curled up
            if (use != null) use.top = Mathf.Lerp(info.height + 0.12f, info.height * 0.55f + 0.12f, lieW);
        }

        void DogBrain(float dt, Vector3 player, bool playerMoving)
        {
            float d = Flat(player - pos).magnitude;
            if (doing == "wander")
            {
                if (path == null && sniffT <= 0) doing = "near";
                if (d > 2.6f || playerMoving && d > 1.6f) doing = "near";
                else return;
            }
            bool far = d > 1.5f || (playerMoving && d > 1.05f);
            if (far)
            {
                repath -= dt;
                if (path == null || repath <= 0)
                {
                    repath = 0.5f;
                    float pyaw = st.Pivot.localEulerAngles.y;
                    var goal = player + Quaternion.Euler(0, pyaw, 0) * new Vector3(0.55f, 0, -0.45f);
                    GoTo(goal, Mathf.Lerp(1.5f, 2.0f, Mathf.InverseLerp(0.17f, 0.52f, info.leg)) * (d > 2.5f ? 1.15f : 1f));
                }
                sitT = 0; lieT = 0;
                return;
            }
            if (path != null && d < 0.8f) path = null;
            if (path == null)
            {
                FaceTo(player, dt);
                // settle down: sit, then lie down if you're not going anywhere
                sitT = idleT > 9f ? 0 : 1; lieT = idleT > 9f ? 1 : 0;
                // bored: go sniff around the room now and then
                thinkT -= dt;
                if (thinkT <= 0 && idleT > 14f)
                {
                    thinkT = UnityEngine.Random.Range(14f, 28f);
                    var spot = RandomSpot(); GoTo(spot, 0.9f, () => { sniffT = 3f; sitT = 0; lieT = 0; }); doing = "wander";
                }
            }
        }

        void CatBrain(float dt, Vector3 player)
        {
            thinkT -= dt;
            if (doing == "home")
            {
                if (onBed) { lieT = 1; sitT = 0; if (happyT <= 0 && thinkT > 0) return; }
                if (thinkT > 0) return;
                // time to explore: hop down, then go somewhere (often to watch the fish)
                thinkT = UnityEngine.Random.Range(7f, 13f); doing = "out";
                if (onBed) HopTo(Free(bedSpot), () => Explore()); else Explore();
                return;
            }
            if (doing == "out")
            {
                if (path == null && !hopping)
                {
                    sitT = 1; lieT = 0;
                    if (room.tank != null && sniffT > 0) FaceTo(room.root.transform.InverseTransformPoint(room.tank.position), dt);
                    if (thinkT <= 0)
                    {
                        thinkT = UnityEngine.Random.Range(25f, 45f); doing = "back";
                        if (hasBed) GoTo(Free(bedSpot), 0.85f, () => HopTo(bedSpot, () => { onBed = true; doing = "home"; yaw = bedYaw; }));
                        else doing = "home";
                    }
                }
            }
        }
        void Explore()
        {
            onBed = false; sitT = 0; lieT = 0;
            if (room.tank != null && UnityEngine.Random.value < 0.45f)
                GoTo(Free(room.root.transform.InverseTransformPoint(room.tank.position)), 0.85f, () => { sniffT = 10f; if (UnityEngine.Random.value < 0.5f) HomeLifeSay("*stares at the fish*"); });
            else GoTo(RandomSpot(), 0.85f);
        }
        void HomeLifeSay(string line) => HomeLife.Say("pet", line);

        Vector3 RandomSpot()
        {
            for (int k = 0; k < 20; k++)
            {
                var p = new Vector3(UnityEngine.Random.Range(-room.W / 2 + 0.5f, room.W / 2 - 0.5f), 0, UnityEngine.Random.Range(-room.D / 2 + 0.5f, room.D / 2 - 0.5f));
                if (room.Free(p)) return p;
            }
            return Free(pos);
        }

        // ------------------------------------------------------------ moving
        Action arrived;
        void GoTo(Vector3 to, float spd, Action then = null)
        {
            path = room.Path(new Vector3(pos.x, 0, pos.z), to); pi = 0; speed = spd; arrived = then;
            if (onBed) { onBed = false; }
        }
        void Walk(float dt)
        {
            bool standing = sitW + lieW < 0.2f;
            bool moving = false;
            if (path != null && pi < path.Count && !hopping)
            {
                sitT = 0; lieT = 0;
                if (standing)
                {
                    var target = path[pi]; var dv = Flat(target - pos); float step = speed * dt;
                    if (dv.magnitude <= step) { pos = new Vector3(target.x, pos.y, target.z); pi++; }
                    else { pos += dv.normalized * step; moving = true; }
                    if (dv.sqrMagnitude > 1e-4f) yaw = Mathf.MoveTowardsAngle(yaw, Mathf.Atan2(dv.x, dv.z) * Mathf.Rad2Deg, 420 * dt);
                    pos.y = Mathf.MoveTowards(pos.y, 0, dt * 2);
                }
                if (pi >= path.Count) { path = null; var cb = arrived; arrived = null; cb?.Invoke(); }
            }
            walkW = Mathf.MoveTowards(walkW, moving ? 1 : 0, dt * 5);
            if (moving) phase += dt * speed / Mathf.Max(0.12f, info.leg * 1.5f) * Mathf.PI * 2;
        }
        void HopTo(Vector3 to, Action then)
        {
            path = null; hopping = true; hopFrom = pos; hopTo = to; hopT = 0; hopDone = then; sitT = 0; lieT = 0;
            var dv = Flat(to - pos); if (dv.sqrMagnitude > 1e-4f) yaw = Mathf.Atan2(dv.x, dv.z) * Mathf.Rad2Deg;
        }
        void Hop(float dt)
        {
            if (sitW + lieW > 0.15f) return;   // stand up first
            hopT += dt / 0.5f; float t = Mathf.Clamp01(hopT);
            float h = Mathf.Max(hopFrom.y, hopTo.y) + 0.25f - Mathf.Lerp(hopFrom.y, hopTo.y, t);
            pos = Vector3.Lerp(hopFrom, hopTo, t) + Vector3.up * (4 * h * t * (1 - t));
            if (t >= 1) { hopping = false; pos = hopTo; var cb = hopDone; hopDone = null; cb?.Invoke(); }
        }
        void FaceTo(Vector3 p, float dt)
        {
            var dv = Flat(p - pos); if (dv.sqrMagnitude < 0.04f) return;
            float want = Mathf.Atan2(dv.x, dv.z) * Mathf.Rad2Deg;
            // turn the body only when the head can't reach
            if (Mathf.Abs(Mathf.DeltaAngle(yaw, want)) > 55) yaw = Mathf.MoveTowardsAngle(yaw, want, 160 * dt);
        }
        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);

        void Place()
        {
            rig.root.localPosition = pos; rig.root.localRotation = Quaternion.Euler(0, yaw, 0);
        }

        // ------------------------------------------------------------ the pose: stand / trot / sit / lie, then head, tail and tongue
        void Pose(float dt)
        {
            float k = dt <= 0 ? 1 : 1 - Mathf.Exp(-dt * 6f);
            sitW = Mathf.Lerp(sitW, path == null && !hopping ? sitT : 0, k); lieW = Mathf.Lerp(lieW, path == null && !hopping ? lieT : 0, k);
            if (onBed && info.Cat && !hopping) { lieW = Mathf.Lerp(lieW, 1, k); sitW = Mathf.Lerp(sitW, 0, k); }
            float leg = info.leg, len = info.len;
            // sitting rocks the body back on its hips; the front legs stay straight, the back legs fold under
            float sitPitch = info.Cat ? 38 : Id == "doberman" ? 28 : 32;
            float p = sitPitch * sitW * (1 - lieW);
            var hip = new Vector3(0, leg, -len / 2);
            var R = Quaternion.Euler(-p, 0, 0);
            float drop = len * Mathf.Sin(p * Mathf.Deg2Rad);
            // lying down: the belly comes down to the floor
            float lieDrop = (leg - 0.02f) * lieW;
            float trot = walkW * Mathf.Abs(Mathf.Sin(phase)) * leg * 0.05f;
            float hop = hopping ? 0 : (happyT > 0 && !info.Cat && !onBed ? Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.06f * Mathf.Clamp01(happyT) * (1 - sitW * 0.5f) : 0);
            rig.body.localPosition = hip + R * (rig.bodyRest - hip) - Vector3.up * (drop + lieDrop) + Vector3.up * (trot + hop);
            rig.body.localRotation = R * Quaternion.Euler(hopping ? -12 * Mathf.Sin(Mathf.Clamp01(hopT) * Mathf.PI) : 0, 0, 0);
            float hipY = leg - drop;
            float fold = Mathf.Acos(Mathf.Clamp(hipY / leg, -1, 1)) * Mathf.Rad2Deg;
            for (int i = 0; i < 4; i++)
            {
                var L = rig.legs[i]; if (L == null) continue;
                bool front = i < 2; float ph = phase + (i == 0 || i == 3 ? 0 : Mathf.PI);
                float swing = Mathf.Sin(ph) * 28f * walkW;
                float sitA = front ? p : p - fold;
                float lieA = info.Cat ? (front ? 95 : -100) : (front ? -82 : -95);
                float hopA = hopping ? (front ? -35 : 35) * Mathf.Sin(Mathf.Clamp01(hopT) * Mathf.PI) : 0;
                float a = Mathf.Lerp(swing + sitA * sitW, lieA, lieW) + hopA;
                L.localRotation = Quaternion.Euler(a, 0, (i % 2 == 0 ? -1 : 1) * 6 * lieW * (info.Cat ? 0 : 1));
            }
            // head: look at you (or the fish, or the floor when sniffing)
            if (rig.head != null)
            {
                Vector3 target = st.Bone(HumanBodyBones.Head) != null ? st.Bone(HumanBodyBones.Head).position : st.Pivot.position + Vector3.up * 1.6f;
                if (sniffT > 0 && room.tank != null && info.Cat) target = room.tank.position + Vector3.up * 1.0f;
                else if (sniffT > 0) target = rig.root.position + rig.root.forward * 0.6f;
                var local = rig.body.InverseTransformDirection((target - rig.head.position).normalized);
                float hy = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -60, 60), hp = Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(local.y, -1, 1)) * Mathf.Rad2Deg, -35, 40);
                if (walkW > 0.5f) { hy *= 0.3f; hp = hp * 0.3f + 6; }
                if (sniffT > 0 && !info.Cat) hp = 35 + Mathf.Sin(Time.time * 12) * 5;
                float tilt = happyT > 0 && !info.Cat ? Mathf.Sin(Time.time * 3) * 14 : 0;
                var want = Quaternion.Euler(hp, hy, tilt) * rig.headRot0;
                rig.head.localRotation = dt <= 0 ? want : Quaternion.Slerp(rig.head.localRotation, want, 1 - Mathf.Exp(-dt * 5));
                rig.head.localPosition = rig.headRest + new Vector3(0, -0.04f * lieW * (info.Cat ? 0.3f : 1), 0.02f * lieW);
            }
            // tail: wag (dogs), swish (cats); faster when happy
            if (rig.tail != null)
            {
                bool happy = happyT > 0;
                float spd = info.Cat ? (happy ? 3.5f : 1.6f) : happy ? 16f : walkW > 0.5f ? 9f : lieW > 0.5f ? 2.5f : 6f;
                float amp = info.Cat ? (happy ? 18 : 10) : happy ? 42 : lieW > 0.5f ? 10 : 22;
                wagT += dt * spd; wagAmp = Mathf.Lerp(wagAmp, amp, dt <= 0 ? 1 : 1 - Mathf.Exp(-dt * 4));
                float down = info.Cat ? 70 * lieW : 30 * lieW + 10 * sitW;
                rig.tail.localRotation = Quaternion.Euler(down, Mathf.Sin(wagT) * wagAmp, 0) * rig.tailRot0;
            }
            if (rig.tongue != null) rig.tongue.gameObject.SetActive(happyT > 0 || (walkW > 0.3f && speed > 1.2f) || idleT < 3f && sitW > 0.5f);
        }
    }
}
