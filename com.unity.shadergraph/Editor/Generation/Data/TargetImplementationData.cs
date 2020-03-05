using System;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing;

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

        internal void DirtyTarget()
        {
            // TODO: Force recompilation here...
        }

        // TODO: Should we have the GUI implementation integrated in this way?
        // TODO: Also I currently use this to rebuild the inspector
        // TODO: How are we going to update the inspector when the data object is changed? (Sai)
        internal abstract void GetProperties(PropertySheet propertySheet, InspectorView inspectorView);
    }
}
