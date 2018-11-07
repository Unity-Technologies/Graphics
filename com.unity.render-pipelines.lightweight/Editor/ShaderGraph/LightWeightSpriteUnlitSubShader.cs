using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [Serializable]
    public class LightWeightSpriteUnlitSubShader : ISpriteUnlitSubShader
    {
        struct Pass
        {
            public string Name;
            public List<int> VertexShaderSlots;
            public List<int> PixelShaderSlots;
        }

        Pass m_UnlitPass = new Pass
        {
            Name = "Pass",
            PixelShaderSlots = new List<int>
            {
                SpriteUnlitMasterNode.ColorSlotId,
                SpriteUnlitMasterNode.AlphaSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                SpriteUnlitMasterNode.PositionSlotId
            }
        };

        public string GetSubshader(IMasterNode masterNode, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // LightWeightSpriteUnlitSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("62511ee827d14492a8c78ba0ef167e7f"));
            }

            var templatePath = GetTemplatePath("lightweightSpriteUnlitPass.template");

            if (!File.Exists(templatePath))
                return string.Empty;

            if (sourceAssetDependencyPaths != null)
            {
                sourceAssetDependencyPaths.Add(templatePath);
            }

            string forwardTemplate = File.ReadAllText(templatePath);
            var unlitMasterNode = masterNode as SpriteUnlitMasterNode;

            var pass = m_UnlitPass;
            var subShader = new ShaderStringBuilder();
            subShader.AppendLine("SubShader");
            using (subShader.BlockScope())
            {
                subShader.AppendLine("Tags{ \"RenderPipeline\" = \"LightweightPipeline\"}");

                var materialTags = ShaderGenerator.BuildMaterialTags(SurfaceType.Transparent);
                var tagsBuilder = new ShaderStringBuilder(0);
                materialTags.GetTags(tagsBuilder);
                subShader.AppendLines(tagsBuilder.ToString());

                var materialOptions = ShaderGenerator.GetMaterialOptions(SurfaceType.Transparent, AlphaMode.Alpha, true);
                subShader.AppendLines(GetShaderPassFromTemplate(
                        forwardTemplate,
                        unlitMasterNode,
                        pass,
                        mode,
                        materialOptions));
            }

            return subShader.ToString();
        }

        static string GetShaderPassFromTemplate(string template, SpriteUnlitMasterNode masterNode, Pass pass, GenerationMode mode, SurfaceMaterialOptions materialOptions)
        {
            var shaderProperties = new PropertyCollector();
            var functionBuilder = new ShaderStringBuilder(1);
            var functionRegistry = new FunctionRegistry(functionBuilder);

            var blendingBuilder = new ShaderStringBuilder(1);
            var cullingBuilder = new ShaderStringBuilder(1);
            var zTestBuilder = new ShaderStringBuilder(1);
            var zWriteBuilder = new ShaderStringBuilder(1);
            var defines = new ShaderStringBuilder(1);
            var graph = new ShaderStringBuilder(0);

            var vertexInputStruct = new ShaderStringBuilder(1);
            var vertexOutputStruct = new ShaderStringBuilder(2);
            var vertexDescriptionStruct = new ShaderStringBuilder(1);
            var vertexDescriptionFunction = new ShaderStringBuilder(1);

            var surfaceDescriptionInputStruct = new ShaderStringBuilder(1);
            var surfaceDescriptionStruct = new ShaderStringBuilder(1);
            var surfaceDescriptionFunction = new ShaderStringBuilder(1);

            var vertexShader = new ShaderStringBuilder(2);
            var vertexShaderDescriptionInputs = new ShaderStringBuilder(2);
            var vertexShaderOutputs = new ShaderStringBuilder(2);

            var pixelShader = new ShaderStringBuilder(2);
            var pixelShaderSurfaceInputs = new ShaderStringBuilder(2);
            var pixelShaderSurfaceRemap = new ShaderStringBuilder(2);

            var vertexSlots = pass.VertexShaderSlots.Select(masterNode.FindSlot<MaterialSlot>).ToList();
            var vertexNodes = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(vertexNodes, masterNode, NodeUtils.IncludeSelf.Include, pass.VertexShaderSlots);

            var pixelSlots = pass.PixelShaderSlots.Select(masterNode.FindSlot<MaterialSlot>).ToList();
            var pixelNodes = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(pixelNodes, masterNode, NodeUtils.IncludeSelf.Include, pass.PixelShaderSlots);

            var pixelRequirements = ShaderGraphRequirements.FromNodes(pixelNodes, ShaderStageCapability.Fragment);
            var vertexRequirements = ShaderGraphRequirements.FromNodes(vertexNodes, ShaderStageCapability.Vertex, false);
            var surfaceRequirements = ShaderGraphRequirements.FromNodes(pixelNodes, ShaderStageCapability.Fragment, false);

            var modelRequiements = ShaderGraphRequirements.none;
            // modelRequiements.requiresNormal |= k_PixelCoordinateSpace;
            // modelRequiements.requiresTangent |= k_PixelCoordinateSpace;
            // modelRequiements.requiresBitangent |= k_PixelCoordinateSpace;
            // modelRequiements.requiresPosition |= k_PixelCoordinateSpace;
            // modelRequiements.requiresViewDir |= k_PixelCoordinateSpace;
            // modelRequiements.requiresMeshUVs.Add(UVChannel.UV1);

            materialOptions.GetBlend(blendingBuilder);
            materialOptions.GetCull(cullingBuilder);
            materialOptions.GetDepthTest(zTestBuilder);
            materialOptions.GetDepthWrite(zWriteBuilder);

            // if (masterNode.IsSlotConnected(UnlitMasterNode.AlphaThresholdSlotId))
            //     defines.AppendLine("#define _AlphaClip 1");

            GraphUtil.GenerateVertexDescriptionStruct(vertexDescriptionStruct, vertexSlots);

            GraphUtil.GenerateVertexDescriptionFunction(
                masterNode.owner as AbstractMaterialGraph,
                vertexDescriptionFunction,
                functionRegistry,
                shaderProperties,
                mode,
                vertexNodes,
                vertexSlots);

            // -------------------------------------
            // Generate Input structure for Surface Description function
            // Surface Description Input requirements are needed to exclude intermediate translation spaces

            surfaceDescriptionInputStruct.AppendLine("struct SurfaceDescriptionInputs");
            using (surfaceDescriptionInputStruct.BlockSemicolonScope())
            {
            //     ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(surfaceRequirements.requiresNormal, InterpolatorType.Normal, surfaceDescriptionInputStruct);
            //     ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(surfaceRequirements.requiresTangent, InterpolatorType.Tangent, surfaceDescriptionInputStruct);
            //     ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(surfaceRequirements.requiresBitangent, InterpolatorType.BiTangent, surfaceDescriptionInputStruct);
            //     ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(surfaceRequirements.requiresViewDir, InterpolatorType.ViewDirection, surfaceDescriptionInputStruct);
            //     ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(surfaceRequirements.requiresPosition, InterpolatorType.Position, surfaceDescriptionInputStruct);

            //     if (surfaceRequirements.requiresVertexColor)
            //         surfaceDescriptionInputStruct.AppendLine("float4 {0};", ShaderGeneratorNames.VertexColor);

            //     if (surfaceRequirements.requiresScreenPosition)
            //         surfaceDescriptionInputStruct.AppendLine("float4 {0};", ShaderGeneratorNames.ScreenPosition);

            //     if (surfaceRequirements.requiresFaceSign)
            //         surfaceDescriptionInputStruct.AppendLine("float {0};", ShaderGeneratorNames.FaceSign);

                foreach (var channel in surfaceRequirements.requiresMeshUVs.Distinct())
                    surfaceDescriptionInputStruct.AppendLine("half4 {0};", channel.GetUVName());
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

            // -------------------------------------
            // Combine Graph sections
            GraphUtil.GenerateApplicationVertexInputs(vertexRequirements.Union(pixelRequirements.Union(modelRequiements)), vertexInputStruct);

            // -------------------------------------
            // Generate standard transformations
            // This method ensures all required transform data is available in vertex and pixel stages
            ShaderGenerator.GenerateStandardTransforms(
                3,
                10,
                vertexOutputStruct,
                vertexShader,
                vertexShaderDescriptionInputs,
                vertexShaderOutputs,
                pixelShader,
                pixelShaderSurfaceInputs,
                pixelRequirements,
                surfaceRequirements,
                modelRequiements,
                vertexRequirements,
                CoordinateSpace.World);

            foreach (var slot in pixelSlots)
            {
                pixelShaderSurfaceRemap.AppendLine("{0} = surf.{0};", slot.shaderOutputName);
            }

            graph.AppendLine(shaderProperties.GetPropertiesDeclaration(1));

            // graph.AppendLine(vertexDescriptionInputStruct.ToString());
            graph.AppendLine(surfaceDescriptionInputStruct.ToString());

            // graph.AppendLine(functionBuilder.ToString());

            // graph.AppendLine(vertexDescriptionStruct.ToString());
            // graph.AppendLine(vertexDescriptionFunction.ToString());

            graph.AppendLine(surfaceDescriptionStruct.ToString());
            graph.AppendLine(surfaceDescriptionFunction.ToString());

            graph.AppendLine(vertexInputStruct.ToString());

            var resultPass = template.Replace("${Tags}", string.Empty);
            resultPass = resultPass.Replace("${Blending}", blendingBuilder.ToString());
            resultPass = resultPass.Replace("${Culling}", cullingBuilder.ToString());
            resultPass = resultPass.Replace("${ZTest}", zTestBuilder.ToString());
            resultPass = resultPass.Replace("${ZWrite}", zWriteBuilder.ToString());
            resultPass = resultPass.Replace("${Defines}", defines.ToString());

            resultPass = resultPass.Replace("${Graph}", graph.ToString());
            resultPass = resultPass.Replace("${VertexOutputStruct}", vertexOutputStruct.ToString());

            resultPass = resultPass.Replace("${VertexShader}", vertexShader.ToString());
            resultPass = resultPass.Replace("${VertexShaderOutputs}", vertexShaderOutputs.ToString());

            resultPass = resultPass.Replace("${PixelShader}", pixelShader.ToString());
            resultPass = resultPass.Replace("${PixelShaderSurfaceInputs}", pixelShaderSurfaceInputs.ToString());
            resultPass = resultPass.Replace("${PixelShaderSurfaceRemap}", pixelShaderSurfaceRemap.ToString());

            return resultPass;
        }

        static string GetTemplatePath(string templateName)
        {
            var pathSegments = new[] { "Packages", "com.unity.render-pipelines.lightweight", "Editor", "ShaderGraph", templateName };
            var path = pathSegments.Aggregate("", Path.Combine);
            if (!File.Exists(path))
                throw new FileNotFoundException(string.Format(@"Cannot find a template with name ""{0}"".", templateName));
            return path;
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is LightweightRenderPipelineAsset;
        }
    }
}