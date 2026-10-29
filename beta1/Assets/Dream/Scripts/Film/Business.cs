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
            if (scene == "p_diner2" && line == 1 && actors.TryGetValue("you", out var you))   // the envelope slides along the counter
            {
                var env = Sets.Find(set, "Prop_Envelope");
                if (env != null) env.GetComponentInParent<Transform>().gameObject.AddComponent<Slide>().Go(env, you.transform.position, 1.6f);
            }
            if (scene == "p_diner2" && line == 8 && actors.TryGetValue("slick", out var slick)) slick.gameObject.SetActive(false);   // gone before the bell stops
            if (scene == "p_offer" && line == 1)   // the phone buzzes on the counter, its screen lit
            {
                var ph = Sets.Find(set, "Prop_Phone"); var scr = Sets.Find(set, "PhoneScreen"); var lockScreen = Sets.Find(set, "PhoneNotif");
                if (lockScreen != null) lockScreen.gameObject.SetActive(true);
                if (ph != null) ph.gameObject.AddComponent<Buzz>().Go(ph, scr);
                if (ph != null && actors.TryGetValue("you", out var me)) me.LookAt(ph.position);   // eyes go to the phone
            }
            if (scene.StartsWith("p_") && line == 1 && Sets.Find(set, "PhoneNotif") is Transform n && scene != "p_offer") n.gameObject.SetActive(false);
        }

        class Buzz : MonoBehaviour
        {
            public void Go(Transform phone, Transform screen) => StartCoroutine(Run(phone, screen));
            IEnumerator Run(Transform phone, Transform screen)
            {
                var r = screen != null ? screen.GetComponent<Renderer>() : null; var m = r != null ? r.material : null;
                if (m != null) foreach (var prop in new[] { "emissiveFactor", "_EmissionColor" }) if (m.HasProperty(prop)) m.SetColor(prop, new Color(0.02f, 0.03f, 0.06f));   // the lock screen wakes, dim: the notification is what reads
                var p0 = phone.localPosition; var q0 = phone.localRotation;
                for (int pulse = 0; pulse < 3; pulse++)
                {
                    for (float t = 0; t < 0.38f; t += Time.deltaTime)   // the buzz walks the phone a few millimetres
                    {
                        phone.localPosition = p0 + new Vector3(Mathf.Sin(t * 260f), 0, Mathf.Cos(t * 210f)) * 0.0012f;
                        phone.localRotation = q0 * Quaternion.Euler(0, Mathf.Sin(t * 190f) * 0.6f, 0);
                        yield return null;
                    }
                    p0 += new Vector3(0.002f, 0, 0.001f); phone.localPosition = p0;
                    yield return new WaitForSeconds(0.52f);
                }
            }
        }

        class Slide : MonoBehaviour
        {
            public void Go(Transform t, Vector3 towards, float time) => StartCoroutine(Run(t, towards, time));
            IEnumerator Run(Transform t, Vector3 towards, float time)
            {
                var a = t.position; var b = new Vector3(Mathf.Lerp(a.x, towards.x, 0.8f), a.y, a.z);   // along the counter, not off its edge
                for (float u = 0; u < time; u += Time.deltaTime) { float k = 1 - (1 - u / time) * (1 - u / time); t.position = Vector3.Lerp(a, b, k); yield return null; }
                t.position = b;
            }
        }
    }
}
