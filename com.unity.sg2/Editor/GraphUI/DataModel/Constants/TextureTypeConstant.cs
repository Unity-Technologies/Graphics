
using System;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using Unity.GraphToolsFoundation;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class TextureTypeConstant : BaseShaderGraphConstant
    {
        protected override void StoreValueForCopy()
        {
            var currentTexture = GetValue();
            if (currentTexture != null)
            {
                var textureObject = (Texture)currentTexture;
                var texturePath = AssetDatabase.GetAssetPath(textureObject.GetInstanceID());
                textureAssetGuid = AssetDatabase.GUIDFromAssetPath(texturePath).ToString();
            }
        }

        public override object GetStoredValueForCopy()
        {
            if (!String.IsNullOrEmpty(textureAssetGuid))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(textureAssetGuid);
                var textureObject = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
                return textureObject;
            }

            return null;
        }

        [SerializeField]
        string textureAssetGuid;

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
                switch (GetTextureType())
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
            switch (GetTextureType())
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
