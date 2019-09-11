using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    public abstract class ShaderInput
    {
        [SerializeField]
        SerializableGuid m_Guid = new SerializableGuid();

        internal Guid guid => m_Guid.guid;
        
        [SerializeField]
        string m_Name;

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
        string m_DefaultReferenceName;

        public string referenceName
        {
            get
            {
                if (string.IsNullOrEmpty(overrideReferenceName))
                {
                    if (string.IsNullOrEmpty(m_DefaultReferenceName))
                        m_DefaultReferenceName = GetDefaultReferenceName();
                    return m_DefaultReferenceName;
                }
                return overrideReferenceName;
            }
        }

        // This is required to handle Material data serialized with "_Color_GUID" reference names
        // m_DefaultReferenceName expects to match the material data and previously used PropertyType
        // ColorShaderProperty is the only case where PropertyType doesnt match ConcreteSlotValueType
        public virtual string GetDefaultReferenceName()
        {
            return $"{concreteShaderValueType.ToString()}_{GuidEncoder.Encode(guid)}";
        }

        [SerializeField]
        string m_OverrideReferenceName;

        internal string overrideReferenceName
        {
            get => m_OverrideReferenceName;
            set => m_OverrideReferenceName = value;
        }

        [SerializeField]
        bool m_GeneratePropertyBlock = true;

        internal bool generatePropertyBlock
        {
            get => m_GeneratePropertyBlock;
            set => m_GeneratePropertyBlock = value;
        }

        internal abstract ConcreteSlotValueType concreteShaderValueType { get; }
        internal abstract bool isExposable { get; }
        internal abstract bool isRenamable { get; }

        internal abstract ShaderInput Copy();
    }
}
