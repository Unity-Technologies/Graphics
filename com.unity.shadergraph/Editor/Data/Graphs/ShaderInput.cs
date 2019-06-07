using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    abstract class ShaderInput
    {
        [SerializeField]
        private SerializableGuid m_Guid = new SerializableGuid();

        public Guid guid => m_Guid.guid;
        
        [SerializeField]
        private string m_Name;

        public string displayName
        {
            get
            {
                if (string.IsNullOrEmpty(m_Name))
                    return $"{concreteShaderValueType}_{GuidEncoder.Encode(guid)}";
                return m_Name;
            }
            set => m_Name = value;
        }

        [SerializeField]
        private string m_DefaultReferenceName;

        public virtual string referenceName
        {
            get
            {
                if (string.IsNullOrEmpty(overrideReferenceName))
                {
                    if (string.IsNullOrEmpty(m_DefaultReferenceName))
                        m_DefaultReferenceName = $"{concreteShaderValueType}_{GuidEncoder.Encode(guid)}";
                    return m_DefaultReferenceName;
                }
                return overrideReferenceName;
            }
        }

        [SerializeField]
        private string m_OverrideReferenceName;

        public string overrideReferenceName
        {
            get => m_OverrideReferenceName;
            set => m_OverrideReferenceName = value;
        }

        [SerializeField]
        private bool m_GeneratePropertyBlock = true;

        public bool generatePropertyBlock
        {
            get => m_GeneratePropertyBlock;
            set => m_GeneratePropertyBlock = value;
        }

        public abstract ConcreteSlotValueType concreteShaderValueType { get; }
        public abstract bool isExposable { get; }
        public abstract bool isRenamable { get; }

        public abstract ShaderInput Copy();
    }
}
