using UnityEngine;

namespace UnityEditor.Rendering
{
    static partial class ProbeVolumeUI
    {
        internal static class Styles
        {
            internal static readonly GUIContent s_Size = new GUIContent("Size", "Modify the size of this Probe Volume. This is unaffected by the GameObject's Transform's Scale property.");
            internal static readonly GUIContent s_Mode = new GUIContent("Mode", "When set to Global this Probe Volume considers all renderers with Contribute Global Illumination enabled. This list updates every time the Scene is saved or the lighting is baked.");
            internal static readonly GUIContent s_OverridesSubdivision = new GUIContent("Override Subdivision Levels", "Whether to override or not the subdivision levels.");
            internal static readonly GUIContent s_HighestSubdivLevel = new GUIContent("Highest Subdivision Level", "Overrides the highest subdivision level used by the system. This determines how finely a probe volume is subdivided, lower values means larger minimum distance between probes.");
            internal static readonly GUIContent s_LowestSubdivLevel = new GUIContent("Lowest Subdivision Level", "Overrides the lowest subdivision level used by the system. This determines how coarsely a probe volume is allowed to be subdivided, higher values means smaller maximum distance between probes.");
            internal static readonly GUIContent s_ObjectLayerMask = new GUIContent("Layer Mask", "Specify Layers to consider during probe placement.");
            internal static readonly GUIContent s_MinRendererVolumeSize = new GUIContent("Override Min Renderer Size", "Specify the smallest renderer size to consider during probe placement, in meters.");
            internal static readonly GUIContent s_OverrideRendererFilters = new GUIContent("Override Renderer Filters", "Overrides the Renderer Filters that determine which renderers influence probe placement.");
            internal static readonly GUIContent s_DistanceBetweenProbes = new GUIContent("Override Probe Spacing", "Overrides the global Probe Spacing specified in the Probe Volume Settings window. Can not exceed the maximum set there.");
            internal static readonly string s_ProbeVolumeChangedMessage = "This Probe Volume has never been baked, or has changed since the last bake. Generate Lighting from the Lighting Window.";

            internal static readonly string[] k_ModeOptions = new string[] { "Global", "Local" };

            internal static readonly Color k_GizmoColorBase = new Color32(137, 222, 144, 255);

            internal static readonly Color[] k_BaseHandlesColor = new Color[]
            {
                k_GizmoColorBase,
                k_GizmoColorBase,
                k_GizmoColorBase,
                k_GizmoColorBase,
                k_GizmoColorBase,
                k_GizmoColorBase
            };
        }
    }
}
