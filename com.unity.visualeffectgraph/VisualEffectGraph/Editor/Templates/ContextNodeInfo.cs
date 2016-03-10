using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class ContextNodeInfo : ScriptableObject
    {
        public Dictionary<string, NodeBlockInfo> nodeBlocks;
        public string Context {get { return m_Context; } set { m_Context = value; } }
        public Dictionary<string, VFXParamValue> ParameterOverrides { get { return m_ParameterOverrides; } }

        [SerializeField]
        private string m_Context;
        [SerializeField]
        private Dictionary<string, VFXParamValue> m_ParameterOverrides;

        public static ContextNodeInfo Create(string Context)
        {
            ContextNodeInfo n =  CreateInstance<ContextNodeInfo>();
            n.Context = Context;
            return n;
        }

        public ContextNodeInfo()
        {
            nodeBlocks = new Dictionary<string, NodeBlockInfo>();
            m_ParameterOverrides = new Dictionary<string, VFXParamValue>();
        }

        public void AddParameterOverride(string name, VFXParamValue ParamValue)
        {
            m_ParameterOverrides.Add(name, ParamValue);
        }
    }
}
