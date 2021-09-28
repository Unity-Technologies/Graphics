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
                public static GUIContent rendererType = EditorGUIUtility.TrTextContent("Renderer", "The series of operations that translates code into visuals. These have different capabilities and performance characteristics.");

                public static GUIContent renderPostProcessing = EditorGUIUtility.TrTextContent("Post Processing", "Enable this to make this camera render post-processing effects.");
                public static GUIContent antialiasing = EditorGUIUtility.TrTextContent("Anti-aliasing", "The method the camera uses to smooth jagged edges.");
                public static GUIContent antialiasingQuality = EditorGUIUtility.TrTextContent("Quality", "The quality level to use for the selected anti-aliasing method.");

                public static GUIContent requireDepthTexture = EditorGUIUtility.TrTextContent("Depth Texture", "If this is enabled, the camera builds a screen-space depth texture. Note that generating the texture incurs a performance cost.");
                public static GUIContent requireOpaqueTexture = EditorGUIUtility.TrTextContent("Opaque Texture", "If this is enabled, the camera copies the rendered view so it can be accessed at a later stage in the pipeline.");

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
