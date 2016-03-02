using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class NodeBlockInfo : ScriptableObject
    {
        public string BlockName { get { return m_BlockName; } set { m_BlockName = value; } }
        public Dictionary<string, VFXParamValue> ParameterOverrides { get { return m_ParameterOverrides; } }
        [SerializeField]
        private Dictionary<string, VFXParamValue> m_ParameterOverrides;
        [SerializeField]
        private string m_BlockName;

        public static NodeBlockInfo Create(string blockname)
        {
            NodeBlockInfo ni = ScriptableObject.CreateInstance<NodeBlockInfo>();
            ni.BlockName = blockname;
            return ni;
        }

        public NodeBlockInfo() {
            m_ParameterOverrides = new Dictionary<string, VFXParamValue>();
        }
            
        public void AddParameterOverride(string name, VFXParamValue ParamValue)
        {
            m_ParameterOverrides.Add(name, ParamValue);
        }
    }
}
