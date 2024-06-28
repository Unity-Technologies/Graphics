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
    partial class WaterSystem
    {
        static int SignedMod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }

        static int HandleWrapMode(int coord, int resolution, TextureWrapMode wrapMode)
        {
            // Only handle repeat and clamp because heh
            if (wrapMode == TextureWrapMode.Repeat)
                return SignedMod(coord, resolution);
            return Mathf.Clamp(coord, 0, resolution - 1);
        }

        // This function does a "repeat" load
        static T LoadTexture2DArray<T>(NativeArray<T> textureRawBuffer, int2 coord, int sliceIndex, int resolution) where T : struct
        {
            int2 repeatCoord = coord;
            repeatCoord.x = SignedMod(repeatCoord.x, resolution);
            repeatCoord.y = SignedMod(repeatCoord.y, resolution);
            int bandOffset = resolution * resolution * sliceIndex;
            return textureRawBuffer[repeatCoord.x + repeatCoord.y * resolution + bandOffset];
        }

        static float4 LoadTexture2D(NativeArray<uint> textureRawBuffer, TextureWrapMode wrapModeU, TextureWrapMode wrapModeV, int2 coord, int2 resolution)
        {
            int2 repeatCoord = coord;
            repeatCoord.x = HandleWrapMode(repeatCoord.x, resolution.x, wrapModeU);
            repeatCoord.y = HandleWrapMode(repeatCoord.y, resolution.y, wrapModeV);
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

        static float LoadTexture2D(NativeArray<half> textureRawBuffer, TextureWrapMode wrapModeU, TextureWrapMode wrapModeV, int2 coord, int2 resolution)
        {
            int2 repeatCoord = coord;
            repeatCoord.x = HandleWrapMode(repeatCoord.x, resolution.x, wrapModeU);
            repeatCoord.y = HandleWrapMode(repeatCoord.y, resolution.y, wrapModeV);
            int tapIndex = repeatCoord.x + repeatCoord.y * resolution.x;
            return textureRawBuffer[tapIndex];
        }

        static float LoadTexture2D(NativeArray<half> textureRawBuffer, int2 coord, int2 resolution)
        {
            int2 repeatCoord = coord;
            repeatCoord.x = SignedMod(repeatCoord.x, resolution.x);
            repeatCoord.y = SignedMod(repeatCoord.y, resolution.y);
            int tapIndex = repeatCoord.x + repeatCoord.y * resolution.x;
            return textureRawBuffer[tapIndex];
        }

        static void PrepareCoordinates(float2 uv, int2 resolution, out int2 tapCoord, out float2 fract)
        {
            float2 unnormalized = (uv * resolution) - 0.5f;
            tapCoord = (int2)floor(floor(unnormalized) + 0.5f);
            fract = frac(unnormalized);
        }

        static float4 SampleTexture2DArrayBilinear(NativeArray<float4> textureBuffer, float2 uvCoord, int sliceIndex, int resolution)
        {
            // Convert the position from uv to floating pixel coordinates (for the bilinear interpolation)
            PrepareCoordinates(uvCoord, resolution, out int2 currentTapCoord, out float2 fract);

            // Read the four samples we want
            float4 p0 = LoadTexture2DArray(textureBuffer, currentTapCoord, sliceIndex, resolution);
            float4 p1 = LoadTexture2DArray(textureBuffer, currentTapCoord + new int2(1, 0), sliceIndex, resolution);
            float4 p2 = LoadTexture2DArray(textureBuffer, currentTapCoord + new int2(0, 1), sliceIndex, resolution);
            float4 p3 = LoadTexture2DArray(textureBuffer, currentTapCoord + new int2(1, 1), sliceIndex, resolution);

            // Do the bilinear interpolation
            float4 i0 = lerp(p0, p1, fract.x);
            float4 i1 = lerp(p2, p3, fract.x);
            return lerp(i0, i1, fract.y);
        }

        static float4 SampleTexture2DArrayBilinear(NativeArray<half4> textureBuffer, float2 uvCoord, int sliceIndex, int resolution)
        {
            // Convert the position from uv to floating pixel coordinates (for the bilinear interpolation)
            PrepareCoordinates(uvCoord, resolution, out int2 currentTapCoord, out float2 fract);

            // Read the four samples we want
            float4 p0 = LoadTexture2DArray(textureBuffer, currentTapCoord, sliceIndex, resolution);
            float4 p1 = LoadTexture2DArray(textureBuffer, currentTapCoord + new int2(1, 0), sliceIndex, resolution);
            float4 p2 = LoadTexture2DArray(textureBuffer, currentTapCoord + new int2(0, 1), sliceIndex, resolution);
            float4 p3 = LoadTexture2DArray(textureBuffer, currentTapCoord + new int2(1, 1), sliceIndex, resolution);

            // Do the bilinear interpolation
            float4 i0 = lerp(p0, p1, fract.x);
            float4 i1 = lerp(p2, p3, fract.x);
            return lerp(i0, i1, fract.y);
        }

        static float4 SampleTexture2DBilinear(NativeArray<uint> textureBuffer, float2 uvCoord, int2 resolution, TextureWrapMode wrapModeU, TextureWrapMode wrapModeV)
        {
            // Convert the position from uv to floating pixel coordinates (for the bilinear interpolation)
            PrepareCoordinates(uvCoord, resolution, out int2 currentTapCoord, out float2 fract);

            // Read the four samples we want
            float4 p0 = LoadTexture2D(textureBuffer, wrapModeU, wrapModeV, currentTapCoord, resolution);
            float4 p1 = LoadTexture2D(textureBuffer, wrapModeU, wrapModeV, currentTapCoord + new int2(1, 0), resolution);
            float4 p2 = LoadTexture2D(textureBuffer, wrapModeU, wrapModeV, currentTapCoord + new int2(0, 1), resolution);
            float4 p3 = LoadTexture2D(textureBuffer, wrapModeU, wrapModeV, currentTapCoord + new int2(1, 1), resolution);

            // Do the bilinear interpolation
            float4 i0 = lerp(p0, p1, fract.x);
            float4 i1 = lerp(p2, p3, fract.x);
            return lerp(i0, i1, fract.y);
        }

        static float SampleTexture2DBilinear(NativeArray<half> textureBuffer, float2 uvCoord, int2 resolution)
        {
            // Convert the position from uv to floating pixel coordinates (for the bilinear interpolation)
            PrepareCoordinates(uvCoord, resolution, out int2 currentTapCoord, out float2 fract);

            // Read the four samples we want
            float p0 = LoadTexture2D(textureBuffer, currentTapCoord, resolution);
            float p1 = LoadTexture2D(textureBuffer, currentTapCoord + new int2(1, 0), resolution);
            float p2 = LoadTexture2D(textureBuffer, currentTapCoord + new int2(0, 1), resolution);
            float p3 = LoadTexture2D(textureBuffer, currentTapCoord + new int2(1, 1), resolution);

            // Do the bilinear interpolation
            float i0 = lerp(p0, p1, fract.x);
            float i1 = lerp(p2, p3, fract.x);
            return lerp(i0, i1, fract.y);
        }

        static float2 SampleTexture2DBilinear_float2(NativeArray<short> textureBuffer, float2 uvCoord, int2 resolution)
        {
            // Convert the position from uv to floating pixel coordinates (for the bilinear interpolation)
            PrepareCoordinates(uvCoord, resolution, out int2 currentTapCoord, out float2 fract);

            // Read the four samples we want
            float2 p0 = LoadTexture2D(textureBuffer, currentTapCoord, resolution);
            float2 p1 = LoadTexture2D(textureBuffer, currentTapCoord + new int2(1, 0), resolution);
            float2 p2 = LoadTexture2D(textureBuffer, currentTapCoord + new int2(0, 1), resolution);
            float2 p3 = LoadTexture2D(textureBuffer, currentTapCoord + new int2(1, 1), resolution);

            // Do the bilinear interpolation
            float2 i0 = lerp(p0, p1, fract.x);
            float2 i1 = lerp(p2, p3, fract.x);
            return lerp(i0, i1, fract.y);
        }

        static float2 EvaluateWaterGroup0CurrentUV(in WaterSimSearchData wsd, float2 currentUV)
        {
            if (wsd.decalWorkflow)
            {
                float3 positionAWS = mul(wsd.rendering.waterToWorldMatrix, float4(currentUV.x, 0, currentUV.y, 1.0f)).xyz;
                return EvaluateDecalUV(wsd, positionAWS);
            }

            return float2(currentUV.x - wsd.group0CurrentRegionOffset.x, currentUV.y + wsd.group0CurrentRegionOffset.y) * wsd.group0CurrentRegionScale + 0.5f;
        }

        static float2 EvaluateWaterGroup1CurrentUV(in WaterSimSearchData wsd, float2 currentUV)
        {
            if (wsd.decalWorkflow)
            {
                float3 positionAWS = mul(wsd.rendering.waterToWorldMatrix, float4(currentUV.x, 0, currentUV.y, 1.0f)).xyz;
                return EvaluateDecalUV(wsd, positionAWS);
            }

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
            float3 largeDirection = SampleTexture2DBilinear(wsd.group0CurrentMap, EvaluateWaterGroup0CurrentUV(wsd, currentUV), wsd.group0CurrentMapResolution, wsd.group0CurrentMapWrapModeU, wsd.group0CurrentMapWrapModeV).xyz;
            DecompressDirection(wsd, largeDirection, wsd.spectrum.groupOrientation.x * Mathf.Deg2Rad, wsd.group0CurrentMapInfluence, out currentData);
        }

        static void EvaluateGroup1CurrentData(in WaterSimSearchData wsd, float2 currentUV, out CurrentData currentData)
        {
            float3 ripplesDirection = SampleTexture2DBilinear(wsd.group1CurrentMap, EvaluateWaterGroup1CurrentUV(wsd, currentUV), wsd.group1CurrentMapResolution, wsd.group1CurrentMapWrapModeU, wsd.group1CurrentMapWrapModeV).xyz;
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

        static void ComputePatchSimData(in WaterSimSearchData wsd, float2 uv, int bandIdx, out PatchSimData simData)
        {
            simData.uv = (uv - OrientationToDirection(wsd.spectrum.patchOrientation[bandIdx]) * wsd.rendering.patchCurrentSpeed[bandIdx] * wsd.rendering.simulationTime) / wsd.spectrum.patchSizes[bandIdx];
            simData.blend = 1.0f;
            simData.swizzle = float4(1, 0, 0, 1);
        }

        static void ComputeWaterUVs(in WaterSimSearchData wsd, float2 uv, out WaterSimCoord simCoord)
        {
            ComputePatchSimData(wsd, uv, 0, out simCoord.data0);
            ComputePatchSimData(wsd, uv, 1, out simCoord.data1);
            ComputePatchSimData(wsd, uv, 2, out simCoord.data2);
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

        static void AddBandContribution(in WaterSimSearchData wsd, in PatchSimData data, int bandIdx, ref float2 horizontalDisplacement, ref float3 verticalDisplacements)
        {
            float3 rawDisplacement = wsd.cpuSimulation ? SampleTexture2DArrayBilinear(wsd.displacementDataCPU, data.uv, bandIdx, wsd.simulationRes).xyz :
                SampleTexture2DArrayBilinear(wsd.displacementDataGPU, data.uv, bandIdx, wsd.simulationRes).xyz;
            rawDisplacement *= wsd.rendering.patchAmplitudeMultiplier[bandIdx] * data.blend;

            horizontalDisplacement += float2(dot(rawDisplacement.yz, data.swizzle.xy), dot(rawDisplacement.yz, data.swizzle.zw));
            verticalDisplacements[bandIdx] = rawDisplacement.x;

        }

        static void EvaluateWaterSimulation(in WaterSimSearchData wsd, WaterSimCoord sc, out float2 horizontalDisplacement, out float3 verticalDisplacements)
        {
            horizontalDisplacement = 0.0f;
            verticalDisplacements = 0.0f;

            AddBandContribution(in wsd, in sc.data0, 0, ref horizontalDisplacement, ref verticalDisplacements);

            if (wsd.activeBandCount > 1)
                AddBandContribution(in wsd, in sc.data1, 1, ref horizontalDisplacement, ref verticalDisplacements);

            if (wsd.activeBandCount > 2)
                AddBandContribution(in wsd, in sc.data2, 2, ref horizontalDisplacement, ref verticalDisplacements);
        }

        static float2 EvaluateDecalUV(in WaterSimSearchData wsd, float3 positionAWS)
        {
            return (positionAWS.xz - wsd.decalRegionCenter) * wsd.decalRegionScale + 0.5f;
        }

        static float3 EvaluateWaterMask(in WaterSimSearchData wsd, float3 positionAWS)
        {
            float3 waterMask = 1.0f;
            if (wsd.activeMask)
            {
                if (wsd.decalWorkflow)
                {
                    float2 maskUV = EvaluateDecalUV(wsd, positionAWS);
                    waterMask = all(maskUV == saturate(maskUV)) ? SampleTexture2DBilinear(wsd.maskBuffer, maskUV, wsd.maskResolution, TextureWrapMode.Clamp, TextureWrapMode.Clamp).xyz : 1;
                }
                else
                {
                    float2 maskUV = RotateUV(wsd, positionAWS.xz - wsd.maskOffset) * wsd.maskScale + 0.5f;
                    waterMask = SampleTexture2DBilinear(wsd.maskBuffer, maskUV, wsd.maskResolution, wsd.maskWrapModeU, wsd.maskWrapModeV).xyz;
                    waterMask = wsd.maskRemap.xxx + waterMask * wsd.maskRemap.yyy;
                }
            }
            return waterMask;
        }

        static float3 ShuffleDisplacement(float3 displacement)
        {
            return float3(-displacement.y, displacement.x, -displacement.z);
        }

        static void EvaluateDisplacedPoints(float3 displacementC, float3 displacementR, float3 displacementU,
                                        float normalization, float pixelSize,
                                        out float3 p0, out float3 p1, out float3 p2)
        {
            p0 = displacementC * normalization;
            p1 = displacementR * normalization + float3(pixelSize, 0, 0);
            p2 = displacementU * normalization + float3(0, 0, pixelSize);
        }

        static float3 SurfaceGradientFromPerturbedNormal(float3 nrmVertexNormal, float3 v)
        {
            float3 n = nrmVertexNormal;
            float s = 1.0f / max(float.Epsilon, abs(dot(n, v)));
            return s * (dot(n, v) * n - v);
        }

        static float2 EvaluateSurfaceGradients(float3 p0, float3 p1, float3 p2)
        {
            float3 v0 = normalize(p1 - p0);
            float3 v1 = normalize(p2 - p0);
            float3 geometryNormal = normalize(cross(v1, v0));
            return SurfaceGradientFromPerturbedNormal(float3(0, 1, 0), geometryNormal).xz;
        }

        static float3 SurfaceGradientResolveNormal(float3 nrmVertexNormal, float3 surfGrad)
        {
            return normalize(nrmVertexNormal - surfGrad);
        }
    }
}
