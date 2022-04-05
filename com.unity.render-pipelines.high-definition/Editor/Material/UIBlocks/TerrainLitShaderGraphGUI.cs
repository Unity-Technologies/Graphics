using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Material GUI for TerrainLit ShaderGraph
    /// </summary>
    internal class TerrainLitShaderGraphGUI : HDShaderGUI, ITerrainLayerCustomUI
    {
        const SurfaceOptionUIBlock.Features surfaceOptionFeatures = SurfaceOptionUIBlock.Features.Unlit | SurfaceOptionUIBlock.Features.ReceiveDecal;
        const AdvancedOptionsUIBlock.Features advancedOptionsFeatures = AdvancedOptionsUIBlock.Features.Instancing;

        protected MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new SurfaceOptionUIBlock(MaterialUIBlock.ExpandableBit.Base, features: surfaceOptionFeatures),
            new TerrainSurfaceOptionsUIBlock(MaterialUIBlock.ExpandableBit.Other),
            new ShaderGraphUIBlock(MaterialUIBlock.ExpandableBit.ShaderGraph),
            new AdvancedOptionsUIBlock(MaterialUIBlock.ExpandableBit.Advance, features: advancedOptionsFeatures),
        };

        protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            uiBlocks.OnGUI(materialEditor, props);
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
