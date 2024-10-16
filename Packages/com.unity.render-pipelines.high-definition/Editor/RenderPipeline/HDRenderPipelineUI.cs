using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditorInternal;
using static UnityEngine.Rendering.HighDefinition.RenderPipelineSettings;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDRenderPipelineAsset>;

    static partial class HDRenderPipelineUI
    {
        #region Expandable States

        internal enum ExpandableGroup
        {
            Rendering = 1 << 4,
            Lighting = 1 << 5,
            LightingTiers = 1 << 6,
            Material = 1 << 7,
            PostProcess = 1 << 8,
            PostProcessTiers = 1 << 9,
            XR = 1 << 10,
            VirtualTexturing = 1 << 11,
            Volumes = 1 << 12
        }

        internal enum ExpandableRendering
        {
            Decal = 1 << 0,
            DynamicResolution = 1 << 1,
            LowResTransparency = 1 << 2,
            Water = 1 << 3,
            // Illegal index 1 << 4 since parent Lighting section index is using it
            HighQualityLineRendering = 1 << 5,
            ComputeThickness = 1 << 6
        }

        internal enum ExpandableDecal
        {
            TextureResolution = 1 << 0
        }

        internal enum ExpandableLighting
        {
            Volumetric = 1 << 0,
            ProbeVolume = 1 << 1,
            Cookie = 1 << 2,
            Reflection = 1 << 3,
            Sky = 1 << 4,
            // Illegal index 1 << 5 since parent Lighting section index is using it
            LightLoop = 1 << 6,
            Shadow = 1 << 7
        }

        internal enum ExpandableLightingQuality
        {
            SSAOQuality = 1 << 0,
            RTAOQuality = 1 << 1,
            ContactShadowQuality = 1 << 2,
            SSRQuality = 1 << 3,
            RTRQuality = 1 << 4,
            FogQuality = 1 << 5,
            RTGIQuality = 1 << 6,
            SSGIQuality = 1 << 7
        }

        internal enum ExpandablePostProcess
        {
            LensFlare = 1 << 0
        }

        internal enum ExpandablePostProcessQuality
        {
            DepthOfFieldQuality = 1 << 0,
            MotionBlurQuality = 1 << 1,
            BloomQuality = 1 << 2,
            ChromaticAberrationQuality = 1 << 3
        }

        enum ExpandableShadows
        {
            PunctualLightShadows = 1 << 1,
            DirectionalLightShadows = 1 << 2,
            AreaLightShadows = 1 << 3,
        }

        enum ExpandableQualities
        {
            Low = 1 << 1,
            Medium = 1 << 2,
            High = 1 << 3,
        }

        static readonly ExpandedState<ExpandableGroup, HDRenderPipelineAsset> k_ExpandedGroupState = new(0, "HDRP");
        static readonly ExpandedState<ExpandableRendering, HDRenderPipelineAsset> k_ExpandableRenderingState = new(0, "HDRP");
        static readonly ExpandedState<ExpandableDecal, HDRenderPipelineAsset> k_ExpandableDecalState = new (0, "HDRP");
        static readonly ExpandedState<ExpandableLighting, HDRenderPipelineAsset> k_ExpandableLightingState = new(0, "HDRP");
        static readonly ExpandedState<ExpandableLightingQuality, HDRenderPipelineAsset> k_ExpandableLightingQualityState = new(0, "HDRP");
        static readonly ExpandedState<ExpandablePostProcess, HDRenderPipelineAsset> k_ExpandablePostProcessState = new(0, "HDRP");
        static readonly ExpandedState<ExpandablePostProcessQuality, HDRenderPipelineAsset> k_ExpandablePostProcessQualityState = new(0, "HDRP");

        static readonly ExpandedState<ExpandableShadows, HDRenderPipelineAsset> k_LightsExpandedState = new(0, "HDRP");

        static internal void ExpandGroup(ExpandableGroup group)
        {
            k_ExpandedGroupState.SetExpandedAreas(group, true);
        }

        static readonly Dictionary<GUIContent, ExpandedState<ExpandableQualities, HDRenderPipelineAsset>>
        k_QualityExpandedStates = new();
        private static CED.IDrawer QualityDrawer<TEnum>(GUIContent content, TEnum mask, ExpandedStateBase<TEnum> state, Action<SerializedHDRenderPipelineAsset, int> qualityActionForTier)
            where TEnum : struct, IConvertible
        {
            // Make sure that the section is not registered
            if (k_QualityExpandedStates.TryGetValue(content, out var key))
                throw new Exception($"Quality Section {content.text} already registered");

            // Register the section
            key = new ExpandedState<ExpandableQualities, HDRenderPipelineAsset>(0, $"HDRP:{content}");
            k_QualityExpandedStates[content] = key;

            const FoldoutOption options = FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd;
            return CED.FoldoutGroup(content, mask, state, options,
                CED.FoldoutGroup(Styles.lowQualityContent, ExpandableQualities.Low, key, options, (s, _) => qualityActionForTier(s, (int)ScalableSettingLevelParameter.Level.Low)),
                CED.FoldoutGroup(Styles.mediumQualityContent, ExpandableQualities.Medium, key, options, (s, _) => qualityActionForTier(s, (int)ScalableSettingLevelParameter.Level.Medium)),
                CED.FoldoutGroup(Styles.highQualityContent, ExpandableQualities.High, key, options, (s, _) => qualityActionForTier(s, (int)ScalableSettingLevelParameter.Level.High)));
        }

        #endregion

        enum ShadowResolutionValue
        {
            [InspectorName("128")]
            ShadowResolution128 = 128,
            [InspectorName("256")]
            ShadowResolution256 = 256,
            [InspectorName("512")]
            ShadowResolution512 = 512,
            [InspectorName("1024")]
            ShadowResolution1024 = 1024,
            [InspectorName("2048")]
            ShadowResolution2048 = 2048,
            [InspectorName("4096")]
            ShadowResolution4096 = 4096,
            [InspectorName("8192")]
            ShadowResolution8192 = 8192,
            [InspectorName("16384")]
            ShadowResolution16384 = 16384
        }

        internal static VirtualTexturingSettingsUI virtualTexturingSettingsUI = new VirtualTexturingSettingsUI();

        static HDRenderPipelineUI()
        {
            Inspector = CED.Group(
                SubInspectors[ExpandableGroup.Rendering] = CED.FoldoutGroup(Styles.renderingSectionTitle, ExpandableGroup.Rendering, k_ExpandedGroupState,
                    CED.Group(GroupOption.Indent, Drawer_SectionRenderingUnsorted),
                    CED.FoldoutGroup(Styles.decalsSubTitle, ExpandableRendering.Decal, k_ExpandableRenderingState, FoldoutOption.Indent | FoldoutOption.SubFoldout,
                        CED.Group(Drawer_SectionDecalSettings),
                        CED.FoldoutGroup(Styles.decalResolutionSubTitle, ExpandableDecal.TextureResolution, k_ExpandableDecalState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionDecalTextureResolution)
                        ),
                    CED.FoldoutGroup(Styles.dynamicResolutionSubTitle, ExpandableRendering.DynamicResolution, k_ExpandableRenderingState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionDynamicResolutionSettings),
                    CED.FoldoutGroup(Styles.lowResTransparencySubTitle, ExpandableRendering.LowResTransparency, k_ExpandableRenderingState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionLowResTransparentSettings),
                    CED.FoldoutGroup(Styles.waterSubTitle, ExpandableRendering.Water, k_ExpandableRenderingState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionWaterSettings),
                    CED.FoldoutGroup(Styles.computeThicknessSubTitle, ExpandableRendering.ComputeThickness, k_ExpandableRenderingState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionComputeThicknessSettings),
                    CED.FoldoutGroup(Styles.highQualityLineRenderingSubTitle, ExpandableRendering.HighQualityLineRendering, k_ExpandableRenderingState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionHighQualityLineRenderingSettings)
                    ),
                SubInspectors[ExpandableGroup.Lighting] = CED.FoldoutGroup(Styles.lightingSectionTitle, ExpandableGroup.Lighting, k_ExpandedGroupState,
                    CED.Group(GroupOption.Indent, Drawer_SectionLightingUnsorted),
                    CED.FoldoutGroup(Styles.volumetricSubTitle, ExpandableLighting.Volumetric, k_ExpandableLightingState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_Volumetric),
                    CED.FoldoutGroup(Styles.lightProbeSubTitle, ExpandableLighting.ProbeVolume, k_ExpandableLightingState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionProbeVolume),
                    CED.FoldoutGroup(Styles.cookiesSubTitle, ExpandableLighting.Cookie, k_ExpandableLightingState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionCookies),
                    CED.FoldoutGroup(Styles.reflectionsSubTitle, ExpandableLighting.Reflection, k_ExpandableLightingState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionReflection),
                    CED.FoldoutGroup(Styles.skySubTitle, ExpandableLighting.Sky, k_ExpandableLightingState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionSky),
                    CED.FoldoutGroup(Styles.shadowSubTitle, ExpandableLighting.Shadow, k_ExpandableLightingState, FoldoutOption.Indent | FoldoutOption.SubFoldout,
                        CED.Group(Drawer_SectionShadows),
                        CED.FoldoutGroup(Styles.punctualLightshadowSubTitle, ExpandableShadows.PunctualLightShadows, k_LightsExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_PunctualLightSectionShadows),
                        CED.FoldoutGroup(Styles.directionalLightshadowSubTitle, ExpandableShadows.DirectionalLightShadows, k_LightsExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_DirectionalLightSectionShadows),
                        CED.FoldoutGroup(Styles.areaLightshadowSubTitle, ExpandableShadows.AreaLightShadows, k_LightsExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_AreaLightSectionShadows)
                        ),
                    CED.FoldoutGroup(Styles.lightLoopSubTitle, ExpandableLighting.LightLoop, k_ExpandableLightingState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionLightLoop),
                    CED.FoldoutGroup(Styles.tierSubTitle, ExpandableGroup.LightingTiers, k_ExpandedGroupState, FoldoutOption.Indent | FoldoutOption.SubFoldout,
                        QualityDrawer(Styles.SSAOQualitySettingSubTitle, ExpandableLightingQuality.SSAOQuality, k_ExpandableLightingQualityState, DrawAOQualitySetting),
                        QualityDrawer(Styles.RTAOQualitySettingSubTitle, ExpandableLightingQuality.RTAOQuality, k_ExpandableLightingQualityState, DrawRTAOQualitySetting),
                        QualityDrawer(Styles.contactShadowsSettingsSubTitle, ExpandableLightingQuality.ContactShadowQuality, k_ExpandableLightingQualityState, DrawContactShadowQualitySetting),
                        QualityDrawer(Styles.SSRSettingsSubTitle, ExpandableLightingQuality.SSRQuality, k_ExpandableLightingQualityState, DrawSSRQualitySetting),
                        QualityDrawer(Styles.RTRSettingsSubTitle, ExpandableLightingQuality.RTRQuality, k_ExpandableLightingQualityState, DrawRTRQualitySetting),
                        QualityDrawer(Styles.FogSettingsSubTitle, ExpandableLightingQuality.FogQuality, k_ExpandableLightingQualityState, DrawVolumetricFogQualitySetting),
                        QualityDrawer(Styles.RTGISettingsSubTitle, ExpandableLightingQuality.RTGIQuality, k_ExpandableLightingQualityState, DrawRTGIQualitySetting),
                        QualityDrawer(Styles.SSGISettingsSubTitle, ExpandableLightingQuality.SSGIQuality, k_ExpandableLightingQualityState, DrawSSGIQualitySetting))
                    ),
                SubInspectors[ExpandableGroup.Material] = CED.FoldoutGroup(Styles.materialSectionTitle, ExpandableGroup.Material, k_ExpandedGroupState, Drawer_SectionMaterialUnsorted),
                SubInspectors[ExpandableGroup.PostProcess] = CED.FoldoutGroup(Styles.postProcessSectionTitle, ExpandableGroup.PostProcess, k_ExpandedGroupState,
                    CED.Group(GroupOption.Indent, Drawer_SectionPostProcessSettings),
                    CED.FoldoutGroup(Styles.LensFlareTitle, ExpandablePostProcess.LensFlare, k_ExpandablePostProcessState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_LensFlare),
                    CED.FoldoutGroup(Styles.tierSubTitle, ExpandableGroup.PostProcessTiers, k_ExpandedGroupState, FoldoutOption.Indent | FoldoutOption.SubFoldout,
                        QualityDrawer(Styles.depthOfFieldQualitySettings, ExpandablePostProcessQuality.DepthOfFieldQuality, k_ExpandablePostProcessQualityState, DrawDepthOfFieldQualitySetting),
                        QualityDrawer(Styles.motionBlurQualitySettings, ExpandablePostProcessQuality.MotionBlurQuality, k_ExpandablePostProcessQualityState, DrawMotionBlurQualitySetting),
                        QualityDrawer(Styles.bloomQualitySettings, ExpandablePostProcessQuality.BloomQuality, k_ExpandablePostProcessQualityState, DrawBloomQualitySetting),
                        QualityDrawer(Styles.chromaticAberrationQualitySettings, ExpandablePostProcessQuality.ChromaticAberrationQuality, k_ExpandablePostProcessQualityState, DrawChromaticAberrationQualitySetting)
                    )
                ),
                SubInspectors[ExpandableGroup.Volumes] = CED.FoldoutGroup(Styles.volumesSectionTitle, ExpandableGroup.Volumes, k_ExpandedGroupState, Drawer_SectionVolumes),
                SubInspectors[ExpandableGroup.XR] = CED.FoldoutGroup(Styles.xrTitle, ExpandableGroup.XR, k_ExpandedGroupState, Drawer_SectionXRSettings),
                SubInspectors[ExpandableGroup.VirtualTexturing] = CED.FoldoutGroup(Styles.virtualTexturingTitle, ExpandableGroup.VirtualTexturing, k_ExpandedGroupState, Drawer_SectionVTSettings)
            );
        }

        public static readonly CED.IDrawer Inspector;
        internal static Dictionary<ExpandableGroup, CED.IDrawer> SubInspectors = new Dictionary<ExpandableGroup, CED.IDrawer>();

        static void Drawer_Volumetric(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportVolumetrics, Styles.supportVolumetricFogContent);

            using (new EditorGUI.DisabledGroupScope(!serialized.renderPipelineSettings.supportVolumetrics.boolValue))
            {
                EditorGUI.indentLevel++;
                var lightSettings = serialized.renderPipelineSettings.lightLoopSettings;

                EditorGUILayout.PropertyField(lightSettings.maxLocalVolumetricFogOnScreen, Styles.maxLocalVolumetricFogOnScreenStyle);
                lightSettings.maxLocalVolumetricFogOnScreen.intValue = Mathf.Clamp(lightSettings.maxLocalVolumetricFogOnScreen.intValue, 1, HDRenderPipeline.k_MaxVisibleLocalVolumetricFogCount);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportVolumetricClouds, Styles.supportVolumetricCloudsContent);
        }

        static void Drawer_SectionProbeVolume(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightProbeSystem, Styles.lightProbeSystemContent);
            if (serialized.renderPipelineSettings.lightProbeSystem.intValue == (int)LightProbeSystem.AdaptiveProbeVolumes)
            {
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.probeVolumeTextureSize, Styles.probeVolumeMemoryBudget);
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.probeVolumeSHBands, Styles.probeVolumeSHBands);
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportProbeVolumeScenarios, Styles.supportProbeVolumeScenarios);
                if (serialized.renderPipelineSettings.supportProbeVolumeScenarios.boolValue)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportProbeVolumeScenarioBlending, Styles.supportProbeVolumeScenarioBlending);
                        if (serialized.renderPipelineSettings.supportProbeVolumeScenarioBlending.boolValue)
                        {
                            using (new EditorGUI.IndentLevelScope())
                                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.probeVolumeBlendingTextureSize, Styles.probeVolumeBlendingMemoryBudget);
                        }
                    }
                }

                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportProbeVolumeGPUStreaming, Styles.supportProbeVolumeGPUStreaming);
                using (new EditorGUI.DisabledScope(serialized.renderPipelineSettings.supportProbeVolumeGPUStreaming.hasMultipleDifferentValues || !serialized.renderPipelineSettings.supportProbeVolumeGPUStreaming.boolValue))
                    using (new EditorGUI.IndentLevelScope())
                        EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportProbeVolumeDiskStreaming, Styles.supportProbeVolumeDiskStreaming);

                int estimatedVMemCost = ProbeReferenceVolume.instance.GetVideoMemoryCost();
                string message = string.Format(Styles.cacheInfoFormat, HDEditorUtils.HumanizeWeight(estimatedVMemCost));
                if (estimatedVMemCost == 0)
                    message += "\nProbe reference volume is not used in the scene and resources haven't been allocated yet.";
                EditorGUILayout.HelpBox(message, MessageType.Info);
            }
        }

        static void Drawer_SectionCookies(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.cookieAtlasSize, Styles.cookieAtlasSizeContent);
            EditorGUI.BeginChangeCheck();
            if (serialized.renderPipelineSettings.lightLoopSettings.cookieAtlasSize.hasMultipleDifferentValues)
                EditorGUILayout.HelpBox(Styles.multipleDifferenteValueMessage, MessageType.Info);
            else
            {
                GraphicsFormat cookieFormat = (GraphicsFormat)serialized.renderPipelineSettings.lightLoopSettings.cookieFormat.intValue;
                long currentCache = PowerOfTwoTextureAtlas.GetApproxCacheSizeInByte(1, serialized.renderPipelineSettings.lightLoopSettings.cookieAtlasSize.intValue, true, cookieFormat);
                string message = string.Format(Styles.cacheInfoFormat, HDEditorUtils.HumanizeWeight(currentCache));
                EditorGUILayout.HelpBox(message, MessageType.Info);
            }
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.cookieAtlasLastValidMip, Styles.cookieAtlasLastValidMipContent);
            if (EditorGUI.EndChangeCheck())
                serialized.renderPipelineSettings.lightLoopSettings.cookieAtlasLastValidMip.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.cookieAtlasLastValidMip.intValue, 0, Texture2DAtlas.maxMipLevelPadding);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.cookieFormat, Styles.cookieAtlasFormatContent);
