using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXCodeGenerator
    {
        public VFXCodeGenerator(string templatePath, bool computeShader = true)
        {
            m_templatePath = templatePath;
            m_computeShader = computeShader;
        }

        public void Build(VFXContext context, StringBuilder stringBuilder, VFXExpressionMapper gpuMapper, ref bool computeShader)
        {
            computeShader = m_computeShader;

            var dependencies = new HashSet<Object>();
            context.CollectDependencies(dependencies);

            foreach (var data in dependencies.OfType<VFXDataParticle>())
            {
                data.DebugBuildAttributeBuffers(); //TMP Debug log
            }

            var templateContent = new StringBuilder(System.IO.File.ReadAllText("Assets/VFXShaders/" + m_templatePath));

            var uniformMapper = new VFXUniformMapper(gpuMapper);
            var cbuffer = new StringBuilder();
            VFXShaderWriter.WriteCBuffer(uniformMapper, cbuffer, "parameters");

            var parameters = new StringBuilder();
            var expressionToName = new Dictionary<VFXExpression, string>();

            foreach (var exp in gpuMapper.expressions)
            {
                if (exp.Is(VFXExpression.Flags.InvalidOnGPU))
                {
                    continue;
                }
                var name = string.Format("param_{0}", VFXCodeGeneratorHelper.GeneratePrefix((uint)expressionToName.Count));
                expressionToName.Add(exp, name);
                VFXShaderWriter.WriteParameter(parameters, exp, uniformMapper, name);
            }

            /* BEGIN TEMP */
            var attributes = dependencies.OfType<VFXDataParticle>().SelectMany(o => o.GetAttributes()).Select(o => o.attrib).Distinct().ToArray();
            foreach (var attribute in attributes)
            {
                var name = string.Format("param_{0}", VFXCodeGeneratorHelper.GeneratePrefix((uint)expressionToName.Count));
                expressionToName.Add(new VFXAttributeExpression(attribute), name);
                parameters.AppendFormat("{0} {1} = ({0})0; //{2}", VFXExpression.TypeToCode(attribute.type), name, attribute.name);
                parameters.AppendLine();
            }
            /* END TEMP */

            //Add dummy attribute if needed (if used in template code)
            foreach (var builtInAttribute in VFXAttribute.All)
            {
                var attributeMarker = string.Format("{{Attribute_{0}}}", builtInAttribute);
                if (templateContent.ToString().Contains(attributeMarker))
                {
                    string name;
                    var attribute = VFXAttribute.Find(builtInAttribute, VFXAttributeLocation.Source);
                    if (expressionToName.Any(o => (o.Key is VFXAttributeExpression) && (o.Key as VFXAttributeExpression).attributeName == builtInAttribute))
                    {
                        name = expressionToName[new VFXAttributeExpression(attribute)];
                    }
                    else
                    {
                        name = string.Format("dummy_for_{0}", builtInAttribute);
                        parameters.AppendFormat("{0} {1} = ({0})0;", VFXExpression.TypeToCode(attribute.type), name);
                    }
                    templateContent.Replace(attributeMarker, name);
                }
            }

            var blockFunction = new StringBuilder();
            foreach (var block in context.GetChildren().GroupBy(o => o.name))
            {
                VFXShaderWriter.WriteBlockFunction(blockFunction, block.First());
            }

            var blockCallFunction = new StringBuilder();
            foreach (var block in context.GetChildren())
            {
                VFXShaderWriter.WriteCallFunction(blockCallFunction, block, expressionToName);
            }

            var globalIncludeContent = new StringBuilder();
            globalIncludeContent.AppendLine("#include \"HLSLSupport.cginc\"");
            globalIncludeContent.AppendLine("#define NB_THREADS_PER_GROUP 256");
            globalIncludeContent.AppendLine(System.IO.File.ReadAllText("Assets/VFXShaders/VFXCommon.cginc"));

            stringBuilder.Append(templateContent);

            stringBuilder.Replace("${VFXGlobalInclude}", globalIncludeContent.ToString());
            stringBuilder.Replace("${VFXCBuffer}", cbuffer.ToString());
            stringBuilder.Replace("${VFXGeneratedBlockFunction}", blockFunction.ToString());

            stringBuilder.Replace("${VFXComputeParameters}", parameters.ToString());
            stringBuilder.Replace("${VFXProcessBlock}", blockCallFunction.ToString());
            stringBuilder.Replace("${WriteAttribute}", "");
        }

        private string m_templatePath;
        private bool m_computeShader;
    }
}
