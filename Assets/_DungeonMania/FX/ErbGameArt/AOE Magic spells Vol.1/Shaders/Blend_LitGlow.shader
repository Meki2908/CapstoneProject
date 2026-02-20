Shader "EGA/Particles/Blend_LitGlow" {
    Properties {
        _MainTex ("MainTex", 2D) = "white" {}
        _Noise ("Noise", 2D) = "white" {}
        _TintColor ("Color", Color) = (0.5,0.5,0.5,1)
        _Emission ("Emission", Float ) = 2
        _SpeedMainTexUVNoiseUV ("Speed MainTex U/V + Noise U/V", Vector) = (0,0,0,0)
        _Opacity ("Opacity", Range(0, 1)) = 1
        [MaterialToggle] _Usedepth ("Use depth?", Float ) = 0
        _Depthpower ("Depth power", Float ) = 1
        [MaterialToggle] _Usecenterglow ("Use center glow?", Float ) = 0
        _Mask ("Mask", 2D) = "white" {}
        _LightFalloff ("Light Falloff", Float ) = 0.5
        _LightRange ("Light Range", Float ) = 2
        _LightEmission ("Light Emission", Float ) = 1
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
			"PreviewType"="Plane"
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            //#define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            //#pragma multi_compile_fwdbase
			#pragma multi_compile_particles
            //#pragma target 3.0
            uniform float4 _LightColor0;
            uniform sampler2D _CameraDepthTexture;
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform float4 _TintColor;
            uniform sampler2D _Mask; uniform float4 _Mask_ST;
            uniform float _Emission;
            uniform fixed _Usecenterglow;
            uniform sampler2D _Noise; uniform float4 _Noise_ST;
            uniform float4 _SpeedMainTexUVNoiseUV;
            uniform float _Opacity;
            uniform fixed _Usedepth;
            uniform float _Depthpower;
            uniform float _LightFalloff;
            uniform float _LightRange;
            uniform float _LightEmission;
            struct VertexInput {
                float4 vertex : POSITION;
                float4 texcoord0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float4 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float4 vertexColor : COLOR;
                float4 projPos : TEXCOORD2;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                float3 lightColor = _LightColor0.rgb;
                o.pos = UnityObjectToClipPos( v.vertex );
                o.projPos = ComputeScreenPos (o.pos);
                COMPUTE_EYEDEPTH(o.projPos.z);
                return o;
            }
            float4 frag(VertexOutput i, float facing : VFACE) : COLOR {
                float isFrontFace = ( facing >= 0 ? 1 : 0 );
                float faceSign = ( facing >= 0 ? 1 : -1 );
                float sceneZ = max(0,LinearEyeDepth (UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)))) - _ProjectionParams.g);
                float partZ = max(0,i.projPos.z - _ProjectionParams.g);
                float3 lightColor = _LightColor0.rgb;
                float2 node_3348 = ((_Time.g*float2(_SpeedMainTexUVNoiseUV.r,_SpeedMainTexUVNoiseUV.g))+i.uv0);
                float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(node_3348, _MainTex));
                float2 node_8986 = ((_Time.g*float2(_SpeedMainTexUVNoiseUV.b,_SpeedMainTexUVNoiseUV.a))+i.uv0);
                float4 _Noise_var = tex2D(_Noise,TRANSFORM_TEX(node_8986, _Noise));
                float3 node_9394 = ((_MainTex_var.rgb*_Noise_var.rgb)*i.vertexColor.rgb*_TintColor.rgb);
                float4 _Mask_var = tex2D(_Mask,TRANSFORM_TEX(i.uv0, _Mask));
                float3 node_9929 = (_Emission*lerp( node_9394, (node_9394*saturate((_Mask_var.rgb*saturate((_Mask_var.rgb-(i.uv0.b*-1.0+1.0)))))), _Usecenterglow ));
                float3 node_2600 = (_LightColor0.rgb*(0.0 + ( (max((_LightRange-pow(distance(_WorldSpaceLightPos0.rgb,float4(i.posWorld.rgb,i.posWorld.a)),_LightFalloff)),0.0) - 0.0) * (1.0 - 0.0) ) / (_LightRange - 0.0))*_LightEmission);
                float3 finalColor = node_9929 + node_2600;
                float node_6301 = (_MainTex_var.a*_Noise_var.a*i.vertexColor.a*_TintColor.a);
				#ifdef SOFTPARTICLES_ON
					float fade = saturate ((sceneZ-partZ)/_Depthpower);
					node_6301 *= lerp(1, fade, _Usedepth);
				#endif
                return fixed4(finalColor,node_6301*_Opacity);
            }
            ENDCG
        }
        Pass {
            Name "FORWARD_DELTA"
            Tags {
                "LightMode"="ForwardAdd"
            }
            Blend One One
            Cull Off
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            //#define UNITY_PASS_FORWARDADD
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #pragma multi_compile_fwdadd
            //#pragma target 3.0
            uniform float4 _LightColor0;
            uniform sampler2D _CameraDepthTexture;
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform float4 _TintColor;
            uniform sampler2D _Mask; uniform float4 _Mask_ST;
            uniform float _Emission;
            uniform fixed _Usecenterglow;
            uniform sampler2D _Noise; uniform float4 _Noise_ST;
            uniform float4 _SpeedMainTexUVNoiseUV;
            uniform float _Opacity;
            uniform fixed _Usedepth;
            uniform float _Depthpower;
            uniform float _LightFalloff;
            uniform float _LightRange;
            uniform float _LightEmission;
            struct VertexInput {
                float4 vertex : POSITION;
                float4 texcoord0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float4 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float4 vertexColor : COLOR;
                float4 projPos : TEXCOORD2;
                LIGHTING_COORDS(3,4)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                float3 lightColor = _LightColor0.rgb;
                o.pos = UnityObjectToClipPos( v.vertex );
                o.projPos = ComputeScreenPos (o.pos);
                COMPUTE_EYEDEPTH(o.projPos.z);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            float4 frag(VertexOutput i, float facing : VFACE) : COLOR {
                float isFrontFace = ( facing >= 0 ? 1 : 0 );
                float faceSign = ( facing >= 0 ? 1 : -1 );
                float sceneZ = max(0,LinearEyeDepth (UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)))) - _ProjectionParams.g);
                float partZ = max(0,i.projPos.z - _ProjectionParams.g);
                float3 lightColor = _LightColor0.rgb;
                float3 node_2600 = (_LightColor0.rgb*(0.0 + ( (max((_LightRange-pow(distance(_WorldSpaceLightPos0.rgb,float4(i.posWorld.rgb,i.posWorld.a)),_LightFalloff)),0.0) - 0.0) * (1.0 - 0.0) ) / (_LightRange - 0.0))*_LightEmission);
                float2 node_3348 = ((_Time.g*float2(_SpeedMainTexUVNoiseUV.r,_SpeedMainTexUVNoiseUV.g))+i.uv0);
                float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(node_3348, _MainTex));
                float2 node_8986 = ((_Time.g*float2(_SpeedMainTexUVNoiseUV.b,_SpeedMainTexUVNoiseUV.a))+i.uv0);
                float4 _Noise_var = tex2D(_Noise,TRANSFORM_TEX(node_8986, _Noise));
                float node_6301 = (_MainTex_var.a*_Noise_var.a*i.vertexColor.a*_TintColor.a);
				#ifdef SOFTPARTICLES_ON
					float fade = saturate ((sceneZ-partZ)/_Depthpower);
					node_6301 *= lerp(1, fade, _Usedepth);
				#endif
                return fixed4(node_2600 * node_6301,0);
            }
            ENDCG
        }      
    }
}
