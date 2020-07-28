using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    public abstract class AbstractShaderProperty : ShaderInput
    {

        internal enum CBufferUsage
        {
            PerMaterial,
            HybridRenderer,
            Excluded
        }

        public abstract PropertyType propertyType { get; }

        internal override ConcreteSlotValueType concreteShaderValueType => propertyType.ToConcreteShaderValueType();

        [SerializeField]
        Precision m_Precision = Precision.Inherit;
        

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
        internal bool isBatchable { get => SupportsCBufferUsage(CBufferUsage.PerMaterial) || SupportsCBufferUsage(CBufferUsage.HybridRenderer); }

        internal abstract bool SupportsCBufferUsage(CBufferUsage usage);

        [SerializeField]
        private CBufferUsage m_CBufferUsage = CBufferUsage.Excluded;
        internal CBufferUsage cBufferUsage
        {
            get => m_CBufferUsage;
            set
            {
                if(value == m_CBufferUsage)
                {
                    return;
                }

                if(SupportsCBufferUsage(value))
                {
                    m_CBufferUsage = value;
                }
                else
                {
                    Debug.LogError("Cannot set CBufferUsage to unsupported " + value.ToString());
                }
            }
        }



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
        internal virtual void GetPropertyReferenceNames(List<string> result)
        {
            result.Add(referenceName);
        }
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
        internal virtual void AppendPropertyBlockStrings(ShaderStringBuilder builder, bool hidden = false)
        {
            builder.AppendLine((hidden ? hideTagString : "") + GetPropertyBlockString());
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
