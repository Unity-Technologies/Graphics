using UnityEngine;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Material GUI for Lit ShaderGraph
    /// </summary>
    internal class LitShaderGraphGUI : LightingShaderGraphGUI
    {
        public LitShaderGraphGUI()
        {
            // Lit SG have refraction block 
            uiBlocks.Insert(1, new TransparencyUIBlock(MaterialUIBlock.Expandable.Transparency, TransparencyUIBlock.Features.Refraction));
        }

        /// <summary>
        /// Sets up the keywords and passes for a Lit Shader Graph material.
        /// </summary>
        /// <param name="material">The target material.</param>
        public static void SetupMaterialKeywordsAndPass(Material material)
        {
            SynchronizeShaderGraphProperties(material);

            LitGUI.SetupMaterialKeywordsAndPass(material);

            bool receiveSSR = false;
            if (material.GetSurfaceType() == SurfaceType.Transparent)
                receiveSSR = material.HasProperty(kReceivesSSRTransparent) ? material.GetFloat(kReceivesSSRTransparent) != 0 : false;
            else
                receiveSSR = material.HasProperty(kReceivesSSR) ? material.GetFloat(kReceivesSSR) != 0 : false;
            bool useSplitLighting = material.HasProperty(kUseSplitLighting) ? material.GetInt(kUseSplitLighting) != 0: false;
            BaseLitGUI.SetupStencil(material, receiveSSR, useSplitLighting);

            if (material.HasProperty(kAddPrecomputedVelocity))
                CoreUtils.SetKeyword(material, "_ADD_PRECOMPUTED_VELOCITY", material.GetInt(kAddPrecomputedVelocity) != 0);
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material) => SetupMaterialKeywordsAndPass(material);
    }
}
