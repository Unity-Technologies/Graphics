using System;
using System.Collections.Generic;
using UnityEditor.Rendering.HighDefinition;
using UnityEditor.Rendering.HighDefinition.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
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

        static Dictionary<HDShaderUtils.ShaderID, string> s_ShaderNames = new Dictionary<HDShaderUtils.ShaderID, string>()
        {
            { HDShaderUtils.ShaderID.SG_Lit,      "Lit"      },
            { HDShaderUtils.ShaderID.SG_StackLit, "StackLit" },
            { HDShaderUtils.ShaderID.SG_Hair,     "Hair"     },
            { HDShaderUtils.ShaderID.SG_Eye,      "Eye"      },
            { HDShaderUtils.ShaderID.SG_Fabric,   "Fabric"   },
            { HDShaderUtils.ShaderID.SG_Unlit,    "Unlit"    },
        };

        HDShaderUtils.ShaderID GetShaderEnumFromShaderGraph(ShaderGraphVfxAsset shaderGraph)
        {
            bool TryGetHDMetadata(out HDMetadata obj)
            {
                obj = null;

                var path = AssetDatabase.GetAssetPath(shaderGraph);
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is HDMetadata metadataAsset)
                    {
                        obj = metadataAsset;
                        return true;
                    }
                }

                return false;
            }

            HDMetadata obj;

            if (!TryGetHDMetadata(out obj))
                throw new ArgumentException("Unknown shader");

            return obj.shaderID;
        }

        public override void SetupMaterial(Material mat, bool hasMotionVector = false, bool hasShadowCasting = false, ShaderGraphVfxAsset shaderGraph = null)
        {
            try
            {
                if (shaderGraph != null)
                {
                    // Recover the HDRP Shader Enum from the VFX Shader Graph.
                    var shaderID = GetShaderEnumFromShaderGraph(shaderGraph);
                    HDShaderUtils.ResetMaterialKeywords(mat, shaderID);

                    // Configure HDRP Shadow + MV
                    mat.SetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr, hasMotionVector);
                    mat.SetShaderPassEnabled(HDShaderPassNames.s_ShadowCasterStr, hasShadowCasting);
                }
                else
                    HDShaderUtils.ResetMaterialKeywords(mat);
            }
            catch (ArgumentException) // Silently catch the 'Unknown shader' in case of non HDRP shaders
            {}
        }

        public override VFXAbstractRenderedOutput.BlendMode GetBlendModeFromMaterial(VFXMaterialSerializedSettings materialSettings)
        {
            var blendMode = VFXAbstractRenderedOutput.BlendMode.Opaque;

            if (!materialSettings.HasProperty(HDMaterialProperties.kSurfaceType) ||
                !materialSettings.HasProperty(HDMaterialProperties.kBlendMode))
            {
                return blendMode;
            }

            var surfaceType = materialSettings.GetFloat(HDMaterialProperties.kSurfaceType);
            if (surfaceType == (int)SurfaceType.Transparent)
            {
                switch (materialSettings.GetFloat(HDMaterialProperties.kBlendMode))
                {
                    case (int)BlendMode.Additive:
                        blendMode = VFXAbstractRenderedOutput.BlendMode.Additive;
                        break;
                    case (int)BlendMode.Alpha:
                        blendMode = VFXAbstractRenderedOutput.BlendMode.Alpha;
                        break;
                    case (int)BlendMode.Premultiply:
                        blendMode = VFXAbstractRenderedOutput.BlendMode.AlphaPremultiplied;
                        break;
                }
            }

            return blendMode;
        }

        public override bool TransparentMotionVectorEnabled(Material material)
        {
            if (!material.HasProperty(HDMaterialProperties.kSurfaceType) ||
                !material.HasProperty(HDMaterialProperties.kTransparentWritingMotionVec))
            {
                return false;
            }

            var surfaceType = material.GetFloat(HDMaterialProperties.kSurfaceType);

            if (surfaceType == (int)SurfaceType.Transparent)
                return material.GetFloat(HDMaterialProperties.kTransparentWritingMotionVec) == 1f;

            return false;
        }

        public override string GetShaderName(ShaderGraphVfxAsset shaderGraph)
        {
            // Recover the HDRP Shader Enum from the VFX Shader Graph.
            var shaderID = GetShaderEnumFromShaderGraph(shaderGraph);

            if (s_ShaderNames.ContainsKey(shaderID))
                return s_ShaderNames[shaderID];

            return string.Empty;
        }
    }
}
