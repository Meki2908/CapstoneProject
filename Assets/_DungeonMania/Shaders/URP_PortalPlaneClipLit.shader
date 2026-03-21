// Unity 6 / URP 17+ — Mesh “lộ dần” khi xuyên cổng: clip theo nửa không gian (mặt phẳng cổng).
// Gán _PortalPoint + _PortalNormal từ PortalPlaneClipBinder (MaterialPropertyBlock).
Shader "DungeonMania/URP/PortalPlaneClipLit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor]   _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        _Smoothness("Smoothness", Range(0, 1)) = 0.4
        _Metallic("Metallic", Range(0, 1)) = 0.0

        [Header(Portal clip)]
        _PortalPoint("Portal Point (world)", Vector) = (0, 0, 0, 0)
        _PortalNormal("Portal Normal (world)", Vector) = (0, 0, 1, 0)
        [Toggle] _PortalClipEnabled("Portal Clip Enabled", Float) = 1
        [Toggle] _PortalInvert("Invert Visible Side", Float) = 0
        _PortalSoftness("Soft Edge (world units)", Range(0, 0.5)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Smoothness;
                float _Metallic;
                float4 _PortalPoint;
                float4 _PortalNormal;
                float _PortalClipEnabled;
                float _PortalInvert;
                float _PortalSoftness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogCoord : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            void ApplyPortalClip(float3 worldPos)
            {
                if (_PortalClipEnabled < 0.5)
                    return;

                float3 p = _PortalPoint.xyz;
                float3 n = normalize(_PortalNormal.xyz);
                float d = dot(worldPos - p, n);
                if (_PortalInvert > 0.5)
                    d = -d;

                float soft = max(_PortalSoftness, 1e-5);
                float a = smoothstep(-soft, soft, d);
                clip(a - 0.001);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogCoord = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                ApplyPortalClip(input.positionWS);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float3 n = normalize(input.normalWS);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half att = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                half NdotL = saturate(dot(n, mainLight.direction));
                half3 radiance = mainLight.color * (NdotL * att);

                half3 gi = SampleSH(n);

                half3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 h = normalize(mainLight.direction + viewDir);
                half specMask = pow(saturate(dot(n, h)), lerp(8.0h, 128.0h, _Smoothness));
                half3 f0 = lerp(half3(0.04h, 0.04h, 0.04h), albedo.rgb, _Metallic);
                half3 spec = f0 * specMask * att * mainLight.color * _Smoothness;

                half3 lit = albedo.rgb * (radiance + gi) + spec;
                lit = MixFog(lit, input.fogCoord);
                return half4(lit, albedo.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex shadowVert
            #pragma fragment shadowFrag

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // Giống URP ShadowCasterPass — Unity gán khi vẽ shadow map (normal bias).
            float3 _LightDirection;
            float3 _LightPosition;

            float4 _PortalPoint;
            float4 _PortalNormal;
            float _PortalClipEnabled;
            float _PortalInvert;
            float _PortalSoftness;

            struct AttributesShadow
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VaryingsShadow
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            void ApplyPortalClipShadow(float3 worldPos)
            {
                if (_PortalClipEnabled < 0.5)
                    return;
                float3 p = _PortalPoint.xyz;
                float3 n = normalize(_PortalNormal.xyz);
                float d = dot(worldPos - p, n);
                if (_PortalInvert > 0.5)
                    d = -d;
                float soft = max(_PortalSoftness, 1e-5);
                float a = smoothstep(-soft, soft, d);
                clip(a - 0.001);
            }

            VaryingsShadow shadowVert(AttributesShadow input)
            {
                VaryingsShadow output;
                UNITY_SETUP_INSTANCE_ID(input);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

#if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
                float3 lightDirectionWS = _LightDirection;
#endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                positionCS = ApplyShadowClamping(positionCS);

                output.positionCS = positionCS;
                output.positionWS = positionWS;
                return output;
            }

            half4 shadowFrag(VaryingsShadow i) : SV_Target
            {
                ApplyPortalClipShadow(i.positionWS);
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
