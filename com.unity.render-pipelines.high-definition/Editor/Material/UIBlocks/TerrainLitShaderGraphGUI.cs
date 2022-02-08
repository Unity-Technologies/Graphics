using UnityEngine;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Material GUI for Lit ShaderGraph
    /// </summary>
    internal class TerrainLitShaderGraphGUI : HDShaderGUI
    {
        const SurfaceOptionUIBlock.Features surfaceOptionFeatures = SurfaceOptionUIBlock.Features.Surface | SurfaceOptionUIBlock.Features.ReceiveDecal;
        const AdvancedOptionsUIBlock.Features advancedOptionsFeatures = AdvancedOptionsUIBlock.Features.Instancing;

        private MaterialUIBlockList m_UIBlocks = new MaterialUIBlockList
        {
            new SurfaceOptionUIBlock(MaterialUIBlock.ExpandableBit.Base, features: surfaceOptionFeatures),
            new AdvancedOptionsUIBlock(MaterialUIBlock.ExpandableBit.Advance, features: advancedOptionsFeatures),
        };

        /// <summary>List of UI Blocks used to render the material inspector.</summary>
        protected MaterialUIBlockList uiBlocks => m_UIBlocks;

        public TerrainLitShaderGraphGUI()
        {

        }

        protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            uiBlocks.Initialize(materialEditor, props);
            uiBlocks.FetchUIBlock<SurfaceOptionUIBlock>().UpdateMaterialProperties(props);
            uiBlocks.FetchUIBlock<SurfaceOptionUIBlock>().OnGUI();

            uiBlocks.FetchUIBlock<AdvancedOptionsUIBlock>().UpdateMaterialProperties(props);
            uiBlocks.FetchUIBlock<AdvancedOptionsUIBlock>().OnGUI();
        }
    }
}
