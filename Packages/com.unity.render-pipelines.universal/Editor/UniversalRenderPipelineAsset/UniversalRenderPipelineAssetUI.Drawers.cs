using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    using CED = CoreEditorDrawer<SerializedUniversalRenderPipelineAsset>;

    internal partial class UniversalRenderPipelineAssetUI
    {
        internal enum Expandable
        {
            Rendering = 1 << 1,
            Quality = 1 << 2,
            Lighting = 1 << 3,
            Shadows = 1 << 4,
            PostProcessing = 1 << 5,
#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
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
            CED.AdditionalPropertiesFoldoutGroup(Styles.renderingSettingsText, Expandable.Rendering, k_ExpandedState, ExpandableAdditional.Rendering, k_AdditionalPropertiesState, DrawRendering, DrawRenderingAdditional),
            CED.FoldoutGroup(Styles.qualitySettingsText, Expandable.Quality, k_ExpandedState, DrawQuality),
            CED.AdditionalPropertiesFoldoutGroup(Styles.lightingSettingsText, Expandable.Lighting, k_ExpandedState, ExpandableAdditional.Lighting, k_AdditionalPropertiesState, DrawLighting, DrawLightingAdditional),
            CED.AdditionalPropertiesFoldoutGroup(Styles.shadowSettingsText, Expandable.Shadows, k_ExpandedState, ExpandableAdditional.Shadows, k_AdditionalPropertiesState, DrawShadows, DrawShadowsAdditional),
            CED.FoldoutGroup(Styles.postProcessingSettingsText, Expandable.PostProcessing, k_ExpandedState, DrawPostProcessing),
            CED.FoldoutGroup(Styles.volumeSettingsText, Expandable.Volumes, k_ExpandedState, DrawVolumes)
#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
            , CED.FoldoutGroup(Styles.adaptivePerformanceText, Expandable.AdaptivePerformance, k_ExpandedState, CED.Group(DrawAdaptivePerformance))
#endif
        );

        static void DrawRendering(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
            if (ownerEditor is UniversalRenderPipelineAssetEditor urpAssetEditor)
            {
                EditorGUILayout.Space();
                urpAssetEditor.rendererList.DoLayoutList();

                if (!serialized.asset.ValidateRendererData(-1))
                    EditorGUILayout.HelpBox(Styles.rendererMissingDefaultMessage.text, MessageType.Error, true);
                else if (!serialized.asset.ValidateRendererDataList(true))
                    EditorGUILayout.HelpBox(Styles.rendererMissingMessage.text, MessageType.Warning, true);
                else if (!ValidateRendererGraphicsAPIs(serialized.asset, out var unsupportedGraphicsApisMessage))
                    EditorGUILayout.HelpBox(Styles.rendererUnsupportedAPIMessage.text + unsupportedGraphicsApisMessage, MessageType.Warning, true);

                EditorGUILayout.PropertyField(serialized.requireDepthTextureProp, Styles.requireDepthTextureText);
                EditorGUILayout.PropertyField(serialized.requireOpaqueTextureProp, Styles.requireOpaqueTextureText);
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
                    --EditorGUI.indentLevel;

                    if (brgStrippingError)
                        EditorGUILayout.HelpBox(Styles.brgShaderStrippingErrorMessage.text, MessageType.Warning, true);
                    if (lightingModeError)
                        EditorGUILayout.HelpBox(Styles.lightModeErrorMessage.text, MessageType.Warning, true);
                    if (staticBatchingWarning)
                        EditorGUILayout.HelpBox(Styles.staticBatchingInfoMessage.text, MessageType.Info, true);
                    if (serialized.gpuResidentDrawerEnableOcclusionCullingInCameras.boolValue && GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode)
                        EditorGUILayout.HelpBox(Styles.renderGraphNotEnabledErrorMessage.text, MessageType.Info, true);
                }
            }
        }

        private static bool HasCorrectLightingModes(UniversalRenderPipelineAsset asset)
        {
            foreach (var rendererData in asset.m_RendererDataList)
            {
                if (rendererData is not UniversalRendererData { renderingMode: RenderingMode.ForwardPlus })
                    return false;
            }
            return true;
        }

        static void DrawRenderingAdditional(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
            EditorGUILayout.PropertyField(serialized.srpBatcher, Styles.srpBatcher);
            EditorGUILayout.PropertyField(serialized.supportsDynamicBatching, Styles.dynamicBatching);
            EditorGUILayout.PropertyField(serialized.debugLevelProp, Styles.debugLevel);
            EditorGUILayout.PropertyField(serialized.storeActionsOptimizationProperty, Styles.storeActionsOptimizationText);
        }

        static void DrawQuality(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
            DrawHDR(serialized, ownerEditor);

            EditorGUILayout.PropertyField(serialized.msaa, Styles.msaaText);
            serialized.renderScale.floatValue = EditorGUILayout.Slider(Styles.renderScaleText, serialized.renderScale.floatValue, UniversalRenderPipeline.minRenderScale, UniversalRenderPipeline.maxRenderScale);
            EditorGUILayout.PropertyField(serialized.upscalingFilter, Styles.upscalingFilterText);
            if (serialized.asset.upscalingFilter == UpscalingFilterSelection.FSR)
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
            else if (serialized.asset.upscalingFilter == UpscalingFilterSelection.STP)
            {
                // Warn users if they attempt to enable STP without render graph
                if (GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode)
                {
                    EditorGUILayout.HelpBox(Styles.stpRequiresRenderGraph, MessageType.Warning, true);
                }

                // Warn users about performance expectations if they attempt to enable STP on a mobile platform
                if (PlatformAutoDetect.isShaderAPIMobileDefined)
                {
                    EditorGUILayout.HelpBox(Styles.stpMobilePlatformWarning, MessageType.Warning, true);
                }
            }
            EditorGUILayout.PropertyField(serialized.enableLODCrossFadeProp, Styles.enableLODCrossFadeText);
            EditorGUI.BeginDisabledGroup(!serialized.enableLODCrossFadeProp.boolValue);
            EditorGUILayout.PropertyField(serialized.lodCrossFadeDitheringTypeProp, Styles.lodCrossFadeDitheringTypeText);
            EditorGUI.EndDisabledGroup();
        }

        static void DrawHDR(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
            EditorGUILayout.PropertyField(serialized.hdr, Styles.hdrText);

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

#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
        static void DrawAdaptivePerformance(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
            EditorGUILayout.PropertyField(serialized.useAdaptivePerformance, Styles.useAdaptivePerformance);
        }
#endif
    }
}
