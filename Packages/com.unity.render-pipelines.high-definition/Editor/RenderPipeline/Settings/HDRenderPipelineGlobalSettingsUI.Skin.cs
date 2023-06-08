using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    internal partial class HDRenderPipelineGlobalSettingsUI
    {
        internal class Styles
        {
            public const int labelWidth = 220;
            public const int defaultVolumeLabelWidth = 260;

            public static readonly GUIContent defaultVolumeProfileSectionLabel = EditorGUIUtility.TrTextContent("Default Volume Profile");
            public static readonly GUIContent defaultVolumeProfileAssetLabel = EditorGUIUtility.TrTextContent("Volume Profile", "Settings that will be applied project-wide to all Volumes by default when High Definition Render Pipeline is active.");
            public static readonly GUIContent lookDevVolumeProfileSectionLabel = EditorGUIUtility.TrTextContent("LookDev Volume Profile");
            public static readonly GUIContent lookDevVolumeProfileAssetLabel = EditorGUIUtility.TrTextContent("Volume Profile");

            public static readonly GUIContent frameSettingsLabel = EditorGUIUtility.TrTextContent("Frame Settings (Default Values)");

            public static readonly GUIContent customPostProcessOrderLabel = EditorGUIUtility.TrTextContent("Custom Post Process Orders");

            public static readonly GUIContent resourceLabel = EditorGUIUtility.TrTextContent("Resources");
            public static readonly GUIContent renderPipelineResourcesContent = EditorGUIUtility.TrTextContent("Player Resources", "Set of resources that need to be loaded when creating stand alone");
            public static readonly GUIContent renderPipelineRayTracingResourcesContent = EditorGUIUtility.TrTextContent("Ray Tracing Resources", "Set of resources that need to be loaded when using ray tracing");
            public static readonly GUIContent renderPipelineEditorResourcesContent = EditorGUIUtility.TrTextContent("Editor Resources", "Set of resources that need to be loaded for working in editor");

            public static readonly GUIContent generalSettingsLabel = EditorGUIUtility.TrTextContent("Miscellaneous");

            public static readonly GUIContent defaultRenderingLayerMaskLabel = EditorGUIUtility.TrTextContent("Default Mesh Rendering Layer Mask", "The Default Rendering Layer Mask for newly created Renderers.");
            public static readonly GUIContent renderingLayersLabel = EditorGUIUtility.TrTextContent("Rendering Layers");
            public static readonly GUIContent renderingLayerNamesLabel = EditorGUIUtility.TrTextContent("Rendering Layer Names");

            public static readonly GUIContent lensAttenuationModeContentLabel = EditorGUIUtility.TrTextContent("Lens Attenuation Mode", "Set the attenuation mode of the lens that is used to compute exposure. With imperfect lens some energy is lost when converting from EV100 to the exposure multiplier.");
            public static readonly GUIContent colorGradingSpaceContentLabel = EditorGUIUtility.TrTextContent("Color Grading Space", "Set the color space in which color grading is performed. If ACES is used as tonemapper, the grading always happens in ACEScg. sRGB will lead to rendering in a non-wide color gamut, while ACEScg is a wider color gamut that will allow to exploit the wide color gamut on UHD TV when outputting in HDR.");

            public static readonly GUIContent useDLSSCustomProjectIdLabel = EditorGUIUtility.TrTextContent("Use DLSS Custom Project Id", "Set to utilize a custom project Id for the NVIDIA Deep Learning Super Sampling extension.");
            public static readonly GUIContent DLSSProjectIdLabel = EditorGUIUtility.TrTextContent("DLSS Custom Project Id", "The custom project ID string to utilize for the NVIDIA Deep Learning Super Sampling extension.");

            public static readonly GUIContent fixAssetButtonLabel = EditorGUIUtility.TrTextContent("Fix", "Ensure a HD Global Settings Asset is assigned.");

            public static readonly GUIContent probeVolumeSupportContentLabel = EditorGUIUtility.TrTextContent("Probe Volumes", "Set whether Probe volumes are supported by the project. The feature is highly experimental and subject to changes.");
            public static readonly GUIContent rendererListCulling = EditorGUIUtility.TrTextContent("Dynamic Render Pass Culling", "When enabled, rendering passes are automatically culled based on what is visible on the camera.");
            public static readonly GUIContent specularFade = EditorGUIUtility.TrTextContent("Specular Fade", "When enabled, specular values below 2% will be gradually faded to suppress specular lighting completely. Do note that this behavior is NOT physically correct.");
            public static readonly GUIContent autoRegisterDiffusionProfilesContentLabel = EditorGUIUtility.TrTextContent("Auto Register Diffusion Profiles", "When enabled, diffusion profiles referenced by an imported material will be automatically added to the diffusion profile list in the HDRP Global Settings.");

            public static readonly GUIContent analyticDerivativeEmulationContentLabel = EditorGUIUtility.TrTextContent("Analytic Derivative Emulation (experimental)", "When enabled, imported shaders will use analytic derivatives for their Forward and GBuffer pass. This is a developer-only feature for testing.");
            public static readonly GUIContent analyticDerivativeDebugOutputContentLabel = EditorGUIUtility.TrTextContent("Analytic Derivative Debug Output (experimental)", "When enabled, output detailed logs of the analytic derivative parser. This is a developer-only feature for testing.");
        }
    }
}
