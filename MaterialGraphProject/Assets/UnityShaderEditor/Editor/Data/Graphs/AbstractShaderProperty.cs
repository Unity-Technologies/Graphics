using System;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public abstract class AbstractShaderProperty<T> : ISerializationCallbackReceiver, IShaderProperty
    {
        [SerializeField]
        private T m_Value;

        [SerializeField]
        private string m_Description;

        [SerializeField]
        private string m_Name;

        [NonSerialized]
        private Guid m_Guid;

        [SerializeField]
        private string m_GuidSerialized;

        [SerializeField]
        private bool m_GeneratePropertyBlock = true;

        protected AbstractShaderProperty()
        {
            m_Guid = Guid.NewGuid();
        }

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
                    return m_Guid.ToString();
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
            get { return m_Guid; }
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

        public virtual void OnBeforeSerialize()
        {
            m_GuidSerialized = m_Guid.ToString();
        }

        public virtual void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(m_GuidSerialized))
                m_Guid = new Guid(m_GuidSerialized);
            else
                m_Guid = Guid.NewGuid();
        }
    }
}
