using UnityEngine;

namespace UnityEditor.Experimental.Rendering
{
    static partial class ProbeVolumeUI
    {
        internal static class Styles
        {
            internal static readonly GUIContent s_Size = new GUIContent("Size", "Modify the size of this Probe Volume. This is independent of the Transform's Scale.");
            internal static readonly GUIContent s_GlobalVolume = new GUIContent("Global", "If the volume is marked as global, it will be fit to the scene content every time the scene is saved or the baking starts.");
            internal static readonly GUIContent s_HighestSubdivLevel = new GUIContent("Highest Subdivision Level", "Overrides the highest subdivision level used by the system. This determines how finely a probe volume is subdivided, lower values means larger minimum distance between probes.");
            internal static readonly GUIContent s_LowestSubdivLevel = new GUIContent("Lowest Subdivision Level", "Overrides the lowest subdivision level used by the system. This determines how coarsely a probe volume is allowed to be subdivided, higher values means smaller maximum distance between probes.");
            internal static readonly GUIContent s_ObjectLayerMask = new GUIContent("Object Layer Mask", "Control which layers will be used to select the meshes for the probe placement algorithm.");
            internal static readonly GUIContent s_GeometryDistanceOffset = new GUIContent("Geometry Distance Offset", "Affects the minimum distance at which the subdivision system will place probes near the geometry.");
            internal static readonly string s_ProbeVolumeChangedMessage = "The probe volume has changed since last baking or the data was never baked.\nPlease bake lighting in the lighting panel to update the lighting data.";

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
