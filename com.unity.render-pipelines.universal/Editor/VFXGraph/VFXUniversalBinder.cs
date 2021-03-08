#if USE_VFX
using System;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.VFX;
using UnityEngine;

namespace UnityEditor.VFX.HDRP
{
    class VFXUniversalBinder : VFXSRPBinder
    {
        //TODOPAUL : Move template as well
        public override string templatePath { get { return "Packages/com.unity.visualeffectgraph/Shaders/RenderPipeline/Universal"; } }
        public override string SRPAssetTypeStr { get { return "UniversalRenderPipelineAsset"; } }
        public override Type SRPOutputDataType { get { return typeof(VFXUniversalSubOutput); } }
    }

    class VFXLWRPBinder : VFXUniversalBinder
    {
        public override string SRPAssetTypeStr { get { return "LightweightRenderPipelineAsset"; } }
    }
}
#endif
