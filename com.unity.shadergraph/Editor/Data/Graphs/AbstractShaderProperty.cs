using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    // describes the different ways we can generate HLSL declarations
    [Flags]
    enum PropertyHLSLGenerationType
    {
        None = 0,                       // NOT declared in HLSL
        Global = 1 << 0,                // declared in the global scope, mainly for use with state coming from Shader.SetGlobal*()
        UnityPerMaterial = 1 << 1,      // declared in the UnityPerMaterial cbuffer, populated by Material or MaterialPropertyBlock
        HybridRenderer = 1 << 2,        // declared using HybridRenderer path (v1 or v2) to get DOTS GPU instancing
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
        internal abstract void AppendPropertyDeclarations(ShaderStringBuilder builder, Func<string, string> nameModifier, PropertyHLSLGenerationType generationTypes);


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
