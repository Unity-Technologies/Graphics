#if HAS_VFX_GRAPH
using System;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.VFX;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.VFX.URP
{
    class VFXUniversalBinder : VFXSRPBinder
    {
        public override string templatePath { get { return "Packages/com.unity.render-pipelines.universal/Editor/VFXGraph/Shaders"; } }
        public override string SRPAssetTypeStr { get { return "UniversalRenderPipelineAsset"; } }
        public override Type SRPOutputDataType { get { return typeof(VFXUniversalSubOutput); } }
    }

    class VFXLWRPBinder : VFXUniversalBinder
    {
        public override string SRPAssetTypeStr { get { return "LightweightRenderPipelineAsset"; } }
    }
}
#endif
