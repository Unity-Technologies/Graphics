using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    abstract class AbstractShaderProperty
    {
        [SerializeField]
        private string m_Name;

        [SerializeField]
        private bool m_GeneratePropertyBlock = true;

        [SerializeField]
        private SerializableGuid m_Guid = new SerializableGuid();

        public Guid guid
        {
            get { return m_Guid.guid; }
        }
        
        public string displayName
        {
            get
            {
                if (string.IsNullOrEmpty(m_Name))
                    return guid.ToString();
                return m_Name;
            }
            set { m_Name = value; }
        }

        [SerializeField]
        string m_DefaultReferenceName;

        public string referenceName
        {
            get
            {
                if (string.IsNullOrEmpty(overrideReferenceName))
                {
                    if (string.IsNullOrEmpty(m_DefaultReferenceName))
                        m_DefaultReferenceName = string.Format("{0}_{1}", propertyType, GuidEncoder.Encode(guid));
                    return m_DefaultReferenceName;
                }
                return overrideReferenceName;
            }
        }
        
        [SerializeField]
        string m_OverrideReferenceName;

        public string overrideReferenceName
        {
            get { return m_OverrideReferenceName; }
            set { m_OverrideReferenceName = value; }
        }

        public bool generatePropertyBlock
        {
            get { return m_GeneratePropertyBlock; }
            set { m_GeneratePropertyBlock = value; }
        }

        private ConcretePrecision m_ConcretePrecision = ConcretePrecision.Float;

        public ConcretePrecision concretePrecision => m_ConcretePrecision;

        [SerializeField]
        private Precision m_Precision = Precision.Inherit;

        public Precision precision
        {
            get => m_Precision;
            set => m_Precision = value;
        }

        public abstract PropertyType propertyType { get; }
        
        public abstract Vector4 defaultValue { get; }
        public abstract bool isBatchable { get; }
        public abstract bool isExposable { get; }
        public abstract bool isRenamable { get; }
        public abstract string GetPropertyBlockString();
        public abstract string GetPropertyDeclarationString(string delimiter = ";");

        public virtual string GetPropertyAsArgumentString()
        {
            return GetPropertyDeclarationString(string.Empty);
        }

        public void SetConcretePrecision(ConcretePrecision inheritedPrecision)
        {
            m_ConcretePrecision = (precision == Precision.Inherit) ? inheritedPrecision : precision.ToConcrete();
        }

        public abstract PreviewProperty GetPreviewMaterialProperty();
        public abstract AbstractMaterialNode ToConcreteNode();
        public abstract AbstractShaderProperty Copy();
    }
    
    [Serializable]
    abstract class AbstractShaderProperty<T> : AbstractShaderProperty
    {
        [SerializeField]
        private T m_Value;

        public T value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }
    }
}
