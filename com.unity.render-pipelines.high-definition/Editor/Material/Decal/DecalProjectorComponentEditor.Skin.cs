using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public partial class DecalProjectorComponentEditor
    {
        const string k_EditShapePreservingUVTooltip = "Modify Decal size preserving UV positions. They will be stretched.\nIn addition to customizable shortcut, you can press shift to quickly swap between preserving and not preserving UV.";
        const string k_EditShapeWithoutPreservingUVTooltip = "Modify Decal volume without preserving UV positions. They will be cropped.\nIn addition to customizable shortcut, you can press shift to quickly swap between preserving and not preserving UV.";
        const string k_EditUVTooltip = "Modify the UV positions only";

        static readonly GUIContent k_SizeContent = EditorGUIUtility.TrTextContent("Size", "Sets the size of the projector.");
        static readonly GUIContent k_MaterialContent = EditorGUIUtility.TrTextContent("Material", "Specifies the Material this component projects as a decal.");
        static readonly GUIContent k_DistanceContent = EditorGUIUtility.TrTextContent("Draw Distance", "Sets the distance from the Camera at which HDRP stop rendering the decal.");
        static readonly GUIContent k_FadeScaleContent = EditorGUIUtility.TrTextContent("Start Fade", "Controls the distance from the Camera at which this component begins to fade the decal out.");
        static readonly GUIContent k_UVScaleContent = EditorGUIUtility.TrTextContent("Tilling", "Sets the scale for the decal Material. Scales the decal along its UV axes.");
        static readonly GUIContent k_UVBiasContent = EditorGUIUtility.TrTextContent("Offset", "Sets the offset for the decal Material. Moves the decal along its UV axes.");
        static readonly GUIContent k_FadeFactorContent = EditorGUIUtility.TrTextContent("Fade Factor", "Controls the transparency of the decal.");
        static readonly GUIContent k_AffectTransparentContent = EditorGUIUtility.TrTextContent("Affects Transparent", "When enabled, HDRP draws this projector's decal on top of transparent surfaces.");

        public static readonly Color k_GizmoColorBase = Color.white;
        public static readonly Color[] k_BaseHandlesColor = new Color[]
        {
            Color.white,
            Color.white,
            Color.white,
            Color.white,
            Color.white,
            Color.white
        };
    }
}
