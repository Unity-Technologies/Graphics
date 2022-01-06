using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderFoundry
{
    class MaterialPropertyDeclarationInfo
    {
        internal List<string> Attributes;
        internal string UniformName;
        internal string DisplayName;
        internal string DisplayType;
        internal string DefaultValueExpression;

        internal void Declare(ShaderBuilder builder)
        {
            builder.Indentation();
            if (Attributes != null)
            {
                foreach (var attribute in Attributes)
                    builder.Append(attribute);
            }
            builder.Append($"{UniformName}(\"{DisplayName}\", {DisplayType}) = {DefaultValueExpression}");
            builder.NewLine();
        }
    }

    class UniformDeclarationInfo
    {
        internal ShaderType Type;
        internal string Name;
        internal HLSLDeclaration declarationType;
        internal string DeclarationOverride = null;

        internal void Declare(UniformDeclarationContext context)
        {
            if(DeclarationOverride != null)
            {
                DeclareUniform(context, DeclarationOverride, declarationType);
                return;
            }

            if (Type == Type.Container._Texture2D)
                DeclareUniform(context, $"TEXTURE2D({Name})", HLSLDeclaration.Global);
            else if (Type == Type.Container._Texture2DArray)
                DeclareUniform(context, $"TEXTURE2D_ARRAY({Name})", HLSLDeclaration.Global);
            else if (Type == Type.Container._TextureCube)
                DeclareUniform(context, $"TEXTURECUBE({Name})", HLSLDeclaration.Global);
            else if (Type == Type.Container._Texture3D)
                DeclareUniform(context, $"TEXTURE3D({Name})", HLSLDeclaration.Global);
            else if (Type == Type.Container._SamplerState)
                DeclareUniform(context, $"SAMPLER({Name})", HLSLDeclaration.Global);
            else
                DeclareUniform(context, $"{Type.Name} {Name}", declarationType);
        }

        static void DeclareUniform(UniformDeclarationContext context, string uniformDeclaration, HLSLDeclaration mode)
        {
            if (uniformDeclaration == null || mode == HLSLDeclaration.DoNotDeclare)
                return;

            var builder = context.PerMaterialBuilder;
            if (mode == HLSLDeclaration.Global)
                builder = context.GlobalBuilder;
            if (mode == HLSLDeclaration.HybridPerInstance)
            {
                builder.AppendLine("#ifdef UNITY_HYBRID_V1_INSTANCING_ENABLED");
                builder.AddLine($"{uniformDeclaration}_dummy;");
                builder.AppendLine("#else // V2");
                builder.AddLine($"{uniformDeclaration};");
                builder.AppendLine("#endif");
            }
            else
                builder.AddLine($"{uniformDeclaration};");
        }
    }

    class UniformReadingInfo
    {
        internal string Rhs;

        internal void Copy(ShaderFunction.Builder builder, VariableLinkInstance owningVariable)
        {
            string variableDeclaration = owningVariable.GetDeclarationString();
            builder.AddLine($"{variableDeclaration} = {Rhs};");
        }
    }

    class PropertyInfo
    {
        internal ShaderType Type;
        internal string FieldName;
        internal PropertyAttribute PropertyAttribute;
        internal IEnumerable<ShaderAttribute> Attributes;

        internal List<MaterialPropertyDeclarationInfo> MaterialPropertyDeclarations; 
        internal List<UniformDeclarationInfo> UniformDeclarations; 
        internal UniformReadingInfo UniformReadingInfo;

        class Context
        {
            internal ShaderContainer Container;
            internal ShaderType FieldType;
            internal string FieldName;
            internal IEnumerable<ShaderAttribute> Attributes;
            internal PropertyAttribute PropertyAttribute;
            internal Dictionary<string, ShaderAttribute> ProcessedAttributes = new Dictionary<string, ShaderAttribute>();
        }

        static Dictionary<string, Func<Context, PropertyInfo, bool>> TypeHandlers = new Dictionary<string, Func<Context, PropertyInfo, bool>>
        {
            {"bool", HandlePrimitiveType },
            {"int", HandlePrimitiveType },
            {"float", HandlePrimitiveType },
            {"float2", HandlePrimitiveType },
            {"float3", HandlePrimitiveType },
            {"float4", HandlePrimitiveType },
            {"UnityTexture2D", HandleUnityTexture2D },
            {"UnityTexture2DArray", HandleUnityTexture2DArray },
            {"UnityTextureCube", HandleUnityTextureCube },
            {"UnityTexture3D", HandleUnityTexture3D },
            {"VTPropertyWithTextureType", HandleVirtualTexture },
        };

        static Dictionary<string, Action<ShaderAttribute, Context>> AttributeHandlers = new Dictionary<string, Action<ShaderAttribute, Context>>
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

        internal static PropertyInfo Extract(ShaderType type, string name, IEnumerable<ShaderAttribute> attributes)
        {
            var propertyAttribute = PropertyAttribute.Find(attributes);
            if (propertyAttribute == null)
                return null;
            
            if (!TypeHandlers.TryGetValue(type.Name, out var handler))
                return null;

            var container = type.Container;
            var context = new Context
            {
                Container = container,
                FieldType = type,
                FieldName = name,
                Attributes = attributes,
                PropertyAttribute = propertyAttribute,
            };
            ProcessAttributes(context, attributes);

            var result = new PropertyInfo();
            result.Type = type;
            result.FieldName = name;
            result.Attributes = attributes;
            result.PropertyAttribute = propertyAttribute;
            result.UniformDeclarations = new List<UniformDeclarationInfo>();
            result.MaterialPropertyDeclarations = new List<MaterialPropertyDeclarationInfo>();
            handler(context, result);

            return result;
        }

        static void DefaultAttributeProcessor(ShaderAttribute attribute, Context context)
        {
            if (!context.ProcessedAttributes.ContainsKey(attribute.Name))
                context.ProcessedAttributes.Add(attribute.Name, attribute);
        }

        static List<string> ParseTags(string tags)
        {
            if (string.IsNullOrEmpty(tags))
                return new List<string>();

            int start = 0;
            int end = 0;
            var results = new List<string>();
            while (start < tags.Length)
            {
                start = tags.IndexOf('[', end);
                end = tags.IndexOf(']', start + 1);
                if (start == -1 || end == -1)
                    break;
                results.Add(tags.Substring(start, end - start + 1));
                start = end + 1;
            }

            return results;
        }

        static string BuildAttributeString(ShaderAttribute attribute)
        {
            var builder = new StringBuilder();
            builder.Append("[");
            builder.Append(attribute.Name);
            var paramCount = 0;
            foreach (var param in attribute.Parameters)
            {
                if (paramCount == 0)
                    builder.Append("(");
                else
                    builder.Append(", ");
                ++paramCount;
                builder.Append(param.Value);
            }
            if (paramCount != 0)
                builder.Append(")");
            builder.Append("]");
            return builder.ToString();
        }

        static void BuildBasicMaterialProperty(Context context, string displayType, string defaultValue, PropertyInfo propInfo)
        {
            var fieldName = context.FieldName;
            var propertyAttribute = context.PropertyAttribute;
            var uniformName = propertyAttribute.UniformName ?? fieldName;
            var displayName = propertyAttribute.DisplayName ?? fieldName;
            displayType = propertyAttribute.DisplayType ?? displayType;
            var attributes = ParseTags(propertyAttribute.Tags);
            foreach (var value in context.ProcessedAttributes.Values)
            {
                attributes.Add(BuildAttributeString(value));
            }
            var matProp = new MaterialPropertyDeclarationInfo
            {
                UniformName = uniformName,
                DisplayName = displayName,
                DisplayType = displayType,
                DefaultValueExpression = defaultValue,
                Attributes = attributes,
            };
            propInfo.MaterialPropertyDeclarations.Add(matProp);
        }

        static void BuildBasicUniform(Context context, PropertyInfo propInfo)
        {
            var propertyAttribute = context.PropertyAttribute;
            var uniformName = propertyAttribute.UniformName ?? context.FieldName;
            var mode = TranslateMode(propertyAttribute.Mode);
            propInfo.UniformDeclarations.Add(new UniformDeclarationInfo { declarationType = mode, Name = uniformName, Type = context.FieldType });
        }

        static void BuildBasicUniformReadingInfo(Context context, PropertyInfo propInfo)
        {
            var mode = TranslateMode(context.PropertyAttribute.Mode);
            var accessExpression = GetAccessExpression(context.FieldType, context.FieldName, mode);
            propInfo.UniformReadingInfo = new UniformReadingInfo { Rhs = accessExpression };
        }

        static void BuildBasicProperty(Context context, string displayType, string defaultValue, PropertyInfo propInfo)
        {
            BuildBasicUniformReadingInfo(context, propInfo);
            BuildBasicUniform(context, propInfo);
            BuildBasicMaterialProperty(context, displayType, defaultValue, propInfo);
        }

        static void ProcessAttributes(Context context, IEnumerable<ShaderAttribute> attributes)
        {
            if (attributes == null)
                return;
            foreach (var attribute in attributes)
            {
                if (AttributeHandlers.TryGetValue(attribute.Name, out var attributeHandler))
                    attributeHandler(attribute, context);
            }
        }

        static bool HandlePrimitiveType(Context context, PropertyInfo result)
        {
            if (context.FieldType == context.Container._float)
            {
                BuildBasicProperty(context, "Float", "1", result);
            }
            else if (context.FieldType == context.Container._float2 || context.FieldType == context.Container._float3 || context.FieldType == context.Container._float4)
            {
                BuildBasicProperty(context, "Vector", "(1, 1, 1, 1)", result);
            }
            else if (context.FieldType == context.Container._bool)
            {
                BuildBasicProperty(context, "Float", "1", result);
            }
            else if (context.FieldType == context.Container._int)
            {
                BuildBasicProperty(context, "Int", "1", result);
            }
            else
                return false;
            return true;
        }

        static bool HandleUnityTexture2D(Context context, PropertyInfo result)
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
            return HandleUnityTextureShared(context, result, buildingData);
        }

        static bool HandleUnityTexture2DArray(Context context, PropertyInfo result)
        {
            var buildingData = new TextureBuildingData
            {
                TextureUniformType = context.Container._Texture2DArray,
                MaterialPropertyType = "2DArray",
                AssignmentFunctionName = "UnityBuildTexture2DArrayStruct",
            };
            return HandleUnityTextureShared(context, result, buildingData);
        }

        static bool HandleUnityTextureCube(Context context, PropertyInfo result)
        {
            var buildingData = new TextureBuildingData
            {
                TextureUniformType = context.Container._TextureCube,
                MaterialPropertyType = "CUBE",
                AssignmentFunctionName = "UnityBuildTextureCubeStruct",
            };
            return HandleUnityTextureShared(context, result, buildingData);
        }

        static bool HandleUnityTexture3D(Context context, PropertyInfo result)
        {
            var buildingData = new TextureBuildingData
            {
                TextureUniformType = context.Container._Texture3D,
                MaterialPropertyType = "3D",
                AssignmentFunctionName = "UnityBuildTexture3DStruct",
            };
            return HandleUnityTextureShared(context, result, buildingData);
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

        static bool HandleUnityTextureShared(Context context, PropertyInfo result, TextureBuildingData buildingData)
        {
            var container = context.Container;
            var fieldName = context.FieldName;
            var propertyAttribute = context.PropertyAttribute;
            var uniformName = propertyAttribute.UniformName ?? fieldName;
            var mode = TranslateMode(propertyAttribute.Mode);
            result.UniformReadingInfo = new UniformReadingInfo { Rhs = $"{buildingData.AssignmentFunctionName}({uniformName})" };
            result.UniformDeclarations.Add(new UniformDeclarationInfo { declarationType = mode, Name = $"{uniformName}", Type = buildingData.TextureUniformType });
            result.UniformDeclarations.Add(new UniformDeclarationInfo { declarationType = mode, Name = $"sampler{uniformName}", Type = container._SamplerState });
            if (buildingData.HasTexelSize)
                result.UniformDeclarations.Add(new UniformDeclarationInfo { declarationType = mode, Name = $"{uniformName}_TexelSize", Type = container._float4 });
            if (buildingData.HasScaleOffset)
                result.UniformDeclarations.Add(new UniformDeclarationInfo { declarationType = mode, Name = $"{uniformName}_ST", Type = container._float4 });
            BuildBasicMaterialProperty(context, buildingData.MaterialPropertyType, buildingData.MaterialPropertyDefaultValue, result);
            return true;
        }

        static bool HandleVirtualTexture(Context context, PropertyInfo result)
        {
            var attributes = context.Attributes;
            var vTexLayers = VirtualTextureLayers.Build(attributes);
            if (vTexLayers == null)
                return false;

            var container = context.Container;
            var propertyAttribute = context.PropertyAttribute;
            var uniformName = propertyAttribute.UniformName ?? context.FieldName;
            var displayName = propertyAttribute.DisplayName ?? context.FieldName;

            // Declare a material property for each layer
            var layerCount = vTexLayers?.LayerCount ?? 1;
            for (var layer = 0; layer < layerCount; ++layer)
            {
                var layerUniformName = $"{uniformName}_Layer{layer}";
                var layerDisplayName = $"{displayName}_Layer{layer}";
                result.MaterialPropertyDeclarations.Add(new MaterialPropertyDeclarationInfo { UniformName = layerUniformName, DisplayName = layerDisplayName, DisplayType = "2D", DefaultValueExpression = "\"\" {}" });
            }

            // Add texture and sampler uniforms for each layer
            result.UniformDeclarations.Add(new UniformDeclarationInfo { DeclarationOverride = $"DECLARE_STACK_CB({uniformName})", declarationType = HLSLDeclaration.UnityPerMaterial });
            for (var i = 0; i < vTexLayers.LayerCount; ++i)
            {
                var layerTextureName = $"{uniformName}_Layer{i}";
                var layerSamplerName = $"sampler{uniformName}_Layer{i}";
                result.UniformDeclarations.Add(new UniformDeclarationInfo { declarationType = HLSLDeclaration.Global, Name = layerTextureName, Type = container._Texture2D });
                result.UniformDeclarations.Add(new UniformDeclarationInfo { declarationType = HLSLDeclaration.Global, Name = layerSamplerName, Type = container._SamplerState });
            }
            // Add the special stack uniform depending on the layer count
            if (layerCount == 1)
                result.UniformDeclarations.Add(new UniformDeclarationInfo { DeclarationOverride = $"DECLARE_STACK({uniformName}, {uniformName}_Layer0)", declarationType = HLSLDeclaration.Global });
            else if (layerCount == 2)
                result.UniformDeclarations.Add(new UniformDeclarationInfo { DeclarationOverride = $"DECLARE_STACK2({uniformName}, {uniformName}_Layer0, {uniformName}_Layer1)", declarationType = HLSLDeclaration.Global });
            else if (layerCount == 3)
                result.UniformDeclarations.Add(new UniformDeclarationInfo { DeclarationOverride = $"DECLARE_STACK3({uniformName}, {uniformName}_Layer0, {uniformName}_Layer1, {uniformName}_Layer2)", declarationType = HLSLDeclaration.Global });
            else if (layerCount == 4)
                result.UniformDeclarations.Add(new UniformDeclarationInfo { DeclarationOverride = $"DECLARE_STACK4({uniformName}, {uniformName}_Layer0, {uniformName}_Layer1, {uniformName}_Layer2, {uniformName}_Layer3)", declarationType = HLSLDeclaration.Global });

            // Build the assignment expression which is also dependent on the layer count and the types for each layer
            var builder = new StringBuilder();
            builder.Append($"AddTextureType(BuildVTProperties_{uniformName}()");
            for (var layer = 0; layer < vTexLayers.LayerCount; ++layer)
            {
                if (vTexLayers.LayerTypes[layer] == "NormalTangentSpace")
                    builder.Append($", TEXTURETYPE_NORMALTANGENTSPACE");
                else if (vTexLayers.LayerTypes[layer] == "NormalObjectSpace")
                    builder.Append($", TEXTURETYPE_NORMALOBJECTSPACE");
                else
                    builder.Append($", TEXTURETYPE_DEFAULT");
            }
            builder.Append(");");

            result.UniformReadingInfo = new UniformReadingInfo { Rhs = builder.ToString() };
            return true;
        }

        static string GetAccessExpression(ShaderType type, string name, HLSLDeclaration declarationType)
        {
            if (declarationType == HLSLDeclaration.HybridPerInstance)
                return $"UNITY_ACCESS_HYBRID_INSTANCED_PROP({name}, {type.Name})";
            else
                return name;
        }

        static HLSLDeclaration TranslateMode(UniformMode mode)
        {
            if (mode == UniformMode.Global)
                return HLSLDeclaration.Global;
            else if (mode == UniformMode.UnityPerMaterial)
                return HLSLDeclaration.UnityPerMaterial;
            else if (mode == UniformMode.DoNotDeclare)
                return HLSLDeclaration.DoNotDeclare;
            else if (mode == UniformMode.HybridPerInstance)
                return HLSLDeclaration.HybridPerInstance;
            else
                return HLSLDeclaration.UnityPerMaterial;
        }
    }
}
