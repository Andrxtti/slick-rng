// Film hair (DAY2 Batch A2): hair cards with the strand texture's alpha as a cutout, two-sided, lit by URP's PBR for the
// diffuse plus a Kajiya-Kay highlight: light catches along the strand (the cards' V direction), as a band across the
// head, with a second, warmer and broader band below it (light that went into the hair and came out tinted).
Shader "Dream/Hair"
{
    Properties
    {
        _BaseMap("Strands", 2D) = "white" {}
        _BaseColor("Colour", Color) = (1, 1, 1, 1)
        _Cutoff("Alpha cutoff", Range(0, 1)) = 0.4
        _Smoothness("Smoothness", Range(0, 1)) = 0.35
        _Spec1("Highlight", Range(0, 2)) = 0.09
        _Exp1("Highlight sharpness", Float) = 90
        _Spec2("Second highlight", Range(0, 2)) = 0.12
        _Exp2("Second sharpness", Float) = 20
        _Shift("Second shift", Float) = -0.12
    }
    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "RenderPipeline" = "UniversalPipeline" "Queue" = "AlphaTest" }
        Cull Off
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
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; half4 _BaseColor; half _Cutoff, _Smoothness, _Spec1, _Exp1, _Spec2, _Exp2, _Shift;
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

            half Strand(float3 B, float3 N, float3 H, half shift, half e)
            {
                float3 b = normalize(B + N * shift);
                half bh = dot(b, H);
                return pow(saturate(sqrt(saturate(1 - bh * bh))), e) * smoothstep(-1, 0, bh);
            }

            half3 Highlights(Light l, float3 B, float3 N, float3 V, half3 alb)
            {
                float3 H = normalize(l.direction + V);
                half k = l.distanceAttenuation * l.shadowAttenuation * saturate(dot(N, l.direction) * 0.5 + 0.5);
                return l.color * k * (_Spec1 * Strand(B, N, H, 0, _Exp1) + _Spec2 * Strand(B, N, H, _Shift, _Exp2) * alb * 2);
            }

            half4 frag(Varyings i, bool front : SV_IsFrontFace) : SV_Target
            {
                half4 alb = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                clip(alb.a - _Cutoff);
                float3 N = normalize(i.normalWS) * (front ? 1 : -1);
                float3 T = normalize(i.tangentWS.xyz), B = normalize(cross(N, T) * i.tangentWS.w);   // B runs along the strand (the cards' V)

                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS; inputData.normalWS = N; inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(i.positionWS); inputData.fogCoord = i.fog; inputData.bakedGI = SampleSH(N);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS); inputData.shadowMask = half4(1, 1, 1, 1);
                SurfaceData s = (SurfaceData)0;
                s.albedo = alb.rgb; s.alpha = 1; s.smoothness = _Smoothness * 0.5; s.occlusion = 1; s.normalTS = half3(0, 0, 1);
                half4 c = UniversalFragmentPBR(inputData, s);

                float3 V = inputData.viewDirectionWS;
                half3 hl = Highlights(GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask), B, N, V, alb.rgb);
                uint count = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(count)
                    hl += Highlights(GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask), B, N, V, alb.rgb);
                LIGHT_LOOP_END
                c.rgb += hl * lerp(0.35, 1, saturate(Luminance(alb.rgb) * 4));   // dark hair shines less, and never white
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
