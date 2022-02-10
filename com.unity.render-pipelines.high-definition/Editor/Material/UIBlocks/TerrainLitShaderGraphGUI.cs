using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Material GUI for TerrainLit ShaderGraph
    /// </summary>
    internal class TerrainLitShaderGraphGUI : HDShaderGUI, ITerrainLayerCustomUI
    {
        const SurfaceOptionUIBlock.Features surfaceOptionFeatures = SurfaceOptionUIBlock.Features.Surface | SurfaceOptionUIBlock.Features.ReceiveDecal;
        const AdvancedOptionsUIBlock.Features advancedOptionsFeatures = AdvancedOptionsUIBlock.Features.Instancing;

        protected MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new SurfaceOptionUIBlock(MaterialUIBlock.ExpandableBit.Base, features: surfaceOptionFeatures),
            new TerrainSurfaceOptionsUIBlock(MaterialUIBlock.ExpandableBit.Base),
            new AdvancedOptionsUIBlock(MaterialUIBlock.ExpandableBit.Advance, features: advancedOptionsFeatures),
        };

        protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            uiBlocks.Initialize(materialEditor, props);

            uiBlocks.FetchUIBlock<SurfaceOptionUIBlock>().UpdateMaterialProperties(props);
            uiBlocks.FetchUIBlock<SurfaceOptionUIBlock>().OnGUI();

            uiBlocks.FetchUIBlock<TerrainSurfaceOptionsUIBlock>().UpdateMaterialProperties(props);
            uiBlocks.FetchUIBlock<TerrainSurfaceOptionsUIBlock>().OnGUI();

            uiBlocks.FetchUIBlock<AdvancedOptionsUIBlock>().UpdateMaterialProperties(props);
            uiBlocks.FetchUIBlock<AdvancedOptionsUIBlock>().OnGUI();
        }

        bool ITerrainLayerCustomUI.OnTerrainLayerGUI(TerrainLayer terrainLayer, Terrain terrain)
        {
            var terrainSurfaceOptions = uiBlocks.FetchUIBlock<TerrainSurfaceOptionsUIBlock>();
            if (terrainSurfaceOptions == null)
                return false;

            return terrainSurfaceOptions.OnTerrainLayerGUI(terrainLayer, terrain);
        }

        public override void ValidateMaterial(Material material) => TerrainLitAPI.ValidateMaterial(material);
    }
}