#if UNITY_2020_1_OR_NEWER
#else
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.pointCookieSize, Styles.pointCoockieSizeContent);
#endif
            EditorGUI.BeginChangeCheck();
        }

        static void Drawer_SectionReflection(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            Vector2Int cacheDim = GlobalLightLoopSettings.GetReflectionProbeTextureCacheDim((ReflectionProbeTextureCacheResolution)serialized.renderPipelineSettings.lightLoopSettings.reflectionProbeTexCacheSize.intValue);

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportSSR, Styles.supportSSRContent);
            // Both support SSR and support transparent depth prepass are required for ssr transparent to be supported.
            using (new EditorGUI.DisabledScope(!(serialized.renderPipelineSettings.supportSSR.boolValue && serialized.renderPipelineSettings.supportTransparentDepthPrepass.boolValue)))
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportSSRTransparent, Styles.supportSSRTransparentContent);
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.reflectionProbeFormat, Styles.reflectionProbeFormatContent);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.reflectionProbeTexCacheSize, Styles.reflectionProbeAtlasSizeContent);

            if (serialized.renderPipelineSettings.lightLoopSettings.reflectionProbeTexCacheSize.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox(Styles.multipleDifferenteValueMessage, MessageType.Info);
            }
            else
            {
                long currentCache = ReflectionProbeTextureCache.GetApproxCacheSizeInByte(
                    serialized.renderPipelineSettings.lightLoopSettings.supportFabricConvolution.boolValue ? 2 : 1,
                    cacheDim.x, cacheDim.y, (GraphicsFormat)serialized.renderPipelineSettings.lightLoopSettings.reflectionProbeFormat.intValue);
                string message = string.Format(Styles.cacheInfoFormat, HDEditorUtils.HumanizeWeight(currentCache));
                EditorGUILayout.HelpBox(message, MessageType.Info);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.lightLoopSettings.reflectionProbeTexLastValidCubeMip, Styles.reflectionProbeAtlasLastValidCubeMipContent);
            if (EditorGUI.EndChangeCheck())
                serialized.renderPipelineSettings.lightLoopSettings.reflectionProbeTexLastValidCubeMip.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.reflectionProbeTexLastValidCubeMip.intValue, 0, (int)EnvConstants.ConvolutionMipCount - 1);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.lightLoopSettings.reflectionProbeTexLastValidPlanarMip, Styles.reflectionProbeAtlasLastValidPlanarMipContent);
            if (EditorGUI.EndChangeCheck())
                serialized.renderPipelineSettings.lightLoopSettings.reflectionProbeTexLastValidPlanarMip.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.reflectionProbeTexLastValidPlanarMip.intValue, 0, (int)EnvConstants.ConvolutionMipCount - 1);

            serialized.renderPipelineSettings.cubeReflectionResolution.ValueGUI<CubeReflectionResolution>(Styles.cubeResolutionTitle);
            // We need to clamp the values to the resolution

            int minAtlasRes = Math.Min(cacheDim.x, cacheDim.y);
            int cubeNumLevels = serialized.renderPipelineSettings.cubeReflectionResolution.values.arraySize;
            for (int levelIdx = 0; levelIdx < cubeNumLevels; ++levelIdx)
            {
                SerializedProperty levelValue = serialized.renderPipelineSettings.cubeReflectionResolution.values.GetArrayElementAtIndex(levelIdx);
                levelValue.intValue = Mathf.Min(levelValue.intValue, minAtlasRes);
            }

            serialized.renderPipelineSettings.planarReflectionResolution.ValueGUI<PlanarReflectionAtlasResolution>(Styles.planarResolutionTitle);
            // We need to clamp the values to the resolution
            int numLevels = serialized.renderPipelineSettings.planarReflectionResolution.values.arraySize;
            for (int levelIdx = 0; levelIdx < numLevels; ++levelIdx)
            {
                SerializedProperty levelValue = serialized.renderPipelineSettings.planarReflectionResolution.values.GetArrayElementAtIndex(levelIdx);
                levelValue.intValue = Mathf.Min(levelValue.intValue, minAtlasRes);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.lightLoopSettings.maxCubeReflectionsOnScreen, Styles.maxCubeProbesContent);
            if (EditorGUI.EndChangeCheck())
                serialized.renderPipelineSettings.lightLoopSettings.maxCubeReflectionsOnScreen.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.maxCubeReflectionsOnScreen.intValue, 1, HDRenderPipeline.k_MaxCubeReflectionsOnScreen);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.lightLoopSettings.maxPlanarReflectionsOnScreen, Styles.maxPlanarProbesContent);
            if (EditorGUI.EndChangeCheck())
                serialized.renderPipelineSettings.lightLoopSettings.maxPlanarReflectionsOnScreen.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.maxPlanarReflectionsOnScreen.intValue, 1, HDRenderPipeline.k_MaxPlanarReflectionsOnScreen);

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.reflectionCacheCompressed, Styles.reflectionProbeCompressCacheContent);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.reflectionProbeDecreaseResToFit, Styles.reflectionProbeDecreaseResToFitContent);
        }

        static void Drawer_SectionSky(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.skyReflectionSize, Styles.skyReflectionSizeContent);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.skyLightingOverrideLayerMask, Styles.skyLightingOverrideMaskContent);
            if (!serialized.renderPipelineSettings.lightLoopSettings.skyLightingOverrideLayerMask.hasMultipleDifferentValues
                && serialized.renderPipelineSettings.lightLoopSettings.skyLightingOverrideLayerMask.intValue == -1)
            {
                EditorGUILayout.HelpBox(Styles.skyLightingHelpBoxContent, MessageType.Warning);
            }
        }

        static void Drawer_SectionShadows(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportShadowMask, Styles.supportShadowMaskContent);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.hdShadowInitParams.maxShadowRequests, Styles.maxRequestContent);
            if (EditorGUI.EndChangeCheck())
                serialized.renderPipelineSettings.hdShadowInitParams.maxShadowRequests.intValue = Mathf.Max(1, Mathf.Min(65536, serialized.renderPipelineSettings.hdShadowInitParams.maxShadowRequests.intValue));

            if (!serialized.renderPipelineSettings.supportedLitShaderMode.hasMultipleDifferentValues)
            {
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.hdShadowInitParams.punctualShadowFilteringQuality, Styles.punctualFilteringQuality);
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.hdShadowInitParams.directionalShadowFilteringQuality, Styles.directionalFilteringQuality);
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.hdShadowInitParams.areaShadowFilteringQuality, Styles.areaFilteringQuality);
            }
            else
            {
                using (new EditorGUI.DisabledGroupScope(true))
                    EditorGUILayout.LabelField(Styles.multipleDifferenteValueMessage);
            }

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.hdShadowInitParams.supportScreenSpaceShadows, Styles.supportScreenSpaceShadows);
            using (new EditorGUI.DisabledGroupScope(!serialized.renderPipelineSettings.hdShadowInitParams.supportScreenSpaceShadows.boolValue))
            {
                ++EditorGUI.indentLevel;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots, Styles.maxScreenSpaceShadowSlots);
                serialized.renderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots.intValue = Mathf.Max(serialized.renderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots.intValue, 4);
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.hdShadowInitParams.screenSpaceShadowBufferFormat, Styles.screenSpaceShadowFormat);
                --EditorGUI.indentLevel;
            }

            SerializedScalableSettingUI.ValueGUI<bool>(serialized.renderPipelineSettings.lightSettings.useContactShadows, Styles.useContactShadows);
        }

        static void DrawLightShadow(
            SerializedHDShadowAtlasInitParams serializedAtlasInitParams,
            SerializedScalableSetting scalableSetting,
            SerializedProperty resolutionProperty,
            int defaultCachedResolutionPropertyValue)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                scalableSetting.ValueGUI<int>(Styles.shadowResolutionTiers);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.DelayedIntField(resolutionProperty, Styles.maxShadowResolution);
                if (EditorGUI.EndChangeCheck())
                    resolutionProperty.intValue = Mathf.Max(1, resolutionProperty.intValue);

                EditorGUILayout.LabelField(Styles.shadowLightAtlasSubTitle, EditorStyles.boldLabel);

                using (new EditorGUI.IndentLevelScope())
                {
                    CoreEditorUtils.DrawEnumPopup(serializedAtlasInitParams.shadowMapResolution, typeof(ShadowResolutionValue), Styles.resolutionContent);

                    // Because we don't know if the asset is old and had the cached shadow map resolution field, if it was set as default float (0) we force a default.
                    if (serializedAtlasInitParams.cachedResolution.intValue == 0)
                        serializedAtlasInitParams.cachedResolution.intValue = defaultCachedResolutionPropertyValue;

                    CoreEditorUtils.DrawEnumPopup(serializedAtlasInitParams.cachedResolution, typeof(ShadowResolutionValue), Styles.cachedShadowAtlasResolution);

                    EditorGUILayout.IntPopup(serializedAtlasInitParams.shadowMapDepthBits, Styles.shadowBitDepthNames, Styles.shadowBitDepthValues, Styles.precisionContent);
                    EditorGUILayout.PropertyField(serializedAtlasInitParams.useDynamicViewportRescale, Styles.dynamicRescaleContent);
                }
            }
        }

        static void Drawer_PunctualLightSectionShadows(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            DrawLightShadow(
                serialized.renderPipelineSettings.hdShadowInitParams.serializedPunctualAtlasInit,
                serialized.renderPipelineSettings.hdShadowInitParams.shadowResolutionPunctual,
                serialized.renderPipelineSettings.hdShadowInitParams.maxPunctualShadowMapResolution,
                2048);
        }

        static void Drawer_AreaLightSectionShadows(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            DrawLightShadow(
                serialized.renderPipelineSettings.hdShadowInitParams.serializedAreaAtlasInit,
                serialized.renderPipelineSettings.hdShadowInitParams.shadowResolutionArea,
                serialized.renderPipelineSettings.hdShadowInitParams.maxAreaShadowMapResolution,
                1024);
        }

        static void Drawer_DirectionalLightSectionShadows(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.hdShadowInitParams.allowDirectionalMixedCachedShadows, Styles.allowMixedCachedCascadeShadows);
                EditorGUILayout.IntPopup(serialized.renderPipelineSettings.hdShadowInitParams.directionalShadowMapDepthBits, Styles.shadowBitDepthNames, Styles.shadowBitDepthValues, Styles.directionalShadowPrecisionContent);
                serialized.renderPipelineSettings.hdShadowInitParams.shadowResolutionDirectional.ValueGUI<int>(Styles.shadowResolutionTiers);
                EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.hdShadowInitParams.maxDirectionalShadowMapResolution, Styles.maxShadowResolution);
            }
        }

        static void Drawer_SectionDecalSettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportDecals, Styles.supportDecalContent);

            ++EditorGUI.indentLevel;
            using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.supportDecals.boolValue))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.decalSettings.drawDistance, Styles.drawDistanceContent);
                if (EditorGUI.EndChangeCheck())
                    serialized.renderPipelineSettings.decalSettings.drawDistance.intValue = Mathf.Max(serialized.renderPipelineSettings.decalSettings.drawDistance.intValue, 0);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.decalSettings.atlasWidth, Styles.atlasWidthContent);
                if (EditorGUI.EndChangeCheck())
                    serialized.renderPipelineSettings.decalSettings.atlasWidth.intValue = Mathf.Max(serialized.renderPipelineSettings.decalSettings.atlasWidth.intValue, 0);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.decalSettings.atlasHeight, Styles.atlasHeightContent);
                if (EditorGUI.EndChangeCheck())
                    serialized.renderPipelineSettings.decalSettings.atlasHeight.intValue = Mathf.Max(serialized.renderPipelineSettings.decalSettings.atlasHeight.intValue, 0);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.decalSettings.perChannelMask, Styles.metalAndAOContent);
                if (EditorGUI.EndChangeCheck())
                {
                    // Tell VFX
                    ((HDRenderPipelineEditor)owner).needRefreshVfxWarnings = true;
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.lightLoopSettings.maxDecalsOnScreen, Styles.maxDecalContent);
                if (EditorGUI.EndChangeCheck())
                    serialized.renderPipelineSettings.lightLoopSettings.maxDecalsOnScreen.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.maxDecalsOnScreen.intValue, 1, HDRenderPipeline.k_MaxDecalsOnScreen);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportDecalLayers, Styles.supportDecalLayersContent);
                if (EditorGUI.EndChangeCheck())
                {
                    // Tell VFX
                    ((HDRenderPipelineEditor)owner).needRefreshVfxWarnings = true;
                }
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportSurfaceGradient, Styles.supportSurfaceGradientContent);

                if (serialized.renderPipelineSettings.supportSurfaceGradient.boolValue)
                {
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.decalNormalBufferHP, Styles.decalNormalFormatContent);
                    --EditorGUI.indentLevel;
                }
            }
            --EditorGUI.indentLevel;
        }

        static void Drawer_SectionDecalTextureResolution(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                serialized.renderPipelineSettings.decalSettings.transparentTextureResolution.ValueGUI<int>(Styles.decalResolutionTiers);
            }
        }

        static void Drawer_SectionLightLoop(SerializedHDRenderPipelineAsset serialized, Editor o)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.lightLoopSettings.maxDirectionalLightsOnScreen, Styles.maxDirectionalContent);
            if (EditorGUI.EndChangeCheck())
                serialized.renderPipelineSettings.lightLoopSettings.maxDirectionalLightsOnScreen.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.maxDirectionalLightsOnScreen.intValue, 1, HDRenderPipeline.k_MaxDirectionalLightsOnScreen);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.lightLoopSettings.maxPunctualLightsOnScreen, Styles.maxPonctualContent);
            if (EditorGUI.EndChangeCheck())
                serialized.renderPipelineSettings.lightLoopSettings.maxPunctualLightsOnScreen.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.maxPunctualLightsOnScreen.intValue, 1, HDRenderPipeline.k_MaxPunctualLightsOnScreen);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.lightLoopSettings.maxAreaLightsOnScreen, Styles.maxAreaContent);
            if (EditorGUI.EndChangeCheck())
                serialized.renderPipelineSettings.lightLoopSettings.maxAreaLightsOnScreen.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.maxAreaLightsOnScreen.intValue, 1, HDRenderPipeline.k_MaxAreaLightsOnScreen);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.lightLoopSettings.maxLightsPerClusterCell, Styles.maxLightPerCellContent);
            if (EditorGUI.EndChangeCheck())
                serialized.renderPipelineSettings.lightLoopSettings.maxLightsPerClusterCell.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.maxLightsPerClusterCell.intValue, 1, HDRenderPipeline.k_MaxLightsPerClusterCell);
        }

