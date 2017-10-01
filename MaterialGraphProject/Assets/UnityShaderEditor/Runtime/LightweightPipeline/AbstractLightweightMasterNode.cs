using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public abstract class AbstractLightweightMasterNode : MasterNode
    {
        protected abstract IEnumerable<int> masterSurfaceInputs { get; }
        protected abstract IEnumerable<int> masterVertexInputs { get; }
        protected abstract string GetTemplateName();

        protected virtual void GetLightweightDefinesAndRemap(ShaderGenerator defines, ShaderGenerator surfaceOutputRemap)
        {
            foreach (var slot in GetInputSlots<MaterialSlot>())
            {
                var edge = owner.GetEdges(slot.slotReference).FirstOrDefault();
                if (edge == null)
                    continue;

                surfaceOutputRemap.AddShaderChunk(slot.shaderOutputName
                                                  + " = surf."
                                                  + slot.shaderOutputName + ";", true);

            }
        }

        public override string GetSubShader(ShaderGraphRequirements externalGraphRequiements)
        {
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

            var interpolators = new ShaderGenerator();
            var vertexShader = new ShaderGenerator();
            var localPixelShader = new ShaderGenerator();
            var surfaceInputs = new ShaderGenerator();

            ShaderGenerator.GenerateStandardTransforms(
                GetInterpolatorStartIndex(),
                interpolators,
                vertexShader,
                localPixelShader,
                surfaceInputs,
                externalGraphRequiements,
                GetNodeSpecificRequirements());

            ShaderGenerator defines = new ShaderGenerator();
            ShaderGenerator surfaceOutputRemap = new ShaderGenerator();
            GetLightweightDefinesAndRemap(defines, surfaceOutputRemap);

            var templateLocation = ShaderGenerator.GetTemplatePath(GetTemplateName());

            if (!File.Exists(templateLocation))
                return string.Empty;

            var subShaderTemplate = File.ReadAllText(templateLocation);
            var resultShader = subShaderTemplate.Replace("${Defines}", defines.GetShaderString(3));
            resultShader = resultShader.Replace("${Interpolators}", interpolators.GetShaderString(3));
            resultShader = resultShader.Replace("${VertexShader}", vertexShader.GetShaderString(3));
            resultShader = resultShader.Replace("${LocalPixelShader}", localPixelShader.GetShaderString(3));
            resultShader = resultShader.Replace("${SurfaceInputs}", surfaceInputs.GetShaderString(3));
            resultShader = resultShader.Replace("${SurfaceOutputRemap}", surfaceOutputRemap.GetShaderString(3));

            resultShader = resultShader.Replace("${Tags}", tagsVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${Blending}", blendingVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${Culling}", cullingVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ZTest}", zTestVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ZWrite}", zWriteVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${LOD}", "" + m_MaterialOptions.lod);
            return resultShader;
        }

        protected abstract int GetInterpolatorStartIndex();
    }
}
