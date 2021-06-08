using UnityEngine;

namespace UnityEditor.Experimental.Rendering
{
    static partial class ProbeVolumeUI
    {
        internal static class Styles
        {
            internal static readonly GUIContent s_Size = new GUIContent("Size", "Modify the size of this Probe Volume. This is independent of the Transform's Scale.");
            internal static readonly GUIContent s_GlobalVolume = new GUIContent("Global", "If the volume is marked as global, it will be fit to the scene content every time the scene is saved or the baking starts.");
            internal static readonly GUIContent s_MinMaxSubdivSlider = new GUIContent("Subdivision Controller", "Control how much the probe baking system will subdivide in this volume.\nBoth min and max values are used to compute the allowed subdivision levels inside this volume. e.g. a Min subdivision of 2 will ensure that there is at least 2 levels of subdivision everywhere in the volume.");
            internal static readonly GUIContent s_ObjectLayerMask = new GUIContent("Object Layer Mask", "Control which layers will be used to select the meshes for the probe placement algorithm.");
            internal static readonly GUIContent s_GeometryDistanceOffset = new GUIContent("Geometry Distance Offset", "Affects the minimum distance at which the subdivision system will place probes near the geometry.");

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
