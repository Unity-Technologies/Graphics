
using System;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal
{
    internal struct URPLightShadowCullingInfos
    {
        public NativeArray<ShadowSliceData> slices;
        public uint slicesValidMask;

        public readonly bool IsSliceValid(int i) => (slicesValidMask & (1 << i)) != 0;
    }

    internal static class ShadowCulling
    {
        static readonly ProfilingSampler computeShadowCasterCullingInfosMarker = new ProfilingSampler($"{nameof(UniversalRenderPipeline)}.{nameof(ComputeShadowCasterCullingInfos)}");

        public static NativeArray<URPLightShadowCullingInfos> CullShadowCasters(ref ScriptableRenderContext context,
            UniversalShadowData shadowData,
            ref AdditionalLightsShadowAtlasLayout shadowAtlasLayout,
            ref CullingResults cullResults)
        {
            ShadowCastersCullingInfos shadowCullingInfos;
            NativeArray<URPLightShadowCullingInfos> urpVisibleLightsShadowCullingInfos;
            ComputeShadowCasterCullingInfos(shadowData, ref shadowAtlasLayout, ref cullResults, out shadowCullingInfos, out urpVisibleLightsShadowCullingInfos);

            context.CullShadowCasters(cullResults, shadowCullingInfos);

            return urpVisibleLightsShadowCullingInfos;
        }

        static void ComputeShadowCasterCullingInfos(UniversalShadowData shadowData,
            ref AdditionalLightsShadowAtlasLayout shadowAtlasLayout,
            ref CullingResults cullingResults,
            out ShadowCastersCullingInfos shadowCullingInfos,
            out NativeArray<URPLightShadowCullingInfos> urpVisibleLightsShadowCullingInfos)
        {
            const int MaxShadowSplitCount = 6;
            using var profScope = new ProfilingScope(computeShadowCasterCullingInfosMarker);

            NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
            NativeArray<ShadowSplitData> splitBuffer = new NativeArray<ShadowSplitData>(visibleLights.Length * MaxShadowSplitCount, Allocator.Temp);
            NativeArray<LightShadowCasterCullingInfo> perLightInfos = new NativeArray<LightShadowCasterCullingInfo>(visibleLights.Length, Allocator.Temp);
            urpVisibleLightsShadowCullingInfos = new NativeArray<URPLightShadowCullingInfos>(visibleLights.Length, Allocator.Temp);

            int totalSplitCount = 0;
            int splitBufferOffset = 0;

            for (int lightIndex = 0; lightIndex < visibleLights.Length; ++lightIndex)
            {
                ref VisibleLight visibleLight = ref cullingResults.visibleLights.UnsafeElementAt(lightIndex);
                LightType lightType = visibleLight.lightType;

                NativeArray<ShadowSliceData> slices = default;
                uint slicesValidMask = 0;

                if (lightType == LightType.Directional)
                {
                    if (!shadowData.supportsMainLightShadows)
                        continue;

                    int splitCount = shadowData.mainLightShadowCascadesCount;
                    int renderTargetWidth = shadowData.mainLightRenderTargetWidth;
                    int renderTargetHeight = shadowData.mainLightRenderTargetHeight;
                    int shadowResolution = shadowData.mainLightShadowResolution;

                    slices = new NativeArray<ShadowSliceData>(splitCount, Allocator.Temp);
                    slicesValidMask = 0;

                    for (int i = 0; i < splitCount; ++i)
                    {
                        ShadowSliceData slice = default;
                        bool isValid = ShadowUtils.ExtractDirectionalLightMatrix(ref cullingResults, shadowData,
                            lightIndex, i, renderTargetWidth, renderTargetHeight, shadowResolution, visibleLight.light.shadowNearPlane,
                            out _, // Vector4 cascadeSplitDistance. This is basically just the culling sphere which is already present in ShadowSplitData
                            out slice);

                        if (isValid)
                            slicesValidMask |= 1u << i;

                        slices[i] = slice;
                        splitBuffer[splitBufferOffset + i] = slice.splitData;
                    }
                }
                else if (lightType == LightType.Point)
                {
                    if (!shadowData.supportsAdditionalLightShadows || !shadowAtlasLayout.HasSpaceForLight(lightIndex))
                        continue;

                    int splitCount = ShadowUtils.GetPunctualLightShadowSlicesCount(lightType);
                    int sliceResolution = shadowAtlasLayout.GetSliceShadowResolutionRequest(lightIndex, 0).allocatedResolution;
                    bool shadowFiltering = visibleLight.light.shadows == LightShadows.Soft;

                    // Note: the same fovBias will also be used to compute ShadowUtils.GetShadowBias
                    float fovBias = Internal.AdditionalLightsShadowCasterPass.GetPointLightShadowFrustumFovBiasInDegrees(sliceResolution, shadowFiltering);

                    slices = new NativeArray<ShadowSliceData>(splitCount, Allocator.Temp);
                    slicesValidMask = 0;

                    for (int i = 0; i < splitCount; ++i)
                    {
                        ShadowSliceData slice = default;
                        bool isValid = ShadowUtils.ExtractPointLightMatrix(ref cullingResults,
                            shadowData,
                            lightIndex,
                            (CubemapFace)i,
                            fovBias,
                            out slice.shadowTransform,
                            out slice.viewMatrix,
                            out slice.projectionMatrix,
                            out slice.splitData);

                        if (isValid)
                            slicesValidMask |= 1u << i;

                        slices[i] = slice;
                        splitBuffer[splitBufferOffset + i] = slice.splitData;
                    }
                }
                else if (lightType == LightType.Spot)
                {
                    if (!shadowData.supportsAdditionalLightShadows || !shadowAtlasLayout.HasSpaceForLight(lightIndex))
                        continue;

                    slices = new NativeArray<ShadowSliceData>(1, Allocator.Temp);
                    slicesValidMask = 0;

                    ShadowSliceData slice = default;
                    bool isValid = ShadowUtils.ExtractSpotLightMatrix(ref cullingResults,
                        shadowData,
                        lightIndex,
                        out slice.shadowTransform,
                        out slice.viewMatrix,
                        out slice.projectionMatrix,
                        out slice.splitData);

                    if (isValid)
                        slicesValidMask |= 1u << 0;

                    slices[0] = slice;
                    splitBuffer[splitBufferOffset + 0] = slice.splitData;
                }

                URPLightShadowCullingInfos infos = default;
                infos.slices = slices;
                infos.slicesValidMask = slicesValidMask;

                urpVisibleLightsShadowCullingInfos[lightIndex] = infos;
                perLightInfos[lightIndex] = new LightShadowCasterCullingInfo
                {
                    splitRange = new RangeInt(splitBufferOffset, slices.Length),
                    projectionType = GetCullingProjectionType(lightType),
                };
                splitBufferOffset += slices.Length;
                totalSplitCount += slices.Length;
            }

            shadowCullingInfos = default;
            shadowCullingInfos.splitBuffer = splitBuffer.GetSubArray(0, totalSplitCount);
            shadowCullingInfos.perLightInfos = perLightInfos;
        }

        static BatchCullingProjectionType GetCullingProjectionType(LightType type)
        {
            switch (type)
            {
                case LightType.Point: return BatchCullingProjectionType.Perspective;
                case LightType.Spot: return BatchCullingProjectionType.Perspective;
                case LightType.Directional: return BatchCullingProjectionType.Orthographic;
            }

            return BatchCullingProjectionType.Unknown;
        }
    }
}
