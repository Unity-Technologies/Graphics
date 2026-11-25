Shader "Hidden/Universal Render Pipeline/FogVolume"
{
    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    // Must match FogVolumeData in FogVolumeCullingJob.cs.
    struct FogVolumeData
    {
        float4 axisX;
        float4 axisY;
        float4 axisZ;
        float4 center;
        float4 scattering;
        float4 clipBounds;
        float3 densityTextureTiling;
        uint startSlice; // 16-byte boundary
        float3 densityTextureScrollSpeed;
        uint sliceCount; // 16-byte boundary
        uint densityMode;
        uint3 padding; // 16-byte boundary
    };

    StructuredBuffer<FogVolumeData> _FogVolumes;
    float4x4 _VBufferCoordToViewDirWS;
    int      _FogVolumeRunStart;
    int      _FogVolumeDensityTextureMode;

    TEXTURE3D(_FogVolumeDensityTexture);
    SAMPLER(sampler_FogVolumeDensityTexture);

    // Set globally by VolumetricFogGlobalsPass.
    float4 _DepthDecodingParams;
    float  _VBufferRcpSliceCount;

    // Must match FogVolumeDensityMode in FogVolume.cs.
    static const uint k_FogVolumeDensityModeConstant = 0;
    static const uint k_FogVolumeDensityModeTexture = 1;

    // Must match DensityTextureMode in FogVolumesPass.cs.
    static const int k_DensityTextureModeNone = 0;
    static const int k_DensityTextureModeColor = 1;
    static const int k_DensityTextureModeAlpha = 2;
    static const int k_DensityTextureModeRed = 3;

    // Must match k_QuadVertexCount in FogVolumesPass.cs.
    static const uint k_QuadVertexCount = 6;

    static const float2 k_QuadCorners[k_QuadVertexCount] =
    {
        float2(0.0, 0.0), float2(1.0, 0.0), float2(0.0, 1.0),
        float2(0.0, 1.0), float2(1.0, 0.0), float2(1.0, 1.0)
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        nointerpolation uint slice : TEXCOORD0;
        nointerpolation uint fogVolumeIndex : TEXCOORD1;
        nointerpolation uint renderTargetIndex : SV_RenderTargetArrayIndex;
    };

    Varyings Vert(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
    {
        uint fogVolumeIndex = _FogVolumeRunStart + instanceID;
        FogVolumeData fogVolume = _FogVolumes[fogVolumeIndex];
        float4 clipBounds = fogVolume.clipBounds;

        uint quadIndex = vertexID / k_QuadVertexCount;
        uint cornerIndex = vertexID % k_QuadVertexCount;

        Varyings output;
        output.positionCS = float4(clipBounds.xy + k_QuadCorners[cornerIndex] * clipBounds.zw, 0.5, 1.0);
        output.slice = fogVolume.startSlice + quadIndex;
        output.fogVolumeIndex = fogVolumeIndex;
        output.renderTargetIndex = output.slice;

        if (quadIndex >= fogVolume.sliceCount)
        {
            output.positionCS = float4(0.0, 0.0, 0.5, 1.0);
            output.renderTargetIndex = 0;
        }

        return output;
    }

    float4 Frag(Varyings input) : SV_Target
    {
        FogVolumeData fogVolume = _FogVolumes[input.fogVolumeIndex];

        // Matches the voxel centers written by VolumeVoxelization.compute.
        float3 rayDirWS = mul(-float4(input.positionCS.xy, 1, 1), _VBufferCoordToViewDirWS).xyz;
        float rayLength = length(rayDirWS);
        float3 dirWS = rayDirWS / rayLength;
        float tStart = _ProjectionParams.y * rayLength;

        float e0 = input.slice * _VBufferRcpSliceCount;
        float t0 = max(tStart, DecodeLogarithmicDepthGeneralized(e0, _DepthDecodingParams));
        float t1 = max(tStart, DecodeLogarithmicDepthGeneralized(e0 + _VBufferRcpSliceCount, _DepthDecodingParams));
        float3 voxelCenterWS = _WorldSpaceCameraPos + 0.5 * (t0 + t1) * dirWS;

        float3 offset = voxelCenterWS - fogVolume.center.xyz;
        float3 positionLS = float3(dot(offset, fogVolume.axisX.xyz), dot(offset, fogVolume.axisY.xyz), dot(offset, fogVolume.axisZ.xyz));
        if (any(abs(positionLS) > 1.0))
            discard;

        // RGB tints the albedo and alpha scales the density.
        float4 densityTexel = 1.0;
        [branch]
        if (fogVolume.densityMode == k_FogVolumeDensityModeTexture)
        {
            // Fog volumes without a 3D texture have no fog.
            densityTexel = 0.0;
            if (_FogVolumeDensityTextureMode != k_DensityTextureModeNone)
            {
                float3 uvw = (positionLS * 0.5 + 0.5) * fogVolume.densityTextureTiling - fogVolume.densityTextureScrollSpeed * _Time.y;
                densityTexel = SAMPLE_TEXTURE3D_LOD(_FogVolumeDensityTexture, sampler_FogVolumeDensityTexture, uvw, 0);
                if (_FogVolumeDensityTextureMode == k_DensityTextureModeAlpha)
                    densityTexel.rgb = 1.0;
                else if (_FogVolumeDensityTextureMode == k_DensityTextureModeRed)
                    densityTexel = float4(1.0, 1.0, 1.0, densityTexel.r);
            }
        }

        return fogVolume.scattering * float4(densityTexel.rgb * densityTexel.a, densityTexel.a);
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        ZWrite Off
        ZTest Always
        Cull Off

        // Pass order must match FogVolumeBlendMode.
        Pass
        {
            Name "Overwrite"
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }

        Pass
        {
            Name "Add"
            Blend One One

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }

        Pass
        {
            Name "Multiply"
            Blend DstColor Zero

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }

        Pass
        {
            Name "Min"
            Blend One One
            BlendOp Min

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }

        Pass
        {
            Name "Max"
            Blend One One
            BlendOp Max

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
