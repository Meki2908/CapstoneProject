Shader "EGA/Particles/Add_DistortTexture"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _TintColor ("Color", Color) = (0.5,0.5,0.5,1)
        _Emission ("Emission", Float) = 2
        _Noise ("Noise", 2D) = "white" {}
        _NoisespeedUV ("Noise speed U/V", Vector) = (0,0,0,0)
        _Mask ("Mask", 2D) = "white" {}
        _Distortionpower ("Distortion power", Float) = 1
        [Toggle] _Usedepth ("Use depth?", Float) = 0
        _Depthpower ("Depth power", Float) = 1
    }
    SubShader
    {
        Tags 
        { 
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForwardOnly" }
            
            Blend One One
            Cull Off
            ZWrite Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_particles
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 texcoord : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv0 : TEXCOORD0;
                float4 color : COLOR;
                float4 screenPos : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_Noise);
            SAMPLER(sampler_Noise);
            TEXTURE2D(_Mask);
            SAMPLER(sampler_Mask);
            
            #ifdef _USEDEPTH_ON
            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);
            #endif
            
            float4 _MainTex_ST;
            float4 _Noise_ST;
            float4 _Mask_ST;
            float4 _TintColor;
            float _Emission;
            float4 _NoisespeedUV;
            float _Distortionpower;
            float _Usedepth;
            float _Depthpower;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv0 = input.texcoord;
                output.color = input.color;
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 maskTex = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, input.uv0.xy * _Mask_ST.xy + _Mask_ST.zw);
                
                float2 noiseUV = (_Time.y * float2(_NoisespeedUV.r, _NoisespeedUV.g) + input.uv0.xy) * _Noise_ST.xy + _Noise_ST.zw;
                float4 noiseTex = SAMPLE_TEXTURE2D(_Noise, sampler_Noise, noiseUV);
                
                float2 distortedUV = ((maskTex.rgb * noiseTex.rgb).rg * input.uv0.xy * _Distortionpower) + input.uv0.xy;
                float4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV);
                
                float3 main = (mainTex.rgb * input.color.rgb * _TintColor.rgb) * mainTex.a * input.color.a * _TintColor.a;
                
                float alpha = mainTex.a * input.color.a * _TintColor.a;
                
                #ifdef _USEDEPTH_ON
                float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, input.screenPos.xy / input.screenPos.w).r;
                float sceneZ = LinearEyeDepth(depth, _ZBufferParams);
                float partZ = input.screenPos.w;
                float fade = saturate((sceneZ - partZ) / _Depthpower);
                alpha *= lerp(1, fade, _Usedepth);
                #endif
                
                float3 emission = main * _Emission;
                return float4(emission * alpha, alpha);
            }
            ENDHLSL
        }
    }
    Fallback "Particles/Standard Unlit"
}
