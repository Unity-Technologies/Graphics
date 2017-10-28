/*using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public abstract class ProjectSpecificMasterNode : AbstractMasterNode
    {
        public override string GetSubShader(GenerationMode mode)
        {

        }

        public override string GetFullShader(
            GenerationMode mode,
            out List<PropertyGenerator.TextureInfo> configuredTextures)
        {
            var shaderTemplateLocation = ShaderGenerator.GetTemplatePath("shader.template");

            if (!File.Exists(shaderTemplateLocation))
            {
                configuredTextures = new List<PropertyGenerator.TextureInfo>();
                return string.Empty;
            }

            var templateText = File.ReadAllText(shaderTemplateLocation);

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

            m_MaterialOptions.GetTags(tagsVisitor);
            m_MaterialOptions.GetBlend(blendingVisitor);
            m_MaterialOptions.GetCull(cullingVisitor);
            m_MaterialOptions.GetDepthTest(zTestVisitor);
            m_MaterialOptions.GetDepthWrite(zWriteVisitor);

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
           ShaderGenerator propertyUsages,
           GenerationMode mode)
        {
            var activeNodeList = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this);

            foreach (var node in activeNodeList.OfType<AbstractMaterialNode>())
            {
                node.GeneratePropertyUsages(propertyUsages, mode);
            }

        }
    }
}*/
