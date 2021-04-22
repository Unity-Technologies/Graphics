using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Text;
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
            VirtualTexturing = 1 << 27,
            FogQuality = 1 << 28,
            Volumetric = 1 << 29,
            ProbeVolume = 1 << 30,
            RTAOQuality = 1 << 31,
            RTRQuality = 1 << 32,
            RTGIQuality = 1 << 33
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

        internal static SelectedFrameSettings selectedFrameSettings;

        internal static VirtualTexturingSettingsUI virtualTexturingSettingsUI = new VirtualTexturingSettingsUI();

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
                    CED.FoldoutGroup(Styles.cookiesSubTitle, Expandable.Cookie, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionCookies),
                    CED.FoldoutGroup(Styles.reflectionsSubTitle, Expandable.Reflection, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionReflection),
                    CED.FoldoutGroup(Styles.skySubTitle, Expandable.Sky, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionSky),
                    CED.FoldoutGroup(Styles.shadowSubTitle, Expandable.Shadow, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout, Drawer_SectionShadows),
                    CED.FoldoutGroup(Styles.lightLoopSubTitle, Expandable.LightLoop, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionLightLoop)
                    ),
                CED.FoldoutGroup(Styles.lightingQualitySettings, Expandable.LightingQuality, k_ExpandedState,
                    CED.FoldoutGroup(Styles.SSAOQualitySettingSubTitle, Expandable.SSAOQuality, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionSSAOQualitySettings),
                    CED.FoldoutGroup(Styles.RTAOQualitySettingSubTitle, Expandable.RTAOQuality, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionRTAOQualitySettings),
                    CED.FoldoutGroup(Styles.contactShadowsSettingsSubTitle, Expandable.ContactShadowQuality, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionContactShadowQualitySettings),
                    CED.FoldoutGroup(Styles.SSRSettingsSubTitle, Expandable.SSRQuality, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionSSRQualitySettings),
                    CED.FoldoutGroup(Styles.RTRSettingsSubTitle, Expandable.RTRQuality, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionRTRQualitySettings),
                    CED.FoldoutGroup(Styles.FogSettingsSubTitle, Expandable.FogQuality, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionFogQualitySettings),
                    CED.FoldoutGroup(Styles.RTGISettingsSubTitle, Expandable.RTGIQuality, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionRTGIQualitySettings)
                    ),
                CED.FoldoutGroup(Styles.materialSectionTitle, Expandable.Material, k_ExpandedState, Drawer_SectionMaterialUnsorted),
                CED.FoldoutGroup(Styles.postProcessSectionTitle, Expandable.PostProcess, k_ExpandedState, Drawer_SectionPostProcessSettings),
                CED.FoldoutGroup(Styles.postProcessQualitySubTitle, Expandable.PostProcessQuality, k_ExpandedState,
                    CED.FoldoutGroup(Styles.depthOfFieldQualitySettings, Expandable.DepthOfFieldQuality, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionDepthOfFieldQualitySettings),
                    CED.FoldoutGroup(Styles.motionBlurQualitySettings, Expandable.MotionBlurQuality, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionMotionBlurQualitySettings),
                    CED.FoldoutGroup(Styles.bloomQualitySettings, Expandable.BloomQuality, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionBloomQualitySettings),
                    CED.FoldoutGroup(Styles.chromaticAberrationQualitySettings, Expandable.ChromaticAberrationQuality, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, Drawer_SectionChromaticAberrationQualitySettings)
                    ),
                CED.FoldoutGroup(Styles.xrTitle, Expandable.XR, k_ExpandedState, Drawer_SectionXRSettings),
                CED.FoldoutGroup(Styles.virtualTexturingTitle, Expandable.VirtualTexturing, k_ExpandedState, Drawer_SectionVTSettings)
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

            EditorGUILayout.PropertyField(serialized.lensAttenuation, Styles.GeneralSection.lensAttenuationModeContent);

            m_ShowLightLayerNames = EditorGUILayout.Foldout(m_ShowLightLayerNames, Styles.lightLayerNamesText, true);
            if (m_ShowLightLayerNames)
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

            m_ShowDecalLayerNames = EditorGUILayout.Foldout(m_ShowDecalLayerNames, Styles.decalLayerNamesText, true);
            if (m_ShowDecalLayerNames)
            {
                ++EditorGUI.indentLevel;
                HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName0, serialized.renderPipelineSettings.decalLayerName0);
                HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName1, serialized.renderPipelineSettings.decalLayerName1);
                HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName2, serialized.renderPipelineSettings.decalLayerName2);
                HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName3, serialized.renderPipelineSettings.decalLayerName3);
                HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName4, serialized.renderPipelineSettings.decalLayerName4);
                HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName5, serialized.renderPipelineSettings.decalLayerName5);
                HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName6, serialized.renderPipelineSettings.decalLayerName6);
                HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName7, serialized.renderPipelineSettings.decalLayerName7);
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
#if UNITY_2020_1_OR_NEWER
#else
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.pointCookieSize, Styles.pointCoockieSizeContent);
#endif
            EditorGUI.BeginChangeCheck();
        }

        static void Drawer_SectionReflection(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportSSR, Styles.supportSSRContent);
            using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.supportSSR.boolValue))
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportSSRTransparent, Styles.supportSSRTransparentContent);
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.reflectionProbeFormat, Styles.reflectionProbeFormatContent);

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

            // Planar reflection probes section
            {
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.planarReflectionAtlasSize, Styles.planarAtlasSizeContent);
                serialized.renderPipelineSettings.planarReflectionResolution.ValueGUI<PlanarReflectionAtlasResolution>(Styles.planarResolutionTitle);
                // We need to clamp the values to the resolution
                int atlasResolution = serialized.renderPipelineSettings.lightLoopSettings.planarReflectionAtlasSize.intValue;
                int numLevels = serialized.renderPipelineSettings.planarReflectionResolution.values.arraySize;
                for (int levelIdx = 0; levelIdx < numLevels; ++levelIdx)
                {
                    SerializedProperty levelValue = serialized.renderPipelineSettings.planarReflectionResolution.values.GetArrayElementAtIndex(levelIdx);
                    levelValue.intValue = Mathf.Min(levelValue.intValue, atlasResolution);
                }
                if (serialized.renderPipelineSettings.lightLoopSettings.planarReflectionAtlasSize.hasMultipleDifferentValues)
                    EditorGUILayout.HelpBox(Styles.multipleDifferenteValueMessage, MessageType.Info);
                else
                {
                    long currentCache = PlanarReflectionProbeCache.GetApproxCacheSizeInByte(1, serialized.renderPipelineSettings.lightLoopSettings.planarReflectionAtlasSize.intValue, GraphicsFormat.R16G16B16A16_UNorm);
                    string message = string.Format(Styles.cacheInfoFormat, HDEditorUtils.HumanizeWeight(currentCache));
                    EditorGUILayout.HelpBox(message, MessageType.Info);
                }
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightLoopSettings.maxPlanarReflectionOnScreen, Styles.maxPlanarReflectionOnScreen);
                if (EditorGUI.EndChangeCheck())
                    serialized.renderPipelineSettings.lightLoopSettings.maxPlanarReflectionOnScreen.intValue = Mathf.Clamp(serialized.renderPipelineSettings.lightLoopSettings.maxPlanarReflectionOnScreen.intValue, 1, ShaderVariablesGlobal.s_MaxEnv2DLight);
            }

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

        static private bool m_ShowLightLayerNames = false;
        static private bool m_ShowDecalLayerNames = false;
        static private bool m_ShowDirectionalLightSection = false;
        static private bool m_ShowPunctualLightSection = false;
        static private bool m_ShowAreaLightSection = false;

        static void Drawer_SectionShadows(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportShadowMask, Styles.supportShadowMaskContent);

            EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.hdShadowInitParams.maxShadowRequests, Styles.maxRequestContent);

            if (!serialized.renderPipelineSettings.supportedLitShaderMode.hasMultipleDifferentValues)
            {
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

                ++EditorGUI.indentLevel;
                // Because we don't know if the asset is old and had the cached shadow map resolution field, if it was set as default float (0) we force a default.
                if (serialized.renderPipelineSettings.hdShadowInitParams.cachedPunctualShadowAtlasResolution.intValue == 0)
                {
                    serialized.renderPipelineSettings.hdShadowInitParams.cachedPunctualShadowAtlasResolution.intValue = 2048;
                }
                CoreEditorUtils.DrawEnumPopup(serialized.renderPipelineSettings.hdShadowInitParams.cachedPunctualShadowAtlasResolution, typeof(ShadowResolutionValue), Styles.cachedShadowAtlasResolution);
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

                ++EditorGUI.indentLevel;
                if (serialized.renderPipelineSettings.hdShadowInitParams.cachedAreaShadowAtlasResolution.intValue == 0)
                {
                    serialized.renderPipelineSettings.hdShadowInitParams.cachedAreaShadowAtlasResolution.intValue = 1024;
                }
                CoreEditorUtils.DrawEnumPopup(serialized.renderPipelineSettings.hdShadowInitParams.cachedAreaShadowAtlasResolution, typeof(ShadowResolutionValue), Styles.cachedShadowAtlasResolution);
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

                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportDecalLayers, Styles.supportDecalLayersContent);
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

        static void Drawer_SectionVTSettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            virtualTexturingSettingsUI.OnGUI(serialized, owner);
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
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.DoFPhysicallyBased.GetArrayElementAtIndex(tier), Styles.dofPhysicallyBased);
            --EditorGUI.indentLevel;
        }

        static void Drawer_SectionDepthOfFieldQualitySettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            m_ShowDoFLowQualitySection = EditorGUILayout.Foldout(m_ShowDoFLowQualitySection, Styles.lowQualityContent, true);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowDoFLowQualitySection);
            if (m_ShowDoFLowQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Low;
                DrawDepthOfFieldQualitySetting(serialized, quality);
            }

            m_ShowDoFMediumQualitySection = EditorGUILayout.Foldout(m_ShowDoFMediumQualitySection, Styles.mediumQualityContent, true);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowDoFMediumQualitySection);
            if (m_ShowDoFMediumQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Medium;
                DrawDepthOfFieldQualitySetting(serialized, quality);
            }

            m_ShowDoFHighQualitySection = EditorGUILayout.Foldout(m_ShowDoFHighQualitySection, Styles.highQualityContent, true);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowDoFHighQualitySection);
            if (m_ShowDoFHighQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.High;
                DrawDepthOfFieldQualitySetting(serialized, quality);
            }
        }

        static private bool m_ShowMotionBlurLowQualitySection = false;
        static private bool m_ShowMotionBlurMediumQualitySection = false;
        static private bool m_ShowMotionBlurHighQualitySection = false;

        static void DrawMotionBlurQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.MotionBlurSampleCount.GetArrayElementAtIndex(tier), Styles.sampleCountQuality);
            --EditorGUI.indentLevel;
        }

        static void Drawer_SectionMotionBlurQualitySettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            m_ShowMotionBlurLowQualitySection = EditorGUILayout.Foldout(m_ShowMotionBlurLowQualitySection, Styles.lowQualityContent, true);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowMotionBlurLowQualitySection);
            if (m_ShowMotionBlurLowQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Low;
                DrawMotionBlurQualitySetting(serialized, quality);
            }
            m_ShowMotionBlurMediumQualitySection = EditorGUILayout.Foldout(m_ShowMotionBlurMediumQualitySection, Styles.mediumQualityContent, true);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowMotionBlurMediumQualitySection);
            if (m_ShowMotionBlurMediumQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Medium;
                DrawMotionBlurQualitySetting(serialized, quality);
            }
            m_ShowMotionBlurHighQualitySection = EditorGUILayout.Foldout(m_ShowMotionBlurHighQualitySection, Styles.highQualityContent, true);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowMotionBlurHighQualitySection);
            if (m_ShowMotionBlurHighQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.High;
                DrawMotionBlurQualitySetting(serialized, quality);
            }
        }

        static private bool m_ShowBloomLowQualitySection = false;
        static private bool m_ShowBloomMediumQualitySection = false;
        static private bool m_ShowBloomHighQualitySection = false;

        static void DrawBloomQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.BloomRes.GetArrayElementAtIndex(tier), Styles.resolutionQuality);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.BloomHighPrefilteringQuality.GetArrayElementAtIndex(tier), Styles.highQualityPrefiltering);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.BloomHighFilteringQuality.GetArrayElementAtIndex(tier), Styles.highQualityFiltering);
            --EditorGUI.indentLevel;
        }

        static void Drawer_SectionBloomQualitySettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            m_ShowBloomLowQualitySection = EditorGUILayout.Foldout(m_ShowBloomLowQualitySection, Styles.lowQualityContent, true);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowBloomLowQualitySection);
            if (m_ShowBloomLowQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Low;
                DrawBloomQualitySetting(serialized, quality);
            }
            m_ShowBloomMediumQualitySection = EditorGUILayout.Foldout(m_ShowBloomMediumQualitySection, Styles.mediumQualityContent, true);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowBloomMediumQualitySection);
            if (m_ShowBloomMediumQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Medium;
                DrawBloomQualitySetting(serialized, quality);
            }
            m_ShowBloomHighQualitySection = EditorGUILayout.Foldout(m_ShowBloomHighQualitySection, Styles.highQualityContent, true);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowBloomHighQualitySection);
            if (m_ShowBloomHighQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.High;
                DrawBloomQualitySetting(serialized, quality);
            }
        }

        static private bool m_ShowChromaticAberrationLowQualitySection = false;
        static private bool m_ShowChromaticAberrationMediumQualitySection = false;
        static private bool m_ShowChromaticAberrationHighQualitySection = false;

        static void DrawChromaticAberrationQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.postProcessQualitySettings.ChromaticAbMaxSamples.GetArrayElementAtIndex(tier), Styles.maxSamplesQuality);
            --EditorGUI.indentLevel;
        }

        static void Drawer_SectionChromaticAberrationQualitySettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            m_ShowChromaticAberrationLowQualitySection = EditorGUILayout.Foldout(m_ShowChromaticAberrationLowQualitySection, Styles.lowQualityContent, true);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowChromaticAberrationLowQualitySection);
            if (m_ShowChromaticAberrationLowQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Low;
                DrawChromaticAberrationQualitySetting(serialized, quality);
            }
            m_ShowChromaticAberrationMediumQualitySection = EditorGUILayout.Foldout(m_ShowChromaticAberrationMediumQualitySection, Styles.mediumQualityContent, true);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowChromaticAberrationMediumQualitySection);
            if (m_ShowChromaticAberrationMediumQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Medium;
                DrawChromaticAberrationQualitySetting(serialized, quality);
            }
            m_ShowChromaticAberrationHighQualitySection = EditorGUILayout.Foldout(m_ShowChromaticAberrationHighQualitySection, Styles.highQualityContent, true);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowChromaticAberrationHighQualitySection);
            if (m_ShowChromaticAberrationHighQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.High;
                DrawChromaticAberrationQualitySetting(serialized, quality);
            }
        }

        static void DrawAOQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.AOStepCount.GetArrayElementAtIndex(tier), Styles.AOStepCount);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.AOFullRes.GetArrayElementAtIndex(tier), Styles.AOFullRes);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.AOMaximumRadiusPixels.GetArrayElementAtIndex(tier), Styles.AOMaxRadiusInPixels);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.AODirectionCount.GetArrayElementAtIndex(tier), Styles.AODirectionCount);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.AOBilateralUpsample.GetArrayElementAtIndex(tier), Styles.AOBilateralUpsample);
            --EditorGUI.indentLevel;
        }

        static void DrawRTAOQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTAORayLength.GetArrayElementAtIndex(tier), Styles.RTAORayLength);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTAOSampleCount.GetArrayElementAtIndex(tier), Styles.RTAOSampleCount);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTAODenoise.GetArrayElementAtIndex(tier), Styles.RTAODenoise);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTAODenoiserRadius.GetArrayElementAtIndex(tier), Styles.RTAODenoiserRadius);
            --EditorGUI.indentLevel;
        }

        static void CheckFoldoutClick(Rect foldoutRect, ref bool foldoutFlag)
        {
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (foldoutRect.Contains(e.mousePosition))
                {
                    foldoutFlag = !foldoutFlag;
                }
            }
        }

        static private bool m_ShowAOLowQualitySection = false;
        static private bool m_ShowAOMediumQualitySection = false;
        static private bool m_ShowAOHighQualitySection = false;

        static void Drawer_SectionSSAOQualitySettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            m_ShowAOLowQualitySection = EditorGUILayout.Foldout(m_ShowAOLowQualitySection, Styles.lowQualityContent);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowAOLowQualitySection);
            if (m_ShowAOLowQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Low;
                DrawAOQualitySetting(serialized, quality);
            }

            m_ShowAOMediumQualitySection = EditorGUILayout.Foldout(m_ShowAOMediumQualitySection, Styles.mediumQualityContent);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowAOMediumQualitySection);
            if (m_ShowAOMediumQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Medium;
                DrawAOQualitySetting(serialized, quality);
            }

            m_ShowAOHighQualitySection = EditorGUILayout.Foldout(m_ShowAOHighQualitySection, Styles.highQualityContent);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowAOHighQualitySection);
            if (m_ShowAOHighQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.High;
                DrawAOQualitySetting(serialized, quality);
            }
        }

        static private bool m_ShowRTAOLowQualitySection = false;
        static private bool m_ShowRTAOMediumQualitySection = false;
        static private bool m_ShowRTAOHighQualitySection = false;

        static void Drawer_SectionRTAOQualitySettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            m_ShowRTAOLowQualitySection = EditorGUILayout.Foldout(m_ShowRTAOLowQualitySection, Styles.lowQualityContent);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowRTAOLowQualitySection);
            if (m_ShowRTAOLowQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Low;
                DrawRTAOQualitySetting(serialized, quality);
            }

            m_ShowRTAOMediumQualitySection = EditorGUILayout.Foldout(m_ShowRTAOMediumQualitySection, Styles.mediumQualityContent);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowRTAOMediumQualitySection);
            if (m_ShowRTAOMediumQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Medium;
                DrawRTAOQualitySetting(serialized, quality);
            }

            m_ShowRTAOHighQualitySection = EditorGUILayout.Foldout(m_ShowRTAOHighQualitySection, Styles.highQualityContent);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowRTAOHighQualitySection);
            if (m_ShowRTAOHighQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.High;
                DrawRTAOQualitySetting(serialized, quality);
            }
        }

        static private bool m_ShowContactShadowLowQualitySection = false;
        static private bool m_ShowContactShadowMediumQualitySection = false;
        static private bool m_ShowContactShadowHighQualitySection = false;

        static void DrawContactShadowQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.ContactShadowSampleCount.GetArrayElementAtIndex(tier), Styles.contactShadowsSampleCount);
            --EditorGUI.indentLevel;
        }

        static void Drawer_SectionContactShadowQualitySettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            m_ShowContactShadowLowQualitySection = EditorGUILayout.Foldout(m_ShowContactShadowLowQualitySection, Styles.lowQualityContent);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowContactShadowLowQualitySection);
            if (m_ShowContactShadowLowQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Low;
                DrawContactShadowQualitySetting(serialized, quality);
            }

            m_ShowContactShadowMediumQualitySection = EditorGUILayout.Foldout(m_ShowContactShadowMediumQualitySection, Styles.mediumQualityContent);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowContactShadowMediumQualitySection);
            if (m_ShowContactShadowMediumQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Medium;
                DrawContactShadowQualitySetting(serialized, quality);
            }

            m_ShowContactShadowHighQualitySection = EditorGUILayout.Foldout(m_ShowContactShadowHighQualitySection, Styles.highQualityContent);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowContactShadowHighQualitySection);
            if (m_ShowContactShadowHighQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.High;
                DrawContactShadowQualitySetting(serialized, quality);
            }
        }

        static private bool m_ShowSSRLowQualitySection = false;
        static private bool m_ShowSSRMediumQualitySection = false;
        static private bool m_ShowSSRHighQualitySection = false;

        static void DrawSSRQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.SSRMaxRaySteps.GetArrayElementAtIndex(tier), Styles.contactShadowsSampleCount);
            --EditorGUI.indentLevel;
        }

        static void Drawer_SectionSSRQualitySettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            m_ShowSSRLowQualitySection = EditorGUILayout.Foldout(m_ShowSSRLowQualitySection, Styles.lowQualityContent);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowSSRLowQualitySection);
            if (m_ShowSSRLowQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Low;
                DrawSSRQualitySetting(serialized, quality);
            }

            m_ShowSSRMediumQualitySection = EditorGUILayout.Foldout(m_ShowSSRMediumQualitySection, Styles.mediumQualityContent);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowSSRMediumQualitySection);
            if (m_ShowSSRMediumQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Medium;
                DrawSSRQualitySetting(serialized, quality);
            }

            m_ShowSSRHighQualitySection = EditorGUILayout.Foldout(m_ShowSSRHighQualitySection, Styles.highQualityContent);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowSSRHighQualitySection);
            if (m_ShowSSRHighQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.High;
                DrawSSRQualitySetting(serialized, quality);
            }
        }

        static private bool m_ShowRTRLowQualitySection = false;
        static private bool m_ShowRTRMediumQualitySection = false;
        static private bool m_ShowRTRHighQualitySection = false;

        static void DrawRTRQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTRMinSmoothness.GetArrayElementAtIndex(tier), Styles.RTRMinSmoothness);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTRSmoothnessFadeStart.GetArrayElementAtIndex(tier), Styles.RTRSmoothnessFadeStart);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTRRayLength.GetArrayElementAtIndex(tier), Styles.RTRRayLength);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTRClampValue.GetArrayElementAtIndex(tier), Styles.RTRClampValue);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTRFullResolution.GetArrayElementAtIndex(tier), Styles.RTRFullResolution);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTRDenoise.GetArrayElementAtIndex(tier), Styles.RTRDenoise);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTRDenoiserRadius.GetArrayElementAtIndex(tier), Styles.RTRDenoiserRadius);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTRSmoothDenoising.GetArrayElementAtIndex(tier), Styles.RTRSmoothDenoising);
            --EditorGUI.indentLevel;
        }

        static void Drawer_SectionRTRQualitySettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            m_ShowRTRLowQualitySection = EditorGUILayout.Foldout(m_ShowRTRLowQualitySection, Styles.lowQualityContent);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowRTRLowQualitySection);
            if (m_ShowRTRLowQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Low;
                DrawRTRQualitySetting(serialized, quality);
            }

            m_ShowRTRMediumQualitySection = EditorGUILayout.Foldout(m_ShowRTRMediumQualitySection, Styles.mediumQualityContent);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowRTRMediumQualitySection);
            if (m_ShowRTRMediumQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Medium;
                DrawRTRQualitySetting(serialized, quality);
            }

            m_ShowRTRHighQualitySection = EditorGUILayout.Foldout(m_ShowRTRHighQualitySection, Styles.highQualityContent);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowRTRHighQualitySection);
            if (m_ShowRTRHighQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.High;
                DrawRTRQualitySetting(serialized, quality);
            }
        }

        static private bool m_ShowFogLowQualitySection = false;
        static private bool m_ShowFogMediumQualitySection = false;
        static private bool m_ShowFogHighQualitySection = false;

        static void DrawVolumetricFogQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            ++EditorGUI.indentLevel;
            var budget = serialized.renderPipelineSettings.lightingQualitySettings.VolumetricFogBudget.GetArrayElementAtIndex(tier);
            EditorGUILayout.PropertyField(budget, Styles.FogSettingsBudget);
            budget.floatValue = Mathf.Clamp(budget.floatValue, 0.0f, 1.0f);
            var ratio = serialized.renderPipelineSettings.lightingQualitySettings.VolumetricFogRatio.GetArrayElementAtIndex(tier);
            EditorGUILayout.PropertyField(ratio, Styles.FogSettingsRatio);
            ratio.floatValue = Mathf.Clamp(ratio.floatValue, 0.0f, 1.0f);
            --EditorGUI.indentLevel;
        }

        static void Drawer_SectionFogQualitySettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            m_ShowFogLowQualitySection = EditorGUILayout.Foldout(m_ShowFogLowQualitySection, Styles.lowQualityContent);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowFogLowQualitySection);
            if (m_ShowFogLowQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Low;
                DrawVolumetricFogQualitySetting(serialized, quality);
            }

            m_ShowFogMediumQualitySection = EditorGUILayout.Foldout(m_ShowFogMediumQualitySection, Styles.mediumQualityContent);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowFogMediumQualitySection);
            if (m_ShowFogMediumQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Medium;
                DrawVolumetricFogQualitySetting(serialized, quality);
            }

            m_ShowFogHighQualitySection = EditorGUILayout.Foldout(m_ShowFogHighQualitySection, Styles.highQualityContent);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowFogHighQualitySection);
            if (m_ShowFogHighQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.High;
                DrawVolumetricFogQualitySetting(serialized, quality);
            }
        }

        static private bool m_ShowRTGILowQualitySection = false;
        static private bool m_ShowRTGIMediumQualitySection = false;
        static private bool m_ShowRTGIHighQualitySection = false;

        static void DrawRTGIQualitySetting(SerializedHDRenderPipelineAsset serialized, int tier)
        {
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTGIRayLength.GetArrayElementAtIndex(tier), Styles.RTGIRayLength);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTGIClampValue.GetArrayElementAtIndex(tier), Styles.RTGIClampValue);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTGIFullResolution.GetArrayElementAtIndex(tier), Styles.RTGIFullResolution);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTGIUpScaleRadius.GetArrayElementAtIndex(tier), Styles.RTGIUpScaleRadius);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTGIDenoise.GetArrayElementAtIndex(tier), Styles.RTGIDenoise);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTGIHalfResDenoise.GetArrayElementAtIndex(tier), Styles.RTGIHalfResDenoise);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTGIDenoiserRadius.GetArrayElementAtIndex(tier), Styles.RTGIDenoiserRadius);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.lightingQualitySettings.RTGISecondDenoise.GetArrayElementAtIndex(tier), Styles.RTGISecondDenoise);
            --EditorGUI.indentLevel;
        }

        static void Drawer_SectionRTGIQualitySettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            m_ShowRTGILowQualitySection = EditorGUILayout.Foldout(m_ShowRTGILowQualitySection, Styles.lowQualityContent);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowRTGILowQualitySection);
            if (m_ShowRTGILowQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Low;
                DrawRTGIQualitySetting(serialized, quality);
            }

            m_ShowRTGIMediumQualitySection = EditorGUILayout.Foldout(m_ShowRTGIMediumQualitySection, Styles.mediumQualityContent);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowRTGIMediumQualitySection);
            if (m_ShowRTGIMediumQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.Medium;
                DrawRTGIQualitySetting(serialized, quality);
            }

            m_ShowRTGIHighQualitySection = EditorGUILayout.Foldout(m_ShowRTGIHighQualitySection, Styles.highQualityContent);
            CheckFoldoutClick(GUILayoutUtility.GetLastRect(), ref m_ShowRTGIHighQualitySection);
            if (m_ShowRTGIHighQualitySection)
            {
                int quality = (int)ScalableSettingLevelParameter.Level.High;
                DrawRTGIQualitySetting(serialized, quality);
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

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportRayTracing, Styles.supportRaytracing);
            using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.supportRayTracing.boolValue))
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportedRayTracingMode, Styles.supportedRayTracingMode);
                if (serialized.renderPipelineSettings.supportRayTracing.boolValue && !UnityEngine.SystemInfo.supportsRayTracing)
                {
                    if (PlayerSettings.GetGraphicsAPIs(EditorUserBuildSettings.activeBuildTarget)[0] != GraphicsDeviceType.Direct3D12)
                    {
                        EditorGUILayout.HelpBox(Styles.rayTracingDX12OnlyWarning.text, MessageType.Warning, wide: true);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(Styles.rayTracingUnsupportedWarning.text, MessageType.Warning, wide: true);
                    }
                }
                --EditorGUI.indentLevel;
            }

            serialized.renderPipelineSettings.lodBias.ValueGUI<float>(Styles.LODBias);
            serialized.renderPipelineSettings.maximumLODLevel.ValueGUI<int>(Styles.maximumLODLevel);

            EditorGUILayout.Space(); //to separate with following sub sections
        }

        static void Drawer_SectionLightingUnsorted(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportSSAO, Styles.supportSSAOContent);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportSSGI, Styles.supportSSGIContent);

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportVolumetrics, Styles.supportVolumetricContent);

            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportLightLayers, Styles.supportLightLayerContent);

            if (ShaderConfig.s_EnableProbeVolumes == 1)
            {
                EditorGUILayout.PropertyField(serialized.renderPipelineSettings.supportProbeVolume, Styles.supportProbeVolumeContent);
                using (new EditorGUI.DisabledScope(!serialized.renderPipelineSettings.supportProbeVolume.boolValue))
                {
                    ++EditorGUI.indentLevel;

                    if (serialized.renderPipelineSettings.supportProbeVolume.boolValue)
                        EditorGUILayout.HelpBox(Styles.probeVolumeInfo, MessageType.Warning);

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.probeVolumeSettings.atlasResolution, Styles.probeVolumeAtlasResolution);
                    if (EditorGUI.EndChangeCheck())
                    {
                        serialized.renderPipelineSettings.probeVolumeSettings.atlasResolution.intValue = Mathf.Max(serialized.renderPipelineSettings.probeVolumeSettings.atlasResolution.intValue, 0);
                    }
                    else
                    {
                        long currentCache = HDRenderPipeline.GetApproxProbeVolumeAtlasSizeInByte(serialized.renderPipelineSettings.probeVolumeSettings.atlasResolution.intValue);
                        if (currentCache > HDRenderPipeline.k_MaxCacheSize)
                        {
                            int reserved = HDRenderPipeline.GetMaxProbeVolumeAtlasSizeForWeightInByte(HDRenderPipeline.k_MaxCacheSize);
                            string message = string.Format(Styles.cacheErrorFormat, HDEditorUtils.HumanizeWeight(currentCache), reserved);
                            EditorGUILayout.HelpBox(message, MessageType.Error);
                        }
                        else
                        {
                            string message = string.Format(Styles.cacheInfoFormat, HDEditorUtils.HumanizeWeight(currentCache));
                            EditorGUILayout.HelpBox(message, MessageType.Info);
                        }
                    }

                    EditorGUI.BeginDisabledGroup(ShaderConfig.s_ProbeVolumesBilateralFilteringMode != ProbeVolumesBilateralFilteringModes.OctahedralDepth);
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.DelayedIntField(serialized.renderPipelineSettings.probeVolumeSettings.atlasOctahedralDepthResolution, Styles.probeVolumeAtlasOctahedralDepthResolution);
                    if (EditorGUI.EndChangeCheck())
                    {
                        serialized.renderPipelineSettings.probeVolumeSettings.atlasOctahedralDepthResolution.intValue = Mathf.Max(serialized.renderPipelineSettings.probeVolumeSettings.atlasOctahedralDepthResolution.intValue, 0);
                    }
                    else if (ShaderConfig.s_ProbeVolumesBilateralFilteringMode == ProbeVolumesBilateralFilteringModes.OctahedralDepth)
                    {
                        // Only display memory allocation info if octahedral depth feature is actually enabled. Only then will memory be allocated.
                        long currentCache = HDRenderPipeline.GetApproxProbeVolumeOctahedralDepthAtlasSizeInByte(serialized.renderPipelineSettings.probeVolumeSettings.atlasOctahedralDepthResolution.intValue);
                        if (currentCache > HDRenderPipeline.k_MaxCacheSize)
                        {
                            int reserved = HDRenderPipeline.GetMaxProbeVolumeOctahedralDepthAtlasSizeForWeightInByte(HDRenderPipeline.k_MaxCacheSize);
                            string message = string.Format(Styles.cacheErrorFormat, HDEditorUtils.HumanizeWeight(currentCache), reserved);
                            EditorGUILayout.HelpBox(message, MessageType.Error);
                        }
                        else
                        {
                            string message = string.Format(Styles.cacheInfoFormat, HDEditorUtils.HumanizeWeight(currentCache));
                            EditorGUILayout.HelpBox(message, MessageType.Info);
                        }
                    }
                    EditorGUI.EndDisabledGroup();

                    if (serialized.renderPipelineSettings.probeVolumeSettings.atlasResolution.intValue <= 0)
                    {
                        // Detected legacy probe volume atlas (atlasResolution did not exist. Was explicitly defined by atlasWidth, atlasHeight, atlasDepth).
                        // Initialize with default values.
                        // TODO: (Nick) This can be removed in release. It's currently here to reduce user pain on internal projects actively using this WIP tech.
                        serialized.renderPipelineSettings.probeVolumeSettings.atlasResolution.intValue = GlobalProbeVolumeSettings.@default.atlasResolution;
                    }

                    if (serialized.renderPipelineSettings.probeVolumeSettings.atlasOctahedralDepthResolution.intValue <= 0)
                    {
                        // Detected legacy probe volume atlas (atlasOctahedralDepthResolution did not exist. Was explicitly defined by atlasWidth, atlasHeight, atlasDepth).
                        // Initialize with default values.
                        // TODO: (Nick) This can be removed in release. It's currently here to reduce user pain on internal projects actively using this WIP tech.
                        serialized.renderPipelineSettings.probeVolumeSettings.atlasOctahedralDepthResolution.intValue = GlobalProbeVolumeSettings.@default.atlasOctahedralDepthResolution;
                    }

                    --EditorGUI.indentLevel;
                }
            } // s_ProbeVolumesEvaluationMode

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

            if (serialized.renderPipelineSettings.supportDecalLayers.hasMultipleDifferentValues)
                builder.AppendLine().AppendFormat(supportedFormaterMultipleValue, Styles.decalsSubTitle.text);
            else if (serialized.renderPipelineSettings.supportDecals.boolValue && serialized.renderPipelineSettings.supportDecalLayers.boolValue)
                builder.AppendLine().AppendFormat(supportedFormater, Styles.decalsSubTitle.text, Styles.supportDrawbacks[Styles.supportDecalLayersContent]);

            if (serialized.renderPipelineSettings.decalSettings.perChannelMask.hasMultipleDifferentValues)
                builder.AppendLine().AppendFormat(supportedFormaterMultipleValue, Styles.decalsMetalAndAOSubTitle.text);
            else if (serialized.renderPipelineSettings.supportDecals.boolValue && serialized.renderPipelineSettings.decalSettings.perChannelMask.boolValue)
                builder.AppendLine().AppendFormat(supportedFormater, Styles.decalsMetalAndAOSubTitle.text, Styles.supportDrawbacks[Styles.metalAndAOContent]);

            AppendSupport(builder, serialized.renderPipelineSettings.supportMotionVectors, Styles.supportMotionVectorContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportRuntimeDebugDisplay, Styles.supportRuntimeDebugDisplayContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportRuntimeAOVAPI, Styles.supportRuntimeAOVAPIContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportDitheringCrossFade, Styles.supportDitheringCrossFadeContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportTerrainHole, Styles.supportTerrainHoleContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportDistortion, Styles.supportDistortion);
            AppendSupport(builder, serialized.renderPipelineSettings.supportTransparentBackface, Styles.supportTransparentBackface);
            AppendSupport(builder, serialized.renderPipelineSettings.supportTransparentDepthPrepass, Styles.supportTransparentDepthPrepass);
            AppendSupport(builder, serialized.renderPipelineSettings.supportTransparentDepthPostpass, Styles.supportTransparentDepthPostpass);
            AppendSupport(builder, serialized.renderPipelineSettings.supportRayTracing, Styles.supportRaytracing);
            if (ShaderConfig.s_EnableProbeVolumes == 1)
                AppendSupport(builder, serialized.renderPipelineSettings.supportProbeVolume, Styles.supportProbeVolumeContent);
            AppendSupport(builder, serialized.renderPipelineSettings.supportedRayTracingMode, Styles.supportedRayTracingMode);

            EditorGUILayout.HelpBox(builder.ToString(), MessageType.Info, wide: true);
        }
    }
}
