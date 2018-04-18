Shader "DDX"
{
	Properties
	{
	}
	SubShader
	{
		Tags{ "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline"}
		Tags
		{
			"RenderType"="Opaque"
			"Queue"="Geometry"
		}
		
		Pass
		{
			Tags{"LightMode" = "LightweightForward"}
			
					Blend One Zero
		
					Cull Back
		
					ZTest LEqual
		
					ZWrite On
		
			
		    HLSLPROGRAM
		    // Required to compile gles 2.0 with standard srp library
		    #pragma prefer_hlslcc gles
		    #pragma exclude_renderers d3d11_9x
		
		    #pragma vertex vert
		    #pragma fragment frag
		    #pragma multi_compile_fog
		    #pragma shader_feature _SAMPLE_GI
		    #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON
		    #pragma multi_compile_instancing
		
			#pragma vertex vert
		    #pragma fragment frag
		
		    // Lighting include is needed because of GI
		    #include "LWRP/ShaderLibrary/Core.hlsl"
		    #include "LWRP/ShaderLibrary/Lighting.hlsl"
		    #include "CoreRP/ShaderLibrary/Color.hlsl"
		    #include "LWRP/ShaderLibrary/InputSurfaceUnlit.hlsl"
		    #include "ShaderGraphLibrary/Functions.hlsl"
		
		    
		
		    					struct SurfaceInputs{
								half4 uv0;
							};
					
					
					        void Unity_DDX_float4(float4 In, out float4 Out)
					        {
					            Out = ddx(In);
					        }
					
							struct GraphVertexInput
							{
								float4 vertex : POSITION;
								float3 normal : NORMAL;
								float4 tangent : TANGENT;
								float4 texcoord0 : TEXCOORD0;
								UNITY_VERTEX_INPUT_INSTANCE_ID
							};
					
							struct SurfaceDescription{
								float3 Color;
								float Alpha;
								float AlphaClipThreshold;
							};
					
							GraphVertexInput PopulateVertexData(GraphVertexInput v){
								return v;
							}
					
							SurfaceDescription PopulateSurfaceData(SurfaceInputs IN) {
								SurfaceDescription surface = (SurfaceDescription)0;
								float4 _UV_EA5BC76B_Out = IN.uv0;
								float4 _DDX_D57D2B05_Out;
								Unity_DDX_float4(_UV_EA5BC76B_Out, _DDX_D57D2B05_Out);
								surface.Color = (_DDX_D57D2B05_Out.xyz);
								surface.Alpha = 1;
								surface.AlphaClipThreshold = 0;
								return surface;
							}
					
		
		
		    struct GraphVertexOutput
		    {
		        float4 position : POSITION;
		        			half4 uv0 : TEXCOORD3;
		
		        UNITY_VERTEX_INPUT_INSTANCE_ID
		    };
		
		    GraphVertexOutput vert (GraphVertexInput v)
			{
			    v = PopulateVertexData(v);
				
		        GraphVertexOutput o = (GraphVertexOutput)0;
		        
		        UNITY_SETUP_INSTANCE_ID(v);
		        UNITY_TRANSFER_INSTANCE_ID(v, o);
		
		        o.position = TransformObjectToHClip(v.vertex.xyz);
		        			o.uv0 = v.texcoord0;
		
		        return o;
			}
		
		    half4 frag (GraphVertexOutput IN) : SV_Target
		    {
		        UNITY_SETUP_INSTANCE_ID(IN);
		
		    				float4 uv0 = IN.uv0;
		
		    	
		        SurfaceInputs surfaceInput = (SurfaceInputs)0;
		        			surfaceInput.uv0 = uv0;
		
		
		        SurfaceDescription surf = PopulateSurfaceData(surfaceInput);
		        float3 Color = float3(0.5, 0.5, 0.5);
		        float Alpha = 1;
		        float AlphaClipThreshold = 0;
							Color = surf.Color;
					Alpha = surf.Alpha;
					AlphaClipThreshold = surf.AlphaClipThreshold;
		
				
		#if _AlphaClip
		        clip(Alpha - AlphaClipThreshold);
		#endif
		    	return half4(Color, Alpha);
		    }
		    ENDHLSL
		}
		
		
		Pass
		{
			Tags{"LightMode" = "DepthOnly"}
		
			ZWrite On
			Cull Back
			ColorMask 0
		
			HLSLPROGRAM
			// Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0
		
			#pragma vertex DepthOnlyVertex
			#pragma fragment DepthOnlyFragment
		
			// -------------------------------------
			// Material Keywords
			#pragma shader_feature _ALPHATEST_ON
		
			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
		
			#include "LWRP/ShaderLibrary/InputSurfaceUnlit.hlsl"
			#include "LWRP/ShaderLibrary/LightweightPassDepthOnly.hlsl"
			ENDHLSL
		}
		
	}
	
	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		// Unlit shader always render in forward
		Pass
		{
		    Name "ForwardUnlit"
		    Tags { "LightMode" = "DepthForwardOnly" }
		
		    		Tags
				{
					"RenderType"="Opaque"
					"Queue"="Geometry"
				}
		
		    		Blend One Zero
		
		    		Cull Back
		
		    		ZTest LEqual
		
		    		ZWrite On
		
		
		    HLSLPROGRAM
		    
		    #pragma target 4.5
		    #pragma only_renderers d3d11 ps4 vulkan metal // TEMP: until we go further in dev
		    //#pragma enable_d3d11_debug_symbols
		
		    #pragma vertex Vert
		    #pragma fragment Frag
		
		    #define UNITY_MATERIAL_UNLIT // Need to be define before including Material.hlsl
		
		    #include "CoreRP/ShaderLibrary/common.hlsl"
		    #include "HDRP/ShaderVariables.hlsl"
		    #include "HDRP/ShaderPass/FragInputs.hlsl"
		    #include "HDRP/ShaderPass/ShaderPass.cs.hlsl"
		    #include "ShaderGraphLibrary/Functions.hlsl"
		
		    			#define SHADERPASS SHADERPASS_DEPTH_ONLY
					#define ATTRIBUTES_NEED_TEXCOORD0
					#define VARYINGS_NEED_TEXCOORD0
		
		    
		    #include "HDRP/Material/Material.hlsl"
		
		    // This include will define the various Attributes/Varyings structure
		    #include "HDRP/ShaderPass/VaryingMesh.hlsl"
		
		    					struct SurfaceInputs{
								half4 uv0;
							};
					
					
					        void Unity_DDX_float4(float4 In, out float4 Out)
					        {
					            Out = ddx(In);
					        }
					
							struct SurfaceDescription{
								float3 Color;
								float Alpha;
							};
					
							SurfaceDescription PopulateSurfaceData(SurfaceInputs IN) {
								SurfaceDescription surface = (SurfaceDescription)0;
								float4 _UV_EA5BC76B_Out = IN.uv0;
								float4 _DDX_D57D2B05_Out;
								Unity_DDX_float4(_UV_EA5BC76B_Out, _DDX_D57D2B05_Out);
								surface.Color = (_DDX_D57D2B05_Out.xyz);
								surface.Alpha = 1;
								return surface;
							}
					
		
		
		    void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
		    {
		        
		
		        SurfaceInputs surfaceInput;
		        			surfaceInput.uv0 = half4(input.texCoord0, 0, 0);
		
		
		        SurfaceDescription surf = PopulateSurfaceData(surfaceInput);
		        float3 Color = 0;
		        float Alpha = 0;
		        			Color = surf.Color;
					Alpha = surf.Alpha;
		
		        
		        surfaceData.color = Color;
		
		    #ifdef _ALPHATEST_ON
		        DoAlphaTest(Alpha, _AlphaCutoff);
		    #endif
		
		        // Builtin Data
		        builtinData.opacity = Alpha;
		        builtinData.bakeDiffuseLighting = float3(0.0, 0.0, 0.0);
		        builtinData.emissiveIntensity = 0; 
		        builtinData.emissiveColor = 0;
		        builtinData.velocity = float2(0.0, 0.0);
		        builtinData.shadowMask0 = 0.0;
		        builtinData.shadowMask1 = 0.0;
		        builtinData.shadowMask2 = 0.0;
		        builtinData.shadowMask3 = 0.0;
		        builtinData.distortion = float2(0.0, 0.0);
		        builtinData.distortionBlur = 0.0;
		        builtinData.depthOffset = 0.0;
		    }
		
		    #include "HDRP/ShaderPass/ShaderPassDepthOnly.hlsl"
		
		    ENDHLSL
		}
		// Unlit shader always render in forward
		Pass
		{
		    Name "ForwardUnlit"
		    Tags { "LightMode" = "ForwardOnly" }
		
		    		Tags
				{
					"RenderType"="Opaque"
					"Queue"="Geometry"
				}
		
		    		Blend One Zero
		
		    		Cull Back
		
		    		ZTest LEqual
		
		    		ZWrite On
		
		
		    HLSLPROGRAM
		    
		    #pragma target 4.5
		    #pragma only_renderers d3d11 ps4 vulkan metal // TEMP: until we go further in dev
		    //#pragma enable_d3d11_debug_symbols
		
		    #pragma vertex Vert
		    #pragma fragment Frag
		
		    #define UNITY_MATERIAL_UNLIT // Need to be define before including Material.hlsl
		
		    #include "CoreRP/ShaderLibrary/common.hlsl"
		    #include "HDRP/ShaderVariables.hlsl"
		    #include "HDRP/ShaderPass/FragInputs.hlsl"
		    #include "HDRP/ShaderPass/ShaderPass.cs.hlsl"
		    #include "ShaderGraphLibrary/Functions.hlsl"
		
		    			#define SHADERPASS SHADERPASS_FORWARD_UNLIT
					#define ATTRIBUTES_NEED_TEXCOORD0
					#define VARYINGS_NEED_TEXCOORD0
		
		    
		    #include "HDRP/Material/Material.hlsl"
		
		    // This include will define the various Attributes/Varyings structure
		    #include "HDRP/ShaderPass/VaryingMesh.hlsl"
		
		    					struct SurfaceInputs{
								half4 uv0;
							};
					
					
					        void Unity_DDX_float4(float4 In, out float4 Out)
					        {
					            Out = ddx(In);
					        }
					
							struct SurfaceDescription{
								float3 Color;
								float Alpha;
							};
					
							SurfaceDescription PopulateSurfaceData(SurfaceInputs IN) {
								SurfaceDescription surface = (SurfaceDescription)0;
								float4 _UV_EA5BC76B_Out = IN.uv0;
								float4 _DDX_D57D2B05_Out;
								Unity_DDX_float4(_UV_EA5BC76B_Out, _DDX_D57D2B05_Out);
								surface.Color = (_DDX_D57D2B05_Out.xyz);
								surface.Alpha = 1;
								return surface;
							}
					
		
		
		    void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
		    {
		        
		
		        SurfaceInputs surfaceInput;
		        			surfaceInput.uv0 = half4(input.texCoord0, 0, 0);
		
		
		        SurfaceDescription surf = PopulateSurfaceData(surfaceInput);
		        float3 Color = 0;
		        float Alpha = 0;
		        			Color = surf.Color;
					Alpha = surf.Alpha;
		
		        
		        surfaceData.color = Color;
		
		    #ifdef _ALPHATEST_ON
		        DoAlphaTest(Alpha, _AlphaCutoff);
		    #endif
		
		        // Builtin Data
		        builtinData.opacity = Alpha;
		        builtinData.bakeDiffuseLighting = float3(0.0, 0.0, 0.0);
		        builtinData.emissiveIntensity = 0; 
		        builtinData.emissiveColor = 0;
		        builtinData.velocity = float2(0.0, 0.0);
		        builtinData.shadowMask0 = 0.0;
		        builtinData.shadowMask1 = 0.0;
		        builtinData.shadowMask2 = 0.0;
		        builtinData.shadowMask3 = 0.0;
		        builtinData.distortion = float2(0.0, 0.0);
		        builtinData.distortionBlur = 0.0;
		        builtinData.depthOffset = 0.0;
		    }
		
		    #include "HDRP/ShaderPass/ShaderPassForwardUnlit.hlsl"
		
		    ENDHLSL
		}
	}
	
	FallBack "Hidden/InternalErrorShader"
}
