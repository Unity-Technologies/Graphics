using System;
using System.Linq;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    public abstract class ShaderInput : JsonObject
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
                    return $"{concreteShaderValueType}_{objectId}";
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
            return $"{concreteShaderValueType.ToString()}_{objectId}";
        }

        [SerializeField]
        string m_OverrideReferenceName;

        internal string overrideReferenceName
        {
            get => m_OverrideReferenceName;
            set => m_OverrideReferenceName = value;
        }

        internal enum InputLevelDescriptor
        {
            PerMaterial,
            Global,
            HybridInstanced
        }

        [SerializeField]
        InputLevelDescriptor m_InputLevelDescriptor = InputLevelDescriptor.PerMaterial;

        internal InputLevelDescriptor inputLevelDescriptor { get => m_InputLevelDescriptor; set => m_InputLevelDescriptor = value; }

        internal abstract ConcreteSlotValueType concreteShaderValueType { get; }
        internal abstract bool isRenamable { get; }

        internal enum PropertyBlockUsage
        {
            Included,
            Hidden,
            Excluded
        }

        internal abstract bool SupportsBlockUsage(PropertyBlockUsage usage); 

        [SerializeField]
        private PropertyBlockUsage m_PropertyBlockUsage = PropertyBlockUsage.Excluded;
        internal PropertyBlockUsage propertyBlockUsage
        {
            get => m_PropertyBlockUsage;
            set
            {
                if(value == m_PropertyBlockUsage)
                {
                    return;
                }

                if(SupportsBlockUsage(value))
                {
                    m_PropertyBlockUsage = value;
                }
                else
                {
                    Debug.LogError("Cannot set PropertyBlockUsage to unsupported " + value.ToString());
                }
            }
        }



        internal abstract ShaderInput Copy();
    }
}
