using System;
using UnityEngine;

namespace CFDream.Character
{
    // Keeps a piece where it was built relative to its bone, every frame after the body animates. If the body is
    // rebuilt and the bone replaced, it finds the new bone by name (UMA keeps bone names unique).
    [DefaultExecutionOrder(1000)]
    public class BoneFollow : MonoBehaviour
    {
        Func<GameObject> bodyOf; Transform bone; string boneName; Vector3 pos; Quaternion rot = Quaternion.identity; bool pinned;

        public void Init(Func<GameObject> body, Transform b) { bodyOf = body; bone = b; boneName = b != null ? b.name : null; }

        // remember where the piece sits relative to the bone right now
        public void Pin()
        {
            if (bone == null) return;
            pos = bone.InverseTransformPoint(transform.position);
            rot = Quaternion.Inverse(bone.rotation) * transform.rotation;
            pinned = true;
        }

        void LateUpdate()
        {
            if (!pinned) return;
            if (bone == null) bone = Find();
            if (bone == null) return;
            transform.SetPositionAndRotation(bone.TransformPoint(pos), bone.rotation * rot);
        }

        Transform Find()
        {
            var go = bodyOf?.Invoke(); var root = go != null ? go.transform : null;
            if (root == null || string.IsNullOrEmpty(boneName)) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == boneName) return t;
            return null;
        }
    }
}
