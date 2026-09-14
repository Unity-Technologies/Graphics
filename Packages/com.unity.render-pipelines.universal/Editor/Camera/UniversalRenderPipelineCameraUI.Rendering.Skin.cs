using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    static partial class UniversalRenderPipelineCameraUI
    {
        public partial class Rendering
        {
            public class Styles
            {
                public static GUIContent rendererType = L10n.TextContent("Renderer", "The series of operations that translates code into visuals. These have different capabilities and performance characteristics.", null, null);

                public static readonly string xrAntialiasingInfo = L10n.Tr("When targeting an XR device, the XR runtime or compositor may provide anti-aliasing. In that case this camera's anti-aliasing can be redundant or ignored - review your XR provider settings to configure anti-aliasing.", null);
                public static readonly string xrAntialiasingInfoButton = L10n.Tr("Open Project Validation", null);
                public static GUIContent renderPostProcessing = L10n.TextContent("Post Processing", "Enable this to make this camera render post-processing effects.", null, null);
                public static GUIContent antialiasing = L10n.TextContent("Anti-aliasing", "The method the camera uses to smooth jagged edges.", null, null);
                public static GUIContent antialiasingQuality = L10n.TextContent("Quality", "The quality level to use for the selected anti-aliasing method.", null, null);

                public static GUIContent taaContrastAdaptiveSharpening = L10n.TextContent("Contrast Adaptive Sharpening", "Enables high quality post sharpening to reduce TAA blur. The FSR upscaling overrides this setting if enabled.", null, null);
                public static readonly GUIContent taaBaseBlendFactor = L10n.TextContent("Base Blend Factor", "Determines how much the history buffer is blended together with current frame result. Higher values means more history contribution, which leads to better anti aliasing, but also more prone to ghosting.", null, null);
                public static readonly GUIContent taaJitterScale = L10n.TextContent("Jitter Scale", "Determines the scale to the jitter applied when TAA is enabled. Lowering this value will lead to less visible flickering and jittering, but also will produce more aliased images.", null, null);
                public static readonly GUIContent taaMipBias = L10n.TextContent("Mip Bias", "Determines how much texture mip map selection is biased when rendering. Lowering this can slightly reduce blur on textures at the cost of performance. Requires mip maps in textures.", null, null);
                public static readonly GUIContent taaVarianceClampScale = L10n.TextContent("Variance Clamp Scale", "Determines the strength of the history color rectification clamp. Lower values can reduce ghosting, but produce more flickering. Higher values reduce flickering, but are prone to blur and ghosting.", null, null);

                public static GUIContent taaResetHistory = L10n.TextContent("Reset History", "Reset the history buffers.", null, null);

                public static GUIContent requireDepthTexture = L10n.TextContent("Depth Texture", "If this is enabled, the camera builds a screen-space depth texture. Note that generating the texture incurs a performance cost.", null, null);
                public static GUIContent requireOpaqueTexture = L10n.TextContent("Opaque Texture", "If this is enabled, the camera copies the rendered view so it can be accessed at a later stage in the pipeline.", null, null);

                public static GUIContent clearDepth = L10n.TextContent("Clear Depth", "If enabled, depth from the previous camera will be cleared.", null, null);
                public static GUIContent renderingShadows = L10n.TextContent("Render Shadows", "Makes this camera render shadows.", null, null);

                public static GUIContent priority = L10n.TextContent("Priority", "A camera with a higher priority is drawn on top of a camera with a lower priority [ -100, 100 ].", null, null);

                public static readonly string noRendererError = L10n.Tr("There are no valid Renderers available on the Universal Render Pipeline asset.", null);
                public static readonly string missingRendererWarning = L10n.Tr("The currently selected Renderer is missing from the Universal Render Pipeline asset.", null);
                public static readonly string disabledPostprocessing = L10n.Tr("Post Processing is currently disabled on the current Universal Render Pipeline renderer.", null);
                public static readonly string selectRenderPipelineAsset = L10n.Tr("Select Render Pipeline Asset", null);
                public static readonly string disabledPostprocessingAntiAliasWarning = L10n.Tr("Post Processing based Anti-aliasing requires Post Processing enabled to function.", null);
                public static readonly string MSAAWarning = L10n.Tr("MSAA is enabled. TAA will be disabled when using current UniversalRenderPipelineAsset.", null);
            }
        }
    }
}
