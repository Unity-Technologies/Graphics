using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// The UI block that represents common properties for unlit Shader Graphs.
    /// </summary>
    public class UnlitShaderGraphGUI : HDShaderGUI
    {
        const SurfaceOptionUIBlock.Features surfaceOptionFeatures = SurfaceOptionUIBlock.Features.Unlit;

        MaterialUIBlockList m_UIBlocks = new MaterialUIBlockList
        {
            new SurfaceOptionUIBlock(MaterialUIBlock.ExpandableBit.Base, features: surfaceOptionFeatures),
            new ShaderGraphUIBlock(MaterialUIBlock.ExpandableBit.ShaderGraph, ShaderGraphUIBlock.Features.Unlit),
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
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                m_UIBlocks.OnGUI(materialEditor, props);
                ApplyKeywordsAndPassesIfNeeded(changed.changed, m_UIBlocks.materials);
            }
        }

        /// <summary>
        /// Sets up the keywords and passes for the Unlit Shader Graph material you pass in.
        /// </summary>
        /// <param name="material">The target material.</param>
        public static void SetupUnlitKeywordsAndPass(Material material)
        {
            SynchronizeShaderGraphProperties(material);
            UnlitGUI.SetupUnlitKeywordsAndPass(material);
        }

        /// <summary>
        /// Sets up the keywords and passes for the current selected material.
        /// </summary>
        /// <param name="material">The selected material.</param>
        protected override void SetupMaterialKeywordsAndPass(Material material) => SetupUnlitKeywordsAndPass(material);
    }
}
