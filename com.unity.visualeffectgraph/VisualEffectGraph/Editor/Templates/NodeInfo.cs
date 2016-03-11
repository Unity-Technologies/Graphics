using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal abstract class NodeInfo : ScriptableObject
    { 
        public Dictionary<string, VFXParamValue> ParameterOverrides { get { return m_ParameterOverrides; } }
        [SerializeField]
        private Dictionary<string, VFXParamValue> m_ParameterOverrides;

        public string m_UniqueName;
            
        public NodeInfo()
        {
            m_ParameterOverrides = new Dictionary<string, VFXParamValue>();
        }

        public void AddParameterOverride(string name, VFXParamValue ParamValue)
        {
            m_ParameterOverrides.Add(name, ParamValue);
        }
    }
}
