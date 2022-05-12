using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderFoundry;
using UnityEngine;


namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal class SamplerStateExampleNode : INodeDefinitionBuilder
    {
        void INodeDefinitionBuilder.BuildNode(NodeHandler node, Registry registry)
        {
            var input = node.AddPort<SamplerStateType>("In", true, registry);
            input.GetTypeField().AddSubField("IsStatic", true); // TODO: should be handled by ui hints or metadata or something.
            node.AddPort<SamplerStateType>("Out", false, registry);
        }

        RegistryFlags IRegistryEntry.GetRegistryFlags() => RegistryFlags.Func;

        RegistryKey IRegistryEntry.GetRegistryKey() => new RegistryKey { Name = "SamplerStateExampleNode", Version = 1 };

        ShaderFunction INodeDefinitionBuilder.GetShaderFunction(NodeHandler node, ShaderContainer container, Registry registry)
        {
            // This example just naively inlines a sampler state type so we can test if connections are working correctly.
            // (since textures nodes shouldn't generate sampler states if they are not connected, they should just use the one that comes w/the texture).
            var builder = new ShaderFunction.Builder(container, "SamplerStateExampleNode");
            builder.AddInput(container._UnitySamplerState, "In");
            builder.AddOutput(container._UnitySamplerState, "Out");
            builder.AddLine("Out = In;");
            return builder.Build();
        }
    }

    internal class SamplerStateType : ITypeDefinitionBuilder
    {
        public static RegistryKey kRegistryKey => new RegistryKey { Name = "SamplerStateType", Version = 1 };
        public RegistryKey GetRegistryKey() => kRegistryKey;
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Type;

        #region LocalNames
        public const string kFilter = "Filter";
        public const string kWrap = "Wrap";
        public const string kAniso = "Aniso";
        public const string kCompare = "Compare";
        #endregion

        private static string GetUniqueSamplerName(FieldHandler field)
        {
            return field.ID.FullPath.Replace(".", "_");
        }

        public enum Filter { Point, Linear, Trilinear }
        public enum Wrap { Clamp, Repeat, Mirror, MirrorOnce } // optionally can be per component- can be added later.
        public enum Aniso { None = 0, Ansio2 = 2, Ansio8 = 8, Ansio16 = 16 } // optional

        #region GetSet
        public static void SetDepthComparison(FieldHandler field, bool enable)
        {
            try     { field.GetSubField<bool>(kCompare).SetData(enable); }
            catch   { field.AddSubField<bool>(kCompare, enable); }
        }
        public static bool GetDepthComparison(FieldHandler field)
        {
            try     { return field.GetSubField<bool>(kCompare).GetData(); }
            catch   { return false;  }
        }
        public static void SetAniso(FieldHandler field, Aniso aniso)
        {
            try     { field.GetSubField<Aniso>(kAniso).SetData(aniso); }
            catch   { field.AddSubField<Aniso>(kAniso, aniso); }
        }
        public static Aniso GetAniso(FieldHandler field)
        {
            try     { return field.GetSubField<Aniso>(kAniso).GetData(); }
            catch   { return Aniso.None; }

        }
        public static void SetFilter(FieldHandler field, Filter filter)
        {
            try     { field.GetSubField<Filter>(kFilter).SetData(filter); }
            catch   { field.AddSubField<Filter>(kFilter, filter); }
        }
        public static Filter GetFilter(FieldHandler field)
        {
            try     { return field.GetSubField<Filter>(kFilter).GetData(); }
            catch   { return Filter.Linear; }
        }

        public static void SetWrap(FieldHandler field, Wrap wrap)
        {
            try     { field.GetSubField<Wrap>(kWrap).SetData(wrap); }
            catch   { field.AddSubField<Wrap>(kWrap, wrap); }
        }
        public static Wrap GetWrap(FieldHandler field)
        {
            try     { return field.GetSubField<Wrap>(kWrap).GetData(); }
            catch   { return Wrap.Repeat; }
        }

        public static bool IsInitialized(FieldHandler field)
        {
            return field.GetSubField<Filter>(kFilter) != null
                || field.GetSubField<Wrap>(kWrap) != null
                || field.GetSubField<Aniso>(kAniso) != null
                || field.GetSubField<bool>(kCompare) != null;
        }


        #endregion


        public void BuildType(FieldHandler field, Registry registry)
        {
        }

        public void CopySubFieldData(FieldHandler src, FieldHandler dst)
        {
        }

        public ShaderType GetShaderType(FieldHandler field, ShaderContainer container, Registry registry)
        {
            return container._UnitySamplerState;
        }

        public string GetInitializerList(FieldHandler field, Registry registry)
        {
            return $"In.{GetUniqueSamplerName(field)}";
        }


        internal static StructField UniformPromotion(FieldHandler field, ShaderContainer container, Registry registry)
        {
            var name = GetUniqueSamplerName(field);

            var fieldbuilder = new StructField.Builder(container, name, registry.GetShaderType(field, container));
            var attributeBuilder = new ShaderAttribute.Builder(container, SamplerStateAttribute.AttributeName);

            attributeBuilder.Param(SamplerStateAttribute.FilterModeParamName, GetFilter(field).ToString());
            attributeBuilder.Param(SamplerStateAttribute.WrapModeParamName, GetWrap(field).ToString());

            if (GetAniso(field) != Aniso.None)
                attributeBuilder.Param(SamplerStateAttribute.AnisotropicLevelParamName, ((int)GetAniso(field)).ToString());

            if (GetDepthComparison(field))
                attributeBuilder.Param(SamplerStateAttribute.DepthCompareParamName, GetDepthComparison(field).ToString());

            fieldbuilder.AddAttribute(attributeBuilder.Build());

            var propAttr = new ShaderAttribute.Builder(container, CommonShaderAttributes.Property);
            fieldbuilder.AddAttribute(propAttr.Build());

            return fieldbuilder.Build();
        }
    }

    internal class SamplerStateAssignment : ICastDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() => new RegistryKey { Name = "SamplerStateAssignment", Version = 1 };
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Cast;
        public (RegistryKey, RegistryKey) GetTypeConversionMapping() => (SamplerStateType.kRegistryKey, SamplerStateType.kRegistryKey);
        public bool CanConvert(FieldHandler src, FieldHandler dst) => true;

        public ShaderFunction GetShaderCast(FieldHandler src, FieldHandler dst, ShaderContainer container, Registry registry)
        {
            var type = registry.GetShaderType(src, container);
            string castName = $"Cast{type.Name}_{type.Name}";
            var builder = new ShaderFunction.Builder(container, castName);
            builder.AddInput(type, "In");
            builder.AddOutput(type, "Out");
            builder.AddLine("Out = In;");
            return builder.Build();
        }
    }
}
