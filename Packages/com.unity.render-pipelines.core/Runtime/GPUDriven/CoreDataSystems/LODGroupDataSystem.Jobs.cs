using System;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Mathematics;

namespace UnityEngine.Rendering
{
    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal struct UpdateLODGroupTransformJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<JaggedJobRange> jobRanges;
        [ReadOnly] public NativeParallelHashMap<EntityId, GPUInstanceIndex> lodGroupDataHash;
        [ReadOnly] public JaggedSpan<EntityId> jaggedLODGroups;
        [ReadOnly] public JaggedSpan<float> jaggedWorldSpaceSizes;
        [ReadOnly] public JaggedSpan<float3> jaggedWorldSpaceReferencePoints;
        [ReadOnly] public bool requiresGPUUpload;
        [ReadOnly] public bool supportDitheringCrossFade;

        [NativeDisableContainerSafetyRestriction, NoAlias, ReadOnly] public NativeList<LODGroupData> lodGroupDatas;
        [NativeDisableContainerSafetyRestriction, NoAlias, WriteOnly] public NativeList<LODGroupCullingData> lodGroupCullingDatas;

        public unsafe void Execute(int jobIndex)
        {
            var jobRange = jobRanges[jobIndex];

            NativeArray<EntityId> lodGroupSection = jaggedLODGroups[jobRange.sectionIndex];
            NativeArray<float> worldSpaceSizesSection = jaggedWorldSpaceSizes[jobRange.sectionIndex];
            NativeArray<float3> worldSpaceReferencePointSection = jaggedWorldSpaceReferencePoints[jobRange.sectionIndex];

            for (int indexInRange = 0; indexInRange < jobRange.length; indexInRange++)
            {
                int localIndex = jobRange.localStart + indexInRange;
                EntityId lodGroup = lodGroupSection[localIndex];

                if (lodGroupDataHash.TryGetValue(lodGroup, out var lodGroupInstance))
                {
                    float worldSpaceSize = worldSpaceSizesSection[localIndex];

                    ref LODGroupData lodGroupData = ref lodGroupDatas.ElementAt(lodGroupInstance.index);
                    ref LODGroupCullingData lodGroupTransformResult = ref lodGroupCullingDatas.ElementAt(lodGroupInstance.index);
                    lodGroupTransformResult.worldSpaceSize = worldSpaceSize;
                    lodGroupTransformResult.worldSpaceReferencePoint = worldSpaceReferencePointSection[localIndex];

                    for (int i = 0; i < lodGroupData.lodCount; ++i)
                    {
                        float lodHeight = lodGroupData.screenRelativeTransitionHeights[i];

                        float lodDist = LODRenderingUtils.CalculateLODDistance(lodHeight, worldSpaceSize);
                        lodGroupTransformResult.sqrDistances[i] = lodDist * lodDist;

                        if (supportDitheringCrossFade && !lodGroupTransformResult.percentageFlags[i])
                        {
                            float prevLODHeight = i != 0 ? lodGroupData.screenRelativeTransitionHeights[i - 1] : 1.0f;
                            float transitionHeight = lodHeight + lodGroupData.fadeTransitionWidth[i] * (prevLODHeight - lodHeight);
                            float transitionDistance = lodDist - LODRenderingUtils.CalculateLODDistance(transitionHeight, worldSpaceSize);
                            transitionDistance = Mathf.Max(0.0f, transitionDistance);
                            lodGroupTransformResult.transitionDistances[i] = transitionDistance;
                        }
                        else
                        {
                            lodGroupTransformResult.transitionDistances[i] = 0f;
                        }
                    }
                }
            }
        }
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal unsafe struct UpdateLODGroupDataJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<JaggedJobRange> jobRanges;
        [ReadOnly] public NativeArray<GPUInstanceIndex> lodGroupInstances;
        [ReadOnly] public LODGroupUpdateBatch updateBatch;
        [ReadOnly] public bool supportDitheringCrossFade;

        public NativeArray<LODGroupData> lodGroupsData;
        public NativeArray<LODGroupCullingData> lodGroupsCullingData;

        [NativeDisableUnsafePtrRestriction] public UnsafeAtomicCounter32 rendererCount;

