// Hands and gaze on top of whatever the body is playing: reach a hand to a point, look at something, lean in.
// Uses the animator's IK pass (on in Dream's own controller). Weights ease in and out so nothing snaps.
using UnityEngine;

namespace CFDream.Character
{
    [RequireComponent(typeof(Animator))]
    public class Gestures : MonoBehaviour
    {
        Animator an;
        Vector3 rh, lh, look; float rw, lw, lookW, bodyW, rwT, lwT, lookWT, bodyWT;

        void Awake() { an = GetComponent<Animator>(); }

        public void Right(Vector3 p, float w) { rh = p; rwT = w; }
        public void Left(Vector3 p, float w) { lh = p; lwT = w; }
        public void Both(Vector3 left, Vector3 right, float w) { Left(left, w); Right(right, w); }
        public void Look(Vector3 p, float w, float body = 0) { look = p; lookWT = w; bodyWT = body; }
        public void Clear() { rwT = lwT = lookWT = bodyWT = 0; }

        void Update()
        {
            float k = 1 - Mathf.Exp(-Time.deltaTime * 9f);
            rw = Mathf.Lerp(rw, rwT, k); lw = Mathf.Lerp(lw, lwT, k); lookW = Mathf.Lerp(lookW, lookWT, k); bodyW = Mathf.Lerp(bodyW, bodyWT, k);
        }

        void OnAnimatorIK(int layer)
        {
            if (an == null) return;
            an.SetIKPositionWeight(AvatarIKGoal.RightHand, rw); an.SetIKPosition(AvatarIKGoal.RightHand, rh);
            an.SetIKPositionWeight(AvatarIKGoal.LeftHand, lw); an.SetIKPosition(AvatarIKGoal.LeftHand, lh);
            an.SetLookAtWeight(lookW, bodyW, 0.85f, 1f, 0.6f); an.SetLookAtPosition(look);
        }
    }
}
