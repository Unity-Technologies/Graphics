using UnityEngine;
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
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Serialization;
using Object = System.Object;

namespace UnityEditor.ShaderGraph
{
    [ExcludeFromPreset]
#if ENABLE_HYBRID_RENDERER_V2
    // Bump the version number when Hybrid Renderer V2 is enabled, to make
    // sure that all shader graphs get re-imported. Re-importing is required,
    // because the shader graph codegen is different for V2.
    // This ifdef can be removed once V2 is the only option.
    [ScriptedImporter(114, Extension, -902)]
#else
    [ScriptedImporter(46, Extension, -902)]
#endif

    class ShaderGraphImporter : ScriptedImporter
    {
        public const string Extension = "shadergraph";
        public const string LegacyExtension = "ShaderGraph";

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
            var oldShader = AssetDatabase.LoadAssetAtPath<Shader>(ctx.assetPath);
            if (oldShader != null)
                ShaderUtil.ClearShaderMessages(oldShader);

            List<PropertyCollector.TextureInfo> configuredTextures;
            string path = ctx.assetPath;

            AssetCollection assetCollection = new AssetCollection();
            MinimalGraphData.GatherMinimalDependenciesFromFile(assetPath, assetCollection);

            var textGraph = File.ReadAllText(path, Encoding.UTF8);
            var graph = new GraphData
            {
                messageManager = new MessageManager(), assetGuid = AssetDatabase.AssetPathToGUID(path)
            };
            MultiJson.Deserialize(graph, textGraph);
            graph.OnEnable();
            graph.ValidateGraph();

            Shader shader = null;
#if VFX_GRAPH_10_0_0_OR_NEWER
            if (!graph.isOnlyVFXTarget)
#endif
            {
                // build the shader text
                // this will also add Target dependencies into the asset collection
                var text = GetShaderText(path, out configuredTextures, assetCollection, graph);

#if UNITY_2021_1_OR_NEWER
                // 2021.1 or later is guaranteed to have the new version of this function
                shader = ShaderUtil.CreateShaderAsset(ctx, text, false);
#else
                // earlier builds of Unity may or may not have it
                // here we try to invoke the new version via reflection
                var createShaderAssetMethod = typeof(ShaderUtil).GetMethod(
                    "CreateShaderAsset",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.ExactBinding,
                    null,
                    new Type[] { typeof(AssetImportContext), typeof(string), typeof(bool) },
                    null);

                if (createShaderAssetMethod != null)
                {
                    shader = createShaderAssetMethod.Invoke(null, new Object[] { ctx, text, false }) as Shader;
                }
                else
                {
                    // method doesn't exist in this version of Unity, call old version
                    // this doesn't create dependencies properly, but is the best that we can do
                    shader = ShaderUtil.CreateShaderAsset(text, false);
                }
#endif

                if (graph.messageManager.nodeMessagesChanged)
                {
                    foreach (var pair in graph.messageManager.GetNodeMessages())
                    {
                        var node = graph.GetNodeFromId(pair.Key);
                        MessageManager.Log(node, path, pair.Value.First(), shader);
                    }
                }

                EditorMaterialUtility.SetShaderDefaults(
                    shader,
                    configuredTextures.Where(x => x.modifiable).Select(x => x.name).ToArray(),
                    configuredTextures.Where(x => x.modifiable).Select(x => EditorUtility.InstanceIDToObject(x.textureId) as Texture).ToArray());
                EditorMaterialUtility.SetShaderNonModifiableDefaults(
                    shader,
                    configuredTextures.Where(x => !x.modifiable).Select(x => x.name).ToArray(),
                    configuredTextures.Where(x => !x.modifiable).Select(x => EditorUtility.InstanceIDToObject(x.textureId) as Texture).ToArray());
            }

            UnityEngine.Object mainObject = shader;
#if VFX_GRAPH_10_0_0_OR_NEWER
            ShaderGraphVfxAsset vfxAsset = null;
            if (graph.hasVFXTarget)
            {
                vfxAsset = GenerateVfxShaderGraphAsset(graph);
                if (mainObject == null)
                {
                    mainObject = vfxAsset;
                }
                else
                {
                    //Correct main object if we have a shader and ShaderGraphVfxAsset : save as sub asset
                    vfxAsset.name = Path.GetFileNameWithoutExtension(path);
                    ctx.AddObjectToAsset("VFXShaderGraph", vfxAsset);
                }
            }
#endif

            Texture2D texture = Resources.Load<Texture2D>("Icons/sg_graph_icon@64");
            ctx.AddObjectToAsset("MainAsset", mainObject, texture);
            ctx.SetMainObject(mainObject);

            foreach(var target in graph.activeTargets)
            {
                if(target is IHasMetadata iHasMetadata)
                {
                    var metadata = iHasMetadata.GetMetadataObject();
                    if(metadata == null)
                        continue;

                    metadata.hideFlags = HideFlags.HideInHierarchy;
                    ctx.AddObjectToAsset($"{iHasMetadata.identifier}:Metadata", metadata);
                }
            }

