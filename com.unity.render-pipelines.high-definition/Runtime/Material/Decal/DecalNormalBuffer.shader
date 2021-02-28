Shader "Hidden/HDRP/Material/Decal/DecalNormalBuffer"
{

    Properties
    {
        // Stencil state
        [HideInInspector] _DecalNormalBufferStencilRef("_DecalNormalBufferStencilRef", Int) = 0           // set at runtime
        [HideInInspector] _DecalNormalBufferStencilReadMask("_DecalNormalBufferStencilReadMask", Int) = 0 // set at runtime
    }

    HLSLINCLUDE

        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

#if defined(PLATFORM_NEEDS_UNORM_UAV_SPECIFIER) && defined(PLATFORM_SUPPORTS_EXPLICIT_BINDING)
        // Explicit binding is needed on D3D since we bind the UAV to slot 1 and we don't have a colour RT bound to fix a D3D warning.
        RW_TEXTURE2D_X(unorm float4, _NormalBuffer) : register(u1);
#else
        RW_TEXTURE2D_X(float4, _NormalBuffer);
#endif

        struct Attributes
        {
            uint vertexID : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord   : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        DECLARE_DBUFFER_TEXTURE(_DBufferTexture);

        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
            return output;
        }

        // Force the stencil test before the UAV write.
        [earlydepthstencil]
        void FragNearest(Varyings input)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            FETCH_DBUFFER(DBuffer, _DBufferTexture, input.texcoord * _ScreenSize.xy);
            DecalSurfaceData decalSurfaceData;
            DECODE_FROM_DBUFFER(DBuffer, decalSurfaceData);

            uint2 positionSS = uint2(input.texcoord * _ScreenSize.xy);
            float4 normalbuffer = _NormalBuffer[COORD_TEXTURE2D_X(positionSS)];
            NormalData normalData;
            DecodeFromNormalBuffer(normalbuffer, normalData);
            normalData.normalWS.xyz = normalize(normalData.normalWS.xyz * decalSurfaceData.normalWS.w + decalSurfaceData.normalWS.xyz);
            normalData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(PerceptualRoughnessToPerceptualSmoothness(normalData.perceptualRoughness) * decalSurfaceData.mask.w + decalSurfaceData.mask.z);
            EncodeIntoNormalBuffer(normalData, normalbuffer);
            _NormalBuffer[COORD_TEXTURE2D_X(positionSS)] = normalbuffer;
        }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            Stencil
            {
                WriteMask [_DecalNormalBufferStencilReadMask]
                ReadMask [_DecalNormalBufferStencilReadMask]
                Ref [_DecalNormalBufferStencilRef]
                Comp Equal
                Pass Zero   // Clear bits since they are not needed anymore.
                            // Note: this is fine with the combination
                            // _DecalNormalBufferStencilReadMask - StencilUsage.Decals | (int)StencilUsage.RequiresDeferredLighting
                            // _DecalNormalBufferStencilRef = (int)StencilUsage.Decals
                            // Because the test success only if RequiresDeferredLighting isn't set, and thus we can clear the 2 bits, RequiresDeferredLighting already don't exist
            }

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragNearest
            ENDHLSL
        }
    }

    Fallback Off
}
