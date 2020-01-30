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
    class HDUnlitGUI : HDShaderGUI
    {
        // For surface option shader graph we only want all unlit features but alpha clip, double sided mode and back then front rendering
        const SurfaceOptionUIBlock.Features   surfaceOptionFeatures = SurfaceOptionUIBlock.Features.Unlit
            ^ SurfaceOptionUIBlock.Features.AlphaCutoffThreshold
            ^ SurfaceOptionUIBlock.Features.DoubleSidedNormalMode
            ^ SurfaceOptionUIBlock.Features.BackThenFrontRendering;

        MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new SurfaceOptionUIBlock(MaterialUIBlock.Expandable.Base, features: surfaceOptionFeatures),
            new ShaderGraphUIBlock(MaterialUIBlock.Expandable.ShaderGraph, ShaderGraphUIBlock.Features.Unlit),
        };

        protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                uiBlocks.OnGUI(materialEditor, props);
                ApplyKeywordsAndPassesIfNeeded(changed.changed, uiBlocks.materials);
            }
        }

        public static void SetupMaterialKeywordsAndPass(Material material)
        {
            SynchronizeShaderGraphProperties(material);
            UnlitGUI.SetupUnlitMaterialKeywordsAndPass(material);
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material) => SetupMaterialKeywordsAndPass(material);
    }
}
