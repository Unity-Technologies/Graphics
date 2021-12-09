using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    static partial class ProbeVolumeUI
    {
        internal static class Styles
        {
            internal const string k_FeatureWarning = "Warning: Probe Volumes is a highly experimental feature.\nIt is disabled by default for this reason.\nIt's functionality is subject to breaking changes and whole sale removal.\nIt is not recommended for use outside of for providing feedback.\nIt should not be used in production.";
            internal const string k_FeatureEnableInfo = "\nProbe Volumes feature is disabled. To enable, set:\nProbeVolumesEvaluationMode = ProbeVolumesEvaluationModes.MaterialPass\ninside of ShaderConfig.cs. Then inside of the editor run:\nEdit->Render Pipeline->Generate Shader Includes\nProbe Volumes feature must also be enabled inside of your HDRenderPipelineAsset.";
            internal const string k_FeatureAdditiveBlendingDisabledError = "Error: ProbeVolumesAdditiveBlending feature is disabled inside of ShaderConfig.cs.\nThis probe volume will not be rendered.\nTo enable, set:\nProbeVolumesAdditiveBlending = 1\ninside of ShaderConfig.cs. Then inside of the editor run:\nEdit->Render Pipeline->Generate Shader Includes.";
            internal const string k_FeatureOctahedralDepthEnabledNoData = "Error: ProbeVolumesBilateralFilteringMode inside of ShaderConfig.cs is set to OctahedralDepth, but asset was baked with OctahedralDepth disabled.\nPlease rebake if you would like this probe volume to use octahedral depth filtering.";
            internal const string k_FeatureOctahedralDepthDisableYesData = "Error: ProbeVolumesBilateralFilteringMode inside of ShaderConfig.cs is not set to OctahedralDepth, but was baked with OctahedralDepth enabled.\nPlease rebake to discard octahedral depth data from asset.";
            internal const string k_VolumeHeader = "Volume";
            internal const string k_ProbesHeader = "Probes";
            internal const string k_BakingHeader = "Baking";
            internal const string k_BakeSelectedText = "Bake Selected";

            internal static readonly GUIContent[] s_Toolbar_Contents = new GUIContent[]
            {
                EditorGUIUtility.IconContent("EditCollider", "|Modify the base shape. (SHIFT+1)"),
                EditorGUIUtility.IconContent("PreMatCube", "|Modify the influence volume. (SHIFT+2)")
            };

            internal static readonly GUIContent s_Size = new GUIContent("Size", "Modify the size of this Probe Volume. This is independent of the Transform's Scale.");
            internal static readonly GUIContent s_DebugColorLabel = new GUIContent("Debug Color", "This color is used to visualize per-pixel probe volume weights in the render pipeline debugger.");
            internal static readonly GUIContent s_DrawProbesLabel = new GUIContent("Draw Probes", "Enable or disable drawing probes.");
            internal static readonly GUIContent s_DrawValidityLabel = new GUIContent("Draw Validity", "Visualize per-probe validity. Green is full validity, no backfaces visible. Red is no validity, max backfaces visible.");
            internal static readonly GUIContent s_HighlightRingingLabel = new GUIContent("Highlight Ringing", "Negative values on probes due to ringing will be highlighted and blinked in red.");
            internal static readonly GUIContent s_DrawNeighborsLabel = new GUIContent("Draw Neighbors", "Enable or disable drawing probe neighborhood to debug dynamic gi.");
            internal static readonly GUIContent s_DrawEmissionLabel = new GUIContent("Draw Emission", "Show neighborhood emission instead of albedo.");
            internal static readonly GUIContent s_NeighborsQuadScaleLabel = new GUIContent("Draw Neighbors Scale", "The size of the debug quads when viewing draw neighbors mode");
            internal static readonly GUIContent s_DrawOctahedralDepthRays = new GUIContent("Draw Octahedral Depth Rays", "Enable or disable drawing rays to visualize to the octahedral depth data.");
            internal static readonly GUIContent s_DrawOctahedralDepthRayIndexX = new GUIContent("Octahedral Depth Rays Probe X", "Specifies the x index of the probe to visualize octahedral depth rays for.");
            internal static readonly GUIContent s_DrawOctahedralDepthRayIndexY = new GUIContent("Octahedral Depth Rays Probe Y", "Specifies the y index of the probe to visualize octahedral depth rays for.");
            internal static readonly GUIContent s_DrawOctahedralDepthRayIndexZ = new GUIContent("Octahedral Depth Rays Probe Z", "Specifies the z index of the probe to visualize octahedral depth rays for.");

            internal static readonly GUIContent s_BlendLabel = new GUIContent("Blend Distance", "Interior distance from the Size where the contribution fades in completely.");
            internal static readonly GUIContent s_NormalModeContent = new GUIContent("Normal", "Exposes standard parameters.");
            internal static readonly GUIContent s_AdvancedModeContent = new GUIContent("Advanced", "Exposes advanced parameters.");

            internal static readonly GUIContent s_DistanceFadeStartLabel = new GUIContent("Distance Fade Start");
            internal static readonly GUIContent s_DistanceFadeEndLabel   = new GUIContent("Distance Fade End");
            internal static readonly GUIContent s_ProbeSpacingModeLabel = new GUIContent("Probe Spacing Mode");
            internal static readonly GUIContent s_ResolutionXLabel = new GUIContent("Resolution X", "Modify the resolution (number of probes in X)");
            internal static readonly GUIContent s_ResolutionYLabel = new GUIContent("Resolution Y", "Modify the resolution (number of probes in Y)");
            internal static readonly GUIContent s_ResolutionZLabel = new GUIContent("Resolution Z", "Modify the resolution (number of probes in Z)");

            internal static readonly GUIContent s_DensityXLabel = new GUIContent("Density X", "Modify the density (number of probes per unit in X). Resolution will be automatically computed based on density.");
            internal static readonly GUIContent s_DensityYLabel = new GUIContent("Density Y", "Modify the density (number of probes per unit in Y). Resolution will be automatically computed based on density.");
            internal static readonly GUIContent s_DensityZLabel = new GUIContent("Density Z", "Modify the density (number of probes per unit in Z). Resolution will be automatically computed based on density.");

            internal static readonly GUIContent s_VolumeBlendModeLabel = new GUIContent("Volume Blend Mode", "A blending mode for the entire volume when overlapping others.");
            internal static readonly GUIContent s_WeightLabel = new GUIContent("Weight", "Weigh the probe contribution for the entire volume.");
            internal static readonly GUIContent s_NormalBiasWSLabel = new GUIContent("Normal Bias", "Controls the distance in world space units to bias along the surface normal to mitigate light leaking and self-shadowing artifacts.\nA value of 0.0 is physically accurate, but can result in self shadowing artifacts on surfaces that contribute to GI.\nIncrease value to mitigate self shadowing and light leak artifacts.\nSignificantly large values can have performance implications, as normal bias will dilate a probe volumes bounding box, causing it to be sampled in additional neighboring tiles / clusters.");
            internal static readonly GUIContent s_ViewBiasWSLabel = new GUIContent("View Bias", "Controls the distance in world space units to bias toward the camera position to mitigate light leaking and self-shadowing artifacts.\nA value of 0.0 is physically accurate, but can result in self shadowing artifacts on surfaces that contribute to GI.\nIncrease value to mitigate self shadowing and light leak artifacts.\nSignificantly large values can have performance implications, as view bias will dilate a probe volumes bounding box, causing it to be sampled in additional neighboring tiles / clusters.");


            internal static readonly GUIContent s_BackfaceToleranceLabel = new GUIContent("Backface Tolerance", "The percentage of backfaces sampled per probe is acceptable before probe will receive dilated data.");
            internal static readonly GUIContent s_DilationIterationLabel = new GUIContent("Dilation Iterations", "The number of iterations Dilation copies over data from each probe to its neighbors.");

            internal static readonly GUIContent s_DataAssetLabel = new GUIContent("Data asset", "The asset which serializes all probe related data in this volume.");

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