            var sgMetadata = ScriptableObject.CreateInstance<ShaderGraphMetadata>();
            sgMetadata.hideFlags = HideFlags.HideInHierarchy;
            sgMetadata.assetDependencies = new List<UnityEngine.Object>();

            foreach (var asset in assetCollection.assets)
            {
                if (asset.Value.HasFlag(AssetCollection.Flags.IncludeInExportPackage))
                {
                    // this sucks that we have to fully load these assets just to set the reference,
                    // which then gets serialized as the GUID that we already have here.  :P

                    var dependencyPath = AssetDatabase.GUIDToAssetPath(asset.Key);
                    if (!string.IsNullOrEmpty(dependencyPath))
                    {
                        sgMetadata.assetDependencies.Add(
                            AssetDatabase.LoadAssetAtPath(dependencyPath, typeof(UnityEngine.Object)));
                    }
                }
            }
            ctx.AddObjectToAsset("SGInternal:Metadata", sgMetadata);

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
                        Debug.LogWarning($"Invalid dependency path: {assetPath}", mainObject);
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

        internal static string GetShaderText(string path, out List<PropertyCollector.TextureInfo> configuredTextures, AssetCollection assetCollection, GraphData graph)
        {
            string shaderString = null;
            var shaderName = Path.GetFileNameWithoutExtension(path);
            try
            {
                if (!string.IsNullOrEmpty(graph.path))
                    shaderName = graph.path + "/" + shaderName;
                var generator = new Generator(graph, graph.outputNode, GenerationMode.ForReals, shaderName, assetCollection);
                shaderString = generator.generatedShader;
                configuredTextures = generator.configuredTextures;

                if (graph.messageManager.AnyError())
                {
                    shaderString = null;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                configuredTextures = new List<PropertyCollector.TextureInfo>();

                // ignored
            }

            return shaderString ?? k_ErrorShader.Replace("Hidden/GraphErrorShader2", shaderName);
        }
        internal static string GetShaderText(string path, out List<PropertyCollector.TextureInfo> configuredTextures, AssetCollection assetCollection, out GraphData graph)
        {
            var textGraph = File.ReadAllText(path, Encoding.UTF8);
            graph = new GraphData
            {
                messageManager = new MessageManager(), assetGuid = AssetDatabase.AssetPathToGUID(path)
            };
            MultiJson.Deserialize(graph, textGraph);
            graph.OnEnable();
            graph.ValidateGraph();

            return GetShaderText(path, out configuredTextures, assetCollection, graph);
        }

        internal static string GetShaderText(string path, out List<PropertyCollector.TextureInfo> configuredTextures)
        {
            var textGraph = File.ReadAllText(path, Encoding.UTF8);
            GraphData graph = new GraphData
            {
                messageManager = new MessageManager(), assetGuid = AssetDatabase.AssetPathToGUID(path)
            };
            MultiJson.Deserialize(graph, textGraph);
            graph.OnEnable();
            graph.ValidateGraph();

            return GetShaderText(path, out configuredTextures, null, graph);
        }

#if VFX_GRAPH_10_0_0_OR_NEWER
        // TODO: Fix this
        static ShaderGraphVfxAsset GenerateVfxShaderGraphAsset(GraphData graph)
        {
            var target = graph.activeTargets.FirstOrDefault(x => x is VFXTarget) as VFXTarget;
            if(target == null)
                return null;

            var nl = Environment.NewLine;
            var indent = new string(' ', 4);
            var asset = ScriptableObject.CreateInstance<ShaderGraphVfxAsset>();
            var result = asset.compilationResult = new GraphCompilationResult();
            var mode = GenerationMode.ForReals;

            asset.lit = target.lit;
            asset.alphaClipping = target.alphaTest;

            var assetGuid = graph.assetGuid;
            var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            var hlslName = NodeUtils.GetHLSLSafeName(Path.GetFileNameWithoutExtension(assetPath));

            var ports = new List<MaterialSlot>();
            var nodes = new List<AbstractMaterialNode>();

            foreach (var vertexBlock in graph.vertexContext.blocks)
            {
                vertexBlock.value.GetInputSlots(ports);
                NodeUtils.DepthFirstCollectNodesFromNode(nodes, vertexBlock);
            }

            foreach (var fragmentBlock in graph.fragmentContext.blocks)
            {
                fragmentBlock.value.GetInputSlots(ports);
                NodeUtils.DepthFirstCollectNodesFromNode(nodes, fragmentBlock);
            }

            //Remove inactive blocks from generation
            {
                var tmpCtx = new TargetActiveBlockContext(new List<BlockFieldDescriptor>(), null);
                target.GetActiveBlocks(ref tmpCtx);
                ports.RemoveAll(materialSlot =>
                {
                    return !tmpCtx.activeBlocks.Any(o => materialSlot.RawDisplayName() == o.displayName);
                });
            }

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
            bodySb.currentNode = null;

            var portNodeSets = new HashSet<AbstractMaterialNode>[ports.Count];
            for (var portIndex = 0; portIndex < ports.Count; portIndex++)
            {
                var port = ports[portIndex];
                var nodeSet = new HashSet<AbstractMaterialNode>();
                NodeUtils.CollectNodeSet(nodeSet, port);
                portNodeSets[portIndex] = nodeSet;
            }

            var portPropertySets = new HashSet<string>[ports.Count];
            for (var portIndex = 0; portIndex < ports.Count; portIndex++)
            {
                portPropertySets[portIndex] = new HashSet<string>();
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
                        portPropertySets[portIndex].Add(propertyNode.property.objectId);
                    }
                }
            }

