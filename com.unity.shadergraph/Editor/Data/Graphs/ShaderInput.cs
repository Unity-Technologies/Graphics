using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    abstract class ShaderInput
    {
        [SerializeField]
        SerializableGuid m_Guid = new SerializableGuid();

        public Guid guid => m_Guid.guid;
        
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
                        m_DefaultReferenceName = $"{referenceNameBase}_{GuidEncoder.Encode(guid)}";
                    return m_DefaultReferenceName;
                }
                return overrideReferenceName;
            }
        }

        // This is required to handle Material data serialized with "_Color_GUID" reference names
        // m_DefaultReferenceName expects to match the material data and previously used PropertyType
        // ColorShaderProperty is the only case where PropertyType doesnt match ConcreteSlotValueType
        public virtual string referenceNameBase => concreteShaderValueType.ToString();

        [SerializeField]
        string m_OverrideReferenceName;

        public string overrideReferenceName
        {
            get => m_OverrideReferenceName;
            set => m_OverrideReferenceName = value;
        }

        [SerializeField]
        bool m_GeneratePropertyBlock = true;

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
