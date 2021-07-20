using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal sealed class GraphStorage : ContextLayeredDataStorage
    {
        public List<Element> nodes = new List<Element>();
        public NodeRef AddNode(string name)
        {
            var elem = AddData(name);
            nodes.Add(elem);
            NodeRef output = new NodeRef(elem);
            return output;
        }

        public NodeRef GetNode(string name)
        {
            Element n = Search(name);
            return new NodeRef(n);
        }

    }
}
