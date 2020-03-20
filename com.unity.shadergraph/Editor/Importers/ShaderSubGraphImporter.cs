using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [ExcludeFromPreset]
    [ScriptedImporter(11, Extension)]
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
            var graphData = new GraphData { isSubGraph = true, assetGuid = subGraphGuid };
            var messageManager = new MessageManager();
            graphData.messageManager = messageManager;
            JsonUtility.FromJsonOverwrite(textGraph, graphData);

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
                if (messageManager.nodeMessagesChanged)
                {
                    graphAsset.isValid = false;
                    foreach (var pair in messageManager.GetNodeMessages())
                    {
                        var node = graphData.GetNodeFromGuid(pair.Key);
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
        }

        static void ProcessSubGraph(SubGraphAsset asset, GraphData graph)
        {
            var registry = new FunctionRegistry(new ShaderStringBuilder(), true);
            registry.names.Clear();
            asset.functions.Clear();
            asset.nodeProperties.Clear();
            asset.isValid = true;

            graph.OnEnable();
            graph.messageManager.ClearAll();
            graph.ValidateGraph();

            var assetPath = AssetDatabase.GUIDToAssetPath(asset.assetGuid);
            asset.hlslName = NodeUtils.GetHLSLSafeName(Path.GetFileNameWithoutExtension(assetPath));
            asset.inputStructName = $"Bindings_{asset.hlslName}_{asset.assetGuid}";
            asset.functionName = $"SG_{asset.hlslName}_{asset.assetGuid}";
            asset.path = graph.path;

            var outputNode = (SubGraphOutputNode)graph.outputNode;

            asset.outputs.Clear();
            outputNode.GetInputSlots(asset.outputs);

            List<AbstractMaterialNode> nodes = new List<AbstractMaterialNode>();
            NodeUtils.DepthFirstCollectNodesFromNode(nodes, outputNode);

            asset.effectiveShaderStage = ShaderStageCapability.All;
            foreach (var slot in asset.outputs)
            {
                var stage = NodeUtils.GetEffectiveShaderStageCapability(slot, true);
                if (stage != ShaderStageCapability.All)
                {
                    asset.effectiveShaderStage = stage;
                    break;
                }
            }

            asset.requirements = ShaderGraphRequirements.FromNodes(nodes, asset.effectiveShaderStage, false);
            asset.inputs = graph.properties.ToList();
            asset.keywords = graph.keywords.ToList();
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
                foreach (var prop in asset.inputs)
                {
                    prop.ValidateConcretePrecision(asset.graphPrecision);
                    arguments.Add(string.Format("{0}", prop.GetPropertyAsArgumentString()));
                }

                // now pass surface inputs
                arguments.Add(string.Format("{0} IN", asset.inputStructName));

                // Now generate outputs
                foreach (var output in asset.outputs)
                    arguments.Add($"out {output.concreteValueType.ToShaderString(asset.outputPrecision)} {output.shaderOutputName}_{output.id}");

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

                    foreach (var slot in asset.outputs)
                    {
                        sb.AppendLine($"{slot.shaderOutputName}_{slot.id} = {outputNode.GetSlotValue(slot.id, GenerationMode.ForReals, asset.outputPrecision)};");
                    }
                }
            });

            asset.functions.AddRange(registry.names.Select(x => new FunctionPair(x, registry.sources[x].code)));

            var collector = new PropertyCollector();
            asset.nodeProperties = collector.properties;
            foreach (var node in nodes)
            {
                node.CollectShaderProperties(collector, GenerationMode.ForReals);
            }

            asset.OnBeforeSerialize();
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
