using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{

    /// <summary>
    /// GUI for HDRP AxF materials
    /// </summary>
    class AxFGUI : HDShaderGUI
    {
        // protected override uint defaultExpandedState { get { return (uint)(Expandable.Base | Expandable.Detail | Expandable.Emissive | Expandable.Input | Expandable.Other | Expandable.Tesselation | Expandable.Transparency | Expandable.VertexAnimation); } }

        MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new SurfaceOptionUIBlock(MaterialUIBlock.ExpandableBit.Base,
                features: SurfaceOptionUIBlock.Features.Surface | SurfaceOptionUIBlock.Features.BlendMode | SurfaceOptionUIBlock.Features.DoubleSided |
                SurfaceOptionUIBlock.Features.AlphaCutoff |  SurfaceOptionUIBlock.Features.AlphaCutoffShadowThreshold | SurfaceOptionUIBlock.Features.DoubleSidedNormalMode |
                SurfaceOptionUIBlock.Features.ReceiveSSR | SurfaceOptionUIBlock.Features.ReceiveDecal | SurfaceOptionUIBlock.Features.PreserveSpecularLighting
            ),
            new AxfMainSurfaceInputsUIBlock(MaterialUIBlock.ExpandableBit.Input),
            new AxfSurfaceInputsUIBlock(MaterialUIBlock.ExpandableBit.Other),
            new AdvancedOptionsUIBlock(MaterialUIBlock.ExpandableBit.Advance, AdvancedOptionsUIBlock.Features.Instancing | AdvancedOptionsUIBlock.Features.SpecularOcclusion | AdvancedOptionsUIBlock.Features.AddPrecomputedVelocity),
        };

        protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            uiBlocks.OnGUI(materialEditor, props);
        }

        public override void ValidateMaterial(Material material) => AxFAPI.ValidateMaterial(material);
    }
} // namespace UnityEditor
