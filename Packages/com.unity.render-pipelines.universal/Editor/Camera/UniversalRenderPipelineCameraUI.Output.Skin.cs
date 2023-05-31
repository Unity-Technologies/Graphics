using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    static partial class UniversalRenderPipelineCameraUI
    {
        public partial class Output
        {
            public class Styles
            {
#if ENABLE_VR && ENABLE_XR_MODULE
                public static GUIContent[] xrTargetEyeOptions =
                {
                    EditorGUIUtility.TrTextContent("None"),
                    EditorGUIUtility.TrTextContent("Both"),
                };

                public static int[] xrTargetEyeValues = { 0, 1 };
                public static readonly GUIContent xrTargetEye = EditorGUIUtility.TrTextContent("Target Eye",
                    "Allows XR rendering if target eye sets to both eye. Disable XR for this camera otherwise.");
#endif
                // Using the pipeline Settings
                public static GUIContent[] displayedCameraOptions =
                {
                    EditorGUIUtility.TrTextContent("Off"),
                    EditorGUIUtility.TrTextContent("Use settings from Render Pipeline Asset"),
                };
                
                public static int[] cameraOptions = { 0, 1 };
                
                // Using the project settings
                public static GUIContent[] hdrOuputOptions =
                {
                    EditorGUIUtility.TrTextContent("Off"),
                    EditorGUIUtility.TrTextContent("Use Project Settings"),
                };
                public static int[] hdrOuputValues = { 0, 1 };

                public static readonly GUIContent targetTextureLabel = EditorGUIUtility.TrTextContent("Output Texture", "The texture to render this camera into, if none then this camera renders to screen.");

                public static string inspectorOverlayCameraText = L10n.Tr("Inspector Overlay Camera");
                public static readonly GUIContent allowMSAA = EditorGUIUtility.TrTextContent("MSAA", "Enables Multi-Sample Anti-Aliasing, a technique that smooths jagged edges.");
                public static readonly GUIContent allowHDR = EditorGUIUtility.TrTextContent("HDR Rendering", "High Dynamic Range gives you a wider range of light intensities, so your lighting looks more realistic. With it, you can still see details and experience less saturation even with bright light.", (Texture)null);
                public static readonly GUIContent allowDynamicResolution = EditorGUIUtility.TrTextContent("URP Dynamic Resolution", "Whether to support URP dynamic resolution.");
                public static readonly GUIContent allowHDROutput = EditorGUIUtility.TrTextContent("HDR Output", "Whether to support outputting to HDR displays.");

                public static string cameraTargetTextureMSAA = L10n.Tr("Camera target texture requires {0}x MSAA. Universal pipeline {1}.");
                public static string pipelineMSAACapsSupportSamples = L10n.Tr("is set to support {0}x");
                public static string pipelineMSAACapsDisabled = L10n.Tr("has MSAA disabled");
                public static string disabledHDRRenderingWithHDROutput = L10n.Tr("HDR Output is enabled but HDR rendering is disabled. Image may appear underexposed or oversaturated on an HDR display.");
            }
        }
    }
}
