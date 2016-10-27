using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public class ShaderGenerator
    {
        private struct ShaderChunk
        {
            public ShaderChunk(int indentLevel, string shaderChunkString)
            {
                m_IndentLevel = indentLevel;
                m_ShaderChunkString = shaderChunkString;
            }

            private readonly int m_IndentLevel;
            private readonly string m_ShaderChunkString;

            public int chunkIndentLevel { get { return m_IndentLevel; } }
            public string chunkString { get { return m_ShaderChunkString; } }
        }

        private readonly List<ShaderChunk> m_ShaderChunks = new List<ShaderChunk>();
        private int m_IndentLevel;
        private string m_Pragma = string.Empty;

        public void AddPragmaChunk(string s)
        {
            m_Pragma += s;
        }

        public string GetPragmaString()
        {
            return m_Pragma;
        }

        public void AddShaderChunk(string s, bool unique)
        {
            if (unique && m_ShaderChunks.Any(x => x.chunkString == s))
                return;

            m_ShaderChunks.Add(new ShaderChunk(m_IndentLevel, s));
        }

        public void Indent() { m_IndentLevel++; }
        public void Deindent() { m_IndentLevel = Math.Max(0, m_IndentLevel - 1); }

        public string GetShaderString(int baseIndentLevel)
        {
            var sb = new StringBuilder();
            foreach (var shaderChunk in m_ShaderChunks)
            {
                var lines = shaderChunk.chunkString.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                for (int index = 0; index < lines.Length; index++)
                {
                    var line = lines[index];
                    for (var i = 0; i < shaderChunk.chunkIndentLevel + baseIndentLevel; i++)
                        sb.Append("\t");

                    sb.AppendLine(line);
                }
            }
            return sb.ToString();
        }

        private static string GetTemplatePath(string templateName)
        {
            var path = new List<string>
            {
                Application.dataPath,
                "UnityShaderEditor",
                "Editor",
                "Templates"
            };

            string result = path[0];
            for (int i = 1; i < path.Count; i++)
                result = Path.Combine(result, path[i]);

            result = Path.Combine(result, templateName);
            return result;
        }

        private const string kErrorString = @"ERROR!";
        public static string AdaptNodeOutput(AbstractMaterialNode node, int outputSlotId, ConcreteSlotValueType convertToType, bool textureSampleUVHack = false)
        {
            var outputSlot = node.FindOutputSlot<MaterialSlot>(outputSlotId);

            if (outputSlot == null)
                return kErrorString;

            var convertFromType = outputSlot.concreteValueType;
            var rawOutput = node.GetVariableNameForSlot(outputSlotId);
            if (convertFromType == convertToType)
                return rawOutput;

            switch (convertToType)
            {
                case ConcreteSlotValueType.Vector1:
                    return string.Format("({0}).x", rawOutput);
                case ConcreteSlotValueType.Vector2:
                    switch (convertFromType)
                    {
                        case ConcreteSlotValueType.Vector1:
                            return string.Format("({0}{1})", rawOutput, textureSampleUVHack ? ".xx" : string.Empty);
                        case ConcreteSlotValueType.Vector3:
                        case ConcreteSlotValueType.Vector4:
                            return string.Format("({0}.xy)", rawOutput);
                        default:
                            return kErrorString;
                    }
                case ConcreteSlotValueType.Vector3:
                    switch (convertFromType)
                    {
                        case ConcreteSlotValueType.Vector1:
                            return string.Format("({0}{1})", rawOutput, textureSampleUVHack ? ".xxx" : string.Empty);
                        case ConcreteSlotValueType.Vector4:
                            return string.Format("({0}.xyz)", rawOutput);
                        default:
                            return kErrorString;
                    }
                case ConcreteSlotValueType.Vector4:
                    switch (convertFromType)
                    {
                        case ConcreteSlotValueType.Vector1:
                            return string.Format("({0}{1})", rawOutput, textureSampleUVHack ? ".xxxx" : string.Empty);
                        default:
                            return kErrorString;
                    }
                default:
                    return kErrorString;
            }
        }

        public static string AdaptNodeOutputForPreview(AbstractMaterialNode node, int outputSlotId, ConcreteSlotValueType convertToType)
        {
            var outputSlot = node.FindOutputSlot<MaterialSlot>(outputSlotId);

            if (outputSlot == null)
                return kErrorString;

            var convertFromType = outputSlot.concreteValueType;

            // if we are in a normal situation, just convert!
            if (convertFromType >= convertToType || convertFromType == ConcreteSlotValueType.Vector1)
                return AdaptNodeOutput(node, outputSlotId, convertToType);

            var rawOutput = node.GetVariableNameForSlot(outputSlotId);

            // otherwise we need to pad output for the preview!
            switch (convertToType)
            {
                case ConcreteSlotValueType.Vector3:
                    switch (convertFromType)
                    {
                        case ConcreteSlotValueType.Vector2:
                            return string.Format("half3({0}.x, {0}.y, 0.0)", rawOutput);
                        default:
                            return kErrorString;
                    }
                case ConcreteSlotValueType.Vector4:
                    switch (convertFromType)
                    {
                        case ConcreteSlotValueType.Vector2:
                            return string.Format("half4({0}.x, {0}.y, 0.0, 0.0)", rawOutput);
                        case ConcreteSlotValueType.Vector3:
                            return string.Format("half4({0}.x, {0}.y, {0}.z, 0.0)", rawOutput);
                        default:
                            return kErrorString;
                    }
                default:
                    return kErrorString;
            }
        }

        public static string GeneratePreviewShader(AbstractMaterialNode node, out PreviewMode generatedShaderMode)
        {
            if (!node.GetOutputSlots<MaterialSlot>().Any())
            {
                generatedShaderMode = PreviewMode.Preview2D;
                return string.Empty;
            }

            // figure out what kind of preview we want!
            var activeNodeList = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, node);
            var generationMode = GenerationMode.Preview2D;
            generatedShaderMode = PreviewMode.Preview2D;

            if (activeNodeList.OfType<AbstractMaterialNode>().Any(x => x.previewMode == PreviewMode.Preview3D))
            {
                generationMode = GenerationMode.Preview3D;
                generatedShaderMode = PreviewMode.Preview3D;
            }

            string templateLocation = GetTemplatePath(generationMode == GenerationMode.Preview2D ? "2DPreview.template" : "3DPreview.template");
            if (!File.Exists(templateLocation))
                return null;

            string template = File.ReadAllText(templateLocation);

            var shaderBodyVisitor = new ShaderGenerator();
            var shaderInputVisitor = new ShaderGenerator();
            var shaderFunctionVisitor = new ShaderGenerator();
            var shaderPropertiesVisitor = new PropertyGenerator();
            var shaderPropertyUsagesVisitor = new ShaderGenerator();
            var vertexShaderBlock = new ShaderGenerator();

            var shaderName = "Hidden/PreviewShader/" + node.GetVariableNameForSlot(node.GetOutputSlots<MaterialSlot>().First().id);

            foreach (var activeNode in activeNodeList.OfType<AbstractMaterialNode>())
            {
                if (activeNode is IGeneratesFunction)
                    (activeNode as IGeneratesFunction).GenerateNodeFunction(shaderFunctionVisitor, generationMode);
                if (activeNode is IGeneratesVertexToFragmentBlock)
                    (activeNode as IGeneratesVertexToFragmentBlock).GenerateVertexToFragmentBlock(shaderInputVisitor, generationMode);
                if (activeNode is IGeneratesBodyCode)
                    (activeNode as IGeneratesBodyCode).GenerateNodeCode(shaderBodyVisitor, generationMode);
                if (activeNode is IGeneratesVertexShaderBlock)
                    (activeNode as IGeneratesVertexShaderBlock).GenerateVertexShaderBlock(vertexShaderBlock, generationMode);

                activeNode.GeneratePropertyBlock(shaderPropertiesVisitor, generationMode);
                activeNode.GeneratePropertyUsages(shaderPropertyUsagesVisitor, generationMode);
            }

            if (shaderInputVisitor.numberOfChunks == 0)
            {
                shaderInputVisitor.AddShaderChunk("float4 color : COLOR;", true);
            }

            if (generationMode == GenerationMode.Preview2D)
                shaderBodyVisitor.AddShaderChunk("return " + AdaptNodeOutputForPreview(node, node.GetOutputSlots<MaterialSlot>().First().id, ConcreteSlotValueType.Vector4) + ";", true);
            else
                shaderBodyVisitor.AddShaderChunk("o.Emission = " + AdaptNodeOutputForPreview(node, node.GetOutputSlots<MaterialSlot>().First().id, ConcreteSlotValueType.Vector3) + ";", true);

            template = template.Replace("${ShaderName}", shaderName);
            template = template.Replace("${ShaderPropertiesHeader}", shaderPropertiesVisitor.GetShaderString(2));
            template = template.Replace("${ShaderPropertyUsages}", shaderPropertyUsagesVisitor.GetShaderString(3));
            template = template.Replace("${ShaderInputs}", shaderInputVisitor.GetShaderString(4));
            template = template.Replace("${ShaderFunctions}", shaderFunctionVisitor.GetShaderString(3));
            template = template.Replace("${VertexShaderBody}", vertexShaderBlock.GetShaderString(4));
            template = template.Replace("${PixelShaderBody}", shaderBodyVisitor.GetShaderString(4));

            string vertexShaderBody = vertexShaderBlock.GetShaderString(4);
            if (vertexShaderBody.Length > 0)
            {
                template = template.Replace("${VertexShaderDecl}", "vertex:vert");
                template = template.Replace("${VertexShaderBody}", vertexShaderBody);
            }
            else
            {
                template = template.Replace("${VertexShaderDecl}", "");
                template = template.Replace("${VertexShaderBody}", vertexShaderBody);
            }

            return Regex.Replace(template, @"\r\n|\n\r|\n|\r", Environment.NewLine);
        }

        private static void GenerateSurfaceShaderInternal(
            AbstractMasterNode masterNode, 
            ShaderGenerator shaderBody, 
            ShaderGenerator inputStruct,
            ShaderGenerator lightFunction,
            ShaderGenerator surfaceOutput,
            ShaderGenerator nodeFunction, 
            PropertyGenerator shaderProperties, 
            ShaderGenerator propertyUsages, 
            ShaderGenerator vertexShader, 
            bool isPreview)
        {
            masterNode.GenerateSurfaceOutput(surfaceOutput);
            masterNode.GenerateLightFunction(lightFunction);

            var genMode = isPreview ? GenerationMode.Preview3D : GenerationMode.SurfaceShader;

            var activeNodes = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodes, masterNode);
            var activeMaterialNodes = activeNodes.OfType<AbstractMaterialNode>();

            foreach (var node in activeMaterialNodes)
            {
                if (node is IGeneratesFunction) (node as IGeneratesFunction).GenerateNodeFunction(nodeFunction, genMode);
                if (node is IGeneratesVertexToFragmentBlock) (node as IGeneratesVertexToFragmentBlock).GenerateVertexToFragmentBlock(inputStruct, genMode);
                if (node is IGeneratesVertexShaderBlock) (node as IGeneratesVertexShaderBlock).GenerateVertexShaderBlock(vertexShader, genMode);

                if (node is IGenerateProperties)
                {
                    (node as IGenerateProperties).GeneratePropertyBlock(shaderProperties, genMode);
                    (node as IGenerateProperties).GeneratePropertyUsages(propertyUsages, genMode);
                }
            }

            masterNode.GenerateNodeCode(shaderBody, genMode);
        }

        public static string GenerateSurfaceShader(AbstractMasterNode node, MaterialOptions options, string shaderName, bool isPreview, out List<PropertyGenerator.TextureInfo> configuredTextures)
        {
            var templateLocation = GetTemplatePath("shader.template");

            if (!File.Exists(templateLocation))
            {
                configuredTextures = new List<PropertyGenerator.TextureInfo>();
                return string.Empty;
            }

            var templateText = File.ReadAllText(templateLocation);

            var shaderBodyVisitor = new ShaderGenerator();
            var shaderInputVisitor = new ShaderGenerator();
            var shaderLightFunctionVisitor = new ShaderGenerator();
            var shaderOutputSurfaceVisitor = new ShaderGenerator();
            var shaderFunctionVisitor = new ShaderGenerator();
            var shaderPropertiesVisitor = new PropertyGenerator();
            var shaderPropertyUsagesVisitor = new ShaderGenerator();
            var vertexShaderBlock = new ShaderGenerator();

            GenerateSurfaceShaderInternal(
                node,
                shaderBodyVisitor,
                shaderInputVisitor,
                shaderLightFunctionVisitor,
                shaderOutputSurfaceVisitor,
                shaderFunctionVisitor,
                shaderPropertiesVisitor,
                shaderPropertyUsagesVisitor,
                vertexShaderBlock,
                isPreview);

            if (shaderInputVisitor.numberOfChunks == 0)
            {
                shaderInputVisitor.AddShaderChunk("float4 color : COLOR;", true);
            }

            var tagsVisitor = new ShaderGenerator();
            var blendingVisitor = new ShaderGenerator();
            var cullingVisitor = new ShaderGenerator();
            var zTestVisitor = new ShaderGenerator();
            var zWriteVisitor = new ShaderGenerator();

            options.GetTags(tagsVisitor);
            options.GetBlend(blendingVisitor);
            options.GetCull(cullingVisitor);
            options.GetDepthTest(zTestVisitor);
            options.GetDepthWrite(zWriteVisitor);

            var resultShader = templateText.Replace("${ShaderName}", shaderName);
            resultShader = resultShader.Replace("${ShaderPropertiesHeader}", shaderPropertiesVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ShaderPropertyUsages}", shaderPropertyUsagesVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${LightingFunctionName}", shaderLightFunctionVisitor.GetPragmaString());
            resultShader = resultShader.Replace("${LightingFunction}", shaderLightFunctionVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${SurfaceOutputStructureName}", shaderOutputSurfaceVisitor.GetPragmaString());
            resultShader = resultShader.Replace("${ShaderFunctions}", shaderFunctionVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ShaderInputs}", shaderInputVisitor.GetShaderString(3));
            resultShader = resultShader.Replace("${PixelShaderBody}", shaderBodyVisitor.GetShaderString(3));
            resultShader = resultShader.Replace("${Tags}", tagsVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${Blending}", blendingVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${Culling}", cullingVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ZTest}", zTestVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ZWrite}", zWriteVisitor.GetShaderString(2));

            string vertexShaderBody = vertexShaderBlock.GetShaderString(3);
            if (vertexShaderBody.Length > 0)
            {
                resultShader = resultShader.Replace("${VertexShaderDecl}", "vertex:vert");
                resultShader = resultShader.Replace("${VertexShaderBody}", vertexShaderBody);
            }
            else
            {
                resultShader = resultShader.Replace("${VertexShaderDecl}", "");
                resultShader = resultShader.Replace("${VertexShaderBody}", "");
            }

            configuredTextures = shaderPropertiesVisitor.GetConfiguredTexutres();

            return Regex.Replace(resultShader, @"\r\n|\n\r|\n|\r", Environment.NewLine);
        }

        public int numberOfChunks
        {
            get { return m_ShaderChunks.Count; }
        }
    }
}
