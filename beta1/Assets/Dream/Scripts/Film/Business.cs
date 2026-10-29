using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dream.Film
{
    // Stage business: the small physical moments the script's {directions} call for, keyed by scene and line.
    public static class Business
    {
        public static void Before(string scene, int line, GameObject set, Dictionary<string, Actor> actors)
        {
            if (set == null) return;
            if (scene == "p_diner" && line == 9 && actors.TryGetValue("you", out var you))   // the envelope slides along the counter
            {
                var env = Sets.Find(set, "Prop_Envelope");
                if (env != null) env.GetComponentInParent<Transform>().gameObject.AddComponent<Slide>().Go(env, you.transform.position, 1.6f);
            }
        }

        class Slide : MonoBehaviour
        {
            public void Go(Transform t, Vector3 towards, float time) => StartCoroutine(Run(t, towards, time));
            IEnumerator Run(Transform t, Vector3 towards, float time)
            {
                var a = t.position; var b = new Vector3(Mathf.Lerp(a.x, towards.x, 0.8f), a.y, Mathf.Lerp(a.z, towards.z, 0.8f));
                for (float u = 0; u < time; u += Time.deltaTime) { float k = 1 - (1 - u / time) * (1 - u / time); t.position = Vector3.Lerp(a, b, k); yield return null; }
                t.position = b;
            }
        }
    }
}
