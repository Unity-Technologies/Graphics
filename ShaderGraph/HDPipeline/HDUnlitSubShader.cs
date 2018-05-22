using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class HDUnlitSubShader
    {
        static NeededCoordinateSpace m_VertexCoordinateSpace = NeededCoordinateSpace.Object;
        static NeededCoordinateSpace m_PixelCoordinateSpace = NeededCoordinateSpace.World;

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
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId
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
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId
            }
        };

        public string GetSubshader(IMasterNode inMasterNode, GenerationMode mode)
        {
            var templatePath = GetTemplatePath("HDUnlitPassForward.template");
            if (!File.Exists(templatePath))
                return string.Empty;

            string passTemplate = File.ReadAllText(templatePath);

            var masterNode = inMasterNode as UnlitMasterNode;
            var subShader = new ShaderStringBuilder();
            subShader.AppendLine("SubShader");
            using(subShader.BlockScope())
            {
                subShader.AppendLine("Tags{ \"RenderPipeline\" = \"HDRenderPipeline\"}");
                subShader.AppendLine("Tags{ \"RenderType\" = \"Opaque\" }");

                subShader.AppendLines(
                    GetShaderPassFromTemplate(
                        passTemplate,
                        masterNode,
                        m_UnlitPassForwardDepthOnly,
                        mode));

                subShader.AppendLines(
                    GetShaderPassFromTemplate(
                        passTemplate,
                        masterNode,
                        m_UnlitPassForwardOnly,
                        mode));
            }
            
            return subShader.ToString();
        }

        static string GetTemplatePath(string templateName)
        {
            var templatePath = ShaderGenerator.GetTemplatePath(templateName);
                if (File.Exists(templatePath))
                    return templatePath;

            throw new FileNotFoundException(string.Format(@"Cannot find a template with name ""{0}"".", templateName));
        }

        private static string GetShaderPassFromTemplate(string template, UnlitMasterNode masterNode, Pass pass, GenerationMode mode)
        {
            // ----------------------------------------------------- //
            //                         SETUP                         //
            // ----------------------------------------------------- //

            // -------------------------------------
            // String builders

            var shaderProperties = new PropertyCollector();
            var functionBuilder = new ShaderStringBuilder(1);
            var functionRegistry = new FunctionRegistry(functionBuilder);

            var defines = new ShaderStringBuilder(1);
            var graph = new ShaderStringBuilder(0);

            var surfaceDescriptionInputStruct = new ShaderStringBuilder(1);
            var surfaceDescriptionFunction = new ShaderStringBuilder(1);
            var surfaceDescriptionStruct = new ShaderStringBuilder(1); 

            var pixelShader = new ShaderStringBuilder(2);
            var pixelShaderSurfaceInputs = new ShaderStringBuilder(2);
            var pixelShaderSurfaceRemap = new ShaderStringBuilder(2);

            // -------------------------------------
            // Get Slot and Node lists per stage

            var pixelSlots = pass.PixelShaderSlots.Select(masterNode.FindSlot<MaterialSlot>).ToList();
            var pixelNodes = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(pixelNodes, masterNode, NodeUtils.IncludeSelf.Include, pass.PixelShaderSlots);

            // -------------------------------------
            // Get Requirements

            var pixelRequirements = ShaderGraphRequirements.FromNodes(pixelNodes);

            // ----------------------------------------------------- //
            //                START SHADER GENERATION                //
            // ----------------------------------------------------- //

            // -------------------------------------
            // Calculate material options

            var tagsBuilder = new ShaderStringBuilder(1);
            var blendingBuilder = new ShaderStringBuilder(1);
            var cullingBuilder = new ShaderStringBuilder(1);
            var zTestBuilder = new ShaderStringBuilder(1);
            var zWriteBuilder = new ShaderStringBuilder(1);

            var materialOptions = new SurfaceMaterialOptions();
            materialOptions.GetTags(tagsBuilder);
            materialOptions.GetBlend(blendingBuilder);
            materialOptions.GetCull(cullingBuilder);
            materialOptions.GetDepthTest(zTestBuilder);
            materialOptions.GetDepthWrite(zWriteBuilder);

            // -------------------------------------
            // Generate defines

            defines.AppendLine("#define SHADERPASS {0}", pass.ShaderPassName);
            foreach (var channel in pixelRequirements.requiresMeshUVs.Distinct())
            {
                defines.AppendLine("#define ATTRIBUTES_NEED_TEXCOORD{0}", (int)channel);
                defines.AppendLine("#define VARYINGS_NEED_TEXCOORD{0}", (int)channel);
            }

            if (masterNode.IsSlotConnected(PBRMasterNode.AlphaThresholdSlotId))
                defines.AppendLine("#define _ALPHATEST_ON");

            // ----------------------------------------------------- //
            //               START SURFACE DESCRIPTION               //
            // ----------------------------------------------------- //

            // -------------------------------------
            // Generate Input structure for Surface Description function
            // Surface Description Input requirements are needed to exclude intermediate translation spaces

            surfaceDescriptionInputStruct.AppendLine("struct SurfaceDescriptionInputs", false);
            using(surfaceDescriptionInputStruct.BlockSemicolonScope())
            {
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(pixelRequirements.requiresNormal, InterpolatorType.Normal, surfaceDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(pixelRequirements.requiresTangent, InterpolatorType.Tangent, surfaceDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(pixelRequirements.requiresBitangent, InterpolatorType.BiTangent, surfaceDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(pixelRequirements.requiresViewDir, InterpolatorType.ViewDirection, surfaceDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(pixelRequirements.requiresPosition, InterpolatorType.Position, surfaceDescriptionInputStruct);

                if (pixelRequirements.requiresVertexColor)
                    surfaceDescriptionInputStruct.AppendLine("float4 {0};", ShaderGeneratorNames.VertexColor);

                if (pixelRequirements.requiresScreenPosition)
                    surfaceDescriptionInputStruct.AppendLine("float4 {0};", ShaderGeneratorNames.ScreenPosition);

                foreach (var channel in pixelRequirements.requiresMeshUVs.Distinct())
            {
                    surfaceDescriptionInputStruct.AppendLine("half4 {0};", channel.GetUVName());
                }
            }

            // -------------------------------------
            // Generate Output structure for Surface Description function

            GraphUtil.GenerateSurfaceDescriptionStruct(surfaceDescriptionStruct, pixelSlots, true);
            
            // -------------------------------------
            // Generate Surface Description function

            GraphUtil.GenerateSurfaceDescriptionFunction(
                pixelNodes,
                masterNode,
                masterNode.owner as AbstractMaterialGraph,
                surfaceDescriptionFunction,
                functionRegistry,
                shaderProperties,
                pixelRequirements,
                mode,
                "PopulateSurfaceData",
                "SurfaceDescription",
                null,
                pixelSlots);

            // ----------------------------------------------------- //
            //           GENERATE VERTEX > PIXEL PIPELINE            //
            // ----------------------------------------------------- //

            // -------------------------------------
            // TODO - Why is this not a full generation?
            // Generate standard transformations

            foreach (var channel in pixelRequirements.requiresMeshUVs.Distinct())
                pixelShaderSurfaceInputs.AppendLine("surfaceInput.{0} = {1};", channel.GetUVName(), string.Format("half4(input.texCoord{0}, 0, 0)", (int)channel));

            // -------------------------------------
            // Generate pixel shader surface remap

            foreach (var slot in pixelSlots)
            {
                pixelShaderSurfaceRemap.AppendLine("{0} = surf.{0};", slot.shaderOutputName);
            }

            // ----------------------------------------------------- //
            //                      FINALIZE                         //
            // ----------------------------------------------------- //

            // -------------------------------------
            // Combine Graph sections
            
            graph.AppendLine(shaderProperties.GetPropertiesDeclaration(1));

            graph.AppendLine(functionBuilder.ToString());

            graph.AppendLine(surfaceDescriptionInputStruct.ToString());
            graph.AppendLine(surfaceDescriptionStruct.ToString());
            graph.AppendLine(surfaceDescriptionFunction.ToString());

            // -------------------------------------
            // Generate final subshader

            var resultPass = template.Replace("${Tags}", tagsBuilder.ToString());
            resultPass = resultPass.Replace("${Blending}", blendingBuilder.ToString());
            resultPass = resultPass.Replace("${Culling}", cullingBuilder.ToString());
            resultPass = resultPass.Replace("${ZTest}", zTestBuilder.ToString());
            resultPass = resultPass.Replace("${ZWrite}", zWriteBuilder.ToString());
            resultPass = resultPass.Replace("${Defines}", defines.ToString());
            resultPass = resultPass.Replace("${LOD}", "" + materialOptions.lod);

            resultPass = resultPass.Replace("${LightMode}", pass.Name);
            resultPass = resultPass.Replace("${ShaderPassInclude}", pass.ShaderPassInclude);
            
            resultPass = resultPass.Replace("${Graph}", graph.ToString());

            resultPass = resultPass.Replace("${PixelShader}", pixelShader.ToString());
            resultPass = resultPass.Replace("${PixelShaderSurfaceInputs}", pixelShaderSurfaceInputs.ToString());
            resultPass = resultPass.Replace("${PixelShaderSurfaceRemap}", pixelShaderSurfaceRemap.ToString());
            
            return resultPass;
        }
    }
}
