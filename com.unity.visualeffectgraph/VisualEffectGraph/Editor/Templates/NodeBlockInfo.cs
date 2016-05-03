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
    internal class NodeBlockInfo : ScriptableObject
    {
        public string BlockName { get { return m_BlockName; } set { m_BlockName = value; } }
        public string BlockLibraryName { get { return m_BlockLibraryName; } set { m_BlockLibraryName = value; } }

        public Dictionary<string, VFXValue> ParameterOverrides { get { return m_ParameterOverrides; } }
        [SerializeField]
        private Dictionary<string, VFXValue> m_ParameterOverrides;
        [SerializeField]
        private string m_BlockName;
        [SerializeField]
        private string m_BlockLibraryName;

        public static NodeBlockInfo Create(string blockname, string blocklibraryname)
        {
            NodeBlockInfo ni = CreateInstance<NodeBlockInfo>();
            ni.BlockName = blockname;
            ni.BlockLibraryName = blocklibraryname;
            return ni;
        }

        public NodeBlockInfo() {
            m_ParameterOverrides = new Dictionary<string, VFXValue>();
        }

        public void AddParameterOverride(string name, VFXValue value)
        {
            m_ParameterOverrides.Add(name, value);
        }
    }
}
