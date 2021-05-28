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

namespace UnityEditor.ShaderGraph
{
    [ExcludeFromPreset]
    [ScriptedImporter(20, Extension, -905)]
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

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var graphAsset = ScriptableObject.CreateInstance<SubGraphAsset>();
            var subGraphPath = ctx.assetPath;
            var subGraphGuid = AssetDatabase.AssetPathToGUID(subGraphPath);
            graphAsset.assetGuid = subGraphGuid;
            var textGraph = File.ReadAllText(subGraphPath, Encoding.UTF8);
            var messageManager = new MessageManager();
            var graphData = new GraphData
            {
                isSubGraph = true, assetGuid = subGraphGuid, messageManager = messageManager
            };
            MultiJson.Deserialize(graphData, textGraph);

            try
            {
                ProcessSubGraph(graphAsset, graphData);
            }
            catch (Exception e)
            {
                graphAsset.isValid = false;
                Debug.LogException(e, graphAsset);
            }
            finally
            {
                if (messageManager.AnyError())
                {
                    graphAsset.isValid = false;
                    foreach (var pair in messageManager.GetNodeMessages())
                    {
                        var node = graphData.GetNodeFromId(pair.Key);
                        foreach (var message in pair.Value)
                        {
                            MessageManager.Log(node, subGraphPath, message, graphAsset);
                        }
                    }
                }
                messageManager.ClearAll();
            }

            Texture2D texture = Resources.Load<Texture2D>("Icons/sg_subgraph_icon@64");
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
                        Debug.LogWarning($"Invalid dependency path: {assetPath}", graphAsset);
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

        static void ProcessSubGraph(SubGraphAsset asset, GraphData graph)
        {
            var registry = new FunctionRegistry(new ShaderStringBuilder(), true);
            registry.names.Clear();
            asset.functions.Clear();
            asset.isValid = true;

            graph.OnEnable();
            graph.messageManager.ClearAll();
            graph.ValidateGraph();

            var assetPath = AssetDatabase.GUIDToAssetPath(asset.assetGuid);
            asset.hlslName = NodeUtils.GetHLSLSafeName(Path.GetFileNameWithoutExtension(assetPath));
            asset.inputStructName = $"Bindings_{asset.hlslName}_{asset.assetGuid}";
            asset.functionName = $"SG_{asset.hlslName}_{asset.assetGuid}";
            asset.path = graph.path;

            var outputNode = graph.outputNode;

            var outputSlots = PooledList<MaterialSlot>.Get();
            outputNode.GetInputSlots(outputSlots);

            List<AbstractMaterialNode> nodes = new List<AbstractMaterialNode>();
            NodeUtils.DepthFirstCollectNodesFromNode(nodes, outputNode);

            asset.effectiveShaderStage = ShaderStageCapability.All;
            foreach (var slot in outputSlots)
            {
                var stage = NodeUtils.GetEffectiveShaderStageCapability(slot, true);
                if (stage != ShaderStageCapability.All)
                {
                    asset.effectiveShaderStage = stage;
                    break;
                }
            }

            asset.vtFeedbackVariables = VirtualTexturingFeedbackUtils.GetFeedbackVariables(outputNode as SubGraphOutputNode);
            asset.requirements = ShaderGraphRequirements.FromNodes(nodes, asset.effectiveShaderStage, false);
            asset.graphPrecision = graph.concretePrecision;
            asset.outputPrecision = outputNode.concretePrecision;
            asset.previewMode = graph.previewMode;

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
                Debug.LogError($"Error in Graph at {assetPath}: Sub Graph contains a circular dependency.", asset);
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
                    registry.builder.ReplaceInCurrentMapping(PrecisionUtil.Token, node.concretePrecision.ToShaderString());
                }
            }

            // provide top level subgraph function
            registry.ProvideFunction(asset.functionName, sb =>
            {
                GenerationUtils.GenerateSurfaceInputStruct(sb, asset.requirements, asset.inputStructName);
                sb.AppendNewLine();

                // Generate arguments... first INPUTS
                var arguments = new List<string>();
                foreach (var prop in graph.properties)
                {
                    prop.ValidateConcretePrecision(asset.graphPrecision);
                    arguments.Add(prop.GetPropertyAsArgumentString());
                }

                // now pass surface inputs
                arguments.Add(string.Format("{0} IN", asset.inputStructName));

                // Now generate outputs
                foreach (MaterialSlot output in outputSlots)
                    arguments.Add($"out {output.concreteValueType.ToShaderString(asset.outputPrecision)} {output.shaderOutputName}_{output.id}");

                // Vt Feedback arguments
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
                            sb.ReplaceInCurrentMapping(PrecisionUtil.Token, node.concretePrecision.ToShaderString());
                        }
                    }

                    foreach (var slot in outputSlots)
                    {
                        sb.AppendLine($"{slot.shaderOutputName}_{slot.id} = {outputNode.GetSlotValue(slot.id, GenerationMode.ForReals, asset.outputPrecision)};");
                    }

                    foreach (var slot in asset.vtFeedbackVariables)
                    {
                        sb.AppendLine($"{slot}_out = {slot};");
                    }
                }
            });

            asset.functions.AddRange(registry.names.Select(x => new FunctionPair(x, registry.sources[x].code)));

            var collector = new PropertyCollector();
            foreach (var node in nodes)
            {
                int previousPropertyCount = Math.Max(0, collector.propertyCount-1);

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
            asset.WriteData(graph.properties, graph.keywords, collector.properties, outputSlots, graph.unsupportedTargets);
            outputSlots.Dispose();
        }

        static void GatherDescendentsFromGraph(GUID rootAssetGuid, out bool containsCircularDependency, out HashSet<GUID> descendentGuids)
        {
            var dependencyMap = new Dictionary<GUID, GUID[]>();
            AssetCollection tempAssetCollection = new AssetCollection();
            using (var tempList = ListPool<GUID>.GetDisposable())
            {
                GatherDependencyMap(rootAssetGuid, dependencyMap, tempAssetCollection);
                containsCircularDependency = ContainsCircularDependency(rootAssetGuid, dependencyMap, tempList.value);
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
    }
}