#if ENABLE_NVIDIA && !ENABLE_NVIDIA_MODULE
        static bool s_DisplayNvidiaModuleButtonInstall = true;
#endif

#if ENABLE_AMD && !ENABLE_AMD_MODULE
        static bool s_DisplayAMDModuleButtonInstall = true;
#endif
        static void Drawer_SectionDynamicResolutionSettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.enabled, Styles.enabled);

            bool showUpsampleFilterAsFallback = false;
            int advancedUpscalersAvailable = 0;
            int advancedUpscalersDetectedMask = 0;
            int advancedUpscalersEnabledMask = 0;

#if ENABLE_AMD && ENABLE_AMD_MODULE
            advancedUpscalersDetectedMask |= HDDynamicResolutionPlatformCapabilities.FSR2Detected ? (1 << (int)AdvancedUpscalers.FSR2) : 0;
            advancedUpscalersAvailable |= (1 << (int)AdvancedUpscalers.FSR2);
#endif

#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
            advancedUpscalersDetectedMask |= HDDynamicResolutionPlatformCapabilities.DLSSDetected ? (1 << (int)AdvancedUpscalers.DLSS) : 0;
            advancedUpscalersAvailable |= (1 << (int)AdvancedUpscalers.DLSS);
#endif

            // STP is always available & detected because its implementation doesn't depend on a native module
            advancedUpscalersDetectedMask |= (1 << (int)AdvancedUpscalers.STP);
            advancedUpscalersAvailable |= (1 << (int)AdvancedUpscalers.STP);

            for (int i = 0; i < serialized.renderPipelineSettings.dynamicResolutionSettings.advancedUpscalersByPriority.arraySize; ++i)
            {
                int upscalerMaskValue = 1 << serialized.renderPipelineSettings.dynamicResolutionSettings.advancedUpscalersByPriority.GetArrayElementAtIndex(i).intValue;
                advancedUpscalersEnabledMask |= upscalerMaskValue;
            }

            ++EditorGUI.indentLevel;

            using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.dynamicResolutionSettings.enabled.boolValue))
            {
                if (advancedUpscalersDetectedMask != 0)
                {
                    ReorderableList reorderableList = null;
                    if(owner as HDRenderPipelineEditor != null)
                    {
                        HDRenderPipelineEditor editor = owner as HDRenderPipelineEditor;
                        reorderableList = editor.reusableReorderableList;

                        reorderableList ??= new ReorderableList(serialized.serializedObject, serialized.renderPipelineSettings.dynamicResolutionSettings.advancedUpscalersByPriority, true, true, true, true)
                        {
                            drawHeaderCallback = (Rect rect) =>
                            {
                                EditorGUI.LabelField(new Rect(rect.x - 45f, rect.y, rect.width - 45f, rect.height), "Advanced Upscalers by Priority", EditorStyles.boldLabel);
                            },
                            drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                            {
                                var element = serialized.renderPipelineSettings.dynamicResolutionSettings.advancedUpscalersByPriority.GetArrayElementAtIndex(index);
                                rect.y += 2;
                                EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element.enumDisplayNames[element.enumValueIndex], EditorStyles.label);
                            },
                            onAddDropdownCallback = (rect,list) => {
                                int availableScalers = math.countbits(advancedUpscalersAvailable);
                                AdvancedUpscalers[] possible = new AdvancedUpscalers[availableScalers];
                                var names = new GUIContent[availableScalers];
                                var enabled = new bool[availableScalers];
                                for (int upscalerRemainingMask = advancedUpscalersAvailable, nextItem = 0; upscalerRemainingMask != 0;)
                                {
                                    AdvancedUpscalers upscalerIndex = (AdvancedUpscalers)math.tzcnt(upscalerRemainingMask);
                                    enabled[nextItem] = (advancedUpscalersEnabledMask & (1 << (int)upscalerIndex)) == 0;
                                    possible[nextItem] = upscalerIndex;
                                    names[nextItem] = new GUIContent(upscalerIndex.ToString());
                                    upscalerRemainingMask ^= (1 << (int)upscalerIndex);//turn off the bit
                                    nextItem++;
                                }

                                EditorUtility.SelectMenuItemFunction value = (userData, options, selected) =>
                                {
                                    //Check if upscalerPriority already contains this selected upscalertype
                                    bool containsSelection = false;
                                    for(int i = 0; i < serialized.renderPipelineSettings.dynamicResolutionSettings.advancedUpscalersByPriority.arraySize; ++i)
                                    {
                                        if(serialized.renderPipelineSettings.dynamicResolutionSettings.advancedUpscalersByPriority.GetArrayElementAtIndex(i).intValue == (int)possible[selected])
                                        {
                                            containsSelection = true;
                                            break;
                                        }
                                    }

                                    //if it doesnt then add item
                                    if(!containsSelection)
                                    {
                                        int index = list.count > 0 ? list.index : 0;
                                        serialized.renderPipelineSettings.dynamicResolutionSettings.advancedUpscalersByPriority.InsertArrayElementAtIndex(index);
                                        var newElement = serialized.renderPipelineSettings.dynamicResolutionSettings.advancedUpscalersByPriority.GetArrayElementAtIndex(index);
                                        newElement.enumValueIndex = (int)possible[selected];
                                        serialized.serializedObject.ApplyModifiedProperties();
                                    }
                                };
                                EditorUtility.DisplayCustomMenu(rect, names, enabled.Length, value, possible, false);

                            }
                        };
                        editor.reusableReorderableList = reorderableList;
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.BeginVertical(new GUIStyle() { margin = new RectOffset((EditorGUI.indentLevel + 1) * 15, 0, 0, 0) });
                    reorderableList.DoLayoutList();
                    EditorGUILayout.EndVertical();

                }

                bool containsDLSS = ((1 << (int)AdvancedUpscalers.DLSS) & advancedUpscalersEnabledMask) != 0;
                bool dlssDetected = ((1 << (int)AdvancedUpscalers.DLSS) & advancedUpscalersDetectedMask) != 0;
                if (containsDLSS)
                {
                    ++EditorGUI.indentLevel;
                    var v = EditorGUILayout.EnumPopup(
                        Styles.DLSSQualitySettingContent,
                        (UnityEngine.NVIDIA.DLSSQuality)
                        serialized.renderPipelineSettings.dynamicResolutionSettings.DLSSPerfQualitySetting.intValue);

                    serialized.renderPipelineSettings.dynamicResolutionSettings.DLSSPerfQualitySetting.intValue = (int)(object)v;

                    int injectionPointVal = EditorGUILayout.IntPopup(Styles.DLSSInjectionPoint, serialized.renderPipelineSettings.dynamicResolutionSettings.DLSSInjectionPoint.intValue, Styles.UpscalerInjectionPointNames, Styles.UpscalerInjectionPointValues);
                    serialized.renderPipelineSettings.dynamicResolutionSettings.DLSSInjectionPoint.intValue = injectionPointVal;
                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.DLSSUseOptimalSettings, Styles.DLSSUseOptimalSettingsContent);

                    using (new EditorGUI.DisabledScope(serialized.renderPipelineSettings.dynamicResolutionSettings.DLSSUseOptimalSettings.boolValue))
                    {
                        EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.DLSSSharpness, Styles.DLSSSharpnessContent);
                    }
                    --EditorGUI.indentLevel;
                }

                showUpsampleFilterAsFallback = showUpsampleFilterAsFallback || containsDLSS;
                if (containsDLSS)
                {
                    EditorGUILayout.HelpBox(
                        dlssDetected ? Styles.DLSSFeatureDetectedMsg : Styles.DLSSFeatureNotDetectedMsg,
                        dlssDetected ? MessageType.Info : MessageType.Warning);
                }

                if (dlssDetected && EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64 && containsDLSS)
                {
                    --EditorGUI.indentLevel;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox(Styles.DLSSWinTargetWarning, MessageType.Info);
                    if (GUILayout.Button(Styles.DLSSSwitchTarget64Button, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                    {
                        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
                    }
                    EditorGUILayout.EndHorizontal();
                    ++EditorGUI.indentLevel;
                }

                bool containsFSR2 = ((1 << (int)AdvancedUpscalers.FSR2) & advancedUpscalersEnabledMask) != 0;
                bool fsr2Detected = ((1 << (int)AdvancedUpscalers.FSR2) & advancedUpscalersDetectedMask) != 0;
                if (containsFSR2)
                {
                    ++EditorGUI.indentLevel;
                    int injectionPointVal = EditorGUILayout.IntPopup(Styles.FSR2InjectionPoint, serialized.renderPipelineSettings.dynamicResolutionSettings.FSR2InjectionPoint.intValue, Styles.UpscalerInjectionPointNames, Styles.UpscalerInjectionPointValues);
                    serialized.renderPipelineSettings.dynamicResolutionSettings.FSR2InjectionPoint.intValue = injectionPointVal;
                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.FSR2EnableSharpness, Styles.FSR2EnableSharpness);
                    using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.dynamicResolutionSettings.FSR2EnableSharpness.boolValue))
                    {
                        EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.FSR2Sharpness, Styles.FSR2Sharpness);
                    }

                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.FSR2UseOptimalSettings, Styles.FSR2UseOptimalSettingsContent);
                    if (serialized.renderPipelineSettings.dynamicResolutionSettings.FSR2UseOptimalSettings.boolValue)
                    {
                        var v = EditorGUILayout.EnumPopup(
                            Styles.FSR2QualitySettingContent,
                            (UnityEngine.AMD.FSR2Quality)
                            serialized.renderPipelineSettings.dynamicResolutionSettings.FSR2QualitySetting.intValue);
                        serialized.renderPipelineSettings.dynamicResolutionSettings.FSR2QualitySetting.intValue = (int)(object)v;
                    }
                    --EditorGUI.indentLevel;
                }

                showUpsampleFilterAsFallback = showUpsampleFilterAsFallback || containsFSR2;
                if (containsFSR2)
                {
                    EditorGUILayout.HelpBox(
                        fsr2Detected ? Styles.FSR2FeatureDetectedMsg : Styles.FSR2FeatureNotDetectedMsg,
                        fsr2Detected ? MessageType.Info : MessageType.Warning);
                }
                if (fsr2Detected && EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64 && containsFSR2)
                {
                    --EditorGUI.indentLevel;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox(Styles.FSR2WinTargetWarning, MessageType.Info);
                    if (GUILayout.Button(Styles.FSR2SwitchTarget64Button, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                    {
                        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
                    }
                    EditorGUILayout.EndHorizontal();
                    ++EditorGUI.indentLevel;
                }

                bool containsSTP = ((1 << (int)AdvancedUpscalers.STP) & advancedUpscalersEnabledMask) != 0;
                if (containsSTP)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        // Draw STP settings
                        int value = EditorGUILayout.IntPopup(Styles.STPInjectionPoint, serialized.renderPipelineSettings.dynamicResolutionSettings.STPInjectionPoint.intValue, Styles.UpscalerInjectionPointNames, Styles.UpscalerInjectionPointValues);
                        serialized.renderPipelineSettings.dynamicResolutionSettings.STPInjectionPoint.intValue = value;
                    }
                }

                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.dynamicResType, Styles.dynResType);
                bool isHwDrs = (serialized.renderPipelineSettings.dynamicResolutionSettings.dynamicResType.intValue == (int)DynamicResolutionType.Hardware);
                bool gfxDeviceSupportsHwDrs = HDUtils.IsHardwareDynamicResolutionSupportedByDevice(SystemInfo.graphicsDeviceType);

                if (isHwDrs && !gfxDeviceSupportsHwDrs)
                {
                    EditorGUILayout.HelpBox($"{Styles.dynResTypeWarning}", MessageType.Warning, wide: true);
                }

                if (serialized.renderPipelineSettings.dynamicResolutionSettings.dynamicResType.hasMultipleDifferentValues)
                {
                    using (new EditorGUI.DisabledGroupScope(true))
                        EditorGUILayout.LabelField(Styles.multipleDifferenteValueMessage);
                }
                else
                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.softwareUpsamplingFilter, showUpsampleFilterAsFallback ? Styles.fallbackUpsampleFilter : Styles.upsampleFilter);

                // When the FSR upscaling filter is selected, allow the user to configure its sharpness.
                DynamicResUpscaleFilter currentUpscaleFilter = (DynamicResUpscaleFilter)serialized.renderPipelineSettings.dynamicResolutionSettings.softwareUpsamplingFilter.intValue;
                bool isFsrEnabled = (currentUpscaleFilter == DynamicResUpscaleFilter.EdgeAdaptiveScalingUpres);
                if (isFsrEnabled)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.fsrOverrideSharpness, Styles.fsrOverrideSharpness);

                        // We put the FSR sharpness override value behind a top-level override checkbox so we can tell when the user intends to use a custom value rather than the default.
                        if (serialized.renderPipelineSettings.dynamicResolutionSettings.fsrOverrideSharpness.boolValue)
                        {
                            using (new EditorGUI.IndentLevelScope())
                            {
                                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.fsrSharpness, Styles.fsrSharpnessText);
                            }
                        }
                    }
                }

                using (new EditorGUI.IndentLevelScope())
                {
                    if (currentUpscaleFilter == DynamicResUpscaleFilter.TAAU)
                    {
                        int ip = EditorGUILayout.IntPopup(Styles.TAAUInjectionPoint, serialized.renderPipelineSettings.dynamicResolutionSettings.TAAUInjectionPoint.intValue, Styles.UpscalerInjectionPointNames, Styles.UpscalerInjectionPointValues);
                        serialized.renderPipelineSettings.dynamicResolutionSettings.TAAUInjectionPoint.intValue = ip;
                    }
                    // Catmull-Rom is combined to the final pass, so we can't change it's injection point
                    // FSR 1.0 (EdgeAdaptiveScalingUpres) only works with perceptual data, so we can't change it's injection point.
                    else if (currentUpscaleFilter != DynamicResUpscaleFilter.CatmullRom && currentUpscaleFilter != DynamicResUpscaleFilter.EdgeAdaptiveScalingUpres)
                    {
                        int ip = EditorGUILayout.IntPopup(Styles.defaultInjectionPoint, serialized.renderPipelineSettings.dynamicResolutionSettings.defaultInjectionPoint.intValue, Styles.UpscalerInjectionPointNames, Styles.UpscalerInjectionPointValues);
                        serialized.renderPipelineSettings.dynamicResolutionSettings.defaultInjectionPoint.intValue = ip;
                    }
                }

                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.useMipBias, Styles.useMipBias);

                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.forcePercentage, Styles.forceScreenPercentage);
                if (serialized.renderPipelineSettings.dynamicResolutionSettings.forcePercentage.hasMultipleDifferentValues)
                {
                    using (new EditorGUI.DisabledGroupScope(true))
                        EditorGUILayout.LabelField(Styles.multipleDifferenteValueMessage);
                }

                using (new EditorGUI.IndentLevelScope())
                {
                    if (!serialized.renderPipelineSettings.dynamicResolutionSettings.forcePercentage.hasMultipleDifferentValues
                        && serialized.renderPipelineSettings.dynamicResolutionSettings.forcePercentage.boolValue)
                    {
                        EditorGUI.showMixedValue = serialized.renderPipelineSettings.dynamicResolutionSettings.forcedPercentage.hasMultipleDifferentValues;
                        float forcePercentage = serialized.renderPipelineSettings.dynamicResolutionSettings.forcedPercentage.floatValue;
                        EditorGUI.BeginChangeCheck();
                        forcePercentage = EditorGUILayout.DelayedFloatField(Styles.forcedScreenPercentage, forcePercentage);
                        if (EditorGUI.EndChangeCheck())
                            serialized.renderPipelineSettings.dynamicResolutionSettings.forcedPercentage.floatValue = Mathf.Clamp(forcePercentage, 0.0f, 100.0f);
                        EditorGUI.showMixedValue = false;
                    }

                    if (!serialized.renderPipelineSettings.dynamicResolutionSettings.forcePercentage.hasMultipleDifferentValues
                        && !serialized.renderPipelineSettings.dynamicResolutionSettings.forcePercentage.boolValue)
                    {
#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
                        if (dlssDetected && containsDLSS && serialized.renderPipelineSettings.dynamicResolutionSettings.DLSSUseOptimalSettings.boolValue)
                        {
                            EditorGUILayout.HelpBox(Styles.DLSSIgnorePercentages, MessageType.Info);
                        }
#endif

                        // Show a warning if STP is selected with software DRS and a dynamic scaling range
                        if (containsSTP)
                        {
                            if (!isHwDrs || !gfxDeviceSupportsHwDrs)
                            {
                                EditorGUILayout.HelpBox($"{Styles.STPSwDrsWarningMsg}", MessageType.Warning, wide: true);
                            }
                        }

                        float minPercentage = serialized.renderPipelineSettings.dynamicResolutionSettings.minPercentage.floatValue;
                        float maxPercentage = serialized.renderPipelineSettings.dynamicResolutionSettings.maxPercentage.floatValue;

                        EditorGUI.showMixedValue = serialized.renderPipelineSettings.dynamicResolutionSettings.minPercentage.hasMultipleDifferentValues;
                        EditorGUI.BeginChangeCheck();
                        minPercentage = EditorGUILayout.DelayedFloatField(HDRenderPipelineUI.Styles.minPercentage, minPercentage);
                        if (EditorGUI.EndChangeCheck())
                            serialized.renderPipelineSettings.dynamicResolutionSettings.minPercentage.floatValue = Mathf.Clamp(minPercentage, 0.0f, maxPercentage);

                        EditorGUI.showMixedValue = serialized.renderPipelineSettings.dynamicResolutionSettings.maxPercentage.hasMultipleDifferentValues;
                        EditorGUI.BeginChangeCheck();
                        maxPercentage = EditorGUILayout.DelayedFloatField(HDRenderPipelineUI.Styles.maxPercentage, maxPercentage);
                        if (EditorGUI.EndChangeCheck())
                            serialized.renderPipelineSettings.dynamicResolutionSettings.maxPercentage.floatValue = Mathf.Clamp(maxPercentage, minPercentage, 100.0f);

                        EditorGUI.showMixedValue = false;
                    }
                }

                {
                    EditorGUI.showMixedValue = serialized.renderPipelineSettings.dynamicResolutionSettings.lowResTransparencyMinimumThreshold.hasMultipleDifferentValues;
                    float lowResTransparencyMinimumThreshold = serialized.renderPipelineSettings.dynamicResolutionSettings.lowResTransparencyMinimumThreshold.floatValue;
                    EditorGUI.BeginChangeCheck();
                    lowResTransparencyMinimumThreshold = EditorGUILayout.DelayedFloatField(Styles.lowResTransparencyMinimumThreshold, lowResTransparencyMinimumThreshold);
                    if (EditorGUI.EndChangeCheck())
                        serialized.renderPipelineSettings.dynamicResolutionSettings.lowResTransparencyMinimumThreshold.floatValue = Mathf.Clamp(lowResTransparencyMinimumThreshold, 0.0f, 50.0f);
                    if (serialized.renderPipelineSettings.dynamicResolutionSettings.lowResTransparencyMinimumThreshold.floatValue > 0.0f && !serialized.renderPipelineSettings.lowresTransparentSettings.enabled.boolValue)
                        EditorGUILayout.HelpBox(Styles.lowResTransparencyThresholdDisabledMsg, MessageType.Info);
                }

                {
                    EditorGUI.showMixedValue = serialized.renderPipelineSettings.dynamicResolutionSettings.lowResSSGIMinimumThreshold.hasMultipleDifferentValues;
                    float lowResSSGIMinimumThreshold = serialized.renderPipelineSettings.dynamicResolutionSettings.lowResSSGIMinimumThreshold.floatValue;
                    EditorGUI.BeginChangeCheck();
                    lowResSSGIMinimumThreshold = EditorGUILayout.DelayedFloatField(Styles.lowResSSGIMinimumThreshold, lowResSSGIMinimumThreshold);
                    if (EditorGUI.EndChangeCheck())
                        serialized.renderPipelineSettings.dynamicResolutionSettings.lowResSSGIMinimumThreshold.floatValue = Mathf.Clamp(lowResSSGIMinimumThreshold, 0.0f, 50.0f);
                }

                {
                    EditorGUI.showMixedValue = serialized.renderPipelineSettings.dynamicResolutionSettings.lowResVolumetricCloudsMinimumThreshold.hasMultipleDifferentValues;
                    float lowResVolumetricCloudsMinimumThreshold = serialized.renderPipelineSettings.dynamicResolutionSettings.lowResVolumetricCloudsMinimumThreshold.floatValue;
                    EditorGUI.BeginChangeCheck();
                    lowResVolumetricCloudsMinimumThreshold = EditorGUILayout.DelayedFloatField(Styles.lowResVolumetricCloudsMinimumThreshold, lowResVolumetricCloudsMinimumThreshold);
                    if (EditorGUI.EndChangeCheck())
                        serialized.renderPipelineSettings.dynamicResolutionSettings.lowResVolumetricCloudsMinimumThreshold.floatValue = Mathf.Clamp(lowResVolumetricCloudsMinimumThreshold, 0.001f, 100.0f);
                }

                {
                    float rayTracingHalfResThreshold = serialized.renderPipelineSettings.dynamicResolutionSettings.rayTracingHalfResThreshold.floatValue;
                    EditorGUI.BeginChangeCheck();
                    rayTracingHalfResThreshold = EditorGUILayout.DelayedFloatField(Styles.rayTracingHalfResThreshold, rayTracingHalfResThreshold);
                    if (EditorGUI.EndChangeCheck())
                        serialized.renderPipelineSettings.dynamicResolutionSettings.rayTracingHalfResThreshold.floatValue = Mathf.Clamp(rayTracingHalfResThreshold, 0.0f, 100.0f);
                }
            }
            --EditorGUI.indentLevel;

