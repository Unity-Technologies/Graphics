using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal partial class UniversalRenderPipelineAssetUI
    {
        internal static class Styles
        {
            // Groups
            public static GUIContent renderersSettingsText = EditorGUIUtility.TrTextContent("Renderer List", "Settings that control the list of renderers used by the Render pipeline.");
            public static GUIContent qualitySettingsText = EditorGUIUtility.TrTextContent("Quality", "Settings that control the quality level of the Render pipeline, improving performance and graphics quality.");
            public static GUIContent postProcessingSettingsText = EditorGUIUtility.TrTextContent("Post-processing", "Settings that allow for fine tuning of post-processing effects in the Scene when this Render Pipeline Asset is in use.");
            public static GUIContent advancedSettingsText = EditorGUIUtility.TrTextContent("Advanced");
            public static GUIContent adaptivePerformanceText = EditorGUIUtility.TrTextContent("Adaptive Performance");

            // Rendering
            public static GUIContent rendererHeaderText = EditorGUIUtility.TrTextContent("Renderer List", "Lists all the renderers available to this Render Pipeline Asset.");
            public static GUIContent rendererAddMessage = EditorGUIUtility.TrTextContent("Add Renderer", "Lists all the renderers available to add for this Render Pipeline Asset.");
            public static GUIContent rendererDefaultText = EditorGUIUtility.TrTextContent("Default Renderer", "This renderer is currently the default for the render pipeline.");
            public static GUIContent rendererSetDefaultText = EditorGUIUtility.TrTextContent("Set Default", "Makes this renderer the default for the render pipeline.");
            public static GUIContent rendererSettingsText = EditorGUIUtility.TrIconContent("_Menu", "Opens settings for this renderer.");
            public static GUIContent rendererMissingText = EditorGUIUtility.TrIconContent("console.warnicon.sml", "Renderer missing. Click this to select a new renderer.");
            public static GUIContent rendererDefaultMissingText = EditorGUIUtility.TrIconContent("console.erroricon.sml", "Default renderer missing. Click this to select a new renderer.");
            public static GUIContent srpBatcher = EditorGUIUtility.TrTextContent("SRP Batcher", "If enabled, the render pipeline uses the SRP batcher.");
            public static GUIContent dynamicBatching = EditorGUIUtility.TrTextContent("Dynamic Batching", "If enabled, the render pipeline will batch drawcalls with few triangles together by copying their vertex buffers into a shared buffer on a per-frame basis.");

            public static string colorGradingModeWarning = "HDR rendering is required to use the high dynamic range color grading mode. The low dynamic range will be used instead.";
            public static string colorGradingModeSpecInfo = "The high dynamic range color grading mode works best on platforms that support floating point textures.";
            public static string colorGradingLutSizeWarning = "The minimal recommended LUT size for the high dynamic range color grading mode is 32. Using lower values will potentially result in color banding and posterization effects.";

            // Quality
            public static GUIContent hdrText = EditorGUIUtility.TrTextContent("HDR", "Controls the global HDR settings.");
            public static GUIContent hdrColorBufferPrecisionText = EditorGUIUtility.TrTextContent("HDR Precision", "Controls the precision of the camera color buffer in HDR rendering. 32-bits is the default. 64-bits can reduce banding artifacts at the cost of memory and performance.");
            public static GUIContent msaaText = EditorGUIUtility.TrTextContent("Anti Aliasing (MSAA)", "Controls the global anti aliasing settings.");
            public static GUIContent renderScaleText = EditorGUIUtility.TrTextContent("Render Scale", "Scales the camera render target allowing the game to render at a resolution different than native resolution. UI is always rendered at native resolution.");
            public static GUIContent upscalingFilterText = EditorGUIUtility.TrTextContent("Upscaling Filter", "Controls the type of filter used for upscaling when render scale is lower than 1.0.");
            public static GUIContent fsrOverrideSharpness = EditorGUIUtility.TrTextContent("Override FSR Sharpness", "Overrides the FSR sharpness value for the render pipeline asset.");
            public static GUIContent fsrSharpnessText = EditorGUIUtility.TrTextContent("FSR Sharpness", "Controls the intensity of the sharpening filter used by FidelityFX Super Resolution.");

            // Adaptive performance settings
            public static GUIContent useAdaptivePerformance = EditorGUIUtility.TrTextContent("Use adaptive performance", "Allows Adaptive Performance to adjust rendering quality during runtime");

            // Renderer List Messages
            public static GUIContent rendererListDefaultMessage =
                EditorGUIUtility.TrTextContent("Cannot remove Default Renderer",
                    "Removal of the Default Renderer is not allowed. To remove, set another Renderer to be the new Default and then remove.");

            public static GUIContent rendererMissingDefaultMessage =
                EditorGUIUtility.TrTextContent("Missing Default Renderer\nThere is no default renderer assigned, so Unity can’t perform any rendering. Set another renderer to be the new Default, or assign a renderer to the Default slot.");
            public static GUIContent rendererMissingMessage =
                EditorGUIUtility.TrTextContent("Missing Renderer(s)\nOne or more renderers are either missing or unassigned.  Switching to these renderers at runtime can cause issues.");
            public static GUIContent rendererUnsupportedAPIMessage =
                EditorGUIUtility.TrTextContent("Some Renderer(s) in the Renderer List are incompatible with the Player Graphics APIs list.  Switching to these renderers at runtime can cause issues.\n\n");

            // Dropdown menu options
            public static string[] opaqueDownsamplingOptions = { "None", "2x (Bilinear)", "4x (Box)", "4x (Bilinear)" };
        }
    }
}
