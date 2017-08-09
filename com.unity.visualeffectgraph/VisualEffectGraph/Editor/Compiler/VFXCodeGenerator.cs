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

        //This function insure to keep padding while replacing a specific string
        private static void ReplaceMultiline(StringBuilder target, string targetQuery, StringBuilder value)
        {
            string[] delim = { System.Environment.NewLine, "\n" };
            var valueLines = value.ToString().Split(delim, System.StringSplitOptions.None);
            if (valueLines.Length <= 1)
            {
                target.Replace(targetQuery, value.ToString());
            }
            else
            {
                while (true)
                {
                    var targetCopy = target.ToString();
                    var index = targetCopy.IndexOf(targetQuery);
                    if (index == -1)
                    {
                        break;
                    }

                    var padding = "";
                    index--;
                    while (index > 0 && (targetCopy[index] == ' ' || targetCopy[index] == '\t'))
                    {
                        padding = targetCopy[index] + padding;
                        index--;
                    }

                    var currentValue = new StringBuilder();
                    foreach (var line in valueLines)
                    {
                        currentValue.AppendLine(padding + line);
                    }
                    target.Replace(padding + targetQuery, currentValue.ToString());
                }
            }
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

            var attributes = dependencies.OfType<VFXDataParticle>().SelectMany(o => o.GetAttributes()).Select(o => o.attrib).Distinct().ToArray();
            /* BEGIN TEMP */
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
            foreach (var attribute in attributes)
            {
                globalIncludeContent.AppendFormat("#define VFX_USE_{0}_{1} 1", attribute.name.ToUpper(), attribute.location == VFXAttributeLocation.Current ? "CURRENT" : "SOURCE");
                globalIncludeContent.AppendLine();
            }

            globalIncludeContent.AppendLine(System.IO.File.ReadAllText("Assets/VFXShaders/VFXCommon.cginc"));

            stringBuilder.Append(templateContent);

            ReplaceMultiline(stringBuilder, "${VFXGlobalInclude}", globalIncludeContent);
            ReplaceMultiline(stringBuilder, "${VFXCBuffer}", cbuffer);
            ReplaceMultiline(stringBuilder, "${VFXGeneratedBlockFunction}", blockFunction);

            ReplaceMultiline(stringBuilder, "${VFXComputeParameters}", parameters);
            ReplaceMultiline(stringBuilder, "${VFXProcessBlock}", blockCallFunction);
            ReplaceMultiline(stringBuilder, "${WriteAttribute}", new StringBuilder(""));

            Debug.LogFormat("GENERATED_OUTPUT_FILE_FOR : {0}\n{1}", context.ToString(), stringBuilder.ToString());
        }

        private string m_templatePath;
        private bool m_computeShader;
    }
}
