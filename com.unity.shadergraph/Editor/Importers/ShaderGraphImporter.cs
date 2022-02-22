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
    [ScriptedImporter(127, Extension, -902)]
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

            var importLog = new AssetImportErrorLog(ctx);

            List<PropertyCollector.TextureInfo> configuredTextures;
            string path = ctx.assetPath;

            AssetCollection assetCollection = new AssetCollection();
            MinimalGraphData.GatherMinimalDependenciesFromFile(assetPath, assetCollection);

            var textGraph = File.ReadAllText(path, Encoding.UTF8);
            var graph = new GraphData
            {
                messageManager = new MessageManager(),
                assetGuid = AssetDatabase.AssetPathToGUID(path)
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

                ReportErrors(graph, shader, path, importLog);

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

            if (shader != null)
            {
                Material material = new Material(shader) { name = "Material/" + Path.GetFileNameWithoutExtension(path) };
                ctx.AddObjectToAsset("Material", material);
            }

            Texture2D texture = Resources.Load<Texture2D>("Icons/sg_graph_icon");
            ctx.AddObjectToAsset("MainAsset", mainObject, texture);
            ctx.SetMainObject(mainObject);

            var graphDataReadOnly = new GraphDataReadOnly(graph);
            foreach (var target in graph.activeTargets)
            {
                if (target is IHasMetadata iHasMetadata)
                {
                    var metadata = iHasMetadata.GetMetadataObject(graphDataReadOnly);
                    if (metadata == null)
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

            List<GraphInputData> inputInspectorDataList = new List<GraphInputData>();
            foreach (AbstractShaderProperty property in graph.properties)
            {
                // Don't write out data for non-exposed blackboard items
                if (!property.isExposed)
                    continue;

                // VTs are treated differently
                if (property is VirtualTextureShaderProperty virtualTextureShaderProperty)
                    inputInspectorDataList.Add(MinimalCategoryData.ProcessVirtualTextureProperty(virtualTextureShaderProperty));
                else
                    inputInspectorDataList.Add(new GraphInputData() { referenceName = property.referenceName, propertyType = property.propertyType, isKeyword = false });
            }
            foreach (ShaderKeyword keyword in graph.keywords)
            {
                // Don't write out data for non-exposed blackboard items
                if (!keyword.isExposed)
                    continue;

                var sanitizedReferenceName = keyword.referenceName;
                if (keyword.keywordType == KeywordType.Boolean && keyword.referenceName.Contains("_ON"))
                    sanitizedReferenceName = sanitizedReferenceName.Replace("_ON", String.Empty);

                inputInspectorDataList.Add(new GraphInputData() { referenceName = sanitizedReferenceName, keywordType = keyword.keywordType, isKeyword = true });
            }

            sgMetadata.categoryDatas = new List<MinimalCategoryData>();
            foreach (CategoryData categoryData in graph.categories)
            {
                // Don't write out empty categories
                if (categoryData.childCount == 0)
                    continue;

                MinimalCategoryData mcd = new MinimalCategoryData()
                {
                    categoryName = categoryData.name,
                    propertyDatas = new List<GraphInputData>()
                };
                foreach (var input in categoryData.Children)
                {
                    GraphInputData propData;
                    // Only write out data for exposed blackboard items
                    if (input.isExposed == false)
                        continue;

                    // VTs are treated differently
                    if (input is VirtualTextureShaderProperty virtualTextureShaderProperty)
                    {
                        propData = MinimalCategoryData.ProcessVirtualTextureProperty(virtualTextureShaderProperty);
                        inputInspectorDataList.RemoveAll(inputData => inputData.referenceName == propData.referenceName);
                        mcd.propertyDatas.Add(propData);
                        continue;
                    }
                    else if (input is ShaderKeyword keyword)
                    {
                        var sanitizedReferenceName = keyword.referenceName;
                        if (keyword.keywordType == KeywordType.Boolean && keyword.referenceName.Contains("_ON"))
                            sanitizedReferenceName = sanitizedReferenceName.Replace("_ON", String.Empty);

                        propData = new GraphInputData() { referenceName = sanitizedReferenceName, keywordType = keyword.keywordType, isKeyword = true };
                    }
                    else
                    {
                        var prop = input as AbstractShaderProperty;
                        propData = new GraphInputData() { referenceName = input.referenceName, propertyType = prop.propertyType, isKeyword = false };
                    }

                    mcd.propertyDatas.Add(propData);
                    inputInspectorDataList.Remove(propData);
                }
                sgMetadata.categoryDatas.Add(mcd);
            }

            // Any uncategorized elements get tossed into an un-named category at the top as a fallback
            if (inputInspectorDataList.Count > 0)
            {
                sgMetadata.categoryDatas.Insert(0, new MinimalCategoryData() { categoryName = "", propertyDatas = inputInspectorDataList });
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
                        importLog.LogWarning($"Invalid dependency path: {assetPath}", mainObject);
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

        internal class AssetImportErrorLog : MessageManager.IErrorLog
        {
            AssetImportContext ctx;
            public AssetImportErrorLog(AssetImportContext ctx)
            {
                this.ctx = ctx;
            }

            public void LogError(string message, UnityEngine.Object context = null)
            {
                // Note: if you get sent here by clicking on a ShaderGraph error message,
                // this is a bug in the scripted importer system, not being able to link import error messages to the imported asset
                ctx.LogImportError(message, context);
            }

            public void LogWarning(string message, UnityEngine.Object context = null)
            {
                ctx.LogImportWarning(message, context);
            }
        }

        static bool NodeWasUsedByGraph(string nodeId, GraphData graphData)
        {
            var node = graphData.GetNodeFromId(nodeId);
            return node?.wasUsedByGenerator ?? false;
        }

        // error messages should be reported through the asset import context, so that object references are translated properly (in the future), and the error is associated with the import
        static void ReportErrors(GraphData graph, Shader shader, string path, AssetImportErrorLog importLog)
        {
            // Grab any messages from the shader compiler
            var messages = ShaderUtil.GetShaderMessages(shader);

            var errors = graph.messageManager.ErrorStrings((nodeId) => NodeWasUsedByGraph(nodeId, graph));
            int errCount = errors.Count();

            // Find the first compiler message that's an error
            int firstShaderUtilErrorIndex = -1;
            if (messages != null)
                firstShaderUtilErrorIndex = Array.FindIndex(messages, m => (m.severity == Rendering.ShaderCompilerMessageSeverity.Error));

            // Display only one message. Bias towards shader compiler messages over node messages and within that bias errors over warnings.
            if (firstShaderUtilErrorIndex != -1)
            {
                // if shader compiler reported an error, show that
                MessageManager.Log(path, messages[firstShaderUtilErrorIndex], shader, importLog);
            }
            else if (errCount > 0)
            {
                // otherwise show node errors
                var firstError = errors.FirstOrDefault();
                importLog.LogError($"Shader Graph at {path} has {errCount} error(s), the first is: {firstError}", shader);
            }
            else if (messages.Length != 0)
            {
                // otherwise show shader compiler warnings
                MessageManager.Log(path, messages[0], shader, importLog);
            }
            else if (graph.messageManager.nodeMessagesChanged)
            {
                // otherwise show node warnings
                var warnings = graph.messageManager.ErrorStrings((nodeId) => NodeWasUsedByGraph(nodeId, graph), Rendering.ShaderCompilerMessageSeverity.Warning);
                var warnCount = warnings.Count();
                var firstWarning = warnings.FirstOrDefault();
                if (warnCount > 0)
                    importLog.LogWarning($"Shader Graph at {path} has {warnCount} warning(s), the first is: {firstWarning}", shader);
            }
        }

        internal static string GetShaderText(string path, out List<PropertyCollector.TextureInfo> configuredTextures, AssetCollection assetCollection, GraphData graph, GenerationMode mode = GenerationMode.ForReals, Target[] targets = null)
        {
            string shaderString = null;
            var shaderName = Path.GetFileNameWithoutExtension(path);
            try
            {
                if (!string.IsNullOrEmpty(graph.path))
                    shaderName = graph.path + "/" + shaderName;

                Generator generator;
                if (targets != null)
                    generator = new Generator(graph, graph.outputNode, mode, shaderName, assetCollection, targets);
                else
                    generator = new Generator(graph, graph.outputNode, mode, shaderName, assetCollection);

                shaderString = generator.generatedShader;
                configuredTextures = generator.configuredTextures;

                // we only care if an error was reported for a node that we actually used
                if (graph.messageManager.AnyError((nodeId) => NodeWasUsedByGraph(nodeId, graph)))
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

            if (shaderString == null)
            {
                shaderString = k_ErrorShader.Replace("Hidden/GraphErrorShader2", shaderName);
            }

            return shaderString;
        }

        internal static string GetShaderText(string path, out List<PropertyCollector.TextureInfo> configuredTextures, AssetCollection assetCollection, out GraphData graph)
        {
            var textGraph = File.ReadAllText(path, Encoding.UTF8);
            graph = new GraphData
            {
                messageManager = new MessageManager(),
                assetGuid = AssetDatabase.AssetPathToGUID(path)
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
                messageManager = new MessageManager(),
                assetGuid = AssetDatabase.AssetPathToGUID(path)
            };
            MultiJson.Deserialize(graph, textGraph);
            graph.OnEnable();
            graph.ValidateGraph();

            return GetShaderText(path, out configuredTextures, null, graph);
        }

#if VFX_GRAPH_10_0_0_OR_NEWER
        // TODO: Fix this - VFX Graph can now use ShaderGraph as a code generation path. However, currently, the new
        // generation path still slightly depends on this container (The implementation of it was tightly coupled in VFXShaderGraphParticleOutput,
        // and we keep it now as there is no migration path for users yet). This will need to be decoupled so that we can eventually
        // remove this container.
        static ShaderGraphVfxAsset GenerateVfxShaderGraphAsset(GraphData graph)
        {
            var target = graph.activeTargets.FirstOrDefault(x => x.SupportsVFX());

            if (target == null)
                return null;

            var nl = Environment.NewLine;
            var indent = new string(' ', 4);
            var asset = ScriptableObject.CreateInstance<ShaderGraphVfxAsset>();
            var result = asset.compilationResult = new GraphCompilationResult();
            var mode = GenerationMode.ForReals;

            if (target is VFXTarget vfxTarget)
            {
                asset.lit = vfxTarget.lit;
                asset.alphaClipping = vfxTarget.alphaTest;
                asset.generatesWithShaderGraph = false;
            }
            else
            {
                asset.lit = true;
                asset.alphaClipping = false;
                asset.generatesWithShaderGraph = true;
            }

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

            //Remove inactive blocks from legacy generation
            if (!asset.generatesWithShaderGraph)
            {
                var tmpCtx = new TargetActiveBlockContext(new List<BlockFieldDescriptor>(), null);

                // NOTE: For whatever reason, this call fails for custom interpolator ports (ie, active ones are not detected as active).
                // For the sake of compatibility with custom interpolator with shadergraph generation, skip the removal of inactive blocks.
                target.GetActiveBlocks(ref tmpCtx);

                ports.RemoveAll(materialSlot =>
                {
                    return !tmpCtx.activeBlocks.Any(o => materialSlot.RawDisplayName() == o.displayName);
                });
            }

            var bodySb = new ShaderStringBuilder(1);
            var graphIncludes = new IncludeCollection();
            var registry = new FunctionRegistry(new ShaderStringBuilder(), graphIncludes, true);

            foreach (var properties in graph.properties)
            {
                properties.SetupConcretePrecision(graph.graphDefaultConcretePrecision);
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

            foreach (var include in graphIncludes)
            {
                sharedCodeIndices.Add(codeSnippets.Count);
                codeSnippets.Add(include.value + nl);
            }

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
                if (property.isExposed)
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

            foreach (var prop in shaderProperties.properties)
            {
                if (!graph.properties.Contains(prop) && (prop is SamplerStateShaderProperty))
                {
                    sharedCodeIndices.Add(codeSnippets.Count);
                    ShaderStringBuilder builder = new ShaderStringBuilder();
                    prop.ForeachHLSLProperty(h => h.AppendTo(builder));

                    codeSnippets.Add($"// Property: {prop.displayName}{nl}{builder.ToCodeBlock()}{nl}{nl}");
                }
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
            AddCoordinateSpaceSnippets(InterpolatorType.PositionPredisplacement, r => r.requiresPositionPredisplacement);

            AddRequirementsSnippet(r => r.requiresVertexColor, $"float4 {ShaderGeneratorNames.VertexColor}");
            AddRequirementsSnippet(r => r.requiresScreenPosition, $"float4 {ShaderGeneratorNames.ScreenPosition}");
            AddRequirementsSnippet(r => r.requiresNDCPosition, $"float2 {ShaderGeneratorNames.NDCPosition}");
            AddRequirementsSnippet(r => r.requiresPixelPosition, $"float2 {ShaderGeneratorNames.PixelPosition}");
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
                codeSnippets.Add($"{nl}{indent}{port.concreteValueType.ToShaderString(graph.graphDefaultConcretePrecision)} {port.shaderOutputName}_{originialPortIds[portIndex]};");
            }

            sharedCodeIndices.Add(codeSnippets.Count);
            codeSnippets.Add($"{nl}}};{nl}{nl}");

        #endregion

        #region Graph Function

            sharedCodeIndices.Add(codeSnippets.Count);
            codeSnippets.Add($"{outputStructName} {evaluationFunctionName}({nl}{indent}{inputStructName} IN");

            var inputProperties = new List<AbstractShaderProperty>();
            var portPropertyIndices = new List<int>[ports.Count];
            var propertiesStages = new List<ShaderStageCapability>();
            for (var portIndex = 0; portIndex < ports.Count; portIndex++)
            {
                portPropertyIndices[portIndex] = new List<int>();
            }

            // Fetch properties from the categories to keep the same order as in the shader graph blackboard
            // Union with the flat properties collection because previous shader graph version could store properties without category
            var sortedProperties = graph.categories
                .SelectMany(x => x.Children)
                .OfType<AbstractShaderProperty>()
                .Union(graph.properties)
                .Where(x => x.isExposed);

            foreach (var property in sortedProperties)
            {
                var propertyIndex = inputProperties.Count;
                var codeIndex = codeSnippets.Count;

                ShaderStageCapability stageCapability = 0;
                for (var portIndex = 0; portIndex < ports.Count; portIndex++)
                {
                    var portPropertySet = portPropertySets[portIndex];
                    if (portPropertySet.Contains(property.objectId))
                    {
                        portCodeIndices[portIndex].Add(codeIndex);
                        portPropertyIndices[portIndex].Add(propertyIndex);
                        stageCapability |= ports[portIndex].stageCapability;
                    }
                }

                propertiesStages.Add(stageCapability);
                inputProperties.Add(property);
                codeSnippets.Add($",{nl}{indent}/* Property: {property.displayName} */ {property.GetPropertyAsArgumentStringForVFX(property.concretePrecision.ToShaderString())}");
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
                codeSnippets.Add($"{indent}OUT.{port.shaderOutputName}_{originialPortIds[portIndex]} = {port.owner.GetSlotValue(port.id, GenerationMode.ForReals, graph.graphDefaultConcretePrecision)};{nl}");
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
            asset.m_PropertiesStages = propertiesStages.ToArray();
            asset.concretePrecision = graph.graphDefaultConcretePrecision;
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
