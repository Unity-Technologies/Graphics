using UnityEngine;

namespace UnityEditor.Rendering
{
    partial class DecalEditorBase
    {
        const string k_EditShapePreservingUVTooltip = "Modifies the projector boundaries and crops/tiles the decal to fill them.";
        const string k_EditShapeWithoutPreservingUVTooltip = "Modifies the projector boundaries and stretches the decal to fill them.";
        const string k_EditUVTooltip = "Modify the UV and the pivot position without moving the projection box. It can alter Transform.";

        protected static readonly GUIContent k_SizeContent = EditorGUIUtility.TrTextContent("Size", "Sets the size of the projector.");
        protected static readonly GUIContent[] k_SizeSubContent = new[]
        {
            EditorGUIUtility.TrTextContent("Width", "Sets the width of the projection plan."),
            EditorGUIUtility.TrTextContent("Height", "Sets the height of the projection plan.")
        };


        static readonly GUIContent k_ProjectionDepthContent = EditorGUIUtility.TrTextContent("Projection Depth", "Sets the projection depth of the projector.");
        static readonly GUIContent k_MaterialContent = EditorGUIUtility.TrTextContent("Material", "Specifies the Material this component projects as a decal.");
        static readonly GUIContent k_Offset = EditorGUIUtility.TrTextContent("Pivot", "Controls the position of the pivot point of the decal.");
        static readonly GUIContent k_UVScaleContent = EditorGUIUtility.TrTextContent("Tilling", "Sets the scale for the decal Material. Scales the decal along its UV axes.");
        static readonly GUIContent k_UVBiasContent = EditorGUIUtility.TrTextContent("Offset", "Sets the offset for the decal Material. Moves the decal along its UV axes.");
    }
}
