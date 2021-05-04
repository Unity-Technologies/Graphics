#if HAS_VFX_GRAPH
using System;

namespace UnityEditor.VFX.URP
{
    class VFXURPBinder : VFXSRPBinder
    {
        public override string templatePath { get { return "Packages/com.unity.render-pipelines.universal/Editor/VFXGraph/Shaders"; } }
        public override string runtimePath { get { return "Packages/com.unity.render-pipelines.universal/Runtime/VFXGraph/Shaders"; } }
        public override string SRPAssetTypeStr { get { return "UniversalRenderPipelineAsset"; } }
        public override Type SRPOutputDataType { get { return typeof(VFXURPSubOutput); } }
    }

    class VFXLWRPBinder : VFXURPBinder
    {
        public override string SRPAssetTypeStr { get { return "LightweightRenderPipelineAsset"; } }
    }
}
#endif
