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
    [ScriptedImporter(14, Extension, -905)]
    class ShaderSubGraphImporter : ScriptedImporter
    {
        public const string Extension = "shadersubgraph";

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        static string[] GatherDependenciesFromSourceFile(string assetPath)
        {
            try
            {
                return MinimalGraphData.GetDependencyPaths(assetPath);
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
            var deps = GatherDependenciesFromSourceFile(ctx.assetPath);
            foreach (string dependency in deps)
            {
                metadata.assetDependencies.Add(AssetDatabase.LoadAssetAtPath(dependency, typeof(UnityEngine.Object)));
            }
            ctx.AddObjectToAsset("Metadata", metadata);
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

            GatherFromGraph(assetPath, out var containsCircularDependency, out var descendents);
            asset.descendents.AddRange(descendents);

            var childrenSet = new HashSet<string>();
            var anyErrors = false;
            foreach (var node in nodes)
            {
                if (node is SubGraphNode subGraphNode)
                {
                    var subGraphGuid = subGraphNode.subGraphGuid;
                    if (childrenSet.Add(subGraphGuid))
                    {
                        asset.children.Add(subGraphGuid);
                    }
                }

                if (node.hasError)
                {
                    anyErrors = true;
                }
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

            registry.ProvideFunction(asset.functionName, sb =>
            {
                GenerationUtils.GenerateSurfaceInputStruct(sb, asset.requirements, asset.inputStructName);
                sb.AppendNewLine();

                // Generate arguments... first INPUTS
                var arguments = new List<string>();
                foreach (var prop in graph.properties)
                {
                    prop.ValidateConcretePrecision(asset.graphPrecision);
                    arguments.Add(string.Format("{0}", prop.GetPropertyAsArgumentString()));
                }

                // now pass surface inputs
                arguments.Add(string.Format("{0} IN", asset.inputStructName));

                // Now generate outputs
                foreach (var output in outputSlots)
                    arguments.Add($"out {output.concreteValueType.ToShaderString(asset.outputPrecision)} {output.shaderOutputName}_{output.id}");

                // Vt Feedback arguments
                foreach (var output in asset.vtFeedbackVariables)
                    arguments.Add($"out {ConcreteSlotValueType.Vector4.ToShaderString(ConcretePrecision.Float)} {output}_out");

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
                int previousPropertyCount = Math.Max(0, collector.properties.Count-1);

                node.CollectShaderProperties(collector, GenerationMode.ForReals);

                // This is a stop-gap to prevent the autogenerated values from JsonObject and ShaderInput from
                // resulting in non-deterministic import data. While we should move to local ids in the future,
                // this will prevent cascading shader recompilations.
                for (int i = previousPropertyCount; i < collector.properties.Count; ++i)
                {
                    var prop = collector.properties[i];
                    var namespaceId = node.objectId;
                    var nameId = prop.referenceName;

                    prop.OverrideObjectId(namespaceId, nameId + "_ObjectId_" + i);
                    prop.OverrideGuid(namespaceId, nameId + "_Guid_" + i);
                }
            }
            asset.WriteData(graph.properties, graph.keywords, collector.properties, outputSlots, graph.unsupportedTargets);
            outputSlots.Dispose();
        }

        static void GatherFromGraph(string assetPath, out bool containsCircularDependency, out HashSet<string> descendentGuids)
        {
            var dependencyMap = new Dictionary<string, string[]>();
            using (var tempList = ListPool<string>.GetDisposable())
            {
                GatherDependencies(assetPath, dependencyMap, tempList.value);
                containsCircularDependency = ContainsCircularDependency(assetPath, dependencyMap, tempList.value);
            }

            descendentGuids = new HashSet<string>();
            GatherDescendents(assetPath, descendentGuids, dependencyMap);
        }

        static void GatherDependencies(string assetPath, Dictionary<string, string[]> dependencyMap, List<string> dependencies)
        {
            if (!dependencyMap.ContainsKey(assetPath))
            {
                if(assetPath.EndsWith(Extension))
                    MinimalGraphData.GetDependencyPaths(assetPath, dependencies);

                var dependencyPaths = dependencyMap[assetPath] = dependencies.ToArray();
                dependencies.Clear();
                foreach (var dependencyPath in dependencyPaths)
                {
                    GatherDependencies(dependencyPath, dependencyMap, dependencies);
                }
            }
        }

        static void GatherDescendents(string assetPath, HashSet<string> descendentGuids, Dictionary<string, string[]> dependencyMap)
        {
            var dependencies = dependencyMap[assetPath];
            foreach (var dependency in dependencies)
            {
                if (descendentGuids.Add(AssetDatabase.AssetPathToGUID(dependency)))
                {
                    GatherDescendents(dependency, descendentGuids, dependencyMap);
                }
            }
        }

        static bool ContainsCircularDependency(string assetPath, Dictionary<string, string[]> dependencyMap, List<string> ancestors)
        {
            if (ancestors.Contains(assetPath))
            {
                return true;
            }

            ancestors.Add(assetPath);
            foreach (var dependencyPath in dependencyMap[assetPath])
            {
                if (ContainsCircularDependency(dependencyPath, dependencyMap, ancestors))
                {
                    return true;
                }
            }
            ancestors.RemoveAt(ancestors.Count - 1);

            return false;
        }
    }
}
