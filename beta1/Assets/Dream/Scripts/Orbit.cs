using UnityEngine;

namespace Dream
{
    // Slow orbit around a point: the render test's camera move.
    public class Orbit : MonoBehaviour
    {
        public Vector3 target = new Vector3(0, 0.5f, 0);
        public float speed = 15f;
        void Update() { transform.RotateAround(target, Vector3.up, speed * Time.deltaTime); transform.LookAt(target); }
    }
}
