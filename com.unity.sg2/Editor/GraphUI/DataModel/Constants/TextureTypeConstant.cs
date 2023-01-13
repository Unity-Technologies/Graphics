using System;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using Unity.GraphToolsFoundation;
using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [Serializable]
    class TextureTypeConstant : BaseShaderGraphConstant
    {
        [SerializeReference]
        Texture m_CopyPasteData;

        protected override object GetValue() => BaseTextureType.GetTextureAsset(GetField());
        protected override void SetValue(object value) => BaseTextureType.SetTextureAsset(GetField(), (Texture)value);
        public override object DefaultValue => null;

        BaseTextureType.TextureType GetTextureType() => BaseTextureType.GetTextureType(GetField());

        /// <inheritdoc />
        public override void BindTo(string nodeName, string portName)
        {
            base.BindTo(nodeName, portName);

            if (OwnerModel is NodeModel nodeModel)
            {
                nodeModel.DefineNode();
            }
        }

        public override Type Type
        {
            get
            {
                if (!IsBound)
                    return typeof(Texture2D);

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

        public override TypeHandle GetTypeHandle()
        {
            if (!IsBound)
                return ShaderGraphExampleTypes.Texture2DTypeHandle;

            switch (GetTextureType())
            {
                case BaseTextureType.TextureType.Texture3D: return ShaderGraphExampleTypes.Texture3DTypeHandle;
                case BaseTextureType.TextureType.CubeMap: return ShaderGraphExampleTypes.CubemapTypeHandle;
                case BaseTextureType.TextureType.Texture2DArray: return ShaderGraphExampleTypes.Texture2DArrayTypeHandle;
                case BaseTextureType.TextureType.Texture2D:
                default: return ShaderGraphExampleTypes.Texture2DTypeHandle;
            }
        }

        /// <inheritdoc />
        public override bool IsAssignableFrom(Type t)
        {
            return typeof(Texture).IsAssignableFrom(t);
        }

        /// <inheritdoc />
        public override void OnBeforeCopy()
        {
            m_CopyPasteData = ObjectValue as Texture;
        }

        /// <inheritdoc />
        public override void OnAfterPaste()
        {
            ObjectValue = m_CopyPasteData;
            m_CopyPasteData = null;
        }
    }
}
