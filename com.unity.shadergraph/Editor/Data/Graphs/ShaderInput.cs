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

        public abstract ConcreteSlotValueType concreteShaderValueType { get; }

        public abstract ShaderInput Copy();
    }
}
