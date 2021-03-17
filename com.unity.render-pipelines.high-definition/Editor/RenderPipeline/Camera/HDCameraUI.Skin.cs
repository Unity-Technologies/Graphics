using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    static partial class HDCameraUI
    {
        class Styles
        {
            public static GUIContent projectionSettingsHeaderContent { get; } = EditorGUIUtility.TrTextContent("Projection");
            public static GUIContent outputSettingsHeaderContent { get; } = EditorGUIUtility.TrTextContent("Output");

            public static GUIContent clippingPlaneMultiFieldTitle = EditorGUIUtility.TrTextContent("Clipping Planes");

            public const string msaaWarningMessage = "Manual MSAA target set with deferred rendering. This will lead to undefined behavior.";

            public static readonly GUIContent cullingMaskContent = EditorGUIUtility.TrTextContent("Culling Mask");

            public static readonly GUIContent projectionContent = EditorGUIUtility.TrTextContent("Projection", "How the Camera renders perspective.\n\nChoose Perspective to render objects with perspective.\n\nChoose Orthographic to render objects uniformly, with no sense of perspective.");
            public static readonly GUIContent sizeContent = EditorGUIUtility.TrTextContent("Size");
            public static readonly GUIContent fieldOfViewContent = EditorGUIUtility.TrTextContent("Field of View", "The height of the Camera’s view angle, measured in degrees along the local Y axis.");
            public static readonly GUIContent FOVAxisModeContent = EditorGUIUtility.TrTextContent("Field of View Axis", "Field of view axis.");
            public static readonly GUIContent physicalCameraContent = EditorGUIUtility.TrTextContent("Physical Camera", "Enables Physical camera mode for FOV calculation. When checked, the field of view is calculated from properties for simulating physical attributes (focal length, sensor size, and lens shift).");
            public static readonly GUIContent nearPlaneContent = EditorGUIUtility.TrTextContent("Near", "The closest point relative to the camera that drawing occurs.");
            public static readonly GUIContent farPlaneContent = EditorGUIUtility.TrTextContent("Far", "The furthest point relative to the camera that drawing occurs.");

            // TODO: Tooltips
            public static readonly GUIContent antialiasingContent = EditorGUIUtility.TrTextContent("Post Anti-aliasing", "The postprocess anti-aliasing method to use.");
            public static readonly GUIContent SMAAQualityPresetContent = EditorGUIUtility.TrTextContent("SMAA Quality Preset", "The quality preset for SMAA, low has the best performance but worst quality, High has the highest quality but worst performance.");
            public static readonly GUIContent TAASharpenContent = EditorGUIUtility.TrTextContent("TAA Sharpen Strength", "The intensity of the sharpen filter used to counterbalance the blur introduced by TAA. A high value might create artifacts such as dark lines depending on the frame content.");
            public static readonly GUIContent TAAHistorySharpening = EditorGUIUtility.TrTextContent("TAA History Sharpening", "Values closer to 0 lead to softer look when movement is detected, but can further reduce aliasing. Values closer to 1 lead to sharper results, with the risk of reintroducing a bit of aliasing.");
            public static readonly GUIContent TAAAntiFlicker = EditorGUIUtility.TrTextContent("TAA Anti-flickering", "With high values flickering might be reduced, but it can lead to more ghosting or disocclusion artifacts.");
            public static readonly GUIContent TAAMotionVectorRejection = EditorGUIUtility.TrTextContent("TAA Speed Rejection", "Higher this value, more likely history will be rejected when current and reprojected history motion vector differ by a substantial amount. High values can decrease ghosting but will also reintroduce aliasing on the aforementioned cases.");
            public static readonly GUIContent TAAQualityLevelContent = EditorGUIUtility.TrTextContent("TAA Quality Preset", "Low quality is fast, but can lead to more ghosting and blurrier output when moving, Medium quality has better ghosting handling and sharper results upon movement, High allows for velocity rejection policy, has better antialiasing and has mechanism to combat ringing for over sharpening the history.");
            public static readonly GUIContent TAAAntiRingingContent = EditorGUIUtility.TrTextContent("TAA Anti-ringing", "When enabled, ringing artifacts (dark or strangely saturated edges) caused by history sharpening will be improved. This comes at a potential loss of sharpness upon motion.");

            public static readonly GUIContent ditheringContent = EditorGUIUtility.TrTextContent("Dithering", "Should we apply 8-bit dithering to the final render?");
            public static readonly GUIContent stopNaNsContent = EditorGUIUtility.TrTextContent("Stop NaNs", "Automatically replaces NaN/Inf in shaders by a black pixel to avoid breaking some effects. This will slightly affect performances and should only be used if you experience NaN issues that you can't fix.");

            public static readonly GUIContent allowDynResContent = EditorGUIUtility.TrTextContent("Allow Dynamic Resolution", "Whether to support dynamic resolution.");

            public static readonly GUIContent viewportContent = EditorGUIUtility.TrTextContent("Viewport Rect", "Four values that indicate where on the screen HDRP draws this Camera view. Measured in Viewport Coordinates (values in the range of [0, 1]).");
            public static readonly GUIContent depthContent = EditorGUIUtility.TrTextContent("Depth");
            public static readonly GUIContent xrRenderingContent = EditorGUIUtility.TrTextContent("XR Rendering");

#if ENABLE_MULTIPLE_DISPLAYS
            public static readonly GUIContent targetDisplayContent = EditorGUIUtility.TrTextContent("Target Display");
#endif

            public static readonly GUIContent[] antialiasingModeNames =
            {
                EditorGUIUtility.TrTextContent("No Anti-aliasing"),
                EditorGUIUtility.TrTextContent("Fast Approximate Anti-aliasing (FXAA)"),
                EditorGUIUtility.TrTextContent("Temporal Anti-aliasing (TAA)"),
                EditorGUIUtility.TrTextContent("Subpixel Morphological Anti-aliasing (SMAA)")
            };
        }
    }
}
