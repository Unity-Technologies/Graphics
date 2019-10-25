using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    static class ShaderPropertyUtil
    {
        public static bool IsAnyTextureType(this AbstractShaderProperty shaderProperty)
            => shaderProperty is Texture2DShaderProperty
               || shaderProperty is Texture2DArrayShaderProperty
               || shaderProperty is Texture3DShaderProperty
               || shaderProperty is CubemapShaderProperty;

        public static string GetShaderVariableDeclarationString(this AbstractShaderProperty shaderProperty, HashSet<string> systemSamplerNames)
        {
            if (shaderProperty is GradientShaderProperty gradientProperty)
                return gradientProperty.GetGraidentPropertyDeclarationString();
            else if (shaderProperty is SamplerStateShaderProperty samplerProperty)
                return samplerProperty.GetSamplerPropertyDeclarationString(systemSamplerNames);
            else
                return $"{shaderProperty.propertyType.FormatDeclarationString(shaderProperty.concretePrecision, shaderProperty.referenceName)};";
        }
    }
}
