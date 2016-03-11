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
        public ContextNodeInfo Previous;
        public ContextNodeInfo Next;

        public static FlowConnection Create(ContextNodeInfo input, ContextNodeInfo output)
        {
            FlowConnection fc = CreateInstance<FlowConnection>();
            fc.Previous = input;
            fc.Next = output;
            return fc;
        }

        public FlowConnection() { }

    }
}
