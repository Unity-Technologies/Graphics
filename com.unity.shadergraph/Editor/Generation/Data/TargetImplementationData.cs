using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    internal abstract class TargetImplementationData
    {
        [SerializeField]
        string m_ImplementationName;
        
        internal string implementationName
        { 
            get => m_ImplementationName;
            set => m_ImplementationName = value;
        }
    }
}
