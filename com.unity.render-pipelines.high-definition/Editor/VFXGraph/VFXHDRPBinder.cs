using System;
using UnityEditor.Rendering.HighDefinition;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.VFX.HDRP
{
    class VFXHDRPBinder : VFXSRPBinder
    {
        public override string templatePath     { get { return "Packages/com.unity.render-pipelines.high-definition/Editor/VFXGraph/Shaders"; } }
        public override string runtimePath      { get { return "Packages/com.unity.render-pipelines.high-definition/Runtime/VFXGraph/Shaders"; } }

        public override string SRPAssetTypeStr  { get { return typeof(HDRenderPipelineAsset).Name; } }
        public override Type SRPOutputDataType  { get { return typeof(VFXHDRPSubOutput); } }

        public override void SetupMaterial(Material mat)
        {
            try
            {
                HDShaderUtils.ResetMaterialKeywords(mat);
            }
            catch (ArgumentException) // Silently catch the 'Unknown shader' in case of non HDRP shaders
            {}
        }
    }
}
