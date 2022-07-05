using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor.ContextLayeredDataStorage;
using CLDS = UnityEditor.ContextLayeredDataStorage.ContextLayeredDataStorage;
using System.Linq;

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
    internal class ContextConnection
    {
        [SerializeField]
        private string m_context;
        public string Context { get => m_context; }
        [SerializeField]
        private ElementID m_input;
        public ElementID Input { get => m_input; }

        public ContextConnection()
        {
        }
        public ContextConnection(string context, ElementID input)
        {
            this.m_context = context;
            this.m_input = input;
        }

        public bool Equals(ContextConnection obj)
        {
            return Context.Equals(obj.Context) && Input.Equals(obj.Input);
        }
    }


    // Needed to get around Unitys inability to serialize list of lists
    [Serializable]
    internal class ReferableToReferenceNodeMapping
    {
        public string this[int key]
        {
            get => referenceNodeNames[key];
            set => referenceNodeNames[key] = value;
        }

        [SerializeField]
        internal List<string> referenceNodeNames;
    }

    [Serializable]
    internal sealed partial class GraphStorage : CLDS, ISerializationCallbackReceiver
    {
        [SerializeField]
        internal List<Edge> edges = new List<Edge>();

        [SerializeField]
        internal List<ContextConnection> defaultConnections = new List<ContextConnection>();

        // TODO (Sai): Cleanup how this is exposed and consult with Liz to a better solution
        internal Dictionary<string, ReferableToReferenceNodeMapping> referableToReferenceNodeMap = new();

        [SerializeField]
        List<string> referableNames;

        [SerializeField]
        List<ReferableToReferenceNodeMapping> referenceNodeMappings;

        protected override void AddDefaultLayers()
        {
            AddLayer(0, GraphDelta.k_concrete, false);
            AddLayer(1, GraphDelta.k_user,     true);
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();

            referableNames = referableToReferenceNodeMap.Keys.ToList();
            referenceNodeMappings = referableToReferenceNodeMap.Values.ToList();
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            referableToReferenceNodeMap = new Dictionary<string, ReferableToReferenceNodeMapping>();

            for(var index = 0; index < referableNames.Count; index++)
            {
                var referable = referableNames[index];
                referableToReferenceNodeMap.Add(referable, referenceNodeMappings[index]);
            }
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

        internal void CopyDataBranch(NodeHandler src, NodeHandler dst)
        {
            CopyDataBranch(src.Reader, dst.Writer);
        }

        new internal void CopyDataBranch(DataReader src, DataWriter dst)
        {
            base.CopyDataBranch(src, dst);
        }

        [Serializable]
        internal class EdgeList
        {
            public List<Edge> edges;
            public List<ContextConnection> defaultConnections;
        }
        internal (string layerData, string metaData, string edgeData) CreateCopyLayerData(IEnumerable<NodeHandler> nodes)
        {
            List<DataReader> readers = new List<DataReader>();
            EdgeList edgeList = new EdgeList();
            edgeList.edges = new List<Edge>();
            edgeList.defaultConnections = new List<ContextConnection>();
            foreach(var node in nodes)
            {
                readers.Add(node.Reader);
                foreach(var port in node.GetPorts())
                {
                    if(port.IsInput)
                    {
                        foreach(var edge in edges.Where(e => e.Input.Equals(port.ID)))
                        {
                            if(nodes.Any(n => edge.Output.ParentPath.Equals(n.ID.FullPath)) && !edgeList.edges.Any(e => e.Equals(edge)))
                            {
                                edgeList.edges.Add(edge);
                            }
                        }
                        foreach(var def in defaultConnections.Where(e => e.Input.Equals(port.ID)))
                        {
                            edgeList.defaultConnections.Add(def);
                        }

                    }
                    else
                    {
                        foreach (var edge in edges.Where(e => e.Output.Equals(port.ID)))
                        {
                            if (nodes.Any(n => edge.Input.ParentPath.Equals(n.ID.FullPath)) && !edgeList.edges.Any(e => e.Equals(edge)))
                            {
                                edgeList.edges.Add(edge);
                            }
                        }
                    }
                }
                GatherAll(node.Reader, out List<DataReader> accumulator);
                readers.AddRange(accumulator);
            }
            var cpy = CopyElementCollection(readers);

            return (cpy.layer, cpy.metadata, EditorJsonUtility.ToJson(edgeList, true));
        }
    }
}
