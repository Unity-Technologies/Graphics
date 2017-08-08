using System.Text;
using System.Linq;

namespace UnityEditor.VFX
{
    class VFXCodeGenerator
    {
        public VFXCodeGenerator(string templatePath, bool computeShader = true)
        {
            m_templatePath = templatePath;
            m_computeShader = computeShader;
        }

        public void Build(StringBuilder stringBuilder, VFXExpressionMapper gpuMapper, ref bool computeShader)
        {
            var uniformMapper = new VFXUniformMapper(gpuMapper);
            var cbuffer = new StringBuilder();
            VFXShaderWriter.WriteCBuffer(uniformMapper, cbuffer, "uniform");

            var parameters = new StringBuilder();
            uint prefixIndex = 0u;
            foreach (var exp in gpuMapper.expressions)
            {
                if (exp.Is(VFXExpression.Flags.InvalidOnGPU))
                {
                    continue;
                }
                VFXShaderWriter.WriteParameter(parameters, exp, uniformMapper, string.Format("param_{0}", VFXCodeGeneratorHelper.GeneratePrefix(prefixIndex++)));
            }

            computeShader = m_computeShader;
            var globalIncludeContent = System.IO.File.ReadAllText("Assets/VFXShaders/VFXCommon.cginc");

            var templateContent = System.IO.File.ReadAllText(string.Format("Assets/VFXShaders/{0}", m_templatePath));
            stringBuilder.Append(templateContent);

            stringBuilder.Replace("${VFXGlobalInclude}", globalIncludeContent);
            stringBuilder.Replace("${VFXCBuffer}", cbuffer.ToString());

            stringBuilder.Replace("${VFXComputeParameters}", parameters.ToString());
            stringBuilder.Replace("${VFXProcessBlock}", "");
        }

        private string m_templatePath;
        private bool m_computeShader;
    }
}
