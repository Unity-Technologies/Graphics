using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.Universal
{
    delegate void OnGeneratePassDelegate(IMasterNode masterNode, ref Pass pass, ref ShaderGraphRequirements requirements);
    struct Pass
    {
        public string Name;
        public string TemplatePath;
        public List<int> VertexShaderSlots;
        public List<int> PixelShaderSlots;
        public ShaderGraphRequirements Requirements;
        public List<string> ExtraDefines;

        public void OnGeneratePass(IMasterNode masterNode, ShaderGraphRequirements requirements)
        {
            if (OnGeneratePassImpl != null)
            {
                OnGeneratePassImpl(masterNode, ref this, ref requirements);
            }
        }
        public OnGeneratePassDelegate OnGeneratePassImpl;
    }

    static class UniversalSubShaderUtilities
    {
        public static readonly NeededCoordinateSpace k_PixelCoordinateSpace = NeededCoordinateSpace.World;

        static string GetTemplatePath(string templateName)
        {
            var basePath = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/";
            string templatePath = Path.Combine(basePath, templateName);

            if (File.Exists(templatePath))
                return templatePath;

            throw new FileNotFoundException(string.Format(@"Cannot find a template with name ""{0}"".", templateName));
        }

        public static string GetSubShader<T>(T masterNode, SurfaceMaterialTags tags, SurfaceMaterialOptions options, Pass[] passes, 
            GenerationMode mode, string customEditorPath = null, List<string> sourceAssetDependencyPaths = null) where T : IMasterNode
        {
            if (sourceAssetDependencyPaths != null)
            {
                var relativePath = "Packages/com.unity.render-pipelines.universal/";
                var fullPath = Path.GetFullPath(relativePath);
                var shaderFiles = Directory.GetFiles(Path.Combine(fullPath, "ShaderLibrary")).Select(x => Path.Combine(relativePath, x.Substring(fullPath.Length)));
                sourceAssetDependencyPaths.AddRange(shaderFiles);
            }

            var subShader = new ShaderStringBuilder();
            subShader.AppendLine("SubShader");
            using (subShader.BlockScope())
            {
                var tagsBuilder = new ShaderStringBuilder(0);
                tags.GetTags(tagsBuilder, UnityEngine.Rendering.Universal.UniversalRenderPipeline.k_ShaderTagName);
                subShader.AppendLines(tagsBuilder.ToString());

                foreach(Pass pass in passes)
                {
                    var templatePath = GetTemplatePath(pass.TemplatePath);
                    if (!File.Exists(templatePath))
                        continue;

                    if (sourceAssetDependencyPaths != null)
                        sourceAssetDependencyPaths.Add(templatePath);

                    string template = File.ReadAllText(templatePath);

                    subShader.AppendLines(GetShaderPassFromTemplate(
                        template,
                        masterNode,
                        pass,
                        mode,
                        options));
                }
            }

            if(!string.IsNullOrEmpty(customEditorPath))
                subShader.Append($"CustomEditor \"{customEditorPath}\"");

            return subShader.ToString();
        }

        static string GetShaderPassFromTemplate(string template, IMasterNode iMasterNode, Pass pass, GenerationMode mode, SurfaceMaterialOptions materialOptions)
        {
            // ----------------------------------------------------- //
            //                         SETUP                         //
            // ----------------------------------------------------- //

            AbstractMaterialNode masterNode = iMasterNode as AbstractMaterialNode;

            // -------------------------------------
            // String builders

            var shaderProperties = new PropertyCollector();
            var shaderPropertyUniforms = new ShaderStringBuilder(1);
            var functionBuilder = new ShaderStringBuilder(1);
            var functionRegistry = new FunctionRegistry(functionBuilder);

            var defines = new ShaderStringBuilder(1);
            var graph = new ShaderStringBuilder(0);

            var vertexDescriptionInputStruct = new ShaderStringBuilder(1);
            var vertexDescriptionStruct = new ShaderStringBuilder(1);
            var vertexDescriptionFunction = new ShaderStringBuilder(1);

            var surfaceDescriptionInputStruct = new ShaderStringBuilder(1);
            var surfaceDescriptionStruct = new ShaderStringBuilder(1);
            var surfaceDescriptionFunction = new ShaderStringBuilder(1);

            var vertexInputStruct = new ShaderStringBuilder(1);
            var vertexOutputStruct = new ShaderStringBuilder(2);

            var vertexShader = new ShaderStringBuilder(2);
            var vertexShaderDescriptionInputs = new ShaderStringBuilder(2);
            var vertexShaderOutputs = new ShaderStringBuilder(2);

            var pixelShader = new ShaderStringBuilder(2);
            var pixelShaderSurfaceInputs = new ShaderStringBuilder(2);
            var pixelShaderSurfaceRemap = new ShaderStringBuilder(2);

            // -------------------------------------
            // Get Slot and Node lists per stage

            var vertexSlots = pass.VertexShaderSlots.Select(masterNode.FindSlot<MaterialSlot>).ToList();
            var vertexNodes = ListPool<AbstractMaterialNode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(vertexNodes, masterNode, NodeUtils.IncludeSelf.Include, pass.VertexShaderSlots);

            var pixelSlots = pass.PixelShaderSlots.Select(masterNode.FindSlot<MaterialSlot>).ToList();
            var pixelNodes = ListPool<AbstractMaterialNode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(pixelNodes, masterNode, NodeUtils.IncludeSelf.Include, pass.PixelShaderSlots);

            // -------------------------------------
            // Get Requirements

            var vertexRequirements = ShaderGraphRequirements.FromNodes(vertexNodes, ShaderStageCapability.Vertex, false);
            var pixelRequirements = ShaderGraphRequirements.FromNodes(pixelNodes, ShaderStageCapability.Fragment);
            var graphRequirements = pixelRequirements.Union(vertexRequirements);
            var surfaceRequirements = ShaderGraphRequirements.FromNodes(pixelNodes, ShaderStageCapability.Fragment, false);
            var modelRequirements = pass.Requirements;

            // ----------------------------------------------------- //
            //                START SHADER GENERATION                //
            // ----------------------------------------------------- //

            // -------------------------------------
            // Calculate material options

            var blendingBuilder = new ShaderStringBuilder(1);
            var cullingBuilder = new ShaderStringBuilder(1);
            var zTestBuilder = new ShaderStringBuilder(1);
            var zWriteBuilder = new ShaderStringBuilder(1);

            materialOptions.GetBlend(blendingBuilder);
            materialOptions.GetCull(cullingBuilder);
            materialOptions.GetDepthTest(zTestBuilder);
            materialOptions.GetDepthWrite(zWriteBuilder);

            // -------------------------------------
            // Generate defines

            pass.OnGeneratePass(iMasterNode, graphRequirements);
            
            foreach(string define in pass.ExtraDefines)
                defines.AppendLine(define);

            // ----------------------------------------------------- //
            //                START VERTEX DESCRIPTION               //
            // ----------------------------------------------------- //

            // -------------------------------------
            // Generate Input structure for Vertex Description function
            // TODO - Vertex Description Input requirements are needed to exclude intermediate translation spaces

            vertexDescriptionInputStruct.AppendLine("struct VertexDescriptionInputs");
            using (vertexDescriptionInputStruct.BlockSemicolonScope())
            {
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(vertexRequirements.requiresNormal, InterpolatorType.Normal, vertexDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(vertexRequirements.requiresTangent, InterpolatorType.Tangent, vertexDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(vertexRequirements.requiresBitangent, InterpolatorType.BiTangent, vertexDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(vertexRequirements.requiresViewDir, InterpolatorType.ViewDirection, vertexDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(vertexRequirements.requiresPosition, InterpolatorType.Position, vertexDescriptionInputStruct);

                if (vertexRequirements.requiresVertexColor)
                    vertexDescriptionInputStruct.AppendLine("float4 {0};", ShaderGeneratorNames.VertexColor);

                if (vertexRequirements.requiresScreenPosition)
                    vertexDescriptionInputStruct.AppendLine("float4 {0};", ShaderGeneratorNames.ScreenPosition);

                foreach (var channel in vertexRequirements.requiresMeshUVs.Distinct())
                    vertexDescriptionInputStruct.AppendLine("half4 {0};", channel.GetUVName());

                if (vertexRequirements.requiresTime)
                {
                    vertexDescriptionInputStruct.AppendLine("float3 {0};", ShaderGeneratorNames.TimeParameters);
                }
            }

            // -------------------------------------
            // Generate Output structure for Vertex Description function

            GraphUtil.GenerateVertexDescriptionStruct(vertexDescriptionStruct, vertexSlots);

            // -------------------------------------
            // Generate Vertex Description function

            GraphUtil.GenerateVertexDescriptionFunction(
                masterNode.owner as GraphData,
                vertexDescriptionFunction,
                functionRegistry,
                shaderProperties,
                mode,
                vertexNodes,
                vertexSlots);

            // ----------------------------------------------------- //
            //               START SURFACE DESCRIPTION               //
            // ----------------------------------------------------- //

            // -------------------------------------
            // Generate Input structure for Surface Description function
            // Surface Description Input requirements are needed to exclude intermediate translation spaces

            surfaceDescriptionInputStruct.AppendLine("struct SurfaceDescriptionInputs");
            using (surfaceDescriptionInputStruct.BlockSemicolonScope())
            {
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(surfaceRequirements.requiresNormal, InterpolatorType.Normal, surfaceDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(surfaceRequirements.requiresTangent, InterpolatorType.Tangent, surfaceDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(surfaceRequirements.requiresBitangent, InterpolatorType.BiTangent, surfaceDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(surfaceRequirements.requiresViewDir, InterpolatorType.ViewDirection, surfaceDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(surfaceRequirements.requiresPosition, InterpolatorType.Position, surfaceDescriptionInputStruct);

                if (surfaceRequirements.requiresVertexColor)
                    surfaceDescriptionInputStruct.AppendLine("float4 {0};", ShaderGeneratorNames.VertexColor);

                if (surfaceRequirements.requiresScreenPosition)
                    surfaceDescriptionInputStruct.AppendLine("float4 {0};", ShaderGeneratorNames.ScreenPosition);

                if (surfaceRequirements.requiresFaceSign)
                    surfaceDescriptionInputStruct.AppendLine("float {0};", ShaderGeneratorNames.FaceSign);

                foreach (var channel in surfaceRequirements.requiresMeshUVs.Distinct())
                    surfaceDescriptionInputStruct.AppendLine("half4 {0};", channel.GetUVName());

                if (surfaceRequirements.requiresTime)
                {
                    surfaceDescriptionInputStruct.AppendLine("float3 {0};", ShaderGeneratorNames.TimeParameters);
                }
            }

            // -------------------------------------
            // Generate Output structure for Surface Description function

            GraphUtil.GenerateSurfaceDescriptionStruct(surfaceDescriptionStruct, pixelSlots);

            // -------------------------------------
            // Generate Surface Description function

            GraphUtil.GenerateSurfaceDescriptionFunction(
                pixelNodes,
                masterNode,
                masterNode.owner as GraphData,
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
            // Property uniforms

            shaderProperties.GetPropertiesDeclaration(shaderPropertyUniforms, mode, masterNode.owner.concretePrecision);

            // -------------------------------------
            // Generate Input structure for Vertex shader

            GraphUtil.GenerateApplicationVertexInputs(vertexRequirements.Union(pixelRequirements.Union(modelRequirements)), vertexInputStruct);

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
                modelRequirements,
                vertexRequirements,
                CoordinateSpace.World);

            // -------------------------------------
            // Generate pixel shader surface remap

            foreach (var slot in pixelSlots)
            {
                pixelShaderSurfaceRemap.AppendLine("{0} = surf.{0};", slot.shaderOutputName);
            }

            // -------------------------------------
            // Extra pixel shader work

            var faceSign = new ShaderStringBuilder();

            if (pixelRequirements.requiresFaceSign)
                faceSign.AppendLine(", half FaceSign : VFACE");

            // ----------------------------------------------------- //
            //                      FINALIZE                         //
            // ----------------------------------------------------- //

            // -------------------------------------
            // Combine Graph sections

            graph.AppendLines(shaderPropertyUniforms.ToString());

            graph.AppendLine(vertexDescriptionInputStruct.ToString());
            graph.AppendLine(surfaceDescriptionInputStruct.ToString());

            graph.AppendLine(functionBuilder.ToString());

            graph.AppendLine(vertexDescriptionStruct.ToString());
            graph.AppendLine(vertexDescriptionFunction.ToString());

            graph.AppendLine(surfaceDescriptionStruct.ToString());
            graph.AppendLine(surfaceDescriptionFunction.ToString());

            graph.AppendLine(vertexInputStruct.ToString());

            // -------------------------------------
            // Generate final subshader

            var resultPass = template.Replace("${Tags}", string.Empty);
            resultPass = resultPass.Replace("${Blending}", blendingBuilder.ToString());
            resultPass = resultPass.Replace("${Culling}", cullingBuilder.ToString());
            resultPass = resultPass.Replace("${ZTest}", zTestBuilder.ToString());
            resultPass = resultPass.Replace("${ZWrite}", zWriteBuilder.ToString());
            resultPass = resultPass.Replace("${Defines}", defines.ToString());

            resultPass = resultPass.Replace("${Graph}", graph.ToString());
            resultPass = resultPass.Replace("${VertexOutputStruct}", vertexOutputStruct.ToString());

            resultPass = resultPass.Replace("${VertexShader}", vertexShader.ToString());
            resultPass = resultPass.Replace("${VertexShaderDescriptionInputs}", vertexShaderDescriptionInputs.ToString());
            resultPass = resultPass.Replace("${VertexShaderOutputs}", vertexShaderOutputs.ToString());

            resultPass = resultPass.Replace("${FaceSign}", faceSign.ToString());
            resultPass = resultPass.Replace("${PixelShader}", pixelShader.ToString());
            resultPass = resultPass.Replace("${PixelShaderSurfaceInputs}", pixelShaderSurfaceInputs.ToString());
            resultPass = resultPass.Replace("${PixelShaderSurfaceRemap}", pixelShaderSurfaceRemap.ToString());

            return resultPass;
        }
    }
}
