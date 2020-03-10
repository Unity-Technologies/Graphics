using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    internal abstract class TargetImplementationData
    {
        [SerializeField]
        string m_SerializedImplementation;

        [NonSerialized]
        ITargetImplementation m_Implementation;
        
        internal string serializedImplementation
        { 
            get => m_SerializedImplementation;
            set => m_SerializedImplementation = value;
        }

        internal ITargetImplementation implementation
        { 
            get => m_Implementation;
            set => m_Implementation = value;
        }

        internal void Init(ITargetImplementation implementation)
        {
            m_Implementation = implementation;
        }
    }
}
