using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    class PropertyDeclarations
    {
        static Dictionary<string, Func<FieldPropertyContext, FieldPropertyData, bool>> TypeHandlers = new Dictionary<string, Func<FieldPropertyContext, FieldPropertyData, bool>>
        {
            {"bool", HandleBoolType },
            {"int", HandleIntegerType },
            {"float", HandleFloatType },
            {"float2", HandleFloat2Type },
            {"float3", HandleFloat3Type },
            {"float4", HandleFloat4Type },
            {"UnityTexture2D", HandleUnityTexture2D },
            {"UnityTexture2DArray", HandleUnityTexture2DArray },
            {"UnityTextureCube", HandleUnityTextureCube },
            {"UnityTexture3D", HandleUnityTexture3D },
            {"VTPropertyWithTextureType", HandleVirtualTexture },
        };

        static Dictionary<string, Action<ShaderAttribute, FieldPropertyContext>> AttributeHandlers = new Dictionary<string, Action<ShaderAttribute, FieldPropertyContext>>
        {
            {"Gamma", DefaultAttributeProcessor},
            {"HDR", DefaultAttributeProcessor},
            {"HideInInspector", DefaultAttributeProcessor},
            {"MainTexture", DefaultAttributeProcessor},
            {"MainColor", DefaultAttributeProcessor},
            {"NoScaleOffset", DefaultAttributeProcessor},
            {"Normal", DefaultAttributeProcessor},
            {"PerRendererData", DefaultAttributeProcessor},
            {"KeywordEnum", DefaultAttributeProcessor},
            {"Toggle", DefaultAttributeProcessor},
            {"ToggleOff", DefaultAttributeProcessor},
            {"Enum", DefaultAttributeProcessor},
            {"PowerSlider", DefaultAttributeProcessor},
            {"IntRange", DefaultAttributeProcessor},
            {"Space", DefaultAttributeProcessor},
            {"Header", DefaultAttributeProcessor},
            {"VTexLayers", DefaultAttributeProcessor },
        };

        internal static FieldPropertyData Extract(ShaderType type, string name, IEnumerable<ShaderAttribute> attributes)
        {
            var propertyAttribute = PropertyAttribute.FindFirst(attributes);
            if (propertyAttribute == null)
                return null;

            if (!TypeHandlers.TryGetValue(type.Name, out var handler))
                throw new Exception($"Type {type.Name} is an invalid property type.");

            var container = type.Container;
            var context = new FieldPropertyContext(container, type, name, attributes, propertyAttribute);
            ProcessAttributes(context, attributes);

            var result = new FieldPropertyData();
            handler(context, result);
            return result;
        }

        static void ProcessAttributes(FieldPropertyContext context, IEnumerable<ShaderAttribute> attributes)
        {
            if (attributes == null)
                return;
            foreach (var attribute in attributes)
            {
                if (AttributeHandlers.TryGetValue(attribute.Name, out var attributeHandler))
                    attributeHandler(attribute, context);
            }
        }

        static void DefaultAttributeProcessor(ShaderAttribute attribute, FieldPropertyContext context)
        {
            if (!context.ProcessedAttributes.ContainsKey(attribute.Name))
                context.ProcessedAttributes.Add(attribute.Name, attribute);
        }

        static bool HandleFloatType(FieldPropertyContext context, FieldPropertyData result)
        {
            var displayType = GetDisplayType(context, "Float", new HashSet<string> { "Integer", "Int", "Range" });
            BuildSimpleProperty(context, displayType, "0", result);
            return true;
        }

        static bool HandleFloat2Type(FieldPropertyContext context, FieldPropertyData result)
        {
            var displayType = GetDisplayType(context, "Vector", new HashSet<string> { "Color" });
            BuildSimpleProperty(context, displayType, "(0, 0, 0, 0)", result);
            return true;
        }

        static bool HandleFloat3Type(FieldPropertyContext context, FieldPropertyData result)
        {
            var displayType = GetDisplayType(context, "Vector", new HashSet<string> { "Color" });
            BuildSimpleProperty(context, displayType, "(0, 0, 0, 0)", result);
            return true;
        }

        static bool HandleFloat4Type(FieldPropertyContext context, FieldPropertyData result)
        {
            var displayType = GetDisplayType(context, "Vector", new HashSet<string> { "Color" });
            BuildSimpleProperty(context, displayType, "(0, 0, 0, 0)", result);
            return true;
        }

        static bool HandleIntegerType(FieldPropertyContext context, FieldPropertyData result)
        {
            // If this is actually an enum keyword
            var enumKeywordAttribute = EnumKeywordAttribute.FindFirst(context.Attributes);
            if (enumKeywordAttribute != null)
                return HandleEnumKeywordType(context, enumKeywordAttribute, result);

            var displayType = GetDisplayType(context, "Integer", new HashSet<string> { "Int", "Float", "Range" });
            BuildSimpleProperty(context, displayType, "0", result);
            return true;
        }

        static bool HandleBoolType(FieldPropertyContext context, FieldPropertyData result)
        {
            // If this is actually a bool keyword
            var boolKeywordAttribute = BoolKeywordAttribute.FindFirst(context.Attributes);
            if (boolKeywordAttribute != null)
                return HandleBoolKeywordType(context, boolKeywordAttribute, result);

            var displayType = GetDisplayType(context, "Integer", new HashSet<string> { "Int", "Float" });
            context.ExtraAttributes = new List<string> { "[Toggle]" };
            BuildSimpleProperty(context, displayType, "0", result);
            return true;
        }

        static bool HandleBoolKeywordType(FieldPropertyContext context, BoolKeywordAttribute boolKeywordAttribute, FieldPropertyData result)
        {
            var displayType = GetDisplayType(context, "Integer", new HashSet<string> { "Int", "Float" });
            var uniformName = context.UniformName;
            var keywordName = boolKeywordAttribute.GetKeywordName(uniformName);
            bool usePreProcessor = boolKeywordAttribute.KeywordMode != KeywordMode.dynamic_branch;

            var ifString = "if";
            var elseString = "else";
            if (usePreProcessor)
            {
                ifString = "#if";
                elseString = "#else";
            }

            context.ExtraAttributes = new List<string> { $"[Toggle({keywordName})]" };
            MaterialPropertyDeclarationData.BuildSimple(context, displayType, "0", result);

            result.UniformReadingData = new UniformReadingData
            {
                ReadUniformCallback = (builder, owningVariable) =>
                {
                    string variableDeclaration = owningVariable.GetDeclarationString();
                    var keywordName = boolKeywordAttribute.GetKeywordName(uniformName);
                    builder.AddLine($"{ifString} ({keywordName})");
                    builder.Indent();
                    builder.AddLine($"{variableDeclaration} = 1;");
                    builder.Deindent();
                    builder.AddLine(elseString);
                    builder.Indent();
                    builder.AddLine($"{variableDeclaration} = 0;");
                    builder.Deindent();

                    if (usePreProcessor)
                        builder.AddLine("#endif");
                }
            };
            return true;
        }

        static bool HandleEnumKeywordType(FieldPropertyContext context, EnumKeywordAttribute enumKeywordAttribute, FieldPropertyData result)
        {
            var fieldName = context.FieldName;
            var uniformName = context.UniformName;
            var displayType = GetDisplayType(context, "Integer", new HashSet<string> { "Int", "Float", "Range" });

            bool usePreProcessor = enumKeywordAttribute.KeywordMode != KeywordMode.dynamic_branch;
            var ifString = "if";
            var elseIfString = "else if";
            var elseString = "else";
            if (usePreProcessor)
            {
                ifString = "#if";
                elseIfString = "#elif";
                elseString = "#else";
            }

            context.ExtraAttributes = new List<string> { enumKeywordAttribute.BuildPropertyAttributeString() };
            MaterialPropertyDeclarationData.BuildSimple(context, displayType, "0", result);

            result.UniformReadingData = new UniformReadingData
            {
                ReadUniformCallback = (builder, owningVariable) =>
                {
                    string variableDeclaration = owningVariable.GetDeclarationString();
                    for (var index = 0; index < enumKeywordAttribute.EnumCount; ++index)
                    {
                        var pair = enumKeywordAttribute.GetEnum(index);
                        var keywordName = enumKeywordAttribute.GetKeywordName(uniformName, pair.Name);

                        if (index == 0)
                            builder.AddLine($"{ifString} ({keywordName})");
                        else
                            builder.AddLine($"{elseIfString} ({keywordName})");
                        builder.Indent();
                        builder.AddLine($"{variableDeclaration} = {pair.Value};");
                        builder.Deindent();
                    }

                    builder.AddLine(elseString);
                    builder.Indent();
                    builder.AddLine($"{variableDeclaration} = {enumKeywordAttribute.EnumCount};");
                    builder.Deindent();
                    if (usePreProcessor)
                        builder.AddLine("#endif");
                }
            };
            return true;
        }

        static bool HandleUnityTexture2D(FieldPropertyContext context, FieldPropertyData result)
        {
            var hasScaleOffset = context.Attributes.FindFirst("ScaleOffset").IsValid;
            var accessorFnName = "UnityBuildTexture2DStruct";
            if (!hasScaleOffset)
                accessorFnName = "UnityBuildTexture2DStructNoScale";
            var buildingData = new TextureBuildingData
            {
                TextureUniformType = context.Container._Texture2D,
                MaterialPropertyType = "2D",
                AssignmentFunctionName = accessorFnName,
                HasTexelSize = true,
                HasScaleOffset = hasScaleOffset,
            };
            return HandleUnityTextureShared(context, buildingData, result);
        }

        static bool HandleUnityTexture2DArray(FieldPropertyContext context, FieldPropertyData result)
        {
            var buildingData = new TextureBuildingData
            {
                TextureUniformType = context.Container._Texture2DArray,
                MaterialPropertyType = "2DArray",
                AssignmentFunctionName = "UnityBuildTexture2DArrayStruct",
            };
            return HandleUnityTextureShared(context, buildingData, result);
        }

        static bool HandleUnityTextureCube(FieldPropertyContext context, FieldPropertyData result)
        {
            var buildingData = new TextureBuildingData
            {
                TextureUniformType = context.Container._TextureCube,
                MaterialPropertyType = "CUBE",
                AssignmentFunctionName = "UnityBuildTextureCubeStruct",
            };
            return HandleUnityTextureShared(context, buildingData, result);
        }

        static bool HandleUnityTexture3D(FieldPropertyContext context, FieldPropertyData result)
        {
            var buildingData = new TextureBuildingData
            {
                TextureUniformType = context.Container._Texture3D,
                MaterialPropertyType = "3D",
                AssignmentFunctionName = "UnityBuildTexture3DStruct",
            };
            return HandleUnityTextureShared(context, buildingData, result);
        }

        internal class TextureBuildingData
        {
            internal ShaderType TextureUniformType;
            internal string MaterialPropertyType;
            internal string MaterialPropertyDefaultValue = "\"\" {}";
            internal string AssignmentFunctionName;
            internal bool HasScaleOffset = false;
            internal bool HasTexelSize = false;
        }

        static bool HandleUnityTextureShared(FieldPropertyContext context, TextureBuildingData buildingData, FieldPropertyData result)
        {
            var container = context.Container;
            var uniformName = context.UniformName;
            result.UniformReadingData = new UniformReadingData { Rhs = $"{buildingData.AssignmentFunctionName}({uniformName})" };
            result.UniformDeclarations.Add(new UniformDeclarationData { dataSource = context.DataSource, Name = $"{uniformName}", Type = buildingData.TextureUniformType });
            result.UniformDeclarations.Add(new UniformDeclarationData { dataSource = context.DataSource, Name = $"sampler{uniformName}", Type = container._SamplerState });
            if (buildingData.HasTexelSize)
                result.UniformDeclarations.Add(new UniformDeclarationData { dataSource = context.DataSource, Name = $"{uniformName}_TexelSize", Type = container._float4 });
            if (buildingData.HasScaleOffset)
                result.UniformDeclarations.Add(new UniformDeclarationData { dataSource = context.DataSource, Name = $"{uniformName}_ST", Type = container._float4 });
            MaterialPropertyDeclarationData.BuildSimple(context, buildingData.MaterialPropertyType, buildingData.MaterialPropertyDefaultValue, result);
            return true;
        }

        static bool HandleVirtualTexture(FieldPropertyContext context, FieldPropertyData result)
        {
            var attributes = context.Attributes;
            var virtualTextureAttribute = VirtualTextureAttribute.FindFirst(attributes);
            if (virtualTextureAttribute == null)
                return false;

            var container = context.Container;
            var uniformName = context.UniformName;
            var displayName = context.DisplayName;

            // Process all layers into a list. If a layer is not specified then it is left as null.
            var layerCount = virtualTextureAttribute.LayerCount;
            var layerInfo = new List<VirtualTextureLayerAttribute>(layerCount);
            for (var i = 0; i < layerCount; ++i)
                layerInfo.Add(null);
            foreach (var attribute in attributes)
            {
                var layerAttribute = VirtualTextureLayerAttribute.TryParse(attribute);
                if (layerAttribute != null && layerAttribute.Index < layerCount)
                    layerInfo[layerAttribute.Index] = layerAttribute;
            }

            // The stack is built by calling a macro with each layer name
            var declareStackBuilder = new ShaderBuilder();
            declareStackBuilder.Add("DECLARE_STACK");
            if (layerCount != 1)
                declareStackBuilder.Add(layerCount.ToString());
            declareStackBuilder.Add($"({uniformName}");
            // To read the uniform, we have to invoke AddTextureType with each layer's texture type.
            var readUniformBuilder = new ShaderBuilder();
            readUniformBuilder.Add($"AddTextureType(BuildVTProperties_{uniformName}()");

            // First, virtual textures always declare the "root" uniform via a macro
            result.UniformDeclarations.Add(new UniformDeclarationData { DeclarationOverride = $"DECLARE_STACK_CB({uniformName})", dataSource = UniformDataSource.PerMaterial });
            // Iterate through each layer, building up the material declaration, uniform declarations, and uniform reading assignments.
            for (var layer = 0; layer < layerCount; ++layer)
            {
                var layerUniformName = layerInfo[layer]?.UniformName ?? $"{uniformName}_Layer{layer}";
                var layerDisplayName = layerInfo[layer]?.DisplayName ?? $"{displayName}_Layer{layer}";
                var layerTextureName = layerUniformName;
                var layerSamplerName = $"sampler{layerTextureName}";
                var layerTextureType = layerInfo[layer]?.TextureType ?? VirtualTextureLayerAttribute.LayerTextureType.Default;
                var layerDefaultName = layerInfo[layer]?.TextureName ?? "\"\" {}";

                // Declare a property and two uniforms (texture + sampler) per layer
                result.MaterialPropertyDeclarations.Add(new MaterialPropertyDeclarationData { UniformName = layerUniformName, DisplayName = layerDisplayName, DisplayType = "2D", DefaultValueExpression = layerDefaultName });
                result.UniformDeclarations.Add(new UniformDeclarationData { dataSource = UniformDataSource.Global, Name = layerTextureName, Type = container._Texture2D });
                result.UniformDeclarations.Add(new UniformDeclarationData { dataSource = UniformDataSource.Global, Name = layerSamplerName, Type = container._SamplerState });

                // The stack builder is used to declare the stack uniform
                declareStackBuilder.Add(", ", layerUniformName);
                // Reading the uniform requires the texture type for each layer as an argument
                if (layerTextureType == VirtualTextureLayerAttribute.LayerTextureType.NormalTangentSpace)
                    readUniformBuilder.Add($", TEXTURETYPE_NORMALTANGENTSPACE");
                else if (layerTextureType == VirtualTextureLayerAttribute.LayerTextureType.NormalObjectSpace)
                    readUniformBuilder.Add($", TEXTURETYPE_NORMALOBJECTSPACE");
                else
                    readUniformBuilder.Add($", TEXTURETYPE_DEFAULT");
            }
            declareStackBuilder.Add(")");
            readUniformBuilder.Add(");");
            result.UniformDeclarations.Add(new UniformDeclarationData { DeclarationOverride = declareStackBuilder.ToString(), dataSource = UniformDataSource.Global });
            result.UniformReadingData = new UniformReadingData { Rhs = readUniformBuilder.ToString() };

            return true;
        }

        static void BuildSimpleProperty(FieldPropertyContext context, string displayType, string defaultValue, FieldPropertyData resultProperty)
        {
            UniformReadingData.BuildSimple(context, resultProperty);
            UniformDeclarationData.BuildSimple(context, resultProperty);
            MaterialPropertyDeclarationData.BuildSimple(context, displayType, defaultValue, resultProperty);
        }

        static string GetDisplayType(FieldPropertyContext context, string displayType, HashSet<string> allowedDisplayTypeOverrides)
        {
            foreach (var attribute in context.Attributes)
            {
                if (allowedDisplayTypeOverrides.Contains(attribute.Name))
                    return attribute.Name;
            }
            return displayType;
        }
    }
}
