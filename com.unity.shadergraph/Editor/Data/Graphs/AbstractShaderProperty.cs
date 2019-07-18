using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    abstract class AbstractShaderProperty : ShaderInput
    {
        public abstract PropertyType propertyType { get; }

        public override ConcreteSlotValueType concreteShaderValueType => propertyType.ToConcreteShaderValueType();

        [SerializeField]
        Precision m_Precision = Precision.Inherit;
        
        ConcretePrecision m_ConcretePrecision = ConcretePrecision.Float;

        public Precision precision
        {
            get => m_Precision;
            set => m_Precision = value;
        }

        public ConcretePrecision concretePrecision => m_ConcretePrecision;

        public void ValidateConcretePrecision(ConcretePrecision graphPrecision)
        {
            m_ConcretePrecision = (precision == Precision.Inherit) ? graphPrecision : precision.ToConcrete();
        }

        public abstract bool isBatchable { get; }

        [SerializeField]
        bool m_Hidden = false;

        public bool hidden
        {
            get => m_Hidden;
            set => m_Hidden = value;
        }

        public string hideTagString => hidden ? "[HideInInspector]" : "";

        public virtual string GetPropertyBlockString()
        {
            return string.Empty;
        }

        public virtual string GetPropertyDeclarationString(string delimiter = ";")
        {
            SlotValueType type = ConcreteSlotValueType.Vector4.ToSlotValueType();
            return $"{concreteShaderValueType.ToShaderString(concretePrecision.ToShaderString())} {referenceName}{delimiter}";
        }

        public enum GenerationMode
        {
            InConstantBuffer,
            InRoot
        };

        // Most properties just go either in the constant buffer or in the root. So we just delegate to the function
        // and make the other path return null so nothing gets included in the other.
        // Some properties must go in both root and cb so these will need to override this function 
        public virtual string GetPropertyDeclarationStringForBatchMode(GenerationMode mode, string delimiter = ";")
        {
            if (mode == GenerationMode.InConstantBuffer)
            {
                if (isBatchable && generatePropertyBlock)
                {
                    return GetPropertyDeclarationString(delimiter);
                }
            }
            else if (mode == GenerationMode.InRoot)
            {
                if (!isBatchable || !generatePropertyBlock)
                {
                    return GetPropertyDeclarationString(delimiter);
                }
            }
            else
            {
                throw new Exception("Bad Generation Mode");
            }

            return null;
        }

        public virtual string GetPropertyAsArgumentString()
        {
            return GetPropertyDeclarationString(string.Empty);
        }
        
        public abstract AbstractMaterialNode ToConcreteNode();
        public abstract PreviewProperty GetPreviewMaterialProperty();
    }
    
    [Serializable]
    abstract class AbstractShaderProperty<T> : AbstractShaderProperty
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
