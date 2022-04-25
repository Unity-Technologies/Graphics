using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderFoundry;
using UnityEngine;


namespace UnityEditor.ShaderGraph.GraphDelta
{
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

        private static string ToSamplerString(FieldHandler field)
        {
            // https://docs.unity3d.com/Manual/SL-SamplerStates.html

            string result = $"SamplerState_{GetFilter(field)}_{GetWrap(field)}";

            if (GetDepthComparison(field))
                result += "_Compare";

            var aniso = GetAniso(field);
            if (aniso != Aniso.None)
                result += $"_{aniso}";

            return result;
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

        public ShaderType GetShaderType(FieldHandler field, ShaderContainer container, Registry registry)
        {
            return container._UnitySamplerState;
        }

        public string GetInitializerList(FieldHandler field, Registry registry)
        {
            return $"In.{ToSamplerString(field)}";
        }


        internal static StructField UniformPromotion(FieldHandler field, ShaderContainer container, Registry registry)
        {
            var name = ToSamplerString(field);


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


    //            Oh, I should note that sampler states require an additional attribute, e.g.:
    //[SamplerState(filterMode = Linear, wrapMode = "Clamp,RepeatU", depthCompare = true, anisotropicLevel = 4)]
    //            You can see the values in and maybe re - use the SamplerStateAttribute class. The values are defaulted to:
    //filterMode = Linear
    //wrapMode = Repeat
    //depthCompare = false
    //anisotropicLevel = 0
    //internal List<ShaderAttribute> BuildAttributes(ShaderContainer container)
    //{
    //    var attributes = new List<ShaderAttribute>();
    //    var attributeBuilder = new ShaderAttribute.Builder(container, SamplerStateAttribute.AttributeName);
    //    if (FilterMode is SamplerStateAttribute.FilterModeEnum filterMode)
    //        attributeBuilder.Param(SamplerStateAttribute.FilterModeParamName, filterMode.ToString());
    //    var wrapModeString = SamplerStateAttribute.BuildWrapModeParameterValue(WrapModes);
    //    if (!string.IsNullOrEmpty(wrapModeString))
    //        attributeBuilder.Param(SamplerStateAttribute.WrapModeParamName, wrapModeString);
    //    if (DepthCompare is bool depthCompare)
    //        attributeBuilder.Param(SamplerStateAttribute.DepthCompareParamName, depthCompare.ToString());
    //    if (AnisotropicLevel is int anisotropicLevel)
    //        attributeBuilder.Param(SamplerStateAttribute.AnisotropicLevelParamName, anisotropicLevel.ToString());
    //    attributes.Add(attributeBuilder.Build());
    //    return attributes;
    //}




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
