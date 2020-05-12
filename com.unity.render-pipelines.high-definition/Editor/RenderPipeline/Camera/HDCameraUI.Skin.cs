using UnityEditor.Rendering;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    static partial class HDCameraUI
    {
        const string generalSettingsHeaderContent = "General";
        const string physicalSettingsHeaderContent = "Physical";
        const string outputSettingsHeaderContent = "Output";

        const string clippingPlaneMultiFieldTitle = "Clipping Planes";

        const string msaaWarningMessage = "Manual MSAA target set with deferred rendering. This will lead to undefined behavior.";

        static readonly GUIContent clearModeContent = EditorGUIUtility.TrTextContent("Background Type", "Specifies the type of background the Camera applies when it clears the screen before rendering a frame. Be aware that when setting this to None, the background is never cleared and since HDRP shares render texture between cameras, you may end up with garbage from previous rendering.");
        static readonly GUIContent backgroundColorContent = EditorGUIUtility.TrTextContent("Background Color", "The Background Color used to clear the screen when selecting Background Color before rendering.");
        static readonly GUIContent cullingMaskContent = EditorGUIUtility.TrTextContent("Culling Mask");
        static readonly GUIContent volumeLayerMaskContent = EditorGUIUtility.TrTextContent("Volume Layer Mask");
        static readonly GUIContent volumeAnchorOverrideContent = EditorGUIUtility.TrTextContent("Volume Anchor Override");
        static readonly GUIContent occlusionCullingContent = EditorGUIUtility.TrTextContent("Occlusion Culling");

        static readonly GUIContent projectionContent = EditorGUIUtility.TrTextContent("Projection", "How the Camera renders perspective.\n\nChoose Perspective to render objects with perspective.\n\nChoose Orthographic to render objects uniformly, with no sense of perspective.");
        static readonly GUIContent sizeContent = EditorGUIUtility.TrTextContent("Size");
        static readonly GUIContent fieldOfViewContent = EditorGUIUtility.TrTextContent("Field of View", "The height of the Camera’s view angle, measured in degrees along the local Y axis.");
        static readonly GUIContent focalLengthContent = EditorGUIUtility.TrTextContent("Focal Length", "The simulated distance between the lens and the sensor of the physical camera. Larger values give a narrower field of view.");
        static readonly GUIContent FOVAxisModeContent = EditorGUIUtility.TrTextContent("FOV Axis", "Field of view axis.");
        static readonly GUIContent sensorSizeContent = EditorGUIUtility.TrTextContent("Sensor Size", "The size of the camera sensor in millimeters.");
        static readonly GUIContent lensShiftContent = EditorGUIUtility.TrTextContent("Shift", "Offset from the camera sensor. Use these properties to simulate a shift lens. Measured as a multiple of the sensor size.");
        static readonly GUIContent physicalCameraContent = EditorGUIUtility.TrTextContent("Link FOV to Physical Camera", "Enables Physical camera mode for FOV calculation. When checked, the field of view is calculated from properties for simulating physical attributes (focal length, sensor size, and lens shift).");
        static readonly GUIContent cameraTypeContent = EditorGUIUtility.TrTextContent("Sensor Type", "Common sensor sizes. Choose an item to set Sensor Size, or edit Sensor Size for your custom settings.");
        static readonly GUIContent gateFitContent = EditorGUIUtility.TrTextContent("Gate Fit", "Determines how the rendered area (resolution gate) fits into the sensor area (film gate).");
        static readonly GUIContent nearPlaneContent = EditorGUIUtility.TrTextContent("Near", "The closest point relative to the camera that drawing occurs.");
        static readonly GUIContent farPlaneContent = EditorGUIUtility.TrTextContent("Far", "The furthest point relative to the camera that drawing occurs.");
        static readonly GUIContent probeLayerMaskContent = EditorGUIUtility.TrTextContent("Probe Layer Mask", "The layer mask to use to cull probe influences.");
        static readonly GUIContent fullScreenPassthroughContent = EditorGUIUtility.TrTextContent("Fullscreen Passthrough", "This will skip rendering settings to directly rendering in fullscreen(for instance: Useful for video)");

        static readonly GUIContent renderingPathContent = EditorGUIUtility.TrTextContent("Custom Frame Settings", "Define the custom Frame Settings for this Camera to use.");

        // TODO: Tooltips
        static readonly GUIContent isoContent = EditorGUIUtility.TrTextContent("Iso");
        static readonly GUIContent shutterSpeedContent = EditorGUIUtility.TrTextContent("Shutter Speed");
        static readonly GUIContent apertureContent = EditorGUIUtility.TrTextContent("Aperture");
        static readonly GUIContent bladeCountContent = EditorGUIUtility.TrTextContent("Blade Count");
        static readonly GUIContent curvatureContent = EditorGUIUtility.TrTextContent("Curvature");
        static readonly GUIContent barrelClippingContent = EditorGUIUtility.TrTextContent("Barrel Clipping");
        static readonly GUIContent anamorphismContent = EditorGUIUtility.TrTextContent("Anamorphism");

        static readonly GUIContent antialiasingContent = EditorGUIUtility.TrTextContent("Anti-aliasing", "The anti-aliasing method to use.");
        static readonly GUIContent SMAAQualityPresetContent = EditorGUIUtility.TrTextContent("SMAA Quality Preset", "The quality preset for SMAA, low has the best performance but worst quality, High has the highest quality but worst performance.");
        static readonly GUIContent TAASharpenContent = EditorGUIUtility.TrTextContent("TAA Sharpen Strength", "The intensity of the sharpen filter used to counterbalance the blur introduced by TAA. A high value might create artifacts such as dark lines depending on the frame content.");

        static readonly GUIContent ditheringContent = EditorGUIUtility.TrTextContent("Dithering", "Should we apply 8-bit dithering to the final render?");
        static readonly GUIContent stopNaNsContent = EditorGUIUtility.TrTextContent("Stop NaNs", "Automatically replaces NaN/Inf in shaders by a black pixel to avoid breaking some effects. This will slightly affect performances and should only be used if you experience NaN issues that you can't fix.");

        static readonly GUIContent allowDynResContent = EditorGUIUtility.TrTextContent("Allow Dynamic Resolution", "Whether to support dynamic resolution.");

        static readonly GUIContent viewportContent = EditorGUIUtility.TrTextContent("Viewport Rect", "Four values that indicate where on the screen HDRP draws this Camera view. Measured in Viewport Coordinates (values in the range of [0, 1]).");
        static readonly GUIContent depthContent = EditorGUIUtility.TrTextContent("Depth");
        static readonly GUIContent xrRenderingContent = EditorGUIUtility.TrTextContent("XR Rendering");

#if ENABLE_MULTIPLE_DISPLAYS
        static readonly GUIContent targetDisplayContent = EditorGUIUtility.TrTextContent("Target Display");
#endif

        static readonly GUIContent[] antialiasingModeNames =
        {
            new GUIContent("No Anti-aliasing"),
            new GUIContent("Fast Approximate Anti-aliasing (FXAA)"),
            new GUIContent("Temporal Anti-aliasing (TAA)"),
            new GUIContent("Subpixel Morphological Anti-aliasing (SMAA)")
        };
    }
}
