using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    [Title("Math/Add Node")]
    public abstract class AbstractSurfaceMasterNode : AbstractMasterNode
    {
        public const string AlbedoSlotName = "Albedo";
        public const string NormalSlotName = "Normal";
        public const string EmissionSlotName = "Emission";
        public const string SmoothnessSlotName = "Smoothness";
        public const string OcclusionSlotName = "Occlusion";
        public const string AlphaSlotName = "Alpha";

        public const int AlbedoSlotId = 0;
        public const int NormalSlotId = 1;
        public const int EmissionSlotId = 3;
        public const int SmoothnessSlotId = 4;
        public const int OcclusionSlotId = 5;
        public const int AlphaSlotId = 6;

        public abstract string GetSurfaceOutputName();
        public abstract string GetLightFunction();

        public override string GetShader(
            MaterialOptions options,
            GenerationMode mode,
            out List<PropertyGenerator.TextureInfo> configuredTextures)
        {
            var templateLocation = ShaderGenerator.GetTemplatePath("shader.template");

            if (!File.Exists(templateLocation))
            {
                configuredTextures = new List<PropertyGenerator.TextureInfo>();
                return string.Empty;
            }

            var templateText = File.ReadAllText(templateLocation);

            var shaderBodyVisitor = new ShaderGenerator();
            var shaderFunctionVisitor = new ShaderGenerator();
            var shaderPropertiesVisitor = new PropertyGenerator();
            var shaderPropertyUsagesVisitor = new ShaderGenerator();
            var shaderInputVisitor = new ShaderGenerator();
            var vertexShaderBlock = new ShaderGenerator();

            GenerateSurfaceShaderInternal(
                shaderBodyVisitor,
                shaderFunctionVisitor,
                shaderInputVisitor,
                vertexShaderBlock,
                shaderPropertiesVisitor,
                shaderPropertyUsagesVisitor,
                mode);
            
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

            var resultShader = templateText.Replace("${ShaderName}", GetType() + guid.ToString());
            resultShader = resultShader.Replace("${ShaderPropertiesHeader}", shaderPropertiesVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ShaderPropertyUsages}", shaderPropertyUsagesVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${LightingFunctionName}", GetLightFunction());
            resultShader = resultShader.Replace("${SurfaceOutputStructureName}", GetSurfaceOutputName());
            resultShader = resultShader.Replace("${ShaderFunctions}", shaderFunctionVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ShaderInputs}", shaderInputVisitor.GetShaderString(3));
            resultShader = resultShader.Replace("${PixelShaderBody}", shaderBodyVisitor.GetShaderString(3));
            resultShader = resultShader.Replace("${Tags}", tagsVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${Blending}", blendingVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${Culling}", cullingVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ZTest}", zTestVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ZWrite}", zWriteVisitor.GetShaderString(2));

            resultShader = resultShader.Replace("${VertexShaderDecl}", "vertex:vert");
            resultShader = resultShader.Replace("${VertexShaderBody}", vertexShaderBlock.GetShaderString(3));

            configuredTextures = shaderPropertiesVisitor.GetConfiguredTexutres();

            return Regex.Replace(resultShader, @"\r\n|\n\r|\n|\r", Environment.NewLine);

        }

        private void GenerateSurfaceShaderInternal(
           ShaderGenerator shaderBody,
           ShaderGenerator nodeFunction,
           ShaderGenerator shaderInputVisitor,
           ShaderGenerator vertexShaderBlock,
           PropertyGenerator shaderProperties,
           ShaderGenerator propertyUsages,
           GenerationMode mode)
        {
            var activeNodeList = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this);

            foreach (var node in activeNodeList.OfType<AbstractMaterialNode>())
            {
                if (node is IGeneratesFunction) (node as IGeneratesFunction).GenerateNodeFunction(nodeFunction, mode);

                if (node is IGenerateProperties)
                {
                    (node as IGenerateProperties).GeneratePropertyBlock(shaderProperties, mode);
                    (node as IGenerateProperties).GeneratePropertyUsages(propertyUsages, mode);
                }
            }

            // always add color because why not.
            shaderInputVisitor.AddShaderChunk("float4 color : COLOR;", true);

            if (activeNodeList.Any(x => x is IRequiresMeshUV))
            {
                shaderInputVisitor.AddShaderChunk("half4 meshUV0 : TEXCOORD0;", true);
                vertexShaderBlock.AddShaderChunk("o.meshUV0 = v.texcoord;", true);
            }

            if (activeNodeList.Any(x => x is IRequiresViewDirection))
            {
                shaderInputVisitor.AddShaderChunk("float3 viewDir;", true);
                shaderBody.AddShaderChunk("fixed3 worldViewDir = IN.viewDir;", true);
            }
            
            if (activeNodeList.Any(x => x is IRequiresWorldPosition))
            {
                shaderInputVisitor.AddShaderChunk("float3 worldPos;", true);
            }

            if (activeNodeList.Any(x => x is IRequiresScreenPosition))
            {
                shaderInputVisitor.AddShaderChunk("float4 screenPos;", true);
            }

            GenerateNodeCode(shaderBody, mode);
        }

        public void GenerateNodeCode(ShaderGenerator shaderBody, GenerationMode generationMode)
        {
            var firstPassSlotId = NormalSlotId;
            // do the normal slot first so that it can be used later in the shader :)
            var firstPassSlot = FindInputSlot<MaterialSlot>(firstPassSlotId);
            var nodes = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(nodes, this, firstPassSlotId, NodeUtils.IncludeSelf.Exclude);

            for (int index = 0; index < nodes.Count; index++)
            {
                var node = nodes[index];
                if (node is IGeneratesBodyCode)
                    (node as IGeneratesBodyCode).GenerateNodeCode(shaderBody, generationMode);
            }

            foreach (var edge in owner.GetEdges(firstPassSlot.slotReference))
            {
                var outputRef = edge.outputSlot;
                var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
                if (fromNode == null)
                    continue;

                shaderBody.AddShaderChunk("o." + firstPassSlot.shaderOutputName + " = " + fromNode.GetVariableNameForSlot(outputRef.slotId) + ";", true);
            }

            // track the last index of nodes... they have already been processed :)
            int pass2StartIndex = nodes.Count;

            //Get the rest of the nodes for all the other slots
            NodeUtils.DepthFirstCollectNodesFromNode(nodes, this, null, NodeUtils.IncludeSelf.Exclude);
            for (var i = pass2StartIndex; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node is IGeneratesBodyCode)
                    (node as IGeneratesBodyCode).GenerateNodeCode(shaderBody, generationMode);
            }

            ListPool<INode>.Release(nodes);

            foreach (var slot in GetInputSlots<MaterialSlot>())
            {
                foreach (var edge in owner.GetEdges(slot.slotReference))
                {
                    var outputRef = edge.outputSlot;
                    var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
                    if (fromNode == null)
                        continue;

                    shaderBody.AddShaderChunk("o." + slot.shaderOutputName + " = " + fromNode.GetVariableNameForSlot(outputRef.slotId) + ";", true);
                }
            }
        }
    }
}
