Shader "Hidden/HDRenderPipeline/CopyStencilBuffer"
{
    Properties
    {
        [HideInInspector] _StencilRef("_StencilRef", Int) = 1
    }

    SubShader
    {
        Pass
        {
            Stencil
            {
                Ref  [_StencilRef]
                Comp Equal
                Pass Keep
            }

            Cull   Off
            ZTest  Always
            ZWrite Off
            Blend  Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 metal // TEMP: until we go further in dev
            #pragma multi_compile _ EXPORT_HTILE
            // #pragma enable_d3d11_debug_symbols

            #pragma vertex Vert
            #pragma fragment Frag

            #include "../../../../ShaderLibrary/Packing.hlsl"
            #include "../../../ShaderVariables.hlsl"
            #include "../../../Lighting/LightDefinition.cs.hlsl"

            int _StencilRef;
            RW_TEXTURE2D(float, _HTile); // DXGI_FORMAT_R8_UINT is not supported by Unity

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_Position;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                return output;
            }

            // Force the stencil test before the UAV write.
            [earlydepthstencil]
            float4 Frag(Varyings input) : SV_Target // use SV_StencilRef in D3D 11.3+
            {
                uint2 positionSS = (uint2)input.positionCS.xy;

                #ifdef EXPORT_HTILE
                    // There's no need for atomics as we are always writing the same value.
                    // Note: the GCN tile size is 8x8 pixels.
                    _HTile[positionSS / 8] = _StencilRef;
                #endif

                return PackByte(_StencilRef);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
