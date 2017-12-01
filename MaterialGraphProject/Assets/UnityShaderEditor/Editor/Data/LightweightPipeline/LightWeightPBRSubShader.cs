using System;
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

        private static string GetShaderPassFromTemplate(string template, PBRMasterNode masterNode, Pass pass, GenerationMode mode, SurfaceMaterialOptions materialOptions)
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

            var usedSlots = new List<MaterialSlot>();
            foreach (var id in pass.PixelShaderSlots)
                usedSlots.Add(masterNode.FindSlot<MaterialSlot>(id));

            AbstractMaterialGraph.GenerateSurfaceDescription(
                activeNodeList,
                masterNode,
                masterNode.owner as AbstractMaterialGraph,
                surfaceDescriptionFunction,
                shaderFunctionVisitor,
                shaderProperties,
                requirements,
                mode,
                "PopulateSurfaceData",
                "SurfaceDescription",
                null,
                null,
                usedSlots);

            var graph = new ShaderGenerator();
            graph.AddShaderChunk(shaderFunctionVisitor.GetShaderString(2), false);
            graph.AddShaderChunk(graphVertexInput, false);
            graph.AddShaderChunk(surfaceInputs.GetShaderString(2), false);
            graph.AddShaderChunk(surfaceDescriptionStruct.GetShaderString(2), false);
            graph.AddShaderChunk(shaderProperties.GetPropertiesDeclaration(2), false);
            graph.AddShaderChunk(surfaceVertexShader.GetShaderString(2), false);
            graph.AddShaderChunk(surfaceDescriptionFunction.GetShaderString(2), false);

            var blendingVisitor = new ShaderGenerator();
            var cullingVisitor = new ShaderGenerator();
            var zTestVisitor = new ShaderGenerator();
            var zWriteVisitor = new ShaderGenerator();


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

            if (masterNode.IsSlotConnected(PBRMasterNode.NormalSlotId))
                defines.AddShaderChunk("#define _NORMALMAP 1", true);

            if (masterNode.model == PBRMasterNode.Model.Specular)
                defines.AddShaderChunk("#define _SPECULAR_SETUP 1", true);

            switch (masterNode.alphaMode)
            {
                case PBRMasterNode.AlphaMode.AlphaBlend:
                case PBRMasterNode.AlphaMode.AdditiveBlend:
                    defines.AddShaderChunk("#define _AlphaOut 1", true);
                    break;
                case PBRMasterNode.AlphaMode.Clip:
                    defines.AddShaderChunk("#define _AlphaClip 1", true);
                    break;
            }

            var templateLocation = ShaderGenerator.GetTemplatePath(template);

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


            resultPass = resultPass.Replace("${Tags}", string.Empty);
            resultPass = resultPass.Replace("${Blending}", blendingVisitor.GetShaderString(2));
            resultPass = resultPass.Replace("${Culling}", cullingVisitor.GetShaderString(2));
            resultPass = resultPass.Replace("${ZTest}", zTestVisitor.GetShaderString(2));
            resultPass = resultPass.Replace("${ZWrite}", zWriteVisitor.GetShaderString(2));
            return resultPass;
        }

        public IEnumerable<string> GetSubshader(PBRMasterNode masterNode, GenerationMode mode)
        {
            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            subShader.AddShaderChunk("Tags{ \"RenderPipeline\" = \"LightweightPipeline\"}", true);

            var materialOptions = new SurfaceMaterialOptions();
            switch (masterNode.alphaMode)
            {
                case PBRMasterNode.AlphaMode.Overwrite:
                case PBRMasterNode.AlphaMode.Clip:
                    materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.One;
                    materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.Zero;
                    materialOptions.cullMode = SurfaceMaterialOptions.CullMode.Back;
                    materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                    materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.On;
                    materialOptions.renderQueue = SurfaceMaterialOptions.RenderQueue.Geometry;
                    materialOptions.renderType = SurfaceMaterialOptions.RenderType.Opaque;
                    break;
                case PBRMasterNode.AlphaMode.AlphaBlend:
                    materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.SrcAlpha;
                    materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.OneMinusSrcAlpha;
                    materialOptions.cullMode = SurfaceMaterialOptions.CullMode.Back;
                    materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                    materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.Off;
                    materialOptions.renderQueue = SurfaceMaterialOptions.RenderQueue.Transparent;
                    materialOptions.renderType = SurfaceMaterialOptions.RenderType.Transparent;
                    break;
                case PBRMasterNode.AlphaMode.AdditiveBlend:
                    materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.One;
                    materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.One;
                    materialOptions.cullMode = SurfaceMaterialOptions.CullMode.Back;
                    materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                    materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.Off;
                    materialOptions.renderQueue = SurfaceMaterialOptions.RenderQueue.Transparent;
                    materialOptions.renderType = SurfaceMaterialOptions.RenderType.Transparent;
                    break;
            }

            var tagsVisitor = new ShaderGenerator();
            materialOptions.GetTags(tagsVisitor);
            subShader.AddShaderChunk(tagsVisitor.GetShaderString(0), true);

            subShader.AddShaderChunk(
                GetShaderPassFromTemplate(
                    "lightweightPBRForwardPass.template",
                    masterNode,
                    masterNode.model == PBRMasterNode.Model.Metallic ? m_ForwardPassMetallic : m_ForwardPassSpecular,
                    mode,
                    materialOptions),
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
