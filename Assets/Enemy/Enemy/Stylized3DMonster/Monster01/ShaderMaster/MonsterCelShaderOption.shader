Shader "Custom/Flat2D_Outline_Rim_Metallic_Emission_Opaque"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)

        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Float) = 0.005

        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0.5,10)) = 3
        _RimIntensity ("Rim Intensity", Range(0,3)) = 1

        _MetallicStrength ("Metallic Strength", Range(0,1)) = 0
        _MetallicColor ("Metallic Color", Color) = (1,1,1,1)
        _EmissionColor ("Emission Color", Color) = (0,0,0,1)

        _ShadeThreshold ("Shade Threshold", Range(0,1)) = 0.5
        _ShadeIntensity ("Shade Intensity", Range(0,1)) = 0.5
        _ShadeColor ("Shade Color", Color) = (0,0,0,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        // === Outline Pass ===
        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 pos : SV_POSITION;
            };

            float _OutlineWidth;
            fixed4 _OutlineColor;

            v2f vert(appdata v)
            {
                v2f o;
                float3 norm = normalize(v.normal);
                v.vertex.xyz += norm * _OutlineWidth;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }

        // === Base Lighting Pass ===
        Pass
        {
            Name "BASE"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            // No transparency
            Blend Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;

            fixed4 _RimColor;
            float _RimPower;
            float _RimIntensity;

            float _MetallicStrength;
            fixed4 _MetallicColor;
            fixed4 _EmissionColor;

            float _ShadeThreshold;
            float _ShadeIntensity;
            fixed4 _ShadeColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Base color
                fixed4 baseCol = tex2D(_MainTex, i.uv) * _Color;

                // Lighting setup
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 normal = normalize(i.worldNormal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = saturate(dot(normal, lightDir));

                // Toon shading
                float shadeMask = step(NdotL, _ShadeThreshold);
                float litMask = 1.0 - shadeMask;

                fixed3 litCol = baseCol.rgb * NdotL;
                fixed3 shadeCol = baseCol.rgb * _ShadeColor.rgb * _ShadeIntensity;
                fixed3 lightingCol = litCol * litMask + shadeCol * shadeMask;

                // Rim Light
                float rim = pow(1 - saturate(dot(viewDir, normal)), _RimPower) * _RimIntensity;
                fixed3 rimCol = _RimColor.rgb * rim;

                // Specular highlight
                float3 halfDir = normalize(lightDir + viewDir);
                float spec = pow(saturate(dot(normal, halfDir)), 16) * _MetallicStrength;
                fixed3 specCol = _MetallicColor.rgb * spec;

                // Final color
                fixed3 finalCol = lightingCol + rimCol + specCol + _EmissionColor.rgb;
                return fixed4(finalCol, 1.0); // Always opaque
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
