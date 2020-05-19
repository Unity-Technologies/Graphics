// This shader visuzlizes the normal vector values on the mesh.
Shader "Unlit/URPUnlitShaderNormal"
{    
    Properties
    { }
    
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
                // Declaring the normal variable containing the normal vector for each vertex.
                half3 normal        : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                half3 normal        : TEXCOORD0;
            };                                   
            
            Varyings vert(Attributes IN)
            {                
                Varyings OUT;                
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);       
                // Using the TransformObjectToWorldNormal function to transform the normals from object to world space.
                // This function is from the SpaceTransforms.hlsl file, which is in one of the #include definitions referenced in Core.hlsl.
                OUT.normal = TransformObjectToWorldNormal(IN.normal);                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {                
                half4 color = 0;
                // IN.normal is a 3D vector. Each vector component has the range -1..1.
                // To show the vector elements as color, compress each value into the range 0..1.                
                color.rgb = IN.normal * 0.5 + 0.5;                
                return color;
            }
            ENDHLSL
        }
    }
}