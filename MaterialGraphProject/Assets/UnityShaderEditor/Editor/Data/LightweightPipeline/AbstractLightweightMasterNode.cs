using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public abstract class AbstractLightweightMasterNode : MasterNode
    {
        private const int kMaxInterpolators = 8;

        protected abstract IEnumerable<int> masterSurfaceInputs { get; }
        protected abstract IEnumerable<int> masterVertexInputs { get; }
        protected abstract string GetTemplateName();

        protected virtual void GetLightweightDefinesAndRemap(ShaderGenerator defines, ShaderGenerator surfaceOutputRemap, MasterRemapGraph remapper)
        {
            // Step 1: no remapper, working with raw master node..
            if (remapper == null)
            {
                foreach (var slot in GetInputSlots<MaterialSlot>())
                {
                    surfaceOutputRemap.AddShaderChunk(slot.shaderOutputName
                                                      + " = surf."
                                                      + slot.shaderOutputName + ";", true);
                }
            }
            // Step 2: remapper present... complex workflow time
            else
            {
                surfaceOutputRemap.AddShaderChunk("{", false);
                surfaceOutputRemap.Indent();

                foreach (var prop in remapper.properties)
                {
                    surfaceOutputRemap.AddShaderChunk(prop.GetInlinePropertyDeclarationString(), true);
                    surfaceOutputRemap.AddShaderChunk(string.Format("{0} = surf.{0};", prop.referenceName), true);
                }

                List<INode> nodes = new List<INode>();
                NodeUtils.DepthFirstCollectNodesFromNode(nodes, this, NodeUtils.IncludeSelf.Exclude);
                foreach (var activeNode in nodes.OfType<AbstractMaterialNode>())
                {
                    if (activeNode is IGeneratesBodyCode)
                        (activeNode as IGeneratesBodyCode).GenerateNodeCode(surfaceOutputRemap, GenerationMode.ForReals);
                }

                foreach (var input in GetInputSlots<MaterialSlot>())
                {
                    foreach (var edge in owner.GetEdges(input.slotReference))
                    {
                        var outputRef = edge.outputSlot;
                        var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
                        if (fromNode == null)
                            continue;

                        surfaceOutputRemap.AddShaderChunk(
                            string.Format("{0} = {1};", input.shaderOutputName,
                                fromNode.GetVariableNameForSlot(outputRef.slotId)), true);
                    }
                }

                surfaceOutputRemap.Deindent();
                surfaceOutputRemap.AddShaderChunk("}", false);
            }
        }

        struct Pass
        {
            public string Name;
            public List<int> VertexShaderSlots;
            public List<int> PixelShaderSlots;
        }

        public string GetShader(GenerationMode mode, out List<PropertyCollector.TextureInfo> configuredTextures)
        {
            var activeNodeList = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this);

            var shaderProperties = new PropertyCollector();

            var abstractMaterialGraph = owner as AbstractMaterialGraph;
            if (abstractMaterialGraph != null)
                abstractMaterialGraph.CollectShaderProperties(shaderProperties, mode);

            foreach (var activeNode in activeNodeList.OfType<AbstractMaterialNode>())
                activeNode.CollectShaderProperties(shaderProperties, mode);

            var finalShader = new ShaderGenerator();
            finalShader.AddShaderChunk(string.Format(@"Shader ""{0}""", name), false);
            finalShader.AddShaderChunk("{", false);
            finalShader.Indent();

            finalShader.AddShaderChunk("Properties", false);
            finalShader.AddShaderChunk("{", false);
            finalShader.Indent();
            finalShader.AddShaderChunk(shaderProperties.GetPropertiesBlock(2), false);
            finalShader.Deindent();
            finalShader.AddShaderChunk("}", false);

            var lwSub = new LightWeightSubShader();
            foreach (var subshader in lwSub.GetSubshader(this, GenerationMode.ForReals))
                finalShader.AddShaderChunk(subshader, true);

            finalShader.Deindent();
            finalShader.AddShaderChunk("}", false);

            configuredTextures = shaderProperties.GetConfiguredTexutres();
            return finalShader.GetShaderString(0);
        }

        public class LightWeightSubShader
        {
            Pass m_ForwardPass = new Pass()
            {
                Name = "LightweightForward",
                PixelShaderSlots = new List<int>()
                {
                    AbstractLightweightPBRMasterNode.AlbedoSlotId,
                    AbstractLightweightPBRMasterNode.EmissionSlotId,
                    AbstractLightweightPBRMasterNode.NormalSlotId,
                    AbstractLightweightPBRMasterNode.OcclusionSlotId,
                    AbstractLightweightPBRMasterNode.SmoothnessSlotId,
                    AbstractLightweightPBRMasterNode.AlphaSlotId
                }
            };

            public IEnumerable<string> GetSubshader(AbstractLightweightMasterNode masterNode, GenerationMode mode)
            {
                var surfaceVertexShader = new ShaderGenerator();
                var surfaceDescriptionFunction = new ShaderGenerator();
                var surfaceDescriptionStruct = new ShaderGenerator();
                var shaderFunctionVisitor = new ShaderGenerator();
                var surfaceInputs = new ShaderGenerator();

                var shaderProperties = new PropertyCollector();

                var graphVertexInput = @"
struct GraphVertexInput
{
     float4 vertex : POSITION;
     float3 normal : NORMAL;
     float4 tangent : TANGENT;
     float4 color : COLOR;
     float4 texcoord0 : TEXCOORD0;
     float4 lightmapUV : TEXCOORD1;
     UNITY_VERTEX_INPUT_INSTANCE_ID
};";

                surfaceInputs.AddShaderChunk("struct SurfaceInputs{", false);
                surfaceInputs.Indent();

                var activeNodeList = ListPool<INode>.Get();
                NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, masterNode, NodeUtils.IncludeSelf.Include, m_ForwardPass.PixelShaderSlots);

                var requirements = AbstractMaterialGraph.GetRequirements(activeNodeList);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresNormal, InterpolatorType.Normal, surfaceInputs);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresTangent, InterpolatorType.Tangent, surfaceInputs);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresBitangent, InterpolatorType.BiTangent, surfaceInputs);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresViewDir, InterpolatorType.ViewDirection, surfaceInputs);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresPosition, InterpolatorType.Position, surfaceInputs);

                if (requirements.requiresVertexColor)
                    surfaceInputs.AddShaderChunk(string.Format("float4 {0};", ShaderGeneratorNames.VertexColor), false);

                if (requirements.requiresScreenPosition)
                    surfaceInputs.AddShaderChunk(string.Format("float4 {0};", ShaderGeneratorNames.ScreenPosition), false);

                foreach (var channel in requirements.requiresMeshUVs.Distinct())
                    surfaceInputs.AddShaderChunk(string.Format("half4 {0};", channel.GetUVName()), false);

                surfaceInputs.Deindent();
                surfaceInputs.AddShaderChunk("};", false);

                surfaceVertexShader.AddShaderChunk("GraphVertexInput PopulateVertexData(GraphVertexInput v){", false);
                surfaceVertexShader.Indent();
                surfaceVertexShader.AddShaderChunk("return v;", false);
                surfaceVertexShader.Deindent();
                surfaceVertexShader.AddShaderChunk("}", false);

                var slots = new List<MaterialSlot>();
                foreach (var id in m_ForwardPass.PixelShaderSlots)
                    slots.Add(masterNode.FindSlot<MaterialSlot>(id));
                AbstractMaterialGraph.GenerateSurfaceDescriptionStruct(surfaceDescriptionStruct, slots, true);

                AbstractMaterialGraph.GenerateSurfaceDescription(
                    masterNode,
                    m_ForwardPass.PixelShaderSlots,
                    masterNode.owner as AbstractMaterialGraph,
                    surfaceDescriptionFunction,
                    shaderFunctionVisitor,
                    shaderProperties,
                    requirements,
                    mode);

                var graph = new ShaderGenerator();
                graph.AddShaderChunk(shaderFunctionVisitor.GetShaderString(2), false);
                graph.AddShaderChunk(graphVertexInput, false);
                graph.AddShaderChunk(surfaceInputs.GetShaderString(2), false);
                graph.AddShaderChunk(surfaceDescriptionStruct.GetShaderString(2), false);
                graph.AddShaderChunk(shaderProperties.GetPropertiesDeclaration(2), false);
                graph.AddShaderChunk(surfaceVertexShader.GetShaderString(2), false);
                graph.AddShaderChunk(surfaceDescriptionFunction.GetShaderString(2), false);

                var tagsVisitor = new ShaderGenerator();
                var blendingVisitor = new ShaderGenerator();
                var cullingVisitor = new ShaderGenerator();
                var zTestVisitor = new ShaderGenerator();
                var zWriteVisitor = new ShaderGenerator();

                var materialOptions = new SurfaceMaterialOptions();
                materialOptions.GetTags(tagsVisitor);
                materialOptions.GetBlend(blendingVisitor);
                materialOptions.GetCull(cullingVisitor);
                materialOptions.GetDepthTest(zTestVisitor);
                materialOptions.GetDepthWrite(zWriteVisitor);

                var interpolators = new ShaderGenerator();
                var localVertexShader = new ShaderGenerator();
                var localPixelShader = new ShaderGenerator();
                var localSurfaceInputs = new ShaderGenerator();
                var surfaceOutputRemap = new ShaderGenerator();

                var reqs = ShaderGraphRequirements.none;
                reqs.requiresNormal |= NeededCoordinateSpace.World;
                reqs.requiresTangent |= NeededCoordinateSpace.World;
                reqs.requiresBitangent |= NeededCoordinateSpace.World;
                reqs.requiresPosition |= NeededCoordinateSpace.World;
                reqs.requiresViewDir |= NeededCoordinateSpace.World;

                ShaderGenerator.GenerateStandardTransforms(
                    3,
                    10,
                    interpolators,
                    localVertexShader,
                    localPixelShader,
                    localSurfaceInputs,
                    requirements,
                    reqs,
                    CoordinateSpace.World);

                ShaderGenerator defines = new ShaderGenerator();
                var templateLocation = ShaderGenerator.GetTemplatePath("lightweightSubshaderPBR.template");

                var usedSlots = new List<MaterialSlot>();
                foreach (var id in m_ForwardPass.PixelShaderSlots)
                    usedSlots.Add(masterNode.FindSlot<MaterialSlot>(id));

                foreach (var slot in usedSlots)
                {
                    surfaceOutputRemap.AddShaderChunk(slot.shaderOutputName
                        + " = surf."
                        + slot.shaderOutputName + ";", true);
                }

                if (!File.Exists(templateLocation))
                    return new string[] { };

                var subShaderTemplate = File.ReadAllText(templateLocation);
                var resultShader = subShaderTemplate.Replace("${Defines}", defines.GetShaderString(3));
                resultShader = resultShader.Replace("${Graph}", graph.GetShaderString(3));
                resultShader = resultShader.Replace("${Interpolators}", interpolators.GetShaderString(3));
                resultShader = resultShader.Replace("${VertexShader}", localVertexShader.GetShaderString(3));
                resultShader = resultShader.Replace("${LocalPixelShader}", localPixelShader.GetShaderString(3));
                resultShader = resultShader.Replace("${SurfaceInputs}", localSurfaceInputs.GetShaderString(3));
                resultShader = resultShader.Replace("${SurfaceOutputRemap}", surfaceOutputRemap.GetShaderString(3));


                resultShader = resultShader.Replace("${Tags}", tagsVisitor.GetShaderString(2));
                resultShader = resultShader.Replace("${Blending}", blendingVisitor.GetShaderString(2));
                resultShader = resultShader.Replace("${Culling}", cullingVisitor.GetShaderString(2));
                resultShader = resultShader.Replace("${ZTest}", zTestVisitor.GetShaderString(2));
                resultShader = resultShader.Replace("${ZWrite}", zWriteVisitor.GetShaderString(2));
                resultShader = resultShader.Replace("${LOD}", "" + materialOptions.lod);
                return new[] { resultShader };
            }
        }
        
        public override IEnumerable<string> GetSubshader(ShaderGraphRequirements graphRequirements, MasterRemapGraph remapper)
        {
            var tagsVisitor = new ShaderGenerator();
            var blendingVisitor = new ShaderGenerator();
            var cullingVisitor = new ShaderGenerator();
            var zTestVisitor = new ShaderGenerator();
            var zWriteVisitor = new ShaderGenerator();

            m_MaterialOptions.GetTags(tagsVisitor);
            m_MaterialOptions.GetBlend(blendingVisitor);
            m_MaterialOptions.GetCull(cullingVisitor);
            m_MaterialOptions.GetDepthTest(zTestVisitor);
            m_MaterialOptions.GetDepthWrite(zWriteVisitor);

            var interpolators = new ShaderGenerator();
            var vertexShader = new ShaderGenerator();
            var localPixelShader = new ShaderGenerator();
            var surfaceInputs = new ShaderGenerator();

            ShaderGenerator.GenerateStandardTransforms(
                GetInterpolatorStartIndex(),
                10,
                interpolators,
                vertexShader,
                localPixelShader,
                surfaceInputs,
                graphRequirements,
                GetNodeSpecificRequirements(),
                CoordinateSpace.World);

            ShaderGenerator defines = new ShaderGenerator();
            ShaderGenerator surfaceOutputRemap = new ShaderGenerator();
            GetLightweightDefinesAndRemap(defines, surfaceOutputRemap, remapper);

            var templateLocation = ShaderGenerator.GetTemplatePath(GetTemplateName());

            if (!File.Exists(templateLocation))
                return new string[] {};

            var subShaderTemplate = File.ReadAllText(templateLocation);
            var resultShader = subShaderTemplate.Replace("${Defines}", defines.GetShaderString(3));
            resultShader = resultShader.Replace("${Interpolators}", interpolators.GetShaderString(3));
            resultShader = resultShader.Replace("${VertexShader}", vertexShader.GetShaderString(3));
            resultShader = resultShader.Replace("${LocalPixelShader}", localPixelShader.GetShaderString(3));
            resultShader = resultShader.Replace("${SurfaceInputs}", surfaceInputs.GetShaderString(3));
            resultShader = resultShader.Replace("${SurfaceOutputRemap}", surfaceOutputRemap.GetShaderString(3));

            resultShader = resultShader.Replace("${Tags}", tagsVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${Blending}", blendingVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${Culling}", cullingVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ZTest}", zTestVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ZWrite}", zWriteVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${LOD}", "" + m_MaterialOptions.lod);
            return new[] {resultShader};
        }

        protected abstract int GetInterpolatorStartIndex();
    }
}
