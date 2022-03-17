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
        internal EdgeHandler(ElementID output, ElementID input, GraphStorage owner)
        {
            OutputID = output;
            InputID  = input;
            Owner    = owner;
        }

        [SerializeField]
        public ElementID OutputID { get; internal set; }
        [SerializeField]
        public ElementID InputID  { get; internal set; }
        internal GraphStorage Owner { get; set; }

        public PortHandler OuptutPort => Owner.GetHandler(OutputID).ToPortHandler();
        public PortHandler InputPort  => Owner.GetHandler(InputID).ToPortHandler();
    }

    [Serializable]
    internal sealed partial class GraphStorage : CLDS, ISerializationCallbackReceiver
    {
        [SerializeField]
        internal List<(ElementID output, ElementID input)> edges = new List<(ElementID output, ElementID input)>();

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

        internal NodeHandler AddNodeHandler(string layer, ElementID elementID)
        {
            var output = new NodeHandler(elementID, this);
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

        internal IEnumerable<NodeHandler> GetAllChildReaders()
        {
            foreach (var data in m_flatStructureLookup.Values)
            {
                if (data.Header is NodeHeader)
                {
                    yield return new NodeHandler(data.ID, this);
                }
            }
        }

        new internal Element AddElementToLayer(string layer, ElementID elementID)
        {
            return base.AddElementToLayer(layer, elementID);
        }

        new internal Element AddElementToLayer<T>(string layer, ElementID elementID, T data)
        {
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
