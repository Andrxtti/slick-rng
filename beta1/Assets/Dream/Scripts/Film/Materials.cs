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
        { ["skin"] = 0.46f, ["eyes"] = 0.95f, ["teeth"] = 0.3f, ["tongue"] = 0.5f, ["hair"] = 0.38f, ["haircards"] = 0.42f, ["brows"] = 0.2f, ["lashes"] = 0.2f };

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
            if (part == "teeth") tint *= new Color(0.78f, 0.7f, 0.6f);   // teeth sit back in the mouth's shadow: warm, never grey pegs
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
            if (part == "glasses")   // frames: glossy acetate, or metal wire
            {
                m.SetFloat("_Smoothness", 0.82f);
                if (e != null && e.TryGetValue("metal", out var mt)) m.SetFloat("_Metallic", System.Convert.ToSingle(mt) * 0.6f);   // full metal mirrors a dark room as black
                return m;
            }
            if (part == "lenses")   // see-through glass, tinted where the brief says so
            {
                float a = e != null && e.TryGetValue("alpha", out var al) ? System.Convert.ToSingle(al) : 0.15f;
                m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0); m.SetFloat("_ZWrite", 0); m.SetFloat("_Cull", 0);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                m.SetColor("_BaseColor", new Color(tint.r, tint.g, tint.b, a)); m.SetFloat("_Smoothness", 0.9f);
                m.SetOverrideTag("RenderType", "Transparent");
                foreach (var pass in new[] { "ShadowCaster", "DepthOnly", "DepthNormals" }) m.SetShaderPassEnabled(pass, false);   // no shadow or SSAO over the eyes
                return m;
            }
            if (part != "eyes" && part != "teeth")   // skin and cloth don't mirror the room (a bright window turned faces chrome)
            { m.SetFloat("_EnvironmentReflections", 0); m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF"); }
            return m;
        }
    }
}
