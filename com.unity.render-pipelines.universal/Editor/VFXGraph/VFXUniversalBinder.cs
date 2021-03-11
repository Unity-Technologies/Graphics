#if USE_VFX
using System;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.VFX;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.VFX.HDRP
{
    class VFXUniversalBinder : VFXSRPBinder
    {
        //TODOPAUL : Move VFX template as well
        public override string templatePath { get { return "Packages/com.unity.visualeffectgraph/Shaders/RenderPipeline/Universal"; } }
        public override string SRPAssetTypeStr { get { return "UniversalRenderPipelineAsset"; } }
        public override Type SRPOutputDataType { get { return typeof(VFXUniversalSubOutput); } }

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
                        case BaseShaderGUI.BlendMode.Alpha:         vfxBlendMode = VFXAbstractRenderedOutput.BlendMode.Alpha; break;
                        case BaseShaderGUI.BlendMode.Premultiply:   vfxBlendMode = VFXAbstractRenderedOutput.BlendMode.AlphaPremultiplied; break;
                        case BaseShaderGUI.BlendMode.Additive:      vfxBlendMode = VFXAbstractRenderedOutput.BlendMode.Additive; break;
                        case BaseShaderGUI.BlendMode.Multiply:      vfxBlendMode = VFXAbstractRenderedOutput.BlendMode.Additive; break; //TODOPAUL : Should we add it ?
                    }
                }
            }
            return vfxBlendMode;
        }
    }


    //TODOPAUL : not sure it's needed anymore
    class VFXLWRPBinder : VFXUniversalBinder
    {
        public override string SRPAssetTypeStr { get { return "LightweightRenderPipelineAsset"; } }
    }
}
#endif
