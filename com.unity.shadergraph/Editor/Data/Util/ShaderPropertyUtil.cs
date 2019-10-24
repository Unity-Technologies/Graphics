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
    }
}
