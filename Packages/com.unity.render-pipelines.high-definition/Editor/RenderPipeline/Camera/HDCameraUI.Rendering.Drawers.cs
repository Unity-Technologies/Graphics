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
                    Drawer_Rendering_AllowDynamicResolution,
                    Drawer_Rendering_Antialiasing
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

            public static readonly CED.IDrawer Drawer = CED.AdditionalPropertiesFoldoutGroup(CameraUI.Rendering.Styles.header, Expandable.Rendering, k_ExpandedState, AdditionalProperties.Rendering, k_AdditionalPropertiesState, RenderingDrawer, Draw_Rendering_Advanced);

            internal static void RegisterEditor(HDCameraEditor editor)
            {
                k_AdditionalPropertiesState.RegisterEditor(editor);
            }

            internal static void UnregisterEditor(HDCameraEditor editor)
            {
                k_AdditionalPropertiesState.UnregisterEditor(editor);
            }

            [SetAdditionalPropertiesVisibility]
            internal static void SetAdditionalPropertiesVisibility(bool value)
            {
                if (value)
                    k_AdditionalPropertiesState.ShowAll();
                else
                    k_AdditionalPropertiesState.HideAll();
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
                CameraUI.Output.Drawer_Output_AllowDynamicResolution(p, owner);

                var dynamicResSettings = HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings;
                s_IsRunningTAAU = p.allowDynamicResolution.boolValue && dynamicResSettings.upsampleFilter == UnityEngine.Rendering.DynamicResUpscaleFilter.TAAU && dynamicResSettings.enabled;

                if (s_IsRunningTAAU)
                {
                    EditorGUILayout.HelpBox(Styles.taauInfoBox, MessageType.Info);
                    p.antialiasing.intValue = (int)HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
                }

#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
                EditorGUI.indentLevel++;
                Drawer_Draw_DLSS_Section(p, owner);
                EditorGUI.indentLevel--;
#endif
            }

#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
            static void Drawer_Draw_DLSS_Section(SerializedHDCamera p, Editor owner)
            {
                if (!p.allowDynamicResolution.boolValue)
                    return;

                bool isDLSSEnabledInQualityAsset = HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings.enableDLSS;

                EditorGUILayout.PropertyField(p.allowDeepLearningSuperSampling, Styles.DLSSAllow);
                if (!isDLSSEnabledInQualityAsset && p.allowDeepLearningSuperSampling.boolValue)
                {
                    EditorGUILayout.HelpBox(Styles.DLSSNotEnabledInQualityAsset, MessageType.Info);
                }

                if (p.allowDeepLearningSuperSampling.boolValue)
                {
                    EditorGUI.indentLevel++;
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
                        using (new EditorGUI.DisabledScope(p.deepLearningSuperSamplingUseOptimalSettings.boolValue))
                        {
                            EditorGUILayout.PropertyField(p.deepLearningSuperSamplingSharpening, HDRenderPipelineUI.Styles.DLSSSharpnessContent);
                        }
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;
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
            }

#endif


            static void Drawer_Rendering_Antialiasing(SerializedHDCamera p, Editor owner)
            {
                bool showAntialiasContentAsFallback = false;

#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
                bool isDLSSEnabled = HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings.enableDLSS
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

                EditorGUILayout.PropertyField(p.taaSharpenStrength, Styles.TAASharpen);

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
