using System;
using UnityEditor.VFX;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.VFX.HDRP
{
    class VFXHDRPBinder : VFXSRPBinder
    {
        public override string templatePath     { get { return "Packages/com.unity.visualeffectgraph/Shaders/RenderPipeline/HDRP"; } }
        public override string SRPAssetTypeStr  { get { return typeof(HDRenderPipelineAsset).Name; } }
        public override Type SRPOutputDataType  { get { return typeof(VFXHDRPSubOutput); } }
    }
}
