using UnityEngine;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    internal class VFXShaderGraphGUI : LightingShaderGraphGUI
    {
        const SurfaceOptionUIBlock.Features vfxSurfaceOptionFeatures = SurfaceOptionUIBlock.Features.Lit
            | SurfaceOptionUIBlock.Features.ShowDepthOffsetOnly;

        public VFXShaderGraphGUI()
        {
            uiBlocks.Clear();
            uiBlocks.Add(new SurfaceOptionUIBlock(MaterialUIBlock.ExpandableBit.Base, features: vfxSurfaceOptionFeatures));
        }
    }
}
