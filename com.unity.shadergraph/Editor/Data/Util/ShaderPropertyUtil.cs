using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    static class ShaderPropertyUtil
    {
        public static bool IsAnyTextureType(this AbstractShaderProperty shaderProperty)
            => shaderProperty is TextureShaderProperty
               || shaderProperty is Texture2DArrayShaderProperty
               || shaderProperty is Texture3DShaderProperty
               || shaderProperty is CubemapShaderProperty;
    }
}
