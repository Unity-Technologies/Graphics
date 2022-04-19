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

            string result = $"sampler_{GetFilter(field)}_{GetWrap(field)}";

            if (GetDepthComparison(field))
                result += "_Compare";

            var aniso = GetAniso(field);
            if (aniso != Aniso.None)
                result += $"_{aniso}";

            return result;
        }

        public enum Filter { Linear, Point, Trilinear }
        public enum Wrap { Repeat, Clamp1, Mirror, MirrorOnce } // optionally can be per component- can be added later.
        public enum Aniso { None, Ansio2, Ansio8, Ansio16, Ansio32 } // optional

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
            return $"UnityBuildSamplerStateStruct({ToSamplerString(field)})";
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
