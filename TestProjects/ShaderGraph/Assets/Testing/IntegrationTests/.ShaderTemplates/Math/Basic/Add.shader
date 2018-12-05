Shader "Add"
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
		    #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
		    #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"
		    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
		    #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/InputSurfaceUnlit.hlsl"
		    #include "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl"
		
		    
		
		    					struct SurfaceInputs{
							};
					
					
					        void Unity_Add_float2(float2 A, float2 B, out float2 Out)
					        {
					            Out = A + B;
					        }
					
							struct GraphVertexInput
							{
								float4 vertex : POSITION;
								float3 normal : NORMAL;
								float4 tangent : TANGENT;
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
								float2 _Vector2_438531E7_Out = float2(0.2,0.4);
								float4 _Vector4_CFE730D7_Out = float4(0.6,0.5,0.7,1);
								float2 _Add_FCE490FF_Out;
								Unity_Add_float2(_Vector2_438531E7_Out, (_Vector4_CFE730D7_Out.xy), _Add_FCE490FF_Out);
								surface.Color = (float3(_Add_FCE490FF_Out, 0.0));
								surface.Alpha = 1;
								surface.AlphaClipThreshold = 0;
								return surface;
							}
					
		
		
		    struct GraphVertexOutput
		    {
		        float4 position : POSITION;
		        
		        UNITY_VERTEX_INPUT_INSTANCE_ID
		    };
		
		    GraphVertexOutput vert (GraphVertexInput v)
			{
			    v = PopulateVertexData(v);
				
		        GraphVertexOutput o = (GraphVertexOutput)0;
		        
		        UNITY_SETUP_INSTANCE_ID(v);
		        UNITY_TRANSFER_INSTANCE_ID(v, o);
		
		        o.position = TransformObjectToHClip(v.vertex.xyz);
		        
		        return o;
			}
		
		    half4 frag (GraphVertexOutput IN) : SV_Target
		    {
		        UNITY_SETUP_INSTANCE_ID(IN);
		
		    	
		    	
		        SurfaceInputs surfaceInput = (SurfaceInputs)0;
		        
		
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
		
			#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/InputSurfaceUnlit.hlsl"
			#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/LightweightPassDepthOnly.hlsl"
			ENDHLSL
		}
		
	}
	
	FallBack "Hidden/InternalErrorShader"
}
