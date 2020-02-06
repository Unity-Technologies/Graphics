using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    static partial class ProbeVolumeUI
    {
        internal static class Styles
        {
            public const string k_VolumeHeader = "Volume";
            public const string k_ProbesHeader = "Probes";

            public static readonly GUIContent[] s_Toolbar_Contents = new GUIContent[]
            {
                EditorGUIUtility.IconContent("EditCollider", "|Modify the base shape. (SHIFT+1)"),
                EditorGUIUtility.IconContent("PreMatCube", "|Modify the influence volume. (SHIFT+2)")
            };

            public static readonly GUIContent s_Size = new GUIContent("Size", "Modify the size of this Probe Volume. This is independent of the Transform's Scale.");
            public static readonly GUIContent s_DebugColorLabel = new GUIContent("Debug Color", "This color is used to visualize per-pixel probe volume weights in the render pipeline debugger.");
            public static readonly GUIContent s_DrawProbesLabel = new GUIContent("Draw Probes", "Enable or disable drawing probes.");
            public static readonly GUIContent s_BlendLabel = new GUIContent("Blend Distance", "Interior distance from the Size where the contribution fades in completely.");
            public static readonly GUIContent s_NormalModeContent = new GUIContent("Normal", "Exposes standard parameters.");
            public static readonly GUIContent s_AdvancedModeContent = new GUIContent("Advanced", "Exposes advanced parameters.");

            public static readonly GUIContent s_DistanceFadeStartLabel = new GUIContent("Distance Fade Start");
            public static readonly GUIContent s_DistanceFadeEndLabel   = new GUIContent("Distance Fade End");
            public static readonly GUIContent s_ProbeSpacingModeLabel = new GUIContent("Probe Spacing Mode");
            public static readonly GUIContent s_ResolutionXLabel = new GUIContent("Resolution X", "Modify the resolution (number of probes in X)");
            public static readonly GUIContent s_ResolutionYLabel = new GUIContent("Resolution Y", "Modify the resolution (number of probes in Y)");
            public static readonly GUIContent s_ResolutionZLabel = new GUIContent("Resolution Z", "Modify the resolution (number of probes in Z)");

            public static readonly GUIContent s_DensityXLabel = new GUIContent("Density X", "Modify the density (number of probes per unit in X). Resolution will be automatically computed based on density.");
            public static readonly GUIContent s_DensityYLabel = new GUIContent("Density Y", "Modify the density (number of probes per unit in Y). Resolution will be automatically computed based on density.");
            public static readonly GUIContent s_DensityZLabel = new GUIContent("Density Z", "Modify the density (number of probes per unit in Z). Resolution will be automatically computed based on density.");

            public static readonly GUIContent s_VolumeBlendModeLabel = new GUIContent("Volume Blend Mode", "A blending mode for the entire volume when overlapping others.");
            public static readonly GUIContent s_WeightLabel = new GUIContent("Weight", "Weigh the probe contribution for the entire volume.");

            public static readonly GUIContent s_DataAssetLabel = new GUIContent("Data asset", "The asset which serializes all probe related data in this volume.");

            public static readonly Color k_GizmoColorBase = new Color(180 / 255f, 180 / 255f, 180 / 255f, 8 / 255f).gamma;

            public static readonly Color[] k_BaseHandlesColor = new Color[]
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
