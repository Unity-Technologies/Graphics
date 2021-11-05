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

        static internal void AddSimpleAttribute(ShaderContainer container, BlockVariable.Builder builder, string attributeName)
        {
            builder.AddAttribute(new ShaderAttribute.Builder(container, attributeName).Build());
        }

        static internal void AddPropertyDeclaration(ShaderContainer container, BlockVariable.Builder builder)
        {
            AddSimpleAttribute(container, builder, "Property");
        }

        static internal void AddMaterialProperty(ShaderContainer container, BlockVariable.Builder builder, AbstractShaderProperty shaderProp, string propertyType, string attributes = "")
        {
            builder.AddAttribute(new ShaderAttribute.Builder(container, "MaterialProperty").Param($"{attributes}{shaderProp.referenceName}(\"{shaderProp.displayName}\", {propertyType})").Build());
        }

        static internal void AddMaterialPropertyDefault(ShaderContainer container, BlockVariable.Builder builder, string defaultValue)
        {
            builder.AddAttribute(new ShaderAttribute.Builder(container, "MaterialPropertyDefault").Param(defaultValue).Build());
        }

        static internal void AddFullUniformDeclaration(ShaderContainer container, BlockVariable.Builder builder, string name, string declaration)
        {
            var attBuilder = new ShaderAttribute.Builder(container, "UniformDeclaration");
            if (!string.IsNullOrEmpty(name))
                attBuilder.Param("name", name);
            if (!string.IsNullOrEmpty(declaration))
                attBuilder.Param("declaration", declaration);
            builder.AddAttribute(attBuilder.Build());
        }

        static internal void AddUniformDeclaration(ShaderContainer container, BlockVariable.Builder builder, string type)
        {
            AddFullUniformDeclaration(container, builder, "#", $"{type} #");
        }

        static internal void DeclareBasicUniform(ShaderContainer container, BlockVariable.Builder builder, HLSLProperty prop)
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

        static internal BlockVariable.Builder CreateBasicProperty(ShaderContainer container, AbstractShaderProperty shaderProp, ConcretePrecision defaultConcretePrecision)
        {
            var concretizedTypeName = shaderProp.concreteShaderValueType.ToShaderString().Replace("$precision", defaultConcretePrecision.ToShaderString());
            var fieldType = container.GetType(concretizedTypeName);

            var propBuilder = new ShaderFoundry.BlockVariable.Builder(container);
            propBuilder.ReferenceName = shaderProp.referenceName;
            propBuilder.DisplayName = shaderProp.displayName;
            propBuilder.Type = fieldType;

            if (shaderProp.GetDefaultHLSLDeclaration() == HLSLDeclaration.DoNotDeclare)
            {

            }
            else if (shaderProp.GetDefaultHLSLDeclaration() == HLSLDeclaration.Global)
                propBuilder.AddAttribute(new ShaderAttribute.Builder(container, CommonShaderAttributes.Global).Build());
            else if (shaderProp.GetDefaultHLSLDeclaration() == HLSLDeclaration.UnityPerMaterial)
                propBuilder.AddAttribute(new ShaderAttribute.Builder(container, CommonShaderAttributes.PerMaterial).Build());
            else if (shaderProp.GetDefaultHLSLDeclaration() == HLSLDeclaration.HybridPerInstance)
                propBuilder.AddAttribute(new ShaderAttribute.Builder(container, CommonShaderAttributes.Hybrid).Build());
            else
                AddPropertyDeclaration(container, propBuilder);

            return propBuilder;
        }

        static internal void BuildProp(ShaderContainer container, AbstractShaderProperty shaderProp, HLSLProperty hlslProp, HashSet<string> declaredHlslProps, List<BlockVariable> inputs)
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

            var varBuilder = new ShaderFoundry.BlockVariable.Builder(container);
            varBuilder.ReferenceName = hlslProp.name;
            varBuilder.DisplayName = shaderProp.displayName;
            varBuilder.Type = fieldType;

            if (declaration == HLSLDeclaration.DoNotDeclare)
            {

            }
            else if (declaration == HLSLDeclaration.Global)
                varBuilder.AddAttribute(new ShaderAttribute.Builder(container, CommonShaderAttributes.Global).Build());
            else if (declaration == HLSLDeclaration.UnityPerMaterial)
                varBuilder.AddAttribute(new ShaderAttribute.Builder(container, CommonShaderAttributes.PerMaterial).Build());
            else if (declaration == HLSLDeclaration.HybridPerInstance)
                varBuilder.AddAttribute(new ShaderAttribute.Builder(container, CommonShaderAttributes.Hybrid).Build());
            else
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

        static internal void BuildProperties(ShaderContainer container, Block.Builder blockBuilder, List<BlockVariable> inputs, ShaderGraph.PropertyCollector propCollector, ConcretePrecision defaultConcretePrecision)
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

        static internal void BuildProperties(ShaderContainer container, Block.Builder blockBuilder, List<BlockVariable> inputs, AbstractShaderProperty shaderProp, ConcretePrecision defaultConcretePrecision)
        {
            shaderProp.ForeachHLSLProperty((HLSLProperty hlslProp) =>
            {
                string typeName = GetHlslType(hlslProp);

                var fieldType = container.GetType(typeName);
                var inputBuilder = new ShaderFoundry.BlockVariable.Builder(container);
                inputBuilder.ReferenceName = hlslProp.name;
                inputBuilder.DisplayName = shaderProp.displayName;
                inputBuilder.Type = fieldType;

                bool isMaterialProperty = hlslProp.name == shaderProp.referenceName;
                var declaration = hlslProp.declaration;
                bool isExposed = true;
                if (isMaterialProperty)
                {
                    if(shaderProp.overrideHLSLDeclaration)
                        declaration = shaderProp.hlslDeclarationOverride;
                    isExposed = shaderProp.isExposed;
                }

                if (declaration == HLSLDeclaration.DoNotDeclare)
                {

                }
                else if (declaration == HLSLDeclaration.Global)
                    inputBuilder.AddAttribute(new ShaderAttribute.Builder(container, CommonShaderAttributes.Global).Build());
                else if (declaration == HLSLDeclaration.UnityPerMaterial)
                    inputBuilder.AddAttribute(new ShaderAttribute.Builder(container, CommonShaderAttributes.PerMaterial).Build());
                else if (declaration == HLSLDeclaration.HybridPerInstance)
                    inputBuilder.AddAttribute(new ShaderAttribute.Builder(container, CommonShaderAttributes.Hybrid).Build());
                else
                    AddPropertyDeclaration(container, inputBuilder);

                if (shaderProp.referenceName == hlslProp.name)
                {
                    //string defaultValue = null;
                    //string attributesString = null;
                    //string propertyType = null;

                    switch (shaderProp)
                    {
                        case ColorShaderProperty colorProp:
                            {
                                AddUniformDeclaration(container, inputBuilder, "float4");
                                if(isExposed)
                                {
                                    AddMaterialProperty(container, inputBuilder, shaderProp, "Color", colorProp.hdrTagString);
                                    string defaultValue = $"({colorProp.value.r}, {colorProp.value.g}, {colorProp.value.b}, {colorProp.value.a})";
                                    AddMaterialPropertyDefault(container, inputBuilder, defaultValue);
                                }
                                break;
                            }
                        case Texture2DShaderProperty tex2DProp:
                            {
                                DeclareBasicUniform(container, inputBuilder, hlslProp);
                                var normalTagString = (tex2DProp.defaultType == Texture2DShaderProperty.DefaultType.NormalMap) ? "[Normal]" : "";
                                string attributesString = $"{tex2DProp.hideTagString}{tex2DProp.modifiableTagString}{normalTagString}{tex2DProp.mainTextureString}{tex2DProp.useSTString}";
                                AddMaterialProperty(container, inputBuilder, shaderProp, "2D", attributesString);
                                string defaultValue = $"\"{Texture2DShaderProperty.ToShaderLabString(tex2DProp.defaultType)}\" {{}}";
                                AddMaterialPropertyDefault(container, inputBuilder, defaultValue);
                                break;
                            }
                        case Texture3DShaderProperty tex3DProp:
                            DeclareBasicUniform(container, inputBuilder, hlslProp);
                            AddMaterialProperty(container, inputBuilder, shaderProp, "3D");
                            AddMaterialPropertyDefault(container, inputBuilder, "\"\" {}");
                            break;
                        case CubemapShaderProperty cubeProp:
                            DeclareBasicUniform(container, inputBuilder, hlslProp);
                            AddMaterialProperty(container, inputBuilder, shaderProp, "CUBE");
                            AddMaterialPropertyDefault(container, inputBuilder, "\"\" {}");
                            break;
                        case Texture2DArrayShaderProperty tex2DArrayProp:
                            DeclareBasicUniform(container, inputBuilder, hlslProp);
                            AddMaterialProperty(container, inputBuilder, shaderProp, "2DArray");
                            AddMaterialPropertyDefault(container, inputBuilder, "\"\" {}");
                            break;
                    }
                }

                inputs.Add(inputBuilder.Build());
            });
        }

        static internal void BuildProperties(ShaderContainer container, Block.Builder blockBuilder, List<BlockVariable> properties, List<BlockVariable> inputs, AbstractShaderProperty shaderProp, ConcretePrecision defaultConcretePrecision)
        {
            switch (shaderProp)
            {
                case BooleanShaderProperty boolProp:
                    {
                        var propBuilder = CreateBasicProperty(container, shaderProp, defaultConcretePrecision);
                        AddUniformDeclaration(container, propBuilder, "float");
                        propBuilder.AddAttribute(new ShaderAttribute.Builder(container, "MaterialProperty").Param($"[ToggleUI]{shaderProp.referenceName}(\"{shaderProp.displayName}\", Float)").Build());
                        float defaultValue = boolProp.value == false ? 0 : 1;
                        AddMaterialPropertyDefault(container, propBuilder, $"{defaultValue}");
                        properties.Add(propBuilder.Build());
                        inputs.Add(propBuilder.Build());
                        break;
                    }
                case Vector1ShaderProperty vec1Prop:
                    {
                        var propBuilder = CreateBasicProperty(container, shaderProp, defaultConcretePrecision);
                        AddUniformDeclaration(container, propBuilder, "float");
                        AddMaterialProperty(container, propBuilder, shaderProp, "Float");
                        string defaultValue = $"{vec1Prop.value}";
                        AddMaterialPropertyDefault(container, propBuilder, defaultValue);
                        properties.Add(propBuilder.Build());
                        inputs.Add(propBuilder.Build());
                        break;
                    }
                case Vector2ShaderProperty vec2Prop:
                    {
                        var propBuilder = CreateBasicProperty(container, shaderProp, defaultConcretePrecision);
                        AddUniformDeclaration(container, propBuilder, "float2");
                        AddMaterialProperty(container, propBuilder, shaderProp, "Vector");
                        string defaultValue = $"({vec2Prop.value.x}, {vec2Prop.value.y}, 0, 0)";
                        AddMaterialPropertyDefault(container, propBuilder, defaultValue);
                        properties.Add(propBuilder.Build());
                        inputs.Add(propBuilder.Build());
                        break;
                    }
                case Vector3ShaderProperty vec3Prop:
                    {
                        var propBuilder = CreateBasicProperty(container, shaderProp, defaultConcretePrecision);
                        AddUniformDeclaration(container, propBuilder, "float3");
                        AddMaterialProperty(container, propBuilder, shaderProp, "Vector");
                        string defaultValue = $"({vec3Prop.value.x}, {vec3Prop.value.y}, {vec3Prop.value.z}, 0)";
                        AddMaterialPropertyDefault(container, propBuilder, defaultValue);
                        properties.Add(propBuilder.Build());
                        inputs.Add(propBuilder.Build());
                        break;
                    }
                case Vector4ShaderProperty vec4Prop:
                    {
                        var propBuilder = CreateBasicProperty(container, shaderProp, defaultConcretePrecision);
                        AddUniformDeclaration(container, propBuilder, "float4");
                        AddMaterialProperty(container, propBuilder, shaderProp, "Vector");
                        string defaultValue = $"({vec4Prop.value.x}, {vec4Prop.value.y}, {vec4Prop.value.z}, {vec4Prop.value.w})";
                        AddMaterialPropertyDefault(container, propBuilder, defaultValue);
                        blockBuilder.AddProperty(propBuilder.Build());
                        break;
                    }
                case ColorShaderProperty colorProp:
                    {
                        var propBuilder = CreateBasicProperty(container, shaderProp, defaultConcretePrecision);
                        AddUniformDeclaration(container, propBuilder, "float4");
                        AddMaterialProperty(container, propBuilder, shaderProp, "Color", colorProp.hdrTagString);
                        string defaultValue = $"({colorProp.value.r}, {colorProp.value.g}, {colorProp.value.b}, {colorProp.value.a})";
                        AddMaterialPropertyDefault(container, propBuilder, defaultValue);
                        properties.Add(propBuilder.Build());
                        inputs.Add(propBuilder.Build());
                        break;
                    }
                default:
                    {
                        string typeName = shaderProp.GetPropertyTypeString();
                        var propType = container.GetType(typeName);
                        var propBuilder = new ShaderFoundry.BlockVariable.Builder(container);
                        propBuilder.ReferenceName = shaderProp.referenceName;
                        propBuilder.DisplayName = shaderProp.displayName;
                        propBuilder.Type = propType;
                        switch (shaderProp)
                        {
                            case Texture2DShaderProperty tex2DProp:
                                var normalTagString = (tex2DProp.defaultType == Texture2DShaderProperty.DefaultType.NormalMap) ? "[Normal]" : "";
                                string attributesString = $"{tex2DProp.hideTagString}{tex2DProp.modifiableTagString}{normalTagString}{tex2DProp.mainTextureString}{tex2DProp.useSTString}";
                                AddMaterialProperty(container, propBuilder, shaderProp, "2D", attributesString);
                                string defaultValue = $"\"{Texture2DShaderProperty.ToShaderLabString(tex2DProp.defaultType)}\" {{}}";
                                AddMaterialPropertyDefault(container, propBuilder, defaultValue);
                                break;
                            case Texture3DShaderProperty tex3DProp:
                                AddMaterialProperty(container, propBuilder, shaderProp, "3D");
                                AddMaterialPropertyDefault(container, propBuilder, "\"\" {}");
                                break;
                            case CubemapShaderProperty cubeProp:
                                AddMaterialProperty(container, propBuilder, shaderProp, "CUBE");
                                AddMaterialPropertyDefault(container, propBuilder, "\"\" {}");
                                break;
                            case Texture2DArrayShaderProperty tex2DArrayProp:
                                AddMaterialProperty(container, propBuilder, shaderProp, "2DArray");
                                AddMaterialPropertyDefault(container, propBuilder, "\"\" {}");
                                break;
                        }
                        properties.Add(propBuilder.Build());

                        shaderProp.ForeachHLSLProperty((HLSLProperty hlslProp) =>
                            {
                                string typeName = GetHlslType(hlslProp);

                                var fieldType = container.GetType(typeName);
                                var inputBuilder = new ShaderFoundry.BlockVariable.Builder(container);
                                inputBuilder.ReferenceName = hlslProp.name;
                                inputBuilder.DisplayName = shaderProp.displayName;
                                inputBuilder.Type = fieldType;

                                if (hlslProp.declaration == HLSLDeclaration.DoNotDeclare)
                                {

                                }
                                else if (hlslProp.declaration == HLSLDeclaration.Global)
                                    inputBuilder.AddAttribute(new ShaderAttribute.Builder(container, CommonShaderAttributes.Global).Build());
                                else if (hlslProp.declaration == HLSLDeclaration.UnityPerMaterial)
                                    inputBuilder.AddAttribute(new ShaderAttribute.Builder(container, CommonShaderAttributes.PerMaterial).Build());
                                else if (shaderProp.GetDefaultHLSLDeclaration() == HLSLDeclaration.HybridPerInstance)
                                    inputBuilder.AddAttribute(new ShaderAttribute.Builder(container, CommonShaderAttributes.Hybrid).Build());
                                else
                                    AddPropertyDeclaration(container, inputBuilder);

                                DeclareBasicUniform(container, inputBuilder, hlslProp);
                                if (shaderProp.referenceName == hlslProp.name)
                                {
                                    switch (shaderProp)
                                    {
                                        case Texture2DShaderProperty tex2DProp:
                                            var normalTagString = (tex2DProp.defaultType == Texture2DShaderProperty.DefaultType.NormalMap) ? "[Normal]" : "";
                                            string attributesString = $"{tex2DProp.hideTagString}{tex2DProp.modifiableTagString}{normalTagString}{tex2DProp.mainTextureString}{tex2DProp.useSTString}";
                                            AddMaterialProperty(container, inputBuilder, shaderProp, "2D", attributesString);
                                            string defaultValue = $"\"{Texture2DShaderProperty.ToShaderLabString(tex2DProp.defaultType)}\" {{}}";
                                            AddMaterialPropertyDefault(container, inputBuilder, defaultValue);
                                            break;
                                        case Texture3DShaderProperty tex3DProp:
                                            AddMaterialProperty(container, inputBuilder, shaderProp, "3D");
                                            AddMaterialPropertyDefault(container, inputBuilder, "\"\" {}");
                                            break;
                                        case CubemapShaderProperty cubeProp:
                                            AddMaterialProperty(container, inputBuilder, shaderProp, "CUBE");
                                            AddMaterialPropertyDefault(container, inputBuilder, "\"\" {}");
                                            break;
                                        case Texture2DArrayShaderProperty tex2DArrayProp:
                                            AddMaterialProperty(container, inputBuilder, shaderProp, "2DArray");
                                            AddMaterialPropertyDefault(container, inputBuilder, "\"\" {}");
                                            break;
                                    }
                                }

                                inputs.Add(inputBuilder.Build());
                            });
                        break;
                    }
            }
        }
    }
}
