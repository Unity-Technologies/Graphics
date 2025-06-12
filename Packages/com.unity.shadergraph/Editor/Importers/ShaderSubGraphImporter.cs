using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine.Pool;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    [ExcludeFromPreset]
    [ScriptedImporter(30, Extension, -905)]
    [CoreRPHelpURL("Sub-graph", "com.unity.shadergraph")]
    class ShaderSubGraphImporter : ScriptedImporter
    {
        public const string Extension = "shadersubgraph";

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        static string[] GatherDependenciesFromSourceFile(string assetPath)
        {
            try
            {
                AssetCollection assetCollection = new AssetCollection();
                MinimalGraphData.GatherMinimalDependenciesFromFile(assetPath, assetCollection);

                List<string> dependencyPaths = new List<string>();
                foreach (var asset in assetCollection.assets)
                {
                    // only artifact dependencies need to be declared in GatherDependenciesFromSourceFile
                    // to force their imports to run before ours
                    if (asset.Value.HasFlag(AssetCollection.Flags.ArtifactDependency))
                    {
                        var dependencyPath = AssetDatabase.GUIDToAssetPath(asset.Key);

                        // it is unfortunate that we can't declare these dependencies unless they have a path...
                        // I asked AssetDatabase team for GatherDependenciesFromSourceFileByGUID()
                        if (!string.IsNullOrEmpty(dependencyPath))
                            dependencyPaths.Add(dependencyPath);
                    }
                }
                return dependencyPaths.ToArray();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return new string[0];
            }
        }

        static bool NodeWasUsedByGraph(string nodeId, GraphData graphData)
        {
            var node = graphData.GetNodeFromId(nodeId);
            return node?.wasUsedByGenerator ?? false;
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var importLog = new ShaderGraphImporter.AssetImportErrorLog(ctx);

            var graphAsset = ScriptableObject.CreateInstance<SubGraphAsset>();
            var subGraphPath = ctx.assetPath;
            var subGraphGuid = AssetDatabase.AssetPathToGUID(subGraphPath);
            graphAsset.assetGuid = subGraphGuid;
            var textGraph = File.ReadAllText(subGraphPath, Encoding.UTF8);
            var messageManager = new MessageManager();
            var graphData = new GraphData
            {
                isSubGraph = true,
                assetGuid = subGraphGuid,
                messageManager = messageManager
            };
            MultiJson.Deserialize(graphData, textGraph);

            try
            {
                ProcessSubGraph(graphAsset, graphData, importLog);
            }
            catch (Exception e)
            {
                graphAsset.isValid = false;
                Debug.LogException(e, graphAsset);
            }
            finally
            {
                var errors = messageManager.ErrorStrings((nodeId) => NodeWasUsedByGraph(nodeId, graphData));
                int errCount = errors.Count();
                if (errCount > 0)
                {
                    var firstError = errors.FirstOrDefault();
                    importLog.LogError($"Sub Graph at {subGraphPath} has {errCount} error(s), the first is: {firstError}", graphAsset);
                    graphAsset.isValid = false;
                }
                else
                {
                    var warnings = messageManager.ErrorStrings((nodeId) => NodeWasUsedByGraph(nodeId, graphData), Rendering.ShaderCompilerMessageSeverity.Warning);
                    int warningCount = warnings.Count();
                    if (warningCount > 0)
                    {
                        var firstWarning = warnings.FirstOrDefault();
                        importLog.LogWarning($"Sub Graph at {subGraphPath} has {warningCount} warning(s), the first is: {firstWarning}", graphAsset);
                    }
                }
                messageManager.ClearAll();
            }

            Texture2D texture = Resources.Load<Texture2D>("Icons/sg_subgraph_icon");
            ctx.AddObjectToAsset("MainAsset", graphAsset, texture);
            ctx.SetMainObject(graphAsset);

            var metadata = ScriptableObject.CreateInstance<ShaderSubGraphMetadata>();
            metadata.hideFlags = HideFlags.HideInHierarchy;
            metadata.assetDependencies = new List<UnityEngine.Object>();

            AssetCollection assetCollection = new AssetCollection();
            MinimalGraphData.GatherMinimalDependenciesFromFile(assetPath, assetCollection);

            foreach (var asset in assetCollection.assets)
            {
                if (asset.Value.HasFlag(AssetCollection.Flags.IncludeInExportPackage))
                {
                    // this sucks that we have to fully load these assets just to set the reference,
                    // which then gets serialized as the GUID that we already have here.  :P

                    var dependencyPath = AssetDatabase.GUIDToAssetPath(asset.Key);
                    if (!string.IsNullOrEmpty(dependencyPath))
                    {
                        metadata.assetDependencies.Add(
                            AssetDatabase.LoadAssetAtPath(dependencyPath, typeof(UnityEngine.Object)));
                    }
                }
            }
            ctx.AddObjectToAsset("Metadata", metadata);

            // declare dependencies
            foreach (var asset in assetCollection.assets)
            {
                if (asset.Value.HasFlag(AssetCollection.Flags.SourceDependency))
                {
                    ctx.DependsOnSourceAsset(asset.Key);

                    // I'm not sure if this warning below is actually used or not, keeping it to be safe
                    var assetPath = AssetDatabase.GUIDToAssetPath(asset.Key);

                    // Ensure that dependency path is relative to project
                    if (!string.IsNullOrEmpty(assetPath) && !assetPath.StartsWith("Packages/") && !assetPath.StartsWith("Assets/"))
                    {
                        importLog.LogWarning($"Invalid dependency path: {assetPath}", graphAsset);
                    }
                }

                // NOTE: dependencies declared by GatherDependenciesFromSourceFile are automatically registered as artifact dependencies
                // HOWEVER: that path ONLY grabs dependencies via MinimalGraphData, and will fail to register dependencies
                // on GUIDs that don't exist in the project.  For both of those reasons, we re-declare the dependencies here.
                if (asset.Value.HasFlag(AssetCollection.Flags.ArtifactDependency))
                {
                    ctx.DependsOnArtifact(asset.Key);
                }
            }
        }

        static void ProcessSubGraph(SubGraphAsset asset, GraphData graph, ShaderGraphImporter.AssetImportErrorLog importLog)
        {
            var graphIncludes = new IncludeCollection();
            var registry = new FunctionRegistry(new ShaderStringBuilder(), graphIncludes, true);

            asset.functions.Clear();
            asset.isValid = true;

            graph.OnEnable();
            graph.messageManager.ClearAll();
            graph.ValidateGraph();

            var assetPath = AssetDatabase.GUIDToAssetPath(asset.assetGuid);
            asset.hlslName = NodeUtils.GetHLSLSafeName(Path.GetFileNameWithoutExtension(assetPath));
            asset.inputStructName = $"Bindings_{asset.hlslName}_{asset.assetGuid}_$precision";
            asset.functionName = $"SG_{asset.hlslName}_{asset.assetGuid}_$precision";
            asset.path = graph.path;

            var outputNode = graph.outputNode;

            var outputSlots = PooledList<MaterialSlot>.Get();
            outputNode.GetInputSlots(outputSlots);

            List<AbstractMaterialNode> nodes = new List<AbstractMaterialNode>();
            NodeUtils.DepthFirstCollectNodesFromNode(nodes, outputNode);

            // flag the used nodes so we can filter out errors from unused nodes
            foreach (var node in nodes)
                node.SetUsedByGenerator();

            // Start with a clean slate for the input/output capabilities and dependencies
            asset.inputCapabilities.Clear();
            asset.outputCapabilities.Clear();
            asset.slotDependencies.Clear();

            ShaderStageCapability effectiveShaderStage = ShaderStageCapability.All;
            var shaderStageCapabilityCache = new Dictionary<SlotReference, ShaderStageCapability>();
            foreach (var slot in outputSlots)
            {
                var stage = NodeUtils.GetEffectiveShaderStageCapability(slot, true, shaderStageCapabilityCache);
                if (effectiveShaderStage == ShaderStageCapability.All && stage != ShaderStageCapability.All)
                    effectiveShaderStage = stage;

                asset.outputCapabilities.Add(new SlotCapability { slotName = slot.RawDisplayName(), capabilities = stage });

                // Find all unique property nodes used by this slot and record a dependency for this input/output pair
                var inputPropertyNames = new HashSet<string>();
                var nodeSet = new HashSet<AbstractMaterialNode>();
                NodeUtils.CollectNodeSet(nodeSet, slot);
                foreach (var node in nodeSet)
                {
                    if (node is PropertyNode propNode && !inputPropertyNames.Contains(propNode.property.displayName))
                    {
                        inputPropertyNames.Add(propNode.property.displayName);
                        var slotDependency = new SlotDependencyPair();
                        slotDependency.inputSlotName = propNode.property.displayName;
                        slotDependency.outputSlotName = slot.RawDisplayName();
                        asset.slotDependencies.Add(slotDependency);
                    }
                }
            }
            CollectInputCapabilities(asset, graph);

            asset.vtFeedbackVariables = VirtualTexturingFeedbackUtils.GetFeedbackVariables(outputNode as SubGraphOutputNode);
            asset.requirements = ShaderGraphRequirements.FromNodes(nodes, effectiveShaderStage, false);

            // output precision is whatever the output node has as a graph precision, falling back to the graph default
            asset.outputGraphPrecision = outputNode.graphPrecision.GraphFallback(graph.graphDefaultPrecision);

            // this saves the graph precision, which indicates whether this subgraph is switchable or not
            asset.subGraphGraphPrecision = graph.graphDefaultPrecision;

            asset.previewMode = graph.previewMode;

            asset.includes = graphIncludes;

            GatherDescendentsFromGraph(new GUID(asset.assetGuid), out var containsCircularDependency, out var descendents);
            asset.descendents.AddRange(descendents.Select(g => g.ToString()));
            asset.descendents.Sort();   // ensure deterministic order

            var childrenSet = new HashSet<string>();
            var anyErrors = false;
            foreach (var node in nodes)
            {
                if (node is SubGraphNode subGraphNode)
                {
                    var subGraphGuid = subGraphNode.subGraphGuid;
                    childrenSet.Add(subGraphGuid);
                }

                if (node.hasError)
                {
                    anyErrors = true;
                }
                asset.children = childrenSet.ToList();
                asset.children.Sort(); // ensure deterministic order
            }

            if (!anyErrors && containsCircularDependency)
            {
                importLog.LogError($"Error in Graph at {assetPath}: Sub Graph contains a circular dependency.", asset);
                anyErrors = true;
            }

            if (anyErrors)
            {
                asset.isValid = false;
                registry.ProvideFunction(asset.functionName, sb => { });
                return;
            }

            foreach (var node in nodes)
            {
                if (node is IGeneratesFunction generatesFunction)
                {
                    registry.builder.currentNode = node;
                    generatesFunction.GenerateNodeFunction(registry, GenerationMode.ForReals);
                }
            }

            // Need to order the properties so that they are in the same order on a subgraph node in a shadergraph
            // as they are in the blackboard for the subgraph itself.  The (blackboard) categories keep that ordering,
            // so traverse those and add those items to the ordered properties list.  Needs to be used to set up the
            // function _and_ to write out the final asset data so that the function call parameter order matches as well.
            var orderedProperties = new List<AbstractShaderProperty>();
            var propertiesList = graph.properties.ToList();
            foreach (var category in graph.categories)
            {
                foreach (var child in category.Children)
                {
                    var prop = propertiesList.Find(p => p.guid == child.guid);
                    // Not all properties in the category are actually on the graph.
                    // In particular, it seems as if keywords are not properties on sub-graphs.
                    if (prop != null  && !orderedProperties.Contains(prop))
                        orderedProperties.Add(prop);
                }
            }

            // If we are importing an older file that has not had categories generated for it yet, include those now.
            orderedProperties.AddRange(graph.properties.Except(orderedProperties));

            // provide top level subgraph function
            // NOTE: actual concrete precision here shouldn't matter, it's irrelevant when building the subgraph asset
            registry.ProvideFunction(asset.functionName, asset.subGraphGraphPrecision, ConcretePrecision.Single, sb =>
            {
                GenerationUtils.GenerateSurfaceInputStruct(sb, asset.requirements, asset.inputStructName);
                sb.AppendNewLine();

                // Generate the arguments... first INPUTS
                var arguments = new List<string>();
                foreach (var prop in orderedProperties)
                {
                    // apply fallback to the graph default precision (but don't convert to concrete)
                    // this means "graph switchable" properties will use the precision token
                    GraphPrecision propGraphPrecision = prop.precision.ToGraphPrecision(graph.graphDefaultPrecision);
                    string precisionString = propGraphPrecision.ToGenericString();
                    arguments.Add(prop.GetPropertyAsArgumentString(precisionString));
                    if (prop.isConnectionTestable)
                    {
                        arguments.Add($"bool {prop.GetConnectionStateHLSLVariableName()}");
                    }
                }

                {
                    var dropdowns = graph.dropdowns;
                    foreach (var dropdown in dropdowns)
                        arguments.Add($"int {dropdown.referenceName}");
                }

                // now pass surface inputs
                arguments.Add(string.Format("{0} IN", asset.inputStructName));

                // Now generate output arguments
                foreach (MaterialSlot output in outputSlots)
                    arguments.Add($"out {output.concreteValueType.ToShaderString(asset.outputGraphPrecision.ToGenericString())} {output.shaderOutputName}_{output.id}");

                // Vt Feedback output arguments (always full float4)
                foreach (var output in asset.vtFeedbackVariables)
                    arguments.Add($"out {ConcreteSlotValueType.Vector4.ToShaderString(ConcretePrecision.Single)} {output}_out");

                // Create the function prototype from the arguments
                sb.AppendLine("void {0}({1})"
                    , asset.functionName
                    , arguments.Aggregate((current, next) => $"{current}, {next}"));

                // now generate the function
                using (sb.BlockScope())
                {
                    // Just grab the body from the active nodes
                    foreach (var node in nodes)
                    {
                        if (node is IGeneratesBodyCode generatesBodyCode)
                        {
                            sb.currentNode = node;
                            generatesBodyCode.GenerateNodeCode(sb, GenerationMode.ForReals);

                            if (node.graphPrecision == GraphPrecision.Graph)
                            {
                                // code generated by nodes that use graph precision stays in generic form with embedded tokens
                                // those tokens are replaced when this subgraph function is pulled into a graph that defines the precision
                            }
                            else
                            {
                                sb.ReplaceInCurrentMapping(PrecisionUtil.Token, node.concretePrecision.ToShaderString());
                            }
                        }
                    }

                    foreach (var slot in outputSlots)
                    {
                        sb.AppendLine($"{slot.shaderOutputName}_{slot.id} = {outputNode.GetSlotValue(slot.id, GenerationMode.ForReals)};");
                    }

                    foreach (var slot in asset.vtFeedbackVariables)
                    {
                        sb.AppendLine($"{slot}_out = {slot};");
                    }
                }
            });

            // save all of the node-declared functions to the subgraph asset
            foreach (var name in registry.names)
            {
                var source = registry.sources[name];
                var func = new FunctionPair(name, source.code, source.graphPrecisionFlags);
                asset.functions.Add(func);
            }

            var collector = new PropertyCollector();
            foreach (var node in nodes)
            {
                int previousPropertyCount = Math.Max(0, collector.propertyCount - 1);

                node.CollectShaderProperties(collector, GenerationMode.ForReals);

                // This is a stop-gap to prevent the autogenerated values from JsonObject and ShaderInput from
                // resulting in non-deterministic import data. While we should move to local ids in the future,
                // this will prevent cascading shader recompilations.
                for (int i = previousPropertyCount; i < collector.propertyCount; ++i)
                {
                    var prop = collector.GetProperty(i);
                    var namespaceId = node.objectId;
                    var nameId = prop.referenceName;

                    prop.OverrideObjectId(namespaceId, nameId + "_ObjectId_" + i);
                    prop.OverrideGuid(namespaceId, nameId + "_Guid_" + i);
                }
            }

            asset.WriteData(orderedProperties, graph.keywords, graph.dropdowns, collector.properties, outputSlots, graph.unsupportedTargets);
            outputSlots.Dispose();
        }

        static void GatherDescendentsFromGraph(GUID rootAssetGuid, out bool containsCircularDependency, out HashSet<GUID> descendentGuids)
        {
            var dependencyMap = new Dictionary<GUID, GUID[]>();
            AssetCollection tempAssetCollection = new AssetCollection();
            using (UnityEngine.Pool.ListPool<GUID>.Get(out var tempList))
            {
                GatherDependencyMap(rootAssetGuid, dependencyMap, tempAssetCollection);
                containsCircularDependency = ContainsCircularDependency(rootAssetGuid, dependencyMap, tempList);
            }

            descendentGuids = new HashSet<GUID>();
            GatherDescendentsUsingDependencyMap(rootAssetGuid, descendentGuids, dependencyMap);
        }

        static void GatherDependencyMap(GUID rootAssetGUID, Dictionary<GUID, GUID[]> dependencyMap, AssetCollection tempAssetCollection)
        {
            if (!dependencyMap.ContainsKey(rootAssetGUID))
            {
                // if it is a subgraph, try to recurse into it
                var assetPath = AssetDatabase.GUIDToAssetPath(rootAssetGUID);
                if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(Extension, true, null))
                {
                    tempAssetCollection.Clear();
                    MinimalGraphData.GatherMinimalDependenciesFromFile(assetPath, tempAssetCollection);

                    var subgraphGUIDs = tempAssetCollection.assets.Where(asset => asset.Value.HasFlag(AssetCollection.Flags.IsSubGraph)).Select(asset => asset.Key).ToArray();
                    dependencyMap[rootAssetGUID] = subgraphGUIDs;

                    foreach (var guid in subgraphGUIDs)
                    {
                        GatherDependencyMap(guid, dependencyMap, tempAssetCollection);
                    }
                }
            }
        }

        static void GatherDescendentsUsingDependencyMap(GUID rootAssetGUID, HashSet<GUID> descendentGuids, Dictionary<GUID, GUID[]> dependencyMap)
        {
            var dependencies = dependencyMap[rootAssetGUID];
            foreach (GUID dependency in dependencies)
            {
                if (descendentGuids.Add(dependency))
                {
                    GatherDescendentsUsingDependencyMap(dependency, descendentGuids, dependencyMap);
                }
            }
        }

        static bool ContainsCircularDependency(GUID assetGUID, Dictionary<GUID, GUID[]> dependencyMap, List<GUID> ancestors)
        {
            if (ancestors.Contains(assetGUID))
            {
                return true;
            }

            ancestors.Add(assetGUID);
            foreach (var dependencyGUID in dependencyMap[assetGUID])
            {
                if (ContainsCircularDependency(dependencyGUID, dependencyMap, ancestors))
                {
                    return true;
                }
            }
            ancestors.RemoveAt(ancestors.Count - 1);

            return false;
        }

        static void CollectInputCapabilities(SubGraphAsset asset, GraphData graph)
        {
            // Collect each input's capabilities. There can be multiple property nodes
            // contributing to the same input, so we cache these in a map while building
            var inputCapabilities = new Dictionary<string, SlotCapability>();

            var shaderStageCapabilityCache = new Dictionary<SlotReference, ShaderStageCapability>();

            // Walk all property node output slots, computing and caching the capabilities for that slot
            var propertyNodes = graph.GetNodes<PropertyNode>();
            foreach (var propertyNode in propertyNodes)
            {
                foreach (var slot in propertyNode.GetOutputSlots<MaterialSlot>())
                {
                    var slotName = slot.RawDisplayName();
                    SlotCapability capabilityInfo;
                    if (!inputCapabilities.TryGetValue(slotName, out capabilityInfo))
                    {
                        capabilityInfo = new SlotCapability();
                        capabilityInfo.slotName = slotName;
                        inputCapabilities.Add(propertyNode.property.displayName, capabilityInfo);
                    }
                    capabilityInfo.capabilities &= NodeUtils.GetEffectiveShaderStageCapability(slot, false, shaderStageCapabilityCache);
                }
            }
            asset.inputCapabilities.AddRange(inputCapabilities.Values);
        }
    }
}
