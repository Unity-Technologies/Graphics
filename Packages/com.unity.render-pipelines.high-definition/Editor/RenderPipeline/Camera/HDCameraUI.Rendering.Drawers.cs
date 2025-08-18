using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDCamera>;

    static partial class HDCameraUI
    {
        partial class Rendering
        {
            enum AdditionalProperties
            {
                Rendering = 1 << 5,
            }
            readonly static AdditionalPropertiesState<AdditionalProperties, HDAdditionalCameraData> k_AdditionalPropertiesState = new AdditionalPropertiesState<AdditionalProperties, HDAdditionalCameraData>(0, "HDRP");
            static bool s_IsRunningTAAU = false;


            public static readonly CED.IDrawer RenderingDrawer = CED.Group(
                CED.Group(
                    CED.Group(Drawer_Rendering_AllowDynamicResolution),
                    CED.Conditional(
                        (serialized, owner) => serialized.allowDynamicResolution.boolValue,
                        CED.Group(
#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
                            CED.Group(
                                HDRenderPipelineUI.Styles.DLSSTitle,
                                GroupOption.Indent,
                                Drawer_Draw_DLSS_Section
                                ),
#endif
#if ENABLE_AMD && ENABLE_AMD_MODULE
                            CED.Group(
                                HDRenderPipelineUI.Styles.FSR2Title,
                                GroupOption.Indent,
                                Drawer_Draw_FSR2_Section
                                ),
#endif
                            CED.Conditional(
                                (serialized, owner) => HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings.fsrOverrideSharpness,
                                CED.Group(
                                    HDRenderPipelineUI.Styles.FSRTitle,
                                    GroupOption.Indent,
                                    Drawer_Draw_FSR_Section
                                    )
                                )
                            )
                        ),
                        CED.Group(Drawer_Rendering_Antialiasing)
                    ),
                AntialiasingModeDrawer(
                    HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing,
                    Drawer_Rendering_Antialiasing_SMAA),
                AntialiasingModeDrawer(
                    HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing,
                    Drawer_Rendering_Antialiasing_TAA),
                CED.Group(
                    CameraUI.Rendering.Drawer_Rendering_StopNaNs,
                    CameraUI.Rendering.Drawer_Rendering_Dithering,
                    CameraUI.Rendering.Drawer_Rendering_CullingMask,
                    CameraUI.Rendering.Drawer_Rendering_OcclusionCulling,
                    Drawer_Rendering_ExposureTarget,
                    Drawer_Rendering_RenderingPath,
                    Drawer_Rendering_CameraWarnings
                    ),
                CED.Conditional(
                    (serialized, owner) => !serialized.passThrough.boolValue && serialized.customRenderingSettings.boolValue,
                    (serialized, owner) => FrameSettingsUI.Inspector().Draw(serialized.frameSettings, owner)
                )
            );

            public static readonly CED.IDrawer Drawer;

            static Rendering()
            {
                Drawer = CED.AdditionalPropertiesFoldoutGroup(
                    CameraUI.Rendering.Styles.header,
                    Expandable.Rendering, k_ExpandedState,
                    AdditionalProperties.Rendering, k_AdditionalPropertiesState,
                    RenderingDrawer, Draw_Rendering_Advanced);
            }

            internal static void RegisterEditor(HDCameraEditor editor)
            {
                k_AdditionalPropertiesState.RegisterEditor(editor);
            }

            internal static void UnregisterEditor(HDCameraEditor editor)
            {
                k_AdditionalPropertiesState.UnregisterEditor(editor);
            }

            static void Draw_Rendering_Advanced(SerializedHDCamera p, Editor owner)
            { }

            public static readonly CED.IDrawer DrawerPreset = CED.FoldoutGroup(
                CameraUI.Rendering.Styles.header,
                Expandable.Rendering,
                k_ExpandedState,
                FoldoutOption.Indent,
                CameraUI.Rendering.Drawer_Rendering_CullingMask,
                CameraUI.Rendering.Drawer_Rendering_OcclusionCulling
            );

            static void Drawer_Rendering_AllowDynamicResolution(SerializedHDCamera p, Editor owner)
            {
                CameraUI.Output.Drawer_Output_AllowDynamicResolution(p, owner, Styles.allowDynamicResolution);

                var dynamicResSettings = HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings;
                s_IsRunningTAAU = p.allowDynamicResolution.boolValue && dynamicResSettings.upsampleFilter == UnityEngine.Rendering.DynamicResUpscaleFilter.TAAU && dynamicResSettings.enabled;

                if (s_IsRunningTAAU)
                {
                    EditorGUILayout.HelpBox(Styles.taauInfoBox, MessageType.Info);
                    p.antialiasing.intValue = (int)HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
                }
            }

#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
            static void Drawer_Draw_DLSS_Section(SerializedHDCamera p, Editor owner)
            {
                EditorGUI.indentLevel++;
                bool isDLSSEnabledInQualityAsset = HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings.advancedUpscalersByPriority.Contains(UnityEngine.Rendering.AdvancedUpscalers.DLSS);

                EditorGUILayout.PropertyField(p.allowDeepLearningSuperSampling, Styles.DLSSAllow);
                if (!isDLSSEnabledInQualityAsset && p.allowDeepLearningSuperSampling.boolValue)
                {
                    EditorGUILayout.HelpBox(Styles.DLSSNotEnabledInQualityAsset, MessageType.Info);
                }

                if (p.allowDeepLearningSuperSampling.boolValue)
                {
                    EditorGUILayout.PropertyField(p.deepLearningSuperSamplingUseCustomQualitySettings, Styles.DLSSCustomQualitySettings);
                    if (p.deepLearningSuperSamplingUseCustomQualitySettings.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        var v = EditorGUILayout.EnumPopup(
                            HDRenderPipelineUI.Styles.DLSSQualitySettingContent,
                            (UnityEngine.NVIDIA.DLSSQuality)p.deepLearningSuperSamplingQuality.intValue);
                        p.deepLearningSuperSamplingQuality.intValue = (int)(object)v;
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.PropertyField(p.deepLearningSuperSamplingUseCustomAttributes, Styles.DLSSUseCustomAttributes);
                    if (p.deepLearningSuperSamplingUseCustomAttributes.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(p.deepLearningSuperSamplingUseOptimalSettings, HDRenderPipelineUI.Styles.DLSSUseOptimalSettingsContent);
                        EditorGUI.indentLevel--;
                    }
                }

                bool isDLSSEnabled = isDLSSEnabledInQualityAsset && p.allowDeepLearningSuperSampling.boolValue;
                if (isDLSSEnabled)
                {
                    bool featureDetected = HDDynamicResolutionPlatformCapabilities.DLSSDetected;

                    //write here support string for dlss upscaler
                    EditorGUILayout.HelpBox(
                        featureDetected ? Styles.DLSSFeatureDetectedMsg : Styles.DLSSFeatureNotDetectedMsg,
                        featureDetected ? MessageType.Info : MessageType.Warning);
                }
                EditorGUI.indentLevel--;
            }
#endif

#if ENABLE_AMD && ENABLE_AMD_MODULE
            static void Drawer_Draw_FSR2_Section(SerializedHDCamera p, Editor owner)
            {
                EditorGUI.indentLevel++;
                bool isFSR2EnabledInQualityAsset = HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings.advancedUpscalersByPriority.Contains(UnityEngine.Rendering.AdvancedUpscalers.FSR2);


                EditorGUILayout.PropertyField(p.allowFidelityFX2SuperResolution, Styles.FSR2Allow);
                if (!isFSR2EnabledInQualityAsset && p.allowFidelityFX2SuperResolution.boolValue)
                {
                    EditorGUILayout.HelpBox(Styles.FSR2NotEnabledInQualityAsset, MessageType.Info);
                }

                if (p.allowFidelityFX2SuperResolution.boolValue)
                {
                    EditorGUILayout.PropertyField(p.fidelityFX2SuperResolutionUseCustomQualitySettings, Styles.FSR2CustomQualitySettings);
                    if (p.fidelityFX2SuperResolutionUseCustomQualitySettings.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(p.fidelityFX2SuperResolutionUseOptimalSettings, HDRenderPipelineUI.Styles.FSR2UseOptimalSettingsContent);
                        using (new EditorGUI.DisabledScope(!p.fidelityFX2SuperResolutionUseOptimalSettings.boolValue))
                        {
                            var v = EditorGUILayout.EnumPopup(
                                HDRenderPipelineUI.Styles.FSR2QualitySettingContent,
                                (UnityEngine.AMD.FSR2Quality)p.fidelityFX2SuperResolutionQuality.intValue);
                            p.fidelityFX2SuperResolutionQuality.intValue = (int)(object)v;
                        }
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.PropertyField(p.fidelityFX2SuperResolutionUseCustomAttributes, Styles.FSR2UseCustomAttributes);
                    if (p.fidelityFX2SuperResolutionUseCustomAttributes.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(p.fidelityFX2SuperResolutionEnableSharpening, HDRenderPipelineUI.Styles.FSR2EnableSharpness);
                        using (new EditorGUI.DisabledScope(!p.fidelityFX2SuperResolutionEnableSharpening.boolValue))
                        {
                            EditorGUILayout.PropertyField(p.fidelityFX2SuperResolutionSharpening, HDRenderPipelineUI.Styles.FSR2Sharpness);
                        }
                        EditorGUI.indentLevel--;
                    }
                }

                bool isFSR2Enabled = isFSR2EnabledInQualityAsset && p.allowFidelityFX2SuperResolution.boolValue;
                if (isFSR2Enabled)
                {
                    bool featureDetected = HDDynamicResolutionPlatformCapabilities.FSR2Detected;

                    //write here support string for dlss upscaler
                    EditorGUILayout.HelpBox(
                        featureDetected ? Styles.FSR2FeatureDetectedMsg : Styles.FSR2FeatureNotDetectedMsg,
                        featureDetected ? MessageType.Info : MessageType.Warning);
                }
                EditorGUI.indentLevel--;
            }
#endif

            static void Drawer_Draw_FSR_Section(SerializedHDCamera p, Editor owner)
            {
                EditorGUI.indentLevel++;
                var dynamicResSettings = HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings;

                // Only display the per-camera sharpness override if the pipeline level override is enabled
                if (dynamicResSettings.fsrOverrideSharpness)
                {
                    EditorGUILayout.PropertyField(p.fsrOverrideSharpness, Styles.fsrOverrideSharpness);

                    bool overrideSharpness = p.fsrOverrideSharpness.boolValue;
                    if (overrideSharpness)
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            EditorGUILayout.PropertyField(p.fsrSharpness, HDRenderPipelineUI.Styles.fsrSharpnessText);
                        }
                    }
                }
                EditorGUI.indentLevel--;
            }

            static void Drawer_Rendering_Antialiasing(SerializedHDCamera p, Editor owner)
            {
                bool showAntialiasContentAsFallback = false;

#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
                bool isDLSSEnabled = HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings.advancedUpscalersByPriority.Contains(UnityEngine.Rendering.AdvancedUpscalers.DLSS)
                    && p.allowDeepLearningSuperSampling.boolValue;
                showAntialiasContentAsFallback = isDLSSEnabled;
#endif

                using (new EditorGUI.DisabledScope(s_IsRunningTAAU))
                {
                    Rect antiAliasingRect = EditorGUILayout.GetControlRect();
                    EditorGUI.BeginProperty(antiAliasingRect, Styles.antialiasing, p.antialiasing);
                    {
                        EditorGUI.BeginChangeCheck();
                        int selectedValue = (int)(HDAdditionalCameraData.AntialiasingMode)EditorGUI.EnumPopup(antiAliasingRect, showAntialiasContentAsFallback ? Styles.antialiasingContentFallback : Styles.antialiasing, (HDAdditionalCameraData.AntialiasingMode)p.antialiasing.intValue);

                        if (EditorGUI.EndChangeCheck())
                            p.antialiasing.intValue = selectedValue;
                    }
                    EditorGUI.EndProperty();
                }
            }

            static CED.IDrawer AntialiasingModeDrawer(HDAdditionalCameraData.AntialiasingMode antialiasingMode, CED.ActionDrawer antialiasingDrawer)
            {
                return CED.Conditional(
                    (serialized, owner) => (serialized.antialiasing.intValue == (int)antialiasingMode) && (s_IsRunningTAAU ? serialized.antialiasing.intValue == (int)HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing : true),
                    CED.Group(
                        GroupOption.Indent,
                        antialiasingDrawer
                    )
                );
            }

            static void Drawer_Rendering_Antialiasing_SMAA(SerializedHDCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.SMAAQuality, Styles.SMAAQualityPresetContent);
            }

            static void Draw_Rendering_Antialiasing_TAA_Advanced(SerializedHDCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.taaBaseBlendFactor, Styles.TAABaseBlendFactor);
                using (new EditorGUI.DisabledScope(s_IsRunningTAAU))
                {
                    EditorGUILayout.PropertyField(p.taaJitterScale, Styles.TAAJitterScale);
                }
            }

            static void Drawer_Rendering_Antialiasing_TAA(SerializedHDCamera p, Editor owner)
            {
                using (new EditorGUI.DisabledScope(s_IsRunningTAAU))
                {
                    EditorGUILayout.PropertyField(p.taaQualityLevel, Styles.TAAQualityLevel);
                }
                if (s_IsRunningTAAU)
                    p.taaQualityLevel.intValue = (int)HDAdditionalCameraData.TAAQualityLevel.High;


                EditorGUILayout.PropertyField(p.taaSharpenMode, Styles.TAASharpeningMode);
                EditorGUI.indentLevel++;
                if (p.taaSharpenMode.intValue != (int)HDAdditionalCameraData.TAASharpenMode.ContrastAdaptiveSharpening)
                {
                    EditorGUILayout.PropertyField(p.taaSharpenStrength, Styles.TAASharpen);
                    if (p.taaSharpenMode.intValue == (int)HDAdditionalCameraData.TAASharpenMode.PostSharpen)
                    {
                        EditorGUILayout.PropertyField(p.taaRingingReduction, Styles.TAARingingReduction);
                    }
                }
                EditorGUI.indentLevel--;

                if (p.taaQualityLevel.intValue > (int)HDAdditionalCameraData.TAAQualityLevel.Low)
                {
                    EditorGUILayout.PropertyField(p.taaHistorySharpening, Styles.TAAHistorySharpening);
                    EditorGUILayout.PropertyField(p.taaAntiFlicker, Styles.TAAAntiFlicker);
                }

                if (p.taaQualityLevel.intValue == (int)HDAdditionalCameraData.TAAQualityLevel.High)
                {
                    EditorGUILayout.PropertyField(p.taaMotionVectorRejection, Styles.TAAMotionVectorRejection);
                    EditorGUILayout.PropertyField(p.taaAntiRinging, Styles.TAAAntiRinging);
                }

                if (k_AdditionalPropertiesState[AdditionalProperties.Rendering])
                {
                    Draw_Rendering_Antialiasing_TAA_Advanced(p, owner);
                }
            }

            static void Drawer_Rendering_RenderingPath(SerializedHDCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.passThrough, Styles.fullScreenPassthrough);
                using (new EditorGUI.DisabledScope(p.passThrough.boolValue))
                    EditorGUILayout.PropertyField(p.customRenderingSettings, Styles.renderingPath);
            }

            static void Drawer_Rendering_ExposureTarget(SerializedHDCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.exposureTarget, Styles.exposureTarget);
            }

            static readonly MethodInfo k_Camera_GetCameraBufferWarnings = typeof(Camera).GetMethod("GetCameraBufferWarnings", BindingFlags.Instance | BindingFlags.NonPublic);
            static string[] GetCameraBufferWarnings(Camera camera)
            {
                return (string[])k_Camera_GetCameraBufferWarnings.Invoke(camera, null);
            }

            static void Drawer_Rendering_CameraWarnings(SerializedHDCamera p, Editor owner)
            {
                foreach (Camera camera in p.serializedObject.targetObjects)
                {
                    var warnings = GetCameraBufferWarnings(camera);
                    if (warnings.Length > 0)
                        EditorGUILayout.HelpBox(string.Join("\n\n", warnings), MessageType.Warning, true);
                }
            }
        }
    }
}
