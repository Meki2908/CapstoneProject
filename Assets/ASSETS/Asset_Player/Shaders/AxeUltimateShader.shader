Shader "UI/AxeUltimateGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Wind Effect)]
        _WindIntensity ("Wind Intensity", Range(0, 5)) = 2.0
        _WindColor ("Wind Color", Color) = (0.8, 1, 0.8, 1)
        _WindSpeed ("Wind Speed", Range(0.1, 10)) = 2.5
        _WindPulse ("Wind Pulse", Range(0, 1)) = 0.6
        
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
            
            // Wind properties
            float _WindIntensity;
            fixed4 _WindColor;
            float _WindSpeed;
            float _WindPulse;
            
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
                float time = _Time.y * _WindSpeed;
                float pulse = sin(time) * 0.5 + 0.5;
                float windWave = sin(time * 3.0 + IN.texcoord.x * 10.0) * 0.3 + 0.7;
                
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
                    // Ready effect - Wind glow around frame
                    float readyPulse = sin(time * 3.0) * 0.5 + 0.5;
                    float windGlow = _ReadyGlow * (1.0 + readyPulse * _ReadyPulse) * windWave;
                    
                    // Create frame glow effect (only around edges)
                    float2 uv = IN.texcoord;
                    float frameDistance = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
                    float frameGlow = smoothstep(0.0, 0.1, frameDistance) * (1.0 - smoothstep(0.1, 0.2, frameDistance));
                    
                    // Add wind glow to frame with swirling effect
                    float swirl = sin(time * 2.0 + atan2(uv.y - 0.5, uv.x - 0.5) * 3.0) * 0.5 + 0.5;
                    color.rgb += _WindColor.rgb * windGlow * frameGlow * swirl * 0.8;
                    
                    // Enhance the main texture slightly
                    color.rgb *= (1.0 + windGlow * 0.2);
                }
                else
                {
                    // Normal state - subtle wind glow
                    float normalGlow = _WindIntensity * (1.0 + pulse * _WindPulse) * windWave;
                    
                    // Frame glow
                    float2 uv = IN.texcoord;
                    float frameDistance = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
                    float frameGlow = smoothstep(0.0, 0.1, frameDistance) * (1.0 - smoothstep(0.1, 0.2, frameDistance));
                    
                    color.rgb += _WindColor.rgb * normalGlow * frameGlow * 0.3;
                }
                
                // Apply glow to alpha for better blending
                color.a *= (1.0 + _WindIntensity * 0.1);

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
