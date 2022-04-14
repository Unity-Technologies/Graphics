
using System;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class TextureTypeConstant : ICLDSConstant
    {
        override protected object GetValue() => BaseTextureType.GetTextureAsset(GetField());
        override protected void SetValue(object value) => BaseTextureType.SetTextureAsset(GetField(), (Texture)value);
        override public object DefaultValue => null;
        override public Type Type => ObjectValue != null ? ObjectValue.GetType() : typeof(Texture2D);
        override public TypeHandle GetTypeHandle()
        {
            switch (ObjectValue)
            {
                case Texture2DArray: return ShaderGraphExampleTypes.Texture2DArrayTypeHandle;
                case Texture3D: return ShaderGraphExampleTypes.Texture3DTypeHandle;
                case Cubemap: return ShaderGraphExampleTypes.CubemapTypeHandle;
                case Texture2D:
                default: return ShaderGraphExampleTypes.Texture2DTypeHandle;
            }
        }
    }
}
