using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

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

        private static T LoadAssetAtPath<T>(string assetPath) where T : UnityEngine.Object
        {
            var found = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (found == null)
            {
                //Issue specific to SG access during import, see UUM-86821
                var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                foreach (var asset in allAssets)
                {
                    if (asset is T foundType)
                    {
                        found = foundType;
                        break;
                    }
                }
            }
            return found;
        }

        public static ShaderGraphVfxAsset LoadShaderGraphAssetAtPath(string assetPath)
        {
            return LoadAssetAtPath<ShaderGraphVfxAsset>(assetPath);
        }

        public static Material LoadMaterialAtPath(string assetPath)
        {
            return LoadAssetAtPath<Material>(assetPath);
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
                    var type = AbstractShaderProperty.GetVfxType(property.propertyType);
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

        static bool IsExcludedFromSlot(AbstractShaderProperty shaderProperty)
        {
            return shaderProperty.hidden || !shaderProperty.isExposed;
        }

        static bool IsExcludedFromSlot(ShaderGraph.ShaderKeyword shaderKeyword)
        {
            return !shaderKeyword.isExposed;
        }

        public static IEnumerable<Property> GetProperties(ShaderGraphVfxAsset shaderGraph)
        {
            if (shaderGraph == null)
                yield break;

            foreach (var property in shaderGraph.properties)
            {
                if (property is AbstractShaderProperty shaderProperty)
                {
                    if (IsExcludedFromSlot(shaderProperty))
                        continue;

                    var type = AbstractShaderProperty.GetVfxType(shaderProperty.propertyType);
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
                    if (IsExcludedFromSlot(shaderKeyword))
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
                            keywordsMapping[index] = GetEnumKeywordName(shaderKeyword, index);
                            enumNames[index] = shaderKeyword.entries[index].displayName;
                        }

                        yield return new Property
                        {
                            property = new VFXPropertyWithValue(
                                new VFXProperty(typeof(uint), GetKeywordSlotName(shaderKeyword), new VFXPropertyAttributes(new EnumAttribute(enumNames))), (uint)shaderKeyword.value),
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

        static string GetKeywordSlotName(ShaderGraph.ShaderKeyword keyword) => keyword.displayName;

        static string GetEnumKeywordName(ShaderGraph.ShaderKeyword keyword, int entryIndex)
            => keyword.referenceName + "_" + keyword.entries[entryIndex].referenceName;

        public static void EnableEnumKeywordDefaults(ShaderGraphVfxAsset shaderGraph, IEnumerable<VFXSlot> outputInputSlots, Material material)
        {
            if (shaderGraph == null || material == null)
                return;

            var context = new VFXExpression.Context(VFXExpressionContextOption.ConstantFolding);

            foreach (var property in shaderGraph.properties)
            {
                // local enum multi compile or shader feature keywords only
                if (property is not ShaderGraph.ShaderKeyword keyword
                    || keyword.keywordType != KeywordType.Enum
                    || keyword.keywordScope != KeywordScope.Local
                    || (keyword.keywordDefinition != KeywordDefinition.MultiCompile
                        && keyword.keywordDefinition != KeywordDefinition.ShaderFeature))
                    continue;

                // The output's constant value if any, else the graph default.
                var index = keyword.value;
                if (keyword.isExposed
                    && TryGetConstantSlotValue(context, outputInputSlots, GetKeywordSlotName(keyword), out var chosen))
                    index = chosen;
                if (index < 0 || index >= keyword.entries.Count)
                    index = keyword.HasNoneEntry ? 1 : 0;
                if (index >= keyword.entries.Count)
                    continue;

                // Keep the backing [KeywordEnum] float in sync.
                if (material.HasProperty(keyword.referenceName))
                    material.SetFloat(keyword.referenceName, index);

                for (int i = 0; i < keyword.entries.Count; i++)
                {
                    if (keyword.entries[i].IsNoneKeyword)
                        continue;
                    var keywordName = GetEnumKeywordName(keyword, i);
                    if (i == index)
                        material.EnableKeyword(keywordName);
                    else
                        material.DisableKeyword(keywordName);
                }
            }
        }

        static bool TryGetConstantSlotValue(VFXExpression.Context context, IEnumerable<VFXSlot> inputSlots, string propertyName, out int value)
        {
            value = 0;
            if (inputSlots == null)
                return false;

            VFXSlot slot = null;
            foreach (var s in inputSlots)
            {
                if (s.property.name == propertyName)
                {
                    slot = s;
                    break;
                }
            }

            var expression = slot?.GetExpression();
            if (expression == null)
                return false;

            // A linked slot may still reduce to a constant, so fold it rather than reading the inline value.
            context.RegisterExpression(expression);
            context.Compile();
            var reduced = context.GetReduced(expression);
            if (reduced == null || !reduced.Is(VFXExpression.Flags.Constant) || reduced.valueType != VFXValueType.Uint32)
                return false;

            value = (int)reduced.Get<uint>();
            return true;
        }

        public static IEnumerable<string> GetTextureOnlyUsedInternally(ShaderGraphVfxAsset shaderGraph)
        {
            foreach (var tex in shaderGraph.textureInfos)
                yield return tex.name;


            foreach (var property in shaderGraph.properties)
            {
                if (property is AbstractShaderProperty shaderProperty)
                {
                    if (IsExcludedFromSlot(shaderProperty))
                    {
                        var type = AbstractShaderProperty.GetVfxType(shaderProperty.propertyType);
                        if (type == null)
                            continue;

                        if (typeof(Texture).IsAssignableFrom(type))
                            yield return property.referenceName;
                    }
                }
            }
        }

        static VFXExpression GetConstantTextureExpression(Texture texture, TextureDimension dimension)
        {
            var entityID = texture != null ? texture.GetEntityId() : EntityId.None;
            switch (dimension)
            {
                case TextureDimension.Tex2D: return new VFXTexture2DValue(entityID, VFXValue.Mode.Variable);
                case TextureDimension.Tex3D: return new VFXTexture3DValue(entityID, VFXValue.Mode.Variable);
                case TextureDimension.Cube: return new VFXTextureCubeValue(entityID, VFXValue.Mode.Variable);
                case TextureDimension.Tex2DArray: return new VFXTexture2DArrayValue(entityID, VFXValue.Mode.Variable);
                case TextureDimension.CubeArray:  return new VFXTextureCubeArrayValue(entityID, VFXValue.Mode.Variable);
            }
            throw new NotImplementedException("Unexpected dimension: " + dimension);
        }

        public static IEnumerable<VFXNamedExpression> GetTextureConstant(ShaderGraphVfxAsset shaderGraph)
        {
            foreach (var tex in shaderGraph.textureInfos)
                yield return new VFXNamedExpression(GetConstantTextureExpression(tex.texture, tex.dimension), tex.name);

            foreach (var property in shaderGraph.properties)
            {
                if (property is AbstractShaderProperty shaderProperty)
                {
                    if (IsExcludedFromSlot(shaderProperty))
                    {
                        var dimension = GetDimension(shaderProperty.propertyType);
                        if (dimension == TextureDimension.Unknown)
                            continue;

                        var texture = (Texture)GetPropertyValue(shaderProperty); //Can be null
                        yield return new VFXNamedExpression(GetConstantTextureExpression(texture, dimension), shaderProperty.referenceName);
                    }
                }
            }
        }

        static TextureDimension GetDimension(PropertyType type)
        {
            switch (type)
            {
                case PropertyType.Texture2D: return TextureDimension.Tex2D;
                case PropertyType.Texture2DArray: return TextureDimension.Tex2DArray;
                case PropertyType.Texture3D: return TextureDimension.Tex3D;
                case PropertyType.Cubemap: return TextureDimension.Cube;
            }

            return TextureDimension.Unknown;
        }

        public static bool IsTexture(PropertyType type)
        {
            return GetDimension(type) != TextureDimension.Unknown;
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
