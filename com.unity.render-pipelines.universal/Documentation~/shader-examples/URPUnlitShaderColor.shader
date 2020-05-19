// This shader fills the mesh shape with a color that a user can change using the Inspector window on a Material.
Shader "Unlit/URPUnlitShaderColor"
{    
    // The _BaseColor variable is visible as a field called Base Color in the Inspector window on a Material.
    // This variable has the default value, and you can select a custom color using the Base Color field.
    Properties
    { 
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
    }
    
    SubShader
    {        
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }
                
        Pass
        {            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"            
            
            struct Attributes
            {
                float4 positionOS   : POSITION;                 
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
            };

            // To make the Shader SRP Batcher compatible, declare all properties related to a Material 
            // in a UnityPerMaterial CBUFFER.
            CBUFFER_START(UnityPerMaterial)
                // The following line declares the _BaseColor variable, so that you can use it in the fragment shader.
                half4 _BaseColor;            
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag() : SV_Target
            {
                // Returning the _BaseColor value.                
                return _BaseColor;
            }
            ENDHLSL
        }
    }
}