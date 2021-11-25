using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    partial class DecalProjectorEditor
    {
        const string k_EditShapePreservingUVTooltip = "Modifies the projector boundaries and crops/tiles the decal to fill them.";
        const string k_EditShapeWithoutPreservingUVTooltip = "Modifies the projector boundaries and stretches the decal to fill them.";
        const string k_EditUVTooltip = "Modify the UV and the pivot position without moving the projection box. It can alter Transform.";

        static readonly GUIContent k_ScaleMode = EditorGUIUtility.TrTextContent("Scale Mode", "Specifies the scaling mode to apply to decals that use this Decal Projector.");
        static readonly GUIContent k_SizeContent = EditorGUIUtility.TrTextContent("Size", "Sets the size of the projector.");
        static readonly GUIContent[] k_SizeSubContent = new[]
        {
            EditorGUIUtility.TrTextContent("Width", "Sets the width of the projection plan."),
            EditorGUIUtility.TrTextContent("Height", "Sets the height of the projection plan.")
        };
        static readonly GUIContent k_ProjectionDepthContent = EditorGUIUtility.TrTextContent("Projection Depth", "Sets the projection depth of the projector.");
        static readonly GUIContent k_MaterialContent = EditorGUIUtility.TrTextContent("Material", "Specifies the Material this component projects as a decal.");
        static readonly GUIContent k_DecalLayerMaskContent = EditorGUIUtility.TrTextContent("Decal Layer", "Specify the decal layer mask to use for this projector. RenderingLayerMask of Mesh matching this value will receive the decal. Enable Layers in Decal section of HDRP settings to access it.");
        static readonly GUIContent k_DistanceContent = EditorGUIUtility.TrTextContent("Draw Distance", "Sets the distance from the Camera at which HDRP stop rendering the decal.");
        static readonly GUIContent k_FadeScaleContent = EditorGUIUtility.TrTextContent("Start Fade", "Controls the distance from the Camera at which this component begins to fade the decal out.");
        static readonly GUIContent k_AngleFadeContent = EditorGUIUtility.TrTextContent("Angle Fade", "Controls the fade out range of the decal based on the angle between the Decal backward direction and the vertex normal of the receiving surface. Requires 'Decal Layers' to be enabled in the HDRP Asset and Frame Settings.");
        static readonly GUIContent k_UVScaleContent = EditorGUIUtility.TrTextContent("Tilling", "Sets the scale for the decal Material. Scales the decal along its UV axes.");
        static readonly GUIContent k_UVBiasContent = EditorGUIUtility.TrTextContent("Offset", "Sets the offset for the decal Material. Moves the decal along its UV axes.");
        static readonly GUIContent k_FadeFactorContent = EditorGUIUtility.TrTextContent("Fade Factor", "Controls the transparency of the decal.");
        static readonly GUIContent k_AffectTransparentContent = EditorGUIUtility.TrTextContent("Affects Transparent", "When enabled, HDRP draws this projector's decal on top of transparent surfaces.");
        static readonly GUIContent k_Offset = EditorGUIUtility.TrTextContent("Pivot", "Controls the position of the pivot point of the decal.");
    }
}
