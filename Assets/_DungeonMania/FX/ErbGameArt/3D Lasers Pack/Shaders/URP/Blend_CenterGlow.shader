Shader "EGA/Particles/Blend_CenterGlow"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _Noise ("Noise", 2D) = "white" {}
        _TintColor ("Color", Color) = (0.5,0.5,0.5,1)
        _Emission ("Emission", Float) = 2
        _SpeedMainTexUVNoiseZW ("Speed MainTex U/V + Noise Z/W", Vector) = (0,0,0,0)
        _Opacity ("Opacity", Range(0, 1)) = 1
        [Toggle] _Usedepth ("Use depth?", Float) = 0
        _Depthpower ("Depth power", Float) = 1
        [Toggle] _Usecenterglow ("Use center glow?", Float) = 0
        _Mask ("Mask", 2D) = "white" {}
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
            
            Blend SrcAlpha OneMinusSrcAlpha
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
            float4 _SpeedMainTexUVNoiseZW;
            float _Opacity;
            float _Usedepth;
            float _Depthpower;
            float _Usecenterglow;

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
                float2 mainUV = (_Time.y * float2(_SpeedMainTexUVNoiseZW.r, _SpeedMainTexUVNoiseZW.g) + input.uv0.xy) * _MainTex_ST.xy + _MainTex_ST.zw;
                float4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, mainUV);
                
                float2 noiseUV = (_Time.y * float2(_SpeedMainTexUVNoiseZW.b, _SpeedMainTexUVNoiseZW.a) + input.uv0.xy) * _Noise_ST.xy + _Noise_ST.zw;
                float4 noiseTex = SAMPLE_TEXTURE2D(_Noise, sampler_Noise, noiseUV);
                
                float4 maskTex = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, input.uv0.xy * _Mask_ST.xy + _Mask_ST.zw);
                
                float3 mainColor = (mainTex.rgb * noiseTex.rgb) * input.color.rgb * _TintColor.rgb;
                
                float3 centerGlow = _Emission * lerp(mainColor, mainColor * saturate(maskTex.rgb * saturate(maskTex.rgb - (input.uv0.b * -1.0 + 1.0))), _Usecenterglow);
                
                float alpha = mainTex.a * noiseTex.a * input.color.a * _TintColor.a;
                
                #ifdef _USEDEPTH_ON
                float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, input.screenPos.xy / input.screenPos.w).r;
                float sceneZ = LinearEyeDepth(depth, _ZBufferParams);
                float partZ = input.screenPos.w;
                float fade = saturate((sceneZ - partZ) / _Depthpower);
                alpha *= lerp(1, fade, _Usedepth);
                #endif
                
                alpha *= _Opacity;
                
                return float4(centerGlow, alpha);
            }
            ENDHLSL
        }
    }
    Fallback "Particles/Standard Unlit"
}
