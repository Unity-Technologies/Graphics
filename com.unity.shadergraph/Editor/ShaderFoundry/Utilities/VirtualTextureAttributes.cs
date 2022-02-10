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

        internal static VirtualTextureAttribute TryParse(ShaderAttribute attribute)
        {
            if (attribute.Name != AttributeName)
                return null;

            var result = new VirtualTextureAttribute();

            var signature = new AttributeParsing.SignatureDescription();
            signature.ParameterDescriptions = new List<AttributeParsing.ParameterDescription>
            {
                new AttributeParsing.ParameterDescription(LayerCountParamName, (param, index) => AttributeParsing.IntRangeParseCallback(param, index, 0, MaxLayerCount - 1, ref result.LayerCount)),
            };
            signature.UnknownParameterCallback = (param, index) =>
            {
                if (index != 0)
                    throw new Exception($"Attribtute {AttributeName} only allows one argument.");
                AttributeParsing.IntRangeParseCallback(param, index, 0, MaxLayerCount - 1, ref result.LayerCount);
            };
            AttributeParsing.Parse(attribute, signature);

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

        internal static VirtualTextureLayerAttribute TryParse(ShaderAttribute attribute)
        {
            if (attribute.Name != AttributeName)
                return null;
            var result = new VirtualTextureLayerAttribute();

            var signature = new AttributeParsing.SignatureDescription();
            signature.ParameterDescriptions = new List<AttributeParsing.ParameterDescription>
            {
                new AttributeParsing.ParameterDescription(IndexParamName, (param, index) => AttributeParsing.IntParseCallback(param, index, ref result.Index)),
                new AttributeParsing.ParameterDescription(UniformNameParamName, (param, index) => AttributeParsing.StringParseCallback(param, index, ref result.UniformName)),
                new AttributeParsing.ParameterDescription(DisplayNameParamName, (param, index) => AttributeParsing.StringParseCallback(param, index, ref result.DisplayName)),
                new AttributeParsing.ParameterDescription(TextureNameParamName, (param, index) => AttributeParsing.StringParseCallback(param, index, ref result.TextureName)),
                new AttributeParsing.ParameterDescription(TextureTypeParamName, (param, index) => AttributeParsing.EnumParseCallback(param, index, ref result.TextureType) ),
            };
            AttributeParsing.Parse(attribute, signature);

            return result;
        }
    }
}
