#if HAS_VFX_GRAPH
using System;
using UnityEngine;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.VFX.URP
{
    class VFXURPBinder : VFXSRPBinder
    {
        public override string templatePath { get { return "Packages/com.unity.render-pipelines.universal/Editor/VFXGraph/Shaders"; } }
        public override string runtimePath { get { return "Packages/com.unity.render-pipelines.universal/Runtime/VFXGraph/Shaders"; } }
        public override string SRPAssetTypeStr { get { return "UniversalRenderPipelineAsset"; } }
        public override Type SRPOutputDataType { get { return typeof(VFXURPSubOutput); } }

        public override bool IsGraphDataValid(ShaderGraph.GraphData graph)
        {
            //TODOPAUL : Probably filter todo
            return true;
        }

        public override void SetupMaterial(Material mat, bool hasMotionVector = false, bool hasShadowCasting = false, ShaderGraphVfxAsset shaderGraph = null)
        {
            //TODOPAUL (N.B. conflict on this function definition incoming ^)
            try
            {
                if (shaderGraph != null)
                {
                    //TODOPAUL : retrieve id & switch among target here
                    Rendering.Universal.ShaderGUI.UnlitShader.SetMaterialKeywords(mat);

                    // Configure HDRP Shadow + MV
                    //mat.SetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr, hasMotionVector);
                    //mat.SetShaderPassEnabled(HDShaderPassNames.s_ShadowCasterStr, hasShadowCasting);
                }
                else
                {
                    //TODOPAUL : Something todo ?
                    //HDShaderUtils.ResetMaterialKeywords(mat);
                }
            }
            catch (ArgumentException) // TODOPAUL : is it something expected ?
            {}
        }

        public override VFXAbstractRenderedOutput.BlendMode GetBlendModeFromMaterial(VFXMaterialSerializedSettings materialSettings)
        {
            var vfxBlendMode = VFXAbstractRenderedOutput.BlendMode.Opaque;
            if (materialSettings.HasProperty("_Surface"))
            {
                var surfaceType = (BaseShaderGUI.SurfaceType)materialSettings.GetFloat("_Surface");
                if (surfaceType == BaseShaderGUI.SurfaceType.Transparent)
                {
                    BaseShaderGUI.BlendMode blendMode = (BaseShaderGUI.BlendMode)materialSettings.GetFloat("_Blend");
                    switch (blendMode)
                    {
                        case BaseShaderGUI.BlendMode.Alpha: vfxBlendMode = VFXAbstractRenderedOutput.BlendMode.Alpha; break;
                        case BaseShaderGUI.BlendMode.Premultiply: vfxBlendMode = VFXAbstractRenderedOutput.BlendMode.AlphaPremultiplied; break;
                        case BaseShaderGUI.BlendMode.Additive: vfxBlendMode = VFXAbstractRenderedOutput.BlendMode.Additive; break;
                        case BaseShaderGUI.BlendMode.Multiply: vfxBlendMode = VFXAbstractRenderedOutput.BlendMode.Additive; break; //TODOPAUL : Should we add it ?
                    }
                }
            }
            return vfxBlendMode;
        }
    }

    class VFXLWRPBinder : VFXURPBinder
    {
        public override string SRPAssetTypeStr { get { return "LightweightRenderPipelineAsset"; } }
    }
}
#endif
