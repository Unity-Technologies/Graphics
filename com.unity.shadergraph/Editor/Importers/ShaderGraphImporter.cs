using UnityEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.VisualEffectGraph;
using Object = System.Object;

namespace UnityEditor.ShaderGraph
{
    [ScriptedImporter(29, Extension, 3)]
    class ShaderGraphImporter : ScriptedImporter
    {
        public const string Extension = "shadergraph";

        public const string k_ErrorShader = @"
Shader ""Hidden/GraphErrorShader2""
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #include ""UnityCG.cginc""

            struct appdata_t {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(1,0,1,1);
            }
            ENDCG
        }
    }
    Fallback Off
}";

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        static string[] GatherDependenciesFromSourceFile(string assetPath)
        {
            return MinimalGraphData.GetDependencyPaths(assetPath);
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var oldShader = AssetDatabase.LoadAssetAtPath<Shader>(ctx.assetPath);
            if (oldShader != null)
                ShaderUtil.ClearShaderMessages(oldShader);

            string path = ctx.assetPath;
            var sourceAssetDependencyPaths = new List<string>();
            GraphData graph = null;
            var shaderName = Path.GetFileNameWithoutExtension(path);
            try
            {
                var textGraph = File.ReadAllText(path, Encoding.UTF8);
                graph = JsonUtility.FromJson<GraphData>(textGraph);
                graph.messageManager = new MessageManager();
                graph.assetGuid = AssetDatabase.AssetPathToGUID(path);
                graph.OnEnable();
                graph.ValidateGraph();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (graph == null)
            {
                return;
            }

            var masterNode = (MasterNode)graph.outputNode;
            UnityEngine.Object mainObject;

            if (masterNode is VfxMasterNode vfxMasterNode)
            {
                var vfxAsset = GenerateVfxShaderGraphAsset(vfxMasterNode);
                Debug.Log(vfxAsset.compilationResult.GenerateCode("ShaderGraph", new[] { 0, 1 }));
                Debug.Log(vfxAsset.compilationResult.GenerateCode("ShaderGraph", new[] { 0 }));
                Debug.Log(vfxAsset.compilationResult.GenerateCode("ShaderGraph", new[] { 1 }));
                mainObject = vfxAsset;
            }
            else
            {
                string shaderString = null;
                List<PropertyCollector.TextureInfo> configuredTextures;

                try
                {
                    if (!string.IsNullOrEmpty(graph.path))
                        shaderName = graph.path + "/" + shaderName;
                    shaderString = ((IMasterNode)graph.outputNode).GetShader(GenerationMode.ForReals, shaderName, out configuredTextures, sourceAssetDependencyPaths);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    configuredTextures = new List<PropertyCollector.TextureInfo>();
                }

                if (graph.messageManager.nodeMessagesChanged)
                {
                    shaderString = null;
                }

                var text = shaderString ?? k_ErrorShader.Replace("Hidden/GraphErrorShader2", shaderName);
                var shader = ShaderUtil.CreateShaderAsset(text);
                mainObject = shader;

                EditorMaterialUtility.SetShaderDefaults(
                    shader,
                    configuredTextures.Where(x => x.modifiable).Select(x => x.name).ToArray(),
                    configuredTextures.Where(x => x.modifiable).Select(x => EditorUtility.InstanceIDToObject(x.textureId) as Texture).ToArray());
                EditorMaterialUtility.SetShaderNonModifiableDefaults(
                    shader,
                    configuredTextures.Where(x => !x.modifiable).Select(x => x.name).ToArray(),
                    configuredTextures.Where(x => !x.modifiable).Select(x => EditorUtility.InstanceIDToObject(x.textureId) as Texture).ToArray());
            }

            if (graph.messageManager.nodeMessagesChanged)
            {
                foreach (var pair in graph.messageManager.GetNodeMessages())
                {
                    var node = graph.GetNodeFromTempId(pair.Key);
                    MessageManager.Log(node, path, pair.Value.First(), mainObject);
                }
            }

            Texture2D texture = Resources.Load<Texture2D>("Icons/sg_graph_icon@64");
            var identifier = mainObject is Shader ? "MainAsset" : "VfxShaderGraphAsset";
            ctx.AddObjectToAsset(identifier, mainObject, texture);
            ctx.SetMainObject(mainObject);

            foreach (var sourceAssetDependencyPath in sourceAssetDependencyPaths.Distinct())
            {
                // Ensure that dependency path is relative to project
                if (!sourceAssetDependencyPath.StartsWith("Packages/") && !sourceAssetDependencyPath.StartsWith("Assets/"))
                {
                    Debug.LogWarning($"Invalid dependency path: {sourceAssetDependencyPath}", mainObject);
                    continue;
                }

                ctx.DependsOnSourceAsset(sourceAssetDependencyPath);
            }
        }

        static ShaderGraphVfxAsset GenerateVfxShaderGraphAsset(VfxMasterNode masterNode)
        {
            var asset = ScriptableObject.CreateInstance<ShaderGraphVfxAsset>();
            var result = asset.compilationResult = new GraphCompilationResult();
            var mode = GenerationMode.ForReals;
//            var graph = masterNode.owner;

//            var assetGuid = masterNode.owner.assetGuid;
//            var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
//            var hlslName = NodeUtils.GetHLSLSafeName(Path.GetFileNameWithoutExtension(assetPath));

            var ports = new List<MaterialSlot>();
            masterNode.GetInputSlots(ports);

            var nodes = new List<AbstractMaterialNode>();
            NodeUtils.DepthFirstCollectNodesFromNode(nodes, masterNode);

            var bodySb = new ShaderStringBuilder(1);
            var registry = new FunctionRegistry(new ShaderStringBuilder(), true);

            foreach (var node in nodes)
            {
                // TODO: Handle global code (IGeneratesFunction)
                if (node is IGeneratesBodyCode bodyGenerator)
                {
                    bodySb.currentNode = node;
                    bodySb.AppendLine($"// {node.name} ({node.guid})");
                    bodyGenerator.GenerateNodeCode(bodySb, mode);
                    bodySb.ReplaceInCurrentMapping(PrecisionUtil.Token, node.concretePrecision.ToShaderString());
                }

                if (node is IGeneratesFunction generatesFunction)
                {
                    registry.builder.currentNode = node;
                    generatesFunction.GenerateNodeFunction(registry, mode);
                }
            }

            var portNodeSets = new List<HashSet<AbstractMaterialNode>>();
            foreach (var port in ports)
            {
                var nodeSet = new HashSet<AbstractMaterialNode>();
                NodeUtils.CollectNodeSet(nodeSet, port);
                portNodeSets.Add(nodeSet);
            }

            var bodyCodes = new List<string>();
            var portBodyCodeIndices = new List<int>[ports.Count];
            for (var i = 0; i < ports.Count; i++)
            {
                portBodyCodeIndices[i] = new List<int>();
            }

            for (var codeIndex = 0; codeIndex < bodySb.mappings.Count; codeIndex++)
            {
                var mapping = bodySb.mappings[codeIndex];
                var code = bodySb.ToString(mapping.startIndex, mapping.count);
                bodyCodes.Add(code);
                for (var portIndex = 0; portIndex < ports.Count; portIndex++)
                {
                    var portNodeSet = portNodeSets[portIndex];
                    if (portNodeSet.Contains(mapping.node))
                    {
                        portBodyCodeIndices[portIndex].Add(codeIndex);
                        break;
                    }
                }
            }

            result.bodyCodes = bodyCodes.ToArray();
            result.portBodyCodeIndices = new int[portBodyCodeIndices.Length][];
            for (var i = 0; i < portBodyCodeIndices.Length; i++)
            {
                result.portBodyCodeIndices[i] = portBodyCodeIndices[i].ToArray();
            }

            var globalCodes = new List<string>();
            var portGlobalCodeIndices = new List<int>[ports.Count];
            for (var i = 0; i < ports.Count; i++)
            {
                portGlobalCodeIndices[i] = new List<int>();
            }

            for (var codeIndex = 0; codeIndex < registry.names.Count; codeIndex++)
            {
                var name = registry.names[codeIndex];
                var source = registry.sources[name];
                var precision = source.nodes.First().concretePrecision;

                var hasPrecisionMismatch = false;
                foreach (var node in source.nodes)
                {
                    if (node.concretePrecision != precision)
                    {
                        hasPrecisionMismatch = true;
                        break;
                    }
                }

                if (hasPrecisionMismatch)
                {
                    var message = new StringBuilder($"Precision mismatch for function {name}:");
                    foreach (var node in source.nodes)
                    {
                        message.AppendLine($"{node.name} ({node.guid}): {node.concretePrecision}");
                    }
                    throw new InvalidOperationException(message.ToString());
                }

                var code = source.code.Replace(PrecisionUtil.Token, precision.ToShaderString());
                globalCodes.Add(code);
                for (var portIndex = 0; portIndex < ports.Count; portIndex++)
                {
                    var portNodeSet = portNodeSets[portIndex];
                    foreach (var node in source.nodes)
                    {
                        if (portNodeSet.Contains(node))
                        {
                            portGlobalCodeIndices[portIndex].Add(codeIndex);
                            break;
                        }
                    }
                }
            }

            result.globalCodes = globalCodes.ToArray();
            result.portGlobalCodeIndices = new int[portGlobalCodeIndices.Length][];
            for (var i = 0; i < portGlobalCodeIndices.Length; i++)
            {
                result.portGlobalCodeIndices[i] = portGlobalCodeIndices[i].ToArray();
            }

            return asset;
        }

//        static GraphFunction GenerateGraphFunction(VfxMasterNode masterNode, ShaderStage stage)
//        {
//            var function = new GraphFunction();
//
//            var graph = masterNode.owner;
//
//            var assetGuid = masterNode.owner.assetGuid;
//            var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
//            var hlslName = NodeUtils.GetHLSLSafeName(Path.GetFileNameWithoutExtension(assetPath));
//            function.inputStructName = $"SG_{stage}Input_{hlslName}_{assetGuid}";
//            function.outputStructName = $"SG_{stage}Input_{hlslName}_{assetGuid}";
//            function.functionName = $"SG_{stage}_{hlslName}_{assetGuid}";
//
//            // Master node input slots represents graph output
//            var slots = new List<MaterialSlot>();
//            masterNode.GetInputSlots(slots);
//            slots.RemoveAll(x => !x.stageCapability.TryGetShaderStage(out var slotStage) || slotStage != stage);
//            var slotIds = slots.Select(x => x.id).ToList();
//
//            List<AbstractMaterialNode> nodes = new List<AbstractMaterialNode>();
//            NodeUtils.DepthFirstCollectNodesFromNode(nodes, masterNode, slotIds: slotIds);
//
//            var requirements = ShaderGraphRequirements.FromNodes(nodes, (ShaderStageCapability)stage);
//
//            var sb = new ShaderStringBuilder();
//
//            // TODO: Include properties in struct
//            // TODO: Meta-data
//            GraphUtil.GenerateSurfaceInputStruct(sb, requirements, function.inputStructName);
//            sb.AppendNewLine();
//
//            sb.AppendLine($"struct {function.outputStructName}");
//            using (sb.BlockSemicolonScope())
//            {
//                foreach (var slot in slots)
//                {
//                    sb.AppendLine($"{slot.concreteValueType.ToShaderString(graph.concretePrecision)} {slot.shaderOutputName}_{slot.id};");
//                }
//            }
//
//            sb.AppendLine($"{function.outputStructName} {function.functionName}({function.inputStructName} IN)");
//            using (sb.BlockScope())
//            {
//                sb.AppendLine($"{function.outputStructName} OUT = ({function.outputStructName}){0};");
//            }
//
//            function.code = sb.ToString();
//
//            return function;
//        }
    }
}
