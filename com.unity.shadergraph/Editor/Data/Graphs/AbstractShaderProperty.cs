using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    // class for extracting deprecated data from older versions of AbstractShaderProperty
    class LegacyShaderPropertyData
    {
        // indicates user wishes to support HYBRID renderer GPU instanced path
        [SerializeField]
        public bool m_GPUInstanced = false;
    }

    public enum HLSLType
    {
        // value types
        _float,
        _float2,
        _float3,
        _float4,
        _matrix4x4,

        // object types
        FirstObjectType,
        _Texture2D = FirstObjectType,
        _Texture3D,
        _TextureCube,
        _Texture2DArray,
        _SamplerState,

        // custom type
        _CUSTOM
    }

    // describes the different ways we can generate HLSL declarations
    [Flags]
    internal enum HLSLDeclaration
    {
        None,                       // NOT declared in HLSL
        Global,                     // declared in the global scope, mainly for use with state coming from Shader.SetGlobal*()
        UnityPerMaterial,           // declared in the UnityPerMaterial cbuffer, populated by Material or MaterialPropertyBlock
        HybridPerInstance,          // declared using HybridRenderer path (v1 or v2) to get DOTS GPU instancing
    }

    internal struct HLSLProperty
    {
        public string name;
        public HLSLType type;
        public ConcretePrecision precision;
        public HLSLDeclaration declaration;
        public Action<ShaderStringBuilder> customDeclaration;

        public HLSLProperty(HLSLType type, string name, HLSLDeclaration declaration, ConcretePrecision precision = ConcretePrecision.Single)
        {
            this.type = type;
            this.name = name;
            this.declaration = declaration;
            this.precision = precision;
            this.customDeclaration = null;
        }

        static string[,] kValueTypeStrings = new string[(int)HLSLType.FirstObjectType, 2]
        {
                {"float", "half"},
                {"float2", "half2"},
                {"float3", "half3"},
                {"float4", "half4"},
                {"float4x4", "half4x4"}
        };

        static string[] kObjectTypeStrings = new string[(int) HLSLType._CUSTOM - (int) HLSLType.FirstObjectType]
        {
                "TEXTURE2D",
                "TEXTURE3D",
                "TEXTURECUBE",
                "TEXTURE2D_ARRAY",
                "SAMPLER",
        };

        public void AppendTo(ShaderStringBuilder ssb, Func<string, string> nameModifier = null)
        {
            var mName = nameModifier?.Invoke(name) ?? name;

            if (type < HLSLType.FirstObjectType)
            {
                ssb.Append(kValueTypeStrings[(int)type, (int)precision]);
                ssb.Append(" ");
                ssb.Append(mName);
                ssb.Append(";");
                ssb.AppendNewLine();
            }
            else if (type < HLSLType._CUSTOM)
            {
                ssb.Append(kObjectTypeStrings[type - HLSLType.FirstObjectType]);
                ssb.Append("(");
                ssb.Append(mName);
                ssb.Append(");");
                ssb.AppendNewLine();
            }
            else
            {
                customDeclaration(ssb);
                ssb.AppendNewLine();
            }
        }
    }

    [Serializable]
    public abstract class AbstractShaderProperty : ShaderInput
    {

        public abstract PropertyType propertyType { get; }

        internal override ConcreteSlotValueType concreteShaderValueType => propertyType.ToConcreteShaderValueType();

        // user selected precision setting
        [SerializeField]
        Precision m_Precision = Precision.Inherit;

        // indicates user wishes to support HYBRID renderer GPU instanced path
        [SerializeField]
        private bool m_GPUInstanced = false;
        public bool gpuInstanced
        {
            get { return m_GPUInstanced; }
            set { m_GPUInstanced = value; }
        }

        internal Precision precision
        {
            get => m_Precision;
            set => m_Precision = value;
        }

        ConcretePrecision m_ConcretePrecision = ConcretePrecision.Single;
        public ConcretePrecision concretePrecision => m_ConcretePrecision;
        internal void ValidateConcretePrecision(ConcretePrecision graphPrecision)
        {
            m_ConcretePrecision = (precision == Precision.Inherit) ? graphPrecision : precision.ToConcrete();
        }

        [SerializeField]
        bool m_Hidden = false;
        public bool hidden
        {
            get => m_Hidden;
            set => m_Hidden = value;
        }

        internal string hideTagString => hidden ? "[HideInInspector]" : "";

        // simple properties use a single name; these functions cover that case
        // complex properties can override thes function to produce multiple reference names

        // reference names are the HLSL declaration name / property block ref name
        internal virtual void GetPropertyReferenceNames(List<string> result)
        {
            result.Add(referenceName);
        }

        // display names are used as the UI name in the property block / show up in the Material Inspector
        internal virtual void GetPropertyDisplayNames(List<string> result)
        {
            result.Add(displayName);
        }

        // the simple interface for simple properties
        internal virtual string GetPropertyBlockString()
        {
            return string.Empty;
        }

        // the more complex interface for complex properties (defaulted for simple properties)
        internal virtual void AppendPropertyBlockStrings(ShaderStringBuilder builder)
        {
            builder.AppendLine(GetPropertyBlockString());
        }

        // TODO: name modifier callback?
        internal abstract void ForeachHLSLProperty(Action<HLSLProperty> action);


        internal abstract string GetPropertyAsArgumentString();
        internal abstract AbstractMaterialNode ToConcreteNode();
        internal abstract PreviewProperty GetPreviewMaterialProperty();
        internal virtual bool isGpuInstanceable => false;

        public virtual string GetPropertyTypeString()
        {
            string depString = $" (Deprecated{(ShaderGraphPreferences.allowDeprecatedBehaviors ? " V" + sgVersion : "" )})" ;
            return propertyType.ToString() + (sgVersion < latestVersion ? depString : "");
        }
    }
    
    [Serializable]
    public abstract class AbstractShaderProperty<T> : AbstractShaderProperty
    {
        [SerializeField]
        T m_Value;

        public virtual T value
        {
            get => m_Value;
            set => m_Value = value;
        }
    }
}
