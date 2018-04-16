using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class HDUnlitSubShader : IUnlitSubShader
    {
        struct Pass
        {
            public string Name;
            public string ShaderPassName;
            public string ShaderPassInclude;
            public List<int> VertexShaderSlots;
            public List<int> PixelShaderSlots;
        }

        Pass m_UnlitPassForwardOnly = new Pass()
        {
            Name = "ForwardOnly",
            ShaderPassName = "SHADERPASS_FORWARD_UNLIT",
            ShaderPassInclude = "ShaderPassForwardUnlit",
            PixelShaderSlots = new List<int>()
            {
                UnlitMasterNode.ColorSlotId,
                UnlitMasterNode.AlphaSlotId
            }
        };

        Pass m_UnlitPassForwardDepthOnly = new Pass()
        {
            Name = "DepthForwardOnly",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            ShaderPassInclude = "ShaderPassDepthOnly",
            PixelShaderSlots = new List<int>()
            {
                UnlitMasterNode.ColorSlotId,
                UnlitMasterNode.AlphaSlotId
            }
        };

        private static string GetShaderPassFromTemplate(UnlitMasterNode masterNode, Pass pass, GenerationMode mode)
        {
            var builder = new ShaderStringBuilder();
            builder.IncreaseIndent();
            builder.IncreaseIndent();

            var surfaceDescriptionFunction = new ShaderGenerator();
            var surfaceDescriptionStruct = new ShaderGenerator();
            var surfaceInputs = new ShaderGenerator();
            var functionRegistry = new FunctionRegistry(builder);

            var shaderProperties = new PropertyCollector();

            surfaceInputs.AddShaderChunk("struct SurfaceInputs{", false);
            surfaceInputs.Indent();

            var activeNodeList = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, masterNode, NodeUtils.IncludeSelf.Include, pass.PixelShaderSlots);

            var requirements = ShaderGraphRequirements.FromNodes(activeNodeList);

            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresNormal, InterpolatorType.Normal, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresTangent, InterpolatorType.Tangent, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresBitangent, InterpolatorType.BiTangent, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresViewDir, InterpolatorType.ViewDirection, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresPosition, InterpolatorType.Position, surfaceInputs);

            ShaderGenerator defines = new ShaderGenerator();
            defines.AddShaderChunk(string.Format("#define SHADERPASS {0}", pass.ShaderPassName), true);

            if (requirements.requiresVertexColor)
                surfaceInputs.AddShaderChunk(string.Format("float4 {0};", ShaderGeneratorNames.VertexColor), false);

            if (requirements.requiresScreenPosition)
                surfaceInputs.AddShaderChunk(string.Format("float4 {0};", ShaderGeneratorNames.ScreenPosition), false);

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
            {
                surfaceInputs.AddShaderChunk(string.Format("half4 {0};", channel.GetUVName()), false);
                defines.AddShaderChunk(string.Format("#define ATTRIBUTES_NEED_TEXCOORD{0}", (int)channel), true);
                defines.AddShaderChunk(string.Format("#define VARYINGS_NEED_TEXCOORD{0}", (int)channel), true);
            }

            surfaceInputs.Deindent();
            surfaceInputs.AddShaderChunk("};", false);

            var slots = new List<MaterialSlot>();
            foreach (var id in pass.PixelShaderSlots)
            {
                var slot = masterNode.FindSlot<MaterialSlot>(id);
                if (slot != null)
                    slots.Add(slot);
            }

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
            graph.AddShaderChunk(shaderProperties.GetPropertiesDeclaration(2), false);
            graph.AddShaderChunk(surfaceInputs.GetShaderString(2), false);
            graph.AddShaderChunk(builder.ToString(), false);
            graph.AddShaderChunk(surfaceDescriptionStruct.GetShaderString(2), false);
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

            var localPixelShader = new ShaderGenerator();
            var localSurfaceInputs = new ShaderGenerator();
            var surfaceOutputRemap = new ShaderGenerator();

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
                localSurfaceInputs.AddShaderChunk(string.Format("surfaceInput.{0} = {1};", channel.GetUVName(), string.Format("half4(input.texCoord{0}, 0, 0)", (int)channel)), false);

            var templateLocation = ShaderGenerator.GetTemplatePath("HDUnlitPassForward.template");

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
            resultPass = resultPass.Replace("${LocalPixelShader}", localPixelShader.GetShaderString(3));
            resultPass = resultPass.Replace("${SurfaceInputs}", localSurfaceInputs.GetShaderString(3));
            resultPass = resultPass.Replace("${SurfaceOutputRemap}", surfaceOutputRemap.GetShaderString(3));
            resultPass = resultPass.Replace("${LightMode}", pass.Name);
            resultPass = resultPass.Replace("${ShaderPassInclude}", pass.ShaderPassInclude);

            resultPass = resultPass.Replace("${Tags}", tagsVisitor.GetShaderString(2));
            resultPass = resultPass.Replace("${Blending}", blendingVisitor.GetShaderString(2));
            resultPass = resultPass.Replace("${Culling}", cullingVisitor.GetShaderString(2));
            resultPass = resultPass.Replace("${ZTest}", zTestVisitor.GetShaderString(2));
            resultPass = resultPass.Replace("${ZWrite}", zWriteVisitor.GetShaderString(2));
            resultPass = resultPass.Replace("${LOD}", "" + materialOptions.lod);
            return resultPass;
        }

        public string GetSubshader(IMasterNode inMasterNode, GenerationMode mode)
        {
            var masterNode = inMasterNode as UnlitMasterNode;
            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            subShader.AddShaderChunk("Tags{ \"RenderType\" = \"Opaque\" }", true);

            subShader.AddShaderChunk(
                GetShaderPassFromTemplate(
                    masterNode,
                    m_UnlitPassForwardDepthOnly,
                    mode),
                true);

            subShader.AddShaderChunk(
                GetShaderPassFromTemplate(
                    masterNode,
                    m_UnlitPassForwardOnly,
                    mode),
                true);

            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            return subShader.GetShaderString(0);
        }
    }
}
