using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    static partial class HDCameraUI
    {
        partial class Rendering
        {
            class Styles
            {
                public static readonly GUIContent antialiasing = EditorGUIUtility.TrTextContent("Post Anti-aliasing", "The postprocess anti-aliasing method to use.");
                public static readonly GUIContent antialiasingContentFallback = EditorGUIUtility.TrTextContent("Fallback Post Anti-aliasing", "The postprocess anti-aliasing method to use as a fallback.");

                public static readonly GUIContent SMAAQualityPresetContent = EditorGUIUtility.TrTextContent("Quality Preset", "The quality preset for SMAA, low has the best performance but worst quality, High has the highest quality but worst performance.");

                public static readonly GUIContent TAASharpen = EditorGUIUtility.TrTextContent("Sharpen Strength", "The intensity of the sharpen filter used to counterbalance the blur introduced by TAA. A high value might create artifacts such as dark lines depending on the frame content.");
                public static readonly GUIContent TAAHistorySharpening = EditorGUIUtility.TrTextContent("History Sharpening", "Values closer to 0 lead to softer look when movement is detected, but can further reduce aliasing. Values closer to 1 lead to sharper results, with the risk of reintroducing a bit of aliasing.");
                public static readonly GUIContent TAAAntiFlicker = EditorGUIUtility.TrTextContent("Anti-flickering", "With high values flickering might be reduced, but it can lead to more ghosting or disocclusion artifacts.");
                public static readonly GUIContent TAAMotionVectorRejection = EditorGUIUtility.TrTextContent("Speed Rejection", "Higher this value, more likely history will be rejected when current and reprojected history motion vector differ by a substantial amount. High values can decrease ghosting but will also reintroduce aliasing on the aforementioned cases.");
                public static readonly GUIContent TAAQualityLevel = EditorGUIUtility.TrTextContent("Quality Preset", "Low quality is fast, but can lead to more ghosting and blurrier output when moving, Medium quality has better ghosting handling and sharper results upon movement, High allows for velocity rejection policy, has better antialiasing and has mechanism to combat ringing for over sharpening the history.");
                public static readonly GUIContent TAAAntiRinging = EditorGUIUtility.TrTextContent("Anti-ringing", "When enabled, ringing artifacts (dark or strangely saturated edges) caused by history sharpening will be improved. This comes at a potential loss of sharpness upon motion.");
                // Advanced TAA
                public static readonly GUIContent TAABaseBlendFactor = EditorGUIUtility.TrTextContent("Base blend factor", "Determines how much the history buffer is blended together with current frame result. Higher values means more history contribution, which leads to better anti aliasing, but also more prone to ghosting.");


                public static readonly GUIContent renderingPath = EditorGUIUtility.TrTextContent("Custom Frame Settings", "Define custom values for Frame Settings for this Camera to use.");
                public static readonly GUIContent fullScreenPassthrough = EditorGUIUtility.TrTextContent("Fullscreen Passthrough", "This will skip rendering settings to directly rendering in fullscreen(for instance: Useful for video)");
                public static readonly GUIContent exposureTarget = EditorGUIUtility.TrTextContent("Exposure Target", "The object used as a target for centering the Exposure's Procedural Mask metering mode when target object option is set (See Exposure Volume Component).");

                public static readonly GUIContent allowDynamicResolution = EditorGUIUtility.TrTextContent("Allow Dynamic Resolution", "Whether to support dynamic resolution.");
                public const string taauInfoBox = "When TAA Upsample is enabled, TAA is run as antialiasing algorithm and uses High Quality as base, to select other anti-aliasing methods please change upscale filter for dynamic resolution.";

                public const string DLSSFeatureDetectedMsg = "Unity detected NVIDIA Deep Learning Super Sampling and will ignore the Fallback Anti Aliasing Method.";
                public const string DLSSFeatureNotDetectedMsg = "Unity cannot detect NVIDIA Deep Learning Super Sampling and will use the Fallback Anti Aliasing Method instead.";
                public const string DLSSNotEnabledInQualityAsset = "The quality asset in this project does not have NVIDIA Deep Learning Super Sampling (DLSS) enabled. DLSS will not be running on this camera.";
                public static readonly GUIContent DLSSAllow = EditorGUIUtility.TrTextContent("Allow DLSS", "Allows DLSS for this camera. For the effect to be enabled, it must be set in the quality asset of this project.");
                public static readonly GUIContent DLSSCustomQualitySettings = EditorGUIUtility.TrTextContent("Use Custom Quality", "");
                public static readonly GUIContent DLSSUseCustomAttributes = EditorGUIUtility.TrTextContent("Use Custom Attributes", "");
                public static GUIContent overrideSettingText { get; } = EditorGUIUtility.TrTextContent("", "If enabled, this camera setting will be used instead of the one specified in the quality asset of this project.");
            }
        }
    }
}
