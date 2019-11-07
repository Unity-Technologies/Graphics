using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Text;
using Utilities;
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
            LightLayer = 1 << 22
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
                CED.FoldoutGroup(k_RenderingSectionTitle, Expandable.Rendering, k_ExpandedState,
                    CED.Group(GroupOption.Indent, Drawer_SectionRenderingUnsorted),
                    CED.FoldoutGroup(k_DecalsSubTitle, Expandable.Decal, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionDecalSettings),
                    CED.FoldoutGroup(k_DynamicResolutionSubTitle, Expandable.DynamicResolution, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionDynamicResolutionSettings),
                    CED.FoldoutGroup(k_LowResTransparencySubTitle, Expandable.LowResTransparency, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionLowResTransparentSettings)
                    ),
                CED.FoldoutGroup(k_LightingSectionTitle, Expandable.Lighting, k_ExpandedState,
                    CED.Group(GroupOption.Indent, Drawer_SectionLightingUnsorted),
                    CED.FoldoutGroup(k_LightLayerSubTitle, Expandable.LightLayer, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionLightLayers),
                    CED.FoldoutGroup(k_CookiesSubTitle, Expandable.Cookie, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionCookies),
                    CED.FoldoutGroup(k_ReflectionsSubTitle, Expandable.Reflection, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionReflection),
                    CED.FoldoutGroup(k_SkySubTitle, Expandable.Sky, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionSky),
                    CED.FoldoutGroup(k_ShadowSubTitle, Expandable.Shadow, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionShadows),
                    CED.FoldoutGroup(k_LightLoopSubTitle, Expandable.LightLoop, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionLightLoop)
                    ),
                CED.FoldoutGroup(k_MaterialSectionTitle, Expandable.Material, k_ExpandedState, Drawer_SectionMaterialUnsorted),
                CED.FoldoutGroup(k_PostProcessSectionTitle, Expandable.PostProcess, k_ExpandedState, Drawer_SectionPostProcessSettings),
                CED.FoldoutGroup(k_PostProcessQualitySubTitle, Expandable.PostProcessQuality, k_ExpandedState, 
                    CED.FoldoutGroup(k_DepthOfFieldQualitySettings, Expandable.DepthOfFieldQuality, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionDepthOfFieldQualitySettings),
                    CED.FoldoutGroup(k_MotionBlurQualitySettings, Expandable.MotionBlurQuality, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionMotionBlurQualitySettings),
                    CED.FoldoutGroup(k_BloomQualitySettings, Expandable.BloomQuality, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionBloomQualitySettings),
                    CED.FoldoutGroup(k_ChromaticAberrationQualitySettings, Expandable.ChromaticAberrationQuality, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionChromaticAberrationQualitySettings)
                    ),
                CED.FoldoutGroup(k_XrTitle, Expandable.XR, k_ExpandedState, Drawer_SectionXRSettings)
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
            EditorGUILayout.LabelField(k_DefaultFrameSettingsContent, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            selectedFrameSettings = (SelectedFrameSettings)EditorGUILayout.EnumPopup(selectedFrameSettings);
            if (EditorGUI.EndChangeCheck())
                ApplyChangedDisplayedFrameSettings(serialized, owner);
            GUILayout.EndHorizontal();
        }

        static void Drawer_SectionGeneral(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineResources, k_RenderPipelineResourcesContent);

            HDRenderPipeline hdrp = (RenderPipelineManager.currentPipeline as HDRenderPipeline);
            if (hdrp != null && hdrp.rayTracingSupported)
                EditorGUILayout.PropertyField(serialized.renderPipelineRayTracingResources, k_RenderPipelineRayTracingResourcesContent);

            // Not serialized as editor only datas... Retrieve them in data
            EditorGUI.showMixedValue = serialized.editorResourceHasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var editorResources = EditorGUILayout.ObjectField(k_RenderPipelineEditorResourcesContent, serialized.firstEditorResources, typeof(HDRenderPipelineEditorResources), allowSceneObjects: false) as HDRenderPipelineEditorResources;
            if (EditorGUI.EndChangeCheck())
                serialized.SetEditorResource(editorResources);
            EditorGUI.showMixedValue = false;

            //EditorGUILayout.PropertyField(serialized.enableSRPBatcher, k_SRPBatcher);
            EditorGUILayout.PropertyField(serialized.shaderVariantLogLevel, k_ShaderVariantLogLevel);
        }

        static void Drawer_SectionLightLayers(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportLightLayers, k_SupportLightLayerContent);

            using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.supportLightLayers.boolValue))
            {
                ++EditorGUI.indentLevel;
                HDEditorUtils.DrawDelayedTextField(k_LightLayerName0, serialized.renderPipelineSettings.lightLayerName0);
                HDEditorUtils.DrawDelayedTextField(k_LightLayerName1, serialized.renderPipelineSettings.lightLayerName1);
                HDEditorUtils.DrawDelayedTextField(k_LightLayerName2, serialized.renderPipelineSettings.lightLayerName2);
                HDEditorUtils.DrawDelayedTextField(k_LightLayerName3, serialized.renderPipelineSettings.lightLayerName3);
                HDEditorUtils.DrawDelayedTextField(k_LightLayerName4, serialized.renderPipelineSettings.lightLayerName4);
                HDEditorUtils.DrawDelayedTextField(k_LightLayerName5, serialized.renderPipelineSettings.lightLayerName5);
                HDEditorUtils.DrawDelayedTextField(k_LightLayerName6, serialized.renderPipelineSettings.lightLayerName6);
                HDEditorUtils.DrawDelayedTextField(k_LightLayerName7, serialized.renderPipelineSettings.lightLayerName7);
                --EditorGUI.indentLevel;
            }
        }

        static void Drawer_SectionCookies(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.cookieSize, k_CoockieSizeContent);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.lightLoopSettings.cookieTexArraySize, k_CookieTextureArraySizeContent);
            if (EditorGUI.EndChangeCheck())
                serialized.renderPipelineSettings.lightLoopSettings.cookieTexArraySize.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.cookieTexArraySize.intValue, 1, TextureCache.k_MaxSupported);
            if (serialized.renderPipelineSettings.lightLoopSettings.cookieTexArraySize.hasMultipleDifferentValues)
                EditorGUILayout.HelpBox(k_MultipleDifferenteValueMessage, MessageType.Info);
            else
            {
                long currentCache = TextureCache2D.GetApproxCacheSizeInByte(serialized.renderPipelineSettings.lightLoopSettings.cookieTexArraySize.intValue, serialized.renderPipelineSettings.lightLoopSettings.cookieSize.intValue, 1);
                if (currentCache > HDRenderPipeline.k_MaxCacheSize)
                {
                    int reserved = TextureCache2D.GetMaxCacheSizeForWeightInByte(HDRenderPipeline.k_MaxCacheSize, serialized.renderPipelineSettings.lightLoopSettings.cookieSize.intValue, 1);
                    string message = string.Format(k_CacheErrorFormat, HDEditorUtils.HumanizeWeight(currentCache), reserved);
                    EditorGUILayout.HelpBox(message, MessageType.Error);
                }
                else
                {
                    string message = string.Format(k_CacheInfoFormat, HDEditorUtils.HumanizeWeight(currentCache));
                    EditorGUILayout.HelpBox(message, MessageType.Info);
                }
            }
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.pointCookieSize, k_PointCoockieSizeContent);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.lightLoopSettings.cubeCookieTexArraySize, k_PointCookieTextureArraySizeContent);
            if (EditorGUI.EndChangeCheck())
                serialized.renderPipelineSettings.lightLoopSettings.cubeCookieTexArraySize.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.cubeCookieTexArraySize.intValue, 1, TextureCache.k_MaxSupported);
            if (serialized.renderPipelineSettings.lightLoopSettings.cubeCookieTexArraySize.hasMultipleDifferentValues)
                EditorGUILayout.HelpBox(k_MultipleDifferenteValueMessage, MessageType.Info);
            else
            {
                long currentCache = TextureCacheCubemap.GetApproxCacheSizeInByte(serialized.renderPipelineSettings.lightLoopSettings.cubeCookieTexArraySize.intValue, serialized.renderPipelineSettings.lightLoopSettings.pointCookieSize.intValue, 1);
                if (currentCache > HDRenderPipeline.k_MaxCacheSize)
                {
                    int reserved = TextureCacheCubemap.GetMaxCacheSizeForWeightInByte(HDRenderPipeline.k_MaxCacheSize, serialized.renderPipelineSettings.lightLoopSettings.pointCookieSize.intValue, 1);
                    string message = string.Format(k_CacheErrorFormat, HDEditorUtils.HumanizeWeight(currentCache), reserved);
                    EditorGUILayout.HelpBox(message, MessageType.Error);
                }
                else
                {
                    string message = string.Format(k_CacheInfoFormat, HDEditorUtils.HumanizeWeight(currentCache));
                    EditorGUILayout.HelpBox(message, MessageType.Info);
                }
            }
        }

        static void Drawer_SectionReflection(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportSSR, k_SupportSSRContent);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.reflectionCacheCompressed, k_CompressProbeCacheContent);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.reflectionCubemapSize, k_CubemapSizeContent);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.lightLoopSettings.reflectionProbeCacheSize, k_ProbeCacheSizeContent);
            if (EditorGUI.EndChangeCheck())
                serialized.renderPipelineSettings.lightLoopSettings.reflectionProbeCacheSize.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.reflectionProbeCacheSize.intValue, 1, TextureCache.k_MaxSupported);
            if (serialized.renderPipelineSettings.lightLoopSettings.reflectionProbeCacheSize.hasMultipleDifferentValues)
                EditorGUILayout.HelpBox(k_MultipleDifferenteValueMessage, MessageType.Info);
            else
            {
                long currentCache = ReflectionProbeCache.GetApproxCacheSizeInByte(serialized.renderPipelineSettings.lightLoopSettings.reflectionProbeCacheSize.intValue, serialized.renderPipelineSettings.lightLoopSettings.reflectionCubemapSize.intValue, serialized.renderPipelineSettings.lightLoopSettings.supportFabricConvolution.boolValue ? 2 : 1);
                if (currentCache > HDRenderPipeline.k_MaxCacheSize)
                {
                    int reserved = ReflectionProbeCache.GetMaxCacheSizeForWeightInByte(HDRenderPipeline.k_MaxCacheSize, serialized.renderPipelineSettings.lightLoopSettings.reflectionCubemapSize.intValue, serialized.renderPipelineSettings.lightLoopSettings.supportFabricConvolution.boolValue ? 2 : 1);
                    string message = string.Format(k_CacheErrorFormat, HDEditorUtils.HumanizeWeight(currentCache), reserved);
                    EditorGUILayout.HelpBox(message, MessageType.Error);
                }
                else
                {
                    string message = string.Format(k_CacheInfoFormat, HDEditorUtils.HumanizeWeight(currentCache));
                    EditorGUILayout.HelpBox(message, MessageType.Info);
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.planarReflectionCacheCompressed, k_CompressPlanarProbeCacheContent);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.planarReflectionCubemapSize, k_PlanarTextureSizeContent);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.lightLoopSettings.planarReflectionProbeCacheSize, k_PlanarProbeCacheSizeContent);
            if (EditorGUI.EndChangeCheck())
                serialized.renderPipelineSettings.lightLoopSettings.planarReflectionProbeCacheSize.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.planarReflectionProbeCacheSize.intValue, 1, TextureCache.k_MaxSupported);
            if (serialized.renderPipelineSettings.lightLoopSettings.planarReflectionProbeCacheSize.hasMultipleDifferentValues)
                EditorGUILayout.HelpBox(k_MultipleDifferenteValueMessage, MessageType.Info);
            else
            {
                long currentCache = PlanarReflectionProbeCache.GetApproxCacheSizeInByte(serialized.renderPipelineSettings.lightLoopSettings.planarReflectionProbeCacheSize.intValue, serialized.renderPipelineSettings.lightLoopSettings.planarReflectionCubemapSize.intValue, 1);
                if (currentCache > HDRenderPipeline.k_MaxCacheSize)
                {
                    int reserved = PlanarReflectionProbeCache.GetMaxCacheSizeForWeightInByte(HDRenderPipeline.k_MaxCacheSize, serialized.renderPipelineSettings.lightLoopSettings.planarReflectionCubemapSize.intValue, 1);
                    string message = string.Format(k_CacheErrorFormat, HDEditorUtils.HumanizeWeight(currentCache), reserved);
                    EditorGUILayout.HelpBox(message, MessageType.Error);
                }
                else
                {
                    string message = string.Format(k_CacheInfoFormat, HDEditorUtils.HumanizeWeight(currentCache));
                    EditorGUILayout.HelpBox(message, MessageType.Info);
                }
            }

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.lightLoopSettings.maxEnvLightsOnScreen, k_MaxEnvContent);
            if (EditorGUI.EndChangeCheck())
               serialized.renderPipelineSettings.lightLoopSettings.maxEnvLightsOnScreen.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.maxEnvLightsOnScreen.intValue, 1, HDRenderPipeline.k_MaxEnvLightsOnScreen);
        }

        static void Drawer_SectionSky(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.skyReflectionSize, k_SkyReflectionSizeContent);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.skyLightingOverrideLayerMask, k_SkyLightingOverrideMaskContent);
            if (!serialized.renderPipelineSettings.lightLoopSettings.skyLightingOverrideLayerMask.hasMultipleDifferentValues
                && serialized.renderPipelineSettings.lightLoopSettings.skyLightingOverrideLayerMask.intValue == -1)
            {
                EditorGUILayout.HelpBox(k_SkyLightingHelpBoxContent, MessageType.Warning);
            }
        }

        static private bool m_ShowDirectionalLightSection = false;
        static private bool m_ShowPunctualLightSection = false;
        static private bool m_ShowAreaLightSection = false;

        static void Drawer_SectionShadows(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportShadowMask, k_SupportShadowMaskContent);

            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.hdShadowInitParams.maxShadowRequests, k_MaxRequestContent);

            if (!serialized.renderPipelineSettings.supportedLitShaderMode.hasMultipleDifferentValues)
            {
                bool isDeferredOnly = serialized.renderPipelineSettings.supportedLitShaderMode.intValue == (int)RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly;

                // Deferred Only mode does not allow to change filtering quality, but rather it is hardcoded.
                if (isDeferredOnly)
                    serialized.renderPipelineSettings.hdShadowInitParams.shadowFilteringQuality.intValue = (int)ShaderConfig.s_DeferredShadowFiltering;

                using (new EditorGUI.DisabledScope(isDeferredOnly))
                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.hdShadowInitParams.shadowFilteringQuality, k_FilteringQuality);
            }
            else
            {
                using (new EditorGUI.DisabledGroupScope(true))
                    EditorGUILayout.LabelField(k_MultipleDifferenteValueMessage);
            }

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.hdShadowInitParams.supportScreenSpaceShadows, k_SupportScreenSpaceShadows);
            ++EditorGUI.indentLevel;
            using (new EditorGUI.DisabledGroupScope(!serialized.renderPipelineSettings.hdShadowInitParams.supportScreenSpaceShadows.boolValue))
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadows, k_MaxScreenSpaceShadows);
            --EditorGUI.indentLevel;

            SerializedScalableSettingUI.ValueGUI<bool>(serialized.renderPipelineSettings.lightSettings.useContactShadows, k_UseContactShadows);

            m_ShowDirectionalLightSection = EditorGUILayout.Foldout(m_ShowDirectionalLightSection, k_DirectionalShadowsSubTitle, true);
            if (m_ShowDirectionalLightSection)
            {
                ++EditorGUI.indentLevel;
                    EditorGUILayout.IntPopup(serialized.renderPipelineSettings.hdShadowInitParams.directionalShadowMapDepthBits, k_ShadowBitDepthNames, k_ShadowBitDepthValues, k_DirectionalShadowPrecisionContent);
                serialized.renderPipelineSettings.hdShadowInitParams.shadowResolutionDirectional.ValueGUI<int>(k_DirectionalLightsShadowTiers);
                EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.hdShadowInitParams.maxDirectionalShadowMapResolution, k_MaxShadowResolution);
                --EditorGUI.indentLevel;
            }

            m_ShowPunctualLightSection = EditorGUILayout.Foldout(m_ShowPunctualLightSection, k_PunctualShadowsSubTitle, true);
            if (m_ShowPunctualLightSection)
            {
                ++EditorGUI.indentLevel;
                {
                    EditorGUILayout.LabelField(k_ShadowPunctualLightAtlasSubTitle);
                    ++EditorGUI.indentLevel;
                    {
                        CoreEditorUtils.DrawEnumPopup(serialized.renderPipelineSettings.hdShadowInitParams.serializedPunctualAtlasInit.shadowMapResolution, typeof(ShadowResolutionValue), k_ResolutionContent);
                        EditorGUILayout.IntPopup(serialized.renderPipelineSettings.hdShadowInitParams.serializedPunctualAtlasInit.shadowMapDepthBits, k_ShadowBitDepthNames, k_ShadowBitDepthValues, k_PrecisionContent);
                        EditorGUILayout.PropertyField(serialized.renderPipelineSettings.hdShadowInitParams.serializedPunctualAtlasInit.useDynamicViewportRescale, k_DynamicRescaleContent);
                    }
                    --EditorGUI.indentLevel;
                }
                --EditorGUI.indentLevel;

                ++EditorGUI.indentLevel;
                    EditorGUILayout.LabelField(k_PunctualLightsShadowTiers);
                serialized.renderPipelineSettings.hdShadowInitParams.shadowResolutionPunctual.ValueGUI<int>(k_DirectionalLightsShadowTiers);
                EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.hdShadowInitParams.maxPunctualShadowMapResolution, k_MaxShadowResolution);
                --EditorGUI.indentLevel;
            }

            m_ShowAreaLightSection = EditorGUILayout.Foldout(m_ShowAreaLightSection, k_AreaShadowsSubTitle, true);
            if (m_ShowAreaLightSection)
            {
                ++EditorGUI.indentLevel;
                {
                    EditorGUILayout.LabelField(k_ShadowAreaLightAtlasSubTitle);
                    ++EditorGUI.indentLevel;
                    {
                        CoreEditorUtils.DrawEnumPopup(serialized.renderPipelineSettings.hdShadowInitParams.serializedAreaAtlasInit.shadowMapResolution, typeof(ShadowResolutionValue), k_ResolutionContent);
                        EditorGUILayout.IntPopup(serialized.renderPipelineSettings.hdShadowInitParams.serializedAreaAtlasInit.shadowMapDepthBits, k_ShadowBitDepthNames, k_ShadowBitDepthValues, k_PrecisionContent);
                        EditorGUILayout.PropertyField(serialized.renderPipelineSettings.hdShadowInitParams.serializedAreaAtlasInit.useDynamicViewportRescale, k_DynamicRescaleContent);
                    }
                    --EditorGUI.indentLevel;
                }
                --EditorGUI.indentLevel;

                ++EditorGUI.indentLevel;
                    EditorGUILayout.LabelField(k_AreaLightsShadowTiers);
                serialized.renderPipelineSettings.hdShadowInitParams.shadowResolutionArea.ValueGUI<int>(k_DirectionalLightsShadowTiers);
                EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.hdShadowInitParams.maxAreaShadowMapResolution, k_MaxShadowResolution);
                --EditorGUI.indentLevel;
            }
        }

        static void Drawer_SectionDecalSettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportDecals, k_SupportDecalContent);

            ++EditorGUI.indentLevel;
            using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.supportDecals.boolValue))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.decalSettings.drawDistance, k_DrawDistanceContent);
                if (EditorGUI.EndChangeCheck())
                    serialized.renderPipelineSettings.decalSettings.drawDistance.intValue = Mathf.Max(serialized.renderPipelineSettings.decalSettings.drawDistance.intValue, 0);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.decalSettings.atlasWidth, k_AtlasWidthContent);
                if (EditorGUI.EndChangeCheck())
                    serialized.renderPipelineSettings.decalSettings.atlasWidth.intValue = Mathf.Max(serialized.renderPipelineSettings.decalSettings.atlasWidth.intValue, 0);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.decalSettings.atlasHeight, k_AtlasHeightContent);
                if (EditorGUI.EndChangeCheck())
                    serialized.renderPipelineSettings.decalSettings.atlasHeight.intValue = Mathf.Max(serialized.renderPipelineSettings.decalSettings.atlasHeight.intValue, 0);

                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.decalSettings.perChannelMask, k_MetalAndAOContent);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.lightLoopSettings.maxDecalsOnScreen, k_MaxDecalContent);
                if (EditorGUI.EndChangeCheck())
                    serialized.renderPipelineSettings.lightLoopSettings.maxDecalsOnScreen.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.maxDecalsOnScreen.intValue, 1, HDRenderPipeline.k_MaxDecalsOnScreen);
            }
            --EditorGUI.indentLevel;
        }

        static void Drawer_SectionLightLoop(SerializedHDRenderPipelineAsset serialized, Editor o)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.lightLoopSettings.maxDirectionalLightsOnScreen, k_MaxDirectionalContent);
            if (EditorGUI.EndChangeCheck())
                serialized.renderPipelineSettings.lightLoopSettings.maxDirectionalLightsOnScreen.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.maxDirectionalLightsOnScreen.intValue, 1, HDRenderPipeline.k_MaxDirectionalLightsOnScreen);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.lightLoopSettings.maxPunctualLightsOnScreen, k_MaxPonctualContent);
            if (EditorGUI.EndChangeCheck())
                serialized.renderPipelineSettings.lightLoopSettings.maxPunctualLightsOnScreen.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.maxPunctualLightsOnScreen.intValue, 1, HDRenderPipeline.k_MaxPunctualLightsOnScreen);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.lightLoopSettings.maxAreaLightsOnScreen, k_MaxAreaContent);
            if (EditorGUI.EndChangeCheck())
                serialized.renderPipelineSettings.lightLoopSettings.maxAreaLightsOnScreen.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.maxAreaLightsOnScreen.intValue, 1, HDRenderPipeline.k_MaxAreaLightsOnScreen);
        }

        static void Drawer_SectionDynamicResolutionSettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.enabled, k_Enabled);

            ++EditorGUI.indentLevel;
            using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.dynamicResolutionSettings.enabled.boolValue))
            {
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.dynamicResType, k_DynResType);
                if (serialized.renderPipelineSettings.dynamicResolutionSettings.dynamicResType.hasMultipleDifferentValues)
                {
                    using (new EditorGUI.DisabledGroupScope(true))
                        EditorGUILayout.LabelField(k_MultipleDifferenteValueMessage);
                }
                else
                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.softwareUpsamplingFilter, k_UpsampleFilter);

                if (!serialized.renderPipelineSettings.dynamicResolutionSettings.forcePercentage.hasMultipleDifferentValues
                    && !serialized.renderPipelineSettings.dynamicResolutionSettings.forcePercentage.boolValue)
                {
                    float minPercentage = serialized.renderPipelineSettings.dynamicResolutionSettings.minPercentage.floatValue;
                    float maxPercentage = serialized.renderPipelineSettings.dynamicResolutionSettings.maxPercentage.floatValue;

                    EditorGUI.showMixedValue = serialized.renderPipelineSettings.dynamicResolutionSettings.minPercentage.hasMultipleDifferentValues;
                    EditorGUI.BeginChangeCheck();
                    minPercentage = EditorGUILayout.DelayedFloatField(k_MinPercentage, minPercentage);
                    if (EditorGUI.EndChangeCheck())
                        serialized.renderPipelineSettings.dynamicResolutionSettings.minPercentage.floatValue = Mathf.Clamp(minPercentage, 0.0f, maxPercentage);

                    EditorGUI.showMixedValue = serialized.renderPipelineSettings.dynamicResolutionSettings.maxPercentage.hasMultipleDifferentValues;
                    EditorGUI.BeginChangeCheck();
                    maxPercentage = EditorGUILayout.DelayedFloatField(k_MaxPercentage, maxPercentage);
                    if (EditorGUI.EndChangeCheck())
                        serialized.renderPipelineSettings.dynamicResolutionSettings.maxPercentage.floatValue = Mathf.Clamp(maxPercentage, 0.0f, 100.0f);

                    EditorGUI.showMixedValue = false;
                }

                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.dynamicResolutionSettings.forcePercentage, k_ForceScreenPercentage);

                if (!serialized.renderPipelineSettings.dynamicResolutionSettings.forcePercentage.hasMultipleDifferentValues
                    && serialized.renderPipelineSettings.dynamicResolutionSettings.forcePercentage.boolValue)
                {
                    EditorGUI.showMixedValue = serialized.renderPipelineSettings.dynamicResolutionSettings.forcedPercentage.hasMultipleDifferentValues;
                    float forcePercentage = serialized.renderPipelineSettings.dynamicResolutionSettings.forcedPercentage.floatValue;
                    EditorGUI.BeginChangeCheck();
                    forcePercentage = EditorGUILayout.DelayedFloatField(k_ForcedScreenPercentage, forcePercentage);
                    if (EditorGUI.EndChangeCheck())
                        serialized.renderPipelineSettings.dynamicResolutionSettings.forcedPercentage.floatValue = Mathf.Clamp(forcePercentage, 0.0f, 100.0f);
                    EditorGUI.showMixedValue = false;
                }

                if (serialized.renderPipelineSettings.dynamicResolutionSettings.forcePercentage.hasMultipleDifferentValues)
                {
                    using (new EditorGUI.DisabledGroupScope(true))
                        EditorGUILayout.LabelField(k_MultipleDifferenteValueMessage);
                }
            }
            --EditorGUI.indentLevel;
        }

        static void Drawer_SectionLowResTransparentSettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lowresTransparentSettings.enabled, k_LowResTransparentEnabled);

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
            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.postProcessSettings.lutSize, k_LutSize);
            if (EditorGUI.EndChangeCheck())
                serialized.renderPipelineSettings.postProcessSettings.lutSize.intValue = Mathf.Clamp(serialized.renderPipelineSettings.postProcessSettings.lutSize.intValue, GlobalPostProcessSettings.k_MinLutSize, GlobalPostProcessSettings.k_MaxLutSize);

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessSettings.lutFormat, k_LutFormat);
        }

        static void Drawer_SectionXRSettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.xrSettings.occlusionMesh, k_XROcclusionMesh);
        }

        static private bool m_ShowDoFLowQualitySection = false;
        static private bool m_ShowDoFMediumQualitySection = false;
        static private bool m_ShowDoFHighQualitySection = false;

        static void DrawDepthOfFieldQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            ++EditorGUI.indentLevel;
            {
                EditorGUILayout.LabelField(k_NearBlurSubTitle);
                ++EditorGUI.indentLevel;
                {
                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.NearBlurSampleCount.GetArrayElementAtIndex(tier), k_SampleCountQuality);
                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.NearBlurMaxRadius.GetArrayElementAtIndex(tier), k_MaxRadiusQuality);
                }
                --EditorGUI.indentLevel;
                EditorGUILayout.LabelField(k_FarBlurSubTitle);
                ++EditorGUI.indentLevel;
                {
                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.FarBlurSampleCount.GetArrayElementAtIndex(tier), k_SampleCountQuality);
                    EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.FarBlurMaxRadius.GetArrayElementAtIndex(tier), k_MaxRadiusQuality);
                }
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.DoFResolution.GetArrayElementAtIndex(tier), k_ResolutionQuality);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.DoFHighFilteringQuality.GetArrayElementAtIndex(tier), k_HighQualityFiltering);
            --EditorGUI.indentLevel;
        }

        static void Drawer_SectionDepthOfFieldQualitySettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            m_ShowDoFLowQualitySection = EditorGUILayout.Foldout(m_ShowDoFLowQualitySection, k_LowQualityContent, true);
            if (m_ShowDoFLowQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Low;
                DrawDepthOfFieldQualitySetting(serialized, quality);
            }

            m_ShowDoFMediumQualitySection = EditorGUILayout.Foldout(m_ShowDoFMediumQualitySection, k_MediumQualityContent, true);
            if (m_ShowDoFMediumQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Medium;
                DrawDepthOfFieldQualitySetting(serialized, quality);
            }

            m_ShowDoFHighQualitySection = EditorGUILayout.Foldout(m_ShowDoFHighQualitySection, k_HighQualityContent, true);
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
            m_ShowMotionBlurLowQualitySection = EditorGUILayout.Foldout(m_ShowMotionBlurLowQualitySection, k_LowQualityContent, true);
            if (m_ShowMotionBlurLowQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Low;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.MotionBlurSampleCount.GetArrayElementAtIndex(quality), k_SampleCountQuality);
            }
            m_ShowMotionBlurMediumQualitySection = EditorGUILayout.Foldout(m_ShowMotionBlurMediumQualitySection, k_MediumQualityContent, true);
            if (m_ShowMotionBlurMediumQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Medium;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.MotionBlurSampleCount.GetArrayElementAtIndex(quality), k_SampleCountQuality);
            }
            m_ShowMotionBlurHighQualitySection = EditorGUILayout.Foldout(m_ShowMotionBlurHighQualitySection, k_HighQualityContent, true);
            if (m_ShowMotionBlurHighQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.High;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.MotionBlurSampleCount.GetArrayElementAtIndex(quality), k_SampleCountQuality);
            }
        }

        static private bool m_ShowBloomLowQualitySection = false;
        static private bool m_ShowBloomMediumQualitySection = false;
        static private bool m_ShowBloomHighQualitySection = false;

        static void DrawBloomQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.BloomRes.GetArrayElementAtIndex(tier), k_ResolutionQuality);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.BloomHighFilteringQuality.GetArrayElementAtIndex(tier), k_HighQualityFiltering);
        }

        static void Drawer_SectionBloomQualitySettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            m_ShowBloomLowQualitySection = EditorGUILayout.Foldout(m_ShowBloomLowQualitySection, k_LowQualityContent, true);
            if (m_ShowBloomLowQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Low;
                DrawBloomQualitySetting(serialized, quality);
            }
            m_ShowBloomMediumQualitySection = EditorGUILayout.Foldout(m_ShowBloomMediumQualitySection, k_MediumQualityContent, true);
            if (m_ShowBloomMediumQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Medium;
                DrawBloomQualitySetting(serialized, quality);
            }
            m_ShowBloomHighQualitySection = EditorGUILayout.Foldout(m_ShowBloomHighQualitySection, k_HighQualityContent, true);
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
            m_ShowChromaticAberrationLowQualitySection = EditorGUILayout.Foldout(m_ShowChromaticAberrationLowQualitySection, k_LowQualityContent, true);
            if (m_ShowChromaticAberrationLowQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Low;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.ChromaticAbMaxSamples.GetArrayElementAtIndex(quality), k_MaxSamplesQuality);
            }
            m_ShowChromaticAberrationMediumQualitySection = EditorGUILayout.Foldout(m_ShowChromaticAberrationMediumQualitySection, k_MediumQualityContent, true);
            if (m_ShowChromaticAberrationMediumQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Medium;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.ChromaticAbMaxSamples.GetArrayElementAtIndex(quality), k_MaxSamplesQuality);
            }
            m_ShowChromaticAberrationHighQualitySection = EditorGUILayout.Foldout(m_ShowChromaticAberrationHighQualitySection, k_HighQualityContent, true);
            if (m_ShowChromaticAberrationHighQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.High;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.ChromaticAbMaxSamples.GetArrayElementAtIndex(quality), k_MaxSamplesQuality);
            }
        }

        static void Drawer_SectionRenderingUnsorted(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.colorBufferFormat, k_ColorBufferFormatContent);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportedLitShaderMode, k_SupportLitShaderModeContent);

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
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.MSAASampleCount, k_MSAASampleCountContent);
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportMotionVectors, k_SupportMotionVectorContent);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportRuntimeDebugDisplay, k_SupportRuntimeDebugDisplayContent);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportDitheringCrossFade, k_SupportDitheringCrossFadeContent);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportTerrainHole, k_SupportTerrainHoleContent);

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportTransparentBackface, k_SupportTransparentBackface);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportTransparentDepthPrepass, k_SupportTransparentDepthPrepass);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportTransparentDepthPostpass, k_SupportTransparentDepthPostpass);
            
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportCustomPass, k_SupportCustomPassContent);
            using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.supportCustomPass.boolValue))
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.customBufferFormat, k_CustomBufferFormatContent);
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportRayTracing, k_SupportRaytracing);
            using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.supportRayTracing.boolValue))
            {
                if (serialized.renderPipelineSettings.supportRayTracing.boolValue && !UnityEngine.SystemInfo.supportsRayTracing)
                {
                    EditorGUILayout.HelpBox(k_RayTracingUnsupportedWarning.text, MessageType.Warning, wide: true);
                }
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportedRaytracingTier, k_RaytracingTier);
            }

            serialized.renderPipelineSettings.lodBias.ValueGUI<float>(k_LODBias);
            serialized.renderPipelineSettings.maximumLODLevel.ValueGUI<int>(k_MaximumLODLevel);

            EditorGUILayout.Space(); //to separate with following sub sections
        }

        static void Drawer_SectionLightingUnsorted(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportSSAO, k_SupportSSAOContent);

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportVolumetrics, k_SupportVolumetricContent);
            using (new EditorGUI.DisabledScope(serialized.renderPipelineSettings.supportVolumetrics.hasMultipleDifferentValues
                || !serialized.renderPipelineSettings.supportVolumetrics.boolValue))
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.increaseResolutionOfVolumetrics, k_VolumetricResolutionContent);
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.Space(); //to separate with following sub sections
        }

        static void Drawer_SectionMaterialUnsorted(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.materialQualityLevels);
            var v = EditorGUILayout.EnumPopup(k_CurrentMaterialQualityLevelContent, (MaterialQuality) serialized.currentMaterialQualityLevel.intValue);
            serialized.currentMaterialQualityLevel.intValue = (int)(object)v;
            
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportDistortion, k_SupportDistortion);

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportSubsurfaceScattering, k_SupportedSSSContent);
            using (new EditorGUI.DisabledScope(serialized.renderPipelineSettings.supportSubsurfaceScattering.hasMultipleDifferentValues
                || !serialized.renderPipelineSettings.supportSubsurfaceScattering.boolValue))
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.increaseSssSampleCount, k_SSSSampleCountContent);
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.supportFabricConvolution, k_SupportFabricBSDFConvolutionContent);

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
                builder.AppendLine().AppendFormat(supportedFormater, content.text, k_SupportDrawbacks[content]);
        }

        static void SupportedSettingsInfoSection(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            StringBuilder builder = new StringBuilder("Features supported by this asset:").AppendLine();
            SupportedLitShaderMode supportedLitShaderMode = serialized.renderPipelineSettings.supportedLitShaderMode.GetEnumValue<SupportedLitShaderMode>();
            if (serialized.renderPipelineSettings.supportedLitShaderMode.hasMultipleDifferentValues)
                builder.AppendFormat(supportedFormaterMultipleValue, k_SupportLitShaderModeContent.text);
            else
                builder.AppendFormat(supportedLitShaderModeFormater, k_SupportLitShaderModeContent.text, supportedLitShaderMode, k_SupportLitShaderModeDrawbacks[supportedLitShaderMode]);

            if (serialized.renderPipelineSettings.supportShadowMask.hasMultipleDifferentValues || serialized.renderPipelineSettings.supportedLitShaderMode.hasMultipleDifferentValues)
                builder.AppendLine().AppendFormat(supportedFormaterMultipleValue, k_SupportShadowMaskContent.text);
            else if (serialized.renderPipelineSettings.supportShadowMask.boolValue)
                builder.AppendLine().AppendFormat(supportedFormater, k_SupportShadowMaskContent.text, k_SupportShadowMaskDrawbacks[supportedLitShaderMode]);

            AppendSupport(builder, serialized.renderPipelineSettings.supportSSR, k_SupportSSRContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportSSAO, k_SupportSSAOContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportSubsurfaceScattering, k_SupportedSSSContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportVolumetrics, k_SupportVolumetricContent);

            if (serialized.renderPipelineSettings.supportLightLayers.hasMultipleDifferentValues)
                builder.AppendLine().AppendFormat(supportedFormaterMultipleValue, k_SupportLightLayerContent.text);
            else if (serialized.renderPipelineSettings.supportLightLayers.boolValue)
                builder.AppendLine().AppendFormat(supportedFormater, k_SupportLightLayerContent.text, k_SupportLightLayerDrawbacks[supportedLitShaderMode]);

            if (serialized.renderPipelineSettings.MSAASampleCount.hasMultipleDifferentValues)
                builder.AppendLine().AppendFormat(supportedFormaterMultipleValue, k_MSAASampleCountContent.text);
            else if (serialized.renderPipelineSettings.supportMSAA)
            {
                // NO MSAA in deferred
                if (serialized.renderPipelineSettings.supportedLitShaderMode.intValue != (int)RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly)
                    builder.AppendLine().AppendFormat(supportedFormater, "Multisample Anti-aliasing", k_SupportDrawbacks[k_MSAASampleCountContent]);
            }

            if (serialized.renderPipelineSettings.supportDecals.hasMultipleDifferentValues)
                builder.AppendLine().AppendFormat(supportedFormaterMultipleValue, k_DecalsSubTitle.text);
            else if (serialized.renderPipelineSettings.supportDecals.boolValue)
                builder.AppendLine().AppendFormat(supportedFormater, k_DecalsSubTitle.text, k_SupportDrawbacks[k_SupportDecalContent]);

            if (serialized.renderPipelineSettings.decalSettings.perChannelMask.hasMultipleDifferentValues)
                builder.AppendLine().AppendFormat(supportedFormaterMultipleValue, k_DecalsMetalAndAOSubTitle.text);
            else if (serialized.renderPipelineSettings.supportDecals.boolValue && serialized.renderPipelineSettings.decalSettings.perChannelMask.boolValue)
                builder.AppendLine().AppendFormat(supportedFormater, k_DecalsMetalAndAOSubTitle.text, k_SupportDrawbacks[k_MetalAndAOContent]);

            AppendSupport(builder, serialized.renderPipelineSettings.supportMotionVectors, k_SupportMotionVectorContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportRuntimeDebugDisplay, k_SupportRuntimeDebugDisplayContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportDitheringCrossFade, k_SupportDitheringCrossFadeContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportTerrainHole, k_SupportTerrainHoleContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportDistortion, k_SupportDistortion);
            AppendSupport(builder, serialized.renderPipelineSettings.supportTransparentBackface, k_SupportTransparentBackface);
            AppendSupport(builder, serialized.renderPipelineSettings.supportTransparentDepthPrepass, k_SupportTransparentDepthPrepass);
            AppendSupport(builder, serialized.renderPipelineSettings.supportTransparentDepthPostpass, k_SupportTransparentDepthPostpass);
            AppendSupport(builder, serialized.renderPipelineSettings.supportRayTracing, k_SupportRaytracing);

            EditorGUILayout.HelpBox(builder.ToString(), MessageType.Info, wide: true);
        }
    }
}
