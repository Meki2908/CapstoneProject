Shader "Polytope Studio/PT_Eldritch_Skybox"
{
    Properties
    {
        [Header(Eye Texture)]
        _EyeTex ("Eye Texture", 2D) = "black" {}
        _EyeDirection ("Eye Direction (XYZ)", Vector) = (0, 0.3, 1, 0)
        _TexSize ("Texture Size on Sky", Range(0.1, 3.0)) = 0.8
        _TexBlendMode ("Blend Sharpness", Range(0.5, 5.0)) = 2.0
        
        [Header(Blue Sky)]
        _SkyTop ("Sky Top Color", Color) = (0.15, 0.45, 0.95, 1)
        _SkyHorizon ("Sky Horizon Color", Color) = (0.55, 0.78, 1.0, 1)
        _SkyBottom ("Sky Bottom Color", Color) = (0.7, 0.88, 1.0, 1)
        _SunDirection ("Sun Direction", Vector) = (0.3, 0.8, 0.5, 0)
        _SunColor ("Sun Glow Color", Color) = (1.0, 0.95, 0.8, 1)
        _SunSize ("Sun Glow Size", Range(0.0, 1.0)) = 0.3
        _CloudDensity ("Cloud Density", Range(0.0, 1.5)) = 0.5
        _CloudSpeed ("Cloud Speed", Range(0.0, 0.5)) = 0.05
        
        [Header(Spatial Rift)]
        _RiftSize ("Rift Size", Range(0.05, 0.8)) = 0.35
        _RiftEdgeWidth ("Rift Edge Width", Range(0.01, 0.15)) = 0.06
        _RiftEdgeColor ("Rift Edge Color", Color) = (0.6, 0.2, 1.0, 1)
        _RiftEdgeGlow ("Rift Edge Glow", Range(0.5, 5.0)) = 2.5
        _RiftDistortion ("Rift Distortion", Range(0.0, 0.3)) = 0.1
        _CrackDensity ("Crack Count", Range(3, 20)) = 10
        _CrackLength ("Crack Length", Range(0.1, 1.5)) = 0.7
        _CrackGlow ("Crack Glow", Range(0.0, 3.0)) = 1.5
        _ChromaticStrength ("Chromatic Rainbow", Range(0.0, 1.0)) = 0.5
        
        [Header(Void Behind Rift)]
        _VoidColor1 ("Void Color 1", Color) = (0.01, 0.005, 0.03, 1)
        _VoidColor2 ("Void Color 2", Color) = (0.05, 0.0, 0.1, 1)
        _VoidStarDensity ("Void Star Density", Range(0.0, 3.0)) = 1.5
        
        [Header(Animation)]
        _PulseSpeed ("Pulse Speed", Range(0.0, 3.0)) = 0.8
        _PulseIntensity ("Pulse Intensity", Range(0.0, 0.3)) = 0.08
        _GlobalSpeed ("Global Animation Speed", Range(0.1, 2.0)) = 1.0
    }
    
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
            #include "UnityCG.cginc"
            
            // --- Properties ---
            sampler2D _EyeTex;
            float4 _EyeDirection;
            float _TexSize;
            float _TexBlendMode;
            
            float4 _SkyTop;
            float4 _SkyHorizon;
            float4 _SkyBottom;
            float4 _SunDirection;
            float4 _SunColor;
            float _SunSize;
            float _CloudDensity;
            float _CloudSpeed;
            
            float _RiftSize;
            float _RiftEdgeWidth;
            float4 _RiftEdgeColor;
            float _RiftEdgeGlow;
            float _RiftDistortion;
            float _CrackDensity;
            float _CrackLength;
            float _CrackGlow;
            float _ChromaticStrength;
            
            float4 _VoidColor1;
            float4 _VoidColor2;
            float _VoidStarDensity;
            
            float _PulseSpeed;
            float _PulseIntensity;
            float _GlobalSpeed;
            
            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldDir : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            // =====================================
            //  NOISE FUNCTIONS
            // =====================================
            
            float hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }
            
            float hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }
            
            float2 hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }
            
            float noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            float fbm(float2 p, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * noise2D(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }
            
            // =====================================
            //  BLUE SKY
            // =====================================
            
            float3 blueSky(float3 dir, float time)
            {
                // Sky gradient
                float y = dir.y;
                float3 skyColor;
                if (y > 0.0)
                {
                    float t = pow(saturate(y), 0.6);
                    skyColor = lerp(_SkyHorizon.rgb, _SkyTop.rgb, t);
                }
                else
                {
                    float t = pow(saturate(-y), 0.4);
                    skyColor = lerp(_SkyHorizon.rgb, _SkyBottom.rgb, t);
                }
                
                // Sun glow
                float3 sunDir = normalize(_SunDirection.xyz);
                float sunDot = dot(normalize(dir), sunDir);
                float sunGlow = pow(saturate(sunDot), 8.0 / max(_SunSize, 0.01));
                skyColor += _SunColor.rgb * sunGlow * 0.6;
                float sunCore = pow(saturate(sunDot), 64.0 / max(_SunSize, 0.01));
                skyColor += _SunColor.rgb * sunCore * 1.5;
                
                // Simple clouds
                if (_CloudDensity > 0.01 && y > -0.1)
                {
                    float2 cloudUV = dir.xz / (abs(y) + 0.3) * 3.0;
                    cloudUV += float2(time * _CloudSpeed, time * _CloudSpeed * 0.3);
                    
                    float cloud = fbm(cloudUV, 5);
                    cloud = smoothstep(0.45, 0.75, cloud) * _CloudDensity;
                    cloud *= smoothstep(-0.1, 0.1, y); // fade near horizon
                    
                    float3 cloudColor = lerp(float3(0.95, 0.95, 1.0), _SunColor.rgb * 0.8, 
                                             pow(saturate(sunDot * 0.5 + 0.5), 2.0) * 0.3);
                    skyColor = lerp(skyColor, cloudColor, cloud * 0.7);
                }
                
                return skyColor;
            }
            
            // =====================================
            //  VOID (behind the rift)
            // =====================================
            
            float3 voidSpace(float2 uv, float time)
            {
                float3 voidColor = lerp(_VoidColor1.rgb, _VoidColor2.rgb, fbm(uv * 2.0 + time * 0.02, 3));
                
                // Stars in the void
                if (_VoidStarDensity > 0.01)
                {
                    float2 starUV = uv * 40.0;
                    float2 id = floor(starUV);
                    float2 f = frac(starUV) - 0.5;
                    float rnd = hash21(id);
                    
                    if (rnd > (1.0 - _VoidStarDensity * 0.05))
                    {
                        float2 offset = hash22(id) - 0.5;
                        float d = length(f - offset * 0.3);
                        float star = smoothstep(0.04, 0.0, d);
                        float twinkle = sin(time * 2.0 + rnd * 50.0) * 0.3 + 0.7;
                        voidColor += star * twinkle * float3(0.7, 0.6, 1.0);
                    }
                    
                    // Nebula wisps in void
                    float neb = fbm(uv * 3.0 + time * 0.03, 4);
                    voidColor += float3(0.15, 0.02, 0.25) * pow(neb, 2.5) * 0.8;
                }
                
                return voidColor;
            }
            
            // =====================================
            //  SPATIAL RIFT + CRACKS
            // =====================================
            
            // Returns: x = rift mask (inside void), y = edge glow, z = crack glow, w = total effect
            float4 spatialRift(float2 localUV, float time)
            {
                float dist = length(localUV);
                float angle = atan2(localUV.y, localUV.x);
                
                // Rift shape - irregular hole
                float riftPulse = sin(time * _PulseSpeed) * _PulseIntensity;
                float riftRadius = _RiftSize + riftPulse;
                
                // Make rift shape irregular
                float irregularity = 0.0;
                irregularity += sin(angle * 3.0 + time * 0.2) * 0.03;
                irregularity += sin(angle * 7.0 - time * 0.3) * 0.02;
                irregularity += sin(angle * 11.0 + time * 0.15) * 0.015;
                float riftShape = riftRadius + irregularity;
                
                // Inside rift (void visible)
                float riftMask = smoothstep(riftShape, riftShape - 0.02, dist);
                
                // Rift edge glow
                float edgeDist = abs(dist - riftShape);
                float edgeGlow = exp(-edgeDist / _RiftEdgeWidth) * _RiftEdgeGlow;
                edgeGlow *= smoothstep(riftShape + _RiftEdgeWidth * 4.0, riftShape, dist) + riftMask * 0.3;
                
                // === Radial cracks emanating from rift ===
                float cracks = 0.0;
                int numCracks = (int)_CrackDensity;
                
                for (int i = 0; i < 20; i++)
                {
                    if (i >= numCracks) break;
                    
                    float crackAngle = hash11(float(i) * 73.156) * 6.28318;
                    float angleDiff = abs(angle - crackAngle);
                    angleDiff = min(angleDiff, 6.28318 - angleDiff);
                    
                    // Crack gets thinner away from rift
                    float crackFalloff = saturate((dist - riftShape) / _CrackLength);
                    float crackWidth = lerp(0.025, 0.004, crackFalloff);
                    
                    // Wobble
                    float wobble = sin(dist * 20.0 + hash11(float(i) * 17.0) * 30.0) * 0.01;
                    
                    float crack = smoothstep(crackWidth, crackWidth * 0.15, angleDiff + wobble);
                    
                    // Show only outside the rift, fading with distance
                    crack *= smoothstep(riftShape - 0.01, riftShape + 0.02, dist);
                    crack *= (1.0 - crackFalloff);
                    
                    // Some cracks are shorter
                    float crackMaxLen = _CrackLength * (0.3 + hash11(float(i) * 41.0) * 0.7);
                    crack *= smoothstep(crackMaxLen + riftShape, riftShape, dist);
                    
                    cracks += crack;
                }
                
                // Branch cracks
                for (int j = 0; j < 10; j++)
                {
                    if (j >= numCracks / 2) break;
                    
                    float parentAngle = hash11(float(j) * 73.156) * 6.28318;
                    float branchDist = riftShape + hash11(float(j) * 91.0) * _CrackLength * 0.5;
                    float branchAngle = parentAngle + (hash11(float(j) * 53.0) - 0.5) * 0.8;
                    
                    float2 branchOrigin = float2(cos(parentAngle), sin(parentAngle)) * branchDist;
                    float2 branchDir2 = float2(cos(branchAngle), sin(branchAngle));
                    
                    float2 toPoint = localUV - branchOrigin;
                    float proj = dot(toPoint, branchDir2);
                    float perp = length(toPoint - branchDir2 * proj);
                    
                    float branchLen = _CrackLength * 0.3 * hash11(float(j) * 67.0);
                    float branch = smoothstep(0.008, 0.001, perp) * smoothstep(branchLen, 0.0, proj) * step(0.0, proj);
                    branch *= smoothstep(0.8, 0.3, length(localUV)); // fade far away
                    
                    cracks += branch * 0.7;
                }
                
                cracks = saturate(cracks);
                
                return float4(riftMask, edgeGlow, cracks, saturate(riftMask + edgeGlow + cracks));
            }
            
            // =====================================
            //  EYE TEXTURE SAMPLING
            // =====================================
            
            float4 sampleEyeTexture(float2 localUV, float time, float riftMask)
            {
                // Scale UV to fit within rift
                float2 texUV = localUV / (_RiftSize * 1.2);
                texUV = texUV * 0.5 + 0.5;
                
                // Pulse
                float pulse = sin(time * _PulseSpeed) * _PulseIntensity * 0.5;
                texUV = (texUV - 0.5) * (1.0 - pulse) + 0.5;
                
                if (texUV.x < 0.0 || texUV.x > 1.0 || texUV.y < 0.0 || texUV.y > 1.0)
                    return float4(0, 0, 0, 0);
                
                // Chromatic aberration
                float2 centerOffset = texUV - 0.5;
                float chrStr = _ChromaticStrength * 0.015;
                
                float r = tex2D(_EyeTex, texUV + centerOffset * chrStr).r;
                float g = tex2D(_EyeTex, texUV).g;
                float b = tex2D(_EyeTex, texUV - centerOffset * chrStr).b;
                
                float3 texColor = float3(r, g, b);
                
                // Boost brightness
                float lum = dot(texColor, float3(0.299, 0.587, 0.114));
                texColor += texColor * pow(lum, 0.3) * 0.15;
                
                return float4(texColor, riftMask);
            }
            
            // =====================================
            //  VERTEX / FRAGMENT
            // =====================================
            
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldDir = v.vertex.xyz;
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.worldDir);
                float time = _Time.y * _GlobalSpeed;
                
                // === Build local frame around rift direction ===
                float3 eyeDir = normalize(_EyeDirection.xyz);
                float3 worldUp = abs(eyeDir.y) > 0.99 ? float3(0, 0, 1) : float3(0, 1, 0);
                float3 right = normalize(cross(worldUp, eyeDir));
                float3 up = normalize(cross(eyeDir, right));
                
                float fwd = dot(dir, eyeDir);
                float localX = dot(dir, right);
                float localY = dot(dir, up);
                
                // Gnomonic projection
                float2 localUV = float2(0, 0);
                bool inFront = fwd > 0.01;
                if (inFront)
                {
                    localUV = float2(localX, localY) / fwd;
                    localUV /= _TexSize;
                }
                
                // === 1. Blue Sky ===
                float3 skyColor = blueSky(dir, time);
                
                // === 2. Spatial Rift ===
                float4 rift = float4(0, 0, 0, 0);
                if (inFront)
                {
                    rift = spatialRift(localUV, time);
                }
                
                // === 3. Void behind rift ===
                float3 voidCol = voidSpace(localUV * 2.0, time);
                
                // === 4. Eye texture in the rift ===
                float4 eyeTex = float4(0, 0, 0, 0);
                if (inFront && rift.x > 0.01)
                {
                    eyeTex = sampleEyeTexture(localUV, time, rift.x);
                }
                
                // === Compose ===
                float3 finalColor = skyColor;
                
                // Inside rift: show void + eye
                if (inFront)
                {
                    // Void behind rift
                    finalColor = lerp(finalColor, voidCol, rift.x);
                    
                    // Eye texture over void
                    finalColor = lerp(finalColor, eyeTex.rgb, eyeTex.a * 0.9);
                    
                    // Rift edge glow (purple/mystical energy)
                    float3 edgeColor = _RiftEdgeColor.rgb;
                    // Rainbow on edges
                    float angle = atan2(localUV.y, localUV.x);
                    float rainbow = frac(angle / 6.28318 + time * 0.05);
                    float3 rainbowCol;
                    rainbowCol.r = saturate(sin(rainbow * 6.28318) * 0.5 + 0.5);
                    rainbowCol.g = saturate(sin(rainbow * 6.28318 + 2.094) * 0.5 + 0.5);
                    rainbowCol.b = saturate(sin(rainbow * 6.28318 + 4.189) * 0.5 + 0.5);
                    edgeColor = lerp(edgeColor, rainbowCol * 1.5, _ChromaticStrength * 0.3);
                    
                    finalColor += edgeColor * rift.y;
                    
                    // Cracks on the sky (glowing cracks in reality)
                    float3 crackColor = lerp(edgeColor, rainbowCol, _ChromaticStrength * 0.5);
                    finalColor += crackColor * rift.z * _CrackGlow;
                    
                    // Sky distortion near rift (reality warping)
                    float distortStr = _RiftDistortion * exp(-length(localUV) / (_RiftSize * 2.0));
                    float3 distortedDir = dir + (right * sin(time + length(localUV) * 10.0) 
                                              + up * cos(time * 0.7 + length(localUV) * 8.0)) * distortStr;
                    float3 distortedSky = blueSky(normalize(distortedDir), time);
                    float distortMask = smoothstep(_RiftSize * 0.8, _RiftSize * 2.5, length(localUV))
                                      * (1.0 - rift.x);
                    finalColor = lerp(finalColor, distortedSky, distortStr * distortMask * 3.0);
                }
                
                return float4(finalColor, 1.0);
            }
            ENDCG
        }
    }
    
    Fallback Off
}
