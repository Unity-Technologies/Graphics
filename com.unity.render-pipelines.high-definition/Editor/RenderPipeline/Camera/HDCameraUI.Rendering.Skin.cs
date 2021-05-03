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

                public static readonly GUIContent SMAAQualityPresetContent = EditorGUIUtility.TrTextContent("Quality Preset", "The quality preset for SMAA, low has the best performance but worst quality, High has the highest quality but worst performance.");

                public static readonly GUIContent TAASharpen = EditorGUIUtility.TrTextContent("Sharpen Strength", "The intensity of the sharpen filter used to counterbalance the blur introduced by TAA. A high value might create artifacts such as dark lines depending on the frame content.");
                public static readonly GUIContent TAAHistorySharpening = EditorGUIUtility.TrTextContent("History Sharpening", "Values closer to 0 lead to softer look when movement is detected, but can further reduce aliasing. Values closer to 1 lead to sharper results, with the risk of reintroducing a bit of aliasing.");
                public static readonly GUIContent TAAAntiFlicker = EditorGUIUtility.TrTextContent("Anti-flickering", "With high values flickering might be reduced, but it can lead to more ghosting or disocclusion artifacts.");
                public static readonly GUIContent TAAMotionVectorRejection = EditorGUIUtility.TrTextContent("Speed Rejection", "Higher this value, more likely history will be rejected when current and reprojected history motion vector differ by a substantial amount. High values can decrease ghosting but will also reintroduce aliasing on the aforementioned cases.");
                public static readonly GUIContent TAAQualityLevel = EditorGUIUtility.TrTextContent("Quality Preset", "Low quality is fast, but can lead to more ghosting and blurrier output when moving, Medium quality has better ghosting handling and sharper results upon movement, High allows for velocity rejection policy, has better antialiasing and has mechanism to combat ringing for over sharpening the history.");
                public static readonly GUIContent TAAAntiRinging = EditorGUIUtility.TrTextContent("Anti-ringing", "When enabled, ringing artifacts (dark or strangely saturated edges) caused by history sharpening will be improved. This comes at a potential loss of sharpness upon motion.");

                public static readonly GUIContent renderingPath = EditorGUIUtility.TrTextContent("Custom Frame Settings", "Define custom values for Frame Settings for this Camera to use.");
                public static readonly GUIContent fullScreenPassthrough = EditorGUIUtility.TrTextContent("Fullscreen Passthrough", "This will skip rendering settings to directly rendering in fullscreen(for instance: Useful for video)");
                public static readonly GUIContent exposureTarget = EditorGUIUtility.TrTextContent("Exposure Target", "The object used as a target for centering the Exposure's Procedural Mask metering mode when target object option is set (See Exposure Volume Component).");
            }
        }
    }
}
