using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class FlowConnection : ScriptableObject
    {
        public NodeInfo Previous;
        public NodeInfo Next;

        public static FlowConnection Create(NodeInfo input, NodeInfo output)
        {
            FlowConnection fc = ScriptableObject.CreateInstance<FlowConnection>();
            fc.Previous = input;
            fc.Next = output;
            return fc;
        }

        public FlowConnection() { }

    }
}
