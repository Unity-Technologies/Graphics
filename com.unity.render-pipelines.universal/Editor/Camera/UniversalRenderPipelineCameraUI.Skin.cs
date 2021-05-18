using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal static partial class UniversalRenderPipelineCameraUI
    {
        public class Styles
        {
            public static GUIContent projectionSettingsText = EditorGUIUtility.TrTextContent("Projection", "These settings control how the camera views the world.");
            public static GUIContent environmentSettingsText = EditorGUIUtility.TrTextContent("Environment", "These settings control what the camera background looks like.");
            public static GUIContent outputSettingsText = EditorGUIUtility.TrTextContent("Output", "These settings control how the camera output is formatted.");
            public static GUIContent renderingSettingsText = EditorGUIUtility.TrTextContent("Rendering", "These settings control for the specific rendering features for this camera.");
            public static GUIContent stackSettingsText = EditorGUIUtility.TrTextContent("Stack", "The list of overlay cameras assigned to this camera.");

            public static GUIContent backgroundType = EditorGUIUtility.TrTextContent("Background Type", "Controls how to initialize the Camera's background.\n\nSkybox initializes camera with Skybox, defaulting to a background color if no skybox is found.\n\nSolid Color initializes background with the background color.\n\nUninitialized has undefined values for the camera background. Use this only if you are rendering all pixels in the Camera's view.");
            public static GUIContent cameraType = EditorGUIUtility.TrTextContent("Render Type", "Defines if a camera renders directly to a target or overlays on top of another camera’s output. Overlay option is not available when Deferred Render Data is in use.");
            public static GUIContent renderingShadows = EditorGUIUtility.TrTextContent("Render Shadows", "Makes this camera render shadows.");
            public static GUIContent requireDepthTexture = EditorGUIUtility.TrTextContent("Depth Texture", "On makes this camera create a _CameraDepthTexture, which is a copy of the rendered depth values.\nOff makes the camera not create a depth texture.\nUse Pipeline Settings applies settings from the Render Pipeline Asset.");
            public static GUIContent requireOpaqueTexture = EditorGUIUtility.TrTextContent("Opaque Texture", "On makes this camera create a _CameraOpaqueTexture, which is a copy of the rendered view.\nOff makes the camera not create an opaque texture.\nUse Pipeline Settings applies settings from the Render Pipeline Asset.");
            public static GUIContent allowMSAA = EditorGUIUtility.TrTextContent("MSAA", "Use Multi Sample Anti-Aliasing to reduce aliasing.");
            public static GUIContent allowHDR = EditorGUIUtility.TrTextContent("HDR", "High Dynamic Range gives you a wider range of light intensities, so your lighting looks more realistic. With it, you can still see details and experience less saturation even with bright light.", (Texture)null);
            public static GUIContent priority = EditorGUIUtility.TrTextContent("Priority", "A camera with a higher priority is drawn on top of a camera with a lower priority [ -100, 100 ].");
            public static GUIContent clearDepth = EditorGUIUtility.TrTextContent("Clear Depth", "If enabled, depth from the previous camera will be cleared.");

            public static GUIContent rendererType = EditorGUIUtility.TrTextContent("Renderer", "Controls which renderer this camera uses.");

            public static GUIContent volumeLayerMask = EditorGUIUtility.TrTextContent("Volume Mask", "This camera will only be affected by volumes in the selected scene-layers.");
            public static GUIContent volumeTrigger = EditorGUIUtility.TrTextContent("Volume Trigger", "A transform that will act as a trigger for volume blending. If none is set, the camera itself will act as a trigger.");

            public static GUIContent renderPostProcessing = EditorGUIUtility.TrTextContent("Post Processing", "Enable this to make this camera render post-processing effects.");
            public static GUIContent antialiasing = EditorGUIUtility.TrTextContent("Anti-aliasing", "The anti-aliasing method to use.");
            public static GUIContent antialiasingQuality = EditorGUIUtility.TrTextContent("Quality", "The quality level to use for the selected anti-aliasing method.");
            public static GUIContent stopNaN = EditorGUIUtility.TrTextContent("Stop NaN", "Automatically replaces NaN/Inf in shaders by a black pixel to avoid breaking some effects. This will affect performances and should only be used if you experience NaN issues that you can't fix. Has no effect on GLES2 platforms.");
            public static GUIContent dithering = EditorGUIUtility.TrTextContent("Dithering", "Applies 8-bit dithering to the final render to reduce color banding.");

            public static GUIContent cameras = EditorGUIUtility.TrTextContent("Cameras", "The list of overlay cameras assigned to this camera.");

#if ENABLE_VR && ENABLE_XR_MODULE
            public static GUIContent[] xrTargetEyeOptions =
            {
                EditorGUIUtility.TrTextContent("None"),
                EditorGUIUtility.TrTextContent("Both"),
            };
            public static int[] xrTargetEyeValues = { 0, 1 };
            public static readonly GUIContent xrTargetEye = EditorGUIUtility.TrTextContent("Target Eye", "Allows XR rendering if target eye sets to both eye. Disable XR for this camera otherwise.");
#endif
            public static readonly GUIContent targetTextureLabel = EditorGUIUtility.TrTextContent("Output Texture", "The texture to render this camera into, if none then this camera renders to screen.");

            public static readonly string hdrDisabledWarning = "HDR rendering is disabled in the Universal Render Pipeline asset.";
            public static readonly string mssaDisabledWarning = "Anti-aliasing is disabled in the Universal Render Pipeline asset.";

            public static readonly string missingRendererWarning = "The currently selected Renderer is missing from the Universal Render Pipeline asset.";
            public static readonly string noRendererError = "There are no valid Renderers available on the Universal Render Pipeline asset.";
            public static readonly string disabledPostprocessing = "Post Processing is currently disabled on the current Universal Render Pipeline renderer.";

            public static readonly string pixelPerfectInfo = "Projection settings have been overriden by the Pixel Perfect Camera.";

            public static GUIContent[] cameraBackgroundType =
            {
                EditorGUIUtility.TrTextContent("Skybox"),
                EditorGUIUtility.TrTextContent("Solid Color"),
                EditorGUIUtility.TrTextContent("Uninitialized"),
            };

            public static int[] cameraBackgroundValues = { 0, 1, 2 };

            // Using the pipeline Settings
            public static GUIContent[] displayedCameraOptions =
            {
                EditorGUIUtility.TrTextContent("Off"),
                EditorGUIUtility.TrTextContent("Use Pipeline Settings"),
            };

            public static int[] cameraOptions = { 0, 1 };

            // Beautified anti-aliasing options
            public static GUIContent[] antialiasingOptions =
            {
                EditorGUIUtility.TrTextContent("None"),
                EditorGUIUtility.TrTextContent("Fast Approximate Anti-aliasing (FXAA)"),
                EditorGUIUtility.TrTextContent("Subpixel Morphological Anti-aliasing (SMAA)"),
            };
            public static int[] antialiasingValues = { 0, 1, 2 };

            public static string inspectorOverlayCameraText = L10n.Tr("Inspector Overlay Camera");
        }
    }
}
