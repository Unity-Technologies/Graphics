

using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;
using com.unity.shadergraph.defs;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Registry.Types
{
    // trivial constructor node, should not need a separate definition
    internal class SamplerStateNode : Defs.INodeDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() => new RegistryKey { Name = "SamplerStateNode", Version = 1 };
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

        public const string kInlineStatic = "InlineStatic";
        public const string kOutput = "Out";

        public void BuildNode(INodeReader userData, INodeWriter generatedData, Registry registry)
        {
            var input = generatedData.AddPort<SamplerStateType>(userData, kInlineStatic, true, registry);
            input.SetField("IsStatic", true);
            generatedData.AddPort<SamplerStateType>(userData, kOutput, false, registry);
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

    internal static class SamplerStateHelper
    {
        public static void SetFilter(IFieldWriter field, SamplerStateType.Filter filter) => field.SetField(SamplerStateType.kFilter, filter);
        public static void SetWrap(IFieldWriter field, SamplerStateType.Wrap wrap) => field.SetField(SamplerStateType.kWrap, wrap);

        public static SamplerStateType.Filter GetFilter(IFieldReader field)
        {
            field.GetField(SamplerStateType.kFilter, out SamplerStateType.Filter filter);
            return filter;
        }

        public static SamplerStateType.Wrap GetWrap(IFieldReader field)
        {
            field.GetField(SamplerStateType.kWrap, out SamplerStateType.Wrap wrap);
            return wrap;
        }
    }

    internal class SamplerStateType : Defs.ITypeDefinitionBuilder
    {
        public static RegistryKey kRegistryKey => new RegistryKey { Name = "SamplerStateType", Version = 1 };
        public RegistryKey GetRegistryKey() => kRegistryKey;
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Type;

        public enum Filter { Linear, Point, Trilinear }
        public enum Wrap { Repeat, Clamp1, Mirror, MirrorOnce }

        #region LocalNames
        public const string kFilter = "Filter";
        public const string kWrap = "Wrap";
        #endregion

        public void BuildType(IFieldReader userData, IFieldWriter typeWriter, Registry registry)
        {
            typeWriter.SetField(kFilter, Filter.Linear);
            typeWriter.SetField(kWrap, Wrap.Repeat);
        }

        private static string ToSamplerName(IFieldReader data)
        {
            data.GetField<Filter>(kFilter, out var filter);
            data.GetField<Wrap>(kWrap, out var wrap);
            return $"_SamplerState_{filter}_{wrap}";
        }

        string Defs.ITypeDefinitionBuilder.GetInitializerList(IFieldReader data, Registry registry)
        {
            // ShaderLab generates samplers by strstr(...) the sampler name.
            return $"UnityBuildSamplerStateStruct({ToSamplerName(data)})";
        }

        ShaderFoundry.ShaderType Defs.ITypeDefinitionBuilder.GetShaderType(IFieldReader data, ShaderFoundry.ShaderContainer container, Registry registry)
        {
            var builder = new ShaderFoundry.ShaderType.StructBuilder(container, "UnitySamplerState");
            builder.DeclaredExternally();
            return builder.Build();
        }
    }

    // Trivial assignment-- should not need a separate definition.
    internal class SamplerStateAssignment : Defs.ICastDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() => new RegistryKey { Name = "SamplerStateAssignment", Version = 1 };
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Cast;
        public (RegistryKey, RegistryKey) GetTypeConversionMapping() => (SamplerStateType.kRegistryKey, SamplerStateType.kRegistryKey);
        public bool CanConvert(IFieldReader src, IFieldReader dst) => true;

        public ShaderFoundry.ShaderFunction GetShaderCast(IFieldReader src, IFieldReader dst, ShaderFoundry.ShaderContainer container, Registry registry)
        {
            // This is a trivially convertible assignment, should refactor and impl a shared solution for nodes to refer to.
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


