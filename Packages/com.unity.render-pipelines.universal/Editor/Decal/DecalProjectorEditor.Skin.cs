using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    partial class DecalProjectorEditor
    {
        const string k_EditShapePreservingUVTooltip = "Modifies the projector boundaries and crops/tiles the decal to fill them.";
        const string k_EditShapeWithoutPreservingUVTooltip = "Modifies the projector boundaries and stretches the decal to fill them.";
        const string k_EditUVTooltip = "Modify the UV and the pivot position without moving the projection box. It can alter Transform.";

        static readonly GUIContent k_ScaleMode = L10n.TextContent("Scale Mode", "Specifies the scaling mode to apply to decals that use this Decal Projector.", null, null);
        static readonly GUIContent k_WidthContent = L10n.TextContent("Width", "Sets the width of the projection plan.", null, null);
        static readonly GUIContent k_HeightContent = L10n.TextContent("Height", "Sets the height of the projection plan.", null, null);
        static readonly GUIContent k_ProjectionDepthContent = L10n.TextContent("Projection Depth", "Sets the projection depth of the projector.", null, null);
        static readonly GUIContent k_MaterialContent = L10n.TextContent("Material", "Specifies the Material this component projects as a decal.", null, null);
        static readonly GUIContent k_RenderingLayerMaskContent = L10n.TextContent("Rendering Layers", "Specify the rendering layer mask for this projector. Unity renders decals on all meshes where at least one Rendering Layer value matches.", null, null);
        static readonly GUIContent k_DistanceContent = L10n.TextContent("Draw Distance", "Sets the distance from the Camera at which URP stop rendering the decal.", null, null);
        static readonly GUIContent k_FadeScaleContent = L10n.TextContent("Start Fade", "Controls the distance from the Camera at which this component begins to fade the decal out.", null, null);
        static readonly GUIContent k_AngleFadeContent = L10n.TextContent("Angle Fade", "Controls the fade out range of the decal based on the angle between the Decal backward direction and the vertex normal of the receiving surface. Requires 'Decal Layers' to be enabled in the URP Asset and Frame Settings.", null, null);
        static readonly GUIContent k_UVScaleContent = L10n.TextContent("Tilling", "Sets the scale for the decal Material. Scales the decal along its UV axes.", null, null);
        static readonly GUIContent k_UVBiasContent = L10n.TextContent("Offset", "Sets the offset for the decal Material. Moves the decal along its UV axes.", null, null);
        static readonly GUIContent k_OpacityContent = L10n.TextContent("Opacity", "Controls the transparency of the decal.", null, null);
        static readonly GUIContent k_Offset = L10n.TextContent("Pivot", "Controls the position of the pivot point of the decal.", null, null);
        static readonly GUIContent k_NewMaterialButtonText = L10n.TextContent("New", "Creates a new decal Material asset template.", null, null);

        static readonly string k_BaseSceneEditingToolText = "<color=grey>Decal Scene Editing Mode:</color> ";
        static readonly string k_EditShapeWithoutPreservingUVName = k_BaseSceneEditingToolText + "Scale";
        static readonly string k_EditShapePreservingUVName = k_BaseSceneEditingToolText + "Crop";
        static readonly string k_EditUVAndPivotName = k_BaseSceneEditingToolText + "Pivot / UV";
    }
}
