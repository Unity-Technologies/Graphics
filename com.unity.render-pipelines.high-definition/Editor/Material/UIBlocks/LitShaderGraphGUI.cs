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
            uiBlocks[uiBlocks.FindIndex(b => b is AdvancedOptionsUIBlock)] = new LitAdvancedOptionsUIBlock(MaterialUIBlock.ExpandableBit.Advance, ~AdvancedOptionsUIBlock.Features.SpecularOcclusion);

            // Lit SG have refraction block
            uiBlocks.Insert(1, new TransparencyUIBlock(MaterialUIBlock.ExpandableBit.Transparency, TransparencyUIBlock.Features.Refraction));
        }
    }
}
