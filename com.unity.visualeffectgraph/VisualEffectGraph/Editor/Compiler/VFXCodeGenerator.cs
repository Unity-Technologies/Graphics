using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;
namespace UnityEditor.VFX
{
    class VFXCodeGenerator
    {
        public enum CompilationMode
        {
            Debug,
            Runtime
        }

        public VFXCodeGenerator(string templatePath, bool computeShader = true)
        {
            m_templateName = templatePath;
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

        public void Build(VFXContext context, CompilationMode[] modes, StringBuilder[] stringBuilders, VFXExpressionMapper gpuMapper, ref bool computeShader)
        {
            computeShader = m_computeShader;
            var fallbackTemplate = string.Format("Assets/VFXShaders/{0}.template", m_templateName);
            var processedFile = new Dictionary<string, StringBuilder>();
            for (int i = 0; i < modes.Length; ++i)
            {
                var mode = modes[i];
                var currentTemplate = string.Format("Assets/VFXShaders/{0}_{1}.template", m_templateName, mode.ToString().ToLower());
                if (!System.IO.File.Exists(currentTemplate))
                {
                    currentTemplate = fallbackTemplate;
                }

                if (processedFile.ContainsKey(currentTemplate))
                {
                    stringBuilders[i] = new StringBuilder(processedFile[currentTemplate].ToString());
                }
                else
                {
                    Build(context, currentTemplate, stringBuilders[i], gpuMapper);
                    processedFile.Add(currentTemplate, stringBuilders[i]);
                }
            }
        }

        private void Build(VFXContext context, string templatePath, StringBuilder stringBuilder, VFXExpressionMapper gpuMapper)
        {
            var dependencies = new HashSet<Object>();
            context.CollectDependencies(dependencies);
            foreach (var data in dependencies.OfType<VFXDataParticle>())
            {
                data.DebugBuildAttributeBuffers(); //TMP Debug log
            }

            var templateContent = new StringBuilder(System.IO.File.ReadAllText(templatePath));

            var uniformMapper = new VFXUniformMapper(gpuMapper);
            var cbuffer = new StringBuilder();
            VFXShaderWriter.WriteCBuffer(cbuffer, uniformMapper, "parameters");
            VFXShaderWriter.WriteTexture(cbuffer, uniformMapper);

            var parameters = new StringBuilder();
            var expressionToName = new Dictionary<VFXExpression, string>();

            var attributesFromContext = context.GetData().GetAttributes().ToArray();
            Func<VFXAttributeLocation, string, string> fnAttributeMarker = delegate(VFXAttributeLocation location, string name)
                {
                    return string.Format("${{Attribute_{0}_{1}}}", location == VFXAttributeLocation.Current ? "Current" : "Source", name);
                };

            Func<VFXAttributeLocation, VFXAttributeInfo[]> fnCollectAttributeFromTemplate = delegate(VFXAttributeLocation location)
                {
                    return VFXAttribute.AllAttribute.Where(o =>
                    {
                        var attributeMarker = fnAttributeMarker(location, o.name);
                        return templateContent.ToString().Contains(attributeMarker);
                    }).Select(o => new VFXAttributeInfo(o, VFXAttributeMode.Read)).ToArray();
                };
            var implicitAttributeSource = fnCollectAttributeFromTemplate(VFXAttributeLocation.Source);
            var implicitAttributeCurrent = fnCollectAttributeFromTemplate(VFXAttributeLocation.Current);
            var attributes = attributesFromContext.Concat(implicitAttributeSource).Concat(implicitAttributeCurrent).Distinct().ToArray();

            var attributesSource = attributes.Where(o => o.attrib.location == VFXAttributeLocation.Source).ToArray();
            var attributesCurrent = attributes.Where(o => o.attrib.location == VFXAttributeLocation.Current).ToArray();

            //< Attribute source
            foreach (var attribute in attributesSource.Select(o => o.attrib))
            {
                VFXShaderWriter.WriteVariable(parameters, attribute.type, attribute.name, "0", "Temp, should extract parameters from attribute buffer here");
                expressionToName.Add(new VFXAttributeExpression(attribute), attribute.name);
            }

            //< Attribute current which except a default source
            foreach (var attribute in attributesCurrent.Where(c => !attributesSource.Any(s => s.attrib.name == c.attrib.name)).Select(o => o.attrib))
            {
                if (!attribute.value.Is(VFXExpression.Flags.Constant))
                {
                    throw new Exception(string.Format("Attribute expects constant default value"));
                }

                VFXShaderWriter.WriteParameter(parameters, attribute.value, uniformMapper, attribute.name);
                expressionToName.Add(new VFXAttributeExpression(new VFXAttribute(attribute.name, attribute.value, VFXAttributeLocation.Source)), attribute.name);
            }

            //< Current Attribute
            foreach (var attribute in attributes.Where(o => o.attrib.location == VFXAttributeLocation.Current).Select(o => o.attrib))
            {
                var name = string.Format("current_{0}_{1}", attribute.name, VFXCodeGeneratorHelper.GeneratePrefix((uint)expressionToName.Count));
                expressionToName.Add(new VFXAttributeExpression(attribute), name);
                VFXShaderWriter.WriteVariable(parameters, attribute.type, name, attribute.name);
            }

            //< Replace parameters in template code
            foreach (var implicitAttribute in implicitAttributeCurrent.Concat(implicitAttributeSource).Select(o => o.attrib))
            {
                var attributeMarker = fnAttributeMarker(implicitAttribute.location, implicitAttribute.name);
                templateContent.Replace(attributeMarker, expressionToName[new VFXAttributeExpression(implicitAttribute)]);
            }

            //< Block processor
            var blockFunction = new StringBuilder();
            var blockCallFunction = new StringBuilder();
            var blockDeclared = new HashSet<string>();
            foreach (var block in context.children)
            {
                var expressionParameter = new List<VFXExpression>();
                var nameParameter = new List<string>();
                var modeParameter = new List<VFXAttributeMode>();
                foreach (var attribute in block.attributes)
                {
                    expressionParameter.Add(new VFXAttributeExpression(attribute.attrib));
                    nameParameter.Add(attribute.attrib.name);
                    modeParameter.Add(attribute.mode);
                }

                foreach (var parameter in block.parameters)
                {
                    var expReduced = gpuMapper.FromNameAndId(parameter.name, block.GetParent().GetIndex(block));
                    if (!expReduced.Is(VFXExpression.Flags.InvalidOnGPU))
                    {
                        expressionParameter.Add(expReduced);
                        nameParameter.Add(parameter.name);
                        modeParameter.Add(VFXAttributeMode.None);
                    }
                }

                if (!blockDeclared.Contains(block.name))
                {
                    blockDeclared.Add(block.name);
                    VFXShaderWriter.WriteBlockFunction(blockFunction, gpuMapper, block.name, block.source, expressionParameter, nameParameter, modeParameter);
                }

                //< Parameters (computed and/or extracted from uniform)
                var expressionToNameLocal = new Dictionary<VFXExpression, string>(expressionToName);
                {
                    var parameterCompute = new StringBuilder();
                    foreach (var exp in expressionParameter)
                    {
                        if (expressionToNameLocal.ContainsKey(exp))
                        {
                            continue;
                        }
                        VFXShaderWriter.WriteVariable(parameterCompute, exp, expressionToNameLocal, uniformMapper);
                    }

                    blockCallFunction.Append("{\n\t${tempParameterCompute}\n\t");
                    VFXShaderWriter.WriteCallFunction(blockCallFunction, block.name, expressionParameter, nameParameter, modeParameter, gpuMapper, expressionToNameLocal);
                    blockCallFunction.AppendLine("}");
                    ReplaceMultiline(blockCallFunction, "${tempParameterCompute}", parameterCompute);
                }
            }

            //< Final composition
            var globalIncludeContent = new StringBuilder();
            globalIncludeContent.AppendLine("#include \"HLSLSupport.cginc\"");
            globalIncludeContent.AppendLine("#define NB_THREADS_PER_GROUP 256");
            foreach (var attribute in attributes.Select(o => o.attrib))
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

        private string m_templateName;
        private bool m_computeShader;
    }
}
