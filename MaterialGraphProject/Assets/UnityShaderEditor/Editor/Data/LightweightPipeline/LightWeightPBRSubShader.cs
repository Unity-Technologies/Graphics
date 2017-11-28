using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public class LightWeightPBRSubShader
    {
        Pass m_ForwardPassMetallic = new Pass()
        {
            Name = "LightweightForward",
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId
            }
        };

        struct Pass
        {
            public string Name;
            public List<int> VertexShaderSlots;
            public List<int> PixelShaderSlots;
        }

        Pass m_ForwardPassSpecular = new Pass()
        {
            Name = "LightweightForward",
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId
            }
        };
        
        private static string GetShaderPassFromTemplate(string template, PBRMasterNode masterNode, Pass pass, GenerationMode mode)
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
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, masterNode, NodeUtils.IncludeSelf.Include, pass.PixelShaderSlots);

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
            foreach (var id in pass.PixelShaderSlots)
                slots.Add(masterNode.FindSlot<MaterialSlot>(id));
            AbstractMaterialGraph.GenerateSurfaceDescriptionStruct(surfaceDescriptionStruct, slots, true);

            AbstractMaterialGraph.GenerateSurfaceDescription(
                masterNode,
                pass.PixelShaderSlots,
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
            var templateLocation = ShaderGenerator.GetTemplatePath(template);

            var usedSlots = new List<MaterialSlot>();
            foreach (var id in pass.PixelShaderSlots)
                usedSlots.Add(masterNode.FindSlot<MaterialSlot>(id));

            foreach (var slot in usedSlots)
            {
                surfaceOutputRemap.AddShaderChunk(slot.shaderOutputName
                    + " = surf."
                    + slot.shaderOutputName + ";", true);
            }

            if (!File.Exists(templateLocation))
                return string.Empty;

            var subShaderTemplate = File.ReadAllText(templateLocation);
            var resultPass = subShaderTemplate.Replace("${Defines}", defines.GetShaderString(3));
            resultPass = resultPass.Replace("${Graph}", graph.GetShaderString(3));
            resultPass = resultPass.Replace("${Interpolators}", interpolators.GetShaderString(3));
            resultPass = resultPass.Replace("${VertexShader}", localVertexShader.GetShaderString(3));
            resultPass = resultPass.Replace("${LocalPixelShader}", localPixelShader.GetShaderString(3));
            resultPass = resultPass.Replace("${SurfaceInputs}", localSurfaceInputs.GetShaderString(3));
            resultPass = resultPass.Replace("${SurfaceOutputRemap}", surfaceOutputRemap.GetShaderString(3));


            resultPass = resultPass.Replace("${Tags}", tagsVisitor.GetShaderString(2));
            resultPass = resultPass.Replace("${Blending}", blendingVisitor.GetShaderString(2));
            resultPass = resultPass.Replace("${Culling}", cullingVisitor.GetShaderString(2));
            resultPass = resultPass.Replace("${ZTest}", zTestVisitor.GetShaderString(2));
            resultPass = resultPass.Replace("${ZWrite}", zWriteVisitor.GetShaderString(2));
            resultPass = resultPass.Replace("${LOD}", "" + materialOptions.lod);
            return resultPass;
        }

        public IEnumerable<string> GetSubshader(PBRMasterNode masterNode, GenerationMode mode)
        {
            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            subShader.AddShaderChunk("Tags{ \"RenderType\" = \"Opaque\" \"RenderPipeline\" = \"LightweightPipeline\"}", true);

            subShader.AddShaderChunk(
                GetShaderPassFromTemplate(
                    "lightweightPBRForwardPass.template",
                    masterNode,
                    masterNode.model == PBRMasterNode.Model.Metallic ? m_ForwardPassMetallic : m_ForwardPassSpecular,
                    mode),
                true);

            var extraPassesTemplateLocation = ShaderGenerator.GetTemplatePath("lightweightPBRExtraPasses.template");
            if (File.Exists(extraPassesTemplateLocation))
                subShader.AddShaderChunk(File.ReadAllText(extraPassesTemplateLocation), true);
            
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            return new[] { subShader.GetShaderString(0) };
        }
    }
}