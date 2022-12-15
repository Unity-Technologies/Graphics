using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEditor.Rendering.HighDefinition.ShaderGraph;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{

    /// <summary>
    /// GUI for Volumetric Fog Unlit shader graphs
    /// </summary>
    internal class FogVolumeShaderGUI : HDShaderGUI
    {
        MaterialUIBlockList m_UIBlocks = new MaterialUIBlockList
        {
            // new FogVolumeUIBlock(MaterialUIBlock.ExpandableBit.Base),
            new ShaderGraphUIBlock(MaterialUIBlock.ExpandableBit.ShaderGraph, ShaderGraphUIBlock.Features.Unlit),
        };

        protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            // For now we only expose the fog blending mode
            m_UIBlocks.OnGUI(materialEditor, props);
        }

        public override void ValidateMaterial(Material material) => ShaderGraphAPI.ValidateFogVolumeMaterial(material);
    }
}
