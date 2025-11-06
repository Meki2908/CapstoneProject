Shader "UI/SwordUltimateGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Thunder Effect)]
        _ThunderIntensity ("Thunder Intensity", Range(0, 5)) = 2.0
        _ThunderColor ("Thunder Color", Color) = (0.5, 0.8, 1, 1)
        _ThunderSpeed ("Thunder Speed", Range(0.1, 10)) = 3.0
        _ThunderPulse ("Thunder Pulse", Range(0, 1)) = 0.7
        
        [Header(Cooldown Effect)]
        _CooldownProgress ("Cooldown Progress", Range(0, 1)) = 0
        _CooldownColor ("Cooldown Color", Color) = (0.2, 0.2, 0.2, 1)
        
        [Header(Ready Effect)]
        _ReadyGlow ("Ready Glow", Range(0, 3)) = 2.0
        _ReadyPulse ("Ready Pulse", Range(0, 1)) = 0.8
        
        [Header(Stencil)]
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            
            // Thunder properties
            float _ThunderIntensity;
            fixed4 _ThunderColor;
            float _ThunderSpeed;
            float _ThunderPulse;
            
            // Cooldown properties
            float _CooldownProgress;
            fixed4 _CooldownColor;
            
            // Ready properties
            float _ReadyGlow;
            float _ReadyPulse;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                
                // Time-based effects
                float time = _Time.y * _ThunderSpeed;
                float pulse = sin(time) * 0.5 + 0.5;
                float thunderFlicker = sin(time * 8.0) * 0.3 + 0.7;
                
                // Cooldown state
                bool isOnCooldown = _CooldownProgress > 0.01;
                bool isReady = _CooldownProgress < 0.01;
                
                if (isOnCooldown)
                {
                    // Cooldown effect - dimmed
                    float cooldownFade = 1.0 - _CooldownProgress;
                    color.rgb *= lerp(_CooldownColor.rgb, fixed3(1,1,1), cooldownFade);
                }
                else if (isReady)
                {
                    // Ready effect - Thunder glow around frame
                    float readyPulse = sin(time * 4.0) * 0.5 + 0.5;
                    float thunderGlow = _ReadyGlow * (1.0 + readyPulse * _ReadyPulse) * thunderFlicker;
                    
                    // Create frame glow effect (only around edges)
                    float2 uv = IN.texcoord;
                    float frameDistance = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
                    float frameGlow = smoothstep(0.0, 0.1, frameDistance) * (1.0 - smoothstep(0.1, 0.2, frameDistance));
                    
                    // Add thunder glow to frame
                    color.rgb += _ThunderColor.rgb * thunderGlow * frameGlow * 0.8;
                    
                    // Enhance the main texture slightly
                    color.rgb *= (1.0 + thunderGlow * 0.2);
                }
                else
                {
                    // Normal state - subtle thunder glow
                    float normalGlow = _ThunderIntensity * (1.0 + pulse * _ThunderPulse) * thunderFlicker;
                    
                    // Frame glow
                    float2 uv = IN.texcoord;
                    float frameDistance = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
                    float frameGlow = smoothstep(0.0, 0.1, frameDistance) * (1.0 - smoothstep(0.1, 0.2, frameDistance));
                    
                    color.rgb += _ThunderColor.rgb * normalGlow * frameGlow * 0.3;
                }
                
                // Apply glow to alpha for better blending
                color.a *= (1.0 + _ThunderIntensity * 0.1);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}
