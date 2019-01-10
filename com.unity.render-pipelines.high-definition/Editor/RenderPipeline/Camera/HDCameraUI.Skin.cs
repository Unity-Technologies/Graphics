using UnityEditor.Rendering;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    static partial class HDCameraUI
    {
        const string generalSettingsHeaderContent = "General";
        const string physicalSettingsHeaderContent = "Physical Settings";
        const string outputSettingsHeaderContent = "Output Settings";
        const string xrSettingsHeaderContent = "XR Settings";

        const string clippingPlaneMultiFieldTitle = "Clipping Planes";

        const string msaaWarningMessage = "Manual MSAA target set with deferred rendering. This will lead to undefined behavior.";

        static readonly GUIContent clearModeContent = EditorGUIUtility.TrTextContent("Clear Mode", "The Camera clears the screen to selected mode.");
        static readonly GUIContent backgroundColorContent = EditorGUIUtility.TrTextContent("Background Color", "TThe BackgroundColor used to clear the screen when selecting BackgrounColor before rendering.");
        static readonly GUIContent clearDepthContent = EditorGUIUtility.TrTextContent("ClearDepth", "TThe Camera clears the depth buffer before rendering.");
        static readonly GUIContent cullingMaskContent = EditorGUIUtility.TrTextContent("Culling Mask");
        static readonly GUIContent volumeLayerMaskContent = EditorGUIUtility.TrTextContent("Volume Layer Mask");
        static readonly GUIContent volumeAnchorOverrideContent = EditorGUIUtility.TrTextContent("Volume Anchor Override");
        static readonly GUIContent occlusionCullingContent = EditorGUIUtility.TrTextContent("Occlusion Culling");

        static readonly GUIContent projectionContent = EditorGUIUtility.TrTextContent("Projection", "THow the Camera renders perspective.\n\nChoose Perspective to render objects with perspective.\n\nChoose Orthographic to render objects uniformly, with no sense of perspective.");
        static readonly GUIContent sizeContent = EditorGUIUtility.TrTextContent("Size", "TThe vertical size of the camera view.");
        static readonly GUIContent fieldOfViewContent = EditorGUIUtility.TrTextContent("Field of View", "TThe camera’s view angle measured in degrees along the selected axis.");
        static readonly GUIContent focalLengthContent = EditorGUIUtility.TrTextContent("Focal Length", "TThe simulated distance between the lens and the sensor of the physical camera. Larger values give a narrower field of view.");
        static readonly GUIContent FOVAxisModeContent = EditorGUIUtility.TrTextContent("FOV Axis", "TField of view axis.");
        static readonly GUIContent sensorSizeContent = EditorGUIUtility.TrTextContent("Sensor Size", "TThe size of the camera sensor in millimeters.");
        static readonly GUIContent lensShiftContent = EditorGUIUtility.TrTextContent("Shift", "TOffset from the camera sensor. Use these properties to simulate a shift lens. Measured as a multiple of the sensor size.");
        static readonly GUIContent physicalCameraContent = EditorGUIUtility.TrTextContent("Link FOV to Physical Camera", "TEnables Physical camera mode for FOV calculation. When checked, the field of view is calculated from properties for simulating physical attributes (focal length, sensor size, and lens shift).");
        static readonly GUIContent cameraTypeContent = EditorGUIUtility.TrTextContent("Sensor Type", "TCommon sensor sizes. Choose an item to set Sensor Size, or edit Sensor Size for your custom settings.");
        static readonly GUIContent gateFitContent = EditorGUIUtility.TrTextContent("Gate Fit", "TDetermines how the rendered area (resolution gate) fits into the sensor area (film gate).");
        static readonly GUIContent nearPlaneContent = EditorGUIUtility.TrTextContent("Near", "TThe closest point relative to the camera that drawing will occur.");
        static readonly GUIContent farPlaneContent = EditorGUIUtility.TrTextContent("Far", "TThe furthest point relative to the camera that drawing will occur.");
        static readonly GUIContent probeLayerMaskContent = EditorGUIUtility.TrTextContent("Probe Layer Mask", "The layer mask to use to cull probe influences.");
        
        static readonly GUIContent renderingPathContent = EditorGUIUtility.TrTextContent("Custom Frame Settings", "Here, you must select which settings to override. If you do enable a specific override, the setting uses the pipeline default.");

        // TODO: Tooltips
        static readonly GUIContent isoContent = EditorGUIUtility.TrTextContent("Iso");
        static readonly GUIContent shutterSpeedContent = EditorGUIUtility.TrTextContent("Shutter Speed");
        static readonly GUIContent apertureContent = EditorGUIUtility.TrTextContent("Aperture");
        static readonly GUIContent bladeCountContent = EditorGUIUtility.TrTextContent("Blade Count");
        static readonly GUIContent curvatureContent = EditorGUIUtility.TrTextContent("Curvature");
        static readonly GUIContent barrelClippingContent = EditorGUIUtility.TrTextContent("Barrel Clipping");
        static readonly GUIContent anamorphismContent = EditorGUIUtility.TrTextContent("Anamorphism");

        static readonly GUIContent antialiasingContent = EditorGUIUtility.TrTextContent("Anti-aliasing", "TThe anti-aliasing method to use.");
        static readonly GUIContent ditheringContent = EditorGUIUtility.TrTextContent("Dithering", "TShould we apply 8-bit dithering to the final render?");

        static readonly GUIContent viewportContent = EditorGUIUtility.TrTextContent("Viewport Rect", "TFour values that indicate where on the screen this camera view will be drawn. Measured in Viewport Coordinates (values 0–1).");
        static readonly GUIContent depthContent = EditorGUIUtility.TrTextContent("Depth");
      
#if ENABLE_MULTIPLE_DISPLAYS
        static readonly GUIContent targetDisplayContent = EditorGUIUtility.TrTextContent("Target Display");
#endif


        static readonly GUIContent stereoSeparationContent = EditorGUIUtility.TrTextContent("Stereo Separation");
        static readonly GUIContent stereoConvergenceContent = EditorGUIUtility.TrTextContent("Stereo Convergence");
        static readonly GUIContent targetEyeContent = EditorGUIUtility.TrTextContent("Target Eye");
        static readonly GUIContent[] k_TargetEyes = //order must match k_TargetEyeValues
        {
            new GUIContent("Both"),
            new GUIContent("Left"),
            new GUIContent("Right"),
            new GUIContent("None (Main Display)"),
        };

        static readonly GUIContent[] antialiasingModeNames =
        {
            new GUIContent("No Anti-aliasing"),
            new GUIContent("Fast Approximate Anti-aliasing (FXAA)"),
            new GUIContent("Temporal Anti-aliasing (TAA)")
        };
    }
}
