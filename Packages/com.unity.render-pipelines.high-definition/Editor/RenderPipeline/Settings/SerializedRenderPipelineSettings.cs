using System;
using UnityEngine.Serialization;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedLightSettings
    {
        public SerializedScalableSetting useContactShadows;

        public SerializedLightSettings(SerializedProperty root)
        {
            useContactShadows = new SerializedScalableSetting(root.Find((RenderPipelineSettings.LightSettings s) => s.useContactShadow));
        }
    }

    class SerializedRenderPipelineSettings
    {
        public SerializedProperty root;

        public SerializedProperty supportShadowMask;
        public SerializedProperty supportSSR;
        public SerializedProperty supportSSRTransparent;
        public SerializedProperty supportSSAO;
        public SerializedProperty supportSSGI;
        public SerializedProperty supportSubsurfaceScattering;
        public SerializedScalableSetting sssSampleBudget;
        public SerializedScalableSetting sssDownsampleSteps;
        [FormerlySerializedAs("supportVolumetric")]
        public SerializedProperty supportVolumetrics;
        public SerializedProperty supportVolumetricClouds;

        // Water
        public SerializedProperty supportWater;
        public SerializedProperty waterSimulationResolution;
        public SerializedProperty supportWaterExclusion;
        public SerializedProperty supportWaterDecals;
        public SerializedProperty waterDecalAtlasSize;
        public SerializedProperty maximumWaterDecalCount;
        public SerializedProperty waterScriptInteractionsMode;
        public SerializedProperty waterFullCPUSimulation;

        public SerializedProperty supportComputeThickness;
        public SerializedProperty computeThicknessResolution;
        public SerializedProperty computeThicknessLayerMask;

        public SerializedProperty supportLightLayers;
        public SerializedProperty supportedLitShaderMode;
        public SerializedProperty colorBufferFormat;
        public SerializedProperty supportCustomPass;
        public SerializedProperty customBufferFormat;
        public SerializedProperty renderingLayerMaskBuffer;
        public SerializedScalableSetting planarReflectionResolution;
        public SerializedScalableSetting cubeReflectionResolution;
        public SerializedProperty supportDecals;
        public SerializedProperty supportDecalLayers;
        public SerializedProperty supportSurfaceGradient;
        public SerializedProperty decalNormalBufferHP;
        public SerializedProperty supportHighQualityLineRendering;
        public SerializedProperty highQualityLineRenderingMemoryBudget;

        public SerializedProperty MSAASampleCount;
        public SerializedProperty supportMotionVectors;
        public SerializedProperty supportRuntimeAOVAPI;
        public SerializedProperty supportTerrainHole;
        public SerializedProperty supportRayTracing;
        public SerializedProperty supportVFXRayTracing;
        public SerializedProperty supportedRayTracingMode;
        public SerializedProperty supportDistortion;
        public SerializedProperty supportTransparentBackface;
        public SerializedProperty supportTransparentDepthPrepass;
        public SerializedProperty supportTransparentDepthPostpass;
        internal SerializedProperty lightProbeSystem;
        internal SerializedProperty probeVolumeTextureSize;
        internal SerializedProperty supportProbeVolumeScenarios;
        internal SerializedProperty supportProbeVolumeScenarioBlending;
        internal SerializedProperty probeVolumeBlendingTextureSize;
        internal SerializedProperty supportProbeVolumeGPUStreaming;
        internal SerializedProperty supportProbeVolumeDiskStreaming;
        internal SerializedProperty probeVolumeSHBands;

        public SerializedProperty supportScreenSpaceLensFlare;
        public SerializedProperty supportDataDrivenLensFlare;
        
        public SerializedGlobalLightLoopSettings lightLoopSettings;
        public SerializedHDShadowInitParameters hdShadowInitParams;
        public SerializedGlobalDecalSettings decalSettings;
        public SerializedGlobalPostProcessSettings postProcessSettings;
        public SerializedDynamicResolutionSettings dynamicResolutionSettings;
        public SerializedLowResTransparencySettings lowresTransparentSettings;
        public SerializedXRSettings xrSettings;
        public SerializedPostProcessingQualitySettings postProcessQualitySettings;
        public SerializedLightingQualitySettings lightingQualitySettings;
        public SerializedGPUResidentDrawerSettings gpuResidentDrawerSettings;

        public SerializedLightSettings lightSettings;
        public SerializedScalableSetting lodBias;
        public SerializedScalableSetting maximumLODLevel;

#pragma warning disable 618 // Type or member is obsolete
        [FormerlySerializedAs("enableUltraQualitySSS"), FormerlySerializedAs("increaseSssSampleCount"), Obsolete("For data migration")]
        SerializedProperty m_ObsoleteincreaseSssSampleCount;

        [FormerlySerializedAs("supportDitheringCrossFade"), Obsolete("Merged with LOD Quality Setting")]
        private SerializedProperty m_ObsoleteSupportDitheringCrossFade;
#pragma warning restore 618

        public SerializedRenderPipelineSettings(SerializedProperty root)
        {
            this.root = root;

            supportShadowMask = root.Find((RenderPipelineSettings s) => s.supportShadowMask);
            supportSSR = root.Find((RenderPipelineSettings s) => s.supportSSR);
            supportSSRTransparent = root.Find((RenderPipelineSettings s) => s.supportSSRTransparent);
            supportSSAO = root.Find((RenderPipelineSettings s) => s.supportSSAO);
            supportSSGI = root.Find((RenderPipelineSettings s) => s.supportSSGI);
            supportSubsurfaceScattering = root.Find((RenderPipelineSettings s) => s.supportSubsurfaceScattering);
            sssSampleBudget = new SerializedScalableSetting(root.Find((RenderPipelineSettings s) => s.sssSampleBudget));
            sssDownsampleSteps = new SerializedScalableSetting(root.Find((RenderPipelineSettings s) => s.sssDownsampleSteps));
            supportVolumetrics = root.Find((RenderPipelineSettings s) => s.supportVolumetrics);
            supportVolumetricClouds = root.Find((RenderPipelineSettings s) => s.supportVolumetricClouds);

            // Water data
            supportWater = root.Find((RenderPipelineSettings s) => s.supportWater);
            waterSimulationResolution = root.Find((RenderPipelineSettings s) => s.waterSimulationResolution);
            supportWaterExclusion = root.Find((RenderPipelineSettings s) => s.supportWaterExclusion);
            supportWaterDecals = root.Find((RenderPipelineSettings s) => s.supportWaterDecals);
            waterDecalAtlasSize = root.Find((RenderPipelineSettings s) => s.waterDecalAtlasSize);
            maximumWaterDecalCount = root.Find((RenderPipelineSettings s) => s.maximumWaterDecalCount);
            waterScriptInteractionsMode = root.Find((RenderPipelineSettings s) => s.waterScriptInteractionsMode);
            waterFullCPUSimulation = root.Find((RenderPipelineSettings s) => s.waterFullCPUSimulation);

            supportComputeThickness = root.Find((RenderPipelineSettings s) => s.supportComputeThickness);
            computeThicknessResolution = root.Find((RenderPipelineSettings s) => s.computeThicknessResolution);
            computeThicknessLayerMask = root.Find((RenderPipelineSettings s) => s.computeThicknessLayerMask);

            supportLightLayers = root.Find((RenderPipelineSettings s) => s.supportLightLayers);
            colorBufferFormat = root.Find((RenderPipelineSettings s) => s.colorBufferFormat);
            customBufferFormat = root.Find((RenderPipelineSettings s) => s.customBufferFormat);
            renderingLayerMaskBuffer = root.Find((RenderPipelineSettings s) => s.renderingLayerMaskBuffer);
            supportCustomPass = root.Find((RenderPipelineSettings s) => s.supportCustomPass);
            supportedLitShaderMode = root.Find((RenderPipelineSettings s) => s.supportedLitShaderMode);
            planarReflectionResolution = new SerializedScalableSetting(root.Find((RenderPipelineSettings s) => s.planarReflectionResolution));
            cubeReflectionResolution = new SerializedScalableSetting(root.Find((RenderPipelineSettings s) => s.cubeReflectionResolution));

            supportDecals = root.Find((RenderPipelineSettings s) => s.supportDecals);
            supportDecalLayers = root.Find((RenderPipelineSettings s) => s.supportDecalLayers);
            supportSurfaceGradient = root.Find((RenderPipelineSettings s) => s.supportSurfaceGradient);
            decalNormalBufferHP = root.Find((RenderPipelineSettings s) => s.decalNormalBufferHP);
            MSAASampleCount = root.Find((RenderPipelineSettings s) => s.msaaSampleCount);
            supportMotionVectors = root.Find((RenderPipelineSettings s) => s.supportMotionVectors);
            supportRuntimeAOVAPI = root.Find((RenderPipelineSettings s) => s.supportRuntimeAOVAPI);
            supportTerrainHole = root.Find((RenderPipelineSettings s) => s.supportTerrainHole);
            supportDistortion = root.Find((RenderPipelineSettings s) => s.supportDistortion);
            supportTransparentBackface = root.Find((RenderPipelineSettings s) => s.supportTransparentBackface);
            supportTransparentDepthPrepass = root.Find((RenderPipelineSettings s) => s.supportTransparentDepthPrepass);
            supportTransparentDepthPostpass = root.Find((RenderPipelineSettings s) => s.supportTransparentDepthPostpass);
            lightProbeSystem = root.Find((RenderPipelineSettings s) => s.lightProbeSystem);
            probeVolumeTextureSize = root.Find((RenderPipelineSettings s) => s.probeVolumeMemoryBudget);
            supportProbeVolumeScenarios = root.Find((RenderPipelineSettings s) => s.supportProbeVolumeScenarios);
            supportProbeVolumeScenarioBlending = root.Find((RenderPipelineSettings s) => s.supportProbeVolumeScenarioBlending);
            probeVolumeBlendingTextureSize = root.Find((RenderPipelineSettings s) => s.probeVolumeBlendingMemoryBudget);
            supportProbeVolumeGPUStreaming = root.Find((RenderPipelineSettings s) => s.supportProbeVolumeGPUStreaming);
            supportProbeVolumeDiskStreaming = root.Find((RenderPipelineSettings s) => s.supportProbeVolumeDiskStreaming);
            probeVolumeSHBands = root.Find((RenderPipelineSettings s) => s.probeVolumeSHBands);
            supportRayTracing = root.Find((RenderPipelineSettings s) => s.supportRayTracing);
            supportVFXRayTracing = root.Find((RenderPipelineSettings s) => s.supportVFXRayTracing);
            supportedRayTracingMode = root.Find((RenderPipelineSettings s) => s.supportedRayTracingMode);
            supportHighQualityLineRendering = root.Find((RenderPipelineSettings s) => s.supportHighQualityLineRendering);
            highQualityLineRenderingMemoryBudget = root.Find((RenderPipelineSettings s) => s.highQualityLineRenderingMemoryBudget);

            lightLoopSettings = new SerializedGlobalLightLoopSettings(root.Find((RenderPipelineSettings s) => s.lightLoopSettings));
            hdShadowInitParams = new SerializedHDShadowInitParameters(root.Find((RenderPipelineSettings s) => s.hdShadowInitParams));
            decalSettings = new SerializedGlobalDecalSettings(root.Find((RenderPipelineSettings s) => s.decalSettings));
            postProcessSettings = new SerializedGlobalPostProcessSettings(root.Find((RenderPipelineSettings s) => s.postProcessSettings));
            dynamicResolutionSettings = new SerializedDynamicResolutionSettings(root.Find((RenderPipelineSettings s) => s.dynamicResolutionSettings));
            lowresTransparentSettings = new SerializedLowResTransparencySettings(root.Find((RenderPipelineSettings s) => s.lowresTransparentSettings));
            xrSettings = new SerializedXRSettings(root.Find((RenderPipelineSettings s) => s.xrSettings));
            postProcessQualitySettings = new SerializedPostProcessingQualitySettings(root.Find((RenderPipelineSettings s) => s.postProcessQualitySettings));
            
            supportScreenSpaceLensFlare = root.Find((RenderPipelineSettings s) => s.supportScreenSpaceLensFlare);
            supportDataDrivenLensFlare = root.Find((RenderPipelineSettings s) => s.supportDataDrivenLensFlare);

            lightSettings = new SerializedLightSettings(root.Find((RenderPipelineSettings s) => s.lightSettings));
            lodBias = new SerializedScalableSetting(root.Find((RenderPipelineSettings s) => s.lodBias));
            maximumLODLevel = new SerializedScalableSetting(root.Find((RenderPipelineSettings s) => s.maximumLODLevel));
            lightingQualitySettings = new SerializedLightingQualitySettings(root.Find((RenderPipelineSettings s) => s.lightingQualitySettings));
            gpuResidentDrawerSettings = new SerializedGPUResidentDrawerSettings(root.Find((RenderPipelineSettings s) => s.gpuResidentDrawerSettings));

#pragma warning disable 618 // Type or member is obsolete
            m_ObsoleteincreaseSssSampleCount = root.Find((RenderPipelineSettings s) => s.m_ObsoleteincreaseSssSampleCount);
            m_ObsoleteSupportDitheringCrossFade = root.Find((RenderPipelineSettings s) => s.supportDitheringCrossFade);
#pragma warning restore 618
        }
    }
}
