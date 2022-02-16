using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor.ContextLayeredDataStorage;
using CLDS = UnityEditor.ContextLayeredDataStorage.ContextLayeredDataStorage;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public interface IEdgeHandler
    {
        public PortHandler InputPort { get; }
        public PortHandler OuptutPort { get; }
    }

    internal struct EdgeHandler : IEdgeHandler
    {
        public ElementID input;
        public ElementID output;
        public GraphStorage owner;

        public PortHandler InputPort => owner.GetHandler(input);
        public PortHandler OuptutPort => owner.GetHandler(output);
    }

    internal sealed partial class GraphStorage : CLDS
    {
        public const string k_concrete = "Concrete";
        public const string k_user = "User";

        protected override void AddDefaultLayers()
        {
            AddLayer(0, k_concrete, false);
            AddLayer(1, k_user,     true);
        }

        internal GraphDataHandler GetHandler(ElementID elementID)
        {
            return new GraphDataHandler(elementID, this);
        }

        internal GraphDataHandler AddHandler(string layer, ElementID elementID, DataHeader header)
        {
            var output = new GraphDataHandler(elementID, this);
            output.GetWriter(layer).SetHeader(header);
            return output;
        }

        internal void RemoveHandler(string layer, ElementID elementID)
        {
            var elem = SearchRelative(GetLayerRoot(layer), elementID);
            if(elem != null)
            {
                RemoveDataBranch(elem);
            }
        }

        internal GraphDataHandler AddNodeHandler(string layer, ElementID elementId)
        {
            return AddHandler(layer, elementId, new NodeHeader());
        }
    }
}