#if ENABLE_NVIDIA && !ENABLE_NVIDIA_MODULE
            if (s_DisplayNvidiaModuleButtonInstall)
            {
                CoreEditorUtils.DrawFixMeBox(Styles.DLSSPackageLabel, MessageType.Info, () => {
                    PackageManager.Client.Add("com.unity.modules.nvidia");
                    s_DisplayNvidiaModuleButtonInstall = false;
                });
            }
#endif

#if ENABLE_AMD && !ENABLE_AMD_MODULE
            if (s_DisplayAMDModuleButtonInstall)
            {
                CoreEditorUtils.DrawFixMeBox(Styles.FSR2PackageLabel, MessageType.Info, () => {
                    PackageManager.Client.Add("com.unity.modules.amd");
                    s_DisplayAMDModuleButtonInstall = false;
                });
            }
#endif
        }

        static void Drawer_SectionLowResTransparentSettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lowresTransparentSettings.enabled, Styles.lowResTransparentEnabled);

            /* For the time being we don't enable the option control and default to nearest depth. This might change in a close future.
            ++EditorGUI.indentLevel;
            using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.lowresTransparentSettings.enabled.boolValue))
            {
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lowresTransparentSettings.checkerboardDepthBuffer, k_CheckerboardDepthBuffer);
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lowresTransparentSettings.upsampleType, k_UpsampleFilter);
            }
            --EditorGUI.indentLevel;
            */
        }

        static void Drawer_SectionWaterSettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportWater, Styles.supportWaterContent);
            if (EditorGUI.EndChangeCheck())
                HDSampleBufferNode.UpdateWarningBadges(HDSampleBufferNode.BufferType.IsUnderWater, serialized.renderPipelineSettings.supportWater.boolValue);

            ++EditorGUI.indentLevel;
            using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.supportWater.boolValue))
            {
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.waterSimulationResolution, Styles.waterSimulationResolutionContent);

                // Decals
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportWaterDecals);
                using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.supportWaterDecals.boolValue))
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.waterDecalAtlasSize, Styles.waterDecalAtlasSizeContent);

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.maximumWaterDecalCount, Styles.maximumWaterDecalCountContent);
                    if (EditorGUI.EndChangeCheck())
                        serialized.renderPipelineSettings.maximumWaterDecalCount.intValue = Mathf.Clamp(serialized.renderPipelineSettings.maximumWaterDecalCount.intValue, 1, 256);
                }

                // Exclusion
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportWaterExclusion, Styles.supportWaterExclusionContent);

                // CPU Simulation
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.waterScriptInteractionsMode);

                if (serialized.renderPipelineSettings.waterScriptInteractionsMode.intValue == (int)WaterScriptInteractionsMode.CPUSimulation)
                {
                    EditorGUI.indentLevel++;
                    if (serialized.renderPipelineSettings.waterSimulationResolution.intValue != (int)WaterSimulationResolution.Low64)
                        EditorGUILayout.PropertyField(serialized.renderPipelineSettings.waterFullCPUSimulation);
                    EditorGUI.indentLevel--;
                }

            }
            --EditorGUI.indentLevel;
        }

        static void Drawer_SectionHighQualityLineRenderingSettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportHighQualityLineRendering, Styles.supportHighQualityLineRenderingContent);

            ++EditorGUI.indentLevel;
            using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.supportHighQualityLineRendering.boolValue))
            {
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.highQualityLineRenderingMemoryBudget, Styles.highQualityLineRenderingMemoryBudget);
            }
            --EditorGUI.indentLevel;
        }

        static void Drawer_SectionComputeThicknessSettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportComputeThickness, Styles.computeThicknessEnableContent);
            if (EditorGUI.EndChangeCheck())
                HDSampleBufferNode.UpdateWarningBadges(HDSampleBufferNode.BufferType.Thickness, serialized.renderPipelineSettings.supportComputeThickness.boolValue);

            using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.supportComputeThickness.boolValue))
            {
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.computeThicknessResolution, Styles.computeThicknessResolutionContent);
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.computeThicknessLayerMask, Styles.computeThicknessLayerContent);
            }
        }

        static void Drawer_SectionPostProcessSettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.postProcessSettings.lutSize, Styles.lutSize);
            if (EditorGUI.EndChangeCheck())
                serialized.renderPipelineSettings.postProcessSettings.lutSize.intValue = Mathf.Clamp(serialized.renderPipelineSettings.postProcessSettings.lutSize.intValue, GlobalPostProcessSettings.k_MinLutSize, GlobalPostProcessSettings.k_MaxLutSize);

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessSettings.lutFormat, Styles.lutFormat);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessSettings.bufferFormat, Styles.bufferFormat);
        }

        static Editor s_VolumeProfileEditor;

        static void Drawer_SectionVolumes(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serialized.volumeProfile, Styles.volumeProfileLabel);
            var profile = serialized.volumeProfile.objectReferenceValue as VolumeProfile;
            if (EditorGUI.EndChangeCheck() && HDRenderPipeline.currentAsset == serialized.serializedObject.targetObject && RenderPipelineManager.currentPipeline is HDRenderPipeline)
                VolumeManager.instance.SetQualityDefaultProfile(profile);

            Editor.CreateCachedEditor(profile, typeof(VolumeProfileEditor), ref s_VolumeProfileEditor);

            var contextMenuButtonRect = GUILayoutUtility.GetRect(CoreEditorStyles.contextMenuIcon,
                Styles.volumeProfileContextMenuStyle.Value);
            if (GUI.Button(contextMenuButtonRect, CoreEditorStyles.contextMenuIcon,
                    Styles.volumeProfileContextMenuStyle.Value))
            {
                var profileEditor = s_VolumeProfileEditor as VolumeProfileEditor;
                var componentEditors = profileEditor != null ? profileEditor.componentList.editors : null;
                var srpAsset = serialized.serializedObject.targetObject as HDRenderPipelineAsset;
                var pos = new Vector2(contextMenuButtonRect.x, contextMenuButtonRect.yMax);
                VolumeProfileUtils.OnVolumeProfileContextClick(pos, srpAsset.volumeProfile, componentEditors,
                    overrideStateOnReset: false,
                    defaultVolumeProfilePath: $"Assets/{HDProjectSettings.projectSettingsFolderPath}/{srpAsset.name}_VolumeProfile.asset",
                    onNewVolumeProfileCreated: volumeProfile =>
                    {
                        Undo.RecordObject(srpAsset, "Set HDRenderPipelineAsset Volume Profile");
                        srpAsset.volumeProfile = volumeProfile;
                        if (HDRenderPipeline.currentAsset == srpAsset)
                            VolumeManager.instance.SetQualityDefaultProfile(volumeProfile);
                        EditorUtility.SetDirty(srpAsset);
                    });
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);

            if (profile != null)
            {
                bool oldEnabled = GUI.enabled;
                GUI.enabled = AssetDatabase.IsOpenForEdit(profile);
                s_VolumeProfileEditor.OnInspectorGUI();
                GUI.enabled = oldEnabled;
            }
            else
            {
                CoreUtils.Destroy(s_VolumeProfileEditor);
            }
        }

        static void Drawer_SectionXRSettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.xrSettings.singlePass, Styles.XRSinglePass);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.xrSettings.occlusionMesh, Styles.XROcclusionMesh);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.xrSettings.cameraJitter, Styles.XRCameraJitter);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.xrSettings.allowMotionBlur, Styles.XRMotionBlur);
        }

        static void Drawer_SectionVTSettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            virtualTexturingSettingsUI.OnGUI(serialized, owner);
        }

        static void DrawDepthOfFieldQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            {
                EditorGUILayout.LabelField(Styles.nearBlurSubTitle, EditorStyles.miniLabel);
                ++EditorGUI.indentLevel;
                {
                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.NearBlurSampleCount.GetArrayElementAtIndex(tier), Styles.sampleCountQuality);
                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.NearBlurMaxRadius.GetArrayElementAtIndex(tier), Styles.maxRadiusQuality);
                }
                --EditorGUI.indentLevel;
                EditorGUILayout.LabelField(Styles.farBlurSubTitle, EditorStyles.miniLabel);
                ++EditorGUI.indentLevel;
                {
                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.FarBlurSampleCount.GetArrayElementAtIndex(tier), Styles.sampleCountQuality);
                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.FarBlurMaxRadius.GetArrayElementAtIndex(tier), Styles.maxRadiusQuality);
                }
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.DoFPhysicallyBased.GetArrayElementAtIndex(tier), Styles.dofPhysicallyBased);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.DoFResolution.GetArrayElementAtIndex(tier), Styles.resolutionQuality);
            if (serialized.renderPipelineSettings.postProcessQualitySettings.DoFPhysicallyBased.GetArrayElementAtIndex(tier).boolValue)
            {
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.AdaptiveSamplingWeight.GetArrayElementAtIndex(tier), Styles.adaptiveSamplingWeight);
            }
            else
            {
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.DoFHighFilteringQuality.GetArrayElementAtIndex(tier), Styles.highQualityFiltering);
            }
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.LimitManualRangeNearBlur.GetArrayElementAtIndex(tier), Styles.limitNearBlur);
        }

        static void DrawMotionBlurQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.MotionBlurSampleCount.GetArrayElementAtIndex(tier), Styles.sampleCountQuality);
        }

        static void DrawBloomQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.BloomRes.GetArrayElementAtIndex(tier), Styles.resolutionQuality);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.BloomHighPrefilteringQuality.GetArrayElementAtIndex(tier), Styles.highQualityPrefiltering);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.BloomHighFilteringQuality.GetArrayElementAtIndex(tier), Styles.highQualityFiltering);
        }

        static void DrawChromaticAberrationQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.ChromaticAbMaxSamples.GetArrayElementAtIndex(tier), Styles.maxSamplesQuality);
        }

        static void DrawAOQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.AOStepCount.GetArrayElementAtIndex(tier), Styles.AOStepCount);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.AOFullRes.GetArrayElementAtIndex(tier), Styles.AOFullRes);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.AOMaximumRadiusPixels.GetArrayElementAtIndex(tier), Styles.AOMaxRadiusInPixels);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.AODirectionCount.GetArrayElementAtIndex(tier), Styles.AODirectionCount);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.AOBilateralUpsample.GetArrayElementAtIndex(tier), Styles.AOBilateralUpsample);
        }

        static void DrawRTAOQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTAORayLength.GetArrayElementAtIndex(tier), Styles.RTAORayLength);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTAOSampleCount.GetArrayElementAtIndex(tier), Styles.RTAOSampleCount);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTAODenoise.GetArrayElementAtIndex(tier), Styles.RTAODenoise);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTAODenoiserRadius.GetArrayElementAtIndex(tier), Styles.RTAODenoiserRadius);
        }

        static void DrawContactShadowQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.ContactShadowSampleCount.GetArrayElementAtIndex(tier), Styles.contactShadowsSampleCount);
        }

        static void DrawSSRQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.SSRMaxRaySteps.GetArrayElementAtIndex(tier), Styles.contactShadowsSampleCount);
        }

        static void DrawRTRQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTRMinSmoothness.GetArrayElementAtIndex(tier), Styles.RTRMinSmoothness);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTRSmoothnessFadeStart.GetArrayElementAtIndex(tier), Styles.RTRSmoothnessFadeStart);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTRRayLength.GetArrayElementAtIndex(tier), Styles.RTRRayLength);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTRFullResolution.GetArrayElementAtIndex(tier), Styles.RTRFullResolution);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTRRayMaxIterations.GetArrayElementAtIndex(tier), Styles.RTRRayMaxIterations);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTRDenoise.GetArrayElementAtIndex(tier), Styles.RTRDenoise);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTRDenoiserRadiusDimmer.GetArrayElementAtIndex(tier), Styles.RTRDenoiserRadius);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTRDenoiserAntiFlicker.GetArrayElementAtIndex(tier), Styles.RTRDenoiserAntiFlicker);
        }

        static void DrawVolumetricFogQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            var budget = serialized.renderPipelineSettings.lightingQualitySettings.VolumetricFogBudget.GetArrayElementAtIndex(tier);
            EditorGUILayout.PropertyField(budget, Styles.FogSettingsBudget);
            budget.floatValue = Mathf.Clamp(budget.floatValue, 0.0f, 1.0f);
            var ratio = serialized.renderPipelineSettings.lightingQualitySettings.VolumetricFogRatio.GetArrayElementAtIndex(tier);
            EditorGUILayout.PropertyField(ratio, Styles.FogSettingsRatio);
            ratio.floatValue = Mathf.Clamp(ratio.floatValue, 0.0f, 1.0f);
        }

        static void DrawRTGIQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTGIRayLength.GetArrayElementAtIndex(tier), Styles.RTGIRayLength);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTGIFullResolution.GetArrayElementAtIndex(tier), Styles.RTGIFullResolution);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTGIRaySteps.GetArrayElementAtIndex(tier), Styles.RTGIRaySteps);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTGIDenoise.GetArrayElementAtIndex(tier), Styles.RTGIDenoise);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTGIHalfResDenoise.GetArrayElementAtIndex(tier), Styles.RTGIHalfResDenoise);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTGIDenoiserRadius.GetArrayElementAtIndex(tier), Styles.RTGIDenoiserRadius);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTGISecondDenoise.GetArrayElementAtIndex(tier), Styles.RTGISecondDenoise);
        }

        static void DrawSSGIQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.SSGIRaySteps.GetArrayElementAtIndex(tier), Styles.SSGIRaySteps);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.SSGIDenoise.GetArrayElementAtIndex(tier), Styles.SSGIDenoise);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.SSGIHalfResDenoise.GetArrayElementAtIndex(tier), Styles.SSGIHalfResDenoise);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.SSGIDenoiserRadius.GetArrayElementAtIndex(tier), Styles.SSGIDenoiserRadius);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.SSGISecondDenoise.GetArrayElementAtIndex(tier), Styles.SSGISecondDenoise);
        }

        internal static void DisplayRayTracingSupportBox()
        {
            CoreEditorUtils.DrawFixMeBox(Styles.rayTracingRestrictionOnlyWarning, "Open", () =>
            {
                HDUserSettings.SetOpen(InclusiveMode.DXROptional, true); // Make sure DXR is open
                HDWizard.OpenWindow();
            });

        }

        static void Drawer_SectionRenderingUnsorted(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.colorBufferFormat, Styles.colorBufferFormatContent);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportedLitShaderMode, Styles.supportLitShaderModeContent);

            // MSAA is an option that is only available in full forward but Camera can be set in Full Forward only. Thus MSAA have no dependency currently
            //Note: do not use SerializedProperty.enumValueIndex here as this enum not start at 0 as it is used as flags.
            bool msaaAllowed = true;
            bool hasRayTracing = false;
            bool hasWater = false;
            for (int index = 0; index < serialized.serializedObject.targetObjects.Length && msaaAllowed; ++index)
            {
                var settings = (serialized.serializedObject.targetObjects[index] as HDRenderPipelineAsset).currentPlatformRenderPipelineSettings;
                var litShaderMode = settings.supportedLitShaderMode;
                hasRayTracing |= settings.supportRayTracing;
                hasWater |= settings.supportWater;
                msaaAllowed &= (litShaderMode == SupportedLitShaderMode.ForwardOnly || litShaderMode == SupportedLitShaderMode.Both) && !settings.supportRayTracing && !settings.supportWater;
            }

            using (new EditorGUI.DisabledScope(!msaaAllowed))
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.MSAASampleCount, Styles.MSAASampleCountContent);
                --EditorGUI.indentLevel;
            }

            if (hasRayTracing && serialized.renderPipelineSettings.MSAASampleCount.intValue != (int)MSAASamples.None)
            {
                EditorGUILayout.HelpBox(Styles.rayTracingMSAAUnsupported.text, MessageType.Info, wide: true);
            }

            if (hasWater && serialized.renderPipelineSettings.MSAASampleCount.intValue != (int)MSAASamples.None)
            {
                EditorGUILayout.HelpBox(Styles.waterMSAAUnsupported.text, MessageType.Info, wide: true);
            }

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportMotionVectors, Styles.supportMotionVectorContent);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportRuntimeAOVAPI, Styles.supportRuntimeAOVAPIContent);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportTerrainHole, Styles.supportTerrainHoleContent);

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportTransparentBackface, Styles.supportTransparentBackface);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportTransparentDepthPrepass, Styles.supportTransparentDepthPrepass);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportTransparentDepthPostpass, Styles.supportTransparentDepthPostpass);

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportCustomPass, Styles.supportCustomPassContent);
            using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.supportCustomPass.boolValue))
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.customBufferFormat, Styles.customBufferFormatContent);
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportRayTracing, Styles.supportRaytracing);

            using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.supportRayTracing.boolValue))
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportedRayTracingMode, Styles.supportedRayTracingMode);
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportVFXRayTracing,
                    Styles.supportVFXRayTracing);

                // If ray tracing is enabled by the asset but the current system does not support it display a warning
                if (!HDRenderPipeline.currentSystemSupportsRayTracing)
                {
                    if (serialized.renderPipelineSettings.supportRayTracing.boolValue)
                        DisplayRayTracingSupportBox();
                    else
                        EditorGUILayout.HelpBox(Styles.rayTracingUnsupportedWarning.text, MessageType.Warning, wide: true);
                }
                --EditorGUI.indentLevel;
            }

            EditorGUI.BeginChangeCheck();
            serialized.renderPipelineSettings.lodBias.ValueGUI<float>(Styles.LODBias);
            if (EditorGUI.EndChangeCheck())
            {
                for (var i = 0; i < serialized.renderPipelineSettings.lodBias.GetSchemaLevelCount(); ++i)
                {
                    var prop = serialized.renderPipelineSettings.lodBias.values.GetArrayElementAtIndex(i);
                    prop.SetInline(Mathf.Max(0.01f, prop.GetInline<float>()));
                }
            }

            EditorGUI.BeginChangeCheck();
            serialized.renderPipelineSettings.maximumLODLevel.ValueGUI<int>(Styles.maximumLODLevel);
            if (EditorGUI.EndChangeCheck())
            {
                for (var i = 0; i < serialized.renderPipelineSettings.maximumLODLevel.GetSchemaLevelCount(); ++i)
                {
                    var prop = serialized.renderPipelineSettings.maximumLODLevel.values.GetArrayElementAtIndex(i);
                    prop.SetInline(Mathf.Clamp(prop.GetInline<int>(), 0, 7));
                }
            }

            var gpuResidentDrawerSettings = serialized.renderPipelineSettings.gpuResidentDrawerSettings;
            EditorGUILayout.PropertyField(gpuResidentDrawerSettings.mode, Styles.gpuResidentDrawerMode);

            var brgStrippingError = EditorGraphicsSettings.batchRendererGroupShaderStrippingMode != BatchRendererGroupStrippingMode.KeepAll;
            var staticBatchingInfo = PlayerSettings.GetStaticBatchingForPlatform(EditorUserBuildSettings.activeBuildTarget);
            if ((GPUResidentDrawerMode)gpuResidentDrawerSettings.mode.intValue != GPUResidentDrawerMode.Disabled)
            {
                ++EditorGUI.indentLevel;
                gpuResidentDrawerSettings.smallMeshScreenPercentage.floatValue = Mathf.Clamp(EditorGUILayout.FloatField(Styles.smallMeshScreenPercentage, gpuResidentDrawerSettings.smallMeshScreenPercentage.floatValue), 0.0f, 20.0f);
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.gpuResidentDrawerSettings.enableOcclusionCullingInCameras, Styles.enableOcclusionCullingInCameras);
                if ((GPUResidentDrawerMode)gpuResidentDrawerSettings.mode.intValue == GPUResidentDrawerMode.InstancedDrawing && gpuResidentDrawerSettings.enableOcclusionCullingInCameras.boolValue)
                {
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.gpuResidentDrawerSettings.useDepthPrepassForOccluders, Styles.useDepthPrepassForOccluders);
                    --EditorGUI.indentLevel;
                }
                --EditorGUI.indentLevel;

                if(brgStrippingError)
                    EditorGUILayout.HelpBox(Styles.brgShaderStrippingErrorMessage.text, MessageType.Warning, true);
                if(staticBatchingInfo)
                    EditorGUILayout.HelpBox(Styles.staticBatchingInfoMessage.text, MessageType.Info, true);
            }

            EditorGUILayout.Space(); //to separate with following sub sections
        }

        static void DoThing(){
            Debug.Log("DoThing");
        }

        static void Drawer_SectionLightingUnsorted(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportSSAO, Styles.supportSSAOContent);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportSSGI, Styles.supportSSGIContent);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportLightLayers, Styles.supportLightLayerContent);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.renderingLayerMaskBuffer, Styles.renderingLayerMaskBuffer);
            if (EditorGUI.EndChangeCheck())
                HDSampleBufferNode.UpdateWarningBadges(HDSampleBufferNode.BufferType.RenderingLayerMask, serialized.renderPipelineSettings.renderingLayerMaskBuffer.boolValue);

            EditorGUILayout.Space(); //to separate with following sub sections
        }

        static void Drawer_SectionMaterialUnsorted(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.availableMaterialQualityLevels);
            var v = EditorGUILayout.EnumPopup(Styles.materialQualityLevelContent, (MaterialQuality)serialized.defaultMaterialQualityLevel.intValue);
            serialized.defaultMaterialQualityLevel.intValue = (int)(object)v;

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportDistortion, Styles.supportDistortion);

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportSubsurfaceScattering, Styles.supportedSSSContent);
            using (new EditorGUI.DisabledScope(serialized.renderPipelineSettings.supportSubsurfaceScattering.hasMultipleDifferentValues
                || !serialized.renderPipelineSettings.supportSubsurfaceScattering.boolValue))
            {
                ++EditorGUI.indentLevel;
                serialized.renderPipelineSettings.sssSampleBudget.ValueGUI<int>(Styles.sssSampleBudget);

                EditorGUI.BeginChangeCheck();
                serialized.renderPipelineSettings.sssDownsampleSteps.ValueGUI<int>(Styles.sssDownsampleSteps);
                if (EditorGUI.EndChangeCheck())
                {
                    for (var i = 0; i < serialized.renderPipelineSettings.sssDownsampleSteps.GetSchemaLevelCount(); ++i)
                    {
                        var prop = serialized.renderPipelineSettings.sssDownsampleSteps.values.GetArrayElementAtIndex(i);
                        prop.SetInline(Mathf.Clamp(prop.GetInline<int>(), 0, (int)DefaultSssDownsampleSteps.Max));
                    }
                }

                --EditorGUI.indentLevel;
            }

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.supportFabricConvolution, Styles.supportFabricBSDFConvolutionContent);
        }

        static void Drawer_LensFlare(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportDataDrivenLensFlare, Styles.supportDataDrivenLensFlare);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportScreenSpaceLensFlare, Styles.supportScreenSpaceLensFlare);
        }

        const string supportedFormaterMultipleValue = "\u2022 {0} --Multiple different values--";
        const string supportedFormater = "\u2022 {0} ({1})";
        const string supportedLitShaderModeFormater = "\u2022 {0}: {1} ({2})";
        static void AppendSupport(StringBuilder builder, SerializedProperty property, GUIContent content)
        {
            if (property.hasMultipleDifferentValues)
                builder.AppendLine().AppendFormat(supportedFormaterMultipleValue, content.text);
            else if (property.boolValue)
                builder.AppendLine().AppendFormat(supportedFormater, content.text, Styles.supportDrawbacks[content]);
        }

        static void SupportedSettingsInfoSection(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            StringBuilder builder = new StringBuilder("Features supported by this asset:").AppendLine();
            SupportedLitShaderMode supportedLitShaderMode = serialized.renderPipelineSettings.supportedLitShaderMode.GetEnumValue<SupportedLitShaderMode>();
            if (serialized.renderPipelineSettings.supportedLitShaderMode.hasMultipleDifferentValues)
                builder.AppendFormat(supportedFormaterMultipleValue, Styles.supportLitShaderModeContent.text);
            else
                builder.AppendFormat(supportedLitShaderModeFormater, Styles.supportLitShaderModeContent.text, supportedLitShaderMode, Styles.supportLitShaderModeDrawbacks[supportedLitShaderMode]);

            if (serialized.renderPipelineSettings.supportShadowMask.hasMultipleDifferentValues || serialized.renderPipelineSettings.supportedLitShaderMode.hasMultipleDifferentValues)
                builder.AppendLine().AppendFormat(supportedFormaterMultipleValue, Styles.supportShadowMaskContent.text);
            else if (serialized.renderPipelineSettings.supportShadowMask.boolValue)
                builder.AppendLine().AppendFormat(supportedFormater, Styles.supportShadowMaskContent.text, Styles.supportShadowMaskDrawbacks[supportedLitShaderMode]);

            AppendSupport(builder, serialized.renderPipelineSettings.supportSSR, Styles.supportSSRContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportSSAO, Styles.supportSSAOContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportSubsurfaceScattering, Styles.supportedSSSContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportVolumetrics, Styles.supportVolumetricFogContent);

            if (serialized.renderPipelineSettings.supportLightLayers.hasMultipleDifferentValues)
                builder.AppendLine().AppendFormat(supportedFormaterMultipleValue, Styles.supportLightLayerContent.text);
            else if (serialized.renderPipelineSettings.supportLightLayers.boolValue)
                builder.AppendLine().AppendFormat(supportedFormater, Styles.supportLightLayerContent.text, Styles.supportLightLayerDrawbacks[supportedLitShaderMode]);

            if (serialized.renderPipelineSettings.MSAASampleCount.hasMultipleDifferentValues)
                builder.AppendLine().AppendFormat(supportedFormaterMultipleValue, Styles.MSAASampleCountContent.text);
            else
            {
                // NO MSAA in deferred
                if (serialized.renderPipelineSettings.supportedLitShaderMode.intValue != (int)RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly)
                    builder.AppendLine().AppendFormat(supportedFormater, "Multisample Anti-aliasing", Styles.supportDrawbacks[Styles.MSAASampleCountContent]);
            }

            if (serialized.renderPipelineSettings.supportDecals.hasMultipleDifferentValues)
                builder.AppendLine().AppendFormat(supportedFormaterMultipleValue, Styles.decalsSubTitle.text);
            else if (serialized.renderPipelineSettings.supportDecals.boolValue)
                builder.AppendLine().AppendFormat(supportedFormater, Styles.decalsSubTitle.text, Styles.supportDrawbacks[Styles.supportDecalContent]);

            if (serialized.renderPipelineSettings.supportDecalLayers.hasMultipleDifferentValues)
                builder.AppendLine().AppendFormat(supportedFormaterMultipleValue, Styles.decalsSubTitle.text);
            else if (serialized.renderPipelineSettings.supportDecals.boolValue && serialized.renderPipelineSettings.supportDecalLayers.boolValue)
                builder.AppendLine().AppendFormat(supportedFormater, Styles.decalsSubTitle.text, Styles.supportDrawbacks[Styles.supportDecalLayersContent]);

            if (serialized.renderPipelineSettings.decalSettings.perChannelMask.hasMultipleDifferentValues)
                builder.AppendLine().AppendFormat(supportedFormaterMultipleValue, Styles.decalsMetalAndAOSubTitle.text);
            else if (serialized.renderPipelineSettings.supportDecals.boolValue && serialized.renderPipelineSettings.decalSettings.perChannelMask.boolValue)
                builder.AppendLine().AppendFormat(supportedFormater, Styles.decalsMetalAndAOSubTitle.text, Styles.supportDrawbacks[Styles.metalAndAOContent]);

            AppendSupport(builder, serialized.renderPipelineSettings.supportMotionVectors, Styles.supportMotionVectorContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportRuntimeAOVAPI, Styles.supportRuntimeAOVAPIContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportTerrainHole, Styles.supportTerrainHoleContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportDistortion, Styles.supportDistortion);
            AppendSupport(builder, serialized.renderPipelineSettings.supportTransparentBackface, Styles.supportTransparentBackface);
            AppendSupport(builder, serialized.renderPipelineSettings.supportTransparentDepthPrepass, Styles.supportTransparentDepthPrepass);
            AppendSupport(builder, serialized.renderPipelineSettings.supportTransparentDepthPostpass, Styles.supportTransparentDepthPostpass);
            AppendSupport(builder, serialized.renderPipelineSettings.supportRayTracing, Styles.supportRaytracing);
            AppendSupport(builder, serialized.renderPipelineSettings.lightProbeSystem, Styles.lightProbeSystemContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportedRayTracingMode, Styles.supportedRayTracingMode);

            AppendSupport(builder, serialized.renderPipelineSettings.supportScreenSpaceLensFlare, Styles.supportScreenSpaceLensFlare);
            AppendSupport(builder, serialized.renderPipelineSettings.supportDataDrivenLensFlare, Styles.supportDataDrivenLensFlare);

            EditorGUILayout.HelpBox(builder.ToString(), MessageType.Info, wide: true);
        }
    }
}
