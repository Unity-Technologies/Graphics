using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    public abstract class AbstractShaderProperty : ShaderInput
    {
        public abstract PropertyType propertyType { get; }

        internal override ConcreteSlotValueType concreteShaderValueType => propertyType.ToConcreteShaderValueType();

        // user selected precision setting
        [SerializeField]
        Precision m_Precision = Precision.Inherit;

        [Obsolete("AbstractShaderProperty.gpuInstanced is no longer used")]
        public bool gpuInstanced
        {
            get { return false; }
            set { }
        }

        internal virtual string GetHLSLVariableName(bool isSubgraphProperty, GenerationMode mode)
        {
            if (mode == GenerationMode.VFX)
            {
                // Per-element exposed properties are provided by the properties structure filled by VFX.
                if (overrideHLSLDeclaration && hlslDeclarationOverride != HLSLDeclaration.Global)
                    return $"PROP.{referenceName}";
                // For un-exposed global properties, just read from the cbuffer.
                else
                    return referenceName;
            }

            return referenceName;
        }

        internal string GetConnectionStateHLSLVariableName()
        {
            return GetConnectionStateVariableName(referenceName + "_" + objectId);
        }

        // NOTE: this does not tell you the HLSLDeclaration of the entire property...
        // instead, it tells you what the DEFAULT HLSL Declaration would be, IF the property makes use of the default
        // to check ACTUAL HLSL Declaration types, enumerate the HLSL Properties and check their HLSLDeclarations...
        internal virtual HLSLDeclaration GetDefaultHLSLDeclaration()
        {
            if (overrideHLSLDeclaration)
                return hlslDeclarationOverride;
            // default Behavior switches between UnityPerMaterial and Global based on Exposed checkbox
            if (generatePropertyBlock)
                return HLSLDeclaration.UnityPerMaterial;
            else
                return HLSLDeclaration.Global;
        }

        // by default we disallow UI from choosing "DoNotDeclare"
        // it needs a bit more UI support to disable property node output slots before we make it public
        internal virtual bool AllowHLSLDeclaration(HLSLDeclaration decl) => (decl != HLSLDeclaration.DoNotDeclare);

        [SerializeField]
        internal bool overrideHLSLDeclaration = false;

        [SerializeField]
        internal HLSLDeclaration hlslDeclarationOverride;

        internal Precision precision
        {
            get => m_Precision;
            set => m_Precision = value;
        }

        ConcretePrecision m_ConcretePrecision = ConcretePrecision.Single;
        public ConcretePrecision concretePrecision => m_ConcretePrecision;
        internal void SetupConcretePrecision(ConcretePrecision defaultPrecision)
        {
            m_ConcretePrecision = precision.ToConcrete(defaultPrecision, defaultPrecision);
        }

        [SerializeField]
        bool m_Hidden = false;
        public bool hidden
        {
            get => m_Hidden;
            set => m_Hidden = value;
        }

        internal string hideTagString => hidden ? "[HideInInspector]" : "";

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

        internal abstract void ForeachHLSLProperty(Action<HLSLProperty> action);

        internal virtual string GetPropertyAsArgumentStringForVFX(string precisionString)
        {
            return GetPropertyAsArgumentString(precisionString);
        }

        internal abstract string GetPropertyAsArgumentString(string precisionString);
        internal abstract AbstractMaterialNode ToConcreteNode();
        internal abstract PreviewProperty GetPreviewMaterialProperty();

        public virtual string GetPropertyTypeString()
        {
            var typeString = propertyType.ToString();
            if (sgVersion < latestVersion)
                typeString = $"{typeString} (Legacy v{sgVersion})";
            return typeString;
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

    // class for extracting deprecated data from older versions of AbstractShaderProperty
    class LegacyShaderPropertyData
    {
        // indicates user wishes to support the HYBRID renderer GPU instanced path
        [SerializeField]
        public bool m_GPUInstanced = false;

        // converts the old m_GPUInstanced data into the new override HLSLDeclaration system.
        public static void UpgradeToHLSLDeclarationOverride(string json, AbstractShaderProperty property)
        {
            // this maintains the old behavior for versioned properties:
            //      old exposed GPUInstanced properties are declared hybrid (becomes override in new system)
            //      old unexposed GPUInstanced properties are declared global (becomes override in new system)
            //      old exposed properties are declared UnityPerMaterial (default behavior, no override necessary)
            //      old unexposed properties are declared Global (default behavior, no override necessary)
            // moving forward, users can use the overrides directly to control what it does

            var legacyShaderPropertyData = new LegacyShaderPropertyData();
            JsonUtility.FromJsonOverwrite(json, legacyShaderPropertyData);
            if (legacyShaderPropertyData.m_GPUInstanced)
            {
                property.overrideHLSLDeclaration = true;
                if (property.generatePropertyBlock)
                    property.hlslDeclarationOverride = HLSLDeclaration.HybridPerInstance;
                else
                    property.hlslDeclarationOverride = HLSLDeclaration.Global;
            }
        }
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
        DoNotDeclare,               // NOT declared in HLSL
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

        public bool ValueEquals(HLSLProperty other)
        {
            if ((name != other.name) ||
                (type != other.type) ||
                (precision != other.precision) ||
                (declaration != other.declaration) ||
                ((customDeclaration == null) != (other.customDeclaration == null)))
            {
                return false;
            }
            else if (customDeclaration != null)
            {
                var ssb = new ShaderStringBuilder();
                var ssbother = new ShaderStringBuilder();
                customDeclaration(ssb);
                other.customDeclaration(ssbother);
                if (ssb.ToCodeBlock() != ssbother.ToCodeBlock())
                    return false;
            }
            return true;
        }

        static string[,] kValueTypeStrings = new string[(int)HLSLType.FirstObjectType, 2]
        {
            {"float", "half"},
            {"float2", "half2"},
            {"float3", "half3"},
            {"float4", "half4"},
            {"float4x4", "half4x4"}
        };

        static string[] kObjectTypeStrings = new string[(int)HLSLType._CUSTOM - (int)HLSLType.FirstObjectType]
        {
            "TEXTURE2D",
            "TEXTURE3D",
            "TEXTURECUBE",
            "TEXTURE2D_ARRAY",
            "SAMPLER",
        };

        public bool IsObjectType()
        {
            return type == HLSLType._SamplerState ||
                type == HLSLType._Texture2D ||
                type == HLSLType._Texture3D ||
                type == HLSLType._TextureCube ||
                type == HLSLType._Texture2DArray;
        }

        public string GetValueTypeString()
        {
            if (type < HLSLType.FirstObjectType)
                return kValueTypeStrings[(int)type, (int)precision];
            return null;
        }

        public void AppendTo(ShaderStringBuilder ssb, Func<string, string> nameModifier = null)
        {
            var mName = nameModifier?.Invoke(name) ?? name;

            if (type < HLSLType.FirstObjectType)
            {
                ssb.Append(kValueTypeStrings[(int)type, (int)precision]);
                ssb.Append(" ");
                ssb.Append(mName);
                ssb.Append(";");
            }
            else if (type < HLSLType._CUSTOM)
            {
                ssb.Append(kObjectTypeStrings[type - HLSLType.FirstObjectType]);
                ssb.Append("(");
                ssb.Append(mName);
                ssb.Append(");");
            }
            else
            {
                customDeclaration(ssb);
            }
            //ssb.Append(" // ");
            //ssb.Append(declaration.ToString());
            ssb.AppendNewLine();
        }
    }
}
