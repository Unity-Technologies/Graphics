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

        [SerializeField]
        Precision m_Precision = Precision.Inherit;
        
        [SerializeField]
        private bool m_GPUInstanced = false;

        public bool gpuInstanced
        {
            get { return m_GPUInstanced; }
            set { m_GPUInstanced = value; }
        }

        ConcretePrecision m_ConcretePrecision = ConcretePrecision.Float;

        internal Precision precision
        {
            get => m_Precision;
            set => m_Precision = value;
        }

        public ConcretePrecision concretePrecision => m_ConcretePrecision;

        internal void ValidateConcretePrecision(ConcretePrecision graphPrecision)
        {
            m_ConcretePrecision = (precision == Precision.Inherit) ? graphPrecision : precision.ToConcrete();
        }

        // the simple interface for simple properties
        internal abstract bool isBatchable { get; }

        // the more complex interface for complex properties (defaulted for simple properties)
        internal virtual bool hasBatchableProperties { get { return isBatchable; } }
        internal virtual bool hasNonBatchableProperties { get { return !isBatchable; } }

        [SerializeField]
        bool m_Hidden = false;

        public bool hidden
        {
            get => m_Hidden;
            set => m_Hidden = value;
        }

        internal string hideTagString => hidden ? "[HideInInspector]" : "";

        // simple properties use a single reference name; this function covers that case
        // complex properties can override this function to produce multiple reference names
        internal virtual void ForEachPropertyReferenceName(Func<string, string> func)
        {
            overrideReferenceName = func(referenceName);
        }
        internal virtual void ForEachPropertyDisplayName(Func<string, string> func)
        {
            displayName = func(displayName);
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

        // the simple interface for simple properties
        internal virtual string GetPropertyDeclarationString(string delimiter = ";")
        {
            SlotValueType type = ConcreteSlotValueType.Vector4.ToSlotValueType();
            return $"{concreteShaderValueType.ToShaderString(concretePrecision.ToShaderString())} {referenceName}{delimiter}";
        }

        // the more complex interface for complex properties (defaulted for simple properties)
        internal virtual void AppendBatchablePropertyDeclarations(ShaderStringBuilder builder, string delimiter = ";")
        {
            if (isBatchable)
                builder.AppendLine(GetPropertyDeclarationString(delimiter));
        }

        // the more complex interface for complex properties (defaulted for simple properties)
        internal virtual void AppendNonBatchablePropertyDeclarations(ShaderStringBuilder builder, string delimiter = ";")
        {
            if (!isBatchable)
                builder.AppendLine(GetPropertyDeclarationString(delimiter));
        }

        internal virtual string GetPropertyAsArgumentString()
        {
            return GetPropertyDeclarationString(string.Empty);
        }
        
        internal abstract AbstractMaterialNode ToConcreteNode();
        internal abstract PreviewProperty GetPreviewMaterialProperty();
        internal virtual bool isGpuInstanceable => false;
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
