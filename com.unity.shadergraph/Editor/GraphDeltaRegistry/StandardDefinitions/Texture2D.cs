

using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;
using com.unity.shadergraph.defs;
using UnityEngine;
using System.Linq;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderGraph.Registry.Types
{
    internal class SampleTexture2DNode : Defs.INodeDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() => new RegistryKey { Name = "SampleTexture2DNode", Version = 1 };
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

        public const string kTexture = "InTex";
        public const string kUV = "UV";
        public const string kOutput = "Out";

        public void BuildNode(INodeReader userData, INodeWriter generatedData, Registry registry)
        {
            generatedData.AddPort<Texture2DType>(userData, kTexture, true, registry);
            var uv = generatedData.AddPort<GraphType>(userData, kUV, true, registry);
            // TODO: Sampler
            var color = generatedData.AddPort<GraphType>(userData, kOutput, false, registry);

            // This is annoying
            uv.SetField(GraphType.kPrecision, GraphType.Precision.Single);
            uv.SetField(GraphType.kPrimitive, GraphType.Primitive.Float);
            uv.SetField(GraphType.kLength, GraphType.Length.Two);
            uv.SetField(GraphType.kHeight, GraphType.Height.One);

            color.SetField(GraphType.kPrecision, GraphType.Precision.Single);
            color.SetField(GraphType.kPrimitive, GraphType.Primitive.Float);
            color.SetField(GraphType.kLength, GraphType.Length.Four);
            color.SetField(GraphType.kHeight, GraphType.Height.One);
        }

        public ShaderFoundry.ShaderFunction GetShaderFunction(INodeReader data, ShaderFoundry.ShaderContainer container, Registry registry)
        {
            var builder = new ShaderFoundry.ShaderFunction.Builder(container, GetRegistryKey().Name);

            data.TryGetPort(kTexture, out var port);
            var texType = registry.GetShaderType((IFieldReader)port, container);

            builder.AddInput(texType, kTexture);
            builder.AddInput(container._float2, kUV);
            builder.AddOutput(container._float4, kOutput);

            string body = $"{kOutput} = SAMPLE_TEXTURE2D({kTexture}.tex, {kTexture}.samplerstate, {kTexture}.GetTransformedUV({kUV}));";
            builder.AddLine(body);

            return builder.Build();
        }
    }


    internal class Texture2DNode : Defs.INodeDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() => new RegistryKey { Name = "Texture2DNode", Version = 1 };
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

        public const string kInlineStatic = "InlineStatic";
        public const string kOutput = "Out";

        public void BuildNode(INodeReader userData, INodeWriter generatedData, Registry registry)
        {
            var input = generatedData.AddPort<Texture2DType>(userData, kInlineStatic, true, registry);
            input.SetField<bool>("IsStatic", true);
            var output = generatedData.AddPort<Texture2DType>(userData, kOutput, false, registry);
        }

        public ShaderFoundry.ShaderFunction GetShaderFunction(INodeReader data, ShaderFoundry.ShaderContainer container, Registry registry)
        {
            var shaderFunctionBuilder = new ShaderFoundry.ShaderFunction.Builder(container, GetRegistryKey().Name);
            data.TryGetPort(kInlineStatic, out var port);

            var shaderType = registry.GetShaderType((IFieldReader)port, container);
            shaderFunctionBuilder.AddInput(shaderType, kInlineStatic);
            shaderFunctionBuilder.AddOutput(shaderType, kOutput);
            shaderFunctionBuilder.AddLine($"{kOutput} = {kInlineStatic};");
            return shaderFunctionBuilder.Build();
        }
    }


    public static class Texture2DHelpers
    {
        public static Texture2D GetTextureAsset(IFieldReader data)
        {
            if (data.GetField<UnityEditor.ShaderGraph.Internal.SerializableTexture>(Texture2DType.KAsset, out var asset))
                return (Texture2D)asset.texture;
            else return null;
        }

        public static void SetTextureAsset(IFieldWriter data, Texture2D tex)
        {
            var stex = new UnityEditor.ShaderGraph.Internal.SerializableTexture();
            stex.texture = tex;
            data.SetField(Texture2DType.KAsset, stex);
        }

        public static string GetUniquePropertyName(IFieldReader data) => data.GetFullPath().Replace('.', '_') + "_Tex";

        public static (string, Texture) GetShaderDefault(IFieldReader data) => (GetUniquePropertyName(data), GetTextureAsset(data));

        // Should move this into the ITypeDefinitionBuilder, which should probably be abstract
        internal static IEnumerable<BlockVariable> UniformPromotion(IFieldReader field, ShaderContainer container)
        {
            var uniformName = GetUniquePropertyName(field);
            var location = new ShaderAttribute.Builder(container, CommonShaderAttributes.PerMaterial).Build();

            var textureBuilder = new BlockVariable.Builder(container);
            var samplerBuilder = new BlockVariable.Builder(container);
            var texsizeBuilder = new BlockVariable.Builder(container);
            var sctransBuilder = new BlockVariable.Builder(container);

            textureBuilder.ReferenceName = uniformName;
            samplerBuilder.ReferenceName = $"sampler{uniformName}";
            texsizeBuilder.ReferenceName = $"{uniformName}_TexelSize";
            sctransBuilder.ReferenceName = $"{uniformName}_ST";

            textureBuilder.Type = container._Texture2D;
            samplerBuilder.Type = container._SamplerState;
            texsizeBuilder.Type = container._float4;
            sctransBuilder.Type = container._float4;

            textureBuilder.AddAttribute(location);
            samplerBuilder.AddAttribute(location);
            texsizeBuilder.AddAttribute(location);
            sctransBuilder.AddAttribute(location);

            var attributeBuilder = new ShaderAttribute.Builder(container, CommonShaderAttributes.UniformDeclaration);
            var nameParamBuilder = new ShaderAttributeParam.Builder(container, "name", samplerBuilder.ReferenceName);
            var declarationParamBuilder = new ShaderAttributeParam.Builder(container, "declaration", "SAMPLER(#)");
            attributeBuilder.Param(nameParamBuilder.Build());
            attributeBuilder.Param(declarationParamBuilder.Build());
            samplerBuilder.AddAttribute(attributeBuilder.Build());

            attributeBuilder = new ShaderAttribute.Builder(container, CommonShaderAttributes.UniformDeclaration);
            nameParamBuilder = new ShaderAttributeParam.Builder(container, "name", textureBuilder.ReferenceName);
            declarationParamBuilder = new ShaderAttributeParam.Builder(container, "declaration", "TEXTURE2D(#)");
            attributeBuilder.Param(nameParamBuilder.Build());
            attributeBuilder.Param(declarationParamBuilder.Build());
            textureBuilder.AddAttribute(attributeBuilder.Build());

            var texture = textureBuilder.Build();
            var sampler = samplerBuilder.Build();
            var texsize = texsizeBuilder.Build();
            var sctrans = sctransBuilder.Build();

            yield return texture;
            yield return sampler;
            yield return texsize;
            yield return sctrans;
        }

        internal static IEnumerable<BlockVariable> PropertyPromotion(IFieldReader field, ShaderContainer container)
        {
            var propertyBuilder = new BlockVariable.Builder(container);
            propertyBuilder.ReferenceName = GetUniquePropertyName(field);
            propertyBuilder.DisplayName = $"Property_{GetUniquePropertyName(field)}";
            propertyBuilder.Type = container._Texture2D;

            string paramString = $"{propertyBuilder.ReferenceName}(\"{propertyBuilder.DisplayName}\", 2D)";
            var attributeBuilder = new ShaderAttribute.Builder(container, CommonShaderAttributes.MaterialProperty);
            var paramBuilder = new ShaderAttributeParam.Builder(container, null, paramString);
            attributeBuilder.Param(paramBuilder.Build());
            propertyBuilder.AddAttribute(attributeBuilder.Build());
            propertyBuilder.DefaultExpression = "\"white\" {}";

            yield return propertyBuilder.Build();
        }
    }


    internal class Texture2DType : Defs.ITypeDefinitionBuilder
    {
        public static RegistryKey kRegistryKey => new RegistryKey { Name = "Texture2DType", Version = 1 };
        public RegistryKey GetRegistryKey() => kRegistryKey;
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Type;

        #region LocalNames
        public const string KAsset = "Asset";
        #endregion

        public void BuildType(IFieldReader userData, IFieldWriter typeWriter, Registry registry)
        {
            Texture2DHelpers.SetTextureAsset(typeWriter, Texture2D.whiteTexture);
        }

        string Defs.ITypeDefinitionBuilder.GetInitializerList(IFieldReader data, Registry registry)
        {
            var name = Texture2DHelpers.GetUniquePropertyName(data);
            return $"UnityBuildTexture2DStruct({name})"; // See Textures.hlsl
        }

        ShaderFoundry.ShaderType Defs.ITypeDefinitionBuilder.GetShaderType(IFieldReader data, ShaderFoundry.ShaderContainer container, Registry registry)
        {
            var builder = new ShaderFoundry.ShaderType.StructBuilder(container, "UnityTexture2D");
            builder.DeclaredExternally();
            return builder.Build();
        }
    }

    // Trivial assignment casts should be genericized.
    internal class Texture2DAssignment : Defs.ICastDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() => new RegistryKey { Name = "Texture2DAssignment", Version = 1 };
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Cast;
        public (RegistryKey, RegistryKey) GetTypeConversionMapping() => (Texture2DType.kRegistryKey, Texture2DType.kRegistryKey);
        public bool CanConvert(IFieldReader src, IFieldReader dst) => true;

        public ShaderFoundry.ShaderFunction GetShaderCast(IFieldReader src, IFieldReader dst, ShaderFoundry.ShaderContainer container, Registry registry)
        {
            var srcType = registry.GetTypeBuilder(src.GetRegistryKey()).GetShaderType(src, container, registry);
            var dstType = registry.GetTypeBuilder(dst.GetRegistryKey()).GetShaderType(dst, container, registry);
            string castName = $"Cast{srcType.Name}_{dstType.Name}";
            var builder = new ShaderFoundry.ShaderFunction.Builder(container, castName);
            builder.AddInput(srcType, "In");
            builder.AddOutput(dstType, "Out");
            builder.AddLine("Out = In;");
            return builder.Build();
        }
    }
}
