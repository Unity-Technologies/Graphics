Shader "3af7e7b7-0e1c-4b85-9b74-f027f0cdccb4_preview"
{
	Properties
	{
				[HideInInspector] [NonModifiableTextureData] [NoScaleOffset] Texture_FD0EDC2E("Texture", 2D) = "white" {}
	}
	CGINCLUDE
	#include "UnityCG.cginc"
			void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA)
			{
			    RGBA = float4(R, G, B, A);
			}
			void Unity_Multiply_float(float4 first, float4 second, out float4 result)
			{
			    result = first * second;
			}
			void Unity_HSVToRGB_float(float3 hsv, out float3 rgb)
			{
			    //Reference code from:https://github.com/Unity-Technologies/PostProcessing/blob/master/PostProcessing/Resources/Shaders/ColorGrading.cginc#L175
			    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
			    float3 P = abs(frac(hsv.xxx + K.xyz) * 6.0 - K.www);
			    rgb = hsv.z * lerp(K.xxx, saturate(P - K.xxx), hsv.y);
			}
			void Unity_Subtract_float(float first, float second, out float result)
			{
			    result = first - second;
			}
			void Unity_Multiply_float(float first, float second, out float result)
			{
			    result = first * second;
			}
			void Unity_OneMinus_float(float argument, out float result)
			{
			    result = argument * -1 + 1;;
			}
			void Unity_Multiply_float(float3 first, float3 second, out float3 result)
			{
			    result = first * second;
			}
			void Unity_Add_float(float3 first, float3 second, out float3 result)
			{
			    result = first + second;
			}
	struct GraphVertexInput
	{
	     float4 vertex : POSITION;
	     float3 normal : NORMAL;
	     float4 tangent : TANGENT;
	     float4 texcoord0 : TEXCOORD0;
	     float4 lightmapUV : TEXCOORD1;
	     UNITY_VERTEX_INPUT_INSTANCE_ID
	};
			struct SurfaceInputs{
				half4 uv0;
			};
			struct SurfaceDescription{
				float3 Albedo;
				float3 Normal;
				float3 Emission;
				float Metallic;
				float Smoothness;
				float Occlusion;
				float Alpha;
			};
			UNITY_DECLARE_TEX2D(Texture_FD0EDC2E);
			float Combine_76C873_G;
			float Combine_76C873_B;
			float Combine_76C873_A;
			float4 Property_23AD7FD4_UV;
			float Subtract_4053AE6C_first;
			float Combine_EBDC57C5_G;
			float Combine_EBDC57C5_B;
			float Combine_EBDC57C5_A;
			float4 Multiply_F77E48F9_second;
			float4 Multiply_156218A9_second;
			float4 LightweightMetallicMasterNode_53B6BCE4_Normal;
			float4 LightweightMetallicMasterNode_53B6BCE4_Emission;
			float LightweightMetallicMasterNode_53B6BCE4_Metallic;
			float LightweightMetallicMasterNode_53B6BCE4_Smoothness;
			float LightweightMetallicMasterNode_53B6BCE4_Occlusion;
			float LightweightMetallicMasterNode_53B6BCE4_Alpha;
			GraphVertexInput PopulateVertexData(GraphVertexInput v){
				return v;
			}
			SurfaceDescription PopulateSurfaceData(SurfaceInputs IN) {
				half4 uv0 = IN.uv0;
				float4 Combine_76C873_RGBA;
				Unity_Combine_float(_Time.y, Combine_76C873_G, Combine_76C873_B, Combine_76C873_A, Combine_76C873_RGBA);
				float4 Property_23AD7FD4_RGBA = UNITY_SAMPLE_TEX2D(Texture_FD0EDC2E,uv0.xy);
				float Property_23AD7FD4_R = Property_23AD7FD4_RGBA.r;
				float Property_23AD7FD4_G = Property_23AD7FD4_RGBA.g;
				float Property_23AD7FD4_B = Property_23AD7FD4_RGBA.b;
				float Property_23AD7FD4_A = Property_23AD7FD4_RGBA.a;
				float4 Multiply_89C71AC1_result;
				Unity_Multiply_float(Property_23AD7FD4_RGBA, Combine_76C873_RGBA, Multiply_89C71AC1_result);
				float3 HSVtoRGB_AE8D0A0E_rgb;
				Unity_HSVToRGB_float((Multiply_89C71AC1_result.xyz), HSVtoRGB_AE8D0A0E_rgb);
				float Split_81BA869A_R = HSVtoRGB_AE8D0A0E_rgb[0];
				float Split_81BA869A_G = HSVtoRGB_AE8D0A0E_rgb[1];
				float Split_81BA869A_B = HSVtoRGB_AE8D0A0E_rgb[2];
				float Split_81BA869A_A = 1.0;
				float Subtract_4053AE6C_result;
				Unity_Subtract_float(Subtract_4053AE6C_first, _Time.y, Subtract_4053AE6C_result);
				float Multiply_9164EC34_result;
				Unity_Multiply_float(Split_81BA869A_R, Split_81BA869A_G, Multiply_9164EC34_result);
				float4 Combine_EBDC57C5_RGBA;
				Unity_Combine_float(Subtract_4053AE6C_result, Combine_EBDC57C5_G, Combine_EBDC57C5_B, Combine_EBDC57C5_A, Combine_EBDC57C5_RGBA);
				float Multiply_841C266E_result;
				Unity_Multiply_float(Multiply_9164EC34_result, Split_81BA869A_B, Multiply_841C266E_result);
				float3 HSVtoRGB_6DD0292E_rgb;
				Unity_HSVToRGB_float((Combine_EBDC57C5_RGBA.xyz), HSVtoRGB_6DD0292E_rgb);
				float OneMinus_F47415CA_result;
				Unity_OneMinus_float(Multiply_841C266E_result, OneMinus_F47415CA_result);
				float3 Multiply_E8E6BAEA_result;
				Unity_Multiply_float((OneMinus_F47415CA_result.xxx), HSVtoRGB_6DD0292E_rgb, Multiply_E8E6BAEA_result);
				float3 Multiply_F77E48F9_result;
				Unity_Multiply_float(HSVtoRGB_AE8D0A0E_rgb, Multiply_F77E48F9_second, Multiply_F77E48F9_result);
				float3 Multiply_156218A9_result;
				Unity_Multiply_float(Multiply_E8E6BAEA_result, Multiply_156218A9_second, Multiply_156218A9_result);
				float3 Add_A93EC3AF_result;
				Unity_Add_float(Multiply_156218A9_result, Multiply_F77E48F9_result, Add_A93EC3AF_result);
				SurfaceDescription surface = (SurfaceDescription)0;
				surface.Albedo = Add_A93EC3AF_result;
				surface.Normal = LightweightMetallicMasterNode_53B6BCE4_Normal;
				surface.Emission = LightweightMetallicMasterNode_53B6BCE4_Emission;
				surface.Metallic = LightweightMetallicMasterNode_53B6BCE4_Metallic;
				surface.Smoothness = LightweightMetallicMasterNode_53B6BCE4_Smoothness;
				surface.Occlusion = LightweightMetallicMasterNode_53B6BCE4_Occlusion;
				surface.Alpha = LightweightMetallicMasterNode_53B6BCE4_Alpha;
				return surface;
			}
	ENDCG
	SubShader
	{
		Tags{"RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline"}
		LOD 200
		
		Pass
		{
			Tags{"LightMode" = "LightweightForward"}
					Tags
			{
				"RenderType"="Opaque"
				"Queue"="Geometry"
			}
					Blend One Zero
					Cull Back
					ZTest LEqual
					ZWrite On
			
			CGPROGRAM
			#pragma target 3.0
			
			#pragma multi_compile _ _SINGLE_DIRECTIONAL_LIGHT _SINGLE_SPOT_LIGHT _SINGLE_POINT_LIGHT
			#pragma multi_compile _ LIGHTWEIGHT_LINEAR
			#pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile _ _LIGHT_PROBES_ON
			#pragma multi_compile _ _HARD_SHADOWS _SOFT_SHADOWS _HARD_SHADOWS_CASCADES _SOFT_SHADOWS_CASCADES
			#pragma multi_compile _ _VERTEX_LIGHTS
			#pragma multi_compile_fog
			#pragma multi_compile_instancing
	        #pragma vertex vert
			#pragma fragment frag
			#pragma glsl
			#pragma debug
			
						#define _GLOSSYREFLECTIONS_ON
				#define _SPECULARHIGHLIGHTS_ON
				#define _METALLIC_SETUP 1
			#include "UnityCG.cginc"
			#include "CGIncludes/LightweightPBR.cginc"
			
			struct GraphVertexOutput
	        {
	            float4 position : POSITION;
	            float4 lwCustom : TEXCOORD0;
				float4 fogCoord : TEXCOORD1; // x: fogCoord, yzw: vertexColor
	            			float3 objectSpaceNormal : NORMAL;
				float4 objectSpaceTangent : TANGENT;
				float3 objectSpaceViewDirection : TEXCOORD2;
				float4 objectSpacePosition : TEXCOORD3;
				half4 uv0 : TEXCOORD4;
				UNITY_VERTEX_OUTPUT_STEREO
	        };
			
	        GraphVertexOutput vert (GraphVertexInput v)
			{
			    v = PopulateVertexData(v);
				
				UNITY_SETUP_INSTANCE_ID(v);
	            GraphVertexOutput o = (GraphVertexOutput)0;
	            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
	            o.position = UnityObjectToClipPos(v.vertex);
	            			o.objectSpaceNormal = v.normal;
				o.objectSpaceTangent = v.tangent;
				o.objectSpaceViewDirection = ObjSpaceViewDir(v.vertex);
				o.objectSpacePosition = v.vertex;
				o.uv0 = v.texcoord0;
			#ifdef LIGHTMAP_ON
				o.lwCustom.zw = v.lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
			#endif
				float3 lwWNormal = normalize(UnityObjectToWorldNormal(v.normal));
				float3 lwWorldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				// TODO: change to only support point lights per vertex. This will greatly simplify shader ALU
			#if defined(_VERTEX_LIGHTS) && defined(_MULTIPLE_LIGHTS)
				half3 diffuse = half3(1.0, 1.0, 1.0);
				// pixel lights shaded = min(pixelLights, perObjectLights)
				// vertex lights shaded = min(vertexLights, perObjectLights) - pixel lights shaded
				// Therefore vertexStartIndex = pixelLightCount;  vertexEndIndex = min(vertexLights, perObjectLights)
				int vertexLightStart = min(globalLightCount.x, unity_LightIndicesOffsetAndCount.y);
				int vertexLightEnd = min(globalLightCount.y, unity_LightIndicesOffsetAndCount.y);
				for (int lightIter = vertexLightStart; lightIter < vertexLightEnd; ++lightIter)
				{
					int lightIndex = unity_4LightIndices0[lightIter];
					LightInput lightInput;
					INITIALIZE_LIGHT(lightInput, lightIndex);
					half3 lightDirection;
					half atten = ComputeLightAttenuationVertex(lightInput, lwWNormal, lwWorldPos, lightDirection);
					o.fogCoord.yzw += LightingLambert(diffuse, lightDirection, lwWNormal, atten);
				}
			#endif
			#if defined(_LIGHT_PROBES_ON) && !defined(LIGHTMAP_ON)
				o.fogCoord.yzw += max(half3(0, 0, 0), ShadeSH9(half4(lwWNormal, 1)));
			#endif
				UNITY_TRANSFER_FOG(o, o.position);
				return o;
			}
		
			fixed4 frag (GraphVertexOutput IN) : SV_Target
	        {
	        				float3 objectSpaceNormal = normalize(IN.objectSpaceNormal);
				float4 objectSpaceTangent = IN.objectSpaceTangent;
				float3 objectSpaceBiTangent = normalize(cross(normalize(IN.objectSpaceNormal), normalize(IN.objectSpaceTangent.xyz)) * IN.objectSpaceTangent.w);
				float3 objectSpaceViewDirection = normalize(IN.objectSpaceViewDirection);
				float4 objectSpacePosition = IN.objectSpacePosition;
				float3 worldSpaceNormal = UnityObjectToWorldNormal(objectSpaceNormal);
				float3 worldSpaceTangent = UnityObjectToWorldDir(objectSpaceTangent);
				float3 worldSpaceBiTangent = UnityObjectToWorldDir(objectSpaceBiTangent);
				float3 worldSpaceViewDirection = UnityObjectToWorldDir(objectSpaceViewDirection);
				float3 worldSpacePosition = UnityObjectToWorldDir(objectSpacePosition);
				float4 uv0  = IN.uv0;
	            SurfaceInputs surfaceInput = (SurfaceInputs)0;
	            			surfaceInput.uv0  =uv0;
	            SurfaceDescription surf = PopulateSurfaceData(surfaceInput);
				float3 Albedo = float3(0.5, 0.5, 0.5);
				float3 Specular = float3(0, 0, 0);
				float Metallic = 0;
				float3 Normal = float3(0, 0, 1);
				float3 Emission = 0;
				float Smoothness = 0;
				float Occlusion = 1;
				float Alpha = 1;
	            			Albedo = surf.Albedo;
				Normal = surf.Normal;
				Emission = surf.Emission;
				Metallic = surf.Metallic;
				Smoothness = surf.Smoothness;
				Occlusion = surf.Occlusion;
				Alpha = surf.Alpha;
	#if defined(UNITY_COLORSPACE_GAMMA) 
	           	Albedo = Albedo * Albedo;
	           	Emission = Emission * Emission;
	#endif
				return FragmentLightingPBR(
					IN.lwCustom,
					worldSpacePosition,
					worldSpaceNormal,
					worldSpaceTangent,
					worldSpaceBiTangent,
					worldSpaceViewDirection,
					IN.fogCoord, 
					
					Albedo,
					Metallic,
					Specular,
					Smoothness,
					Normal,
					Occlusion,
					Emission,
					Alpha);
	        }
		
			ENDCG
		}
		
		Pass
		{
			Tags{"Lightmode" = "ShadowCaster"}
			ZWrite On ZTest LEqual
			CGPROGRAM
			#pragma target 2.0
			#include "UnityCG.cginc"
			#include "CGIncludes/LightweightPass.cginc"
			#pragma vertex shadowVert
			#pragma fragment shadowFrag
			ENDCG
		}
		Pass
		{
			Tags{"Lightmode" = "DepthOnly"}
			ZWrite On
			CGPROGRAM
			#pragma target 2.0
			#include "UnityCG.cginc"
			#include "CGIncludes/LightweightPass.cginc"
			#pragma vertex depthVert
			#pragma fragment depthFrag
			ENDCG
		}
	}
}
