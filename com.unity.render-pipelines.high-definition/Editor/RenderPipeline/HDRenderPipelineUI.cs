using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Text;
using Utilities;
using UnityEngine.Experimental.Rendering;
using static UnityEngine.Rendering.HighDefinition.RenderPipelineSettings;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDRenderPipelineAsset>;

    static partial class HDRenderPipelineUI
    {
        enum Expandable
        {
            CameraFrameSettings = 1 << 0,
            BakedOrCustomProbeFrameSettings = 1 << 1,
            RealtimeProbeFrameSettings = 1 << 2,
            General = 1 << 3,
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
        }

        static readonly ExpandedState<Expandable, HDRenderPipelineAsset> k_ExpandedState = new ExpandedState<Expandable, HDRenderPipelineAsset>(Expandable.CameraFrameSettings | Expandable.General, "HDRP");

        enum ShadowResolutionValue
        {
            ShadowResolution128 = 128,
            ShadowResolution256 = 256,
            ShadowResolution512 = 512,
            ShadowResolution1024 = 1024,
            ShadowResolution2048 = 2048,
            ShadowResolution4096 = 4096,
            ShadowResolution8192 = 8192,
            ShadowResolution16384 = 16384
        }

        internal enum SelectedFrameSettings
        {
            Camera,
            BakedOrCustomReflection,
            RealtimeReflection
        }

        internal static DiffusionProfileSettingsListUI diffusionProfileUI = new DiffusionProfileSettingsListUI();

        internal static SelectedFrameSettings selectedFrameSettings;

        static HDRenderPipelineUI()
        {
            Inspector = CED.Group(
                CED.FoldoutGroup(Styles.renderingSectionTitle, Expandable.Rendering, k_ExpandedState,
                    CED.Group(GroupOption.Indent, Drawer_SectionRenderingUnsorted),
                    CED.FoldoutGroup(Styles.decalsSubTitle, Expandable.Decal, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionDecalSettings),
                    CED.FoldoutGroup(Styles.dynamicResolutionSubTitle, Expandable.DynamicResolution, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionDynamicResolutionSettings),
                    CED.FoldoutGroup(Styles.lowResTransparencySubTitle, Expandable.LowResTransparency, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionLowResTransparentSettings)
                    ),
                CED.FoldoutGroup(Styles.lightingSectionTitle, Expandable.Lighting, k_ExpandedState,
                    CED.Group(GroupOption.Indent, Drawer_SectionLightingUnsorted),
                    CED.FoldoutGroup(Styles.lightLayerSubTitle, Expandable.LightLayer, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionLightLayers),
                    CED.FoldoutGroup(Styles.cookiesSubTitle, Expandable.Cookie, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionCookies),
                    CED.FoldoutGroup(Styles.reflectionsSubTitle, Expandable.Reflection, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionReflection),
                    CED.FoldoutGroup(Styles.skySubTitle, Expandable.Sky, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionSky),
                    CED.FoldoutGroup(Styles.shadowSubTitle, Expandable.Shadow, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionShadows),
                    CED.FoldoutGroup(Styles.lightLoopSubTitle, Expandable.LightLoop, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionLightLoop)
                    ),
                CED.FoldoutGroup(Styles.lightingQualitySettings, Expandable.LightingQuality, k_ExpandedState,
                    CED.FoldoutGroup(Styles.SSAOQualitySettingSubTitle, Expandable.SSAOQuality, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionSSAOQualitySettings),
                    CED.FoldoutGroup(Styles.contactShadowsSettingsSubTitle, Expandable.ContactShadowQuality, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionContactShadowQualitySettings),
                    CED.FoldoutGroup(Styles.SSRSettingsSubTitle, Expandable.SSRQuality, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionSSRQualitySettings)
                    ),
                CED.FoldoutGroup(Styles.materialSectionTitle, Expandable.Material, k_ExpandedState, Drawer_SectionMaterialUnsorted),
                CED.FoldoutGroup(Styles.postProcessSectionTitle, Expandable.PostProcess, k_ExpandedState, Drawer_SectionPostProcessSettings),
                CED.FoldoutGroup(Styles.postProcessQualitySubTitle, Expandable.PostProcessQuality, k_ExpandedState,
                    CED.FoldoutGroup(Styles.depthOfFieldQualitySettings, Expandable.DepthOfFieldQuality, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionDepthOfFieldQualitySettings),
                    CED.FoldoutGroup(Styles.motionBlurQualitySettings, Expandable.MotionBlurQuality, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionMotionBlurQualitySettings),
                    CED.FoldoutGroup(Styles.bloomQualitySettings, Expandable.BloomQuality, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionBloomQualitySettings),
                    CED.FoldoutGroup(Styles.chromaticAberrationQualitySettings, Expandable.ChromaticAberrationQuality, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionChromaticAberrationQualitySettings)
                    ),
                CED.FoldoutGroup(Styles.xrTitle, Expandable.XR, k_ExpandedState, Drawer_SectionXRSettings)
            );

            // fix init of selection along what is serialized
            if (k_ExpandedState[Expandable.BakedOrCustomProbeFrameSettings])
                selectedFrameSettings = SelectedFrameSettings.BakedOrCustomReflection;
            else if (k_ExpandedState[Expandable.RealtimeProbeFrameSettings])
                selectedFrameSettings = SelectedFrameSettings.RealtimeReflection;
            else //default value: camera
                selectedFrameSettings = SelectedFrameSettings.Camera;
        }

        public static readonly CED.IDrawer Inspector;

        public static readonly CED.IDrawer GeneralSection = CED.Group(Drawer_SectionGeneral);

        public static readonly CED.IDrawer FrameSettingsSection = CED.Group(
            CED.Group(
                (serialized, owner) => EditorGUILayout.BeginVertical("box"),
                Drawer_TitleDefaultFrameSettings
                ),
            CED.Conditional(
                (serialized, owner) => k_ExpandedState[Expandable.CameraFrameSettings],
                CED.Select(
                    (serialized, owner) => serialized.defaultFrameSettings,
                    FrameSettingsUI.InspectorInnerbox(withOverride: false)
                    )
                ),
            CED.Conditional(
                (serialized, owner) => k_ExpandedState[Expandable.BakedOrCustomProbeFrameSettings],
                CED.Select(
                    (serialized, owner) => serialized.defaultBakedOrCustomReflectionFrameSettings,
                    FrameSettingsUI.InspectorInnerbox(withOverride: false)
                    )
                ),
            CED.Conditional(
                (serialized, owner) => k_ExpandedState[Expandable.RealtimeProbeFrameSettings],
                CED.Select(
                    (serialized, owner) => serialized.defaultRealtimeReflectionFrameSettings,
                    FrameSettingsUI.InspectorInnerbox(withOverride: false)
                    )
                ),
            CED.Group((serialized, owner) => EditorGUILayout.EndVertical())
            );

        static public void ApplyChangedDisplayedFrameSettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            k_ExpandedState.SetExpandedAreas(Expandable.CameraFrameSettings | Expandable.BakedOrCustomProbeFrameSettings | Expandable.RealtimeProbeFrameSettings, false);
            switch (selectedFrameSettings)
            {
                case SelectedFrameSettings.Camera:
                    k_ExpandedState.SetExpandedAreas(Expandable.CameraFrameSettings, true);
                    break;
                case SelectedFrameSettings.BakedOrCustomReflection:
                    k_ExpandedState.SetExpandedAreas(Expandable.BakedOrCustomProbeFrameSettings, true);
                    break;
                case SelectedFrameSettings.RealtimeReflection:
                    k_ExpandedState.SetExpandedAreas(Expandable.RealtimeProbeFrameSettings, true);
                    break;
            }
        }

        static void Drawer_TitleDefaultFrameSettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Styles.defaultFrameSettingsContent, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            selectedFrameSettings = (SelectedFrameSettings)EditorGUILayout.EnumPopup(selectedFrameSettings);
            if (EditorGUI.EndChangeCheck())
                ApplyChangedDisplayedFrameSettings(serialized, owner);
            GUILayout.EndHorizontal();
        }

        static void Drawer_SectionGeneral(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineResources, Styles.GeneralSection.renderPipelineResourcesContent);

            HDRenderPipeline hdrp = (RenderPipelineManager.currentPipeline as HDRenderPipeline);
            if (hdrp != null && hdrp.rayTracingSupported)
                EditorGUILayout.PropertyField(serialized.renderPipelineRayTracingResources, Styles.GeneralSection.renderPipelineRayTracingResourcesContent);

            // Not serialized as editor only datas... Retrieve them in data
            EditorGUI.showMixedValue = serialized.editorResourceHasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var editorResources = EditorGUILayout.ObjectField(Styles.GeneralSection.renderPipelineEditorResourcesContent, serialized.firstEditorResources, typeof(HDRenderPipelineEditorResources), allowSceneObjects: false) as HDRenderPipelineEditorResources;
            if (EditorGUI.EndChangeCheck())
                serialized.SetEditorResource(editorResources);
            EditorGUI.showMixedValue = false;

            //EditorGUILayout.PropertyField(serialized.enableSRPBatcher, k_SRPBatcher);
            EditorGUILayout.PropertyField(serialized.shaderVariantLogLevel, Styles.GeneralSection.shaderVariantLogLevel);
        }

        static void Drawer_SectionLightLayers(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportLightLayers, Styles.supportLightLayerContent);

            using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.supportLightLayers.boolValue))
            {
                ++EditorGUI.indentLevel;
                HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName0, serialized.renderPipelineSettings.lightLayerName0);
                HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName1, serialized.renderPipelineSettings.lightLayerName1);
                HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName2, serialized.renderPipelineSettings.lightLayerName2);
                HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName3, serialized.renderPipelineSettings.lightLayerName3);
                HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName4, serialized.renderPipelineSettings.lightLayerName4);
                HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName5, serialized.renderPipelineSettings.lightLayerName5);
                HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName6, serialized.renderPipelineSettings.lightLayerName6);
                HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName7, serialized.renderPipelineSettings.lightLayerName7);
                --EditorGUI.indentLevel;
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
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.pointCookieSize, Styles.pointCoockieSizeContent);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.lightLoopSettings.cubeCookieTexArraySize, Styles.pointCookieTextureArraySizeContent);
            if (EditorGUI.EndChangeCheck())
                serialized.renderPipelineSettings.lightLoopSettings.cubeCookieTexArraySize.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.cubeCookieTexArraySize.intValue, 1, TextureCache.k_MaxSupported);
            if (serialized.renderPipelineSettings.lightLoopSettings.cubeCookieTexArraySize.hasMultipleDifferentValues)
                EditorGUILayout.HelpBox(Styles.multipleDifferenteValueMessage, MessageType.Info);
            else
            {
                long currentCache = TextureCacheCubemap.GetApproxCacheSizeInByte(serialized.renderPipelineSettings.lightLoopSettings.cubeCookieTexArraySize.intValue, serialized.renderPipelineSettings.lightLoopSettings.pointCookieSize.intValue, 1);
                if (currentCache > HDRenderPipeline.k_MaxCacheSize)
                {
                    int reserved = TextureCacheCubemap.GetMaxCacheSizeForWeightInByte(HDRenderPipeline.k_MaxCacheSize, serialized.renderPipelineSettings.lightLoopSettings.pointCookieSize.intValue, 1);
                    string message = string.Format(Styles.cacheErrorFormat, HDEditorUtils.HumanizeWeight(currentCache), reserved);
                    EditorGUILayout.HelpBox(message, MessageType.Error);
                }
                else
                {
                    string message = string.Format(Styles.cacheInfoFormat, HDEditorUtils.HumanizeWeight(currentCache));
                    EditorGUILayout.HelpBox(message, MessageType.Info);
                }
            }
        }

        static void Drawer_SectionReflection(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportSSR, Styles.supportSSRContent);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.reflectionCacheCompressed, Styles.compressProbeCacheContent);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.reflectionCubemapSize, Styles.cubemapSizeContent);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.lightLoopSettings.reflectionProbeCacheSize, Styles.probeCacheSizeContent);
            if (EditorGUI.EndChangeCheck())
                serialized.renderPipelineSettings.lightLoopSettings.reflectionProbeCacheSize.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.reflectionProbeCacheSize.intValue, 1, TextureCache.k_MaxSupported);
            if (serialized.renderPipelineSettings.lightLoopSettings.reflectionProbeCacheSize.hasMultipleDifferentValues)
                EditorGUILayout.HelpBox(Styles.multipleDifferenteValueMessage, MessageType.Info);
            else
            {
                long currentCache = ReflectionProbeCache.GetApproxCacheSizeInByte(serialized.renderPipelineSettings.lightLoopSettings.reflectionProbeCacheSize.intValue, serialized.renderPipelineSettings.lightLoopSettings.reflectionCubemapSize.intValue, serialized.renderPipelineSettings.lightLoopSettings.supportFabricConvolution.boolValue ? 2 : 1);
                if (currentCache > HDRenderPipeline.k_MaxCacheSize)
                {
                    int reserved = ReflectionProbeCache.GetMaxCacheSizeForWeightInByte(HDRenderPipeline.k_MaxCacheSize, serialized.renderPipelineSettings.lightLoopSettings.reflectionCubemapSize.intValue, serialized.renderPipelineSettings.lightLoopSettings.supportFabricConvolution.boolValue ? 2 : 1);
                    string message = string.Format(Styles.cacheErrorFormat, HDEditorUtils.HumanizeWeight(currentCache), reserved);
                    EditorGUILayout.HelpBox(message, MessageType.Error);
                }
                else
                {
                    string message = string.Format(Styles.cacheInfoFormat, HDEditorUtils.HumanizeWeight(currentCache));
                    EditorGUILayout.HelpBox(message, MessageType.Info);
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.planarReflectionCacheCompressed, Styles.compressPlanarProbeCacheContent);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.planarReflectionAtlasSize, Styles.planarAtlasSizeContent);
            if (serialized.renderPipelineSettings.lightLoopSettings.planarReflectionAtlasSize.hasMultipleDifferentValues)
                EditorGUILayout.HelpBox(Styles.multipleDifferenteValueMessage, MessageType.Info);
            else
            {
                long currentCache = PlanarReflectionProbeCache.GetApproxCacheSizeInByte(1, serialized.renderPipelineSettings.lightLoopSettings.planarReflectionAtlasSize.intValue, GraphicsFormat.R16G16B16A16_UNorm);
                string message = string.Format(Styles.cacheInfoFormat, HDEditorUtils.HumanizeWeight(currentCache));
                EditorGUILayout.HelpBox(message, MessageType.Info);
            }
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.maxPlanarReflectionOnScreen, Styles.maxPlanarReflectionOnScreen);

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.lightLoopSettings.maxEnvLightsOnScreen, Styles.maxEnvContent);
            if (EditorGUI.EndChangeCheck())
               serialized.renderPipelineSettings.lightLoopSettings.maxEnvLightsOnScreen.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.maxEnvLightsOnScreen.intValue, 1, HDRenderPipeline.k_MaxEnvLightsOnScreen);
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

        static private bool m_ShowDirectionalLightSection = false;
        static private bool m_ShowPunctualLightSection = false;
        static private bool m_ShowAreaLightSection = false;

        static void Drawer_SectionShadows(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportShadowMask, Styles.supportShadowMaskContent);

            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.hdShadowInitParams.maxShadowRequests, Styles.maxRequestContent);

            if (!serialized.renderPipelineSettings.supportedLitShaderMode.hasMultipleDifferentValues)
            {
                bool isDeferredOnly = serialized.renderPipelineSettings.supportedLitShaderMode.intValue == (int)RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly;

                // Deferred Only mode does not allow to change filtering quality, but rather it is hardcoded.
                if (isDeferredOnly)
                    serialized.renderPipelineSettings.hdShadowInitParams.shadowFilteringQuality.intValue = (int)ShaderConfig.s_DeferredShadowFiltering;

                using (new EditorGUI.DisabledScope(isDeferredOnly))
                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.hdShadowInitParams.shadowFilteringQuality, Styles.filteringQuality);
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

            m_ShowDirectionalLightSection = EditorGUILayout.Foldout(m_ShowDirectionalLightSection, Styles.directionalShadowsSubTitle, true);
            if (m_ShowDirectionalLightSection)
            {
                ++EditorGUI.indentLevel;
                    EditorGUILayout.IntPopup(serialized.renderPipelineSettings.hdShadowInitParams.directionalShadowMapDepthBits, Styles.shadowBitDepthNames, Styles.shadowBitDepthValues, Styles.directionalShadowPrecisionContent);
                serialized.renderPipelineSettings.hdShadowInitParams.shadowResolutionDirectional.ValueGUI<int>(Styles.directionalLightsShadowTiers);
                EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.hdShadowInitParams.maxDirectionalShadowMapResolution, Styles.maxShadowResolution);
                --EditorGUI.indentLevel;
            }

            m_ShowPunctualLightSection = EditorGUILayout.Foldout(m_ShowPunctualLightSection, Styles.punctualShadowsSubTitle, true);
            if (m_ShowPunctualLightSection)
            {
                ++EditorGUI.indentLevel;
                {
                    EditorGUILayout.LabelField(Styles.shadowPunctualLightAtlasSubTitle);
                    ++EditorGUI.indentLevel;
                    {
                        CoreEditorUtils.DrawEnumPopup(serialized.renderPipelineSettings.hdShadowInitParams.serializedPunctualAtlasInit.shadowMapResolution, typeof(ShadowResolutionValue), Styles.resolutionContent);
                        EditorGUILayout.IntPopup(serialized.renderPipelineSettings.hdShadowInitParams.serializedPunctualAtlasInit.shadowMapDepthBits, Styles.shadowBitDepthNames, Styles.shadowBitDepthValues, Styles.precisionContent);
                        EditorGUILayout.PropertyField(serialized.renderPipelineSettings.hdShadowInitParams.serializedPunctualAtlasInit.useDynamicViewportRescale, Styles.dynamicRescaleContent);
                    }
                    --EditorGUI.indentLevel;
                }
                --EditorGUI.indentLevel;

                ++EditorGUI.indentLevel;
                serialized.renderPipelineSettings.hdShadowInitParams.shadowResolutionPunctual.ValueGUI<int>(Styles.punctualLightsShadowTiers);
                EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.hdShadowInitParams.maxPunctualShadowMapResolution, Styles.maxShadowResolution);
                --EditorGUI.indentLevel;
            }

            m_ShowAreaLightSection = EditorGUILayout.Foldout(m_ShowAreaLightSection, Styles.areaShadowsSubTitle, true);
            if (m_ShowAreaLightSection)
            {
                ++EditorGUI.indentLevel;
                {
                    EditorGUILayout.LabelField(Styles.shadowAreaLightAtlasSubTitle);
                    ++EditorGUI.indentLevel;
                    {
                        CoreEditorUtils.DrawEnumPopup(serialized.renderPipelineSettings.hdShadowInitParams.serializedAreaAtlasInit.shadowMapResolution, typeof(ShadowResolutionValue), Styles.resolutionContent);
                        EditorGUILayout.IntPopup(serialized.renderPipelineSettings.hdShadowInitParams.serializedAreaAtlasInit.shadowMapDepthBits, Styles.shadowBitDepthNames, Styles.shadowBitDepthValues, Styles.precisionContent);
                        EditorGUILayout.PropertyField(serialized.renderPipelineSettings.hdShadowInitParams.serializedAreaAtlasInit.useDynamicViewportRescale, Styles.dynamicRescaleContent);
                    }
                    --EditorGUI.indentLevel;
                }
                --EditorGUI.indentLevel;

                ++EditorGUI.indentLevel;
                serialized.renderPipelineSettings.hdShadowInitParams.shadowResolutionArea.ValueGUI<int>(Styles.areaLightsShadowTiers);
                EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.hdShadowInitParams.maxAreaShadowMapResolution, Styles.maxShadowResolution);
                --EditorGUI.indentLevel;
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

                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.decalSettings.perChannelMask, Styles.metalAndAOContent);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.lightLoopSettings.maxDecalsOnScreen, Styles.maxDecalContent);
                if (EditorGUI.EndChangeCheck())
                    serialized.renderPipelineSettings.lightLoopSettings.maxDecalsOnScreen.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.maxDecalsOnScreen.intValue, 1, HDRenderPipeline.k_MaxDecalsOnScreen);
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
        }

        static void Drawer_SectionDynamicResolutionSettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.enabled, Styles.enabled);

            ++EditorGUI.indentLevel;
            using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.dynamicResolutionSettings.enabled.boolValue))
            {
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.dynamicResType, Styles.dynResType);
                if (serialized.renderPipelineSettings.dynamicResolutionSettings.dynamicResType.hasMultipleDifferentValues)
                {
                    using (new EditorGUI.DisabledGroupScope(true))
                        EditorGUILayout.LabelField(Styles.multipleDifferenteValueMessage);
                }
                else
                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.softwareUpsamplingFilter, Styles.upsampleFilter);

                if (!serialized.renderPipelineSettings.dynamicResolutionSettings.forcePercentage.hasMultipleDifferentValues
                    && !serialized.renderPipelineSettings.dynamicResolutionSettings.forcePercentage.boolValue)
                {
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
                        serialized.renderPipelineSettings.dynamicResolutionSettings.maxPercentage.floatValue = Mathf.Clamp(maxPercentage, 0.0f, 100.0f);

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
            }
            --EditorGUI.indentLevel;
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
        }

        static private bool m_ShowDoFLowQualitySection = false;
        static private bool m_ShowDoFMediumQualitySection = false;
        static private bool m_ShowDoFHighQualitySection = false;

        static void DrawDepthOfFieldQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            ++EditorGUI.indentLevel;
            {
                EditorGUILayout.LabelField(Styles.nearBlurSubTitle);
                ++EditorGUI.indentLevel;
                {
                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.NearBlurSampleCount.GetArrayElementAtIndex(tier), Styles.sampleCountQuality);
                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.NearBlurMaxRadius.GetArrayElementAtIndex(tier), Styles.maxRadiusQuality);
                }
                --EditorGUI.indentLevel;
                EditorGUILayout.LabelField(Styles.farBlurSubTitle);
                ++EditorGUI.indentLevel;
                {
                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.FarBlurSampleCount.GetArrayElementAtIndex(tier), Styles.sampleCountQuality);
                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.FarBlurMaxRadius.GetArrayElementAtIndex(tier), Styles.maxRadiusQuality);
                }
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.DoFResolution.GetArrayElementAtIndex(tier), Styles.resolutionQuality);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.DoFHighFilteringQuality.GetArrayElementAtIndex(tier), Styles.highQualityFiltering);
            --EditorGUI.indentLevel;
        }

        static void Drawer_SectionDepthOfFieldQualitySettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            m_ShowDoFLowQualitySection = EditorGUILayout.Foldout(m_ShowDoFLowQualitySection, Styles.lowQualityContent, true);
            if (m_ShowDoFLowQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Low;
                DrawDepthOfFieldQualitySetting(serialized, quality);
            }

            m_ShowDoFMediumQualitySection = EditorGUILayout.Foldout(m_ShowDoFMediumQualitySection, Styles.mediumQualityContent, true);
            if (m_ShowDoFMediumQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Medium;
                DrawDepthOfFieldQualitySetting(serialized, quality);
            }

            m_ShowDoFHighQualitySection = EditorGUILayout.Foldout(m_ShowDoFHighQualitySection, Styles.highQualityContent, true);
            if (m_ShowDoFHighQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.High;
                DrawDepthOfFieldQualitySetting(serialized, quality);
            }
        }

        static private bool m_ShowMotionBlurLowQualitySection = false;
        static private bool m_ShowMotionBlurMediumQualitySection = false;
        static private bool m_ShowMotionBlurHighQualitySection = false;

        static void Drawer_SectionMotionBlurQualitySettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            m_ShowMotionBlurLowQualitySection = EditorGUILayout.Foldout(m_ShowMotionBlurLowQualitySection, Styles.lowQualityContent, true);
            if (m_ShowMotionBlurLowQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Low;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.MotionBlurSampleCount.GetArrayElementAtIndex(quality), Styles.sampleCountQuality);
            }
            m_ShowMotionBlurMediumQualitySection = EditorGUILayout.Foldout(m_ShowMotionBlurMediumQualitySection, Styles.mediumQualityContent, true);
            if (m_ShowMotionBlurMediumQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Medium;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.MotionBlurSampleCount.GetArrayElementAtIndex(quality), Styles.sampleCountQuality);
            }
            m_ShowMotionBlurHighQualitySection = EditorGUILayout.Foldout(m_ShowMotionBlurHighQualitySection, Styles.highQualityContent, true);
            if (m_ShowMotionBlurHighQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.High;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.MotionBlurSampleCount.GetArrayElementAtIndex(quality), Styles.sampleCountQuality);
            }
        }

        static private bool m_ShowBloomLowQualitySection = false;
        static private bool m_ShowBloomMediumQualitySection = false;
        static private bool m_ShowBloomHighQualitySection = false;

        static void DrawBloomQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.BloomRes.GetArrayElementAtIndex(tier), Styles.resolutionQuality);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.BloomHighFilteringQuality.GetArrayElementAtIndex(tier), Styles.highQualityFiltering);
        }

        static void Drawer_SectionBloomQualitySettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            m_ShowBloomLowQualitySection = EditorGUILayout.Foldout(m_ShowBloomLowQualitySection, Styles.lowQualityContent, true);
            if (m_ShowBloomLowQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Low;
                DrawBloomQualitySetting(serialized, quality);
            }
            m_ShowBloomMediumQualitySection = EditorGUILayout.Foldout(m_ShowBloomMediumQualitySection, Styles.mediumQualityContent, true);
            if (m_ShowBloomMediumQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Medium;
                DrawBloomQualitySetting(serialized, quality);
            }
            m_ShowBloomHighQualitySection = EditorGUILayout.Foldout(m_ShowBloomHighQualitySection, Styles.highQualityContent, true);
            if (m_ShowBloomHighQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.High;
                DrawBloomQualitySetting(serialized, quality);
            }
        }

        static private bool m_ShowChromaticAberrationLowQualitySection = false;
        static private bool m_ShowChromaticAberrationMediumQualitySection = false;
        static private bool m_ShowChromaticAberrationHighQualitySection = false;

        static void Drawer_SectionChromaticAberrationQualitySettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            m_ShowChromaticAberrationLowQualitySection = EditorGUILayout.Foldout(m_ShowChromaticAberrationLowQualitySection, Styles.lowQualityContent, true);
            if (m_ShowChromaticAberrationLowQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Low;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.ChromaticAbMaxSamples.GetArrayElementAtIndex(quality), Styles.maxSamplesQuality);
            }
            m_ShowChromaticAberrationMediumQualitySection = EditorGUILayout.Foldout(m_ShowChromaticAberrationMediumQualitySection, Styles.mediumQualityContent, true);
            if (m_ShowChromaticAberrationMediumQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Medium;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.ChromaticAbMaxSamples.GetArrayElementAtIndex(quality), Styles.maxSamplesQuality);
            }
            m_ShowChromaticAberrationHighQualitySection = EditorGUILayout.Foldout(m_ShowChromaticAberrationHighQualitySection, Styles.highQualityContent, true);
            if (m_ShowChromaticAberrationHighQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.High;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.ChromaticAbMaxSamples.GetArrayElementAtIndex(quality), Styles.maxSamplesQuality);
            }
        }

        static private bool m_ShowAOLowQualitySection = false;
        static private bool m_ShowAOMediumQualitySection = false;
        static private bool m_ShowAOHighQualitySection = false;

        static void DrawAOQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.AOStepCount.GetArrayElementAtIndex(tier), Styles.AOStepCount);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.AOFullRes.GetArrayElementAtIndex(tier), Styles.AOFullRes);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.AOMaximumRadiusPixels.GetArrayElementAtIndex(tier), Styles.AOMaxRadiusInPixels);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.AODirectionCount.GetArrayElementAtIndex(tier), Styles.AODirectionCount);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.AOBilateralUpsample.GetArrayElementAtIndex(tier), Styles.AOBilateralUpsample);
        }

        static void Drawer_SectionSSAOQualitySettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            m_ShowAOLowQualitySection = EditorGUILayout.Foldout(m_ShowAOLowQualitySection, Styles.lowQualityContent);
            if (m_ShowAOLowQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Low;
                DrawAOQualitySetting(serialized, quality);
            }

            m_ShowAOMediumQualitySection = EditorGUILayout.Foldout(m_ShowAOMediumQualitySection, Styles.mediumQualityContent);
            if (m_ShowAOMediumQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Medium;
                DrawAOQualitySetting(serialized, quality);
            }

            m_ShowAOHighQualitySection = EditorGUILayout.Foldout(m_ShowAOHighQualitySection, Styles.highQualityContent);
            if (m_ShowAOHighQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.High;
                DrawAOQualitySetting(serialized, quality);
            }
        }

        static private bool m_ShowContactShadowLowQualitySection = false;
        static private bool m_ShowContactShadowMediumQualitySection = false;
        static private bool m_ShowContactShadowHighQualitySection = false;

        static void Drawer_SectionContactShadowQualitySettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            m_ShowContactShadowLowQualitySection = EditorGUILayout.Foldout(m_ShowContactShadowLowQualitySection, Styles.lowQualityContent);
            if (m_ShowContactShadowLowQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Low;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.ContactShadowSampleCount.GetArrayElementAtIndex(quality), Styles.contactShadowsSampleCount);
            }

            m_ShowContactShadowMediumQualitySection = EditorGUILayout.Foldout(m_ShowContactShadowMediumQualitySection, Styles.mediumQualityContent);
            if (m_ShowContactShadowMediumQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Medium;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.ContactShadowSampleCount.GetArrayElementAtIndex(quality), Styles.contactShadowsSampleCount);
            }

            m_ShowContactShadowHighQualitySection = EditorGUILayout.Foldout(m_ShowContactShadowHighQualitySection, Styles.highQualityContent);
            if (m_ShowContactShadowHighQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.High;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.ContactShadowSampleCount.GetArrayElementAtIndex(quality), Styles.contactShadowsSampleCount);
            }
        }

        static private bool m_ShowSSRLowQualitySection = false;
        static private bool m_ShowSSRMediumQualitySection = false;
        static private bool m_ShowSSRHighQualitySection = false;

        static void Drawer_SectionSSRQualitySettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            m_ShowSSRLowQualitySection = EditorGUILayout.Foldout(m_ShowSSRLowQualitySection, Styles.lowQualityContent);
            if (m_ShowSSRLowQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Low;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.SSRMaxRaySteps.GetArrayElementAtIndex(quality), Styles.contactShadowsSampleCount);
            }

            m_ShowSSRMediumQualitySection = EditorGUILayout.Foldout(m_ShowSSRMediumQualitySection, Styles.mediumQualityContent);
            if (m_ShowSSRMediumQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Medium;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.SSRMaxRaySteps.GetArrayElementAtIndex(quality), Styles.contactShadowsSampleCount);
            }

            m_ShowSSRHighQualitySection = EditorGUILayout.Foldout(m_ShowSSRHighQualitySection, Styles.highQualityContent);
            if (m_ShowSSRHighQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.High;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.SSRMaxRaySteps.GetArrayElementAtIndex(quality), Styles.contactShadowsSampleCount);
            }
        }

        static void Drawer_SectionRenderingUnsorted(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.colorBufferFormat, Styles.colorBufferFormatContent);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportedLitShaderMode, Styles.supportLitShaderModeContent);

            // MSAA is an option that is only available in full forward but Camera can be set in Full Forward only. Thus MSAA have no dependency currently
            //Note: do not use SerializedProperty.enumValueIndex here as this enum not start at 0 as it is used as flags.
            bool msaaAllowed = true;
            for (int index = 0; index < serialized.serializedObject.targetObjects.Length && msaaAllowed; ++index)
            {
                var litShaderMode = (serialized.serializedObject.targetObjects[index] as HDRenderPipelineAsset).currentPlatformRenderPipelineSettings.supportedLitShaderMode;
                msaaAllowed &= litShaderMode == SupportedLitShaderMode.ForwardOnly || litShaderMode == SupportedLitShaderMode.Both;
            }
            using (new EditorGUI.DisabledScope(!msaaAllowed))
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.MSAASampleCount, Styles.MSAASampleCountContent);
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportMotionVectors, Styles.supportMotionVectorContent);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportRuntimeDebugDisplay, Styles.supportRuntimeDebugDisplayContent);
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

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportRayTracing, Styles.supportRaytracing);
            using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.supportRayTracing.boolValue))
            {
                if (serialized.renderPipelineSettings.supportRayTracing.boolValue && !UnityEngine.SystemInfo.supportsRayTracing)
                {
                    EditorGUILayout.HelpBox(Styles.rayTracingUnsupportedWarning.text, MessageType.Warning, wide: true);
                }
            }

            serialized.renderPipelineSettings.lodBias.ValueGUI<float>(Styles.LODBias);
            serialized.renderPipelineSettings.maximumLODLevel.ValueGUI<int>(Styles.maximumLODLevel);

            EditorGUILayout.Space(); //to separate with following sub sections
        }

        static void Drawer_SectionLightingUnsorted(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportSSAO, Styles.supportSSAOContent);

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportVolumetrics, Styles.supportVolumetricContent);
            using (new EditorGUI.DisabledScope(serialized.renderPipelineSettings.supportVolumetrics.hasMultipleDifferentValues
                || !serialized.renderPipelineSettings.supportVolumetrics.boolValue))
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.increaseResolutionOfVolumetrics, Styles.volumetricResolutionContent);
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.Space(); //to separate with following sub sections
        }

        static void Drawer_SectionMaterialUnsorted(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.availableMaterialQualityLevels);
            var v = EditorGUILayout.EnumPopup(Styles.materialQualityLevelContent, (MaterialQuality) serialized.defaultMaterialQualityLevel.intValue);
            serialized.defaultMaterialQualityLevel.intValue = (int)(object)v;

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportDistortion, Styles.supportDistortion);

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportSubsurfaceScattering, Styles.supportedSSSContent);
            using (new EditorGUI.DisabledScope(serialized.renderPipelineSettings.supportSubsurfaceScattering.hasMultipleDifferentValues
                || !serialized.renderPipelineSettings.supportSubsurfaceScattering.boolValue))
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.increaseSssSampleCount, Styles.SSSSampleCountContent);
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.supportFabricConvolution, Styles.supportFabricBSDFConvolutionContent);

            diffusionProfileUI.drawElement = DrawDiffusionProfileElement;
            diffusionProfileUI.OnGUI(serialized.diffusionProfileSettingsList);
        }

        static void DrawDiffusionProfileElement(SerializedProperty element, Rect rect, int index)
        {
            EditorGUI.ObjectField(rect, element, EditorGUIUtility.TrTextContent("Profile " + index));
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
            AppendSupport(builder, serialized.renderPipelineSettings.supportVolumetrics, Styles.supportVolumetricContent);

            if (serialized.renderPipelineSettings.supportLightLayers.hasMultipleDifferentValues)
                builder.AppendLine().AppendFormat(supportedFormaterMultipleValue, Styles.supportLightLayerContent.text);
            else if (serialized.renderPipelineSettings.supportLightLayers.boolValue)
                builder.AppendLine().AppendFormat(supportedFormater, Styles.supportLightLayerContent.text, Styles.supportLightLayerDrawbacks[supportedLitShaderMode]);

            if (serialized.renderPipelineSettings.MSAASampleCount.hasMultipleDifferentValues)
                builder.AppendLine().AppendFormat(supportedFormaterMultipleValue, Styles.MSAASampleCountContent.text);
            else if (serialized.renderPipelineSettings.supportMSAA)
            {
                // NO MSAA in deferred
                if (serialized.renderPipelineSettings.supportedLitShaderMode.intValue != (int)RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly)
                    builder.AppendLine().AppendFormat(supportedFormater, "Multisample Anti-aliasing", Styles.supportDrawbacks[Styles.MSAASampleCountContent]);
            }

            if (serialized.renderPipelineSettings.supportDecals.hasMultipleDifferentValues)
                builder.AppendLine().AppendFormat(supportedFormaterMultipleValue, Styles.decalsSubTitle.text);
            else if (serialized.renderPipelineSettings.supportDecals.boolValue)
                builder.AppendLine().AppendFormat(supportedFormater, Styles.decalsSubTitle.text, Styles.supportDrawbacks[Styles.supportDecalContent]);

            if (serialized.renderPipelineSettings.decalSettings.perChannelMask.hasMultipleDifferentValues)
                builder.AppendLine().AppendFormat(supportedFormaterMultipleValue, Styles.decalsMetalAndAOSubTitle.text);
            else if (serialized.renderPipelineSettings.supportDecals.boolValue && serialized.renderPipelineSettings.decalSettings.perChannelMask.boolValue)
                builder.AppendLine().AppendFormat(supportedFormater, Styles.decalsMetalAndAOSubTitle.text, Styles.supportDrawbacks[Styles.metalAndAOContent]);

            AppendSupport(builder, serialized.renderPipelineSettings.supportMotionVectors, Styles.supportMotionVectorContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportRuntimeDebugDisplay, Styles.supportRuntimeDebugDisplayContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportDitheringCrossFade, Styles.supportDitheringCrossFadeContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportTerrainHole, Styles.supportTerrainHoleContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportDistortion, Styles.supportDistortion);
            AppendSupport(builder, serialized.renderPipelineSettings.supportTransparentBackface, Styles.supportTransparentBackface);
            AppendSupport(builder, serialized.renderPipelineSettings.supportTransparentDepthPrepass, Styles.supportTransparentDepthPrepass);
            AppendSupport(builder, serialized.renderPipelineSettings.supportTransparentDepthPostpass, Styles.supportTransparentDepthPostpass);
            AppendSupport(builder, serialized.renderPipelineSettings.supportRayTracing, Styles.supportRaytracing);

            EditorGUILayout.HelpBox(builder.ToString(), MessageType.Info, wide: true);
        }
    }
}
