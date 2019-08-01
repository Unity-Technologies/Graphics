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
using UnityEditor.ShaderGraph.Internal;
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
                //Debug.Log(vfxAsset.GetCode(new []{ vfxAsset.positionOutput, vfxAsset.baseColorOutput, vfxAsset.metallicOutput, vfxAsset.smoothnessOutput, vfxAsset.alphaOutput }).code);
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
            var nl = Environment.NewLine;
            var indent = new string(' ', 4);
            var asset = ScriptableObject.CreateInstance<ShaderGraphVfxAsset>();
            var result = asset.compilationResult = new GraphCompilationResult();
            var mode = GenerationMode.ForReals;
            var graph = masterNode.owner;

            var assetGuid = masterNode.owner.assetGuid;
            var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            var hlslName = NodeUtils.GetHLSLSafeName(Path.GetFileNameWithoutExtension(assetPath));

            var ports = new List<MaterialSlot>();
            masterNode.GetInputSlots(ports);

            var nodes = new List<AbstractMaterialNode>();
            NodeUtils.DepthFirstCollectNodesFromNode(nodes, masterNode);

            var bodySb = new ShaderStringBuilder(1);
            var registry = new FunctionRegistry(new ShaderStringBuilder(), true);

            foreach (var properties in graph.properties)
            {
                properties.ValidateConcretePrecision(graph.concretePrecision);
            }

            foreach (var node in nodes)
            {
                if (node is IGeneratesBodyCode bodyGenerator)
                {
                    bodySb.currentNode = node;
                    bodyGenerator.GenerateNodeCode(bodySb, mode);
                    bodySb.ReplaceInCurrentMapping(PrecisionUtil.Token, node.concretePrecision.ToShaderString());
                }

                if (node is IGeneratesFunction generatesFunction)
                {
                    registry.builder.currentNode = node;
                    generatesFunction.GenerateNodeFunction(registry, mode);
                }
            }

            var portNodeSets = new HashSet<AbstractMaterialNode>[ports.Count];
            for (var portIndex = 0; portIndex < ports.Count; portIndex++)
            {
                var port = ports[portIndex];
                var nodeSet = new HashSet<AbstractMaterialNode>();
                NodeUtils.CollectNodeSet(nodeSet, port);
                portNodeSets[portIndex] = nodeSet;
            }

            var portPropertySets = new HashSet<Guid>[ports.Count];
            for (var portIndex = 0; portIndex < ports.Count; portIndex++)
            {
                portPropertySets[portIndex] = new HashSet<Guid>();
            }

            foreach (var node in nodes)
            {
                if (!(node is PropertyNode propertyNode))
                {
                    continue;
                }

                for (var portIndex = 0; portIndex < ports.Count; portIndex++)
                {
                    var portNodeSet = portNodeSets[portIndex];
                    if (portNodeSet.Contains(node))
                    {
                        portPropertySets[portIndex].Add(propertyNode.propertyGuid);
                    }
                }
            }

            var codeSnippets = new List<string>();
            var portCodeIndices = new List<int>[ports.Count];
            var sharedCodeIndices = new List<int>();
            for (var i = 0; i < portCodeIndices.Length; i++)
            {
                portCodeIndices[i] = new List<int>();
            }

            sharedCodeIndices.Add(codeSnippets.Count);
            codeSnippets.Add($"#include \"Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl\"{nl}");

            for (var registryIndex = 0; registryIndex < registry.names.Count; registryIndex++)
            {
                var name = registry.names[registryIndex];
                var source = registry.sources[name];
                var precision = source.nodes.First().concretePrecision;

                var hasPrecisionMismatch = false;
                var nodeNames = new HashSet<string>();
                foreach (var node in source.nodes)
                {
                    nodeNames.Add(node.name);
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
                code = $"// Node: {string.Join(", ", nodeNames)}{nl}{code}";
                var codeIndex = codeSnippets.Count;
                codeSnippets.Add(code + nl);
                for (var portIndex = 0; portIndex < ports.Count; portIndex++)
                {
                    var portNodeSet = portNodeSets[portIndex];
                    foreach (var node in source.nodes)
                    {
                        if (portNodeSet.Contains(node))
                        {
                            portCodeIndices[portIndex].Add(codeIndex);
                            break;
                        }
                    }
                }
            }

            foreach (var property in graph.properties)
            {
                if (property.isExposable && property.generatePropertyBlock)
                {
                    continue;
                }

                for (var portIndex = 0; portIndex < ports.Count; portIndex++)
                {
                    var portPropertySet = portPropertySets[portIndex];
                    if (portPropertySet.Contains(property.guid))
                    {
                        portCodeIndices[portIndex].Add(codeSnippets.Count);
                    }
                }

                codeSnippets.Add($"// Property: {property.displayName}{nl}{property.GetPropertyDeclarationString()}{nl}{nl}");
            }

            var inputStructName = $"SG_Input_{assetGuid}";
            var outputStructName = $"SG_Output_{assetGuid}";
            var evaluationFunctionName = $"SG_Evaluate_{assetGuid}";

            #region Input Struct

            sharedCodeIndices.Add(codeSnippets.Count);
            codeSnippets.Add($"struct {inputStructName}{nl}{{{nl}");

            #region Requirements

            var portRequirements = new ShaderGraphRequirements[ports.Count];
            for (var portIndex = 0; portIndex < ports.Count; portIndex++)
            {
                portRequirements[portIndex] = ShaderGraphRequirements.FromNodes(portNodeSets[portIndex].ToList(), ports[portIndex].stageCapability);
            }

            var portIndices = new List<int>();
            portIndices.Capacity = ports.Count;

            void AddRequirementsSnippet(Func<ShaderGraphRequirements, bool> predicate, string snippet)
            {
                portIndices.Clear();
                for (var portIndex = 0; portIndex < ports.Count; portIndex++)
                {
                    if (predicate(portRequirements[portIndex]))
                    {
                        portIndices.Add(portIndex);
                    }
                }

                if (portIndices.Count > 0)
                {
                    foreach (var portIndex in portIndices)
                    {
                        portCodeIndices[portIndex].Add(codeSnippets.Count);
                    }

                    codeSnippets.Add($"{indent}{snippet};{nl}");
                }
            }

            void AddCoordinateSpaceSnippets(InterpolatorType interpolatorType, Func<ShaderGraphRequirements, NeededCoordinateSpace> selector)
            {
                foreach (var space in EnumInfo<CoordinateSpace>.values)
                {
                    var neededSpace = space.ToNeededCoordinateSpace();
                    AddRequirementsSnippet(r => (selector(r) & neededSpace) > 0, $"float3 {space.ToVariableName(interpolatorType)}");
                }
            }

            // TODO: Rework requirements system to make this better
            AddCoordinateSpaceSnippets(InterpolatorType.Normal, r => r.requiresNormal);
            AddCoordinateSpaceSnippets(InterpolatorType.Tangent, r => r.requiresTangent);
            AddCoordinateSpaceSnippets(InterpolatorType.BiTangent, r => r.requiresBitangent);
            AddCoordinateSpaceSnippets(InterpolatorType.ViewDirection, r => r.requiresViewDir);
            AddCoordinateSpaceSnippets(InterpolatorType.Position, r => r.requiresPosition);

            AddRequirementsSnippet(r => r.requiresVertexColor, $"float4 {ShaderGeneratorNames.VertexColor}");
            AddRequirementsSnippet(r => r.requiresScreenPosition, $"float4 {ShaderGeneratorNames.ScreenPosition}");
            AddRequirementsSnippet(r => r.requiresFaceSign, $"float4 {ShaderGeneratorNames.FaceSign}");

            foreach (var uvChannel in EnumInfo<UVChannel>.values)
            {
                AddRequirementsSnippet(r => r.requiresMeshUVs.Contains(uvChannel), $"half4 {uvChannel.GetUVName()}");
            }

            AddRequirementsSnippet(r => r.requiresTime, $"float3 {ShaderGeneratorNames.TimeParameters}");

            #endregion

            sharedCodeIndices.Add(codeSnippets.Count);
            codeSnippets.Add($"}};{nl}{nl}");

            #endregion

            #region Output Struct

            sharedCodeIndices.Add(codeSnippets.Count);
            codeSnippets.Add($"struct {outputStructName}{nl}{{");

            for (var portIndex = 0; portIndex < ports.Count; portIndex++)
            {
                var port = ports[portIndex];
                portCodeIndices[portIndex].Add(codeSnippets.Count);
                codeSnippets.Add($"{nl}{indent}{port.concreteValueType.ToShaderString(graph.concretePrecision)} {port.shaderOutputName}_{port.id};");
            }

            sharedCodeIndices.Add(codeSnippets.Count);
            codeSnippets.Add($"{nl}}};{nl}{nl}");

            #endregion

            #region Graph Function

            sharedCodeIndices.Add(codeSnippets.Count);
            codeSnippets.Add($"{outputStructName} {evaluationFunctionName}({nl}{indent}{inputStructName} IN");

            var inputProperties = new List<AbstractShaderProperty>();
            var portPropertyIndices = new List<int>[ports.Count];
            for (var portIndex = 0; portIndex < ports.Count; portIndex++)
            {
                portPropertyIndices[portIndex] = new List<int>();
            }

            foreach (var property in graph.properties)
            {
                if (!property.isExposable || !property.generatePropertyBlock)
                {
                    continue;
                }

                var propertyIndex = inputProperties.Count;
                var codeIndex = codeSnippets.Count;

                for (var portIndex = 0; portIndex < ports.Count; portIndex++)
                {
                    var portPropertySet = portPropertySets[portIndex];
                    if (portPropertySet.Contains(property.guid))
                    {
                        portCodeIndices[portIndex].Add(codeIndex);
                        portPropertyIndices[portIndex].Add(propertyIndex);
                    }
                }

                inputProperties.Add(property);
                codeSnippets.Add($",{nl}{indent}/* Property: {property.displayName} */ {property.GetPropertyAsArgumentString()}");
            }

            sharedCodeIndices.Add(codeSnippets.Count);
            codeSnippets.Add($"){nl}{{");

            #region Node Code

            for (var mappingIndex = 0; mappingIndex < bodySb.mappings.Count; mappingIndex++)
            {
                var mapping = bodySb.mappings[mappingIndex];
                var code = bodySb.ToString(mapping.startIndex, mapping.count);
                if (string.IsNullOrWhiteSpace(code))
                {
                    continue;
                }

                code = $"{nl}{indent}// Node: {mapping.node.name}{nl}{code}";
                var codeIndex = codeSnippets.Count;
                codeSnippets.Add(code);
                for (var portIndex = 0; portIndex < ports.Count; portIndex++)
                {
                    var portNodeSet = portNodeSets[portIndex];
                    if (portNodeSet.Contains(mapping.node))
                    {
                        portCodeIndices[portIndex].Add(codeIndex);
                        break;
                    }
                }
            }

            #endregion

            #region Output Mapping

            sharedCodeIndices.Add(codeSnippets.Count);
            codeSnippets.Add($"{nl}{indent}// {masterNode.name}{nl}{indent}{outputStructName} OUT;{nl}");

            // Output mapping
            for (var portIndex = 0; portIndex < ports.Count; portIndex++)
            {
                var port = ports[portIndex];
                portCodeIndices[portIndex].Add(codeSnippets.Count);
                codeSnippets.Add($"{indent}OUT.{port.shaderOutputName}_{port.id} = {masterNode.GetSlotValue(port.id, GenerationMode.ForReals, graph.concretePrecision)};{nl}");
            }

            #endregion

            // Function end
            sharedCodeIndices.Add(codeSnippets.Count);
            codeSnippets.Add($"{indent}return OUT;{nl}}}{nl}");

            #endregion

            result.codeSnippets = codeSnippets.ToArray();
            result.sharedCodeIndices = sharedCodeIndices.ToArray();
            result.outputCodeIndices = new IntArray[ports.Count];
            for (var i = 0; i < ports.Count; i++)
            {
                result.outputCodeIndices[i] = portCodeIndices[i].ToArray();
            }

            var positionPort = ports.First(x => x.id == VfxMasterNode.PositionSlotId);
            asset.positionOutput = new OutputMetadata(ports.IndexOf(positionPort), positionPort.shaderOutputName);

            var baseColorPort = ports.First(x => x.id == VfxMasterNode.BaseColorSlotId);
            asset.baseColorOutput = new OutputMetadata(ports.IndexOf(baseColorPort), baseColorPort.shaderOutputName);

            var metallicPort = ports.First(x => x.id == VfxMasterNode.MetallicSlotId);
            asset.metallicOutput = new OutputMetadata(ports.IndexOf(metallicPort), metallicPort.shaderOutputName);

            var smoothnessPort = ports.First(x => x.id == VfxMasterNode.SmoothnessSlotId);
            asset.smoothnessOutput = new OutputMetadata(ports.IndexOf(smoothnessPort), smoothnessPort.shaderOutputName);

            var alphaPort = ports.First(x => x.id == VfxMasterNode.AlphaSlotId);
            asset.alphaOutput = new OutputMetadata(ports.IndexOf(alphaPort), alphaPort.shaderOutputName);

            asset.evaluationFunctionName = evaluationFunctionName;
            asset.inputStructName = inputStructName;
            asset.outputStructName = outputStructName;
            asset.portRequirements = portRequirements;
            asset.concretePrecision = graph.concretePrecision;
            asset.SetProperties(inputProperties);
            asset.outputPropertyIndices = new IntArray[ports.Count];
            for (var portIndex = 0; portIndex < ports.Count; portIndex++)
            {
                asset.outputPropertyIndices[portIndex] = portPropertyIndices[portIndex].ToArray();
            }

            return asset;
        }
    }
}
