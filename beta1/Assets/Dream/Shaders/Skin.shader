// Film skin (DAY2 Batch A1): URP's PBR lighting, plus
//   - pores: a tiled detail normal (FilmData/Tex/pores_n.png, made by Editor/Quality.cs) on top of the skin's own normal
//   - subsurface approximation: wrap lighting. Where a light's falloff would turn the skin black, a band of warm
//     scattered light (the colour of blood under skin) carries on round the terminator, from every light in the set
//   - a little less shine where the skin is thin and red (the albedo's redness drives a touch more roughness)
// Shadow, depth and normals passes come from URP/Lit, so SSAO, depth of field and shadows see the face as usual.
Shader "Dream/Skin"
{
    Properties
    {
        _BaseMap("Albedo", 2D) = "white" {}
        _BaseColor("Colour", Color) = (1, 1, 1, 1)
        _BumpMap("Normal", 2D) = "bump" {}
        _BumpScale("Normal strength", Float) = 1
        _PoreMap("Pores (detail normal)", 2D) = "bump" {}
        _PoreScale("Pore strength", Float) = 0.45
        _PoreTiling("Pore tiling", Float) = 42
        _Smoothness("Smoothness", Range(0, 1)) = 0.46
        _Scatter("Scatter colour", Color) = (0.9, 0.28, 0.16, 1)
        _Wrap("Wrap", Range(0, 1)) = 0.5
        _ScatterStrength("Scatter strength", Range(0, 2)) = 0.8
        _Metallic("Metallic", Range(0, 1)) = 0
        _Cutoff("Alpha cutoff", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_PoreMap); SAMPLER(sampler_PoreMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; half4 _BaseColor; half _BumpScale, _PoreScale, _PoreTiling, _Smoothness, _Wrap, _ScatterStrength, _Metallic, _Cutoff;
                half4 _Scatter;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 tangentOS : TANGENT; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2; float4 tangentWS : TEXCOORD3; half fog : TEXCOORD4;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(v.normalOS, v.tangentOS);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS; o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.normalWS = n.normalWS; o.tangentWS = float4(n.tangentWS, v.tangentOS.w * GetOddNegativeScale());
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            // the light that gets past the terminator: wrapped diffuse minus ordinary diffuse, so it only adds where
            // the surface turns away from the light
            half3 Scatter(Light l, float3 N)
            {
                half ndl = dot(N, l.direction);
                half w = saturate((ndl + _Wrap) / (1 + _Wrap)) - saturate(ndl);
                return l.color * l.distanceAttenuation * lerp(0.35, 1, l.shadowAttenuation) * w;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 alb = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                half3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv), _BumpScale);
                half3 pTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_PoreMap, sampler_PoreMap, i.uv * _PoreTiling), _PoreScale);
                nTS = normalize(half3(nTS.xy + pTS.xy, nTS.z * pTS.z));
                float3 bit = cross(i.normalWS, i.tangentWS.xyz) * i.tangentWS.w;
                float3 N = normalize(TransformTangentToWorld(nTS, half3x3(i.tangentWS.xyz, bit, i.normalWS)));

                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS; inputData.normalWS = N; inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(i.positionWS); inputData.fogCoord = i.fog; inputData.bakedGI = SampleSH(N);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS); inputData.shadowMask = half4(1, 1, 1, 1);

                half red = saturate((alb.r - max(alb.g, alb.b)) * 3);   // flushed, thin skin is a little less shiny
                SurfaceData s = (SurfaceData)0;
                s.albedo = alb.rgb; s.alpha = 1; s.metallic = _Metallic; s.smoothness = _Smoothness * (1 - 0.25 * red) * lerp(0.85, 1, pTS.z);
                s.normalTS = nTS; s.occlusion = 1;
                half4 c = UniversalFragmentPBR(inputData, s);

                half3 sc = Scatter(GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask), N);
                uint count = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(count)
                    sc += Scatter(GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask), N);
                LIGHT_LOOP_END
                c.rgb += sc * alb.rgb * _Scatter.rgb * _ScatterStrength;
                c.rgb = MixFog(c.rgb, inputData.fogCoord);
                return half4(c.rgb, 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    FallBack "Universal Render Pipeline/Lit"
}
