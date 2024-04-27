using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static UnityEngine.Rendering.HighDefinition.RenderPipelineSettings;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDRenderPipelineAsset>;

    static partial class HDRenderPipelineUI
    {
        #region Expandable States

        internal enum Expandable
        {
            // Obsolete values
            Rendering = 1 << 4,
            Lighting = 1 << 5,
            Material = 1 << 6,
            LightLoop = 1 << 7,
            Cookie = 1 << 8,
            Reflection = 1 << 9,
            Sky = 1 << 10,
            Shadow = 1 << 11,
            Decal = 1 << 12,
            PostProcess = 1 << 13,
            DynamicResolution = 1 << 14,
            LowResTransparency = 1 << 15,
            PostProcessQuality = 1 << 16,
            DepthOfFieldQuality = 1 << 17,
            MotionBlurQuality = 1 << 18,
            BloomQuality = 1 << 19,
            ChromaticAberrationQuality = 1 << 20,
            XR = 1 << 21,
            LightLayer = 1 << 22,
            SSAOQuality = 1 << 23,
            ContactShadowQuality = 1 << 24,
            LightingQuality = 1 << 25,
            SSRQuality = 1 << 26,
            VirtualTexturing = 1 << 27,
            FogQuality = 1 << 28,
            Volumetric = 1 << 29,
            ProbeVolume = 1 << 30,
            RTAOQuality = 1 << 31,
            RTRQuality = 1 << 32,
            RTGIQuality = 1 << 33,
            SSGIQuality = 1 << 34,
            Water = 1 << 35,
        }
        
        internal enum ExpandablePostProcess
        {
            LensFlare = 1 << 0
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

        static readonly ExpandedState<Expandable, HDRenderPipelineAsset> k_ExpandedState = new(Expandable.Rendering, "HDRP");
        static readonly ExpandedState<ExpandablePostProcess, HDRenderPipelineAsset> k_ExpandablePostProcessState = new(0, "HDRP");
        static readonly ExpandedState<ExpandableShadows, HDRenderPipelineAsset> k_LightsExpandedState = new(0, "HDRP");

        static readonly Dictionary<GUIContent, ExpandedState<ExpandableQualities, HDRenderPipelineAsset>>
        k_QualityExpandedStates = new();
        private static CED.IDrawer QualityDrawer(GUIContent content, Expandable expandable, Action<SerializedHDRenderPipelineAsset, int> qualityActionForTier)
        {
            // Make sure that the section is not registered
            if (k_QualityExpandedStates.TryGetValue(content, out var key))
                throw new Exception($"Quality Section {content.text} already registered");

            // Register the section
            key = new ExpandedState<ExpandableQualities, HDRenderPipelineAsset>(0, $"HDRP:{content}");
            k_QualityExpandedStates[content] = key;

            const FoldoutOption options = FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd;
            return CED.FoldoutGroup(content, expandable, k_ExpandedState, options,
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
                CED.FoldoutGroup(Styles.renderingSectionTitle, Expandable.Rendering, k_ExpandedState,
                    CED.Group(GroupOption.Indent, Drawer_SectionRenderingUnsorted),
                    CED.FoldoutGroup(Styles.decalsSubTitle, Expandable.Decal, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionDecalSettings),
                    CED.FoldoutGroup(Styles.dynamicResolutionSubTitle, Expandable.DynamicResolution, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionDynamicResolutionSettings),
                    CED.FoldoutGroup(Styles.lowResTransparencySubTitle, Expandable.LowResTransparency, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionLowResTransparentSettings),
                    CED.FoldoutGroup(Styles.waterSubTitle, Expandable.Water, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionWaterSettings)
                    ),
                CED.FoldoutGroup(Styles.lightingSectionTitle, Expandable.Lighting, k_ExpandedState,
                    CED.Group(GroupOption.Indent, Drawer_SectionLightingUnsorted),
                    CED.FoldoutGroup(Styles.volumetricSubTitle, Expandable.Volumetric, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_Volumetric),
                    CED.FoldoutGroup(Styles.lightProbeSubTitle, Expandable.ProbeVolume, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionProbeVolume),
                    CED.FoldoutGroup(Styles.cookiesSubTitle, Expandable.Cookie, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionCookies),
                    CED.FoldoutGroup(Styles.reflectionsSubTitle, Expandable.Reflection, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionReflection),
                    CED.FoldoutGroup(Styles.skySubTitle, Expandable.Sky, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionSky),
                    CED.FoldoutGroup(Styles.shadowSubTitle, Expandable.Shadow, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout,
                        CED.Group(Drawer_SectionShadows),
                        CED.FoldoutGroup(Styles.punctualLightshadowSubTitle, ExpandableShadows.PunctualLightShadows, k_LightsExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_PunctualLightSectionShadows),
                        CED.FoldoutGroup(Styles.directionalLightshadowSubTitle, ExpandableShadows.DirectionalLightShadows, k_LightsExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_DirectionalLightSectionShadows),
                        CED.FoldoutGroup(Styles.areaLightshadowSubTitle, ExpandableShadows.AreaLightShadows, k_LightsExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_AreaLightSectionShadows)
                        ),
                    CED.FoldoutGroup(Styles.lightLoopSubTitle, Expandable.LightLoop, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionLightLoop)
                    ),
                CED.FoldoutGroup(Styles.lightingQualitySettings, Expandable.LightingQuality, k_ExpandedState,
                    QualityDrawer(Styles.SSAOQualitySettingSubTitle, Expandable.SSAOQuality, DrawAOQualitySetting),
                    QualityDrawer(Styles.RTAOQualitySettingSubTitle, Expandable.RTAOQuality, DrawRTAOQualitySetting),
                    QualityDrawer(Styles.contactShadowsSettingsSubTitle, Expandable.ContactShadowQuality, DrawContactShadowQualitySetting),
                    QualityDrawer(Styles.SSRSettingsSubTitle, Expandable.SSRQuality, DrawSSRQualitySetting),
                    QualityDrawer(Styles.RTRSettingsSubTitle, Expandable.RTRQuality, DrawRTRQualitySetting),
                    QualityDrawer(Styles.FogSettingsSubTitle, Expandable.FogQuality, DrawVolumetricFogQualitySetting),
                    QualityDrawer(Styles.RTGISettingsSubTitle, Expandable.RTGIQuality, DrawRTGIQualitySetting),
                    QualityDrawer(Styles.SSGISettingsSubTitle, Expandable.SSGIQuality, DrawSSGIQualitySetting)
                    ),
                CED.FoldoutGroup(Styles.materialSectionTitle, Expandable.Material, k_ExpandedState, Drawer_SectionMaterialUnsorted),
                CED.FoldoutGroup(Styles.postProcessSectionTitle, Expandable.PostProcess, k_ExpandedState, 
                    CED.Group(GroupOption.Indent, Drawer_SectionPostProcessSettings),
                    CED.FoldoutGroup(Styles.LensFlareTitle, ExpandablePostProcess.LensFlare, k_ExpandablePostProcessState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_LensFlare)
                ),
                CED.FoldoutGroup(Styles.postProcessQualitySubTitle, Expandable.PostProcessQuality, k_ExpandedState,
                    QualityDrawer(Styles.depthOfFieldQualitySettings, Expandable.DepthOfFieldQuality, DrawDepthOfFieldQualitySetting),
                    QualityDrawer(Styles.motionBlurQualitySettings, Expandable.MotionBlurQuality, DrawMotionBlurQualitySetting),
                    QualityDrawer(Styles.bloomQualitySettings, Expandable.BloomQuality, DrawBloomQualitySetting),
                    QualityDrawer(Styles.chromaticAberrationQualitySettings, Expandable.ChromaticAberrationQuality, DrawChromaticAberrationQualitySetting)
                    ),
                CED.FoldoutGroup(Styles.xrTitle, Expandable.XR, k_ExpandedState, Drawer_SectionXRSettings),
                CED.FoldoutGroup(Styles.virtualTexturingTitle, Expandable.VirtualTexturing, k_ExpandedState, Drawer_SectionVTSettings)
            );
        }

        public static readonly CED.IDrawer Inspector;
        
        static void Drawer_LensFlare(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportDataDrivenLensFlare, Styles.supportDataDrivenLensFlare);
        }
        
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
            if (serialized.renderPipelineSettings.lightProbeSystem.intValue == (int)LightProbeSystem.ProbeVolumes)
            {
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.probeVolumeTextureSize, Styles.probeVolumeMemoryBudget);
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.probeVolumeBlendingTextureSize, Styles.probeVolumeBlendingMemoryBudget);
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.probeVolumeSHBands, Styles.probeVolumeSHBands);
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportProbeVolumeStreaming, Styles.supportProbeVolumeStreaming);

                int estimatedVMemCost = ProbeReferenceVolume.instance.GetVideoMemoryCost();
                if (estimatedVMemCost == 0)
                {
                    EditorGUILayout.HelpBox($"Estimated GPU Memory cost: 0.\nProbe reference volume is not used in the scene and resources haven't been allocated yet.", MessageType.Info, wide: true);
                }
                else
                {
                    EditorGUILayout.HelpBox($"Estimated GPU Memory cost: {estimatedVMemCost / (1000 * 1000)} MB.", MessageType.Info, wide: true);
                }
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
                serialized.renderPipelineSettings.hdShadowInitParams.maxShadowRequests.intValue = Mathf.Max(1, serialized.renderPipelineSettings.hdShadowInitParams.maxShadowRequests.intValue);

            if (!serialized.renderPipelineSettings.supportedLitShaderMode.hasMultipleDifferentValues)
            {
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.hdShadowInitParams.shadowFilteringQuality, Styles.filteringQuality);
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

        static void Drawer_SectionDynamicResolutionSettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.enabled, Styles.enabled);

            bool showUpsampleFilterAsFallback = false;

            ++EditorGUI.indentLevel;

            using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.dynamicResolutionSettings.enabled.boolValue))
            {
#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
                bool dlssDetected = HDDynamicResolutionPlatformCapabilities.DLSSDetected;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.enableDLSS, Styles.enableDLSS);

                if (serialized.renderPipelineSettings.dynamicResolutionSettings.enableDLSS.boolValue)
                {
                    ++EditorGUI.indentLevel;
                    var v = EditorGUILayout.EnumPopup(
                        Styles.DLSSQualitySettingContent,
                        (UnityEngine.NVIDIA.DLSSQuality)
                        serialized.renderPipelineSettings.dynamicResolutionSettings.DLSSPerfQualitySetting.intValue);

                    serialized.renderPipelineSettings.dynamicResolutionSettings.DLSSPerfQualitySetting.intValue = (int)(object)v;

                    int injectionPointVal = EditorGUILayout.IntPopup(Styles.DLSSInjectionPoint, serialized.renderPipelineSettings.dynamicResolutionSettings.DLSSInjectionPoint.intValue, Styles.DLSSInjectionPointNames, Styles.DLSSInjectionPointValues);
                    serialized.renderPipelineSettings.dynamicResolutionSettings.DLSSInjectionPoint.intValue = injectionPointVal;
                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.DLSSUseOptimalSettings, Styles.DLSSUseOptimalSettingsContent);

                    using (new EditorGUI.DisabledScope(serialized.renderPipelineSettings.dynamicResolutionSettings.DLSSUseOptimalSettings.boolValue))
                    {
                        EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.DLSSSharpness, Styles.DLSSSharpnessContent);
                    }
                    --EditorGUI.indentLevel;
                }

                showUpsampleFilterAsFallback = serialized.renderPipelineSettings.dynamicResolutionSettings.enableDLSS.boolValue;
                if (serialized.renderPipelineSettings.dynamicResolutionSettings.enableDLSS.boolValue)
                {
                    EditorGUILayout.HelpBox(
                        dlssDetected ? Styles.DLSSFeatureDetectedMsg : Styles.DLSSFeatureNotDetectedMsg,
                        dlssDetected ? MessageType.Info : MessageType.Warning);
                }

                if (dlssDetected && EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64 && serialized.renderPipelineSettings.dynamicResolutionSettings.enableDLSS.boolValue)
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
#endif

                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.dynamicResType, Styles.dynResType);
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

                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.useMipBias, Styles.useMipBias);

                if (!serialized.renderPipelineSettings.dynamicResolutionSettings.forcePercentage.hasMultipleDifferentValues
                    && !serialized.renderPipelineSettings.dynamicResolutionSettings.forcePercentage.boolValue)
                {
#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
                    if (dlssDetected && serialized.renderPipelineSettings.dynamicResolutionSettings.enableDLSS.boolValue && serialized.renderPipelineSettings.dynamicResolutionSettings.DLSSUseOptimalSettings.boolValue)
                    {
                        EditorGUILayout.HelpBox(Styles.DLSSIgnorePercentages, MessageType.Info);
                    }
#endif
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

                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.forcePercentage, Styles.forceScreenPercentage);

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


                if (serialized.renderPipelineSettings.dynamicResolutionSettings.forcePercentage.hasMultipleDifferentValues)
                {
                    using (new EditorGUI.DisabledGroupScope(true))
                        EditorGUILayout.LabelField(Styles.multipleDifferenteValueMessage);
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
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportWater, Styles.supportWaterContent);
            using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.supportWater.boolValue))
            {
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.waterSimulationResolution, Styles.waterSimulationResolutionContent);
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.waterCPUSimulation, Styles.cpuSimulationContent);
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
            if (serialized.renderPipelineSettings.postProcessQualitySettings.DoFPhysicallyBased.GetArrayElementAtIndex(tier).boolValue)
            {
                int currentResolution = serialized.renderPipelineSettings.postProcessQualitySettings.DoFResolution.GetArrayElementAtIndex(tier).intValue;
                bool isHighResolution =  currentResolution <= (int)DepthOfFieldResolution.Half;
                isHighResolution = EditorGUILayout.Toggle(Styles.pbrResolutionQualityTitle, isHighResolution);
                serialized.renderPipelineSettings.postProcessQualitySettings.DoFResolution.GetArrayElementAtIndex(tier).intValue = isHighResolution ? Math.Min((int)DepthOfFieldResolution.Half, currentResolution) : (int)DepthOfFieldResolution.Quarter;
            }
            else
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.DoFResolution.GetArrayElementAtIndex(tier), Styles.resolutionQuality);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.DoFHighFilteringQuality.GetArrayElementAtIndex(tier), Styles.highQualityFiltering);
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
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTRClampValue.GetArrayElementAtIndex(tier), Styles.RTRClampValue);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTRFullResolution.GetArrayElementAtIndex(tier), Styles.RTRFullResolution);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTRRayMaxIterations.GetArrayElementAtIndex(tier), Styles.RTRRayMaxIterations);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTRDenoise.GetArrayElementAtIndex(tier), Styles.RTRDenoise);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTRDenoiserRadius.GetArrayElementAtIndex(tier), Styles.RTRDenoiserRadius);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTRSmoothDenoising.GetArrayElementAtIndex(tier), Styles.RTRSmoothDenoising);
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
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTGIClampValue.GetArrayElementAtIndex(tier), Styles.RTGIClampValue);
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
                HDUserSettings.wizardActiveTab = 2; // focus on dxr tab
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
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportDitheringCrossFade, Styles.supportDitheringCrossFadeContent);
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
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportRayTracing, Styles.supportRaytracing);
            if (EditorGUI.EndChangeCheck())
            {
                if (serialized.renderPipelineSettings.supportRayTracing.boolValue)
                    HDRenderPipelineGlobalSettings.instance.EnsureRayTracingResources(forceReload: false);
                else
                    HDRenderPipelineGlobalSettings.instance.ClearRayTracingResources();
            }

            using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.supportRayTracing.boolValue))
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportedRayTracingMode, Styles.supportedRayTracingMode);

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

            EditorGUILayout.Space(); //to separate with following sub sections
        }

        static void Drawer_SectionLightingUnsorted(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportSSAO, Styles.supportSSAOContent);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportSSGI, Styles.supportSSGIContent);

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportLightLayers, Styles.supportLightLayerContent);

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
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.supportFabricConvolution, Styles.supportFabricBSDFConvolutionContent);
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
            AppendSupport(builder, serialized.renderPipelineSettings.supportDitheringCrossFade, Styles.supportDitheringCrossFadeContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportTerrainHole, Styles.supportTerrainHoleContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportDistortion, Styles.supportDistortion);
            AppendSupport(builder, serialized.renderPipelineSettings.supportTransparentBackface, Styles.supportTransparentBackface);
            AppendSupport(builder, serialized.renderPipelineSettings.supportTransparentDepthPrepass, Styles.supportTransparentDepthPrepass);
            AppendSupport(builder, serialized.renderPipelineSettings.supportTransparentDepthPostpass, Styles.supportTransparentDepthPostpass);
            AppendSupport(builder, serialized.renderPipelineSettings.supportRayTracing, Styles.supportRaytracing);
            AppendSupport(builder, serialized.renderPipelineSettings.lightProbeSystem, Styles.lightProbeSystemContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportedRayTracingMode, Styles.supportedRayTracingMode);
            AppendSupport(builder, serialized.renderPipelineSettings.supportDataDrivenLensFlare, Styles.supportDataDrivenLensFlare);

            EditorGUILayout.HelpBox(builder.ToString(), MessageType.Info, wide: true);
        }
    }
}
