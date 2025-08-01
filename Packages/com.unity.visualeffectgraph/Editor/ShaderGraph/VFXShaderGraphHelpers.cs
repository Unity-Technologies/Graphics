using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.VFX
{
    interface IVFXShaderGraphOutput
    {
        ShaderGraphVfxAsset GetShaderGraph();
    }

    static class VFXShaderGraphHelpers
    {
        public static string GetMissingShaderGraphErrorMessage(ShaderGraphVfxAsset shader)
        {
            var missingShaderPath = AssetDatabase.GetAssetPath(shader.GetEntityId());
            if (!string.IsNullOrEmpty(missingShaderPath))
            {
                return $" cannot be compiled because a Shader Graph asset located here '{missingShaderPath}' is missing.";
            }

            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(shader, out var guid, out var localID);
            return $" cannot be compiled because a Shader Graph with GUID '{guid}' is missing.\nYou might find the missing file by searching on your disk this guid in .meta files.";
        }

        private static Type GetPropertyType(AbstractShaderProperty property)
        {
            switch (property.propertyType)
            {
                case PropertyType.Color:
                    return typeof(Color);
                case PropertyType.Texture2D:
                    return typeof(Texture2D);
                case PropertyType.Texture2DArray:
                    return typeof(Texture2DArray);
                case PropertyType.Texture3D:
                    return typeof(Texture3D);
                case PropertyType.Cubemap:
                    return typeof(Cubemap);
                case PropertyType.Gradient:
                    return null;
                case PropertyType.Boolean:
                    return typeof(bool);
                case PropertyType.Float:
                    return typeof(float);
                case PropertyType.Vector2:
                    return typeof(Vector2);
                case PropertyType.Vector3:
                    return typeof(Vector3);
                case PropertyType.Vector4:
                    return typeof(Vector4);
                case PropertyType.Matrix2:
                    return null;
                case PropertyType.Matrix3:
                    return null;
                case PropertyType.Matrix4:
                    return typeof(Matrix4x4);
                case PropertyType.SamplerState:
                default:
                    return null;
            }
        }

        private static object GetPropertyValue(AbstractShaderProperty property)
        {
            switch (property.propertyType)
            {
                case PropertyType.Texture2D:
                    return ((Texture2DShaderProperty) property).value.texture;
                case PropertyType.Texture3D:
                    return ((Texture3DShaderProperty) property).value.texture;
                case PropertyType.Cubemap:
                    return ((CubemapShaderProperty) property).value.cubemap;
                case PropertyType.Texture2DArray:
                    return ((Texture2DArrayShaderProperty) property).value.textureArray;
                default:
                {
                    var type = GetPropertyType(property);
                    PropertyInfo info = property.GetType().GetProperty("value",
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    return VFXConverter.ConvertTo(info?.GetValue(property), type);
                }
            }
        }

        static VFXExpression ConstantFromValue(object value)
        {
            if (value == null)
                throw new NullReferenceException("ConstantFromValue NRE");

            var type = value.GetType();
            if (type == typeof(Color)) return VFXValue.Constant<Vector4>((Color)(value));
            if (type == typeof(bool)) return VFXValue.Constant((bool)value);
            if (type == typeof(float)) return VFXValue.Constant((float)value);
            if (type == typeof(Vector2)) return VFXValue.Constant((Vector2)value);
            if (type == typeof(Vector3)) return VFXValue.Constant((Vector3)value);
            if (type == typeof(Vector4)) return VFXValue.Constant((Vector4)value);

            //This function is only used for constant which can be exposed and aren't textures (see VFXSGInputs usage)
            throw new InvalidOperationException("ConstantFromValue missing support for: " + type);
        }

        public struct Property
        {
            public VFXPropertyWithValue property;
            public bool multiCompile;
            public string[] keywordsMapping;
        }

        public static IEnumerable<Property> GetProperties(ShaderGraphVfxAsset shaderGraph)
        {
            foreach (var property in shaderGraph.properties)
            {
                if (property is AbstractShaderProperty shaderProperty)
                {
                    if (shaderProperty.hidden || !shaderProperty.isExposed)
                        continue;

                    var type = GetPropertyType(shaderProperty);
                    if (type == null)
                        continue;

                    var current = new Property()
                    {
                        keywordsMapping = null
                    };

                    if (shaderProperty.propertyType == PropertyType.Float)
                    {
                        if (property is Vector1ShaderProperty prop)
                        {
                            if (prop.floatType == FloatType.Slider)
                                current.property = new VFXPropertyWithValue(
                                    new VFXProperty(type, property.referenceName,
                                        new RangeAttribute(prop.rangeValues.x, prop.rangeValues.y)),
                                    GetPropertyValue(shaderProperty));
                            else if (prop.floatType == FloatType.Integer)
                                current.property = new VFXPropertyWithValue(
                                    new VFXProperty(typeof(int), property.referenceName),
                                    VFXConverter.ConvertTo(GetPropertyValue(shaderProperty), typeof(int)));
                            else
                                current.property = new VFXPropertyWithValue(new VFXProperty(type, property.referenceName),
                                    GetPropertyValue(shaderProperty));
                        }
                        else
                        {
                            //it could be a diffusion profile in HDRP.
                            continue;
                        }
                    }
                    else
                        current.property = new VFXPropertyWithValue(new VFXProperty(type, property.referenceName),
                            GetPropertyValue(shaderProperty));

                    yield return current;

                }
                else if (property is ShaderGraph.ShaderKeyword shaderKeyword)
                {
                    if (!shaderKeyword.isExposed)
                        continue;

                    if (shaderKeyword.keywordType == KeywordType.Boolean)
                    {
                        yield return new Property()
                        {
                            property = new VFXPropertyWithValue(
                                new VFXProperty(typeof(bool), shaderKeyword.displayName), shaderKeyword.value != 0),
                            multiCompile = shaderKeyword.keywordDefinition == KeywordDefinition.MultiCompile,
                            keywordsMapping = new[] { shaderKeyword.referenceName }
                        };
                    }
                    else if (shaderKeyword.keywordType == KeywordType.Enum)
                    {
                        var keywordsMapping = new string[shaderKeyword.entries.Count];
                        var enumNames = new string[shaderKeyword.entries.Count];
                        for (int index = 0; index < shaderKeyword.entries.Count; ++index)
                        {
                            keywordsMapping[index] = shaderKeyword.referenceName + "_" + shaderKeyword.entries[index].referenceName;
                            enumNames[index] = shaderKeyword.entries[index].displayName;
                        }

                        yield return new Property
                        {
                            property = new VFXPropertyWithValue(
                                new VFXProperty(typeof(uint), shaderKeyword.displayName, new VFXPropertyAttributes(new EnumAttribute(enumNames))), (uint)shaderKeyword.value),
                            multiCompile = shaderKeyword.keywordDefinition == KeywordDefinition.MultiCompile,
                            keywordsMapping = keywordsMapping
                        };
                    }
                    else
                    {
                        throw new InvalidOperationException("Unsupported keyword type: " + shaderKeyword.keywordType);
                    }
                }
                else
                {
                    throw new InvalidOperationException("Unsupported property type: " + property);
                }
            }
        }

        public static IEnumerable<VFXNamedExpression> GetTextureConstant(ShaderGraphVfxAsset shaderGraph)
        {
            foreach (var tex in shaderGraph.textureInfos)
            {
                switch (tex.dimension)
                {
                    case TextureDimension.Tex2D:
                        yield return new VFXNamedExpression(
                            new VFXTexture2DValue(tex.instanceID, VFXValue.Mode.Variable), tex.name);
                        break;
                    case TextureDimension.Tex3D:
                        yield return new VFXNamedExpression(
                            new VFXTexture3DValue(tex.instanceID, VFXValue.Mode.Variable), tex.name);
                        break;
                    case TextureDimension.Cube:
                        yield return new VFXNamedExpression(
                            new VFXTextureCubeValue(tex.instanceID, VFXValue.Mode.Variable), tex.name);
                        break;
                    case TextureDimension.Tex2DArray:
                        yield return new VFXNamedExpression(
                            new VFXTexture2DArrayValue(tex.instanceID, VFXValue.Mode.Variable), tex.name);
                        break;
                    case TextureDimension.CubeArray:
                        yield return new VFXNamedExpression(
                            new VFXTextureCubeArrayValue(tex.instanceID, VFXValue.Mode.Variable), tex.name);
                        break;
                }
            }
        }

        public static bool IsTexture(PropertyType type)
        {
            switch (type)
            {
                case PropertyType.Texture2D:
                case PropertyType.Texture2DArray:
                case PropertyType.Texture3D:
                case PropertyType.Cubemap:
                    return true;
                default:
                    return false;
            }
        }

        public static ShaderGraphVfxAsset GetShaderGraph(VFXContext context)
        {
            if (context is IVFXShaderGraphOutput shaderGraphOutput)
                return shaderGraphOutput.GetShaderGraph();
            return null;
        }

        public static void GetShaderGraphParameters(ShaderGraphVfxAsset shaderGraph, out List<(string name, ShaderStageCapability shaderStage, bool exposed, VFXExpression defaultValue)> parameters)
        {
            parameters = new();
            var properties = shaderGraph.properties;
            for (var propertyIndex = 0; propertyIndex < properties.Count; ++propertyIndex)
            {
                var param = properties[propertyIndex];
                if (param is AbstractShaderProperty property
                    && !IsTexture(property.propertyType))
                {
                    var propertyStage = shaderGraph.GetPropertyStage(propertyIndex);
                    if (propertyStage != ShaderStageCapability.None)
                    {
                        VFXExpression exp = null;
                        if (!param.isExposed)
                        {
                            var value = GetPropertyValue(property);
                            exp = ConstantFromValue(value);
                        } //else, expression will be provided by VFXSlot
                        parameters.Add((param.referenceName, propertyStage, param.isExposed, exp));
                    }
                }
            }
        }
    }
}