            var shaderProperties = new PropertyCollector();
            foreach (var node in nodes)
            {
                node.CollectShaderProperties(shaderProperties, GenerationMode.ForReals);
            }

            asset.SetTextureInfos(shaderProperties.GetConfiguredTextures());

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
                        message.AppendLine($"{node.name} ({node.objectId}): {node.concretePrecision}");
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
                    if (portPropertySet.Contains(property.objectId))
                    {
                        portCodeIndices[portIndex].Add(codeSnippets.Count);
                    }
                }

                ShaderStringBuilder builder = new ShaderStringBuilder();
                property.ForeachHLSLProperty(h => h.AppendTo(builder));

                codeSnippets.Add($"// Property: {property.displayName}{nl}{builder.ToCodeBlock()}{nl}{nl}");
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
                var requirementsNodes = portNodeSets[portIndex].ToList();
                requirementsNodes.Add(ports[portIndex].owner);
                portRequirements[portIndex] = ShaderGraphRequirements.FromNodes(requirementsNodes, ports[portIndex].stageCapability);
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

            // VFX Code heavily relies on the slotId from the original MasterNodes
            // Since we keep these around for upgrades anyway, for now it is simpler to use them
            // Therefore we remap the output blocks back to the original Ids here
            var originialPortIds = new int[ports.Count];
            for (int i = 0; i < originialPortIds.Length; i++)
            {
                if (!VFXTarget.s_BlockMap.TryGetValue((ports[i].owner as BlockNode).descriptor, out var originalId))
                    continue;

                // In Master Nodes we had a different BaseColor/Color slot id between Unlit/Lit
                // In the stack we use BaseColor for both cases. Catch this here.
                if (asset.lit && originalId == ShaderGraphVfxAsset.ColorSlotId)
                {
                    originalId = ShaderGraphVfxAsset.BaseColorSlotId;
                }

                originialPortIds[i] = originalId;
            }

            #region Output Struct

            sharedCodeIndices.Add(codeSnippets.Count);
            codeSnippets.Add($"struct {outputStructName}{nl}{{");

            for (var portIndex = 0; portIndex < ports.Count; portIndex++)
            {
                var port = ports[portIndex];
                portCodeIndices[portIndex].Add(codeSnippets.Count);
                codeSnippets.Add($"{nl}{indent}{port.concreteValueType.ToShaderString(graph.concretePrecision)} {port.shaderOutputName}_{originialPortIds[portIndex]};");
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
                    if (portPropertySet.Contains(property.objectId))
                    {
                        portCodeIndices[portIndex].Add(codeIndex);
                        portPropertyIndices[portIndex].Add(propertyIndex);
                    }
                }

                inputProperties.Add(property);
                codeSnippets.Add($",{nl}{indent}/* Property: {property.displayName} */ {property.GetPropertyAsArgumentStringForVFX()}");
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
                    }
                }
            }

            #endregion

            #region Output Mapping

            sharedCodeIndices.Add(codeSnippets.Count);
            codeSnippets.Add($"{nl}{indent}// VFXMasterNode{nl}{indent}{outputStructName} OUT;{nl}");

            // Output mapping
            for (var portIndex = 0; portIndex < ports.Count; portIndex++)
            {
                var port = ports[portIndex];
                portCodeIndices[portIndex].Add(codeSnippets.Count);
                codeSnippets.Add($"{indent}OUT.{port.shaderOutputName}_{originialPortIds[portIndex]} = {port.owner.GetSlotValue(port.id, GenerationMode.ForReals, graph.concretePrecision)};{nl}");
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

            var outputMetadatas = new OutputMetadata[ports.Count];
            for (int portIndex = 0; portIndex < outputMetadatas.Length; portIndex++)
            {
                outputMetadatas[portIndex] = new OutputMetadata(portIndex, ports[portIndex].shaderOutputName, originialPortIds[portIndex]);
            }

            asset.SetOutputs(outputMetadatas);

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
#endif
    }
}
