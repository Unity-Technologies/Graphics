using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor.ContextLayeredDataStorage;
using CLDS = UnityEditor.ContextLayeredDataStorage.ContextLayeredDataStorage;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    [Serializable]
    public class EdgeHandler
    {
        internal EdgeHandler(ElementID output, ElementID input, GraphDelta owner, Registry registry)
        {
            OutputID = output;
            InputID  = input;
            Owner    = owner;
            Registry = registry;
        }

        [SerializeField]
        public ElementID OutputID { get; internal set; }
        [SerializeField]
        public ElementID InputID  { get; internal set; }
        internal GraphDelta Owner { get; set; }
        internal Registry Registry { get; set; }

        public PortHandler OuptutPort => Owner.m_data.GetHandler(OutputID, Owner, Registry).ToPortHandler();
        public PortHandler InputPort  => Owner.m_data.GetHandler(InputID, Owner, Registry).ToPortHandler();
    }

    [Serializable]
    internal class Edge 
    {
        [SerializeField]
        private ElementID m_output;
        public ElementID Output { get => m_output; }
        [SerializeField]
        private ElementID m_input;
        public ElementID Input { get => m_input; }

        public Edge()
        {
        }
        public Edge(ElementID output, ElementID input) 
        {
            this.m_output = output;
            this.m_input = input;
        }

        public bool Equals(Edge obj)
        {
            return Output.Equals(obj.Output) && Input.Equals(obj.Input);
        }
    }

    [Serializable]
    internal sealed partial class GraphStorage : CLDS, ISerializationCallbackReceiver
    {
        [SerializeField]
        internal List<Edge> edges = new List<Edge>();

        protected override void AddDefaultLayers()
        {
            AddLayer(0, GraphDelta.k_concrete, false);
            AddLayer(1, GraphDelta.k_user,     true);
        }

        internal GraphDataHandler GetHandler(ElementID elementID, GraphDelta delta, Registry registry)
        {
            return new GraphDataHandler(elementID, delta, registry);
        }

        internal GraphDataHandler AddHandler(string layer, ElementID elementID, DataHeader header, GraphDelta delta, Registry registry)
        {
            var output = new GraphDataHandler(elementID, delta, registry);
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

        internal void RemoveHandler(ElementID elementID)
        {
            foreach(var (_,root) in LayerList)
            {
                var elem = SearchRelative(root, elementID);
                if(elem != null)
                {
                    RemoveDataBranch(elem);
                }
            }
        }

        internal NodeHandler AddNodeHandler(string layer, ElementID elementID, GraphDelta delta, Registry registry)
        {
            var output = new NodeHandler(elementID, delta, registry);
            output.GetWriter(layer).SetHeader(new NodeHeader());
            return output;
        }

        new internal Element GetLayerRoot(string layer)
        {
            return base.GetLayerRoot(layer);
        }

        new internal Element SearchRelative(Element element, ElementID elementID)
        {
            return base.SearchRelative(element, elementID);
        }

        internal IEnumerable<NodeHandler> GetNodes(GraphDelta delta, Registry registry)
        {
            foreach (var data in m_flatStructureLookup.Values)
            {
                if (data.Header is NodeHeader)
                {
                    yield return new NodeHandler(data.ID, delta, registry);
                }
            }
        }

        new internal Element AddElementToLayer(string layer, ElementID elementID)
        {
            return base.AddElementToLayer(layer, elementID);
        }

        new internal Element AddElementToLayer<T>(string layer, ElementID elementID, T data)
        {
            if(data is string s && s == null)
            {
                Debug.Log("HERE");
            }
            return base.AddElementToLayer(layer, elementID, data);
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
