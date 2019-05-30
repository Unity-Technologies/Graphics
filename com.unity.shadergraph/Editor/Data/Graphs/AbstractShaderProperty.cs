using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    abstract class AbstractShaderProperty : ShaderInput
    {
#region Type
        public abstract PropertyType propertyType { get; }
        public override ConcreteSlotValueType concreteShaderValueType => propertyType.ToConcreteShaderValueType();
#endregion

#region Precision
        [SerializeField]
        private Precision m_Precision = Precision.Inherit;
        
        private ConcretePrecision m_ConcretePrecision = ConcretePrecision.Float;

        public Precision precision
        {
            get => m_Precision;
            set => m_Precision = value;
        }

        public ConcretePrecision concretePrecision => m_ConcretePrecision;

        public void SetConcretePrecision(ConcretePrecision inheritedPrecision)
        {
            m_ConcretePrecision = (precision == Precision.Inherit) ? inheritedPrecision : precision.ToConcrete();
        }
#endregion

#region Capabilities
        public abstract bool isBatchable { get; }
        public abstract bool isExposable { get; }
        public abstract bool isRenamable { get; }
#endregion

#region PropertyBlock
        [SerializeField]
        private bool m_GeneratePropertyBlock = true;

        public bool generatePropertyBlock
        {
            get => m_GeneratePropertyBlock;
            set => m_GeneratePropertyBlock = value;
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
#endregion

#region ShaderValue
        public virtual string GetPropertyDeclarationString(string delimiter = ";")
        {
            SlotValueType type = ConcreteSlotValueType.Vector4.ToSlotValueType();
            return $"{concreteShaderValueType.ToShaderString(concretePrecision.ToShaderString())} {referenceName}{delimiter}";
        }

        public virtual string GetPropertyAsArgumentString()
        {
            return GetPropertyDeclarationString(string.Empty);
        }
#endregion

#region Utility
        public abstract PreviewProperty GetPreviewMaterialProperty();
        public abstract AbstractShaderProperty Copy();
#endregion
    }
    
    [Serializable]
    abstract class AbstractShaderProperty<T> : AbstractShaderProperty
    {
#region ShaderValue
        [SerializeField]
        private T m_Value;

        public T value
        {
            get => m_Value;
            set => m_Value = value;
        }
#endregion
    }
}
