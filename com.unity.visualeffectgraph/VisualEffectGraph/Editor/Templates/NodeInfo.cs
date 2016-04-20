using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal abstract class NodeInfo : ScriptableObject
    { 
        public Dictionary<string, VFXValue> ParameterOverrides { get { return m_ParameterOverrides; } }
        [SerializeField]
        private Dictionary<string, VFXValue> m_ParameterOverrides;

        public string m_UniqueName;
            
        public NodeInfo()
        {
            m_ParameterOverrides = new Dictionary<string, VFXValue>();
        }

        public void AddParameterOverride(string name, VFXValue value)
        {
            m_ParameterOverrides.Add(name, value);
        }
    }
}
