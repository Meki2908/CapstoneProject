Shader "EGA/HDRP/Particles/Blend_Electricity"
{
    Properties
    {
		_MainTexture("Main Texture", 2D) = "white" {}
		_Dissolveamount("Dissolve amount", Range( 0 , 1)) = 0.332
		_Mask("Mask", 2D) = "white" {}
		_Color("Color", Color) = (0.5,0.5,0.5,1)
		_Emission("Emission", Float) = 6
		_RemapXYFresnelZW("Remap XY/Fresnel ZW", Vector) = (-10,10,2,2)
		_Speed("Speed", Vector) = (0.189,0.225,-0.2,-0.05)
		_Opacity("Opacity", Range( 0 , 1)) = 1
		[Toggle(_USEDEPTH_ON)] _Usedepth("Use depth?", Float) = 0
		_Depthpower("Depth power", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "RenderType"="Transparent" "Queue"="Transparent" }

		Blend SrcAlpha OneMinusSrcAlpha
		Cull Back
		//ZTest Off
		ZWrite Off

		HLSLINCLUDE
		#pragma target 4.5
		#pragma multi_compile_instancing
		ENDHLSL

		
        Pass
        {
			
            Name "Depth prepass"
            Tags { "LightMode"="DepthForwardOnly" }
            ColorMask 0
			
        
            HLSLPROGRAM
        
				#pragma vertex Vert
				#pragma fragment Frag
        
				#define _SURFACE_TYPE_TRANSPARENT 1
				#define _BLENDMODE_ALPHA 1
				#pragma shader_feature _USEDEPTH_ON

        
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Wind.hlsl"
        
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
        
                #define SHADERPASS SHADERPASS_DEPTH_ONLY
        
        
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"
        
				struct AttributesMesh 
				{
					float3 positionOS : POSITION;
					float4 normalOS : NORMAL;
					float4 ase_texcoord : TEXCOORD0;
					float4 ase_color : COLOR;
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC;
					#endif 
				};
        
				struct PackedVaryingsMeshToPS 
				{
					float4 positionCS : SV_Position;
					float4 ase_texcoord : TEXCOORD0;
					float4 ase_color : COLOR;
					float4 ase_texcoord1 : TEXCOORD1;
					float4 ase_texcoord2 : TEXCOORD2;
					float4 ase_texcoord3 : TEXCOORD3;
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC; 
					#endif
				};

				CBUFFER_START(UnityPerMaterial)
				sampler2D _Mask;
				float _Dissolveamount;
				sampler2D _MainTexture;
				float4 _Speed;
				float4 _MainTexture_ST;
				float4 _RemapXYFresnelZW;
				float4 _Color;
				float _Opacity;
				float _Depthpower;
				CBUFFER_END
				                
                struct SurfaceDescription
                {
                    float Alpha;
                    float AlphaClipThreshold;
                };

				void BuildSurfaceData(FragInputs fragInputs, SurfaceDescription surfaceDescription, float3 V, out SurfaceData surfaceData)
				{
					ZERO_INITIALIZE(SurfaceData, surfaceData);
				}
        
				void GetSurfaceAndBuiltinData(SurfaceDescription surfaceDescription, FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
				{ 
				#if _ALPHATEST_ON
					DoAlphaTest ( surfaceDescription.Alpha, surfaceDescription.AlphaClipThreshold );
				#endif

					BuildSurfaceData(fragInputs, surfaceDescription, V, surfaceData);
					ZERO_INITIALIZE(BuiltinData, builtinData);
					builtinData.opacity =  surfaceDescription.Alpha;
					builtinData.distortion = float2(0.0, 0.0);
					builtinData.distortionBlur =0.0;
				}

				PackedVaryingsMeshToPS Vert(AttributesMesh inputMesh   )
				{
					PackedVaryingsMeshToPS outputPackedVaryingsMeshToPS;

					UNITY_SETUP_INSTANCE_ID(inputMesh);
					UNITY_TRANSFER_INSTANCE_ID(inputMesh, outputPackedVaryingsMeshToPS);

					float3 ase_worldPos = GetAbsolutePositionWS( TransformObjectToWorld( (inputMesh.positionOS).xyz ) );
					outputPackedVaryingsMeshToPS.ase_texcoord1.xyz = ase_worldPos;
					float3 ase_worldNormal = TransformObjectToWorldNormal(inputMesh.normalOS.xyz);
					outputPackedVaryingsMeshToPS.ase_texcoord2.xyz = ase_worldNormal;
					float4 ase_clipPos = TransformWorldToHClip( TransformObjectToWorld(inputMesh.positionOS));
					float4 screenPos = ComputeScreenPos( ase_clipPos , _ProjectionParams.x );
					outputPackedVaryingsMeshToPS.ase_texcoord3 = screenPos;
					
					outputPackedVaryingsMeshToPS.ase_texcoord.xy = inputMesh.ase_texcoord.xy;
					outputPackedVaryingsMeshToPS.ase_color = inputMesh.ase_color;
					
					//setting value to unused interpolator channels and avoid initialization warnings
					outputPackedVaryingsMeshToPS.ase_texcoord.zw = 0;
					outputPackedVaryingsMeshToPS.ase_texcoord1.w = 0;
					outputPackedVaryingsMeshToPS.ase_texcoord2.w = 0;
					inputMesh.positionOS.xyz +=  float3( 0, 0, 0 ) ;
					inputMesh.normalOS =  inputMesh.normalOS ;

					float3 positionRWS = TransformObjectToWorld(inputMesh.positionOS);
					outputPackedVaryingsMeshToPS.positionCS = TransformWorldToHClip(positionRWS);  
					return outputPackedVaryingsMeshToPS;
				}

				void Frag(  PackedVaryingsMeshToPS packedInput
							#ifdef WRITE_NORMAL_BUFFER
							, out float4 outNormalBuffer : SV_Target0
								#ifdef WRITE_MSAA_DEPTH
							, out float1 depthColor : SV_Target1
								#endif
							#else
							, out float4 outColor : SV_Target0
							#endif

							#ifdef _DEPTHOFFSET_ON
							, out float outputDepth : SV_Depth
							#endif
							
						)
				{
					FragInputs input;
					ZERO_INITIALIZE(FragInputs, input);
					input.worldToTangent = k_identity3x3;
					input.positionSS = packedInput.positionCS;

					PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

					float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0

					SurfaceData surfaceData;
					BuiltinData builtinData;
					SurfaceDescription surfaceDescription = (SurfaceDescription)0;
					float temp_output_66_0 = (-0.65 + ((1.0 + (_Dissolveamount - 0.0) * (0.0 - 1.0) / (1.0 - 0.0)) - 0.0) * (0.65 - -0.65) / (1.0 - 0.0));
					float2 appendResult21 = (float2(_Speed.x , _Speed.y));
					float2 uv_MainTexture = packedInput.ase_texcoord.xy * _MainTexture_ST.xy + _MainTexture_ST.zw;
					float2 appendResult22 = (float2(_Speed.z , _Speed.w));
					float clampResult72 = clamp( (_RemapXYFresnelZW.x + (( ( temp_output_66_0 + tex2D( _MainTexture, ( ( appendResult21 * _Time.y ) + uv_MainTexture ) ).r ) * ( temp_output_66_0 + tex2D( _MainTexture, ( uv_MainTexture + ( _Time.y * appendResult22 ) ) ).r ) ) - 0.0) * (_RemapXYFresnelZW.y - _RemapXYFresnelZW.x) / (1.0 - 0.0)) , 0.0 , 1.0 );
					float2 appendResult74 = (float2((1.0 + (clampResult72 - 0.0) * (0.0 - 1.0) / (1.0 - 0.0)) , 0.0));
					float clampResult75 = clamp( tex2D( _Mask, appendResult74 ).r , 0.0 , 1.0 );
					float3 ase_worldPos = packedInput.ase_texcoord1.xyz;
					float3 ase_worldViewDir = ( _WorldSpaceCameraPos.xyz - ase_worldPos );
					ase_worldViewDir = normalize(ase_worldViewDir);
					float3 ase_worldNormal = packedInput.ase_texcoord2.xyz;
					float fresnelNdotV83 = dot( ase_worldNormal, ase_worldViewDir );
					float fresnelNode83 = ( 0.0 + 1.0 * pow( 1.0 - fresnelNdotV83, _RemapXYFresnelZW.z ) );
					float clampResult78 = clamp( ( _RemapXYFresnelZW.w * fresnelNode83 ) , 0.0 , 1.0 );
					float temp_output_61_0 = ( clampResult75 * _Color.a * packedInput.ase_color.a * clampResult78 * _Opacity );
					float4 screenPos = packedInput.ase_texcoord3;
					float4 ase_screenPosNorm = screenPos / screenPos.w;
					ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
					float screenDepth49 = LinearEyeDepth( SampleCameraDepth( screenPos.xy/screenPos.w ).r,_ZBufferParams);
					float distanceDepth49 = abs( ( screenDepth49 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _Depthpower ) );
					float clampResult53 = clamp( distanceDepth49 , 0.0 , 1.0 );
					#ifdef _USEDEPTH_ON
					float staticSwitch47 = ( temp_output_61_0 * clampResult53 );
					#else
					float staticSwitch47 = temp_output_61_0;
					#endif
					
					surfaceDescription.Alpha = staticSwitch47;
					surfaceDescription.AlphaClipThreshold =  0;

					GetSurfaceAndBuiltinData(surfaceDescription, input, V, posInput, surfaceData, builtinData);

				#ifdef _DEPTHOFFSET_ON
					outputDepth = posInput.deviceDepth;
				#endif

				#ifdef WRITE_NORMAL_BUFFER
					EncodeIntoNormalBuffer(ConvertSurfaceDataToNormalData(surfaceData), posInput.positionSS, outNormalBuffer);
					#ifdef WRITE_MSAA_DEPTH
					// In case we are rendering in MSAA, reading the an MSAA depth buffer is way too expensive. To avoid that, we export the depth to a color buffer
					depthColor = packedInput.positionCS.z;
					#endif
				#elif defined(SCENESELECTIONPASS)
					// We use depth prepass for scene selection in the editor, this code allow to output the outline correctly
					outColor = float4(_ObjectId, _PassValue, 1.0, 1.0);
				#else
					outColor = float4(0.0, 0.0, 0.0, 0.0);
				#endif
				}
        
            ENDHLSL
        }
		
        Pass
        {
			
            Name "Forward Unlit"
            Tags { "LightMode"="ForwardOnly" }
        
            ColorMask RGBA
			
            HLSLPROGRAM
        
				#pragma vertex Vert
				#pragma fragment Frag

				#define _SURFACE_TYPE_TRANSPARENT 1
				#define _BLENDMODE_ALPHA 1
				#pragma shader_feature _USEDEPTH_ON

        
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Wind.hlsl"
        
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

                #define SHADERPASS SHADERPASS_FORWARD_UNLIT
                #pragma multi_compile _ LIGHTMAP_ON
                #pragma multi_compile _ DIRLIGHTMAP_COMBINED
                #pragma multi_compile _ DYNAMICLIGHTMAP_ON
        
        
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
		        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"
	        
				struct AttributesMesh 
				{
					float3 positionOS : POSITION;
					float4 normalOS : NORMAL;
					float4 ase_texcoord : TEXCOORD0;
					float4 ase_color : COLOR;
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC;
					#endif
				};

				struct PackedVaryingsMeshToPS 
				{
					float4 positionCS : SV_Position;
					float4 ase_texcoord : TEXCOORD0;
					float4 ase_color : COLOR;
					float4 ase_texcoord1 : TEXCOORD1;
					float4 ase_texcoord2 : TEXCOORD2;
					float4 ase_texcoord3 : TEXCOORD3;
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC; 
					#endif 
				};

				CBUFFER_START(UnityPerMaterial)
				sampler2D _Mask;
				float _Dissolveamount;
				sampler2D _MainTexture;
				float4 _Speed;
				float4 _MainTexture_ST;
				float4 _RemapXYFresnelZW;
				float4 _Color;
				float _Emission;
				float _Opacity;
				float _Depthpower;
				CBUFFER_END
				                
		            
				struct SurfaceDescription
				{
					float3 Color;
					float Alpha;
					float AlphaClipThreshold;
				};
        
		
				void BuildSurfaceData(FragInputs fragInputs, SurfaceDescription surfaceDescription, float3 V, out SurfaceData surfaceData)
				{
					ZERO_INITIALIZE(SurfaceData, surfaceData);
					surfaceData.color = surfaceDescription.Color;
				}
        
				void GetSurfaceAndBuiltinData(SurfaceDescription surfaceDescription , FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
				{
				#if _ALPHATEST_ON
					DoAlphaTest ( surfaceDescription.Alpha, surfaceDescription.AlphaClipThreshold );
				#endif
					BuildSurfaceData(fragInputs, surfaceDescription, V, surfaceData);
					ZERO_INITIALIZE(BuiltinData, builtinData); 
					builtinData.opacity = surfaceDescription.Alpha;
					builtinData.distortion = float2(0.0, 0.0); 
					builtinData.distortionBlur = 0.0;
				}
        
         
				PackedVaryingsMeshToPS Vert(AttributesMesh inputMesh )
				{
					PackedVaryingsMeshToPS outputPackedVaryingsMeshToPS;
					UNITY_SETUP_INSTANCE_ID(inputMesh);
					UNITY_TRANSFER_INSTANCE_ID(inputMesh, outputPackedVaryingsMeshToPS);

					float3 ase_worldPos = GetAbsolutePositionWS( TransformObjectToWorld( (inputMesh.positionOS).xyz ) );
					outputPackedVaryingsMeshToPS.ase_texcoord1.xyz = ase_worldPos;
					float3 ase_worldNormal = TransformObjectToWorldNormal(inputMesh.normalOS.xyz);
					outputPackedVaryingsMeshToPS.ase_texcoord2.xyz = ase_worldNormal;
					
					float4 ase_clipPos = TransformWorldToHClip( TransformObjectToWorld(inputMesh.positionOS));
					float4 screenPos = ComputeScreenPos( ase_clipPos , _ProjectionParams.x );
					outputPackedVaryingsMeshToPS.ase_texcoord3 = screenPos;
					
					outputPackedVaryingsMeshToPS.ase_texcoord.xy = inputMesh.ase_texcoord.xy;
					outputPackedVaryingsMeshToPS.ase_color = inputMesh.ase_color;
					
					//setting value to unused interpolator channels and avoid initialization warnings
					outputPackedVaryingsMeshToPS.ase_texcoord.zw = 0;
					outputPackedVaryingsMeshToPS.ase_texcoord1.w = 0;
					outputPackedVaryingsMeshToPS.ase_texcoord2.w = 0;
					inputMesh.positionOS.xyz +=  float3( 0, 0, 0 ) ;
					inputMesh.normalOS =  inputMesh.normalOS ;

					float3 positionRWS = TransformObjectToWorld(inputMesh.positionOS);
					outputPackedVaryingsMeshToPS.positionCS = TransformWorldToHClip(positionRWS);
					return outputPackedVaryingsMeshToPS;
				}

				float4 Frag(PackedVaryingsMeshToPS packedInput) : SV_Target
				{
					
					FragInputs input;
					ZERO_INITIALIZE(FragInputs, input);
					input.worldToTangent = k_identity3x3;
					input.positionSS = packedInput.positionCS;
				
					PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

					float3 V = float3(1.0, 1.0, 1.0);

					SurfaceData surfaceData;
					BuiltinData builtinData;
					SurfaceDescription surfaceDescription = (SurfaceDescription)0;
					float temp_output_66_0 = (-0.65 + ((1.0 + (_Dissolveamount - 0.0) * (0.0 - 1.0) / (1.0 - 0.0)) - 0.0) * (0.65 - -0.65) / (1.0 - 0.0));
					float2 appendResult21 = (float2(_Speed.x , _Speed.y));
					float2 uv_MainTexture = packedInput.ase_texcoord.xy * _MainTexture_ST.xy + _MainTexture_ST.zw;
					float2 appendResult22 = (float2(_Speed.z , _Speed.w));
					float clampResult72 = clamp( (_RemapXYFresnelZW.x + (( ( temp_output_66_0 + tex2D( _MainTexture, ( ( appendResult21 * _Time.y ) + uv_MainTexture ) ).r ) * ( temp_output_66_0 + tex2D( _MainTexture, ( uv_MainTexture + ( _Time.y * appendResult22 ) ) ).r ) ) - 0.0) * (_RemapXYFresnelZW.y - _RemapXYFresnelZW.x) / (1.0 - 0.0)) , 0.0 , 1.0 );
					float2 appendResult74 = (float2((1.0 + (clampResult72 - 0.0) * (0.0 - 1.0) / (1.0 - 0.0)) , 0.0));
					float clampResult75 = clamp( tex2D( _Mask, appendResult74 ).r , 0.0 , 1.0 );
					float3 ase_worldPos = packedInput.ase_texcoord1.xyz;
					float3 ase_worldViewDir = ( _WorldSpaceCameraPos.xyz - ase_worldPos );
					ase_worldViewDir = normalize(ase_worldViewDir);
					float3 ase_worldNormal = packedInput.ase_texcoord2.xyz;
					float fresnelNdotV83 = dot( ase_worldNormal, ase_worldViewDir );
					float fresnelNode83 = ( 0.0 + 1.0 * pow( 1.0 - fresnelNdotV83, _RemapXYFresnelZW.z ) );
					float clampResult78 = clamp( ( _RemapXYFresnelZW.w * fresnelNode83 ) , 0.0 , 1.0 );
					
					float temp_output_61_0 = ( clampResult75 * _Color.a * packedInput.ase_color.a * clampResult78 * _Opacity );
					float4 screenPos = packedInput.ase_texcoord3;
					float4 ase_screenPosNorm = screenPos / screenPos.w;
					ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
					float screenDepth49 = LinearEyeDepth( SampleCameraDepth( screenPos.xy/screenPos.w ).r,_ZBufferParams);
					float distanceDepth49 = abs( ( screenDepth49 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _Depthpower ) );
					float clampResult53 = clamp( distanceDepth49 , 0.0 , 1.0 );
					#ifdef _USEDEPTH_ON
					float staticSwitch47 = ( temp_output_61_0 * clampResult53 );
					#else
					float staticSwitch47 = temp_output_61_0;
					#endif
					
					surfaceDescription.Color =  ( clampResult75 * _Color * packedInput.ase_color * clampResult78 * _Emission ).rgb;
					surfaceDescription.Alpha = staticSwitch47;
					surfaceDescription.AlphaClipThreshold =  0;

					GetSurfaceAndBuiltinData(surfaceDescription, input, V, posInput, surfaceData, builtinData);

					BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);

					float4 outColor = ApplyBlendMode(bsdfData.color + builtinData.emissiveColor, builtinData.opacity);
					outColor = EvaluateAtmosphericScattering(posInput, V, outColor);

					return outColor;
				}

            ENDHLSL
        }

		
		Pass
		{
			
            Name "META"
            Tags { "LightMode"="Meta" }
        
            Cull Off
        
            HLSLPROGRAM
        
				#pragma vertex Vert
				#pragma fragment Frag
        
				#define _SURFACE_TYPE_TRANSPARENT 1
				#define _BLENDMODE_ALPHA 1
				#pragma shader_feature _USEDEPTH_ON

				
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Wind.hlsl"
        
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
        
                #define SHADERPASS SHADERPASS_LIGHT_TRANSPORT
        
                #define ATTRIBUTES_NEED_NORMAL
                #define ATTRIBUTES_NEED_TANGENT
                #define ATTRIBUTES_NEED_TEXCOORD0
                #define ATTRIBUTES_NEED_TEXCOORD1
                #define ATTRIBUTES_NEED_TEXCOORD2
                #define ATTRIBUTES_NEED_COLOR
        
        
			    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
                #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
				#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"

				struct AttributesMesh
				{
					float3 positionOS : POSITION;
					float3 normalOS : NORMAL;
					float4 tangentOS : TANGENT;
					float4 uv0 : TEXCOORD0;
					float4 uv1 : TEXCOORD1;
					float4 uv2 : TEXCOORD2;
					float4 color : COLOR;
					
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC;
					#endif
				};
        
				struct PackedVaryingsMeshToPS
				{
					float4 positionCS : SV_Position;
					float4 ase_texcoord : TEXCOORD0;
					float4 ase_color : COLOR;
					float4 ase_texcoord1 : TEXCOORD1;
					float4 ase_texcoord2 : TEXCOORD2;
					float4 ase_texcoord3 : TEXCOORD3;
					#if UNITY_ANY_INSTANCING_ENABLED
					uint instanceID : INSTANCEID_SEMANTIC;
					#endif
				};

                CBUFFER_START(UnityPerMaterial)
				sampler2D _Mask;
				float _Dissolveamount;
				sampler2D _MainTexture;
				float4 _Speed;
				float4 _MainTexture_ST;
				float4 _RemapXYFresnelZW;
				float4 _Color;
				float _Opacity;
				float _Depthpower;
				CBUFFER_END
				                
                struct SurfaceDescription
                {
                    float3 Color;
                    float Alpha;
                    float AlphaClipThreshold;
                };
                    
				void BuildSurfaceData(FragInputs fragInputs, SurfaceDescription surfaceDescription, float3 V, out SurfaceData surfaceData)
				{
					ZERO_INITIALIZE(SurfaceData, surfaceData);
					surfaceData.color = surfaceDescription.Color;
				}
        
				void GetSurfaceAndBuiltinData(SurfaceDescription surfaceDescription, FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
				{
       
				#if _ALPHATEST_ON
					DoAlphaTest(surfaceDescription.Alpha, surfaceDescription.AlphaClipThreshold);
				#endif

					BuildSurfaceData(fragInputs, surfaceDescription, V, surfaceData);
					ZERO_INITIALIZE(BuiltinData, builtinData);
					builtinData.opacity = surfaceDescription.Alpha;
					builtinData.distortion = float2(0.0, 0.0);
					builtinData.distortionBlur = 0.0;
				}
       
				CBUFFER_START(UnityMetaPass)
				bool4 unity_MetaVertexControl;
				bool4 unity_MetaFragmentControl;
				CBUFFER_END

				float unity_OneOverOutputBoost;
				float unity_MaxOutputValue;

				PackedVaryingsMeshToPS Vert(AttributesMesh inputMesh   )
				{
					PackedVaryingsMeshToPS outputPackedVaryingsMeshToPS;

					UNITY_SETUP_INSTANCE_ID(inputMesh);
					UNITY_TRANSFER_INSTANCE_ID(inputMesh, outputPackedVaryingsMeshToPS);

					float3 ase_worldPos = GetAbsolutePositionWS( TransformObjectToWorld( (inputMesh.positionOS).xyz ) );
					outputPackedVaryingsMeshToPS.ase_texcoord1.xyz = ase_worldPos;
					float3 ase_worldNormal = TransformObjectToWorldNormal(inputMesh.normalOS);
					outputPackedVaryingsMeshToPS.ase_texcoord2.xyz = ase_worldNormal;
					float4 ase_clipPos = TransformWorldToHClip( TransformObjectToWorld(inputMesh.positionOS));
					float4 screenPos = ComputeScreenPos( ase_clipPos , _ProjectionParams.x );
					outputPackedVaryingsMeshToPS.ase_texcoord3 = screenPos;
					
					outputPackedVaryingsMeshToPS.ase_texcoord.xy = inputMesh.uv0.xy;
					outputPackedVaryingsMeshToPS.ase_color = inputMesh.color;
					
					//setting value to unused interpolator channels and avoid initialization warnings
					outputPackedVaryingsMeshToPS.ase_texcoord.zw = 0;
					outputPackedVaryingsMeshToPS.ase_texcoord1.w = 0;
					outputPackedVaryingsMeshToPS.ase_texcoord2.w = 0;
					inputMesh.positionOS.xyz +=  float3( 0, 0, 0 ) ;
					inputMesh.normalOS =  inputMesh.normalOS ;

					float2 uv;

					if (unity_MetaVertexControl.x)
					{
						uv = inputMesh.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
					}
					else if (unity_MetaVertexControl.y)
					{
						uv = inputMesh.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
					}

					outputPackedVaryingsMeshToPS.positionCS = float4(uv * 2.0 - 1.0, inputMesh.positionOS.z > 0 ? 1.0e-4 : 0.0, 1.0);
					return outputPackedVaryingsMeshToPS;
				}

				float4 Frag(PackedVaryingsMeshToPS packedInput) : SV_Target
				{			
					FragInputs input;
					ZERO_INITIALIZE(FragInputs, input);
					input.worldToTangent = k_identity3x3;
					input.positionSS = packedInput.positionCS;
                
				
					PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

					float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
		
					SurfaceData surfaceData;
					BuiltinData builtinData;
					SurfaceDescription surfaceDescription = (SurfaceDescription)0;
					float temp_output_66_0 = (-0.65 + ((1.0 + (_Dissolveamount - 0.0) * (0.0 - 1.0) / (1.0 - 0.0)) - 0.0) * (0.65 - -0.65) / (1.0 - 0.0));
					float2 appendResult21 = (float2(_Speed.x , _Speed.y));
					float2 uv_MainTexture = packedInput.ase_texcoord.xy * _MainTexture_ST.xy + _MainTexture_ST.zw;
					float2 appendResult22 = (float2(_Speed.z , _Speed.w));
					float clampResult72 = clamp( (_RemapXYFresnelZW.x + (( ( temp_output_66_0 + tex2D( _MainTexture, ( ( appendResult21 * _Time.y ) + uv_MainTexture ) ).r ) * ( temp_output_66_0 + tex2D( _MainTexture, ( uv_MainTexture + ( _Time.y * appendResult22 ) ) ).r ) ) - 0.0) * (_RemapXYFresnelZW.y - _RemapXYFresnelZW.x) / (1.0 - 0.0)) , 0.0 , 1.0 );
					float2 appendResult74 = (float2((1.0 + (clampResult72 - 0.0) * (0.0 - 1.0) / (1.0 - 0.0)) , 0.0));
					float clampResult75 = clamp( tex2D( _Mask, appendResult74 ).r , 0.0 , 1.0 );
					float3 ase_worldPos = packedInput.ase_texcoord1.xyz;
					float3 ase_worldViewDir = ( _WorldSpaceCameraPos.xyz - ase_worldPos );
					ase_worldViewDir = normalize(ase_worldViewDir);
					float3 ase_worldNormal = packedInput.ase_texcoord2.xyz;
					float fresnelNdotV83 = dot( ase_worldNormal, ase_worldViewDir );
					float fresnelNode83 = ( 0.0 + 1.0 * pow( 1.0 - fresnelNdotV83, _RemapXYFresnelZW.z ) );
					float clampResult78 = clamp( ( _RemapXYFresnelZW.w * fresnelNode83 ) , 0.0 , 1.0 );
					float temp_output_61_0 = ( clampResult75 * _Color.a * packedInput.ase_color.a * clampResult78 * _Opacity );
					float4 screenPos = packedInput.ase_texcoord3;
					float4 ase_screenPosNorm = screenPos / screenPos.w;
					ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
					float screenDepth49 = LinearEyeDepth( SampleCameraDepth( screenPos.xy/screenPos.w ).r,_ZBufferParams);
					float distanceDepth49 = abs( ( screenDepth49 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _Depthpower ) );
					float clampResult53 = clamp( distanceDepth49 , 0.0 , 1.0 );
					#ifdef _USEDEPTH_ON
					float staticSwitch47 = ( temp_output_61_0 * clampResult53 );
					#else
					float staticSwitch47 = temp_output_61_0;
					#endif
					
					surfaceDescription.Color =  float3( 1, 1, 1 );
					surfaceDescription.Alpha = staticSwitch47;
					surfaceDescription.AlphaClipThreshold =  0;

					GetSurfaceAndBuiltinData(surfaceDescription,input, V, posInput, surfaceData, builtinData);
					BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);
					LightTransportData lightTransportData = GetLightTransportData(surfaceData, builtinData, bsdfData);
					float4 res = float4(0.0, 0.0, 0.0, 1.0);
					if (unity_MetaFragmentControl.x)
					{
						res.rgb = clamp(pow(abs(lightTransportData.diffuseColor), saturate(unity_OneOverOutputBoost)), 0, unity_MaxOutputValue);
					}

					if (unity_MetaFragmentControl.y)
					{
						res.rgb = lightTransportData.emissiveColor;
					}

					return res;
				}

            ENDHLSL
		}
    }
    FallBack "Hidden/InternalErrorShader"
}