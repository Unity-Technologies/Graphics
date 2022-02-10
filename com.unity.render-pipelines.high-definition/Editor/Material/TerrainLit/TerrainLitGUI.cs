using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// GUI for HDRP Terrain Lit materials (does not include ShaderGraphs)
    /// </summary>
    internal class TerrainLitGUI : HDShaderGUI, ITerrainLayerCustomUI
    {
        const SurfaceOptionUIBlock.Features surfaceOptionFeatures = SurfaceOptionUIBlock.Features.Unlit | SurfaceOptionUIBlock.Features.ReceiveDecal;
        const AdvancedOptionsUIBlock.Features advancedOptionsFeatures = AdvancedOptionsUIBlock.Features.Instancing | AdvancedOptionsUIBlock.Features.SpecularOcclusion;

        protected  MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new SurfaceOptionUIBlock(MaterialUIBlock.ExpandableBit.Base, features: surfaceOptionFeatures),
            new TerrainSurfaceOptionsUIBlock(MaterialUIBlock.ExpandableBit.Other),
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
