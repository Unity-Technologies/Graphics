using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    internal class VirtualTextureAttribute
    {
        const string AttributeName = "VirtualTexture";
        const string LayerCountParamName = "layerCount";

        internal const int MaxLayerCount = 4;
        internal int LayerCount = 2;

        internal ShaderAttribute Build(ShaderContainer container)
        {
            var attributeBuilder = new ShaderAttribute.Builder(container, AttributeName);
            attributeBuilder.Param(new ShaderAttributeParam.Builder(container, null, LayerCount.ToString()).Build());
            return attributeBuilder.Build();
        }

        internal static VirtualTextureAttribute FindFirst(IEnumerable<ShaderAttribute> attributes)
        {
            var attribute = attributes.FindFirst(AttributeName);
            if (attribute.IsValid)
                return TryParse(attribute);
            return null;
        }

        static AttributeParsing.SignatureDescription<VirtualTextureAttribute> AttributeSignature = new AttributeParsing.SignatureDescription<VirtualTextureAttribute>()
        {
            ParameterDescriptions = new List<AttributeParsing.ParameterDescription<VirtualTextureAttribute>>
            {
                new AttributeParsing.ParameterDescription<VirtualTextureAttribute>(LayerCountParamName, (param, index, target) => AttributeParsing.ParseIntRange(param, index, 0, MaxLayerCount - 1, ref target.LayerCount)),
            },
            UnknownParameterCallback = (param, index, target) =>
            {
                if (index != 0)
                    ErrorHandling.ReportError($"Attribute {AttributeName} only allows one argument.");
                AttributeParsing.ParseIntRange(param, index, 0, MaxLayerCount - 1, ref target.LayerCount);
            }
        };

        internal static VirtualTextureAttribute TryParse(ShaderAttribute attribute)
        {
            if (attribute.Name != AttributeName)
                return null;

            var result = new VirtualTextureAttribute();
            AttributeParsing.Parse(attribute, AttributeSignature, result);
            return result;
        }
    }

    internal class VirtualTextureLayerAttribute
    {
        const string AttributeName = "VirtualTextureLayer";
        const string IndexParamName = "index";
        const string UniformNameParamName = "uniformName";
        const string DisplayNameParamName = "displayName";
        const string TextureNameParamName = "textureName";
        const string TextureTypeParamName = "textureType";

        internal enum LayerTextureType { Default, NormalTangentSpace, NormalObjectSpace };
        internal int Index = 0;
        internal string UniformName;
        internal string DisplayName;
        internal string TextureName;
        internal LayerTextureType TextureType = LayerTextureType.Default;

        internal ShaderAttribute Build(ShaderContainer container)
        {
            var attributeBuilder = new ShaderAttribute.Builder(container, AttributeName);
            attributeBuilder.Param(new ShaderAttributeParam.Builder(container, IndexParamName, Index.ToString()).Build());
            if (!string.IsNullOrEmpty(UniformName))
                attributeBuilder.Param(new ShaderAttributeParam.Builder(container, UniformNameParamName, UniformName).Build());
            if (!string.IsNullOrEmpty(DisplayName))
                attributeBuilder.Param(new ShaderAttributeParam.Builder(container, DisplayNameParamName, DisplayName).Build());
            if (!string.IsNullOrEmpty(TextureName))
                attributeBuilder.Param(new ShaderAttributeParam.Builder(container, TextureNameParamName, TextureName).Build());
            if (TextureType != LayerTextureType.Default)
                attributeBuilder.Param(new ShaderAttributeParam.Builder(container, TextureTypeParamName, TextureType.ToString()).Build());
            return attributeBuilder.Build();
        }

        internal static VirtualTextureLayerAttribute FindFirst(IEnumerable<ShaderAttribute> attributes)
        {
            var attribute = attributes.FindFirst(AttributeName);
            if (attribute.IsValid)
                return TryParse(attribute);
            return null;
        }

        static AttributeParsing.SignatureDescription<VirtualTextureLayerAttribute> AttributeSignature = new AttributeParsing.SignatureDescription<VirtualTextureLayerAttribute>()
        {
            ParameterDescriptions = new List<AttributeParsing.ParameterDescription<VirtualTextureLayerAttribute>>
            {
                new AttributeParsing.ParameterDescription<VirtualTextureLayerAttribute>(IndexParamName, (param, index, target) => AttributeParsing.ParseInt(param, index, ref target.Index)),
                new AttributeParsing.ParameterDescription<VirtualTextureLayerAttribute>(UniformNameParamName, (param, index, target) => AttributeParsing.ParseString(param, index, ref target.UniformName)),
                new AttributeParsing.ParameterDescription<VirtualTextureLayerAttribute>(DisplayNameParamName, (param, index, target) => AttributeParsing.ParseString(param, index, ref target.DisplayName)),
                new AttributeParsing.ParameterDescription<VirtualTextureLayerAttribute>(TextureNameParamName, (param, index, target) => AttributeParsing.ParseString(param, index, ref target.TextureName)),
                new AttributeParsing.ParameterDescription<VirtualTextureLayerAttribute>(TextureTypeParamName, (param, index, target) => AttributeParsing.ParseEnum(param, index, ref target.TextureType)),
            }
        };

        internal static VirtualTextureLayerAttribute TryParse(ShaderAttribute attribute)
        {
            if (attribute.Name != AttributeName)
                return null;
            var result = new VirtualTextureLayerAttribute();

            var signature = new AttributeParsing.SignatureDescription<VirtualTextureLayerAttribute>();
            AttributeParsing.Parse(attribute, AttributeSignature, result);
            return result;
        }
    }
}
