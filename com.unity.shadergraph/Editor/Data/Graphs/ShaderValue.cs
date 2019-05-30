using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    abstract class ShaderValue
    {
#region Guid
        [SerializeField]
        private SerializableGuid m_Guid = new SerializableGuid();

        public Guid guid => m_Guid.guid;
#endregion

#region Name
        [SerializeField]
        private string m_Name;

        [SerializeField]
        private string m_DefaultReferenceName;

        [SerializeField]
        private string m_OverrideReferenceName;
        
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

        public string overrideReferenceName
        {
            get => m_OverrideReferenceName;
            set => m_OverrideReferenceName = value;
        }
#endregion

#region Type
        public abstract ConcreteSlotValueType concreteShaderValueType { get; }
#endregion
    }
}
