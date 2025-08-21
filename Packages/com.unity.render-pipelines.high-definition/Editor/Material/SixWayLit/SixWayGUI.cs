using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEditor.Rendering.HighDefinition.ShaderGraph;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{

    /// <summary>
    /// GUI for Six-way shader graphs
    /// </summary>
    internal class SixWayGUI : HDShaderGUI
    {
        MaterialUIBlockList m_UIBlocks = new()
        {
            new SurfaceOptionUIBlock(MaterialUIBlock.ExpandableBit.Base,1, (SurfaceOptionUIBlock.Features.Lit | SurfaceOptionUIBlock.Features.ShowDepthOffsetOnly) ^ SurfaceOptionUIBlock.Features.DoubleSidedNormalMode ^ SurfaceOptionUIBlock.Features.PreserveSpecularLighting),
            new SixWayUIBlock(MaterialUIBlock.ExpandableBit.Base),
            new TessellationOptionsUIBlock(MaterialUIBlock.ExpandableBit.Tessellation),
            new ShaderGraphUIBlock(MaterialUIBlock.ExpandableBit.ShaderGraph, ShaderGraphUIBlock.Features.ExposedProperties),
            new AdvancedOptionsUIBlock(MaterialUIBlock.ExpandableBit.Advance, ~AdvancedOptionsUIBlock.Features.SpecularOcclusion)
        };

        /// <summary>List of UI Blocks used to render the material inspector.</summary>
        protected MaterialUIBlockList uiBlocks => m_UIBlocks;

        protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            m_UIBlocks.OnGUI(materialEditor, props);
        }

        public override void ValidateMaterial(Material material) => ShaderGraphAPI.ValidateSixWayMaterial(material);
    }
}
