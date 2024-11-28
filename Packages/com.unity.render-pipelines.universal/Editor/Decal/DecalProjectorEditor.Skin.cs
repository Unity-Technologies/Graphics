using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    partial class DecalProjectorEditor
    {
        const string k_EditShapePreservingUVTooltip = "Modifies the projector boundaries and crops/tiles the decal to fill them.";
        const string k_EditShapeWithoutPreservingUVTooltip = "Modifies the projector boundaries and stretches the decal to fill them.";
        const string k_EditUVTooltip = "Modify the UV and the pivot position without moving the projection box. It can alter Transform.";

        static readonly GUIContent k_ScaleMode = EditorGUIUtility.TrTextContent("Scale Mode", "Specifies the scaling mode to apply to decals that use this Decal Projector.");
        static readonly GUIContent k_WidthContent = EditorGUIUtility.TrTextContent("Width", "Sets the width of the projection plan.");
        static readonly GUIContent k_HeightContent = EditorGUIUtility.TrTextContent("Height", "Sets the height of the projection plan.");
        static readonly GUIContent k_ProjectionDepthContent = EditorGUIUtility.TrTextContent("Projection Depth", "Sets the projection depth of the projector.");
        static readonly GUIContent k_MaterialContent = EditorGUIUtility.TrTextContent("Material", "Specifies the Material this component projects as a decal.");
        static readonly GUIContent k_RenderingLayerMaskContent = EditorGUIUtility.TrTextContent("Rendering Layers", "Specify the rendering layer mask for this projector. Unity renders decals on all meshes where at least one Rendering Layer value matches.");
        static readonly GUIContent k_DistanceContent = EditorGUIUtility.TrTextContent("Draw Distance", "Sets the distance from the Camera at which URP stop rendering the decal.");
        static readonly GUIContent k_FadeScaleContent = EditorGUIUtility.TrTextContent("Start Fade", "Controls the distance from the Camera at which this component begins to fade the decal out.");
        static readonly GUIContent k_AngleFadeContent = EditorGUIUtility.TrTextContent("Angle Fade", "Controls the fade out range of the decal based on the angle between the Decal backward direction and the vertex normal of the receiving surface. Requires 'Decal Layers' to be enabled in the URP Asset and Frame Settings.");
        static readonly GUIContent k_UVScaleContent = EditorGUIUtility.TrTextContent("Tilling", "Sets the scale for the decal Material. Scales the decal along its UV axes.");
        static readonly GUIContent k_UVBiasContent = EditorGUIUtility.TrTextContent("Offset", "Sets the offset for the decal Material. Moves the decal along its UV axes.");
        static readonly GUIContent k_OpacityContent = EditorGUIUtility.TrTextContent("Opacity", "Controls the transparency of the decal.");
        static readonly GUIContent k_Offset = EditorGUIUtility.TrTextContent("Pivot", "Controls the position of the pivot point of the decal.");
        static readonly GUIContent k_NewMaterialButtonText = EditorGUIUtility.TrTextContent("New", "Creates a new decal Material asset template.");

        static readonly string k_NewDecalMaterialText = "New Decal";
        static readonly string k_BaseSceneEditingToolText = "<color=grey>Decal Scene Editing Mode:</color> ";
        static readonly string k_EditShapeWithoutPreservingUVName = k_BaseSceneEditingToolText + "Scale";
        static readonly string k_EditShapePreservingUVName = k_BaseSceneEditingToolText + "Crop";
        static readonly string k_EditUVAndPivotName = k_BaseSceneEditingToolText + "Pivot / UV";
    }
}
