using UnityEditor.Rendering;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{

    partial class InfluenceVolumeUI
    {
        // We need to provide gamma values to the Gizmos and Handle because they are translated back to linear
        // during the drawing call.
        internal static readonly Color k_GizmoThemeColorBase = new Color(230 / 255f, 229 / 255f, 148 / 255f, 0.7f).gamma;
        static readonly Color k_GizmoThemeColorInfluence = new Color(83 / 255f, 255 / 255f, 95 / 255f, 0.7f).gamma;
        static readonly Color k_GizmoThemeColorInfluenceNormal = new Color(128 / 255f, 128 / 255f, 255 / 255f, 0.7f).gamma;
        internal static readonly Color[] k_HandlesColor = new Color[]
        {
            Color.red,
            Color.green,
            Color.blue,
            new Color(.5f, 0f, 0f, 1f),
            new Color(0f, .5f, 0f, 1f),
            new Color(0f, 0f, .5f, 1f)
        };

        static readonly GUIContent shapeContent = EditorGUIUtility.TrTextContent("Shape", "Specifies the shape of the Influence Volume.");
        static readonly GUIContent boxSizeContent = EditorGUIUtility.TrTextContent("Box Size", "Sets the size of the Box Influence Volume on a per axis basis. The Transform Scale does not affect these dimensions.");
        static readonly GUIContent offsetContent = EditorGUIUtility.TrTextContent("Offset", "Sets the coordinates for the Influence Volumes's center relative to the Transform Position.");
        static readonly GUIContent blendDistanceContent = EditorGUIUtility.TrTextContent("Blend Distance", "Sets the boundaries inside the Influence Volume within which the Reflection Probe blends with other Reflection Probes. Only available for deferred Reflection Probes.");
        static readonly GUIContent blendNormalDistanceContent = EditorGUIUtility.TrTextContent("Blend Normal Distance", "Area around the probe where the normals influence the probe. Only available for deferred Reflection Probes.");
        static readonly GUIContent faceFadeContent = EditorGUIUtility.TrTextContent("Face Fade", "Fade faces of the cubemap.");

        static readonly GUIContent radiusContent = EditorGUIUtility.TrTextContent("Radius", "Sets the radius of the Sphere Influence Volume. The Transform Scale does not affect this value.");

        static readonly GUIContent normalModeContent = EditorGUIUtility.TrTextContent("Normal", "Normal parameters mode (Box Shape only).");
        static readonly GUIContent advancedModeContent = EditorGUIUtility.TrTextContent("Advanced", "Advanced parameters mode (Box Shape only).");
    }
}
