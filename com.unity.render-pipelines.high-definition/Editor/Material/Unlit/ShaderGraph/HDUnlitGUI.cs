using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// GUI for HDRP Unlit shader graphs
    /// </summary>
    internal class HDUnlitGUI : HDShaderGUI
    {
        const SurfaceOptionUIBlock.Features surfaceOptionFeatures = SurfaceOptionUIBlock.Features.Unlit;

        MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new SurfaceOptionUIBlock(MaterialUIBlock.Expandable.Base, features: surfaceOptionFeatures),
            new ShaderGraphUIBlock(MaterialUIBlock.Expandable.ShaderGraph, ShaderGraphUIBlock.Features.Unlit),
            new AdvancedOptionsUIBlock(MaterialUIBlock.Expandable.Advance, ~AdvancedOptionsUIBlock.Features.SpecularOcclusion)
        };

        /// <summary>
        /// Implement your custom GUI in this function. To display a UI similar to HDRP shaders, use a MaterialUIBlock.
        /// </summary>
        /// <param name="materialEditor">The current material editor.</param>
        /// <param name="props">The list of properties the material has.</param>
        protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                uiBlocks.OnGUI(materialEditor, props);
                ApplyKeywordsAndPassesIfNeeded(changed.changed, uiBlocks.materials);
            }
        }

        /// <summary>
        /// Sets up the keywords and passes for an Unlit Shader Graph material.
        /// </summary>
        /// <param name="material">The target material.</param>
        public static void SetupMaterialKeywordsAndPass(Material material)
        {
            SynchronizeShaderGraphProperties(material);
            UnlitGUI.SetupUnlitMaterialKeywordsAndPass(material);
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material) => SetupMaterialKeywordsAndPass(material);
    }
}
