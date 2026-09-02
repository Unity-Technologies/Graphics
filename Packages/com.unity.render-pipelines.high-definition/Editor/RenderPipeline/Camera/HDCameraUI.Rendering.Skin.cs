using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    static partial class HDCameraUI
    {
        partial class Rendering
        {
            class Styles
            {
                public static readonly GUIContent antialiasing = L10n.TextContent("Post Anti-aliasing", "The postprocess anti-aliasing method to use.", null, null);
                public static readonly GUIContent antialiasingContentFallback = L10n.TextContent("Fallback Post Anti-aliasing", "The postprocess anti-aliasing method to use as a fallback.", null, null);

                public static readonly GUIContent SMAAQualityPresetContent = L10n.TextContent("Quality Preset", "The quality preset for SMAA, low has the best performance but worst quality, High has the highest quality but worst performance.", null, null);

                public static readonly GUIContent TAASharpen = L10n.TextContent("Sharpen Strength", "The intensity of the sharpen filter used to counterbalance the blur introduced by TAA. A high value might create artifacts such as dark lines depending on the frame content.", null, null);
                public static readonly GUIContent TAARingingReduction = L10n.TextContent("Ringing Reduction", "How much the sharpening result is taken from the result without ringing. This reduces unnatural dark outlines, but might also decrease the impact of sharpening. Values above 0.0 lead to extra cost.", null, null);

                public static readonly GUIContent TAAHistorySharpening = L10n.TextContent("History Sharpening", "Values closer to 0 lead to softer look when movement is detected, but can further reduce aliasing. Values closer to 1 lead to sharper results, with the risk of reintroducing a bit of aliasing.", null, null);
                public static readonly GUIContent TAAAntiFlicker = L10n.TextContent("Anti-flickering", "With high values flickering might be reduced, but it can lead to more ghosting or disocclusion artifacts.", null, null);
                public static readonly GUIContent TAAMotionVectorRejection = L10n.TextContent("Speed Rejection", "Higher this value, more likely history will be rejected when current and reprojected history motion vector differ by a substantial amount. High values can decrease ghosting but will also reintroduce aliasing on the aforementioned cases.", null, null);
                public static readonly GUIContent TAAQualityLevel = L10n.TextContent("Quality Preset", "Low quality is fast, but can lead to more ghosting and blurrier output when moving, Medium quality has better ghosting handling and sharper results upon movement, High allows for velocity rejection policy, has better antialiasing and has mechanism to combat ringing for over sharpening the history.", null, null);
                public static readonly GUIContent TAASharpeningMode = L10n.TextContent("Sharpening Mode", "Low quality is fast, but is prone to artifact and sub-optimal results, PostSharpen is more expensive, but leads to higher quality sharpening. Finally CAS will also be of higher quality than Low Quality option, offering strong sharpening but limited control.", null, null);
                public static readonly GUIContent TAAAntiRinging = L10n.TextContent("Anti-ringing", "When enabled, ringing artifacts (dark or strangely saturated edges) caused by history sharpening will be improved. This comes at a potential loss of sharpness upon motion.", null, null);
                // Advanced TAA
                public static readonly GUIContent TAABaseBlendFactor = L10n.TextContent("Base Blend Factor", "Determines how much the history buffer is blended together with current frame result. Higher values means more history contribution, which leads to better anti aliasing, but also more prone to ghosting.", null, null);
                public static readonly GUIContent TAAJitterScale = L10n.TextContent("Jitter Scale", "Determines the scale to the jitter applied when TAA is enabled. Lowering this value will lead to less visible flickering and jittering, but also will produce more aliased images.", null, null);

                public static readonly GUIContent renderingPath = L10n.TextContent("Custom Frame Settings", "Define custom values for Frame Settings for this Camera to use.", null, null);
                public static readonly GUIContent fullScreenPassthrough = L10n.TextContent("Fullscreen Passthrough", "This will skip rendering settings to directly rendering in fullscreen(for instance: Useful for video)", null, null);
                public static readonly GUIContent exposureTarget = L10n.TextContent("Exposure Target", "The object used as a target for centering the Exposure's Procedural Mask metering mode when target object option is set (See Exposure Volume Component).", null, null);

                public static readonly GUIContent allowDynamicResolution = L10n.TextContent("HDRP Dynamic Resolution", "Whether to support HDRP dynamic resolution.", null, null);
                public const string taauInfoBox = "When TAA Upsample is enabled, TAA is run as antialiasing algorithm and uses High Quality as base, to select other anti-aliasing methods please change upscale filter for dynamic resolution.";

                public const string DLSSFeatureDetectedMsg = "Unity detected NVIDIA Deep Learning Super Sampling and will ignore the Fallback Anti Aliasing Method.";
                public const string DLSSFeatureNotDetectedMsg = "Unity cannot detect NVIDIA Deep Learning Super Sampling and will use the Fallback Anti Aliasing Method instead.";
                public const string DLSSNotEnabledInQualityAsset = "The quality asset in this project does not have NVIDIA Deep Learning Super Sampling (DLSS) enabled. DLSS will not be running on this camera.";
                public static readonly GUIContent DLSSAllow = L10n.TextContent("Allow DLSS", "Allows DLSS for this camera. For the effect to be enabled, it must be set in the quality asset of this project.", null, null);
                public static readonly GUIContent DLSSCustomQualitySettings = L10n.TextContent("Use DLSS Custom Quality", "", null, null);
                public static readonly GUIContent DLSSUseCustomAttributes = L10n.TextContent("Use DLSS Custom Attributes", "", null, null);
                public static readonly GUIContent fsrOverrideSharpness = L10n.TextContent("Override FSR Sharpness", "Overrides the FSR sharpness value for this camera.", null, null);
                public static readonly GUIContent FSR2Allow = L10n.TextContent("Allow FSR2", "Allows FSR2 for this camera. For the effect to be enabled, it must be set in the quality asset of this project.", null, null);
                public static readonly GUIContent FSR2CustomQualitySettings = L10n.TextContent("Use FSR2 Custom Quality", "", null, null);
                public static readonly GUIContent FSR2UseCustomAttributes = L10n.TextContent("Use FSR2 Custom Attributes", "", null, null);
                public static GUIContent overrideSettingText { get; } = L10n.TextContent("", "If enabled, this camera setting will be used instead of the one specified in the quality asset of this project.", null, null);

                public const string FSR2FeatureDetectedMsg = "Unity detected AMD Fidelity FX 2 Super Resolution and will ignore the Fallback Anti Aliasing Method.";
                public const string FSR2FeatureNotDetectedMsg = "Unity cannot detect AMD Fidelity FX 2 Super Resolution and will use the Fallback Anti Aliasing Method instead.";
                public const string FSR2NotEnabledInQualityAsset = "The quality asset in this project does not have Fidelity FX 2 Super Resolution (FSR2) enabled. FSR2 will not be running on this camera.";

            }
        }
    }
}
