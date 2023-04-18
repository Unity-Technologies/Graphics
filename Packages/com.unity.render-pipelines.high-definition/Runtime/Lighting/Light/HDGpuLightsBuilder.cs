using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering.HighDefinition
{
    internal partial class HDGpuLightsBuilder
    {
        #region internal HDRP API

        public struct LightsPerView
        {
            public Matrix4x4 worldToView;
            public int boundsOffset;
            public int boundsCount;
        }

        public const int ArrayCapacity = 100;

        //Light GPU ready arrays
        public NativeArray<LightData> lights => m_Lights;
        public int lightsCount => m_LightCount;
        public NativeArray<DirectionalLightData> directionalLights => m_DirectionalLights;
        public int directionalLightCount => m_LightTypeCounters.IsCreated ? m_LightTypeCounters[(int)GPULightTypeCountSlots.Directional] : 0;
        public int punctualLightCount => m_LightTypeCounters.IsCreated ? m_LightTypeCounters[(int)GPULightTypeCountSlots.Punctual] : 0;
        public int areaLightCount => m_LightTypeCounters.IsCreated ? m_LightTypeCounters[(int)GPULightTypeCountSlots.Area] : 0;


        //Auxiliary GPU arrays for coarse culling
        public NativeArray<LightsPerView> lightsPerView => m_LightsPerView;
        public NativeArray<SFiniteLightBound> lightBounds => m_LightBounds;
        public NativeArray<LightVolumeData> lightVolumes => m_LightVolumes;
        public int lightsPerViewCount => m_LightsPerViewCount;
        public int lightBoundsCount => m_LightBoundsCount;
        public int boundsEyeDataOffset => m_BoundsEyeDataOffset;
        public int allLightBoundsCount => m_BoundsEyeDataOffset * lightsPerViewCount;

        //Counters / singleton lights for shadows etc
        public int currentShadowSortedSunLightIndex => m_CurrentShadowSortedSunLightIndex;
        public HDProcessedVisibleLightsBuilder.ShadowMapFlags currentSunShadowMapFlags => m_CurrentSunShadowMapFlags;
        public DirectionalLightData currentSunLightDirectionalLightData => m_CurrentSunLightDirectionalLightData;
        public int contactShadowIndex => m_ContactShadowIndex;
        public int screenSpaceShadowIndex => m_ScreenSpaceShadowIndex;
        public int screenSpaceShadowChannelSlot => m_ScreenSpaceShadowChannelSlot;
        public int debugSelectedLightShadowIndex => m_DebugSelectedLightShadowIndex;
        public int debugSelectedLightShadowCount => m_DebugSelectedLightShadowCount;
        public HDRenderPipeline.ScreenSpaceShadowData[] currentScreenSpaceShadowData => m_CurrentScreenSpaceShadowData;

        //Packs a sort key for a light
        public static uint PackLightSortKey(LightCategory lightCategory, GPULightType gpuLightType, LightVolumeType lightVolumeType, int lightIndex)
        {
            //We sort directional lights to be in the beginning of the list.
            //This ensures that we can access directional lights very easily after we sort them.
            uint isDirectionalMSB = gpuLightType == GPULightType.Directional ? 0u : 1u;
            uint sortKey = (uint)isDirectionalMSB << 31 | (uint)lightCategory << 27 | (uint)gpuLightType << 22 | (uint)lightVolumeType << 17 | (uint)lightIndex;
            return sortKey;
        }

        //Unpacks a sort key for a light
        public static void UnpackLightSortKey(uint sortKey, out LightCategory lightCategory, out GPULightType gpuLightType, out LightVolumeType lightVolumeType, out int lightIndex)
        {
            lightCategory = (LightCategory)((sortKey >> 27) & 0xF);
            gpuLightType = (GPULightType)((sortKey >> 22) & 0x1F);
            lightVolumeType = (LightVolumeType)((sortKey >> 17) & 0x1F);
            lightIndex = (int)(sortKey & 0xFFFF);
        }

        //Initialization of builder
        public void Initialize(HDRenderPipelineAsset asset, HDShadowManager shadowManager, HDRenderPipeline.LightLoopTextureCaches textureCaches)
        {
            m_Asset = asset;
            m_TextureCaches = textureCaches;
            m_ShadowManager = shadowManager;

            // Screen space shadow
            int numMaxShadows = Math.Max(m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots, 1);
            m_CurrentScreenSpaceShadowData = new HDRenderPipeline.ScreenSpaceShadowData[numMaxShadows];

            //Allocate all the GPU critical buffers, for the case were there are no lights.
            //This ensures we can bind an empty buffer on ComputeBuffer SetData() call
            AllocateLightData(0, 0);
        }

        //Adds bounds for a new light type. Reflection probes / decals add their bounds here.
        public void AddLightBounds(int viewId, in SFiniteLightBound lightBound, in LightVolumeData volumeData)
        {
            var viewData = m_LightsPerView[viewId];
            m_LightBounds[viewData.boundsOffset + viewData.boundsCount] = lightBound;
            m_LightVolumes[viewData.boundsOffset + viewData.boundsCount] = volumeData;
            ++viewData.boundsCount;
            m_LightsPerView[viewId] = viewData;
        }

        //Cleans up / disposes light info.
        public void Cleanup()
        {
            if (m_Lights.IsCreated)
                m_Lights.Dispose();

            if (m_DirectionalLights.IsCreated)
                m_DirectionalLights.Dispose();

            if (m_LightsPerView.IsCreated)
                m_LightsPerView.Dispose();

            if (m_LightBounds.IsCreated)
                m_LightBounds.Dispose();

            if (m_LightVolumes.IsCreated)
                m_LightVolumes.Dispose();

            if (m_LightTypeCounters.IsCreated)
                m_LightTypeCounters.Dispose();
        }
        #endregion

        #region private definitions

        internal enum GPULightTypeCountSlots
        {
            Directional,
            Punctual,
            Area
        }

        private NativeArray<LightsPerView> m_LightsPerView;
        private int m_LighsPerViewCapacity = 0;
        private int m_LightsPerViewCount = 0;

        private NativeArray<SFiniteLightBound> m_LightBounds;
        private NativeArray<LightVolumeData> m_LightVolumes;
        private int m_LightBoundsCapacity = 0;
        private int m_LightBoundsCount = 0;

        private NativeArray<LightData> m_Lights;
        private int m_LightCapacity = 0;
        private int m_LightCount = 0;

        private NativeArray<DirectionalLightData> m_DirectionalLights;
        private int m_DirectionalLightCapacity = 0;
        private int m_DirectionalLightCount = 0;

        private NativeArray<int> m_LightTypeCounters;

        private HDRenderPipelineAsset m_Asset;
        private HDShadowManager m_ShadowManager;
        private HDRenderPipeline.LightLoopTextureCaches m_TextureCaches;
        private HashSet<HDAdditionalLightData> m_ScreenSpaceShadowsUnion = new HashSet<HDAdditionalLightData>();

        private int m_CurrentShadowSortedSunLightIndex = -1;
        private HDProcessedVisibleLightsBuilder.ShadowMapFlags m_CurrentSunShadowMapFlags = HDProcessedVisibleLightsBuilder.ShadowMapFlags.None;
        private DirectionalLightData m_CurrentSunLightDirectionalLightData;

        private int m_ContactShadowIndex = 0;
        private int m_ScreenSpaceShadowIndex = 0;
        private int m_ScreenSpaceShadowChannelSlot = 0;
        private int m_DebugSelectedLightShadowIndex = 0;
        private int m_DebugSelectedLightShadowCount = 0;
        private HDRenderPipeline.ScreenSpaceShadowData[] m_CurrentScreenSpaceShadowData;

        private int m_BoundsEyeDataOffset = 0;

        private void AllocateLightData(int lightCount, int directionalLightCount)
        {
            int requestedLightCount = Math.Max(1, lightCount);
            if (requestedLightCount > m_LightCapacity)
            {
                m_LightCapacity = Math.Max(Math.Max(m_LightCapacity * 2, requestedLightCount), ArrayCapacity);
                m_Lights.ResizeArray(m_LightCapacity);
            }
            m_LightCount = lightCount;

            int requestedDurectinalCount = Math.Max(1, directionalLightCount);
            if (requestedDurectinalCount > m_DirectionalLightCapacity)
            {
                m_DirectionalLightCapacity = Math.Max(Math.Max(m_DirectionalLightCapacity * 2, requestedDurectinalCount), ArrayCapacity);
                m_DirectionalLights.ResizeArray(m_DirectionalLightCapacity);
            }
            m_DirectionalLightCount = directionalLightCount;
        }

        #endregion
    }
}
