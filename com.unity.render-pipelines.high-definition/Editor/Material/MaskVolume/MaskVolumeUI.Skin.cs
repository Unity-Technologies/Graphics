using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    static partial class MaskVolumeUI
    {
        internal static class Styles
        {
            internal const string k_FeatureEnableInfo = "Mask Volumes feature is disabled in the Material section of the HD Render Pipeline Asset.\nIt must also be enabled in Frame Settings.";
            internal const string k_PaintHeader = "Paint";
            internal const string k_VolumeHeader = "Volume";
            internal const string k_MasksHeader = "Mask";
            internal const string k_CreateAssetText = "Create Asset";
            internal const string k_ResampleAssetText = "Resample Asset";

            internal static readonly GUIContent[] s_PaintToolbarContents =
            {
                EditorGUIUtility.IconContent("Grid.PaintTool", "|Paint the volume. (SHIFT+3)")
            };

            internal static readonly GUIContent[] s_VolumeToolbarContents =
            {
                EditorGUIUtility.IconContent("EditCollider", "|Modify the base shape. (SHIFT+1)"),
                EditorGUIUtility.IconContent("PreMatCube", "|Modify the influence volume. (SHIFT+2)"),
            };

            internal static readonly GUIContent s_Size = new GUIContent("Size", "Modify the size of this Mask Volume. This is independent of the Transform's Scale.");
            internal static readonly GUIContent s_DebugColorLabel = new GUIContent("Debug Color", "This color is used to visualize per-pixel mask volume weights in the render pipeline debugger.");
            internal static readonly GUIContent s_DrawGizmosLabel = new GUIContent("Draw Gizmos", "Enable or disable drawing gizmos.");
            internal static readonly GUIContent s_BlendLabel = new GUIContent("Blend Distance", "Interior distance from the Size where the contribution fades in completely.");
            internal static readonly GUIContent s_NormalModeContent = new GUIContent("Normal", "Exposes standard parameters.");
            internal static readonly GUIContent s_AdvancedModeContent = new GUIContent("Advanced", "Exposes advanced parameters.");

            internal static readonly GUIContent s_DistanceFadeStartLabel = new GUIContent("Distance Fade Start");
            internal static readonly GUIContent s_DistanceFadeEndLabel   = new GUIContent("Distance Fade End");
            internal static readonly GUIContent s_MaskSpacingModeLabel = new GUIContent("Mask Spacing Mode");
            internal static readonly GUIContent s_ResolutionXLabel = new GUIContent("Resolution X", "Modify the resolution (number of masks in X)");
            internal static readonly GUIContent s_ResolutionYLabel = new GUIContent("Resolution Y", "Modify the resolution (number of masks in Y)");
            internal static readonly GUIContent s_ResolutionZLabel = new GUIContent("Resolution Z", "Modify the resolution (number of masks in Z)");

            internal static readonly GUIContent s_DensityXLabel = new GUIContent("Density X", "Modify the density (number of masks per unit in X). Resolution will be automatically computed based on density.");
            internal static readonly GUIContent s_DensityYLabel = new GUIContent("Density Y", "Modify the density (number of masks per unit in Y). Resolution will be automatically computed based on density.");
            internal static readonly GUIContent s_DensityZLabel = new GUIContent("Density Z", "Modify the density (number of masks per unit in Z). Resolution will be automatically computed based on density.");

            internal static readonly GUIContent s_VolumeBlendModeLabel = new GUIContent("Volume Blend Mode", "A blending mode for the entire volume when overlapping others.");
            internal static readonly GUIContent s_WeightLabel = new GUIContent("Weight", "Weigh the mask contribution for the entire volume.");
            internal static readonly GUIContent s_NormalBiasWSLabel = new GUIContent("Normal Bias", "Controls the distance in world space units to bias along the surface normal to mitigate light leaking and self-shadowing artifacts.\nA value of 0.0 is physically accurate, but can result in self shadowing artifacts on surfaces that contribute to GI.\nIncrease value to mitigate self shadowing artifacts.\nSignificantly large values can have performance implications, as normal bias will dilate a mask volumes bounding box, causing it to be sampled in additional neighboring tiles / clusters.");

            internal static readonly GUIContent s_BackfaceToleranceLabel = new GUIContent("Backface Tolerance", "The percentage of backfaces sampled per mask is acceptable before mask will receive dilated data.");
            internal static readonly GUIContent s_DilationIterationLabel = new GUIContent("Dilation Iterations", "The number of iterations Dilation copies over data from each mask to its neighbors.");

            internal static readonly GUIContent s_DataAssetLabel = new GUIContent("Data asset", "The asset which serializes all mask related data in this volume.");

            internal static readonly Color k_GizmoColorBase = new Color(180 / 255f, 180 / 255f, 180 / 255f, 8 / 255f).gamma;

            internal static readonly Color[] k_BaseHandlesColor = new Color[]
            {
                new Color(180 / 255f, 180 / 255f, 180 / 255f, 255 / 255f).gamma,
                new Color(180 / 255f, 180 / 255f, 180 / 255f, 255 / 255f).gamma,
                new Color(180 / 255f, 180 / 255f, 180 / 255f, 255 / 255f).gamma,
                new Color(180 / 255f, 180 / 255f, 180 / 255f, 255 / 255f).gamma,
                new Color(180 / 255f, 180 / 255f, 180 / 255f, 255 / 255f).gamma,
                new Color(180 / 255f, 180 / 255f, 180 / 255f, 255 / 255f).gamma
            };
        }
    }
}
