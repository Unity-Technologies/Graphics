using UnityEngine;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    internal class VFXShaderGraphGUILit : LightingShaderGraphGUI
    {
        const SurfaceOptionUIBlock.Features vfxSurfaceOptionFeatures = SurfaceOptionUIBlock.Features.Lit
            | SurfaceOptionUIBlock.Features.ShowDepthOffsetOnly;

        public VFXShaderGraphGUILit()
        {
            uiBlocks.Clear();
            uiBlocks.Add(new SurfaceOptionUIBlock(MaterialUIBlock.ExpandableBit.Base, features: vfxSurfaceOptionFeatures));
            //VFX inspector UI is taking a shortcut here:
            //We aren't doing distinction between LightingShaderGraphGUI & LitShaderGUI
            //Only refraction has to be added to cover all settings cases
            uiBlocks.Add(new TransparencyUIBlock(MaterialUIBlock.ExpandableBit.Transparency, TransparencyUIBlock.Features.Refraction));
        }
    }

    internal class VFXShaderGraphGUIUnlit : UnlitShaderGraphGUI
    {
        const SurfaceOptionUIBlock.Features vfxSurfaceOptionFeatures = SurfaceOptionUIBlock.Features.Unlit
            | SurfaceOptionUIBlock.Features.ShowDepthOffsetOnly;
        public VFXShaderGraphGUIUnlit()
        {
            uiBlocks.Clear();
            uiBlocks.Add(new SurfaceOptionUIBlock(MaterialUIBlock.ExpandableBit.Base, features: vfxSurfaceOptionFeatures));
        }
    }

    internal class VFXShaderGraphGUISixWay : SixWayGUI
    {
        const SurfaceOptionUIBlock.Features vfxSurfaceOptionFeatures = SurfaceOptionUIBlock.Features.Lit
                                                                       | SurfaceOptionUIBlock.Features.ShowDepthOffsetOnly ^ SurfaceOptionUIBlock.Features.PreserveSpecularLighting;

        public VFXShaderGraphGUISixWay()
        {
            uiBlocks.Clear();
            uiBlocks.Add(new SurfaceOptionUIBlock(MaterialUIBlock.ExpandableBit.Base, features: vfxSurfaceOptionFeatures ));
            uiBlocks.Add(new SixWayUIBlock(MaterialUIBlock.ExpandableBit.Base));
        }
    }
}
