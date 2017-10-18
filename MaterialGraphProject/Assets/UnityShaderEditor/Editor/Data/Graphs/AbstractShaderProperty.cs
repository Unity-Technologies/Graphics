using System;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public abstract class AbstractShaderProperty<T> : IShaderProperty
    {
        [SerializeField]
        private T m_Value;

        [SerializeField]
        private string m_Name;

        [SerializeField]
        private bool m_GeneratePropertyBlock = true;

        [SerializeField]
        private SerializableGuid m_Guid = new SerializableGuid();

        public T value
        {
            get { return m_Value; }
            set { m_Value = value; }
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

        public string referenceName
        {
            get
            {
                return string.IsNullOrEmpty(overrideReferenceName)
                    ? string.Format("{0}_{1}", propertyType, GuidEncoder.Encode(guid))
                    : overrideReferenceName;
            }
        }

        public string overrideReferenceName { get; set; }

        public abstract PropertyType propertyType { get; }

        public Guid guid
        {
            get { return m_Guid.guid; }
        }

        public bool generatePropertyBlock
        {
            get { return m_GeneratePropertyBlock; }
            set { m_GeneratePropertyBlock = value; }
        }

        public abstract Vector4 defaultValue { get; }
        public abstract string GetPropertyBlockString();
        public abstract string GetPropertyDeclarationString();

        public virtual string GetInlinePropertyDeclarationString()
        {
            return GetPropertyDeclarationString();
        }

        public abstract PreviewProperty GetPreviewMaterialProperty();
    }
}
