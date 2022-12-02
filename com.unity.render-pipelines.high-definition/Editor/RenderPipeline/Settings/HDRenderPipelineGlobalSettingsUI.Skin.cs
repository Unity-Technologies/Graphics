using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    internal partial class HDRenderPipelineGlobalSettingsUI
    {
        internal class Styles
        {
            public const int labelWidth = 220;

            public static readonly GUIContent defaultVolumeProfileLabel = EditorGUIUtility.TrTextContent("Default Volume Profile Asset");
            public static readonly GUIContent lookDevVolumeProfileLabel = EditorGUIUtility.TrTextContent("LookDev Volume Profile Asset");

            public static readonly GUIContent frameSettingsLabel = EditorGUIUtility.TrTextContent("Frame Settings (Default Values)");
            public static readonly GUIContent frameSettingsLabel_Camera = EditorGUIUtility.TrTextContent("Camera");
            public static readonly GUIContent frameSettingsLabel_RTProbe = EditorGUIUtility.TrTextContent("Realtime Reflection");
            public static readonly GUIContent frameSettingsLabel_BakedProbe = EditorGUIUtility.TrTextContent("Baked or Custom Reflection");
            public static readonly GUIContent renderingSettingsHeaderContent = EditorGUIUtility.TrTextContent("Rendering");
            public static readonly GUIContent lightSettingsHeaderContent = EditorGUIUtility.TrTextContent("Lighting");
            public static readonly GUIContent asyncComputeSettingsHeaderContent = EditorGUIUtility.TrTextContent("Asynchronous Compute Shaders");
            public static readonly GUIContent lightLoopSettingsHeaderContent = EditorGUIUtility.TrTextContent("Light Loop Debug");

            public static readonly GUIContent volumeComponentsLabel = EditorGUIUtility.TrTextContent("Volume Profiles");
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

            public static readonly GUIContent newVolumeProfileLabel = EditorGUIUtility.TrTextContent("New", "Create a new Volume Profile for default in your default resource folder (defined in Wizard)");
            public static readonly GUIContent fixAssetButtonLabel = EditorGUIUtility.TrTextContent("Fix", "Ensure a HD Global Settings Asset is assigned.");

            public static readonly GUIContent probeVolumeSupportContentLabel = EditorGUIUtility.TrTextContent("Probe Volumes", "Set whether Probe volumes are supported by the project. The feature is highly experimental and subject to changes.");
            public static readonly GUIContent rendererListCulling = EditorGUIUtility.TrTextContent("Dynamic Render Pass Culling", "When enabled, rendering passes are automatically culled based on what is visible on the camera.");
            public static readonly GUIContent specularFade = EditorGUIUtility.TrTextContent("Specular Fade", "When enabled, specular values below 2% will be gradually faded to suppress specular lighting completely. Do note that this behavior is NOT physically correct.");
            public static readonly GUIContent autoRegisterDiffusionProfilesContentLabel = EditorGUIUtility.TrTextContent("Auto Register Diffusion Profiles", "When enabled, diffusion profiles referenced by an imported material will be automatically added to the diffusion profile list in the HDRP Global Settings.");
        }
    }
}
