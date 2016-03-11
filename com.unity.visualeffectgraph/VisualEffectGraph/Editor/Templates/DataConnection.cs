using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class DataConnection : ScriptableObject
    {
        public DataParamConnectorInfo Previous;
        public ContextParamConnectorInfo Next;

        public static DataConnection Create(DataParamConnectorInfo previous, ContextParamConnectorInfo next)
        {
            DataConnection fc = CreateInstance<DataConnection>();
            fc.Previous = previous;
            fc.Next = next;
            return fc;
        }

        public DataConnection() { }

    }

    internal class ContextParamConnectorInfo
    {
        internal ContextNodeInfo m_Node;
        public int m_NodeBlockIndex;
        public string m_ParameterName;
        public ContextParamConnectorInfo(ContextNodeInfo node, int blockIndex, string parameterName)
        {
            m_Node = node;
            m_NodeBlockIndex = blockIndex;
            m_ParameterName = parameterName;
        }
    }

    internal class DataParamConnectorInfo
    {
        internal DataNodeInfo m_Node;
        public int m_NodeBlockIndex;
        public string m_ParameterName;
        public DataParamConnectorInfo(DataNodeInfo node, int blockIndex, string parameterName)
        {
            m_Node = node;
            m_NodeBlockIndex = blockIndex;
            m_ParameterName = parameterName;
        }
    }
}
