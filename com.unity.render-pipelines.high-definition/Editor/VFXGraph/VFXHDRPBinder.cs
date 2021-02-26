using System;
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

        public override void SetupMaterial(Material mat, ShaderGraphVfxAsset shaderGraph = null)
        {
            try
            {
                if (shaderGraph != null)
                {
                    // Recover the HDRP Shader Enum from the VFX Shader Graph.
                    var shaderID = GetShaderEnumFromShaderGraph(shaderGraph);
                    HDShaderUtils.ResetMaterialKeywords(mat, shaderID);
                }
                else
                    HDShaderUtils.ResetMaterialKeywords(mat);
            }
            catch (ArgumentException) // Silently catch the 'Unknown shader' in case of non HDRP shaders
            {}
        }

        public override VFXAbstractRenderedOutput.BlendMode GetBlendModeFromMaterial(Material mat)
        {
            var blendMode = VFXAbstractRenderedOutput.BlendMode.Opaque;

            if (!mat.HasProperty(HDMaterialProperties.kSurfaceType) ||
                !mat.HasProperty(HDMaterialProperties.kBlendMode))
            {
                return blendMode;
            }

            var surfaceType = mat.GetFloat(HDMaterialProperties.kSurfaceType);
            if (surfaceType == (int)SurfaceType.Transparent)
            {
                switch (mat.GetFloat(HDMaterialProperties.kBlendMode))
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
    }
}
