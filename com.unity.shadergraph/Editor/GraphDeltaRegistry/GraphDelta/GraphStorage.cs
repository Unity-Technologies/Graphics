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

        public PortHandler InputPort => owner.GetHandler(input) as PortHandler;
        public PortHandler OuptutPort => owner.GetHandler(output) as PortHandler;
    }

    internal sealed partial class GraphStorage : CLDS
    {
        protected override void AddDefaultLayers()
        {
            AddLayer(0, GraphDelta.k_concrete, false);
            AddLayer(1, GraphDelta.k_user,     true);
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

        internal NodeHandler AddNodeHandler(string layer, ElementID elementId)
        {
            return AddHandler(layer, elementId, new NodeHeader()) as NodeHandler;
        }

        new internal Element GetLayerRoot(string layer)
        {
            return base.GetLayerRoot(layer);
        }

        new internal Element SearchRelative(Element element, ElementID elementID)
        {
            return base.SearchRelative(element, elementID);
        }

        new internal Element AddElementToLayer(string layer, ElementID elementID)
        {
            return base.AddElementToLayer(layer, elementID);
        }

        new internal void SetHeader(Element element, DataHeader header)
        {
            base.SetHeader(element, header);
        }

        new internal void RemoveDataBranch(Element element)
        {
            base.RemoveDataBranch(element);
        }
    }
}
