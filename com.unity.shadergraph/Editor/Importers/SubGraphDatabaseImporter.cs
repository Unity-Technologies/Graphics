using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.Rendering;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [ScriptedImporter(1, ".sgsubgraphdb", 2)]
    class SubGraphDatabaseImporter : ScriptedImporter
    {
        public const string path = "Packages/com.unity.shadergraph/Editor/Importers/_.sgsubgraphdb";

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var currentTime = DateTime.Now.Ticks;
            if (ctx.assetPath != path)
            {
                ctx.LogImportError("The sgpostsubgraph extension may only be used internally by Shader Graph.");
                return;
            }

            if (SubGraphDatabase.instance == null)
            {
                SubGraphDatabase.instance = ScriptableObject.CreateInstance<SubGraphDatabase>();
            }
            var database = SubGraphDatabase.instance;

            var allSubGraphGuids = AssetDatabase.FindAssets($"t:{nameof(SubGraphAsset)}").ToList();
            allSubGraphGuids.Sort();
            var subGraphMap = new Dictionary<string, SubGraphData>();
            var graphDataMap = new Dictionary<string, GraphData>();

            foreach (var subGraphData in database.subGraphs)
            {
                if (allSubGraphGuids.BinarySearch(subGraphData.assetGuid) >= 0)
                {
                    subGraphMap.Add(subGraphData.assetGuid, subGraphData);
                }
            }
            
            var dirtySubGraphGuids = new List<string>();

            foreach (var subGraphGuid in allSubGraphGuids)
            {
                var subGraphAsset = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(AssetDatabase.GUIDToAssetPath(subGraphGuid));
                if (!subGraphMap.TryGetValue(subGraphGuid, out var subGraphData))
                {
                    subGraphData = new SubGraphData();
                }
                
                if (subGraphAsset.importedAt > subGraphData.processedAt)
                {
                    dirtySubGraphGuids.Add(subGraphGuid);
                    subGraphData.Reset();
                    subGraphData.processedAt = currentTime;
                    var subGraphPath = AssetDatabase.GUIDToAssetPath(subGraphGuid);
                    var textGraph = File.ReadAllText(subGraphPath, Encoding.UTF8);
                    var graphData = new GraphData { isSubGraph = true, assetGuid = subGraphGuid };
                    JsonUtility.FromJsonOverwrite(textGraph, graphData);
                    subGraphData.children.AddRange(graphData.GetNodes<SubGraphNode>().Select(x => x.subGraphGuid).Distinct());
                    subGraphData.assetGuid = subGraphGuid;
                    subGraphMap[subGraphGuid] = subGraphData;
                    graphDataMap[subGraphGuid] = graphData;
                }
                else
                {
                    subGraphData.ancestors.Clear();
                    subGraphData.descendents.Clear();
                    subGraphData.isRecursive = false;
                }
            }

            database.subGraphs.Clear();
            database.subGraphs.AddRange(subGraphMap.Values);
            database.subGraphs.Sort((s1, s2) => s1.assetGuid.CompareTo(s2.assetGuid));
            database.subGraphGuids.Clear();
            database.subGraphGuids.AddRange(database.subGraphs.Select(x => x.assetGuid));

            var permanentMarks = new HashSet<string>();
            var stack = new Stack<string>(allSubGraphGuids.Count);

            // Detect recursion, and populate `ancestors` and `descendents` per sub graph.
            foreach (var rootSubGraphData in database.subGraphs)
            {
                var rootSubGraphGuid = rootSubGraphData.assetGuid;
                stack.Push(rootSubGraphGuid);
                while (stack.Count > 0)
                {
                    var subGraphGuid = stack.Pop();
                    if (!permanentMarks.Add(subGraphGuid))
                    {
                        continue;
                    }
                    
                    var subGraphData = subGraphMap[subGraphGuid];
                    if (subGraphData != rootSubGraphData)
                    {
                        subGraphData.ancestors.Add(rootSubGraphGuid);
                        rootSubGraphData.descendents.Add(subGraphGuid);
                    }
                    foreach (var childSubGraphGuid in subGraphData.children)
                    {
                        if (childSubGraphGuid == rootSubGraphGuid)
                        {
                            rootSubGraphData.isRecursive = true;
                        }
                        else
                        {
                            stack.Push(childSubGraphGuid);
                        }
                    }
                }
                permanentMarks.Clear();
            }

            // Next up we build a list of sub graphs to be processed, which will later be sorted topologically.
            var sortedSubGraphs = new List<SubGraphData>();
            foreach (var subGraphGuid in dirtySubGraphGuids)
            {
                var subGraphData = subGraphMap[subGraphGuid];
                if (permanentMarks.Add(subGraphGuid))
                {
                    sortedSubGraphs.Add(subGraphData);
                }

                // Note that we're traversing up the graph via ancestors rather than descendents, because all Sub Graphs using the current sub graph needs to be re-processed.
                foreach (var ancestorGuid in subGraphData.ancestors)
                {
                    if (permanentMarks.Add(ancestorGuid))
                    {
                        var ancestorSubGraphData = subGraphMap[ancestorGuid];
                        sortedSubGraphs.Add(ancestorSubGraphData);
                    }
                }
            }
            permanentMarks.Clear();
            
            // Sort topologically. At this stage we can assume there are no loops because all recursive sub graphs have been filtered out.
            sortedSubGraphs.Sort((s1, s2) => s1.descendents.Contains(s2.assetGuid) ? 1 : s2.descendents.Contains(s1.assetGuid) ? -1 : 0);
            
            // Finally process the topologically sorted sub graphs without recursion.
            var registry = new FunctionRegistry(new ShaderStringBuilder(), true);
            var messageManager = new MessageManager();
            foreach (var subGraphData in sortedSubGraphs)
            {
                try
                {
                    var subGraphPath = AssetDatabase.GUIDToAssetPath(subGraphData.assetGuid);
                    if (!graphDataMap.TryGetValue(subGraphData.assetGuid, out var graphData))
                    {
                        var textGraph = File.ReadAllText(subGraphPath, Encoding.UTF8);
                        graphData = new GraphData { isSubGraph = true, assetGuid = subGraphData.assetGuid };
                        JsonUtility.FromJsonOverwrite(textGraph, graphData);
                    }

                    graphData.messageManager = messageManager;
                    ProcessSubGraph(subGraphMap, registry, subGraphData, graphData);
                    if (messageManager.nodeMessagesChanged)
                    {
                        var subGraphAsset = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(AssetDatabase.GUIDToAssetPath(subGraphData.assetGuid));
                        foreach (var pair in messageManager.GetNodeMessages())
                        {
                            var node = graphData.GetNodeFromTempId(pair.Key);
                            foreach (var message in pair.Value)
                            {
                                MessageManager.Log(node, subGraphPath, message, subGraphAsset);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    subGraphData.isValid = false;
                    var subGraphAsset = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(AssetDatabase.GUIDToAssetPath(subGraphData.assetGuid));
                    Debug.LogException(e, subGraphAsset);
                }
                finally
                {
                    subGraphData.processedAt = currentTime;
                    messageManager.ClearAll();
                }
            }
            
            // Carry over functions used by sub-graphs that were not re-processed in this import.
            foreach (var subGraphData in database.subGraphs)
            {
                foreach (var functionName in subGraphData.functionNames)
                {
                    if (!registry.sources.ContainsKey(functionName))
                    {
                        registry.sources.Add(functionName, database.functionSources[database.functionNames.BinarySearch(functionName)]);
                    }
                }
            }

            var functions = registry.sources.ToList();
            functions.Sort((p1, p2) => p1.Key.CompareTo(p2.Key));
            database.functionNames.Clear();
            database.functionSources.Clear();
            foreach (var pair in functions)
            {
                database.functionNames.Add(pair.Key);
                database.functionSources.Add(pair.Value);
            }

            ctx.AddObjectToAsset("MainAsset", database);
            ctx.SetMainObject(database);

            SubGraphDatabase.instance = null;
        }

        static void ProcessSubGraph(Dictionary<string, SubGraphData> subGraphMap, FunctionRegistry registry, SubGraphData subGraphData, GraphData graph)
        {
            registry.names.Clear();
            subGraphData.functionNames.Clear();
            subGraphData.nodeProperties.Clear();
            subGraphData.isValid = true;
            
            graph.OnEnable();
            graph.messageManager.ClearAll();
            graph.ValidateGraph();

            var assetPath = AssetDatabase.GUIDToAssetPath(subGraphData.assetGuid);
            subGraphData.hlslName = NodeUtils.GetHLSLSafeName(Path.GetFileNameWithoutExtension(assetPath));
            subGraphData.inputStructName = $"Bindings_{subGraphData.hlslName}_{subGraphData.assetGuid}";
            subGraphData.functionName = $"SG_{subGraphData.hlslName}_{subGraphData.assetGuid}";
            subGraphData.path = graph.path;

            var outputNode = (SubGraphOutputNode)graph.outputNode;
            
            subGraphData.outputs.Clear();
            outputNode.GetInputSlots(subGraphData.outputs);
            
            List<AbstractMaterialNode> nodes = new List<AbstractMaterialNode>();
            NodeUtils.DepthFirstCollectNodesFromNode(nodes, outputNode);

            subGraphData.effectiveShaderStage = ShaderStageCapability.All;
            foreach (var slot in subGraphData.outputs)
            {
                var stage = NodeUtils.GetEffectiveShaderStageCapability(slot, true);
                if (stage != ShaderStageCapability.All)
                {
                    subGraphData.effectiveShaderStage = stage;
                    break;
                }
            }

            subGraphData.requirements = ShaderGraphRequirements.FromNodes(nodes, subGraphData.effectiveShaderStage, false);
            subGraphData.inputs = graph.properties.ToList();

            foreach (var node in nodes)
            {
                if (node.hasError)
                {
                    subGraphData.isValid = false;
                    registry.ProvideFunction(subGraphData.functionName, sb => { });
                    return;
                }
            }
            
            foreach (var node in nodes)
            {
                if (node is SubGraphNode subGraphNode)
                {
                    var nestedData = subGraphMap[subGraphNode.subGraphGuid];
                    
                    foreach (var functionName in nestedData.functionNames)
                    {
                        registry.names.Add(functionName);
                    }
                }
                else if (node is IGeneratesFunction generatesFunction)
                {
                    generatesFunction.GenerateNodeFunction(registry, new GraphContext(subGraphData.inputStructName), GenerationMode.ForReals);
                }
            }

            registry.ProvideFunction(subGraphData.functionName, sb =>
            {
                var graphContext = new GraphContext(subGraphData.inputStructName);

                GraphUtil.GenerateSurfaceInputStruct(sb, subGraphData.requirements, subGraphData.inputStructName);
                sb.AppendNewLine();

                // Generate arguments... first INPUTS
                var arguments = new List<string>();
                foreach (var prop in subGraphData.inputs)
                    arguments.Add(string.Format("{0}", prop.GetPropertyAsArgumentString()));

                // now pass surface inputs
                arguments.Add(string.Format("{0} IN", subGraphData.inputStructName));

                // Now generate outputs
                foreach (var output in subGraphData.outputs)
                    arguments.Add($"out {output.concreteValueType.ToString(outputNode.precision)} {output.shaderOutputName}");

                // Create the function prototype from the arguments
                sb.AppendLine("void {0}({1})"
                    , subGraphData.functionName
                    , arguments.Aggregate((current, next) => $"{current}, {next}"));

                // now generate the function
                using (sb.BlockScope())
                {
                    // Just grab the body from the active nodes
                    var bodyGenerator = new ShaderGenerator();
                    foreach (var node in nodes)
                    {
                        if (node is IGeneratesBodyCode)
                            (node as IGeneratesBodyCode).GenerateNodeCode(bodyGenerator, graphContext, GenerationMode.ForReals);
                    }

                    foreach (var slot in subGraphData.outputs)
                        bodyGenerator.AddShaderChunk($"{slot.shaderOutputName} = {outputNode.GetSlotValue(slot.id, GenerationMode.ForReals)};");

                    sb.Append(bodyGenerator.GetShaderString(1));
                }
            });
            
            subGraphData.functionNames.AddRange(registry.names.Distinct());

            var collector = new PropertyCollector();
            subGraphData.nodeProperties = collector.properties;
            foreach (var node in nodes)
            {
                node.CollectShaderProperties(collector, GenerationMode.ForReals);
            }
            
            subGraphData.OnBeforeSerialize();
        }
    }
}
