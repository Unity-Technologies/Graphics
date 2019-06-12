using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;

namespace UnityEngine.Rendering.LWRP
{
    [Serializable]
    [FormerName("UnityEngine.Experimental.Rendering.LightweightPipeline.LightWeightUnlitSubShader")]
    [FormerName("UnityEditor.ShaderGraph.LightWeightUnlitSubShader")]
    class LightWeightUnlitSubShader : IUnlitSubShader
    {
        static readonly NeededCoordinateSpace k_PixelCoordinateSpace = NeededCoordinateSpace.World;

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
                UnlitMasterNode.ColorSlotId,
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                UnlitMasterNode.PositionSlotId
            }
        };

        Pass m_DepthShadowPass = new Pass()
        {
            Name = "",
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                PBRMasterNode.PositionSlotId
            }
        };
        
        public int GetPreviewPassIndex() { return 0; }

        public string GetSubshader(IMasterNode masterNode, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // LightWeightUnlitSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("3ef30c5c1d5fc412f88511ef5818b654"));
            }

            var templatePath = GetTemplatePath("lightweightUnlitPass.template");
            var extraPassesTemplatePath = GetTemplatePath("lightweightUnlitExtraPasses.template");
            if (!File.Exists(templatePath) || !File.Exists(extraPassesTemplatePath))
                return string.Empty;

            if (sourceAssetDependencyPaths != null)
            {
                sourceAssetDependencyPaths.Add(templatePath);
                sourceAssetDependencyPaths.Add(extraPassesTemplatePath);

                var relativePath = "Packages/com.unity.render-pipelines.lightweight/";
                var fullPath = Path.GetFullPath(relativePath);
                var shaderFiles = Directory.GetFiles(Path.Combine(fullPath, "ShaderLibrary")).Select(x => Path.Combine(relativePath, x.Substring(fullPath.Length)));
                sourceAssetDependencyPaths.AddRange(shaderFiles);
            }

            string forwardTemplate = File.ReadAllText(templatePath);
            string extraTemplate = File.ReadAllText(extraPassesTemplatePath);

            var unlitMasterNode = masterNode as UnlitMasterNode;
            var pass = m_UnlitPass;
            var subShader = new ShaderStringBuilder();
            subShader.AppendLine("SubShader");
            using (subShader.BlockScope())
            {
                var materialTags = ShaderGenerator.BuildMaterialTags(unlitMasterNode.surfaceType);
                var tagsBuilder = new ShaderStringBuilder(0);
                materialTags.GetTags(tagsBuilder, LightweightRenderPipeline.k_ShaderTagName);
                subShader.AppendLines(tagsBuilder.ToString());

                var materialOptions = ShaderGenerator.GetMaterialOptions(unlitMasterNode.surfaceType, unlitMasterNode.alphaMode, unlitMasterNode.twoSided.isOn);
                subShader.AppendLines(GetShaderPassFromTemplate(
                        forwardTemplate,
                        unlitMasterNode,
                        pass,
                        mode,
                        materialOptions));

                subShader.AppendLines(GetShaderPassFromTemplate(
                        extraTemplate,
                        unlitMasterNode,
                        m_DepthShadowPass,
                        mode,
                        materialOptions));
            }

            return subShader.ToString();
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is LightweightRenderPipelineAsset;
        }

        static string GetTemplatePath(string templateName)
        {
            var basePath = "Packages/com.unity.render-pipelines.lightweight/Editor/ShaderGraph/";
            string templatePath = Path.Combine(basePath, templateName);

            if (File.Exists(templatePath))
                return templatePath;

            throw new FileNotFoundException(string.Format(@"Cannot find a template with name ""{0}"".", templateName));
        }

        static string GetShaderPassFromTemplate(string template, UnlitMasterNode masterNode, Pass pass, GenerationMode mode, SurfaceMaterialOptions materialOptions)
        {
            // ----------------------------------------------------- //
            //                         SETUP                         //
            // ----------------------------------------------------- //

            // -------------------------------------
            // String builders

            var shaderProperties = new PropertyCollector();
            var shaderKeywords = new KeywordCollector();
            var shaderPropertyUniforms = new ShaderStringBuilder(1);
            var shaderKeywordDeclarations = new ShaderStringBuilder(1);
            
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
            // Requirements

            var vertexRequirements = ShaderGraphRequirements.FromNodes(vertexNodes, ShaderStageCapability.Vertex, false);
            var pixelRequirements = ShaderGraphRequirements.FromNodes(pixelNodes, ShaderStageCapability.Fragment);
            var graphRequirements = pixelRequirements.Union(vertexRequirements);
            var surfaceRequirements = ShaderGraphRequirements.FromNodes(pixelNodes, ShaderStageCapability.Fragment, false);

            var modelRequiements = ShaderGraphRequirements.none;
            modelRequiements.requiresNormal |= k_PixelCoordinateSpace;
            modelRequiements.requiresTangent |= k_PixelCoordinateSpace;
            modelRequiements.requiresBitangent |= k_PixelCoordinateSpace;
            modelRequiements.requiresPosition |= k_PixelCoordinateSpace;
            modelRequiements.requiresViewDir |= k_PixelCoordinateSpace;
            modelRequiements.requiresMeshUVs.Add(UVChannel.UV1);

            // ----------------------------------------------------- //
            //                      INTERPOLATORS                    //
            // ----------------------------------------------------- //

            // -------------------------------------
            // Prepare interpolator structs

            vertexDescriptionInputStruct.AppendLine("struct VertexDescriptionInputs");
            vertexDescriptionInputStruct.AppendLine("{");
            vertexDescriptionInputStruct.IncreaseIndent();

            surfaceDescriptionInputStruct.AppendLine("struct SurfaceDescriptionInputs");
            surfaceDescriptionInputStruct.AppendLine("{");
            surfaceDescriptionInputStruct.IncreaseIndent();

            masterNode.owner.CollectShaderKeywords(shaderKeywords, mode);
            var keywordPermutations = KeywordUtil.GetKeywordPermutations(shaderKeywords.keywords);

            for(int i = 0; i < keywordPermutations.Count; i++)
            {
                // If null there are no keywords
                if(keywordPermutations[i] != null)
                {
                    vertexNodes.Clear();
                    pixelNodes.Clear();
                    NodeUtils.DepthFirstCollectNodesFromNode(vertexNodes, masterNode, NodeUtils.IncludeSelf.Include, pass.VertexShaderSlots, keywordPermutations[i]);
                    NodeUtils.DepthFirstCollectNodesFromNode(pixelNodes, masterNode, NodeUtils.IncludeSelf.Include, pass.PixelShaderSlots, keywordPermutations[i]);
                }

                var localVertexRequirements = ShaderGraphRequirements.FromNodes(vertexNodes, ShaderStageCapability.Vertex, false);
                var localSurfaceRequirements = ShaderGraphRequirements.FromNodes(pixelNodes, ShaderStageCapability.Fragment, false);
                var localPixelRequirements = ShaderGraphRequirements.FromNodes(pixelNodes, ShaderStageCapability.Fragment);

                var localModelRequiements = ShaderGraphRequirements.none;
                localModelRequiements.requiresNormal |= k_PixelCoordinateSpace;
                localModelRequiements.requiresTangent |= k_PixelCoordinateSpace;
                localModelRequiements.requiresBitangent |= k_PixelCoordinateSpace;
                localModelRequiements.requiresPosition |= k_PixelCoordinateSpace;
                localModelRequiements.requiresViewDir |= k_PixelCoordinateSpace;
                localModelRequiements.requiresMeshUVs.Add(UVChannel.UV1);

                // -------------------------------------
                // Generate Input structure for Vertex Description function
                // TODO - Vertex Description Input requirements are needed to exclude intermediate translation spaces

                // If null there are no keywords
                if(keywordPermutations[i] != null)
                {
                    vertexDescriptionInputStruct.AppendLine(KeywordUtil.GetKeywordPermutationString(keywordPermutations[i], i, keywordPermutations.Count));
                    vertexDescriptionInputStruct.IncreaseIndent();
                }

                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(localVertexRequirements.requiresNormal, InterpolatorType.Normal, vertexDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(localVertexRequirements.requiresTangent, InterpolatorType.Tangent, vertexDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(localVertexRequirements.requiresBitangent, InterpolatorType.BiTangent, vertexDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(localVertexRequirements.requiresViewDir, InterpolatorType.ViewDirection, vertexDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(localVertexRequirements.requiresPosition, InterpolatorType.Position, vertexDescriptionInputStruct);

                if (localVertexRequirements.requiresVertexColor)
                    vertexDescriptionInputStruct.AppendLine("float4 {0};", ShaderGeneratorNames.VertexColor);

                if (localVertexRequirements.requiresScreenPosition)
                    vertexDescriptionInputStruct.AppendLine("float4 {0};", ShaderGeneratorNames.ScreenPosition);

                foreach (var channel in localVertexRequirements.requiresMeshUVs.Distinct())
                    vertexDescriptionInputStruct.AppendLine("half4 {0};", channel.GetUVName());

                // If null there are no keywords
                if(keywordPermutations[i] != null)
                {
                    vertexDescriptionInputStruct.DecreaseIndent();
                }

                // -------------------------------------
                // Generate Input structure for Surface Description function
                // Surface Description Input requirements are needed to exclude intermediate translation spaces

                // If null there are no keywords
                if(keywordPermutations[i] != null)
                {
                    surfaceDescriptionInputStruct.AppendLine(KeywordUtil.GetKeywordPermutationString(keywordPermutations[i], i, keywordPermutations.Count));
                    surfaceDescriptionInputStruct.IncreaseIndent();
                }

                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(localSurfaceRequirements.requiresNormal, InterpolatorType.Normal, surfaceDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(localSurfaceRequirements.requiresTangent, InterpolatorType.Tangent, surfaceDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(localSurfaceRequirements.requiresBitangent, InterpolatorType.BiTangent, surfaceDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(localSurfaceRequirements.requiresViewDir, InterpolatorType.ViewDirection, surfaceDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(localSurfaceRequirements.requiresPosition, InterpolatorType.Position, surfaceDescriptionInputStruct);

                if (localSurfaceRequirements.requiresVertexColor)
                    surfaceDescriptionInputStruct.AppendLine("float4 {0};", ShaderGeneratorNames.VertexColor);

                if (localSurfaceRequirements.requiresScreenPosition)
                    surfaceDescriptionInputStruct.AppendLine("float4 {0};", ShaderGeneratorNames.ScreenPosition);

                if (localSurfaceRequirements.requiresFaceSign)
                    surfaceDescriptionInputStruct.AppendLine("float {0};", ShaderGeneratorNames.FaceSign);

                foreach (var channel in localSurfaceRequirements.requiresMeshUVs.Distinct())
                    surfaceDescriptionInputStruct.AppendLine("half4 {0};", channel.GetUVName());

                // If null there are no keywords
                if(keywordPermutations[i] != null)
                {
                    surfaceDescriptionInputStruct.DecreaseIndent();
                }

                // -------------------------------------
                // Generate standard transformations
                // This method ensures all required transform data is available in vertex and pixel stages

                // If null there are no keywords
                if(keywordPermutations[i] != null)
                {
                    string keywordPermutationString = KeywordUtil.GetKeywordPermutationString(keywordPermutations[i], i, keywordPermutations.Count);

                    vertexOutputStruct.AppendLine(keywordPermutationString);
                    vertexOutputStruct.IncreaseIndent();

                    vertexShader.AppendLine(keywordPermutationString);
                    vertexShader.IncreaseIndent();

                    vertexShaderDescriptionInputs.AppendLine(keywordPermutationString);
                    vertexShaderDescriptionInputs.IncreaseIndent();

                    vertexShaderOutputs.AppendLine(keywordPermutationString);
                    vertexShaderOutputs.IncreaseIndent();

                    pixelShader.AppendLine(keywordPermutationString);
                    pixelShader.IncreaseIndent();

                    pixelShaderSurfaceInputs.AppendLine(keywordPermutationString);
                    pixelShaderSurfaceInputs.IncreaseIndent();
                }

                ShaderGenerator.GenerateStandardTransforms(
                    3,
                    10,
                    vertexOutputStruct,
                    vertexShader,
                    vertexShaderDescriptionInputs,
                    vertexShaderOutputs,
                    pixelShader,
                    pixelShaderSurfaceInputs,
                    localPixelRequirements,
                    localSurfaceRequirements,
                    localModelRequiements,
                    localVertexRequirements,
                    CoordinateSpace.World);

                // If null there are no keywords
                if(keywordPermutations[i] != null)
                {
                    vertexOutputStruct.DecreaseIndent();
                    vertexShader.DecreaseIndent();
                    vertexShaderDescriptionInputs.DecreaseIndent();
                    vertexShaderOutputs.DecreaseIndent();
                    pixelShader.DecreaseIndent();
                    pixelShaderSurfaceInputs.DecreaseIndent();
                }
            }

            // -------------------------------------
            // Finalise interpolator structs

            // If first entry is null there are no keywords
            if(keywordPermutations[0] != null)
            {
                vertexDescriptionInputStruct.AppendLine("#endif");
                surfaceDescriptionInputStruct.AppendLine("#endif");
                vertexOutputStruct.AppendLine("#endif");
                vertexShader.AppendLine("#endif");
                vertexShaderDescriptionInputs.AppendLine("#endif");
                vertexShaderOutputs.AppendLine("#endif");
                pixelShader.AppendLine("#endif");
                pixelShaderSurfaceInputs.AppendLine("#endif");
            }

            vertexDescriptionInputStruct.DecreaseIndent();
            vertexDescriptionInputStruct.AppendLine("};");

            surfaceDescriptionInputStruct.DecreaseIndent();
            surfaceDescriptionInputStruct.AppendLine("};");

            // ----------------------------------------------------- //
            //                START VERTEX DESCRIPTION               //
            // ----------------------------------------------------- //

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
                shaderKeywords,
                mode,
                masterNode,
                vertexNodes,
                vertexSlots);

            // ----------------------------------------------------- //
            //               START SURFACE DESCRIPTION               //
            // ----------------------------------------------------- //

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
                shaderKeywords,
                mode,
                "PopulateSurfaceData",
                "SurfaceDescription",
                null,
                pixelSlots);

            // ----------------------------------------------------- //
            //           GENERATE VERTEX > PIXEL PIPELINE            //
            // ----------------------------------------------------- //

            // -------------------------------------
            // Generate defines

            if (masterNode.IsSlotConnected(UnlitMasterNode.AlphaThresholdSlotId))
                defines.AppendLine("#define _AlphaClip 1");

            if (masterNode.surfaceType == SurfaceType.Transparent && masterNode.alphaMode == AlphaMode.Premultiply)
                defines.AppendLine("#define _ALPHAPREMULTIPLY_ON 1");

            if (graphRequirements.requiresDepthTexture)
                defines.AppendLine("#define REQUIRE_DEPTH_TEXTURE");

            if (graphRequirements.requiresCameraOpaqueTexture)
                defines.AppendLine("#define REQUIRE_OPAQUE_TEXTURE");

            // -------------------------------------
            // Keyword declarations

            shaderKeywords.GetKeywordsDeclaration(shaderKeywordDeclarations, mode);

            // -------------------------------------
            // Property uniforms

            shaderProperties.GetPropertiesDeclaration(shaderPropertyUniforms, mode, masterNode.owner.concretePrecision);

            // -------------------------------------
            // Generate Input structure for Vertex shader

            GraphUtil.GenerateApplicationVertexInputs(vertexRequirements.Union(pixelRequirements.Union(modelRequiements)), vertexInputStruct);

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

            graph.AppendLines(shaderKeywordDeclarations.ToString());
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
