using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dream.Film
{
    // URP materials for a character part, from its manifest: the diffuse map tinted by the manifest colour, normal map
    // when there is one, alpha clip for hair, brows, lashes and the eyes' cornea, and per-part smoothness. Skin uses
    // the Dream/Skin shader (wrap lighting with a warm scatter tint) when it is there.
    public static class Materials
    {
        static readonly Dictionary<string, float> Smooth = new Dictionary<string, float>
        { ["skin"] = 0.46f, ["eyes"] = 0.95f, ["teeth"] = 0.42f, ["tongue"] = 0.5f, ["hair"] = 0.38f, ["haircards"] = 0.42f, ["brows"] = 0.2f, ["lashes"] = 0.2f };

        public static Material Make(string dir, string part, Dictionary<string, object> man)
        {
            var mats = man["materials"] as Dictionary<string, object>;
            var e = mats != null && mats.TryGetValue(part, out var v) ? v as Dictionary<string, object> : null;
            var skinShader = Shader.Find("Dream/Skin");
            var hairShader = Shader.Find("Dream/Hair");   // strand highlights for the hair cards
            var m = new Material(part == "skin" && skinShader != null ? skinShader : part == "haircards" && hairShader != null ? hairShader : Shader.Find("Universal Render Pipeline/Lit")) { name = part };
            Color tint = Color.white;
            if (man.TryGetValue("colors", out var cs) && cs is Dictionary<string, object> cd && cd.TryGetValue(part, out var cv) && cv is List<object> l)
                tint = new Color(System.Convert.ToSingle(l[0]), System.Convert.ToSingle(l[1]), System.Convert.ToSingle(l[2]));
#if UNITY_EDITOR
            if (e != null && e.TryGetValue("diffuse", out var df)) m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{df}"));
            if (e != null && e.TryGetValue("normalmap", out var nm))
            {
                m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{nm}"));
                m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 1f);
            }
#endif
            if (part == "teeth") tint *= 0.72f;   // teeth and tongue sit in the mouth's shadow: they must never glow
            if (part == "tongue") tint *= 0.6f;
            m.SetColor("_BaseColor", tint);
#if UNITY_EDITOR
            if (part == "skin" && m.HasProperty("_PoreMap")) m.SetTexture("_PoreMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Dream/FilmData/Tex/pores_n.png"));
#endif
            m.SetFloat("_Smoothness", Smooth.TryGetValue(part, out var sm) ? sm : 0.18f);
            m.SetFloat("_Metallic", 0f);
            bool clip = part == "hair" || part == "haircards" || part == "brows" || part == "lashes" || part == "eyes";
            if (clip)
            {
                m.SetFloat("_AlphaClip", 1); m.SetFloat("_Cutoff", part == "hair" ? 0.35f : part == "haircards" ? 0.4f : 0.25f); m.EnableKeyword("_ALPHATEST_ON");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            }
            if (part == "hair" || part == "haircards" || part == "brows" || part == "lashes") m.SetFloat("_Cull", 0);   // two-sided cards
            if (part != "eyes" && part != "teeth")   // skin and cloth don't mirror the room (a bright window turned faces chrome)
            { m.SetFloat("_EnvironmentReflections", 0); m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF"); }
            return m;
        }
    }
}
