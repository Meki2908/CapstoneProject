Shader "Custom/UnlitWithShadows"
{
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _ShadowDarkness ("Shadow Darkness", Range(0,1)) = 0.5
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf UnlitWithShadows addshadow fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        float _ShadowDarkness;

        struct Input {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutput o) {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }

        half4 LightingUnlitWithShadows(SurfaceOutput s, half3 lightDir, half atten)
        {
            // atten = contribution from lights (0..1). When atten==0 -> in shadow.
            half shadowFactor = saturate(atten);
            half mixFactor = lerp(_ShadowDarkness, 1.0, shadowFactor);
            half4 col;
            col.rgb = s.Albedo * mixFactor;
            col.a = 1;
            return col;
        }
        ENDCG
    }
    FallBack "Diffuse"
}


