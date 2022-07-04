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

            public static readonly GUIContent layerNamesLabel = EditorGUIUtility.TrTextContent("Layers Names");
            public static readonly GUIContent lightLayersLabel = EditorGUIUtility.TrTextContent("Light Layer Names", "When enabled, HDRP allocates memory for processing Light Layers. For deferred rendering, this allocation includes an extra render target in memory and extra cost. See the Quality Settings window to enable Light Layers on your Render pipeline asset.");
            public static readonly GUIContent lightLayerName0 = EditorGUIUtility.TrTextContent("Light Layer 0", "The display name for Light Layer 0. This is purely cosmetic, and can be used to articulate intended use of Light Layer 0");
            public static readonly GUIContent lightLayerName1 = EditorGUIUtility.TrTextContent("Light Layer 1", "The display name for Light Layer 1. This is purely cosmetic, and can be used to articulate intended use of Light Layer 1");
            public static readonly GUIContent lightLayerName2 = EditorGUIUtility.TrTextContent("Light Layer 2", "The display name for Light Layer 2. This is purely cosmetic, and can be used to articulate intended use of Light Layer 2");
            public static readonly GUIContent lightLayerName3 = EditorGUIUtility.TrTextContent("Light Layer 3", "The display name for Light Layer 3. This is purely cosmetic, and can be used to articulate intended use of Light Layer 3");
            public static readonly GUIContent lightLayerName4 = EditorGUIUtility.TrTextContent("Light Layer 4", "The display name for Light Layer 4. This is purely cosmetic, and can be used to articulate intended use of Light Layer 4");
            public static readonly GUIContent lightLayerName5 = EditorGUIUtility.TrTextContent("Light Layer 5", "The display name for Light Layer 5. This is purely cosmetic, and can be used to articulate intended use of Light Layer 5");
            public static readonly GUIContent lightLayerName6 = EditorGUIUtility.TrTextContent("Light Layer 6", "The display name for Light Layer 6. This is purely cosmetic, and can be used to articulate intended use of Light Layer 6");
            public static readonly GUIContent lightLayerName7 = EditorGUIUtility.TrTextContent("Light Layer 7", "The display name for Light Layer 7. This is purely cosmetic, and can be used to articulate intended use of Light Layer 7");

            public static readonly GUIContent decalLayersLabel = EditorGUIUtility.TrTextContent("Decal Layer Names", "When enabled, HDRP allocates Shader variants and memory to the decals buffer and cluster decal. See the Quality Settings window to enable Decal Layers on your Render pipeline asset.");
            public static readonly GUIContent decalLayerName0 = EditorGUIUtility.TrTextContent("Decal Layer 0", "The display name for Decal Layer 0. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 0");
            public static readonly GUIContent decalLayerName1 = EditorGUIUtility.TrTextContent("Decal Layer 1", "The display name for Decal Layer 1. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 1");
            public static readonly GUIContent decalLayerName2 = EditorGUIUtility.TrTextContent("Decal Layer 2", "The display name for Decal Layer 2. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 2");
            public static readonly GUIContent decalLayerName3 = EditorGUIUtility.TrTextContent("Decal Layer 3", "The display name for Decal Layer 3. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 3");
            public static readonly GUIContent decalLayerName4 = EditorGUIUtility.TrTextContent("Decal Layer 4", "The display name for Decal Layer 4. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 4");
            public static readonly GUIContent decalLayerName5 = EditorGUIUtility.TrTextContent("Decal Layer 5", "The display name for Decal Layer 5. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 5");
            public static readonly GUIContent decalLayerName6 = EditorGUIUtility.TrTextContent("Decal Layer 6", "The display name for Decal Layer 6. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 6");
            public static readonly GUIContent decalLayerName7 = EditorGUIUtility.TrTextContent("Decal Layer 7", "The display name for Decal Layer 7. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 7");

            public static readonly GUIContent lensAttenuationModeContentLabel = EditorGUIUtility.TrTextContent("Lens Attenuation Mode", "Set the attenuation mode of the lens that is used to compute exposure. With imperfect lens some energy is lost when converting from EV100 to the exposure multiplier.");
            public static readonly GUIContent colorGradingSpaceContentLabel = EditorGUIUtility.TrTextContent("Color Grading Space", "Set the color space in which color grading is performed. If ACES is used as tonemapper, the grading always happens in ACEScg. sRGB will lead to rendering in a non-wide color gamut, while ACEScg is a wider color gamut that will allow to exploit the wide color gamut on UHD TV when outputting in HDR.");

            public static readonly GUIContent useDLSSCustomProjectIdLabel = EditorGUIUtility.TrTextContent("Use DLSS Custom Project Id", "Set to utilize a custom project Id for the NVIDIA Deep Learning Super Sampling extension.");
            public static readonly GUIContent DLSSProjectIdLabel = EditorGUIUtility.TrTextContent("DLSS Custom Project Id", "The custom project ID string to utilize for the NVIDIA Deep Learning Super Sampling extension.");

            public static readonly GUIContent newVolumeProfileLabel = EditorGUIUtility.TrTextContent("New", "Create a new Volume Profile for default in your default resource folder (defined in Wizard)");
            public static readonly GUIContent fixAssetButtonLabel = EditorGUIUtility.TrTextContent("Fix", "Ensure a HD Global Settings Asset is assigned.");

            public static readonly GUIContent probeVolumeSupportContentLabel = EditorGUIUtility.TrTextContent("Probe Volumes", "Set whether Probe volumes are supported by the project. The feature is highly experimental and subject to changes.");
            public static readonly GUIContent rendererListCulling = EditorGUIUtility.TrTextContent("Dynamic Render Pass Culling", "When enabled, rendering passes are automatically culled based on what is visible on the camera.");
            public static readonly GUIContent supportRuntimeDebugDisplayContentLabel = EditorGUIUtility.TrTextContent("Runtime Debug Shaders", "When disabled, all debug display shader variants are removed when you build for the Unity Player. This decreases build time, but prevents the use of Rendering Debugger in Player builds.");
            public static readonly GUIContent autoRegisterDiffusionProfilesContentLabel = EditorGUIUtility.TrTextContent("Auto Register Diffusion Profiles", "When enabled, diffusion profiles referenced by an imported material will be automatically added to the diffusion profile list in the HDRP Global Settings.");
        }
    }
}
