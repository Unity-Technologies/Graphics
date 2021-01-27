using System;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    public abstract class ShaderInput : JsonObject
    {
        [SerializeField]
        SerializableGuid m_Guid = new SerializableGuid();

        internal Guid guid => m_Guid.guid;

        internal void OverrideGuid(string namespaceId, string name) { m_Guid.guid = GenerateNamespaceUUID(namespaceId, name); }

        [SerializeField]
        string m_Name;

        public string displayName
        {
            get
            {
                if (string.IsNullOrEmpty(m_Name))
                    return $"{concreteShaderValueType}_{objectId}";
                return m_Name;
            }
            set
            {
                m_Name = value;
                // Updates any views associated with this input so that display names stay up to date
                m_displayNameUpdateTrigger?.Invoke(m_Name);
            }
        }

        // This delegate and the associated callback can be bound to in order for any one that cares about display name changes to be notified
        internal delegate void ChangeDisplayNameCallback(string newDisplayName);
        ChangeDisplayNameCallback m_displayNameUpdateTrigger;
        internal ChangeDisplayNameCallback displayNameUpdateTrigger
        {
            get => m_displayNameUpdateTrigger;
            set => m_displayNameUpdateTrigger = value;
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
            return $"{concreteShaderValueType.ToString()}_{objectId}";
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
        internal virtual bool isAlwaysExposed => false;
        internal abstract bool isRenamable { get; }

        internal abstract ShaderInput Copy();
    }
}
