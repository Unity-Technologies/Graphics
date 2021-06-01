using System.Linq;
using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    static partial class UniversalRenderPipelineCameraUI
    {
        public partial class Rendering
        {
            public class Styles
            {
                public static GUIContent rendererType = EditorGUIUtility.TrTextContent("Renderer", "Controls which renderer this camera uses.");

                public static GUIContent renderPostProcessing = EditorGUIUtility.TrTextContent("Post Processing", "Enable this to make this camera render post-processing effects.");
                public static GUIContent antialiasing = EditorGUIUtility.TrTextContent("Anti-aliasing", "The anti-aliasing method to use.");
                public static GUIContent antialiasingQuality = EditorGUIUtility.TrTextContent("Quality", "The quality level to use for the selected anti-aliasing method.");

                public static GUIContent requireDepthTexture = EditorGUIUtility.TrTextContent("Depth Texture", "On makes this camera create a _CameraDepthTexture, which is a copy of the rendered depth values.\nOff makes the camera not create a depth texture.\nUse Pipeline Settings applies settings from the Render Pipeline Asset.");
                public static GUIContent requireOpaqueTexture = EditorGUIUtility.TrTextContent("Opaque Texture", "On makes this camera create a _CameraOpaqueTexture, which is a copy of the rendered view.\nOff makes the camera not create an opaque texture.\nUse Pipeline Settings applies settings from the Render Pipeline Asset.");

                public static GUIContent clearDepth = EditorGUIUtility.TrTextContent("Clear Depth", "If enabled, depth from the previous camera will be cleared.");
                public static GUIContent renderingShadows = EditorGUIUtility.TrTextContent("Render Shadows", "Makes this camera render shadows.");

                public static GUIContent priority = EditorGUIUtility.TrTextContent("Priority", "A camera with a higher priority is drawn on top of a camera with a lower priority [ -100, 100 ].");

                public static readonly string noRendererError = L10n.Tr("There are no valid Renderers available on the Universal Render Pipeline asset.");
                public static readonly string missingRendererWarning = L10n.Tr("The currently selected Renderer is missing from the Universal Render Pipeline asset.");
                public static readonly string disabledPostprocessing = L10n.Tr("Post Processing is currently disabled on the current Universal Render Pipeline renderer.");
                public static readonly string stopNaNsMessage = L10n.Tr("Stop NaNs has no effect on GLES2 platforms.");
                public static readonly string SMAANotSupported = L10n.Tr("Sub-pixel Morphological Anti-Aliasing isn't supported on GLES2 platforms.");
                public static readonly string selectRenderPipelineAsset = L10n.Tr("Select Render Pipeline Asset");
            }
        }
    }
}
