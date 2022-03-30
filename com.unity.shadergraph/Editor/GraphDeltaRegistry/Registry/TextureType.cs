using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderFoundry;
using UnityEngine;


namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal class SimpleSampleTexture2DNode : INodeDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() => new RegistryKey { Name = "SimpleSampleTexture2DNode", Version = 1 };
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

        public const string kTexture = "Input";
        public const string kUV = "UV";
        public const string kOutput = "Output";

        public void BuildNode(NodeHandler node, Registry registry)
        {
            node.AddPort<BaseTextureType>(kTexture, true, registry);
            var uv = node.AddPort<GraphType>(kUV, true, registry).GetTypeField();
            var color = node.AddPort<GraphType>(kOutput, false, registry).GetTypeField();

            GraphTypeHelpers.InitGraphType(uv, GraphType.Length.Two);
            GraphTypeHelpers.InitGraphType(color);
        }

        public ShaderFunction GetShaderFunction(NodeHandler node, ShaderContainer container, Registry registry)
        {
            var builder = new ShaderFunction.Builder(container, GetRegistryKey().Name);
            var shaderType = registry.GetShaderType(node.GetPort(kTexture).GetTypeField(), container);

            builder.AddInput(shaderType, kTexture);
            builder.AddInput(container._float2, kUV);
            builder.AddOutput(container._float4, kOutput);

            string body = $"{kOutput} = SAMPLE_TEXTURE2D({kTexture}.tex, {kTexture}.samplerstate, {kTexture}.GetTransformedUV({kUV}));";
            builder.AddLine(body);
            return builder.Build();
        }
    }

    internal class SimpleTextureNode : INodeDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() => new RegistryKey { Name = "SimpleSampleTextureNode", Version = 1 };
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

        public const string kInlineStatic = "InlineStatic";
        public const string kOutput = "Output";

        public void BuildNode(NodeHandler node, Registry registry)
        {
            var input = node.AddPort<BaseTextureType>(kInlineStatic, true, registry);
            input.GetTypeField().AddSubField("IsStatic", true); // TODO: should be handled by ui hints
            node.AddPort<BaseTextureType>(kOutput, false, registry);
        }

        public ShaderFunction GetShaderFunction(NodeHandler node, ShaderContainer container, Registry registry)
        {
            var shaderFunctionBuilder = new ShaderFunction.Builder(container, GetRegistryKey().Name);
            var field = node.GetPort(kInlineStatic).GetTypeField();
            string name;
            switch (BaseTextureType.GetTextureAsset(field))
            {
                case Texture3D: name = $"{GetRegistryKey().Name}_3D"; break;
                case Texture2DArray: name = $"{GetRegistryKey().Name}_2DArray"; break;
                case Cubemap: name = $"{GetRegistryKey().Name}_Cube"; break;
                case Texture2D:
                default: name = $"{GetRegistryKey().Name}_2D"; break;
            }

            var shaderType = registry.GetShaderType(field, container);

            shaderFunctionBuilder.AddInput(shaderType, kInlineStatic);
            shaderFunctionBuilder.AddOutput(shaderType, kOutput);
            shaderFunctionBuilder.AddLine($"{kOutput} = {kInlineStatic};");
            return shaderFunctionBuilder.Build();
        }
    }

    internal class BaseTextureType : ITypeDefinitionBuilder
    {
        public static RegistryKey kRegistryKey => new RegistryKey { Name = "TextureType", Version = 1 };
        public RegistryKey GetRegistryKey() => kRegistryKey;
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Type;

        #region LocalNames
        public const string KAsset = "Asset";
        #endregion

        public static string GetUniquePropertyName(FieldHandler data)
            => data.ID.FullPath.Replace('.', '_') + "_Tex";

        public static Texture GetTextureAsset(FieldHandler data)
            => data.GetSubField<Internal.SerializableTexture>(KAsset).GetData().texture;

        public static void SetTextureAsset(FieldHandler data, Texture tex)
        {
            var stex = new Internal.SerializableTexture();
            stex.texture = tex;

            data.GetSubField<Internal.SerializableTexture>(KAsset).SetData(stex);
        }


        public void BuildType(FieldHandler field, Registry registry)
        {
            var stex = new Internal.SerializableTexture();
            stex.texture = null;
            field.AddSubField(KAsset, stex);
        }

        public ShaderType GetShaderType(FieldHandler field, ShaderContainer container, Registry registry)
        {
            switch (GetTextureAsset(field))
            {
                case Texture3D: return container._UnityTexture3D;
                case Texture2DArray: return container._UnityTexture2DArray;
                case Cubemap: return container._UnityTextureCube;
                case Texture2D:
                default: return container._UnityTexture2D;
            }
        }

        public string GetInitializerList(FieldHandler field, Registry registry)
        {
            string name = GetUniquePropertyName(field);
            switch (GetTextureAsset(field))
            {
                case Texture3D: return $"UnityBuildTexture3DStruct({name})";
                case Texture2DArray: return $"UnityBuildTexture2DArrayStruct({name})";
                case Cubemap: return $"UnityBuildTextureCubeStruct({name})";
                case Texture2D:
                default: return $"UnityBuildTexture2DStruct({name})";
            }
        }

        internal static IEnumerable<BlockVariable> UniformPromotion(FieldHandler field, ShaderContainer container)
        {
            var uniformName = GetUniquePropertyName(field);
            var builder = new BlockVariable.Builder(container);
            builder.Name = uniformName;
            builder.Type = container._UnityTexture2D;

            yield return builder.Build();
            //var uniformName = GetUniquePropertyName(field);
            //var location = new ShaderAttribute.Builder(container, CommonShaderAttributes.PerMaterial).Build();

            //var textureBuilder = new BlockVariable.Builder(container);
            //var samplerBuilder = new BlockVariable.Builder(container);
            //var texsizeBuilder = new BlockVariable.Builder(container);
            //var sctransBuilder = new BlockVariable.Builder(container);

            //textureBuilder.ReferenceName = uniformName;
            //samplerBuilder.ReferenceName = $"sampler{uniformName}";
            //texsizeBuilder.ReferenceName = $"{uniformName}_TexelSize";
            //sctransBuilder.ReferenceName = $"{uniformName}_ST";

            //textureBuilder.Type = container._Texture2D;
            //samplerBuilder.Type = container._SamplerState;
            //texsizeBuilder.Type = container._float4;
            //sctransBuilder.Type = container._float4;

            //textureBuilder.AddAttribute(location);
            //samplerBuilder.AddAttribute(location);
            //texsizeBuilder.AddAttribute(location);
            //sctransBuilder.AddAttribute(location);

            //var attributeBuilder = new ShaderAttribute.Builder(container, CommonShaderAttributes.UniformDeclaration);
            //var nameParamBuilder = new ShaderAttributeParam.Builder(container, "name", samplerBuilder.ReferenceName);
            //var declarationParamBuilder = new ShaderAttributeParam.Builder(container, "declaration", "SAMPLER(#)");
            //attributeBuilder.Param(nameParamBuilder.Build());
            //attributeBuilder.Param(declarationParamBuilder.Build());
            //samplerBuilder.AddAttribute(attributeBuilder.Build());

            //attributeBuilder = new ShaderAttribute.Builder(container, CommonShaderAttributes.UniformDeclaration);
            //nameParamBuilder = new ShaderAttributeParam.Builder(container, "name", textureBuilder.ReferenceName);
            //declarationParamBuilder = new ShaderAttributeParam.Builder(container, "declaration", "TEXTURE2D(#)");
            //attributeBuilder.Param(nameParamBuilder.Build());
            //attributeBuilder.Param(declarationParamBuilder.Build());
            //textureBuilder.AddAttribute(attributeBuilder.Build());

            //var texture = textureBuilder.Build();
            //var sampler = samplerBuilder.Build();
            //var texsize = texsizeBuilder.Build();
            //var sctrans = sctransBuilder.Build();

            //yield return texture;
            //yield return sampler;
            //yield return texsize;
            //yield return sctrans;
        }

    }

    internal class BaseTextureTypeAssignment : ICastDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() => new RegistryKey { Name = "BaseTextureAssignment", Version = 1 };
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Cast;
        public (RegistryKey, RegistryKey) GetTypeConversionMapping() => (BaseTextureType.kRegistryKey, BaseTextureType.kRegistryKey);
        public bool CanConvert(FieldHandler src, FieldHandler dst)
        {
            return BaseTextureType.GetTextureAsset(src).GetType() == BaseTextureType.GetTextureAsset(dst).GetType();
        }

        public ShaderFunction GetShaderCast(FieldHandler src, FieldHandler dst, ShaderContainer container, Registry registry)
        {
            var type = registry.GetShaderType(src, container);
            string castName = $"Cast{type.Name}_{type.Name}";
            var builder = new ShaderFoundry.ShaderFunction.Builder(container, castName);
            builder.AddInput(type, "In");
            builder.AddOutput(type, "Out");
            builder.AddLine("Out = In;");
            return builder.Build();
        }
    }
}
