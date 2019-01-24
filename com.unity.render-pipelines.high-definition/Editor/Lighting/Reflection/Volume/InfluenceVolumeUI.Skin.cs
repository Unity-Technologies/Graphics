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

        static readonly GUIContent shapeContent = EditorGUIUtility.TrTextContent("Shape");
        static readonly GUIContent boxSizeContent = EditorGUIUtility.TrTextContent("Box Size", "The size of the box in which the reflections will be applied to objects. The value is not affected by the Transform of the Game Object.");
        static readonly GUIContent offsetContent = EditorGUIUtility.TrTextContent("Offset", "The center of the InfluenceVolume in which the reflections will be applied to objects. The value is relative to the position of the Game Object.");
        static readonly GUIContent blendDistanceContent = EditorGUIUtility.TrTextContent("Blend Distance", "Area around the probe where it is blended with other probes. Only used in deferred probes.");
        static readonly GUIContent blendNormalDistanceContent = EditorGUIUtility.TrTextContent("Blend Normal Distance", "Area around the probe where the normals influence the probe. Only used in deferred probes.");
        static readonly GUIContent faceFadeContent = EditorGUIUtility.TrTextContent("Face fade", "Fade faces of the cubemap.");

        static readonly GUIContent radiusContent = EditorGUIUtility.TrTextContent("Radius");

        static readonly GUIContent normalModeContent = EditorGUIUtility.TrTextContent("Normal", "Normal parameters mode (only change for box shape).");
        static readonly GUIContent advancedModeContent = EditorGUIUtility.TrTextContent("Advanced", "Advanced parameters mode (only change for box shape).");
    }
}
