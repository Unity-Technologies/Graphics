using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ContextLayeredDataStorage;
using UnityEditor.ShaderGraph.Configuration;
using UnityEngine;
using static UnityEditor.ShaderGraph.Configuration.CPGraphDataProvider;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public class GraphHandler
    {
        internal GraphDelta graphDelta;
        internal Registry registry;

        [Obsolete("The empty constructer for GraphHandler is obselete; please provide a Registry for updated behavior", false)]
        public GraphHandler()
        {
            graphDelta = new GraphDelta();
            registry = null;
        }

        public GraphHandler(Registry registry)
        {
            graphDelta = new GraphDelta();
            this.registry = registry;
        }

        public GraphHandler(string serializedData, Registry registry)
        {
            graphDelta = new GraphDelta(serializedData);
            this.registry = registry;
        }

        static public GraphHandler FromSerializedFormat(string json, Registry registry)
        {
            return new GraphHandler(json, registry);
        }

        public string ToSerializedFormat()
        {
            return EditorJsonUtility.ToJson(graphDelta.m_data, true);
        }

        [Obsolete("AddNode with a provided Registry is obselete; GraphHanlder can now use its own Registry. " +
            "Use AddNode<T>(string name) for updated behavior")]
        internal NodeHandler AddNode<T>(string name, Registry registry) where T : INodeDefinitionBuilder =>
            graphDelta.AddNode<T>(name, registry);

        internal NodeHandler AddNode<T>(string name) where T : INodeDefinitionBuilder => graphDelta.AddNode<T>(name, registry);

        [Obsolete("AddNode with a provided Registry is obselete; GraphHanlder can now use its own Registry. " +
            "Use AddNode(RegistryKey key, string name) for updated behavior")]
        public NodeHandler AddNode(RegistryKey key, string name, Registry registry) =>
            graphDelta.AddNode(key, name, registry);

        public NodeHandler AddNode(RegistryKey key, string name) =>
            graphDelta.AddNode(key, name, registry);

        [Obsolete("AddContextNode with a provided Registry is obselete; GraphHandler can now use its own Registry. " +
            "Use AddContextNode(RegistryKey key) for updated behavior")]
        public NodeHandler AddContextNode(RegistryKey key, Registry registry) =>
            graphDelta.AddContextNode(key, registry);

        [Obsolete("AddContextNode with a RegistryKey is obselete; GraphHandler will now setup registry keys based " +
            "on CustomizationPoints")]
        public NodeHandler AddContextNode(RegistryKey key) => AddContextNode(key, registry);

        public NodeHandler AddContextNode(string name) =>
            graphDelta.AddContextNode(name, registry);

        [Obsolete("ReconcretizeNode with a provided Registry is obselete; GraphHanlder can now use its own Registry. " +
            "Use ReconcretizeNode(string name) for updated behavior")]
        public bool ReconcretizeNode(string name, Registry registry) =>
            graphDelta.ReconcretizeNode(name, registry);

        public bool ReconcretizeNode(string name) =>
            graphDelta.ReconcretizeNode(name, registry);

        [Obsolete("GetNodeReader is obsolete - Use GetNode now", false)]
        public NodeHandler GetNodeReader(string name) =>
            graphDelta.GetNode(name, registry);

        [Obsolete("GetNodeWriter is obselete - Use GetNode now", false)]
        public NodeHandler GetNodeWriter(string name) =>
            graphDelta.GetNode(name, registry);

        public NodeHandler GetNode(ElementID name) =>
            graphDelta.GetNode(name, registry);

        public void RemoveNode(string name) =>
            graphDelta.RemoveNode(name);

        public IEnumerable<NodeHandler> GetNodes() =>
            graphDelta.GetNodes(registry);

        //Temporary workaround for deprecated GraphHanlder constructor
        public EdgeHandler AddEdge(ElementID output, ElementID input) =>
            registry == null ? graphDelta.AddEdge(output, input) : graphDelta.AddEdge(output, input, registry);


        //Temporary workaround for deprecated GraphHanlder constructor
        public void RemoveEdge(ElementID output, ElementID input)
        {
            if (registry == null)
                graphDelta.RemoveEdge(output, input);
            else
                graphDelta.RemoveEdge(output, input, registry);
        }

        public void ReconcretizeAll()
        {
            foreach (var name in GetNodes().Select(e => e.ID.LocalPath).ToList())
            {
                var node = GetNode(name);
                if (node != null)
                {
                    var builder = registry.GetNodeBuilder(node.GetRegistryKey());
                    if (builder != null)
                    {
                        if (builder.GetRegistryFlags() == RegistryFlags.Func)
                        {
                            ReconcretizeNode(node.ID.FullPath);
                        }
                        if (builder.GetRegistryKey().Name == ContextBuilder.kRegistryKey.Name)
                        {
                            ReconcretizeNode(node.ID.FullPath);
                        }
                    }

                }
            }
        }

        [Obsolete("ReconcretizeAll with a provided Registry is obselete; GraphHanlder can now use its own Registry. " +
            "Use ReconcretizeAll() for updated behavior")]
        public void ReconcretizeAll(Registry registry)
        {
            foreach (var name in GetNodes().Select(e => e.ID.LocalPath).ToList())
            {
                var node = graphDelta.GetNode(name, registry);
                if (node != null)
                {
                    var builder = registry.GetNodeBuilder(node.GetRegistryKey());
                    if (builder != null)
                    {
                        if (builder.GetRegistryFlags() == RegistryFlags.Func)
                        {
                            ReconcretizeNode(node.ID.FullPath, registry);
                        }
                    }
                }
            }
        }

        public IEnumerable<PortHandler> GetConnectedPorts(ElementID portID) => graphDelta.GetConnectedPorts(portID, registry);

        public IEnumerable<NodeHandler> GetConnectedNodes(ElementID nodeID) => graphDelta.GetConnectedNodes(nodeID, registry);

        public void RebuildContextData(ElementID contextNode, ITargetProvider target, string templateName, string cpName, bool input)
        {
            void AddEntry(NodeHandler context, CPDataEntryDescriptor desc)
            {
                ContextBuilder.AddReferableEntry(context,
                    new ContextEntry
                    {
                        fieldName = desc.name,
                        height = desc.type.IsMatrix ? (GraphType.Height)desc.type.MatrixRows : GraphType.Height.One,
                        length = desc.type.IsVector ? (GraphType.Length)desc.type.VectorDimension : GraphType.Length.One,
                        primitive = GraphType.Primitive.Float,
                        precision = GraphType.Precision.Fixed
                    },
                    registry);
            }

            var context = GetNode(contextNode);
            if(context == null)
            {
                return;
            }
            context.ClearLayerData(GraphDelta.k_concrete);
            context.DefaultLayer = GraphDelta.k_concrete;

            GatherProviderCPIO(target, out var descriptors);
            foreach(var descriptor in descriptors)
            {
                if(descriptor.templateName.Equals(templateName, StringComparison.OrdinalIgnoreCase))
                {
                    foreach(var cpio in descriptor.CPIO)
                    {
                        if(cpio.customizationPointName.Equals(cpName, StringComparison.OrdinalIgnoreCase))
                        {
                            if(input)
                            {
                                foreach(var i in cpio.inputs)
                                {
                                    AddEntry(context, i);
                                }
                            }
                            else
                            {
                                foreach(var o in cpio.outputs)
                                {
                                    AddEntry(context, o);
                                }
                            }
                            context.AddField("_CustomizationPointName", cpName);
                            return;
                        }
                    }
                }
            }
        }

        public NodeHandler DuplicateNode(NodeHandler sourceNode, bool copyExternalEdges)
        {
            return DuplicateNode(sourceNode, copyExternalEdges, graphDelta.m_data.GetLayerRoot(GraphDelta.k_user).GetUniqueLocalID(sourceNode.ID.LocalPath));
        }

        public NodeHandler DuplicateNode(NodeHandler sourceNode, bool copyExternalEdges, ElementID duplicatedNodeID)
        {
            var copy = graphDelta.DuplicateNode(sourceNode, registry, duplicatedNodeID);
            graphDelta.ReconcretizeNode(copy.ID, registry);
            if(copyExternalEdges)
            {
                var ports = sourceNode.GetPorts().ToList();
                // Use old school for loops because enumerators don't like it when the underlying collection is modified
                for(var i = 0; i < ports.Count(); i++)
                {
                    var port = ports[i];
                    if(port.IsInput)
                    {
                        var connectedPorts = port.GetConnectedPorts().ToList();
                        for(var j = 0; j < connectedPorts.Count; j++)
                        {
                            var p = connectedPorts[j];
                            AddEdge(p.ID, $"{copy.ID.FullPath}.{port.ID.LocalPath}");
                        }
                    }
                }
            }
            return copy;
        }

        public void DuplicateNodes(List<(NodeHandler node, ElementID duplicateID)> sourceNodes, bool copyEdges)
        {
            HashSet<ElementID> duplicatedNodes = new HashSet<ElementID>();
            Dictionary<string, string> renamed = new Dictionary<string, string>();
            Stack<(NodeHandler node, ElementID duplicateID)> workingSet = new Stack<(NodeHandler node, ElementID duplicateID)>(sourceNodes);
            while(workingSet.Count > 0)
            {
                var tup = workingSet.Pop();
                if(duplicatedNodes.Contains(tup.node.ID))
                {
                    continue;
                }

                bool anyIncludedUpstream = false;
                foreach(var connected in graphDelta.GetConnectedIncomingNodes(tup.node.ID, registry))
                {
                    var inc = sourceNodes.Where(neid => neid.node.ID.Equals(connected.ID));
                    if(inc.Any())
                    {
                        foreach (var i in inc)
                        {
                            if (!duplicatedNodes.Contains(i.node.ID) && workingSet.Any(neid => neid.node.ID.Equals(i.node.ID)))
                            {
                                if(!anyIncludedUpstream)
                                {
                                    workingSet.Push(tup);
                                }
                                workingSet.Push(i);
                                anyIncludedUpstream = true;
                            }
                        }
                    }
                }

                if(!anyIncludedUpstream)
                {
                    graphDelta.DuplicateNode(tup.node, registry, tup.duplicateID);
                    duplicatedNodes.Add(tup.node.ID);
                    renamed.Add(tup.node.ID.FullPath, tup.duplicateID.FullPath);
                    if (copyEdges)
                    {
                        var inputEdges = graphDelta.m_data.edges.Where(e => e.Input.ParentPath.Equals(tup.node.ID.FullPath)).ToList();
                        foreach (var edge in inputEdges)
                        {
                            if (renamed.TryGetValue(edge.Output.ParentPath, out string rename))
                            {
                                graphDelta.AddEdge($"{rename}.{edge.Output.LocalPath}",
                                                   $"{tup.duplicateID.FullPath}.{edge.Input.LocalPath}");
                            }
                            else
                            {
                                graphDelta.AddEdge(edge.Output,
                                                   $"{tup.duplicateID.FullPath}.{edge.Input.LocalPath}");
                            }
                        }
                    }
                }
            }


        }

        public void DuplicateContextEntry(string existingEntryName)
        {
            Debug.Log("GraphHandler.DuplicateContextEntry: Currently not implemented!");
        }

        string Copy(List<GraphDataHandler> sourceGraphElements)
        {
            return String.Empty;
        }

        void Paste(string graphDataJSON)
        {
            return;
        }
    }
}
