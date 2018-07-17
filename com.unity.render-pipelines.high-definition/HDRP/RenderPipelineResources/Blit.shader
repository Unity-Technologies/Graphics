Shader "Hidden/HDRenderPipeline/Blit"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
        #include "CoreRP/ShaderLibrary/Common.hlsl"
        #include "../ShaderVariables.hlsl"

        TEXTURE2D(_BlitTexture);
        TEXTURECUBE(_CubemapBlitTexture);
        SamplerState sampler_PointClamp;
        SamplerState sampler_PointRepeat;
        SamplerState sampler_LinearClamp;
        SamplerState sampler_LinearRepeat;
        uniform float4 _BlitScaleBias;
        uniform float4 _BlitScaleBiasRt;
        uniform float _BlitMipLevel;
        uniform uint _BlitFaceIndex;
        uniform float2 _BlitTextureSize;
        uniform uint _BlitPaddingSize;

        struct Attributes
        {
            uint vertexID : SV_VertexID;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord   : TEXCOORD0;
        };

        struct VaryingsCube
        {
            float4 positionCS : SV_POSITION;
            float3 texcoord   : TEXCOORD0;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.texcoord   = GetFullScreenTriangleTexCoord(input.vertexID) * _BlitScaleBias.xy + _BlitScaleBias.zw;
            return output;
        }

        Varyings VertQuad(Attributes input)
        {
            Varyings output;
            output.positionCS = GetQuadVertexPosition(input.vertexID) * float4(_BlitScaleBiasRt.x, _BlitScaleBiasRt.y, 1, 1) + float4(_BlitScaleBiasRt.z, _BlitScaleBiasRt.w, 0, 0);
            output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
            output.texcoord = GetQuadTexCoord(input.vertexID) * _BlitScaleBias.xy + _BlitScaleBias.zw;
            return output;
        }

        Varyings VertQuadPadding(Attributes input)
        {
            Varyings output;
            float2 paddingUV = 1 - ((_BlitTextureSize - _BlitPaddingSize) / _BlitTextureSize);
            output.positionCS = GetQuadVertexPosition(input.vertexID) * float4(_BlitScaleBiasRt.x, _BlitScaleBiasRt.y, 1, 1) + float4(_BlitScaleBiasRt.z, _BlitScaleBiasRt.w, 0, 0);
            output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
            output.texcoord = GetQuadTexCoord(input.vertexID) * (_BlitScaleBias.xy + paddingUV) + _BlitScaleBias.zw - paddingUV / 2;
            return output;
        }
        
        // Duplication of BlitCubemap.shader
        static const float3 faceU[6] = { float3(0, 0, -1), float3(0, 0, 1), float3(1, 0, 0), float3(1, 0, 0), float3(1, 0, 0), float3(-1, 0, 0) };
        static const float3 faceV[6] = { float3(0, -1, 0), float3(0, -1, 0), float3(0, 0, 1), float3(0, 0, -1), float3(0, -1, 0), float3(0, -1, 0) };

        VaryingsCube VertCube(Attributes input)
        {
            VaryingsCube output;
            float4 cubeFaceVertexPosition = GetQuadVertexPosition(input.vertexID) + float4(_BlitFaceIndex % 3, _BlitFaceIndex / 3, 0, 0);
            output.positionCS = cubeFaceVertexPosition * float4(_BlitScaleBiasRt.x, _BlitScaleBiasRt.y, 1, 1) + float4(_BlitScaleBiasRt.z, _BlitScaleBiasRt.w, 0, 0);
            output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
            
            float2 uv = GetQuadTexCoord(input.vertexID) * _BlitScaleBias.xy + _BlitScaleBias.zw;
            uv = uv * 2 - 1;

            int idx = (int)_BlitFaceIndex;
            float3 transformU = faceU[idx];
            float3 transformV = faceV[idx];

            float3 n = cross(transformV, transformU);
            output.texcoord = n + uv.x * transformU + uv.y * transformV;

            return output;
        }

        float4 FragNearest(Varyings input) : SV_Target
        {
            return SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_PointClamp, input.texcoord, _BlitMipLevel);
        }

        float4 FragBilinear(Varyings input) : SV_Target
        {
            return SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, input.texcoord, _BlitMipLevel);
        }
        
        float4 FragNearestRepeat(Varyings input) : SV_Target
        {
            return SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_PointRepeat, input.texcoord, _BlitMipLevel);
        }

        float4 FragBilinearRepeat(Varyings input) : SV_Target
        {
            return SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearRepeat, input.texcoord, _BlitMipLevel);
        }

        float4 FragNearestCube(VaryingsCube input) : SV_Target
        {
            return SAMPLE_TEXTURECUBE_LOD(_CubemapBlitTexture, sampler_PointClamp, input.texcoord, _BlitMipLevel);
        }

        float4 FragBilinearCube(VaryingsCube input) : SV_Target
        {
            return SAMPLE_TEXTURECUBE_LOD(_CubemapBlitTexture, sampler_LinearClamp, input.texcoord, _BlitMipLevel);
        }

        float4 FragNearestLatlongCube(Varyings input) : SV_Target
        {
            float3 coord = LatlongToDirectionCoordinate(input.texcoord);
            return SAMPLE_TEXTURECUBE_LOD(_CubemapBlitTexture, sampler_PointClamp, coord, _BlitMipLevel);
        }
        
        float4 FragBilinearLatlongCube(Varyings input) : SV_Target
        {
            float3 coord = LatlongToDirectionCoordinate(input.texcoord);
            return SAMPLE_TEXTURECUBE_LOD(_CubemapBlitTexture, sampler_LinearClamp, coord, _BlitMipLevel);
        }


        float4 FragNearestOctahedralCube(Varyings input) : SV_Target
        {
            float3 coord = LatlongToDirectionCoordinate(input.texcoord);
            return SAMPLE_TEXTURECUBE_LOD(_CubemapBlitTexture, sampler_PointClamp, coord, _BlitMipLevel);
        }
        
        float4 FragBilinearOctahedralCube(Varyings input) : SV_Target
        {
            float3 coord = LatlongToDirectionCoordinate(input.texcoord);
            return SAMPLE_TEXTURECUBE_LOD(_CubemapBlitTexture, sampler_LinearClamp, coord, _BlitMipLevel);
        }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        // 0: Nearest
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragNearest
            ENDHLSL
        }

        // 1: Bilinear
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragBilinear
            ENDHLSL
        }

        // 2: Nearest quad
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment FragNearest
            ENDHLSL
        }

        // 3: Bilinear quad
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment FragBilinear
            ENDHLSL
        }

        // 4: Nearest Cube
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertCube
                #pragma fragment FragNearestCube
            ENDHLSL
        }

        // 5: Bilinear Cube
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertCube
                #pragma fragment FragBilinearCube
            ENDHLSL
        }

        // 6: Nearest padded quad
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuadPadding
                #pragma fragment FragNearestRepeat
            ENDHLSL
        }

        // 7: Bilinear padded quad
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuadPadding
                #pragma fragment FragBilinearRepeat
            ENDHLSL
        }

        // 8: Nearest Latlong cube
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment FragNearestLatlongCube
            ENDHLSL
        }

        // 9: Bilinear Latlong cube
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment FragBilinearLatlongCube
            ENDHLSL
        }

        // 10: Nearest Octahedral cube
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment FragNearestOctahedralCube
            ENDHLSL
        }

        // 11: Bilinear Octahedral cube
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment FragBilinearOctahedralCube
            ENDHLSL
        }

    }

    Fallback Off
}
