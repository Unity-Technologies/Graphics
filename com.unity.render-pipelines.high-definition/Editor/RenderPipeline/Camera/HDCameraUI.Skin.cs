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
        static readonly GUIContent cullingMaskContent = EditorGUIUtility.TrTextContent("Culling Mask", "Specifies the list of layers the camera should render.");
        static readonly GUIContent volumeLayerMaskContent = EditorGUIUtility.TrTextContent("Volume Layer Mask");
        static readonly GUIContent volumeAnchorOverrideContent = EditorGUIUtility.TrTextContent("Volume Anchor Override");
        static readonly GUIContent occlusionCullingContent = EditorGUIUtility.TrTextContent("Occlusion Culling", "When enabled, the camera does not render objects that are being obscured by other geometry.");

        static readonly GUIContent exposureTargetContent = EditorGUIUtility.TrTextContent("Exposure Target", "The object used as a target for centering the Exposure's Procedural Mask metering mode when target object option is set (See Exposure Volume Component).");

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
        static readonly GUIContent isoContent = EditorGUIUtility.TrTextContent("Iso", "Sets the light sensitivity of the Camera sensor. This property affects Exposure if you set its Mode to Use Physical Camera.");
        static readonly GUIContent shutterSpeedContent = EditorGUIUtility.TrTextContent("Shutter Speed", "The amount of time the Camera sensor is capturing light.");
        static readonly GUIContent apertureContent = EditorGUIUtility.TrTextContent("Aperture", "The f-stop (f-number) of the lens. Lower values give a wider lens aperture.");
        static readonly GUIContent bladeCountContent = EditorGUIUtility.TrTextContent("Blade Count", "The number of blades in the lens aperture. Higher values give a rounder aperture shape.");
        static readonly GUIContent curvatureContent = EditorGUIUtility.TrTextContent("Curvature", "Controls the curvature of the lens aperture blades. The minimum value results in fully-curved, perfectly-circular bokeh, and the maximum value results in visible aperture blades.");
        static readonly GUIContent barrelClippingContent = EditorGUIUtility.TrTextContent("Barrel Clipping", "Controls the self-occlusion of the lens, creating a cat's eye effect.");
        static readonly GUIContent anamorphismContent = EditorGUIUtility.TrTextContent("Anamorphism", "Use the slider to stretch the sensor to simulate an anamorphic look.");

        static readonly GUIContent antialiasingContent = EditorGUIUtility.TrTextContent("Post Anti-aliasing", "The postprocess anti-aliasing method to use.");
        static readonly GUIContent SMAAQualityPresetContent = EditorGUIUtility.TrTextContent("SMAA Quality Preset", "The quality preset for SMAA, low has the best performance but worst quality, High has the highest quality but worst performance.");
        static readonly GUIContent TAASharpenContent = EditorGUIUtility.TrTextContent("TAA Sharpen Strength", "The intensity of the sharpen filter used to counterbalance the blur introduced by TAA. A high value might create artifacts such as dark lines depending on the frame content.");
        static readonly GUIContent TAAHistorySharpening = EditorGUIUtility.TrTextContent("TAA History Sharpening", "Values closer to 0 lead to softer look when movement is detected, but can further reduce aliasing. Values closer to 1 lead to sharper results, with the risk of reintroducing a bit of aliasing.");
        static readonly GUIContent TAAAntiFlicker = EditorGUIUtility.TrTextContent("TAA Anti-flickering", "With high values flickering might be reduced, but it can lead to more ghosting or disocclusion artifacts.");
        static readonly GUIContent TAAMotionVectorRejection = EditorGUIUtility.TrTextContent("TAA Speed Rejection", "Higher this value, more likely history will be rejected when current and reprojected history motion vector differ by a substantial amount. High values can decrease ghosting but will also reintroduce aliasing on the aforementioned cases.");
        static readonly GUIContent TAAQualityLevelContent = EditorGUIUtility.TrTextContent("TAA Quality Preset", "Low quality is fast, but can lead to more ghosting and blurrier output when moving, Medium quality has better ghosting handling and sharper results upon movement, High allows for velocity rejection policy, has better antialiasing and has mechanism to combat ringing for over sharpening the history.");
        static readonly GUIContent TAAAntiRingingContent = EditorGUIUtility.TrTextContent("TAA Anti-ringing", "When enabled, ringing artifacts (dark or strangely saturated edges) caused by history sharpening will be improved. This comes at a potential loss of sharpness upon motion.");

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
