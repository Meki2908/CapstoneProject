Shader "UI/GlassCrack"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _CrackColor ("Crack Color", Color) = (0.9, 0.95, 1.0, 0.85)
        _CrackWidth ("Crack Width", Range(0.001, 0.02)) = 0.004
        _RadialCount ("Radial Crack Count", Range(4, 24)) = 12
        _Distortion ("Crack Distortion", Range(0, 0.5)) = 0.15
        // Click positions packed as Vector4(x, y, radius, time)
        // Support up to 10 click points
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

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _CrackColor;
            float _CrackWidth;
            float _RadialCount;
            float _Distortion;

            // Up to 10 crack impact points: (x, y, radius, intensity)
            float4 _CrackPoints[10];
            int _CrackPointCount;

            // Hash function for pseudo-random
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // Generate crack pattern around a single point
            float crackPattern(float2 uv, float2 center, float maxRadius, float intensity)
            {
                float2 d = uv - center;
                float dist = length(d);

                // Ngoài bán kính → không có crack
                if (dist > maxRadius || dist < 0.001) return 0;

                float crack = 0;
                float angle = atan2(d.y, d.x);

                // ── Radial cracks (tia từ tâm ra ngoài) ──
                float radialCount = _RadialCount;
                float angleStep = 6.28318 / radialCount;

                for (int i = 0; i < 24; i++)
                {
                    if (i >= (int)radialCount) break;

                    // Góc của tia + nhiễu ngẫu nhiên
                    float baseAngle = angleStep * i;
                    float noise = hash(float2(float(i), center.x * 100)) * _Distortion;
                    float crackAngle = baseAngle + noise;

                    // Khoảng cách góc từ pixel đến tia
                    float angleDiff = abs(angle - crackAngle);
                    angleDiff = min(angleDiff, 6.28318 - angleDiff);

                    // Tia mỏng dần theo khoảng cách
                    float width = _CrackWidth * (1.0 + dist * 2.0);

                    // Wobble — vết nứt không thẳng hoàn toàn
                    float wobble = sin(dist * 30.0 + float(i) * 7.0) * 0.02 * dist;
                    angleDiff = abs(angleDiff + wobble);

                    if (angleDiff < width)
                    {
                        float fadeDist = smoothstep(maxRadius, maxRadius * 0.3, dist);
                        float fadeWidth = 1.0 - angleDiff / width;
                        crack = max(crack, fadeWidth * fadeDist * intensity);
                    }

                    // ── Nhánh phụ (sub-cracks branching) ──
                    if (dist > maxRadius * 0.2 && angleDiff < width * 3.0)
                    {
                        float branchAngle = crackAngle + 0.4 * sign(hash(float2(float(i), 3.7)) - 0.5);
                        float branchDiff = abs(angle - branchAngle);
                        branchDiff = min(branchDiff, 6.28318 - branchDiff);
                        float branchStart = maxRadius * (0.2 + hash(float2(float(i), 5.3)) * 0.4);

                        if (branchDiff < width * 0.8 && dist > branchStart)
                        {
                            float bFade = smoothstep(maxRadius, branchStart, dist) * 0.6;
                            crack = max(crack, bFade * intensity);
                        }
                    }
                }

                return saturate(crack);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float totalCrack = 0;

                // Tính crack cho tất cả click points
                for (int p = 0; p < 10; p++)
                {
                    if (p >= _CrackPointCount) break;

                    float2 center = _CrackPoints[p].xy;
                    float radius = _CrackPoints[p].z;
                    float intensity = _CrackPoints[p].w;

                    totalCrack = max(totalCrack, crackPattern(i.uv, center, radius, intensity));
                }

                // Không có crack → transparent
                if (totalCrack < 0.01) discard;

                // Crack color + highlight nhẹ
                float4 col = _CrackColor;
                col.a *= totalCrack;

                // Thêm viền sáng trắng ở rìa vết nứt
                float highlight = smoothstep(0.3, 0.8, totalCrack);
                col.rgb = lerp(col.rgb, float3(1, 1, 1), highlight * 0.4);

                return col * i.color;
            }
            ENDCG
        }
    }
}
