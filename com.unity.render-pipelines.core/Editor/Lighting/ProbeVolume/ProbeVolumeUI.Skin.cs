using UnityEngine;

namespace UnityEditor.Rendering
{
    static partial class ProbeVolumeUI
    {
        internal static class Styles
        {
            internal const string k_featureWarning = "Warning: Probe Volumes is a highly experimental feature.\nIt is disabled by default for this reason.\nIt's functionality is subject to breaking changes and whole sale removal.\nIt is not recommended for use outside of for providing feedback.\nIt should not be used in production.";
            internal const string k_featureEnableInfo = "\nProbe Volumes feature is disabled. To enable, set:\nProbeVolumesEvaluationMode = ProbeVolumesEvaluationModes.MaterialPass\ninside of ShaderConfig.cs and inside of the editor run:\nEdit->Render Pipeline->Generate Shader Includes\nProbe Volumes feature must also be enabled inside of your HDRenderPipelineAsset.";


            internal static readonly GUIContent s_Size = new GUIContent("Size", "Modify the size of this Probe Volume. This is independent of the Transform's Scale.");
            internal static readonly GUIContent s_DebugColorLabel = new GUIContent("Debug Color", "This color is used to visualize per-pixel probe volume weights in the render pipeline debugger.");

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
