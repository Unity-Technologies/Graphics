using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    partial class DecalProjectorEditor
    {
        internal const string k_EditShapePreservingUVTooltip = "Modifies the projector boundaries and crops/tiles the decal to fill them.";
        internal const string k_EditShapeWithoutPreservingUVTooltip = "Modifies the projector boundaries and stretches the decal to fill them.";
        internal const string k_EditUVTooltip = "Modify the UV and the pivot position without moving the projection box. It can alter Transform.";

        static readonly GUIContent k_ScaleMode = L10n.TextContent("Scale Mode", "Specifies the scaling mode to apply to decals that use this Decal Projector.", null, null);
        static readonly GUIContent k_SizeContent = L10n.TextContent("Size", "Sets the size of the projector.", null, null);
        static readonly GUIContent[] k_SizeSubContent = new[]
        {
            L10n.TextContent("Width", "Sets the width of the projection plan.", null, null),
            L10n.TextContent("Height", "Sets the height of the projection plan.", null, null)
        };
        static readonly GUIContent k_ProjectionDepthContent = L10n.TextContent("Projection Depth", "Sets the projection depth of the projector.", null, null);
        static readonly GUIContent k_MaterialContent = L10n.TextContent("Material", "Specifies the Material this component projects as a decal.", null, null);
        static readonly GUIContent k_DecalLayerMaskContent = L10n.TextContent("Rendering Layer Mask", "Specifies the Rendering Layers that the Projector affects. This Projector affects Renderers with matching Rendering Layer flags.\nEnable Layers in the Decal section of the HDRP asset to access it.", null, null);
        static readonly GUIContent k_DistanceContent = L10n.TextContent("Draw Distance", "Sets the distance from the Camera at which HDRP stop rendering the decal. If this is larger than the Draw Distance setting in the HDRP asset the HDRP asset setting is applied", null, null);
        static readonly GUIContent k_FadeScaleContent = L10n.TextContent("Start Fade", "Controls the distance from the Camera at which this component begins to fade the decal out.", null, null);
        static readonly GUIContent k_AngleFadeContent = L10n.TextContent("Angle Fade", "Controls the fade out range of the decal based on the angle between the Decal backward direction and the vertex normal of the receiving surface. Requires 'Decal Layers' to be enabled in the HDRP Asset and Frame Settings.", null, null);
        static readonly GUIContent k_UVScaleContent = L10n.TextContent("Tilling", "Sets the scale for the decal Material. Scales the decal along its UV axes.", null, null);
        static readonly GUIContent k_UVBiasContent = L10n.TextContent("Offset", "Sets the offset for the decal Material. Moves the decal along its UV axes.", null, null);
        static readonly GUIContent k_FadeFactorContent = L10n.TextContent("Fade Factor", "Controls the transparency of the decal.", null, null);
        static readonly GUIContent k_AffectTransparentContent = L10n.TextContent("Affects Transparent", "When enabled, HDRP draws this projector's decal on top of transparent surfaces.", null, null);
        static readonly GUIContent k_TransparentTextureResolutionContent = L10n.TextContent("Transparent Texture resolution", "Resolution of the texture that are used during transparent rendering.", null, null);
        static readonly GUIContent k_Offset = L10n.TextContent("Pivot", "Controls the position of the pivot point of the decal.", null, null);
    }
}