        public void Execute(int jobIndex)
        {
            JaggedJobRange jobRange = jobRanges[jobIndex];

            NativeArray<float3> worldReferencePointSection = updateBatch.worldSpaceReferencePoints[jobRange.sectionIndex];
            NativeArray<float> worldSpaceSizeSection = updateBatch.worldSpaceSizes[jobRange.sectionIndex];
            NativeArray<InternalLODGroupSettings> lodGroupSettingsSection = updateBatch.lodGroupSettings[jobRange.sectionIndex];
            var hasForceLOD = updateBatch.HasAnyComponent(LODGroupComponentMask.ForceLOD);
            NativeArray<byte> forceLODMaskSection = hasForceLOD ? updateBatch.forceLODMask[jobRange.sectionIndex] : default;
            NativeArray<EmbeddedLODBuffer> lodBufferSection = updateBatch.lodBuffers[jobRange.sectionIndex];


            for (int indexInRange = 0; indexInRange < jobRange.length; indexInRange++)
            {
                int localIndex = jobRange.localStart + indexInRange;
                int absoluteIndex = jobRange.absoluteStart + indexInRange;

                GPUInstanceIndex lodGroupInstance = lodGroupInstances[absoluteIndex];
                float3 worldReferencePoint = worldReferencePointSection[localIndex];
                float worldSpaceSize = worldSpaceSizeSection[localIndex];
                InternalLODGroupSettings lodGroupSettings = lodGroupSettingsSection[localIndex];
                byte forceLODMask = hasForceLOD ? forceLODMaskSection[localIndex] : (byte)0;
                ref readonly EmbeddedLODBuffer lodBuffer = ref lodBufferSection.ElementAt(localIndex);

                int lodCount = lodBuffer.Length;
                LODFadeMode fadeMode = lodGroupSettings.fadeMode;
                bool useDitheringCrossFade = fadeMode != LODFadeMode.None && supportDitheringCrossFade;
                bool useSpeedTreeCrossFade = fadeMode == LODFadeMode.SpeedTree;

                int totalRendererCount = 0;
                for (int i = 0; i < lodCount; i++)
                {
                    int lodRendererCount = lodBuffer.GetRendererCount(i);
                    totalRendererCount += lodRendererCount;
                }

                ref LODGroupData lodGroupData = ref lodGroupsData.ElementAtRW(lodGroupInstance.index);
                ref LODGroupCullingData lodGroupCullingData = ref lodGroupsCullingData.ElementAtRW(lodGroupInstance.index);

                lodGroupData.valid = true;
                lodGroupData.lodCount = lodCount;
                lodGroupData.rendererCount = useDitheringCrossFade ? totalRendererCount : 0;
                lodGroupCullingData.worldSpaceSize = worldSpaceSize;
                lodGroupCullingData.worldSpaceReferencePoint = worldReferencePoint;
                lodGroupCullingData.forceLODMask = forceLODMask;
                lodGroupCullingData.lodCount = lodCount;

                rendererCount.Add(lodGroupData.rendererCount);

                int crossFadeLODBegin = 0;

                if (useSpeedTreeCrossFade)
                {
                    int lastLODIndex = lodCount - 1;
                    bool hasBillboardLOD = lodCount > 0 && lodBuffer.GetRendererCount(lastLODIndex) == 1 && lodGroupSettings.lastLODIsBillboard;

                    if (lodCount == 0)
                        crossFadeLODBegin = 0;
                    else if (hasBillboardLOD)
                        crossFadeLODBegin = math.max(lodCount, 2) - 2;
                    else
                        crossFadeLODBegin = lodCount - 1;
                }

                for (int i = 0; i < lodCount; ++i)
                {
                    float lodHeight = lodBuffer.GetScreenRelativeTransitionHeight(i);
                    float lodDist = LODRenderingUtils.CalculateLODDistance(lodHeight, worldSpaceSize);

                    lodGroupData.screenRelativeTransitionHeights[i] = lodHeight;
                    lodGroupData.fadeTransitionWidth[i] = 0.0f;
                    lodGroupCullingData.sqrDistances[i] = lodDist * lodDist;
                    lodGroupCullingData.percentageFlags[i] = false;
                    lodGroupCullingData.transitionDistances[i] = 0.0f;

                    if (useSpeedTreeCrossFade && i < crossFadeLODBegin)
                    {
                        lodGroupCullingData.percentageFlags[i] = true;
                    }
                    else if (useDitheringCrossFade && i >= crossFadeLODBegin)
                    {
                        float fadeTransitionWidth = lodBuffer.GetFadeTransitionWidth(i);
                        float prevLODHeight = i != 0 ? lodBuffer.GetScreenRelativeTransitionHeight(i - 1) : 1.0f;
                        float transitionHeight = lodHeight + fadeTransitionWidth * (prevLODHeight - lodHeight);
                        float transitionDistance = lodDist - LODRenderingUtils.CalculateLODDistance(transitionHeight, worldSpaceSize);
                        transitionDistance = Mathf.Max(0.0f, transitionDistance);

                        lodGroupData.fadeTransitionWidth[i] = fadeTransitionWidth;
                        lodGroupCullingData.transitionDistances[i] = transitionDistance;
                    }
                }
            }
        }
    }
}
