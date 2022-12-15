using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using static Unity.Mathematics.math;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        static int SignedMod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }

        // This function does a "repeat" load
        static float4 LoadTexture2DArray(NativeArray<float4> textureRawBuffer, int2 coord, int sliceIndex, int resolution)
        {
            int2 repeatCoord = coord;
            repeatCoord.x = SignedMod(repeatCoord.x, resolution);
            repeatCoord.y = SignedMod(repeatCoord.y, resolution);
            int bandOffset = resolution * resolution * sliceIndex;
            return textureRawBuffer[repeatCoord.x + repeatCoord.y * resolution + bandOffset];
        }

        static float4 LoadTexture2D(NativeArray<uint> textureRawBuffer, int2 coord, int2 resolution)
        {
            int2 repeatCoord = coord;
            repeatCoord.x = SignedMod(repeatCoord.x, resolution.x);
            repeatCoord.y = SignedMod(repeatCoord.y, resolution.y);
            int tapIndex = repeatCoord.x + repeatCoord.y * resolution.x;
            uint packedData = textureRawBuffer[tapIndex];
            return float4(packedData & 0xff, (packedData >> 8) & 0xff, (packedData >> 16) & 0xff, (packedData >> 24) & 0xff) / 255.0f;
        }

        static float2 LoadTexture2D(NativeArray<short> textureRawBuffer, int2 coord, int2 resolution)
        {
            int2 repeatCoord = coord;
            repeatCoord.x = SignedMod(repeatCoord.x, resolution.x);
            repeatCoord.y = SignedMod(repeatCoord.y, resolution.y);
            int tapIndex = repeatCoord.x + repeatCoord.y * resolution.x;
            short packedData = textureRawBuffer[tapIndex];
            return float2(packedData & 0xff, (packedData >> 8) & 0xff) / 255.0f;
        }

        static float LoadTexture2D(NativeArray<half> textureRawBuffer, int2 coord, int2 resolution)
        {
            int2 repeatCoord = coord;
            repeatCoord.x = SignedMod(repeatCoord.x, resolution.x);
            repeatCoord.y = SignedMod(repeatCoord.y, resolution.y);
            int tapIndex = repeatCoord.x + repeatCoord.y * resolution.x;
            return textureRawBuffer[tapIndex];
        }

        static int2 FloorCoordinate(float2 coord)
        {
            return new int2((int)Mathf.Floor(coord.x), (int)Mathf.Floor(coord.y));
        }

        static float4 SampleTexture2DArrayBilinear(NativeArray<float4> textureBuffer, float2 uvCoord, int sliceIndex, int resolution)
        {
            // Convert the position from uv to floating pixel coordinates (for the bilinear interpolation)
            float2 tapCoord = (uvCoord * resolution);
            int2 currentTapCoord = FloorCoordinate(tapCoord);

            // Read the four samples we want
            float4 p0 = LoadTexture2DArray(textureBuffer, currentTapCoord, sliceIndex, resolution);
            float4 p1 = LoadTexture2DArray(textureBuffer, currentTapCoord + new int2(1, 0), sliceIndex, resolution);
            float4 p2 = LoadTexture2DArray(textureBuffer, currentTapCoord + new int2(0, 1), sliceIndex, resolution);
            float4 p3 = LoadTexture2DArray(textureBuffer, currentTapCoord + new int2(1, 1), sliceIndex, resolution);

            // Do the bilinear interpolation
            float2 fract = tapCoord - currentTapCoord;
            float4 i0 = lerp(p0, p1, fract.x);
            float4 i1 = lerp(p2, p3, fract.x);
            return lerp(i0, i1, fract.y);
        }

        static float4 SampleTexture2DBilinear(NativeArray<uint> textureBuffer, float2 uvCoord, int2 resolution)
        {
            // Convert the position from uv to floating pixel coordinates (for the bilinear interpolation)
            float2 tapCoord = (uvCoord * resolution);
            int2 currentTapCoord = FloorCoordinate(tapCoord);

            // Read the four samples we want
            float4 p0 = LoadTexture2D(textureBuffer, currentTapCoord, resolution);
            float4 p1 = LoadTexture2D(textureBuffer, currentTapCoord + new int2(1, 0), resolution);
            float4 p2 = LoadTexture2D(textureBuffer, currentTapCoord + new int2(0, 1), resolution);
            float4 p3 = LoadTexture2D(textureBuffer, currentTapCoord + new int2(1, 1), resolution);

            // Do the bilinear interpolation
            float2 fract = tapCoord - currentTapCoord;
            float4 i0 = lerp(p0, p1, fract.x);
            float4 i1 = lerp(p2, p3, fract.x);
            return lerp(i0, i1, fract.y);
        }

        static float SampleTexture2DBilinear(NativeArray<half> textureBuffer, float2 uvCoord, int2 resolution)
        {
            // Convert the position from uv to floating pixel coordinates (for the bilinear interpolation)
            float2 tapCoord = (uvCoord * resolution);
            int2 currentTapCoord = FloorCoordinate(tapCoord);

            // Read the four samples we want
            float p0 = LoadTexture2D(textureBuffer, currentTapCoord, resolution);
            float p1 = LoadTexture2D(textureBuffer, currentTapCoord + new int2(1, 0), resolution);
            float p2 = LoadTexture2D(textureBuffer, currentTapCoord + new int2(0, 1), resolution);
            float p3 = LoadTexture2D(textureBuffer, currentTapCoord + new int2(1, 1), resolution);

            // Do the bilinear interpolation
            float2 fract = tapCoord - currentTapCoord;
            float i0 = lerp(p0, p1, fract.x);
            float i1 = lerp(p2, p3, fract.x);
            return lerp(i0, i1, fract.y);
        }

        static float2 SampleTexture2DBilinear_float2(NativeArray<short> textureBuffer, float2 uvCoord, int2 resolution)
        {
            // Convert the position from uv to floating pixel coordinates (for the bilinear interpolation)
            float2 tapCoord = (uvCoord * resolution);
            int2 currentTapCoord = FloorCoordinate(tapCoord);

            // Read the four samples we want
            float2 p0 = LoadTexture2D(textureBuffer, currentTapCoord, resolution);
            float2 p1 = LoadTexture2D(textureBuffer, currentTapCoord + new int2(1, 0), resolution);
            float2 p2 = LoadTexture2D(textureBuffer, currentTapCoord + new int2(0, 1), resolution);
            float2 p3 = LoadTexture2D(textureBuffer, currentTapCoord + new int2(1, 1), resolution);

            // Do the bilinear interpolation
            float2 fract = tapCoord - currentTapCoord;
            float2 i0 = lerp(p0, p1, fract.x);
            float2 i1 = lerp(p2, p3, fract.x);
            return lerp(i0, i1, fract.y);
        }

        static float2 EvaluateWaterGroup0CurrentUV(in WaterSimSearchData wsd, float2 currentUV)
        {
            return float2(currentUV.x - wsd.group0CurrentRegionOffset.x, currentUV.y + wsd.group0CurrentRegionOffset.y) * wsd.group0CurrentRegionScale + 0.5f;
        }

        static float2 EvaluateWaterGroup1CurrentUV(in WaterSimSearchData wsd, float2 currentUV)
        {
            return float2(currentUV.x - wsd.group1CurrentRegionOffset.x, currentUV.y + wsd.group1CurrentRegionOffset.y) * wsd.group1CurrentRegionOffset + 0.5f;
        }

        static float ConvertAngle_0_2PI(float angle)
        {
            angle = angle % (2.0f * PI);
            return angle < 0.0f ? angle + 2.0f * PI : angle;
        }

        static float ConvertAngle_NPI_PPI(float angle)
        {
            angle = angle % (2.0f * PI);
            return angle > PI ? angle - 2.0f * PI : (angle < -PI ? angle + 2.0f * PI : angle);
        }

        static float EvaluateAngle(float3 cmpDir, float orientation, float influence)
        {
            float3 dir = float3(cmpDir.xy * 2.0f - 1.0f, cmpDir.z);
            float angle = ConvertAngle_NPI_PPI(atan2(dir.y, dir.x) - orientation);
            return angle * influence * dir.z;
        }

        struct CurrentData
        {
            public int quadrant;
            public float proportion;
            public float angle;
        };

        static void DecompressDirection(in WaterSimSearchData wsd, float3 cmpDir, float orientation, float influence, out CurrentData currentData)
        {
            float angle = EvaluateAngle(cmpDir, orientation, influence);
            currentData.angle = angle < 0.0f ? angle + 2.0f * PI : angle;
            float data = currentData.angle / WaterConsts.k_SectorSize;
            float relativeAngle = frac(data);
            currentData.quadrant = ((int)data % WaterConsts.k_NumSectors);
            currentData.proportion = pow(currentData.quadrant % 2 == 0 ? relativeAngle : 1.0f - relativeAngle, 0.75f);
        }

        static void EvaluateGroup0CurrentData(in WaterSimSearchData wsd, float2 currentUV, out CurrentData currentData)
        {
            float3 largeDirection = SampleTexture2DBilinear(wsd.group0CurrentMap, EvaluateWaterGroup0CurrentUV(wsd, currentUV), wsd.group0CurrentMapResolution).xyz;
            DecompressDirection(wsd, largeDirection, wsd.spectrum.groupOrientation.x * Mathf.Deg2Rad, wsd.group0CurrentMapInfluence, out currentData);
        }

        static void EvaluateGroup1CurrentData(in WaterSimSearchData wsd, float2 currentUV, out CurrentData currentData)
        {
            float3 ripplesDirection = SampleTexture2DBilinear(wsd.group1CurrentMap, EvaluateWaterGroup1CurrentUV(wsd, currentUV), wsd.group1CurrentMapResolution).xyz;
            DecompressDirection(wsd, ripplesDirection, wsd.spectrum.groupOrientation.y * Mathf.Deg2Rad, wsd.group1CurrentMapInfluence, out currentData);
        }

        static void SwizzleSamplingCoordinates(float2 coord, int quadrant, NativeArray<float4> sectorData, out float4 tapCoord)
        {
            tapCoord = 0.0f;
            int sectorIndex = quadrant + WaterConsts.k_SectorDataSamplingOffset;
            float4 dir0 = sectorData[2 * sectorIndex];
            float4 dir1 = sectorData[2 * sectorIndex + 1];
            tapCoord.xy = float2(dot(coord.xy, dir0.xy), dot(coord.xy, dir0.zw));
            tapCoord.zw = float2(dot(coord.xy, dir1.xy), dot(coord.xy, dir1.zw));
        }

        struct PatchSimData
        {
            public float2 uv;
            public float blend;
            public float4 swizzle;
        }

        struct WaterSimCoord
        {
            public PatchSimData data0;
            public PatchSimData data1;
            public PatchSimData data2;
        }

        static void ComputeWaterUVs(in WaterSimSearchData wsd, float2 uv, out WaterSimCoord simCoord)
        {
            // Band 0
            simCoord.data0.uv = (uv - OrientationToDirection(wsd.spectrum.patchOrientation.x) * wsd.rendering.patchCurrentSpeed.x * wsd.rendering.simulationTime) / wsd.spectrum.patchSizes.x;
            simCoord.data0.blend = 1.0f;
            simCoord.data0.swizzle = float4(1, 0, 0, 1);

            // Band 1
            simCoord.data1.uv = (uv - OrientationToDirection(wsd.spectrum.patchOrientation.y) * wsd.rendering.patchCurrentSpeed.y * wsd.rendering.simulationTime) / wsd.spectrum.patchSizes.y;
            simCoord.data1.blend = 1.0f;
            simCoord.data1.swizzle = float4(1, 0, 0, 1);

            // Band 2
            simCoord.data2.uv = (uv - OrientationToDirection(wsd.spectrum.patchOrientation.z) * wsd.rendering.patchCurrentSpeed.z * wsd.rendering.simulationTime) / wsd.spectrum.patchSizes.z;
            simCoord.data2.blend = 1.0f;
            simCoord.data2.swizzle = float4(1, 0, 0, 1);
        }

        static void FillPatchData(int patchGroup,
            in PatchSimData largeCoord, in PatchSimData ripplesCoord,
            in CurrentData largeCurrent, in CurrentData ripplesCurrent,
            in WaterSectorData largeSector, in WaterSectorData ripplesSector,
            bool firstPass,
            out PatchSimData simData)
        {
            if (patchGroup == 0)
            {
                simData.uv = largeCoord.uv;
                simData.blend = firstPass ? 1.0f - largeCurrent.proportion : largeCurrent.proportion;
                simData.swizzle = firstPass ? largeSector.dir0 : largeSector.dir1;
            }
            else
            {
                simData.uv = ripplesCoord.uv;
                simData.blend = firstPass ? 1.0f - ripplesCurrent.proportion : ripplesCurrent.proportion;
                simData.swizzle = firstPass ? ripplesSector.dir0 : ripplesSector.dir1;
            }
        }

        static void AggregateWaterSimCoords(in WaterSimSearchData wsd,
            in WaterSimCoord gr0SC, in WaterSimCoord gr1SC,
            in CurrentData gr0CD, in CurrentData gr1CD,
            bool firstPass, out WaterSimCoord simCoord)
        {
            // Grab the sector data for both groups
            WaterSectorData gr0SD, gr1SD;
            gr0SD.dir0 = wsd.sectorData[2 * (gr0CD.quadrant + WaterConsts.k_SectorDataOtherOffset)];
            gr0SD.dir1 = wsd.sectorData[2 * (gr0CD.quadrant + WaterConsts.k_SectorDataOtherOffset) + 1];
            gr1SD.dir0 = wsd.sectorData[2 * (gr1CD.quadrant + WaterConsts.k_SectorDataOtherOffset)];
            gr1SD.dir1 = wsd.sectorData[2 * (gr1CD.quadrant + WaterConsts.k_SectorDataOtherOffset) + 1];

            // Pick the right group data
            FillPatchData(wsd.spectrum.patchGroup[0], gr0SC.data0, gr1SC.data0, gr0CD, gr1CD, gr0SD, gr1SD, firstPass, out simCoord.data0);
            FillPatchData(wsd.spectrum.patchGroup[1], gr0SC.data1, gr1SC.data1, gr0CD, gr1CD, gr0SD, gr1SD, firstPass, out simCoord.data1);
            FillPatchData(wsd.spectrum.patchGroup[2], gr0SC.data2, gr1SC.data2, gr0CD, gr1CD, gr0SD, gr1SD, firstPass, out simCoord.data2);
        }

        static void AddBandContribution(in WaterSimSearchData wsd, in PatchSimData data, int bandIdx, float3 waterMask, ref float3 totalDisplacement)
        {
            float3 rawDisplacement = SampleTexture2DArrayBilinear(wsd.displacementData, data.uv, bandIdx, wsd.simulationRes).xyz;
            rawDisplacement *= wsd.rendering.patchAmplitudeMultiplier[bandIdx] * waterMask[bandIdx] * data.blend;
            totalDisplacement += float3(rawDisplacement.x, dot(rawDisplacement.yz, data.swizzle.xy), dot(rawDisplacement.yz, data.swizzle.zw));
        }

        static float3 EvaluateWaterSimulation(in WaterSimSearchData wsd, WaterSimCoord sc, float3 waterMask)
        {
            float3 totalDisplacement = 0.0f;

            AddBandContribution(in wsd, in sc.data0, 0, waterMask, ref totalDisplacement);

            if (wsd.activeBandCount > 1)
                AddBandContribution(in wsd, in sc.data1, 1, waterMask, ref totalDisplacement);

            if (wsd.activeBandCount > 2)
                AddBandContribution(in wsd, in sc.data2, 2, waterMask, ref totalDisplacement);

            return totalDisplacement;
        }

        static float3 EvaluateWaterMask(in WaterSimSearchData wsd, float2 uv)
        {
            float3 waterMask = 1.0f;
            if (wsd.activeMask)
            {
                float2 waterMaskUV = float2(uv.x - wsd.maskOffset.x, uv.y + wsd.maskOffset.y) * wsd.maskScale + 0.5f;
                waterMask = SampleTexture2DBilinear(wsd.maskBuffer, waterMaskUV, wsd.maskResolution).xyz;
                waterMask = wsd.maskRemap.xxx + waterMask * wsd.maskRemap.yyy;
            }
            return waterMask;
        }
    }
}
