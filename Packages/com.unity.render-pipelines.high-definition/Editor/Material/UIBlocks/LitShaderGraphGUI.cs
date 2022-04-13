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
            uiBlocks.Insert(1, new TransparencyUIBlock(MaterialUIBlock.ExpandableBit.Transparency, TransparencyUIBlock.Features.Refraction));
        }
    }
}
