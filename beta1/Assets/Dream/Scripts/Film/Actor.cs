using UnityEngine;

namespace Dream.Film
{
    // A person in a film. Spawn puts the character for `who` on its mark (seated if the mark is at seat height).
    // Until a character has a built body (Cast), a grey stand-in holds the place so shots can be framed.
    public class Actor : MonoBehaviour
    {
        public const int Layer = 1;   // TransparentFX: actors' collision bodies
        public string Who;
        public bool Seated;
        public Transform Head;   // the head bone when there is a body
        protected Vector3 lookTarget; protected bool hasLook;

        public virtual Vector3 Eyes => Head != null ? Head.position + Vector3.up * 0.11f + transform.forward * 0.08f : transform.position + Vector3.up * (Seated ? 0.74f : 1.62f);
        public Vector3 Forward => transform.forward;

        public static Actor Spawn(string who, Transform mark)
        {
            var go = new GameObject("Actor_" + who);
            // glTF turns Blender empties around: a mark made facing +Y in Blender faces -Z here, so add a half turn
            go.transform.SetPositionAndRotation(mark.position, Quaternion.Euler(0, mark.eulerAngles.y + 180f, 0));
            bool seated = mark.position.y - mark.root.position.y > 0.3f;   // the set's floor is at its root
            var a = Cast.Dress(who, go, seated);
            a.Who = who; a.Seated = seated;
            // a body the camera can bump into (Shot.Clear); over-the-shoulder shots ignore this layer
            var cc = go.AddComponent<CapsuleCollider>(); cc.radius = 0.22f; cc.height = seated ? 1.3f : 1.8f; cc.center = Vector3.up * (cc.height / 2);
            go.layer = Layer;
            return a;
        }

        public virtual void Act(string clip, string next, float length) { }
        public virtual void Say(AudioClip clip) { }
        public virtual void LookAt(Vector3 target) { lookTarget = target; hasLook = true; }
    }

    // Who gets which body. Characters built so far are added here (task 4 on); anyone else is a stand-in.
    public static partial class Cast
    {
        static partial void Build(string who, GameObject go, bool seated, ref Actor actor);

        public static Actor Dress(string who, GameObject go, bool seated)
        {
            Actor a = null;
            Build(who, go, seated, ref a);
            if (a != null) return a;
            a = go.AddComponent<Actor>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.35f, 0.35f, 0.38f) };
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); Object.Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(go.transform, false); body.GetComponent<Renderer>().sharedMaterial = mat;
            body.transform.localPosition = Vector3.up * (seated ? 0.3f : 0.75f); body.transform.localScale = new Vector3(0.42f, seated ? 0.35f : 0.75f, 0.28f);
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(head.GetComponent<Collider>());
            head.transform.SetParent(go.transform, false); head.GetComponent<Renderer>().sharedMaterial = mat;
            head.transform.localPosition = Vector3.up * (seated ? 0.7f : 1.58f); head.transform.localScale = new Vector3(0.18f, 0.23f, 0.2f);
            return a;
        }
    }
}
