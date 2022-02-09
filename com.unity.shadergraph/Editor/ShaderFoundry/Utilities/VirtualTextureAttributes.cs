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
            var index = 0;
            foreach (var param in attribute.Parameters)
            {
                if (index > 0)
                    throw new Exception($"Argument at index {index} is invalid. {AttributeName} only accepts one argument.");

                if (param.Name != LayerCountParamName && !string.IsNullOrEmpty(param.Name))
                    throw new Exception($"Parameter {param.Name} isn't valid.");

                if (!int.TryParse(param.Value, out result.LayerCount))
                    throw new Exception($"Parameter {LayerCountParamName} must be an integer value.");

                ++index;
            }
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

            var index = 0;
            foreach (var param in attribute.Parameters)
            {
                if ((index == 0 && string.IsNullOrEmpty(param.Name)) || param.Name == IndexParamName)
                    int.TryParse(param.Value, out result.Index);
                else if (param.Name == UniformNameParamName)
                    result.UniformName = param.Value;
                else if (param.Name == DisplayNameParamName)
                    result.DisplayName = param.Value;
                else if (param.Name == TextureNameParamName)
                    result.TextureName = param.Value;
                else if (param.Name == TextureTypeParamName)
                    Enum.TryParse(param.Value, out result.TextureType);
                ++index;
            }

            //var paramDataList = new List<AttributeParsing.ParamData>
            //{
            //    new AttributeParsing.ParamData { ParamName = IndexParamName, parseDelegate = (param, data) => { result.Index = int.Parse(param.Value); } },
            //    new AttributeParsing.ParamData { ParamName = UniformNameParamName, parseDelegate = (param, data) => { result.UniformName = param.Value; } },
            //    new AttributeParsing.ParamData{ ParamName = DisplayNameParamName, parseDelegate = (param, data) => { result.DisplayName = param.Value; } },
            //    new AttributeParsing.ParamData{ ParamName = TextureNameParamName, parseDelegate = (param, data) => { result.TextureName = param.Value; } },
            //    new AttributeParsing.ParamData{ ParamName = TextureTypeParamName, parseDelegate = (param, data) => { Enum.TryParse(param.Value, out result.TextureType); } },
            //};
            //AttributeParsing.Parse(attribute, paramDataList);

            return result;
        }
    }
}
