using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    using CED = CoreEditorDrawer<SerializedUniversalRenderPipelineAsset>;

    //The internal one is private
    static class StringBuilderPool
    {
        internal static readonly UnityEngine.Pool.ObjectPool<StringBuilder> s_Pool = new (
            () => new StringBuilder(), 
            null, 
            sb => sb.Clear()    //clear on release
            );

        public static StringBuilder Get() => s_Pool.Get();
        public static UnityEngine.Pool.PooledObject<StringBuilder> Get(out StringBuilder value) => s_Pool.Get(out value);
        public static void Release(StringBuilder toRelease) => s_Pool.Release(toRelease);
    }

    internal partial class UniversalRenderPipelineAssetUI
    {
        internal enum Expandable
        {
            Rendering = 1 << 1,
            Quality = 1 << 2,
            Lighting = 1 << 3,
            Shadows = 1 << 4,
            PostProcessing = 1 << 5,
#if ENABLE_ADAPTIVE_PERFORMANCE
            AdaptivePerformance = 1 << 6,
#endif
            Volumes = 1 << 7,
        }

        enum ExpandableAdditional
        {
            Rendering = 1 << 1,
            Lighting = 1 << 2,
            PostProcessing = 1 << 3,
            Shadows = 1 << 4,
            Quality = 1 << 5,
        }

        internal static void RegisterEditor(UniversalRenderPipelineAssetEditor editor)
        {
            k_AdditionalPropertiesState.RegisterEditor(editor);
        }

        internal static void UnregisterEditor(UniversalRenderPipelineAssetEditor editor)
        {
            k_AdditionalPropertiesState.UnregisterEditor(editor);
        }

        static bool ValidateRendererGraphicsAPIsForLightLayers(UniversalRenderPipelineAsset pipelineAsset, out string unsupportedGraphicsApisMessage)
        {
            unsupportedGraphicsApisMessage = null;

            BuildTarget platform = EditorUserBuildSettings.activeBuildTarget;
            GraphicsDeviceType[] graphicsAPIs = PlayerSettings.GetGraphicsAPIs(platform);

            for (int apiIndex = 0; apiIndex < graphicsAPIs.Length; apiIndex++)
            {
                if (!RenderingUtils.SupportsLightLayers(graphicsAPIs[apiIndex]))
                {
                    if (unsupportedGraphicsApisMessage != null)
                        unsupportedGraphicsApisMessage += ", ";
                    unsupportedGraphicsApisMessage += System.String.Format("{0}", graphicsAPIs[apiIndex]);
                }
            }

            if (unsupportedGraphicsApisMessage != null)
                unsupportedGraphicsApisMessage += ".";

            return unsupportedGraphicsApisMessage == null;
        }

        static bool ValidateRendererGraphicsAPIs(UniversalRenderPipelineAsset pipelineAsset, out string unsupportedGraphicsApisMessage)
        {
            // Check the list of Renderers against all Graphics APIs the player is built with.
            unsupportedGraphicsApisMessage = null;

            BuildTarget platform = EditorUserBuildSettings.activeBuildTarget;
            GraphicsDeviceType[] graphicsAPIs = PlayerSettings.GetGraphicsAPIs(platform);
            int rendererCount = pipelineAsset.m_RendererDataList.Length;

            for (int i = 0; i < rendererCount; i++)
            {
                ScriptableRenderer renderer = pipelineAsset.GetRenderer(i);
                if (renderer == null)
                    continue;

                GraphicsDeviceType[] unsupportedAPIs = renderer.unsupportedGraphicsDeviceTypes;

                for (int apiIndex = 0; apiIndex < unsupportedAPIs.Length; apiIndex++)
                {
                    if (Array.FindIndex(graphicsAPIs, element => element == unsupportedAPIs[apiIndex]) >= 0)
                        unsupportedGraphicsApisMessage += $"{renderer} at index {i} does not support {unsupportedAPIs[apiIndex]}.\n";
                }
            }

            return unsupportedGraphicsApisMessage == null;
        }

        static readonly ExpandedState<Expandable, UniversalRenderPipelineAsset> k_ExpandedState = new(Expandable.Rendering, "URP");
        readonly static AdditionalPropertiesState<ExpandableAdditional, Light> k_AdditionalPropertiesState = new(0, "URP");

        internal static void Expand(Expandable expandable, bool state)
        {
            k_ExpandedState[expandable] = state;
        }

        public static readonly CED.IDrawer Inspector = CED.Group(
            CED.Group(PrepareOnTileValidationWarning),
            CED.AdditionalPropertiesFoldoutGroup(Styles.renderingSettingsText, Expandable.Rendering, k_ExpandedState, ExpandableAdditional.Rendering, k_AdditionalPropertiesState, DrawRendering, DrawRenderingAdditional),
            CED.FoldoutGroup(Styles.qualitySettingsText, Expandable.Quality, k_ExpandedState, DrawQuality),
            CED.AdditionalPropertiesFoldoutGroup(Styles.lightingSettingsText, Expandable.Lighting, k_ExpandedState, ExpandableAdditional.Lighting, k_AdditionalPropertiesState, DrawLighting, DrawLightingAdditional),
            CED.AdditionalPropertiesFoldoutGroup(Styles.shadowSettingsText, Expandable.Shadows, k_ExpandedState, ExpandableAdditional.Shadows, k_AdditionalPropertiesState, DrawShadows, DrawShadowsAdditional),
            CED.FoldoutGroup(Styles.postProcessingSettingsText, Expandable.PostProcessing, k_ExpandedState, DrawPostProcessing),
            CED.FoldoutGroup(Styles.volumeSettingsText, Expandable.Volumes, k_ExpandedState, DrawVolumes)
#if ENABLE_ADAPTIVE_PERFORMANCE
            , CED.FoldoutGroup(Styles.adaptivePerformanceText, Expandable.AdaptivePerformance, k_ExpandedState, CED.Group(DrawAdaptivePerformance))
#endif
        );

        static void DrawRendering(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
            if (ownerEditor is not UniversalRenderPipelineAssetEditor urpAssetEditor)
                return;

            EditorGUILayout.Space();
            urpAssetEditor.rendererList.DoLayoutList();

            if (!serialized.asset.ValidateRendererData(-1))
                EditorGUILayout.HelpBox(Styles.rendererMissingDefaultMessage.text, MessageType.Error, true);
            else if (!serialized.asset.ValidateRendererDataList(true))
                EditorGUILayout.HelpBox(Styles.rendererMissingMessage.text, MessageType.Warning, true);
            else if (!ValidateRendererGraphicsAPIs(serialized.asset, out var unsupportedGraphicsApisMessage))
                EditorGUILayout.HelpBox(Styles.rendererUnsupportedAPIMessage.text + unsupportedGraphicsApisMessage, MessageType.Warning, true);

            EditorGUILayout.PropertyField(serialized.requireDepthTextureProp, Styles.requireDepthTextureText);
            DisplayOnTileValidationWarning(serialized.requireDepthTextureProp, p => p.boolValue, Styles.requireDepthTextureText);

            EditorGUILayout.PropertyField(serialized.requireOpaqueTextureProp, Styles.requireOpaqueTextureText);
            DisplayOnTileValidationWarning(serialized.requireOpaqueTextureProp, p => p.boolValue, Styles.requireOpaqueTextureText);

            EditorGUI.BeginDisabledGroup(!serialized.requireOpaqueTextureProp.boolValue);
            EditorGUILayout.PropertyField(serialized.opaqueDownsamplingProp, Styles.opaqueDownsamplingText);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.PropertyField(serialized.supportsTerrainHolesProp, Styles.supportsTerrainHolesText);

            EditorGUILayout.PropertyField(serialized.gpuResidentDrawerMode, Styles.gpuResidentDrawerMode);

            var brgStrippingError = EditorGraphicsSettings.batchRendererGroupShaderStrippingMode != BatchRendererGroupStrippingMode.KeepAll;
            var lightingModeError = !HasCorrectLightingModes(serialized.asset);
            var staticBatchingWarning = PlayerSettings.GetStaticBatchingForPlatform(EditorUserBuildSettings.activeBuildTarget);

            if ((GPUResidentDrawerMode)serialized.gpuResidentDrawerMode.intValue != GPUResidentDrawerMode.Disabled)
            {
                ++EditorGUI.indentLevel;
                serialized.smallMeshScreenPercentage.floatValue = Mathf.Clamp(EditorGUILayout.FloatField(Styles.smallMeshScreenPercentage, serialized.smallMeshScreenPercentage.floatValue), 0.0f, 20.0f);
                EditorGUILayout.PropertyField(serialized.gpuResidentDrawerEnableOcclusionCullingInCameras, Styles.gpuResidentDrawerEnableOcclusionCullingInCameras);
                DisplayOnTileValidationWarning(serialized.gpuResidentDrawerEnableOcclusionCullingInCameras, p => p.boolValue, Styles.gpuResidentDrawerEnableOcclusionCullingInCameras);
                --EditorGUI.indentLevel;

                if (brgStrippingError)
                    EditorGUILayout.HelpBox(Styles.brgShaderStrippingErrorMessage.text, MessageType.Warning, true);
                if (lightingModeError)
                    EditorGUILayout.HelpBox(Styles.lightModeErrorMessage.text, MessageType.Warning, true);
                if (staticBatchingWarning)
                    EditorGUILayout.HelpBox(Styles.staticBatchingInfoMessage.text, MessageType.Info, true);
            }
        }

        private static bool HasCorrectLightingModes(UniversalRenderPipelineAsset asset)
        {
            // Only the URP rendering paths using the cluster light loop (F+ lights & probes) can be used with GRD,
            // since BiRP-style per-object lights and reflection probes are incompatible with DOTS instancing.
            foreach (var rendererData in asset.m_RendererDataList)
            {
                if (rendererData is not UniversalRendererData universalRendererData)
                    return false;

                if (!universalRendererData.usesClusterLightLoop)
                    return false;
            }

            return true;
        }

        static void DrawRenderingAdditional(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
            EditorGUILayout.PropertyField(serialized.srpBatcher, Styles.srpBatcher);
            EditorGUILayout.PropertyField(serialized.supportsDynamicBatching, Styles.dynamicBatching);
            EditorGUILayout.PropertyField(serialized.storeActionsOptimizationProperty, Styles.storeActionsOptimizationText);
        }

        static bool IsAndroidXRTargetted() //Include Quest platform
        {
#if XR_MANAGEMENT_4_0_1_OR_NEWER
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            if (buildTargetGroup != BuildTargetGroup.Android)
                return false;

            var buildTargetSettings = XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
            return buildTargetSettings != null
                && buildTargetSettings.AssignedSettings != null
                && buildTargetSettings.AssignedSettings.activeLoaders.Count > 0;
#else
            return false;
#endif
        }

        static void DrawQuality(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
            DrawHDR(serialized, ownerEditor);

            EditorGUILayout.PropertyField(serialized.msaa, Styles.msaaText);            
            DisplayOnTileValidationWarning(
                serialized.msaa, 
                p => p.intValue != (int)MsaaQuality.Disabled
                    // This operation is actually ok on Quest
                    && !IsAndroidXRTargetted(), 
                Styles.msaaText);

            serialized.renderScale.floatValue = EditorGUILayout.Slider(Styles.renderScaleText, serialized.renderScale.floatValue, UniversalRenderPipeline.minRenderScale, UniversalRenderPipeline.maxRenderScale);
            DisplayOnTileValidationWarning(
                serialized.renderScale, 
                p =>
                {
                    // Duplicating logic from UniversalRenderPipeline.InitializeStackedCameraData
                    const float kRenderScaleThreshold = 0.05f;
                    bool canRequireIntermediateTexture = Mathf.Abs(1.0f - p.floatValue) >= kRenderScaleThreshold;
                    if (!canRequireIntermediateTexture)
                        return false;
                    
                    // This operation is actually ok on Quest
                    return !IsAndroidXRTargetted();
                }, 
                Styles.renderScaleText);

            DrawUpscalingFilterDropdownAndOptions(serialized, ownerEditor);

#if ENABLE_UPSCALER_FRAMEWORK
            bool stpUpscalingSelected = serialized.asset.upscalerName == UniversalRenderPipeline.k_UpscalerName_STP;
            bool fsr1UpscalingSelected = serialized.asset.upscalerName == UniversalRenderPipeline.k_UpscalerName_FSR1;
#else
            bool stpUpscalingSelected = serialized.asset.upscalingFilter == UpscalingFilterSelection.STP;
            bool fsr1UpscalingSelected = serialized.asset.upscalingFilter == UpscalingFilterSelection.FSR;
#endif

            if (serialized.renderScale.floatValue < 1.0f || stpUpscalingSelected || fsr1UpscalingSelected)
            {
                EditorGUILayout.HelpBox("Camera depth isn't supported when Upscaling is turned on in the game view. We will automatically fall back to not doing depth-testing for this pass.", MessageType.Warning, true);
            }

            EditorGUILayout.PropertyField(serialized.enableLODCrossFadeProp, Styles.enableLODCrossFadeText);
            EditorGUI.BeginDisabledGroup(!serialized.enableLODCrossFadeProp.boolValue);
            EditorGUILayout.PropertyField(serialized.lodCrossFadeDitheringTypeProp, Styles.lodCrossFadeDitheringTypeText);
            if (serialized.asset.enableLODCrossFade && serialized.asset.lodCrossFadeDitheringType == LODCrossFadeDitheringType.Stencil)
            {
                var rendererData = serialized.asset.m_RendererDataList[serialized.asset.m_DefaultRendererIndex];
                if (rendererData is UniversalRendererData && ((UniversalRendererData)rendererData).defaultStencilState.overrideStencilState)
                {
                    EditorGUILayout.HelpBox(Styles.stencilLodCrossFadeWarningMessage.text, MessageType.Warning, true);
                }
            }

            EditorGUI.EndDisabledGroup();
        }

        static void DrawUpscalingFilterDropdownAndOptions(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
#if ENABLE_UPSCALER_FRAMEWORK
            // --- 1. Get the available upscaler names ---
            string[] namesArray = null;
            if (UniversalRenderPipeline.upscaling != null)
            {
                // names come in sorted defined by UniversalRenderPipeline.k_UpscalerSortOrder
                namesArray = UniversalRenderPipeline.upscaling.upscalerNames as string[];
            }
            else
            {
                namesArray = Array.Empty<string>();
            }

            // --- 2. Get selected index or fall-back to a safe default ---
            string currentName = serialized.selectedUpscalerName.stringValue;
            int selectedIndex = Array.IndexOf(namesArray, currentName);
            if (selectedIndex == -1)
            {
                selectedIndex = 0; // Default to "Automatic" or "Bilinear"
            }

            // --- 3. Draw the Single Dropdown ---
            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUILayout.Popup(Styles.upscalingFilterText, selectedIndex, namesArray);
            if (EditorGUI.EndChangeCheck())
            {
                // --- 4. Save to the serialzied asset if we change value ---
                serialized.selectedUpscalerName.stringValue = namesArray[selectedIndex];
            }

            DisplayOnTileValidationWarning(serialized.upscalingFilter, p => serialized.selectedUpscalerName.stringValue != UniversalRenderPipeline.k_UpscalerName_Auto, Styles.upscalingFilterText);

            // --- 5. Draw Options per upscaler ---
            string selectedName = namesArray[selectedIndex];
            switch (selectedName)
            {
                // Special-case for FSR1.
                // FSR1 has two passes: Upscaling + Sharpening.
                // IUpscaler framework handles the upscaling part.
                // The sharpening is done in a separate pass from the upscaling (final post),
                // hence we keep the fsrOverrideSharpness & fsrSharpness properties within the
                // URPAsset and render them here under FSR1 upscaler options.
                // This way the user will see it as a single solution for Upscaling+Sharpening, as AMD intended.
                // Typically, the upscaler options are captured by the UpscalerOptions object, excluding this case.
                case UniversalRenderPipeline.k_UpscalerName_FSR1:
                {
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.PropertyField(serialized.fsrOverrideSharpness, Styles.fsrOverrideSharpness);
                    
                    // We put the FSR sharpness override value behind an override checkbox so we can tell when the user intends to use a custom value rather than the default.
                    if (serialized.fsrOverrideSharpness.boolValue)
                    {
                        serialized.fsrSharpness.floatValue = EditorGUILayout.Slider(Styles.fsrSharpnessText, serialized.fsrSharpness.floatValue, 0.0f, 1.0f);
                    }
                    --EditorGUI.indentLevel;
                    break;
                }

                default:
                    // Use options editor of the particular IUpscaler.
                    UpscalerOptions options = serialized.asset.GetUpscalerOptions(selectedName);
                    UniversalRenderPipelineAssetEditor urpEditor = ownerEditor as UniversalRenderPipelineAssetEditor;
                    Editor upscalerOptionsEditor = urpEditor.upscalerOptionsEditorCache.GetOrCreateEditor(options);
                    if (upscalerOptionsEditor != null)
                    {
                        ++EditorGUI.indentLevel;
                        upscalerOptionsEditor.OnInspectorGUI();
                        --EditorGUI.indentLevel;
                    }

                    // Warn users about performance expectations if they attempt to enable STP on a mobile platform
                    if (selectedName == UniversalRenderPipeline.k_UpscalerName_STP && PlatformAutoDetect.isShaderAPIMobileDefined)
                    {
                        EditorGUILayout.HelpBox(Styles.stpMobilePlatformWarning, MessageType.Warning, true);
                    }
                    
                    break;
            }
#else
            // Count builtin upscalers
            int numBuiltInUpscalers = (int)UpscalingFilterSelection.STP + 1;
            int numTotalUpscalers = numBuiltInUpscalers;

            // Create arrays for options and enum values
            string[] names = new string[numTotalUpscalers];

            // Get names and values for builtin upscalers
            {
                var bnames = Enum.GetNames(typeof(UpscalingFilterSelection));

                for (int i = 0; i < numBuiltInUpscalers; i++)
                {
                    // Get the display name from the InspectorName attribute if it exists
                    var field = typeof(UpscalingFilterSelection).GetField(bnames[i]);
                    var inspectorNameAttribute = field.GetCustomAttribute<InspectorNameAttribute>();
                    names[i] = inspectorNameAttribute != null ? inspectorNameAttribute.displayName : bnames[i];
                }
            }

            // Get the current enum value
            UpscalingFilterSelection curUpscaler =
                (UpscalingFilterSelection)serialized.upscalingFilter.enumValueIndex;

            // Find the current selected index
            int selectedIndex = 0;           // [0, iUpscalerCount + BuiltinUpscalerCount)
            {
                selectedIndex = serialized.upscalingFilter.enumValueIndex;
            }

            // --------------------------------------------------- GUI ---------------------------------------------------

            // Show the dropdown
            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUILayout.Popup(Styles.upscalingFilterText, selectedIndex, names);
            if (EditorGUI.EndChangeCheck())
            {
                serialized.upscalingFilter.enumValueIndex = Math.Min(selectedIndex, (int)UpscalingFilterSelection.STP);
            }

            DisplayOnTileValidationWarning(serialized.upscalingFilter, p => p.intValue != (int)UpscalingFilterSelection.Auto, Styles.upscalingFilterText);

            // draw upscaler options, if any
            switch (serialized.asset.upscalingFilter)
            {
                case UpscalingFilterSelection.FSR:
                    {
                        ++EditorGUI.indentLevel;

                        EditorGUILayout.PropertyField(serialized.fsrOverrideSharpness, Styles.fsrOverrideSharpness);

                        // We put the FSR sharpness override value behind an override checkbox so we can tell when the user intends to use a custom value rather than the default.
                        if (serialized.fsrOverrideSharpness.boolValue)
                        {
                            serialized.fsrSharpness.floatValue = EditorGUILayout.Slider(Styles.fsrSharpnessText, serialized.fsrSharpness.floatValue, 0.0f, 1.0f);
                        }

                        --EditorGUI.indentLevel;
                    }
                    break;

                case UpscalingFilterSelection.STP:
                    {
                        // Warn users about performance expectations if they attempt to enable STP on a mobile platform
                        if (PlatformAutoDetect.isShaderAPIMobileDefined)
                        {
                            EditorGUILayout.HelpBox(Styles.stpMobilePlatformWarning, MessageType.Warning, true);
                        }
                    }
                    break;
            }
#endif
        }

        static void DrawHDR(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
            EditorGUILayout.PropertyField(serialized.hdr, Styles.hdrText);
            DisplayOnTileValidationWarning(serialized.hdr, p => p.boolValue, Styles.hdrText);

            // Nested and in-between additional property
            bool additionalProperties = k_ExpandedState[Expandable.Quality] && k_AdditionalPropertiesState[ExpandableAdditional.Quality];
            if (serialized.hdr.boolValue && additionalProperties)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serialized.hdrColorBufferPrecisionProp, Styles.hdrColorBufferPrecisionText);
                EditorGUI.indentLevel--;
            }
        }

        static void DrawLighting(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
            // Main Light
            bool disableGroup = false;
            EditorGUI.BeginDisabledGroup(disableGroup);
            CoreEditorUtils.DrawPopup(Styles.mainLightRenderingModeText, serialized.mainLightRenderingModeProp, Styles.mainLightOptions);
            EditorGUI.EndDisabledGroup();

            EditorGUI.indentLevel++;
            disableGroup |= !serialized.mainLightRenderingModeProp.boolValue;

            EditorGUI.BeginDisabledGroup(disableGroup);
            EditorGUILayout.PropertyField(serialized.mainLightShadowsSupportedProp, Styles.supportsMainLightShadowsText);
            EditorGUI.EndDisabledGroup();

            disableGroup |= !serialized.mainLightShadowsSupportedProp.boolValue;
            EditorGUI.BeginDisabledGroup(disableGroup);
            EditorGUILayout.PropertyField(serialized.mainLightShadowmapResolutionProp, Styles.mainLightShadowmapResolutionText);
            EditorGUI.EndDisabledGroup();

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            // Probe volumes
            EditorGUILayout.PropertyField(serialized.lightProbeSystem, Styles.lightProbeSystemContent);
            if (serialized.lightProbeSystem.intValue == (int)LightProbeSystem.ProbeVolumes)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(serialized.probeVolumeTextureSize, Styles.probeVolumeMemoryBudget);
                EditorGUILayout.PropertyField(serialized.probeVolumeSHBands, Styles.probeVolumeSHBands);

                EditorGUILayout.PropertyField(serialized.supportProbeVolumeGPUStreaming, Styles.supportProbeVolumeGPUStreaming);
                EditorGUI.BeginDisabledGroup(!serialized.supportProbeVolumeGPUStreaming.boolValue);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serialized.supportProbeVolumeDiskStreaming, Styles.supportProbeVolumeDiskStreaming);
                EditorGUI.indentLevel--;
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.PropertyField(serialized.supportProbeVolumeScenarios, Styles.supportProbeVolumeScenarios);
                if (serialized.supportProbeVolumeScenarios.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serialized.supportProbeVolumeScenarioBlending, Styles.supportProbeVolumeScenarioBlending);
                    if (serialized.supportProbeVolumeScenarioBlending.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(serialized.probeVolumeBlendingTextureSize, Styles.probeVolumeBlendingMemoryBudget);
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;
                }

                int estimatedVMemCost = ProbeReferenceVolume.instance.GetVideoMemoryCost();
                if (estimatedVMemCost == 0)
                {
                    EditorGUILayout.HelpBox($"Estimated GPU Memory cost: 0.\nProbe reference volume is not used in the scene and resources haven't been allocated yet.", MessageType.Info, wide: true);
                }
                else
                {
                    EditorGUILayout.HelpBox($"Estimated GPU Memory cost: {estimatedVMemCost / (1000 * 1000)} MB.", MessageType.Info, wide: true);
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            // Additional light
            EditorGUILayout.PropertyField(serialized.additionalLightsRenderingModeProp, Styles.addditionalLightsRenderingModeText);
            EditorGUI.indentLevel++;

            disableGroup = serialized.additionalLightsRenderingModeProp.intValue == (int)LightRenderingMode.Disabled;
            EditorGUI.BeginDisabledGroup(disableGroup);
            serialized.additionalLightsPerObjectLimitProp.intValue = EditorGUILayout.IntSlider(Styles.perObjectLimit, serialized.additionalLightsPerObjectLimitProp.intValue, 0, UniversalRenderPipeline.maxPerObjectLights);
#if UNITY_META_QUEST
            if (serialized.additionalLightsPerObjectLimitProp.intValue > 1)
            {
                EditorGUILayout.HelpBox("When targeting Meta Quest, setting the Per Object Limit to 1 will improve shader performance.", MessageType.Info);
            }
#endif
            EditorGUI.EndDisabledGroup();

            disableGroup |= (serialized.additionalLightsPerObjectLimitProp.intValue == 0 || serialized.additionalLightsRenderingModeProp.intValue != (int)LightRenderingMode.PerPixel);
            EditorGUI.BeginDisabledGroup(disableGroup);
            EditorGUILayout.PropertyField(serialized.additionalLightShadowsSupportedProp, Styles.supportsAdditionalShadowsText);
            EditorGUI.EndDisabledGroup();

            disableGroup |= !serialized.additionalLightShadowsSupportedProp.boolValue;
            EditorGUI.BeginDisabledGroup(disableGroup);
            EditorGUILayout.PropertyField(serialized.additionalLightShadowmapResolutionProp, Styles.additionalLightsShadowmapResolution);
            DrawShadowResolutionTierSettings(serialized, ownerEditor);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            disableGroup = serialized.additionalLightsRenderingModeProp.intValue == (int)LightRenderingMode.Disabled || !serialized.supportsLightCookies.boolValue;

            EditorGUI.BeginDisabledGroup(disableGroup);
            EditorGUILayout.PropertyField(serialized.additionalLightCookieResolutionProp, Styles.additionalLightsCookieResolution);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(disableGroup);
            EditorGUILayout.PropertyField(serialized.additionalLightCookieFormatProp, Styles.additionalLightsCookieFormat);
            EditorGUI.EndDisabledGroup();

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            // Reflection Probes
            EditorGUILayout.LabelField(Styles.reflectionProbesSettingsText);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serialized.reflectionProbeBlendingProp, Styles.reflectionProbeBlendingText);
            EditorGUI.indentLevel++;
            EditorGUI.BeginDisabledGroup(!serialized.reflectionProbeBlendingProp.boolValue);
            EditorGUILayout.PropertyField(serialized.reflectionProbeAtlasProp, Styles.reflectionProbeAtlasText);
            EditorGUI.EndDisabledGroup();

            // Disable probeAtlas when probeBlending is off.
            if (!serialized.reflectionProbeBlendingProp.boolValue)
                serialized.reflectionProbeAtlasProp.boolValue = false;

            if ((GPUResidentDrawerMode)serialized.gpuResidentDrawerMode.intValue != GPUResidentDrawerMode.Disabled)
            {
                if (!serialized.reflectionProbeBlendingProp.boolValue || !serialized.reflectionProbeAtlasProp.boolValue)
                    EditorGUILayout.HelpBox(Styles.reflectionProbeBlendingGpuResidentDrawerWarningText.text, MessageType.Warning, true);
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.PropertyField(serialized.reflectionProbeBoxProjectionProp, Styles.reflectionProbeBoxProjectionText);

            EditorGUI.indentLevel--;
        }

        static void DrawLightingAdditional(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
            EditorGUILayout.PropertyField(serialized.mixedLightingSupportedProp, Styles.mixedLightingSupportLabel);
            EditorGUILayout.PropertyField(serialized.useRenderingLayers, Styles.useRenderingLayers);
            EditorGUILayout.PropertyField(serialized.supportsLightCookies, Styles.supportsLightCookies);
            EditorGUILayout.PropertyField(serialized.shEvalModeProp, Styles.shEvalModeText);

            if (serialized.useRenderingLayers.boolValue && !ValidateRendererGraphicsAPIsForLightLayers(serialized.asset, out var unsupportedGraphicsApisMessage))
                EditorGUILayout.HelpBox(Styles.lightlayersUnsupportedMessage.text + unsupportedGraphicsApisMessage, MessageType.Warning, true);
        }

        static void DrawShadowResolutionTierSettings(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
            // UI code adapted from HDRP U.I logic implemented in com.unity.render-pipelines.high-definition/Editor/RenderPipeline/Settings/SerializedScalableSetting.cs )

            var rect = GUILayoutUtility.GetRect(0, float.Epsilon, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight);
            var contentRect = EditorGUI.PrefixLabel(rect, Styles.additionalLightsShadowResolutionTiers);

            EditorGUI.BeginChangeCheck();

            const int k_ShadowResolutionTiersCount = 3;
            var values = new[] { serialized.additionalLightsShadowResolutionTierLowProp, serialized.additionalLightsShadowResolutionTierMediumProp, serialized.additionalLightsShadowResolutionTierHighProp };

            var num = contentRect.width / (float)k_ShadowResolutionTiersCount;  // space allocated for every field including the label

            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0; // Reset the indentation

            float pixelShift = 0;  // Variable to keep track of the current pixel shift in the rectangle we were assigned for this whole section.
            for (var index = 0; index < k_ShadowResolutionTiersCount; ++index)
            {
                var labelWidth = Mathf.Clamp(EditorStyles.label.CalcSize(Styles.additionalLightsShadowResolutionTierNames[index]).x, 0, num);
                EditorGUI.LabelField(new Rect(contentRect.x + pixelShift, contentRect.y, labelWidth, contentRect.height), Styles.additionalLightsShadowResolutionTierNames[index]);
                pixelShift += labelWidth;           // We need to remove from the position the label size that we've just drawn and shift by it's length
                float spaceLeft = num - labelWidth; // The amount of space left for the field
                if (spaceLeft > 2) // If at least two pixels are left to draw this field, draw it, otherwise, skip
                {
                    var fieldSlot = new Rect(contentRect.x + pixelShift, contentRect.y, num - labelWidth, contentRect.height); // Define the rectangle for the field
                    int value = EditorGUI.DelayedIntField(fieldSlot, values[index].intValue);
                    values[index].intValue = Mathf.Max(UniversalAdditionalLightData.AdditionalLightsShadowMinimumResolution, Mathf.NextPowerOfTwo(value));
                }
                pixelShift += spaceLeft;  // Shift by the slot that was left for the field
            }

            EditorGUI.indentLevel = indentLevel;

            EditorGUI.EndChangeCheck();
        }

        static void DrawShadows(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
            serialized.shadowDistanceProp.floatValue = Mathf.Max(0.0f, EditorGUILayout.FloatField(Styles.shadowDistanceText, serialized.shadowDistanceProp.floatValue));
            EditorUtils.Unit unit = EditorUtils.Unit.Metric;
            if (serialized.shadowCascadeCountProp.intValue != 0)
            {
                EditorGUI.BeginChangeCheck();
                unit = (EditorUtils.Unit)EditorGUILayout.EnumPopup(Styles.shadowWorkingUnitText, serialized.state.value);
                if (EditorGUI.EndChangeCheck())
                {
                    serialized.state.value = unit;
                }
            }

            EditorGUILayout.IntSlider(serialized.shadowCascadeCountProp, UniversalRenderPipelineAsset.k_ShadowCascadeMinCount, UniversalRenderPipelineAsset.k_ShadowCascadeMaxCount, Styles.shadowCascadesText);

            int cascadeCount = serialized.shadowCascadeCountProp.intValue;
            EditorGUI.indentLevel++;

            bool useMetric = unit == EditorUtils.Unit.Metric;
            float baseMetric = serialized.shadowDistanceProp.floatValue;
            int cascadeSplitCount = cascadeCount - 1;

            DrawCascadeSliders(serialized, cascadeSplitCount, useMetric, baseMetric);

            EditorGUI.indentLevel--;
            DrawCascades(serialized, cascadeCount, useMetric, baseMetric);
            EditorGUI.indentLevel++;

            serialized.shadowDepthBiasProp.floatValue = EditorGUILayout.Slider(Styles.shadowDepthBias, serialized.shadowDepthBiasProp.floatValue, 0.0f, UniversalRenderPipeline.maxShadowBias);
            serialized.shadowNormalBiasProp.floatValue = EditorGUILayout.Slider(Styles.shadowNormalBias, serialized.shadowNormalBiasProp.floatValue, 0.0f, UniversalRenderPipeline.maxShadowBias);
            EditorGUILayout.PropertyField(serialized.softShadowsSupportedProp, Styles.supportsSoftShadows);
            if (serialized.softShadowsSupportedProp.boolValue)
            {
                EditorGUI.indentLevel++;
                    DrawShadowsSoftShadowQuality(serialized, ownerEditor);
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
        }

        static void DrawShadowsSoftShadowQuality(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
            int selectedAssetSoftShadowQuality = serialized.softShadowQualityProp.intValue;
            Rect r = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(r, Styles.softShadowsQuality, serialized.softShadowQualityProp);
            {
                using (var checkScope = new EditorGUI.ChangeCheckScope())
                {
                    selectedAssetSoftShadowQuality = EditorGUI.IntPopup(r, Styles.softShadowsQuality, selectedAssetSoftShadowQuality, Styles.softShadowsQualityAssetOptions, Styles.softShadowsQualityAssetValues);
                    if (checkScope.changed)
                    {
                        serialized.softShadowQualityProp.intValue = Math.Clamp(selectedAssetSoftShadowQuality, (int)SoftShadowQuality.Low, (int)SoftShadowQuality.High);
                    }
                }
            }
            EditorGUI.EndProperty();
        }

        static void DrawShadowsAdditional(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
            EditorGUILayout.PropertyField(serialized.conservativeEnclosingSphereProp, Styles.conservativeEnclosingSphere);
        }

        static void DrawCascadeSliders(SerializedUniversalRenderPipelineAsset serialized, int splitCount, bool useMetric, float baseMetric)
        {
            Vector4 shadowCascadeSplit = Vector4.one;
            if (splitCount == 3)
                shadowCascadeSplit = new Vector4(serialized.shadowCascade4SplitProp.vector3Value.x, serialized.shadowCascade4SplitProp.vector3Value.y, serialized.shadowCascade4SplitProp.vector3Value.z, 1);
            else if (splitCount == 2)
                shadowCascadeSplit = new Vector4(serialized.shadowCascade3SplitProp.vector2Value.x, serialized.shadowCascade3SplitProp.vector2Value.y, 1, 0);
            else if (splitCount == 1)
                shadowCascadeSplit = new Vector4(serialized.shadowCascade2SplitProp.floatValue, 1, 0, 0);

            float splitBias = 0.001f;
            float invBaseMetric = baseMetric == 0 ? 0 : 1f / baseMetric;

            // Ensure correct split order
            shadowCascadeSplit[0] = Mathf.Clamp(shadowCascadeSplit[0], 0f, shadowCascadeSplit[1] - splitBias);
            shadowCascadeSplit[1] = Mathf.Clamp(shadowCascadeSplit[1], shadowCascadeSplit[0] + splitBias, shadowCascadeSplit[2] - splitBias);
            shadowCascadeSplit[2] = Mathf.Clamp(shadowCascadeSplit[2], shadowCascadeSplit[1] + splitBias, shadowCascadeSplit[3] - splitBias);


            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < splitCount; ++i)
            {
                float value = shadowCascadeSplit[i];

                float minimum = i == 0 ? 0 : shadowCascadeSplit[i - 1] + splitBias;
                float maximum = i == splitCount - 1 ? 1 : shadowCascadeSplit[i + 1] - splitBias;

                if (useMetric)
                {
                    float valueMetric = value * baseMetric;
                    valueMetric = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent($"Split {i + 1}", "The distance where this cascade ends and the next one starts."), valueMetric, 0f, baseMetric, null);

                    shadowCascadeSplit[i] = Mathf.Clamp(valueMetric * invBaseMetric, minimum, maximum);
                }
                else
                {
                    float valueProcentage = value * 100f;
                    valueProcentage = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent($"Split {i + 1}", "The distance where this cascade ends and the next one starts."), valueProcentage, 0f, 100f, null);

                    shadowCascadeSplit[i] = Mathf.Clamp(valueProcentage * 0.01f, minimum, maximum);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                switch (splitCount)
                {
                    case 3:
                        serialized.shadowCascade4SplitProp.vector3Value = shadowCascadeSplit;
                        break;
                    case 2:
                        serialized.shadowCascade3SplitProp.vector2Value = shadowCascadeSplit;
                        break;
                    case 1:
                        serialized.shadowCascade2SplitProp.floatValue = shadowCascadeSplit.x;
                        break;
                }
            }

            var borderValue = serialized.shadowCascadeBorderProp.floatValue;

            EditorGUI.BeginChangeCheck();
            if (useMetric)
            {
                var lastCascadeSplitSize = splitCount == 0 ? baseMetric : (1.0f - shadowCascadeSplit[splitCount - 1]) * baseMetric;
                var invLastCascadeSplitSize = lastCascadeSplitSize == 0 ? 0 : 1f / lastCascadeSplitSize;
                float valueMetric = borderValue * lastCascadeSplitSize;
                valueMetric = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent("Last Border", "The distance of the last cascade."), valueMetric, 0f, lastCascadeSplitSize, null);

                borderValue = valueMetric * invLastCascadeSplitSize;
            }
            else
            {
                float valueProcentage = borderValue * 100f;
                valueProcentage = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent("Last Border", "The distance of the last cascade."), valueProcentage, 0f, 100f, null);

                borderValue = valueProcentage * 0.01f;
            }

            if (EditorGUI.EndChangeCheck())
            {
                serialized.shadowCascadeBorderProp.floatValue = borderValue;
            }
        }

        static void DrawCascades(SerializedUniversalRenderPipelineAsset serialized, int cascadeCount, bool useMetric, float baseMetric)
        {
            var cascades = new ShadowCascadeGUI.Cascade[cascadeCount];

            Vector3 shadowCascadeSplit = Vector3.zero;
            if (cascadeCount == 4)
                shadowCascadeSplit = serialized.shadowCascade4SplitProp.vector3Value;
            else if (cascadeCount == 3)
                shadowCascadeSplit = serialized.shadowCascade3SplitProp.vector2Value;
            else if (cascadeCount == 2)
                shadowCascadeSplit.x = serialized.shadowCascade2SplitProp.floatValue;
            else
                shadowCascadeSplit.x = serialized.shadowCascade2SplitProp.floatValue;

            float lastCascadePartitionSplit = 0;
            for (int i = 0; i < cascadeCount - 1; ++i)
            {
                cascades[i] = new ShadowCascadeGUI.Cascade()
                {
                    size = i == 0 ? shadowCascadeSplit[i] : shadowCascadeSplit[i] - lastCascadePartitionSplit, // Calculate the size of cascade
                    borderSize = 0,
                    cascadeHandleState = ShadowCascadeGUI.HandleState.Enabled,
                    borderHandleState = ShadowCascadeGUI.HandleState.Hidden,
                };
                lastCascadePartitionSplit = shadowCascadeSplit[i];
            }

            // Last cascade is special
            var lastCascade = cascadeCount - 1;
            cascades[lastCascade] = new ShadowCascadeGUI.Cascade()
            {
                size = lastCascade == 0 ? 1.0f : 1 - shadowCascadeSplit[lastCascade - 1], // Calculate the size of cascade
                borderSize = serialized.shadowCascadeBorderProp.floatValue,
                cascadeHandleState = ShadowCascadeGUI.HandleState.Hidden,
                borderHandleState = ShadowCascadeGUI.HandleState.Enabled,
            };

            EditorGUI.BeginChangeCheck();
            ShadowCascadeGUI.DrawCascades(ref cascades, useMetric, baseMetric);
            if (EditorGUI.EndChangeCheck())
            {
                if (cascadeCount == 4)
                    serialized.shadowCascade4SplitProp.vector3Value = new Vector3(
                        cascades[0].size,
                        cascades[0].size + cascades[1].size,
                        cascades[0].size + cascades[1].size + cascades[2].size
                    );
                else if (cascadeCount == 3)
                    serialized.shadowCascade3SplitProp.vector2Value = new Vector2(
                        cascades[0].size,
                        cascades[0].size + cascades[1].size
                    );
                else if (cascadeCount == 2)
                    serialized.shadowCascade2SplitProp.floatValue = cascades[0].size;

                serialized.shadowCascadeBorderProp.floatValue = cascades[lastCascade].borderSize;
            }
        }

        static void DrawPostProcessing(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
            DisplayOnTileValidationWarningForPostProcessingSection(Styles.postProcessingSettingsText);

            EditorGUILayout.PropertyField(serialized.colorGradingMode, Styles.colorGradingMode);
            bool isHdrOn = serialized.hdr.boolValue;
            if (!isHdrOn && serialized.colorGradingMode.intValue == (int)ColorGradingMode.HighDynamicRange)
                EditorGUILayout.HelpBox(Styles.colorGradingModeWarning, MessageType.Warning);
            else if (isHdrOn && serialized.colorGradingMode.intValue == (int)ColorGradingMode.HighDynamicRange)
                EditorGUILayout.HelpBox(Styles.colorGradingModeSpecInfo, MessageType.Info);
            else if (isHdrOn && PlayerSettings.allowHDRDisplaySupport && serialized.colorGradingMode.intValue == (int)ColorGradingMode.LowDynamicRange)
                EditorGUILayout.HelpBox(Styles.colorGradingModeWithHDROutput, MessageType.Warning);

            EditorGUILayout.DelayedIntField(serialized.colorGradingLutSize, Styles.colorGradingLutSize);
            serialized.colorGradingLutSize.intValue = Mathf.Clamp(serialized.colorGradingLutSize.intValue, UniversalRenderPipelineAsset.k_MinLutSize, UniversalRenderPipelineAsset.k_MaxLutSize);
            if (isHdrOn && serialized.colorGradingMode.intValue == (int)ColorGradingMode.HighDynamicRange && serialized.colorGradingLutSize.intValue < 32)
                EditorGUILayout.HelpBox(Styles.colorGradingLutSizeWarning, MessageType.Warning);

            HDRColorBufferPrecision hdrPrecision = (HDRColorBufferPrecision)serialized.hdrColorBufferPrecisionProp.intValue;
            bool alphaEnabled = !isHdrOn /*RGBA8*/ || (isHdrOn && hdrPrecision == HDRColorBufferPrecision._64Bits); /*RGBA16Float*/
            EditorGUILayout.PropertyField(serialized.allowPostProcessAlphaOutput, Styles.allowPostProcessAlphaOutput);
            if(!alphaEnabled && serialized.allowPostProcessAlphaOutput.boolValue)
                EditorGUILayout.HelpBox(Styles.alphaOutputWarning, MessageType.Warning);

            EditorGUILayout.PropertyField(serialized.useFastSRGBLinearConversion, Styles.useFastSRGBLinearConversion);
            EditorGUILayout.PropertyField(serialized.supportDataDrivenLensFlare, Styles.supportDataDrivenLensFlare);
            EditorGUILayout.PropertyField(serialized.supportScreenSpaceLensFlare, Styles.supportScreenSpaceLensFlare);
        }

        static Editor s_VolumeProfileEditor;
        static void DrawVolumes(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
            CoreEditorUtils.DrawPopup(Styles.volumeFrameworkUpdateMode, serialized.volumeFrameworkUpdateModeProp, Styles.volumeFrameworkUpdateOptions);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serialized.volumeProfileProp, Styles.volumeProfileLabel);
            var profile = serialized.volumeProfileProp.objectReferenceValue as VolumeProfile;
            if (EditorGUI.EndChangeCheck() && UniversalRenderPipeline.asset == serialized.serializedObject.targetObject && RenderPipelineManager.currentPipeline is UniversalRenderPipeline)
                VolumeManager.instance.SetQualityDefaultProfile(serialized.volumeProfileProp.objectReferenceValue as VolumeProfile);

            var contextMenuButtonRect = GUILayoutUtility.GetRect(CoreEditorStyles.contextMenuIcon,
                Styles.volumeProfileContextMenuStyle.Value);
            if (GUI.Button(contextMenuButtonRect, CoreEditorStyles.contextMenuIcon,
                    Styles.volumeProfileContextMenuStyle.Value))
            {
                var profileEditor = s_VolumeProfileEditor as VolumeProfileEditor;
                var componentEditors = profileEditor != null ? profileEditor.componentList.editors : null;
                var srpAsset = serialized.serializedObject.targetObject as UniversalRenderPipelineAsset;
                var pos = new Vector2(contextMenuButtonRect.x, contextMenuButtonRect.yMax);
                VolumeProfileUtils.OnVolumeProfileContextClick(pos, srpAsset.volumeProfile, componentEditors,
                    overrideStateOnReset: false,
                    defaultVolumeProfilePath: $"Assets/{srpAsset.name}_VolumeProfile.asset",
                    onNewVolumeProfileCreated: volumeProfile =>
                    {
                        Undo.RecordObject(srpAsset, "Set UniversalRenderPipelineAsset Volume Profile");
                        srpAsset.volumeProfile = volumeProfile;
                        if (UniversalRenderPipeline.asset == srpAsset)
                            VolumeManager.instance.SetQualityDefaultProfile(volumeProfile);
                        EditorUtility.SetDirty(srpAsset);
                    });
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);

            if (profile != null)
            {
                Editor.CreateCachedEditor(profile, typeof(VolumeProfileEditor), ref s_VolumeProfileEditor);
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

#if ENABLE_ADAPTIVE_PERFORMANCE
        static void DrawAdaptivePerformance(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
            EditorGUILayout.PropertyField(serialized.useAdaptivePerformance, Styles.useAdaptivePerformance);
        }
#endif
        
        struct OnTileValidationInfos
        {
            public bool enabled => !string.IsNullOrEmpty(formatter);
            public readonly string formatter;
            public readonly string rendererNames;
            public readonly string rendererNamesWithPostProcess;

            public OnTileValidationInfos(string formatter, string rendererNames, string rendererNamesWithPostProcess)
            {
                this.formatter = formatter;
                this.rendererNames = rendererNames;
                this.rendererNamesWithPostProcess = rendererNamesWithPostProcess;
            }
        }

        static OnTileValidationInfos lastOnTileValidationInfos; //prevent computing this multiple time for this ImGUI frame

        static void PrepareOnTileValidationWarning(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
            // Rules:
            //   - mono selection:
            //      - only 1 Renderer in the list: Display warning if RendererData's OnTileValidation is enabled
            //      - many Renderers in the list: Display warning with list of RendererData's where OnTileValidation is enabled
            //   - multi selection:
            //      - compute the list interection of RendererDatas where OnTileValidation is enabled amongst all URPAsset in selection
            //      - If list is not empty, display warning with this list. Additionaly specify for item iden due to being at different position
            //   - Additionally for both, for Post Processing section: only show names where Post Processing is enabled

            lastOnTileValidationInfos = default;
            
            // If impacted section are not opened, early exit
            if (!(k_ExpandedState[Expandable.Rendering] || k_ExpandedState[Expandable.Quality] || k_ExpandedState[Expandable.PostProcessing]))
                return;

            // Helper to iterate property of an array quickly (without GetArrayElementAtIndex)
            IEnumerable<SerializedProperty> ArrayElementPropertyEnumerator(SerializedProperty property)
            {
                if (!property.hasVisibleChildren)
                    yield break;

                var iterator = property.Copy();
                var end = iterator.GetEndProperty();

                // Move to the first child property
                iterator.NextVisible(enterChildren: true);
                iterator.NextVisible(enterChildren: false); //skip size

                while (!SerializedProperty.EqualContents(iterator, end))
                {
                    yield return iterator;
                    iterator.NextVisible(enterChildren: false);
                } 
            }
            
            // Helper to filter the list and get only unique result of UniversalRendererData that have the OnTileValidation.
            // The returned IDisposable is for being able to return the HashSet to the pool when Dispose is call like at end of Using. 
            IDisposable SelectUniqueAndCast(IEnumerable<SerializedProperty> properties, out HashSet<UniversalRendererData> uniques)
            {
                var e = properties.GetEnumerator();
                var disposer = HashSetPool<UniversalRendererData>.Get(out uniques);
                while (e.MoveNext())
                    if (!e.Current.hasMultipleDifferentValues 
                        && e.Current.boxedValue is UniversalRendererData universalData 
                        && universalData.onTileValidation)
                        uniques.Add(universalData);
                return disposer;
            }
            
            // Additional select to filter the one that have PostProcessing enabled.
            IEnumerable<UniversalRendererData> WherePostProcessingEnabled(IEnumerable<UniversalRendererData> renderer)
            {
                var e = renderer.GetEnumerator();
                while (e.MoveNext())
                    if (e.Current.postProcessData != null)
                        yield return e.Current;
            }

            // Helper to draw the name in the collection as a string, between '' and with a coma separator 
            string ListElementNames(IEnumerable<UniversalRendererData> collection, string suffix = "")
            {
                var e = collection.GetEnumerator();
                if (!e.MoveNext())
                    return string.Empty;

                string GetName(IEnumerator<UniversalRendererData> e)
                    => $"'{e.Current.name}'{suffix}";

                string last = GetName(e);
                if (!e.MoveNext())
                    return last;

                using var o = StringBuilderPool.Get(out var sb);
                do
                {
                    sb.Append(last);
                    last = $", {GetName(e)}";
                }
                while (e.MoveNext());
                sb.Append(last);

                return sb.ToString();
            }
            
            // Helper for multiple selection to distinguish element that remain at stable position (in the selection) from others
            string ConcatCollectionInName(IEnumerable<UniversalRendererData> rightlyPositioned, IEnumerable<UniversalRendererData> wronglyPositioned)
            {
                var firstPart = ListElementNames(rightlyPositioned);
                var secondPart = ListElementNames(wronglyPositioned, Styles.suffixWhenDifferentPositionOnTileValidation);
                if (string.IsNullOrEmpty(firstPart))
                    return secondPart;
                if (string.IsNullOrEmpty(secondPart)) 
                    return firstPart;
                return $"{firstPart}, {secondPart}";
            }

            string names = null;
            string namesWithPostProcess = null;
            if (!serialized.rendererDatas.hasMultipleDifferentValues)
            {
                // Simple case: all element selected share the same list.
                // Minimize the operation alongs foldout opened
                using (SelectUniqueAndCast(ArrayElementPropertyEnumerator(serialized.rendererDatas), out var renderers))
                {
                    if (renderers.Count == 0)
                        return;

                    if (k_ExpandedState[Expandable.Rendering] || k_ExpandedState[Expandable.Quality])
                        names = ListElementNames(renderers);

                    if (k_ExpandedState[Expandable.PostProcessing])
                        namesWithPostProcess = ListElementNames(WherePostProcessingEnabled(renderers));
                
                    lastOnTileValidationInfos = new OnTileValidationInfos(
                        serialized.rendererDatas.arraySize == 1 ? Styles.formatterOnTileValidationOneRenderer : Styles.formatterOnTileValidationMultipleRenderer,
                        names,
                        namesWithPostProcess);
                    return;
                }
            }

            // Complex case: the renderer list is different in some elements of the selection

            // Let's compute the intersection of each RendererList where it is a UniversalRenderer with On-Tile Validation enabled.
            // If the intersection is empty, it would means no RendererData validate the criteria so we early exit.

            // We can retrieve element at stable position by directly checking the serialization of the selection.
            // Elements in the intersection that are not in the stable positio list are elements shared in all list but with moving index.


            // Helper to build the HashSet of UniversalRenderer that have OnTileValidation on one targeted asset.
            // The returned IDisposable is for being able to return the HashSet to the pool when Dispose is call like at end of Using.
            IDisposable GetUniversalRendererWithOnTileValidationEnabled(UniversalRenderPipelineAsset asset, out HashSet<UniversalRendererData> set)
            {
                IDisposable disposer = HashSetPool<UniversalRendererData>.Get(out set);
                for (int rendererIndex = 0; rendererIndex < asset.rendererDataList.Length; ++rendererIndex)
                    if (asset.rendererDataList[rendererIndex] is UniversalRendererData universalData && universalData.onTileValidation)
                        set.Add(universalData);
                return disposer;
            }

            using (GetUniversalRendererWithOnTileValidationEnabled((UniversalRenderPipelineAsset)serialized.serializedObject.targetObjects[0], out var movingPositions))
            {
                if (movingPositions.Count == 0)
                    return;

                for (int i = 1; i < serialized.serializedObject.targetObjects.Length; ++i)
                    using (GetUniversalRendererWithOnTileValidationEnabled((UniversalRenderPipelineAsset)serialized.serializedObject.targetObjects[i], out var otherIntersection))
                    {
                        if (otherIntersection.Count == 0)
                            return;
                        movingPositions.IntersectWith(otherIntersection);
                        if (movingPositions.Count == 0)
                            return;
                    }

                using (SelectUniqueAndCast(ArrayElementPropertyEnumerator(serialized.rendererDatas), out var stablePositions))
                {
                    foreach (var stablePositionElement in stablePositions)
                        movingPositions.Remove(stablePositionElement);

                    if (k_ExpandedState[Expandable.Rendering] || k_ExpandedState[Expandable.Quality])
                        names = ConcatCollectionInName(stablePositions, movingPositions);
                        
                    if (k_ExpandedState[Expandable.PostProcessing])
                        namesWithPostProcess = ConcatCollectionInName(WherePostProcessingEnabled(stablePositions), WherePostProcessingEnabled(movingPositions));
                }

                lastOnTileValidationInfos = new OnTileValidationInfos(Styles.formatterOnTileValidationMultipleRenderer, names, namesWithPostProcess);
            }
        }

        static void DisplayOnTileValidationWarning(SerializedProperty prop, Func<SerializedProperty, bool> shouldDisplayWarning, GUIContent label = null)
        {
            if (prop == null 
                || shouldDisplayWarning == null 
                || !lastOnTileValidationInfos.enabled
                || !shouldDisplayWarning(prop))
                return;

            EditorGUILayout.HelpBox(
                string.Format(lastOnTileValidationInfos.formatter, label == null ? prop.displayName : label.text, lastOnTileValidationInfos.rendererNames), 
                MessageType.Warning);
        }
        
        //variant for a whole section such as post processing
        static void DisplayOnTileValidationWarningForPostProcessingSection(GUIContent label)
        {
            if (label == null || !lastOnTileValidationInfos.enabled || string.IsNullOrEmpty(lastOnTileValidationInfos.rendererNamesWithPostProcess))
                return;
            
            EditorGUILayout.HelpBox(
                string.Format(lastOnTileValidationInfos.formatter, label.text, lastOnTileValidationInfos.rendererNamesWithPostProcess), 
                MessageType.Warning);
        }
    }
}
