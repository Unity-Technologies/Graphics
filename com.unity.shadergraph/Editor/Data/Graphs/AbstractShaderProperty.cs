using System;
using System.Collections.Generic;
using System.Linq;
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

        internal protected static readonly string s_UnityPerMaterialCbName = "UnityPerMaterial";

        public virtual IEnumerable<(string cbName, string line)> GetPropertyDeclarationStrings()
        {
            yield return (s_UnityPerMaterialCbName, $"{concreteShaderValueType.ToShaderString(concretePrecision.ToShaderString())} {referenceName}");
        }

        public virtual string GetPropertyAsArgumentString()
            => GetPropertyDeclarationStrings().Select(v => v.line).Aggregate((result, current) => $"{result}, {current}");
        
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
