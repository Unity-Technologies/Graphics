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
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
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
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            }
        };

        private static void GenerateApplicationVertexInputs(ShaderGraphRequirements graphRequiements, ShaderGenerator vertexInputs)
        {
            vertexInputs.AddShaderChunk("struct GraphVertexInput", false);
            vertexInputs.AddShaderChunk("{", false);
            vertexInputs.Indent();
            vertexInputs.AddShaderChunk("float4 vertex : POSITION;", false);
            vertexInputs.AddShaderChunk("float3 normal : NORMAL;", false);
            vertexInputs.AddShaderChunk("float4 tangent : TANGENT;", false);

            if (graphRequiements.requiresVertexColor)
            {
                vertexInputs.AddShaderChunk("float4 color : COLOR;", false);
            }

            foreach (var channel in graphRequiements.requiresMeshUVs.Distinct())
                vertexInputs.AddShaderChunk(string.Format("float4 texcoord{0} : TEXCOORD{0};", (int)channel), false);
            
            vertexInputs.Deindent();
            vertexInputs.AddShaderChunk("};", false);
        }

        private static string GetShaderPassFromTemplate(string template, PBRMasterNode masterNode, Pass pass, GenerationMode mode, SurfaceMaterialOptions materialOptions)
        {
            var builder = new ShaderStringBuilder();
            builder.IncreaseIndent();
            builder.IncreaseIndent();
            var vertexInputs = new ShaderGenerator();
            var surfaceVertexShader = new ShaderGenerator();
            var surfaceDescriptionFunction = new ShaderGenerator();
            var surfaceDescriptionStruct = new ShaderGenerator();
            var functionRegistry = new FunctionRegistry(builder);
            var surfaceInputs = new ShaderGenerator();

            var shaderProperties = new PropertyCollector();

            surfaceInputs.AddShaderChunk("struct SurfaceInputs{", false);
            surfaceInputs.Indent();

            var activeNodeList = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, masterNode, NodeUtils.IncludeSelf.Include, pass.PixelShaderSlots);

            var requirements = ShaderGraphRequirements.FromNodes(activeNodeList);

            var modelRequiements = ShaderGraphRequirements.none;
            modelRequiements.requiresNormal |= NeededCoordinateSpace.World;
            modelRequiements.requiresTangent |= NeededCoordinateSpace.World;
            modelRequiements.requiresBitangent |= NeededCoordinateSpace.World;
            modelRequiements.requiresPosition |= NeededCoordinateSpace.World;
            modelRequiements.requiresViewDir |= NeededCoordinateSpace.World;
            modelRequiements.requiresMeshUVs.Add(UVChannel.uv1);

            GenerateApplicationVertexInputs(requirements.Union(modelRequiements), vertexInputs);
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
            GraphUtil.GenerateSurfaceDescriptionStruct(surfaceDescriptionStruct, slots, true);

            var usedSlots = new List<MaterialSlot>();
            foreach (var id in pass.PixelShaderSlots)
                usedSlots.Add(masterNode.FindSlot<MaterialSlot>(id));

            GraphUtil.GenerateSurfaceDescription(
                activeNodeList,
                masterNode,
                masterNode.owner as AbstractMaterialGraph,
                surfaceDescriptionFunction,
                functionRegistry,
                shaderProperties,
                requirements,
                mode,
                "PopulateSurfaceData",
                "SurfaceDescription",
                null,
                usedSlots);

            var graph = new ShaderGenerator();
            graph.AddShaderChunk(builder.ToString(), false);
            graph.AddShaderChunk(vertexInputs.GetShaderString(2), false);
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

            ShaderGenerator.GenerateStandardTransforms(
                3,
                10,
                interpolators,
                localVertexShader,
                localPixelShader,
                localSurfaceInputs,
                requirements,
                modelRequiements,
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
            }

            if (masterNode.IsSlotConnected(PBRMasterNode.AlphaThresholdSlotId))
                    defines.AddShaderChunk("#define _AlphaClip 1", true);

            var templateLocation = ShaderGenerator.GetTemplatePath(template);

            foreach (var slot in usedSlots)
            {
                surfaceOutputRemap.AddShaderChunk(string.Format("{0} = surf.{0};", slot.shaderOutputName), true);
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
                case PBRMasterNode.AlphaMode.Opaque:
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
