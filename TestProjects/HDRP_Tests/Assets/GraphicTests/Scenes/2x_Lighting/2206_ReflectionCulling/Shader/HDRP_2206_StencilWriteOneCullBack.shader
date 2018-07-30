Shader "HDRP_2206/WriteOneToStencilCullBack"
{
    Properties
    {
		// Be careful, do not change the name here to _Color. It will conflict with the "fake" parameters (see end of properties) required for GI.
        _Color("Color", Color) = (1,1,0,1)
        
		// Blending state
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
    }
    
	HLSLINCLUDE
    
	#pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
 
	//-------------------------------------------------------------------------------------
    // Define
    //-------------------------------------------------------------------------------------
     #define UNITY_MATERIAL_UNLIT // Need to be define before including Material.hlsl
    
	//-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

	#include "CoreRP/ShaderLibrary/Common.hlsl"
    #include "HDRP/ShaderPass/FragInputs.hlsl"
    
	//-------------------------------------------------------------------------------------
    // variable declaration
    //-------------------------------------------------------------------------------------
	
	float4 _Color;
    
    #pragma vertex Vert
    #pragma fragment Frag
    
	ENDHLSL
    
	SubShader
    {
        // This tags allow to use the shader replacement features
        Tags{ "RenderPipeline" = "HDRenderPipeline" "RenderType" = "HDUnlitShader" }
		
		// Unlit shader always render in forward
        Pass
        {
            Name "Forward Unlit"
            Tags { "LightMode" = "ForwardOnly" }
            
			Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

			Cull Back

			Stencil
			{
				Ref 1
				Comp Always
				Pass Replace
			}
            
			HLSLPROGRAM
 
			#define SHADERPASS SHADERPASS_FORWARD_UNLIT
            #include "HDRP/Material/Material.hlsl"
			
			struct VertInput
			{
				float3 positionOS   : POSITION;
				float3 normalOS     : NORMAL;
			};

			struct VertToFrag
			{
				float4 positionCS : SV_Position;
				float3 normalWS : TEXCOORD1;
			};
	
			VertToFrag Vert(VertInput input)
			{
				VertToFrag output;
				
				float3 positionRWS = TransformObjectToWorld(input.positionOS);
				output.positionCS = TransformWorldToHClip(positionRWS);
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);

				return output;
			}
 			
			float4 Frag(VertToFrag input, float facing : VFACE) : SV_Target
			{
				float3 normal = input.normalWS;
				return float4(_Color.rgb * max(0.1, dot(normal, float3(0.5, 1, 0).xyz)), 1);
			}

			ENDHLSL
        }
    }
}
