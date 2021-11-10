using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderFoundry
{
    static internal class PropertyUtils
    {
        static internal string GetHlslType(HLSLProperty prop)
        {
            if (!prop.IsObjectType())
                return prop.GetValueTypeString();
            else if (prop.type == HLSLType._Texture2D)
                return "Texture2D";
            else if (prop.type == HLSLType._Texture3D)
                return "Texture3D";
            else if (prop.type == HLSLType._TextureCube)
                return "TextureCube";
            else if (prop.type == HLSLType._Texture2DArray)
                return "Texture2DArray";
            else if (prop.type == HLSLType._SamplerState)
                return "SamplerState";
            return null;
        }

        static internal void AddSimpleAttribute(ShaderContainer container, StructField.Builder builder, string attributeName)
        {
            builder.AddAttribute(new ShaderAttribute.Builder(container, attributeName).Build());
        }

        static internal void AddPropertyDeclaration(ShaderContainer container, StructField.Builder builder)
        {
            AddSimpleAttribute(container, builder, "Property");
        }

        static internal void AddMaterialProperty(ShaderContainer container, StructField.Builder builder, AbstractShaderProperty shaderProp, string propertyType, string attributes = "")
        {
            builder.AddAttribute(new ShaderAttribute.Builder(container, "MaterialProperty").Param($"{attributes}{shaderProp.referenceName}(\"{shaderProp.displayName}\", {propertyType})").Build());
        }

        static internal void AddMaterialPropertyDefault(ShaderContainer container, StructField.Builder builder, string defaultValue)
        {
            builder.AddAttribute(new ShaderAttribute.Builder(container, "MaterialPropertyDefault").Param(defaultValue).Build());
        }

        static internal void AddFullUniformDeclaration(ShaderContainer container, StructField.Builder builder, string name, string declaration)
        {
            var attBuilder = new ShaderAttribute.Builder(container, "UniformDeclaration");
            if (!string.IsNullOrEmpty(name))
                attBuilder.Param("name", name);
            if (!string.IsNullOrEmpty(declaration))
                attBuilder.Param("declaration", declaration);
            builder.AddAttribute(attBuilder.Build());
        }

        static internal void AddUniformDeclaration(ShaderContainer container, StructField.Builder builder, string type)
        {
            AddFullUniformDeclaration(container, builder, "#", $"{type} #");
        }

        static internal void DeclareBasicUniform(ShaderContainer container, StructField.Builder builder, HLSLProperty prop)
        {
            if (!prop.IsObjectType())
                AddFullUniformDeclaration(container, builder, "#", null);
            else if (prop.type == HLSLType._Texture2D)
                AddFullUniformDeclaration(container, builder, "#", "TEXTURE2D(#)");
            else if (prop.type == HLSLType._Texture3D)
                AddFullUniformDeclaration(container, builder, "#", "TEXTURE3D(#)");
            else if (prop.type == HLSLType._TextureCube)
                AddFullUniformDeclaration(container, builder, "#", "TEXTURECUBE(#)");
            else if (prop.type == HLSLType._Texture2DArray)
                AddFullUniformDeclaration(container, builder, "#", "TEXTURE2D_ARRAY(#)");
            else if (prop.type == HLSLType._SamplerState)
                AddFullUniformDeclaration(container, builder, "#", "SAMPLER(#)");
        }

        static internal void BuildProp(ShaderContainer container, AbstractShaderProperty shaderProp, HLSLProperty hlslProp, HashSet<string> declaredHlslProps, List<StructField> inputs)
        {
            if (declaredHlslProps.Contains(hlslProp.name))
                return;
            declaredHlslProps.Add(hlslProp.name);

            string typeName = GetHlslType(hlslProp);
            var fieldType = container.GetType(typeName);
            bool isMaterialProperty = hlslProp.name == shaderProp.referenceName;
            var declaration = hlslProp.declaration;
            bool isExposed = true;
            if (isMaterialProperty)
            {
                if (shaderProp.overrideHLSLDeclaration)
                    declaration = shaderProp.hlslDeclarationOverride;
                isExposed = shaderProp.isExposed;
            }

            var varBuilder = new StructField.Builder(container, hlslProp.name, fieldType);

            if (declaration == HLSLDeclaration.DoNotDeclare)
            {

            }
            else if (declaration == HLSLDeclaration.Global)
                varBuilder.AddAttribute(new ShaderAttribute.Builder(container, CommonShaderAttributes.Global).Build());
            else if (declaration == HLSLDeclaration.UnityPerMaterial)
                varBuilder.AddAttribute(new ShaderAttribute.Builder(container, CommonShaderAttributes.PerMaterial).Build());
            else if (declaration == HLSLDeclaration.HybridPerInstance)
                varBuilder.AddAttribute(new ShaderAttribute.Builder(container, CommonShaderAttributes.Hybrid).Build());
            AddPropertyDeclaration(container, varBuilder);

            if(isMaterialProperty)
            {
                switch (shaderProp)
                {
                    case BooleanShaderProperty boolProp:
                        {
                            AddUniformDeclaration(container, varBuilder, "float");
                            if (isExposed)
                            {
                                string defaultValue = boolProp.value == false ? "0" : "1";
                                AddMaterialProperty(container, varBuilder, shaderProp, "Float");
                                varBuilder.AddAttribute(new ShaderAttribute.Builder(container, "MaterialProperty").Param($"[ToggleUI]{shaderProp.referenceName}(\"{shaderProp.displayName}\", Float)").Build());
                                AddMaterialPropertyDefault(container, varBuilder, defaultValue);
                            }
                            break;
                        }
                    case Vector1ShaderProperty vec1Prop:
                        {
                            AddUniformDeclaration(container, varBuilder, "float");
                            if (isExposed)
                            {
                                string defaultValue = $"{vec1Prop.value}";
                                AddMaterialProperty(container, varBuilder, shaderProp, "Float");
                                AddMaterialPropertyDefault(container, varBuilder, defaultValue);
                            }
                            break;
                        }
                    case Vector2ShaderProperty vec2Prop:
                        {
                            AddUniformDeclaration(container, varBuilder, "float2");
                            if (isExposed)
                            {
                                string defaultValue = $"({vec2Prop.value.x}, {vec2Prop.value.y}, 0, 0)";
                                AddMaterialProperty(container, varBuilder, shaderProp, "Vector");
                                AddMaterialPropertyDefault(container, varBuilder, defaultValue);
                            }
                            break;
                        }
                    case Vector3ShaderProperty vec3Prop:
                        {
                            AddUniformDeclaration(container, varBuilder, "float3");
                            if (isExposed)
                            {
                                string defaultValue = $"({vec3Prop.value.x}, {vec3Prop.value.y}, {vec3Prop.value.z}, 0)";
                                AddMaterialProperty(container, varBuilder, shaderProp, "Vector");
                                AddMaterialPropertyDefault(container, varBuilder, defaultValue);
                            }
                            break;
                        }
                    case Vector4ShaderProperty vec4Prop:
                        {
                            AddUniformDeclaration(container, varBuilder, "float4");
                            if(isExposed)
                            {
                                string defaultValue = $"({vec4Prop.value.x}, {vec4Prop.value.y}, {vec4Prop.value.z}, {vec4Prop.value.w})";
                                AddMaterialProperty(container, varBuilder, shaderProp, "Vector");
                                AddMaterialPropertyDefault(container, varBuilder, defaultValue);
                            }
                            break;
                        }
                    case ColorShaderProperty colorProp:
                        {
                            AddUniformDeclaration(container, varBuilder, "float4");
                            if (isExposed)
                            {
                                AddMaterialProperty(container, varBuilder, shaderProp, "Color", colorProp.hdrTagString);
                                string defaultValue = $"({colorProp.value.r}, {colorProp.value.g}, {colorProp.value.b}, {colorProp.value.a})";
                                AddMaterialPropertyDefault(container, varBuilder, defaultValue);
                            }
                            break;
                        }
                    case Texture2DShaderProperty tex2DProp:
                        {
                            DeclareBasicUniform(container, varBuilder, hlslProp);
                            var normalTagString = (tex2DProp.defaultType == Texture2DShaderProperty.DefaultType.NormalMap) ? "[Normal]" : "";
                            string attributesString = $"{tex2DProp.hideTagString}{tex2DProp.modifiableTagString}{normalTagString}{tex2DProp.mainTextureString}{tex2DProp.useSTString}";
                            AddMaterialProperty(container, varBuilder, shaderProp, "2D", attributesString);
                            string defaultValue = $"\"{Texture2DShaderProperty.ToShaderLabString(tex2DProp.defaultType)}\" {{}}";
                            AddMaterialPropertyDefault(container, varBuilder, defaultValue);
                            break;
                        }
                    case Texture3DShaderProperty tex3DProp:
                        DeclareBasicUniform(container, varBuilder, hlslProp);
                        AddMaterialProperty(container, varBuilder, shaderProp, "3D");
                        AddMaterialPropertyDefault(container, varBuilder, "\"\" {}");
                        break;
                    case CubemapShaderProperty cubeProp:
                        DeclareBasicUniform(container, varBuilder, hlslProp);
                        AddMaterialProperty(container, varBuilder, shaderProp, "CUBE");
                        AddMaterialPropertyDefault(container, varBuilder, "\"\" {}");
                        break;
                    case Texture2DArrayShaderProperty tex2DArrayProp:
                        DeclareBasicUniform(container, varBuilder, hlslProp);
                        AddMaterialProperty(container, varBuilder, shaderProp, "2DArray");
                        AddMaterialPropertyDefault(container, varBuilder, "\"\" {}");
                        break;
                }
            }

            inputs.Add(varBuilder.Build());
        }

        static internal void BuildProperties(ShaderContainer container, Block.Builder blockBuilder, List<StructField> inputs, ShaderGraph.PropertyCollector propCollector, ConcretePrecision defaultConcretePrecision)
        {
            var declaredHlslProps = new HashSet<string>();

            for(var i = 0; i < propCollector.propertyCount; ++i)
            {
                var shaderProp = propCollector.GetProperty(i);
                shaderProp.ForeachHLSLProperty((HLSLProperty hlslProp) =>
                    {
                        BuildProp(container, shaderProp, hlslProp, declaredHlslProps, inputs);
                    });
            }
        }
    }
}
