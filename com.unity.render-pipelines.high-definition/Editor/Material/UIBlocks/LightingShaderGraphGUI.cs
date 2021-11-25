using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Common GUI for Lit ShaderGraphs.
    /// </summary>
    public class LightingShaderGraphGUI : HDShaderGUI
    {
        // For surface option shader graph we only want all unlit features but alpha clip and back then front rendering
        const SurfaceOptionUIBlock.Features surfaceOptionFeatures = SurfaceOptionUIBlock.Features.Lit
            | SurfaceOptionUIBlock.Features.ShowDepthOffsetOnly;

        MaterialUIBlockList m_UIBlocks = new MaterialUIBlockList
        {
            new SurfaceOptionUIBlock(MaterialUIBlock.ExpandableBit.Base, features: surfaceOptionFeatures),
            new TessellationOptionsUIBlock(MaterialUIBlock.ExpandableBit.Tessellation),
            new ShaderGraphUIBlock(MaterialUIBlock.ExpandableBit.ShaderGraph),
            new AdvancedOptionsUIBlock(MaterialUIBlock.ExpandableBit.Advance, ~AdvancedOptionsUIBlock.Features.SpecularOcclusion)
        };

        /// <summary>List of UI Blocks used to render the material inspector.</summary>
        protected MaterialUIBlockList uiBlocks => m_UIBlocks;

        /// <summary>
        /// Implement your custom GUI in this function. To display a UI similar to HDRP shaders, use a MaterialUIBlockList.
        /// </summary>
        /// <param name="materialEditor">The current material editor.</param>
        /// <param name="props">The list of properties the material has.</param>
        protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            m_UIBlocks.OnGUI(materialEditor, props);
        }

        /// <summary>
        /// Sets up the keywords and passes for the current selected material.
        /// </summary>
        /// <param name="material">The selected material.</param>
        public override void ValidateMaterial(Material material) => ShaderGraphAPI.ValidateLightingMaterial(material);
    }
}
