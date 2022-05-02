
using System;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class TextureTypeConstant : BaseShaderGraphConstant
    {
        override protected object GetValue() => BaseTextureType.GetTextureAsset(GetField());
        override protected void SetValue(object value) => BaseTextureType.SetTextureAsset(GetField(), (Texture)value);
        override public object DefaultValue => null;

        public void SetTextureType(BaseTextureType.TextureType textype)
            => BaseTextureType.SetTextureType(GetField(), textype);
        public BaseTextureType.TextureType GetTextureType()
            => BaseTextureType.GetTextureType(GetField());

        override public Type Type
        {
            get
            {
                if (!IsInitialized)
                    return typeof(Texture);
                switch (BaseTextureType.GetTextureType(GetField()))
                {
                    case BaseTextureType.TextureType.Texture3D: return typeof(Texture3D);
                    case BaseTextureType.TextureType.CubeMap: return typeof(Cubemap);
                    case BaseTextureType.TextureType.Texture2DArray: return typeof(Texture2DArray);
                    case BaseTextureType.TextureType.Texture2D:
                    default: return typeof(Texture2D);
                }
            }
        }
        override public TypeHandle GetTypeHandle()
        {
            switch (BaseTextureType.GetTextureType(GetField()))
            {
                case BaseTextureType.TextureType.Texture3D: return ShaderGraphExampleTypes.Texture3DTypeHandle;
                case BaseTextureType.TextureType.CubeMap: return ShaderGraphExampleTypes.CubemapTypeHandle;
                case BaseTextureType.TextureType.Texture2DArray: return ShaderGraphExampleTypes.Texture2DArrayTypeHandle;
                case BaseTextureType.TextureType.Texture2D:
                default: return ShaderGraphExampleTypes.Texture2DTypeHandle;
            }
        }
    }
}
