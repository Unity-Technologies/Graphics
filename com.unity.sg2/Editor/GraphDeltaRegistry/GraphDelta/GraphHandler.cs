//#define HANDLER_PROFILING
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using UnityEditor.ContextLayeredDataStorage;
using UnityEditor.ShaderGraph.Configuration;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;
using static UnityEditor.ShaderGraph.Configuration.CPGraphDataProvider;
using static UnityEditor.ShaderGraph.GraphDelta.GraphStorage;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    [Serializable]
    public class GraphHandler
    {
        [SerializeReference]
        internal GraphDelta graphDelta;

        [NonSerialized]
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

        public void AddReferenceNodeMapping(string nodeName, string contextEntryName)
        {
            if (graphDelta.m_data.referableToReferenceNodeMap.TryGetValue(contextEntryName, out var referenceNodeMapping))
            {
                referenceNodeMapping.referenceNodeNames.Add(nodeName);
            }
            else
            {
                var newReferenceNodeList = new List<string> { nodeName };
                var newMapping = new ReferableToReferenceNodeMapping();
                newMapping.referenceNodeNames = newReferenceNodeList;
                graphDelta.m_data.referableToReferenceNodeMap[contextEntryName] = newMapping;
            }
        }

        public void RemoveReferenceNodeMapping(string nodeName, string contextEntryName)
        {
            if (graphDelta.m_data.referableToReferenceNodeMap.TryGetValue(contextEntryName, out var referenceNodeMapping)
                && referenceNodeMapping != null)
                referenceNodeMapping.referenceNodeNames.Remove(nodeName);
        }

        public List<string> GetReferenceNodeMapping(string contextEntryName)
        {
            if(graphDelta.m_data.referableToReferenceNodeMap.TryGetValue(contextEntryName, out var referenceNodeList))
                return referenceNodeList.referenceNodeNames;
            return null;
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

        public bool ReconcretizeNode(string name)
            => graphDelta.ReconcretizeNode(name, registry);

        public bool ReconcretizeNodeNoPropagation(string name)
        => graphDelta.ReconcretizeNode(name, registry, false);

        [Obsolete("GetNodeReader is obsolete - Use GetNode now", false)]
        public NodeHandler GetNodeReader(string name) =>
            graphDelta.GetNode(name, registry);

        [Obsolete("GetNodeWriter is obselete - Use GetNode now", false)]
        public NodeHandler GetNodeWriter(string name) =>
            graphDelta.GetNode(name, registry);

        public NodeHandler GetNode(ElementID name) =>
            graphDelta.GetNode(name, registry);

        public void RemoveNode(string name) =>
            graphDelta.RemoveNode(name, registry);

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

        public void AddContextConnection(string contextEntryName, ElementID portInput)
        {
            graphDelta.AddDefaultConnection(contextEntryName, portInput, registry);
        }

        public void RemoveContextConnection(string contextEntryName, ElementID portInput)
        {
            graphDelta.RemoveDefaultConnection(contextEntryName, portInput, registry);
        }


        public void ReconcretizeAll()
        {
            foreach(var name in GraphHandlerUtils.GetNodesTopologically(this))
            {
                var node = GetNode(name);
                if (node != null)
                {
                    var builder = registry.GetNodeBuilder(node.GetRegistryKey());
                    try
                    {
                        if (builder != null)
                        {
                            if (builder.GetRegistryFlags() == RegistryFlags.Func)
                            {
                                ReconcretizeNodeNoPropagation(node.ID.FullPath);
                            }

                            if (builder.GetRegistryKey().Name == ContextBuilder.kRegistryKey.Name)
                            {
                                ReconcretizeNodeNoPropagation(node.ID.FullPath);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        public IEnumerable<PortHandler> GetConnectedPorts(ElementID portID) => graphDelta.GetConnectedPorts(portID, registry);

        public IEnumerable<NodeHandler> GetConnectedNodes(ElementID nodeID) => graphDelta.GetConnectedNodes(nodeID, registry);

        private void AddEntry(NodeHandler context, CPEntryDescriptor desc, bool isInput)
        {
#if HANDLER_PROFILING
            Profiler.BeginSample("Add single entry");
#endif
            ContextBuilder.AddReferableEntry(context,
                new ContextEntry
                {
                    fieldName = desc.name,
                    height = desc.type.IsMatrix ? (GraphType.Height)desc.type.MatrixRows : GraphType.Height.One,
                    length = desc.type.IsVector ? (GraphType.Length)desc.type.VectorDimension : GraphType.Length.One,
                    primitive = GraphType.Primitive.Float,
                    precision = GraphType.Precision.Fixed
                },
                registry,
                source: isInput ? ContextEntryEnumTags.DataSource.Global : ContextEntryEnumTags.DataSource.Constant);
#if HANDLER_PROFILING
            Profiler.EndSample();
#endif
        }

        internal void RebuildContextData(
            ElementID contextNode,
            ITargetProvider target,
            string templateName,
            string cpName,
            bool input)
        {
            var context = GetNode(contextNode);
            if(context == null)
            {
                return;
            }
            // work on the concrete layer because user interactions can't change the data
            context.ClearLayerData(GraphDelta.k_concrete);
            context.DefaultLayer = GraphDelta.k_concrete;

            GatherProviderCPIO(target, out var descriptors);
#if HANDLER_PROFILING
            Profiler.BeginSample("Iterate over gathered CustomizationPoint inputs and outputs");
#endif
            foreach(var descriptor in descriptors)
            {
#if HANDLER_PROFILING
                Profiler.BeginSample("Foreach Descriptor");
#endif
                if(descriptor.templateName.Equals(templateName, StringComparison.OrdinalIgnoreCase))
                {
#if HANDLER_PROFILING
                    Profiler.BeginSample("Foreach CustomizationPoint");
#endif
                    foreach(var cpio in descriptor.CPIO)
                    {
                        if(cpio.customizationPointName.Equals(cpName, StringComparison.OrdinalIgnoreCase))
                        {
#if HANDLER_PROFILING
                            Profiler.BeginSample("Foreach Entry");
#endif
                            if(input)
                            {
                                foreach(var i in cpio.inputs)
                                {
                                    AddEntry(context, i, true);
                                }
                            }
                            else
                            {
                                foreach(var o in cpio.outputs)
                                {
                                    AddEntry(context, o, false);
                                }
                            }
                            context.AddField("_CustomizationPointName", cpName);
#if HANDLER_PROFILING
                            Profiler.EndSample();
                            Profiler.EndSample();
                            Profiler.EndSample();
                            Profiler.EndSample();
#endif
                            return;
                        }
                    }
#if HANDLER_PROFILING
                    Profiler.EndSample();
#endif
                }
#if HANDLER_PROFILING
                Profiler.EndSample();
#endif
            }
#if HANDLER_PROFILING
            Profiler.EndSample();
#endif
        }

        public NodeHandler DuplicateNode(NodeHandler sourceNode, bool copyExternalEdges)
        {
            return DuplicateNode(sourceNode, copyExternalEdges, graphDelta.m_data.GetLayerRoot(GraphDelta.k_user).GetUniqueLocalID(sourceNode.ID.LocalPath));
        }

        public NodeHandler DuplicateNode(NodeHandler sourceNode, bool copyExternalEdges, ElementID duplicatedNodeID)
        {
            var copy = graphDelta.DuplicateNode(sourceNode, registry, duplicatedNodeID);
            try
            {
                graphDelta.ReconcretizeNode(copy.ID, registry);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("Failed to Duplicate Node.");
            }

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
                        var defConnections = graphDelta.m_data.defaultConnections.Where(e => e.Input.ParentPath.Equals(tup.node.ID.FullPath)).ToList();
                        foreach (var def in defConnections)
                        {
                                graphDelta.AddDefaultConnection(def.Context,
                                                   $"{tup.duplicateID.FullPath}.{def.Input.LocalPath}", registry);
                        }

                    }
                }
            }


        }

        public void DuplicateContextEntry(string existingEntryName)
        {
            Debug.Log("GraphHandler.DuplicateContextEntry: Currently not implemented!");
        }

        public (string layerData, string metaData, string edgeData) Copy(List<NodeHandler> sourceGraphNodes)
        {
            return graphDelta.m_data.CreateCopyLayerData(sourceGraphNodes);
        }

        public void Paste(string layerData, string metaData, string edgeData)
        {
            var added = graphDelta.m_data.PasteElementCollection(layerData, metaData, GraphDelta.k_user, out var remappings);
            foreach(var reader in added)
            {
                if(reader.Element.Header is NodeHeader)
                {
                    graphDelta.ReconcretizeNode(reader.Element.ID, registry);
                }
            }
            EdgeList edgeList = new EdgeList();
            EditorJsonUtility.FromJsonOverwrite(edgeData, edgeList);
            foreach(var edge in edgeList.edges)
            {
                ElementID input = edge.Input;
                ElementID output = edge.Output;
                foreach(var remap in remappings)
                {
                    input = input.Rename(remap.Key, remap.Value);
                    output = output.Rename(remap.Key, remap.Value);
                }
                AddEdge(output, input);
            }
            foreach (var def in edgeList.defaultConnections)
            {
                ElementID input = def.Input;
                foreach (var remap in remappings)
                {
                    input = input.Rename(remap.Key, remap.Value);
                }
                graphDelta.m_data.defaultConnections.Add(new ContextConnection(def.Context, input));
            }

        }

        public void AddBuildCallback(Action<NodeHandler> callback) => graphDelta.AddBuildCallback(callback);
        public void RemoveBuildCallback(Action<NodeHandler> callback) => graphDelta.RemoveBuildCallback(callback);
    }
}
