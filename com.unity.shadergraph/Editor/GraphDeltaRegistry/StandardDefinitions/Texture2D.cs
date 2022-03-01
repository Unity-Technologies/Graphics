

using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;
using com.unity.shadergraph.defs;
using UnityEngine;
using System.Linq;

namespace UnityEditor.ShaderGraph.Registry.Types
{
    internal class SampleTexture2DNode : Defs.INodeDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() => new RegistryKey { Name = "SampleTexture2DNode", Version = 1 };
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

        public const string kTexture = "Texture2D";
        public const string kUV = "UV";
        public const string kSampler = "Sampler";
        public const string kOutput = "Out";

        public void BuildNode(INodeReader userData, INodeWriter generatedData, Registry registry)
        {
            generatedData.AddPort<Texture2DType>(userData, kTexture, true, registry);
            var uv = generatedData.AddPort<GraphType>(userData, kUV, true, registry);
            generatedData.AddPort<SamplerStateType>(userData, kSampler, true, registry);
            var color = generatedData.AddPort<GraphType>(userData, kOutput, false, registry);

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
            // If there is no connected sampler, we'll generate the NoSampler version.
            data.TryGetPort(kSampler, out var samplerPort);
            bool useSampler = samplerPort.GetConnectedPorts().Count() != 0;

            var builder = new ShaderFoundry.ShaderFunction.Builder(container, GetRegistryKey().Name + (useSampler ? "" : "_NoSampler"));

            data.TryGetPort(kTexture, out var port);
            var texType = registry.GetShaderType((IFieldReader)port, container);

            builder.AddInput(texType, kTexture);
            builder.AddInput(container._float2, kUV);
            builder.AddInput(container._float4, kOutput);

            if (useSampler)
                builder.AddInput(container._SamplerState, kSampler);

            string body = $"{kOutput} = SAMPLE_TEXTURE2D({kTexture}.tex, {(useSampler ? kSampler : kTexture)}.samplerstate, {kTexture}.GetTransformedUV({kUV}));";
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
        public static UnityEditor.ShaderGraph.Internal.SerializableTexture GetTextureAsset(IFieldReader data)
        {
            if (data.GetField<UnityEditor.ShaderGraph.Internal.SerializableTexture>(Texture2DType.KAsset, out var asset))
                return asset;
            else return null;
        }

        public static void SetTextureAsset(IFieldWriter data, UnityEditor.ShaderGraph.Internal.SerializableTexture tex)
        {
            data.SetField(Texture2DType.KAsset, tex);
        }

        public static string GetUniquePropertyName(IFieldReader data) => data.GetFullPath().Replace('.', '_') + "_Tex";
    }


    internal class Texture2DType : Defs.ITypeDefinitionBuilder
    {
        public static RegistryKey kRegistryKey => new RegistryKey { Name = "Texture2DType", Version = 1 };
        public RegistryKey GetRegistryKey() => kRegistryKey;
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Type;

        #region LocalNames
        public const string KAsset = "Asset";
        public const string kPropertyName = "Uniform";
        #endregion

        public void BuildType(IFieldReader userData, IFieldWriter typeWriter, Registry registry)
        {
            // Generate a unique property name based on the path of this port (<node.name>_<port.name>_Tex).
            // Should not modify this.
            typeWriter.SetField<string>(kPropertyName, userData.GetFullPath().Replace('.', '_') + "_Tex");
        }

        string Defs.ITypeDefinitionBuilder.GetInitializerList(IFieldReader data, Registry registry)
        {
            data.GetField(kPropertyName, out string name);
            return $"UnityBuildTexture2DStruct({name})";
        }

        ShaderFoundry.ShaderType Defs.ITypeDefinitionBuilder.GetShaderType(IFieldReader data, ShaderFoundry.ShaderContainer container, Registry registry)
        {
            var gradientBuilder = new ShaderFoundry.ShaderType.StructBuilder(container, "UnityTexture2D");
            gradientBuilder.DeclaredExternally();
            return gradientBuilder.Build();
        }
    }

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
