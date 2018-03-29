using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

using Object = UnityEngine.Object;
using System.Text.RegularExpressions;

namespace UnityEditor.VFX
{
    static class VFXCodeGenerator
    {
        public enum CompilationMode
        {
            Debug,
            Runtime
        }

        private static string GetIndent(string src, int index)
        {
            var indent = "";
            index--;
            while (index > 0 && (src[index] == ' ' || src[index] == '\t'))
            {
                indent = src[index] + indent;
                index--;
            }
            return indent;
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

                    var indent = GetIndent(targetCopy, index);
                    var currentValue = new StringBuilder();
                    foreach (var line in valueLines)
                    {
                        currentValue.AppendLine(indent + line);
                    }
                    target.Replace(indent + targetQuery, currentValue.ToString());
                }
            }
        }

        static private VFXShaderWriter GenerateLoadAttribute(string matching, VFXContext context)
        {
            var r = new VFXShaderWriter();

            var regex = new Regex(matching);
            var attributesFromContext = context.GetData().GetAttributes().Where(o => regex.IsMatch(o.attrib.name)).ToArray();
            var attributesSource = attributesFromContext.Where(o => (o.mode & VFXAttributeMode.ReadSource) != 0).ToArray();
            var attributesCurrent = attributesFromContext.Where(o => o.mode != VFXAttributeMode.ReadSource).Where(a => context.GetData().IsAttributeUsed(a.attrib, context) || (context.contextType == VFXContextType.kInit && context.GetData().IsAttributeStored(a.attrib))).ToArray();

            //< Current Attribute
            foreach (var attribute in attributesCurrent.Select(o => o.attrib))
            {
                var name = attribute.name;
                if (context.contextType != VFXContextType.kInit && context.GetData().IsAttributeStored(attribute))
                {
                    r.WriteVariable(attribute.type, name, context.GetData().GetLoadAttributeCode(attribute, VFXAttributeLocation.Current));
                }
                else
                {
                    r.WriteVariable(attribute.type, name, attribute.value.GetCodeString(null));
                }
                r.WriteLine();
            }

            //< Source Attribute (default temporary behavior, source is always the initial current value)
            foreach (var attribute in attributesSource.Select(o => o.attrib))
            {
                var name = string.Format("{0}_source", attribute.name);
                if (context.contextType == VFXContextType.kInit)
                {
                    r.WriteVariable(attribute.type, name, context.GetData().GetLoadAttributeCode(attribute, VFXAttributeLocation.Source));
                }
                else
                {
                    if (attributesCurrent.Any(o => o.attrib.name == attribute.name))
                    {
                        var reference = new VFXAttributeExpression(new VFXAttribute(attribute.name, attribute.value), VFXAttributeLocation.Current);
                        r.WriteVariable(reference.valueType, name, reference.GetCodeString(null));
                    }
                    else
                    {
                        r.WriteVariable(attribute.type, name, attribute.value.GetCodeString(null));
                    }
                }
                r.WriteLine();
            }
            return r;
        }

        static private StringBuilder GenerateStoreAttribute(string matching, VFXContext context)
        {
            var r = new StringBuilder();
            var regex = new Regex(matching);

            var attributesFromContext = context.GetData().GetAttributes().Where(o => regex.IsMatch(o.attrib.name) &&
                    context.GetData().IsAttributeStored(o.attrib) &&
                    (context.contextType == VFXContextType.kInit || context.GetData().IsCurrentAttributeWritten(o.attrib, context))).ToArray();

            foreach (var attribute in attributesFromContext.Select(o => o.attrib))
            {
                r.Append(context.GetData().GetStoreAttributeCode(attribute, new VFXAttributeExpression(attribute).GetCodeString(null)));
                r.AppendLine(";");
            }
            return r;
        }

        static private VFXShaderWriter GenerateLoadParameter(string matching, VFXNamedExpression[] namedExpressions, Dictionary<VFXExpression, string> expressionToName)
        {
            var r = new VFXShaderWriter();
            var regex = new Regex(matching);

            var filteredNamedExpressions = namedExpressions.Where(o => regex.IsMatch(o.name) &&
                    !(expressionToName.ContainsKey(o.exp) && expressionToName[o.exp] == o.name)); // if parameter already in the global scope, there's nothing to do

            bool needScope = false;
            foreach (var namedExpression in filteredNamedExpressions)
            {
                r.WriteVariable(namedExpression.exp.valueType, namedExpression.name, "0");
                r.WriteLine();
                needScope = true;
            }

            if (needScope)
            {
                var expressionToNameLocal = new Dictionary<VFXExpression, string>(expressionToName);
                r.EnterScope();
                foreach (var namedExpression in filteredNamedExpressions)
                {
                    if (!expressionToNameLocal.ContainsKey(namedExpression.exp))
                    {
                        r.WriteVariable(namedExpression.exp, expressionToNameLocal);
                        r.WriteLine();
                    }
                    r.WriteAssignement(namedExpression.exp.valueType, namedExpression.name, expressionToNameLocal[namedExpression.exp]);
                    r.WriteLine();
                }
                r.ExitScope();
            }

            return r;
        }

        static public void Build(VFXContext context, CompilationMode[] modes, StringBuilder[] stringBuilders, VFXContextCompiledData contextData, string templateName)
        {
            var fallbackTemplate = string.Format("Assets/{0}.template", templateName);
            var processedFile = new Dictionary<string, StringBuilder>();
            for (int i = 0; i < modes.Length; ++i)
            {
                var mode = modes[i];
                var currentTemplate = string.Format("Assets/{0}_{1}.template", templateName, mode.ToString().ToLower());
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
                    Build(context, currentTemplate, stringBuilders[i], contextData);
                    processedFile.Add(currentTemplate, stringBuilders[i]);
                }
            }
        }

        static private void GetFunctionName(VFXBlock block, out string functionName, out string comment)
        {
            var settings = block.GetSettings(true).ToArray();
            if (settings.Length > 0)
            {
                comment = "";
                int hash = 0;
                foreach (var setting in settings)
                {
                    var value = setting.GetValue(block);
                    hash = (hash * 397) ^ value.GetHashCode();
                    comment += string.Format("{0}:{1} ", setting.Name, value.ToString());
                }
                functionName = string.Format("{0}_{1}", block.GetType().Name, hash.ToString("X"));
            }
            else
            {
                comment = null;
                functionName = block.GetType().Name;
            }
        }

        static private string FormatPath(string path)
        {
            return Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .ToLowerInvariant();
        }

        static IEnumerable<Match> GetUniqueMatches(string regexStr, string src)
        {
            var regex = new Regex(regexStr);
            var matches = regex.Matches(src);
            return matches.Cast<Match>().GroupBy(m => m.Groups[0].Value).Select(g => g.First());
        }

        static private VFXShaderWriter GenerateComputeSourceIndex(VFXContext context)
        {
            var r = new VFXShaderWriter();
            var spawnLinkCount = context.GetData().sourceCount;
            r.WriteLine("int sourceIndex = 0;");

            if (spawnLinkCount <= 1)
                r.WriteLine("/*//Loop with 1 iteration generate a wrong IL Assembly (and actually, useless code)");

            r.WriteLine("uint currentSumSpawnCount = 0u;");
            r.WriteLineFormat("for (sourceIndex=0; sourceIndex<{0}; sourceIndex++)", spawnLinkCount);
            r.EnterScope();
            r.WriteLineFormat("currentSumSpawnCount += uint({0});", context.GetData().GetLoadAttributeCode(new VFXAttribute("spawnCount", VFXValueType.Float), VFXAttributeLocation.Source));
            r.WriteLine("if (id.x < currentSumSpawnCount)");
            r.EnterScope();
            r.WriteLine("break;");
            r.ExitScope();
            r.ExitScope();

            if (spawnLinkCount <= 1)
                r.WriteLine("*/");
            return r;
        }

        static private StringBuilder GetFlattenedTemplateContent(string path, List<string> includes, IEnumerable<string> defines)
        {
            var formattedPath = FormatPath(path);

            if (includes.Contains(formattedPath))
            {
                var includeHierarchy = new StringBuilder(string.Format("Cyclic VFXInclude dependency detected: {0}\n", formattedPath));
                foreach (var str in Enumerable.Reverse<string>(includes))
                    includeHierarchy.AppendLine(str);
                throw new InvalidOperationException(includeHierarchy.ToString());
            }

            includes.Add(formattedPath);
            var templateContent = new StringBuilder(System.IO.File.ReadAllText(formattedPath));

            foreach (var match in GetUniqueMatches("\\${VFXInclude\\(\\\"(.*?)\\\"\\)(,.*)?}", templateContent.ToString()))
            {
                var groups = match.Groups;
                var includePath = groups[1].Value;

                if (groups.Count > 2 && !String.IsNullOrEmpty(groups[2].Value))
                {
                    var allDefines = groups[2].Value.Split(new char[] {',', ' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
                    var neededDefines = allDefines.Where(d => d[0] != '!');
                    var forbiddenDefines = allDefines.Except(neededDefines).Select(d => d.Substring(1));
                    if (!neededDefines.All(d => defines.Contains(d)) || forbiddenDefines.Any(d => defines.Contains(d)))
                    {
                        ReplaceMultiline(templateContent, groups[0].Value, new StringBuilder());
                        continue;
                    }
                }

                var includeBuilder = GetFlattenedTemplateContent(includePath, includes, defines);
                ReplaceMultiline(templateContent, groups[0].Value, includeBuilder);
            }

            includes.Remove(formattedPath);
            return templateContent;
        }

        static private void SubstituteMacros(StringBuilder builder)
        {
            var definesToCode = new Dictionary<string, string>();
            var source = builder.ToString();
            Regex beginRegex = new Regex("\\${VFXBegin:(.*)}");

            int currentPos = -1;
            int builderOffset = 0;
            while ((currentPos = source.IndexOf("${")) != -1)
            {
                int endPos = source.IndexOf('}', currentPos);
                if (endPos == -1)
                    throw new FormatException("Ill-formed VFX tag (Missing closing brace");

                var tag = source.Substring(currentPos, endPos - currentPos + 1);
                // Replace any tag found
                if (definesToCode.ContainsKey(tag))
                {
                    var macro = definesToCode[tag];
                    builder.Remove(currentPos + builderOffset, tag.Length);
                    var indentedMacro = macro.Replace("\n", "\n" + GetIndent(source, currentPos));
                    builder.Insert(currentPos + builderOffset, indentedMacro);
                }
                else
                {
                    const string endStr = "${VFXEnd}";
                    var match = beginRegex.Match(source, currentPos, tag.Length);
                    if (match.Success)
                    {
                        var macroStartPos = match.Index + match.Length;
                        var macroEndCodePos = source.IndexOf(endStr, macroStartPos);
                        if (macroEndCodePos == -1)
                            throw new FormatException("${VFXBegin} found without ${VFXEnd}");

                        var defineStr = "${" + match.Groups[1].Value + "}";
                        definesToCode[defineStr] = source.Substring(macroStartPos, macroEndCodePos - macroStartPos);

                        // Remove the define in builder
                        builder.Remove(match.Index + builderOffset, macroEndCodePos - match.Index + endStr.Length);
                    }
                    else if (tag == endStr)
                        throw new FormatException("${VFXEnd} found without ${VFXBegin}");
                    else // Remove undefined tag
                        builder.Remove(currentPos + builderOffset, tag.Length);
                }

                builderOffset += currentPos;
                source = builder.ToString(builderOffset, builder.Length - builderOffset);
            }
        }

        static private void Build(VFXContext context, string templatePath, StringBuilder stringBuilder, VFXContextCompiledData contextData)
        {
            var dependencies = new HashSet<ScriptableObject>();
            context.CollectDependencies(dependencies);

            var templateContent = GetFlattenedTemplateContent(templatePath, new List<string>(), context.additionalDefines);

            var globalDeclaration = new VFXShaderWriter();
            globalDeclaration.WriteCBuffer(contextData.uniformMapper, "parameters");
            globalDeclaration.WriteTexture(contextData.uniformMapper);

            //< Block processor
            var blockFunction = new VFXShaderWriter();
            var blockCallFunction = new VFXShaderWriter();
            var blockDeclared = new HashSet<string>();
            var expressionToName = context.GetData().GetAttributes().ToDictionary(o => new VFXAttributeExpression(o.attrib) as VFXExpression, o => (new VFXAttributeExpression(o.attrib)).GetCodeString(null));
            expressionToName = expressionToName.Union(contextData.uniformMapper.expressionToCode).ToDictionary(s => s.Key, s => s.Value);

            foreach (var current in context.activeChildrenWithImplicit.Select((v, i) => new { block = v, blockIndex = i }))
            {
                var block = current.block;
                var blockIndex = current.blockIndex;

                var parameters = block.attributes.Select(o =>
                    {
                        return new
                        {
                            name = o.attrib.name,
                            expression = new VFXAttributeExpression(o.attrib) as VFXExpression,
                            mode = o.mode
                        };
                    }).ToList();

                foreach (var parameter in block.parameters)
                {
                    var expReduced = contextData.gpuMapper.FromNameAndId(parameter.name, blockIndex);
                    if (VFXExpression.IsTypeValidOnGPU(expReduced.valueType))
                    {
                        parameters.Add(new
                        {
                            name = parameter.name,
                            expression = expReduced,
                            mode = VFXAttributeMode.None
                        });
                    }
                }

                string methodName, commentMethod;
                GetFunctionName(block, out methodName, out commentMethod);
                if (!blockDeclared.Contains(methodName))
                {
                    blockDeclared.Add(methodName);
                    blockFunction.WriteBlockFunction(contextData.gpuMapper,
                        methodName,
                        block.source,
                        parameters.Select(o => o.expression).ToArray(),
                        parameters.Select(o => o.name).ToArray(),
                        parameters.Select(o => o.mode).ToArray(),
                        commentMethod);
                }

                //< Parameters (computed and/or extracted from uniform)
                var expressionToNameLocal = expressionToName;
                bool needScope = parameters.Any(o => !expressionToNameLocal.ContainsKey(o.expression));
                if (needScope)
                {
                    expressionToNameLocal = new Dictionary<VFXExpression, string>(expressionToNameLocal);
                    blockCallFunction.EnterScope();
                    foreach (var exp in parameters.Select(o => o.expression))
                    {
                        if (expressionToNameLocal.ContainsKey(exp))
                        {
                            continue;
                        }
                        blockCallFunction.WriteVariable(exp, expressionToNameLocal);
                    }
                }

                blockCallFunction.WriteCallFunction(methodName,
                    parameters.Select(o => o.expression).ToArray(),
                    parameters.Select(o => o.name).ToArray(),
                    parameters.Select(o => o.mode).ToArray(),
                    contextData.gpuMapper,
                    expressionToNameLocal);

                if (needScope)
                    blockCallFunction.ExitScope();
            }

            //< Final composition
            var globalIncludeContent = new VFXShaderWriter();
            globalIncludeContent.WriteLine("#include \"HLSLSupport.cginc\"");
            globalIncludeContent.WriteLine("#define NB_THREADS_PER_GROUP 64");
            foreach (var attribute in context.GetData().GetAttributes().Where(a => (context.contextType == VFXContextType.kInit && context.GetData().IsAttributeStored(a.attrib)) || (context.GetData().IsAttributeUsed(a.attrib, context))))
                globalIncludeContent.WriteLineFormat("#define VFX_USE_{0}_{1} 1", attribute.attrib.name.ToUpper(), "CURRENT");
            foreach (var attribute in context.GetData().GetAttributes().Where(a => context.GetData().IsSourceAttributeUsed(a.attrib, context)))
                globalIncludeContent.WriteLineFormat("#define VFX_USE_{0}_{1} 1", attribute.attrib.name.ToUpper(), "SOURCE");

            foreach (var additionnalDefine in context.additionalDefines)
                globalIncludeContent.WriteLineFormat("#define {0} 1", additionnalDefine);

            var renderPipePath = VFXManager.renderPipeSettingsPath;

            string renderPipeCommon = "Assets/VFXShaders/Common/VFXCommonCompute.cginc";
            string renderPipePasses = null;
            if (!context.codeGeneratorCompute && !string.IsNullOrEmpty(renderPipePath))
            {
                renderPipeCommon = "Assets/" + renderPipePath + "/VFXCommon.cginc";
                renderPipePasses = "Assets/" + renderPipePath + "/VFXPasses.template";
            }

            globalIncludeContent.WriteLine();
            if (renderPipePasses != null)
                globalIncludeContent.Write(GetFlattenedTemplateContent(renderPipePasses, new List<string>(), context.additionalDefines));
            globalIncludeContent.WriteLine("#include \"" + renderPipeCommon + "\"");

            if (context.GetData() is ISpaceable)
            {
                var spaceable = context.GetData() as ISpaceable;
                globalIncludeContent.WriteLineFormat("#define {0} 1", spaceable.space == CoordinateSpace.Global ? "VFX_WORLD_SPACE" : "VFX_LOCAL_SPACE");
            }

            globalIncludeContent.WriteLine("#include \"Assets/VFXShaders/VFXCommon.cginc\"");

            // Per-block includes
            var includes = Enumerable.Empty<string>();
            foreach (var block in context.activeChildrenWithImplicit)
                includes = includes.Concat(block.includes);
            var uniqueIncludes = new HashSet<string>(includes);
            foreach (var includePath in uniqueIncludes)
                globalIncludeContent.WriteLine(string.Format("#include \"{0}\"", includePath));

            stringBuilder.Append(templateContent);

            ReplaceMultiline(stringBuilder, "${VFXGlobalInclude}", globalIncludeContent.builder);
            ReplaceMultiline(stringBuilder, "${VFXGlobalDeclaration}", globalDeclaration.builder);
            ReplaceMultiline(stringBuilder, "${VFXGeneratedBlockFunction}", blockFunction.builder);
            ReplaceMultiline(stringBuilder, "${VFXProcessBlocks}", blockCallFunction.builder);

            var mainParameters = contextData.gpuMapper.CollectExpression(-1).ToArray();
            foreach (var match in GetUniqueMatches("\\${VFXLoadParameter:{(.*?)}}", stringBuilder.ToString()))
            {
                var str = match.Groups[0].Value;
                var pattern = match.Groups[1].Value;
                var loadParameters = GenerateLoadParameter(pattern, mainParameters, expressionToName);
                ReplaceMultiline(stringBuilder, str, loadParameters.builder);
            }

            //< Compute sourceIndex
            if (stringBuilder.ToString().Contains("${VFXComputeSourceIndex}"))
            {
                var r = GenerateComputeSourceIndex(context);
                ReplaceMultiline(stringBuilder, "${VFXComputeSourceIndex}", r.builder);
            }

            //< Load Attribute
            if (stringBuilder.ToString().Contains("${VFXLoadAttributes}"))
            {
                var loadAttributes = GenerateLoadAttribute(".*", context);
                ReplaceMultiline(stringBuilder, "${VFXLoadAttributes}", loadAttributes.builder);
            }

            foreach (var match in GetUniqueMatches("\\${VFXLoadAttributes:{(.*?)}}", stringBuilder.ToString()))
            {
                var str = match.Groups[0].Value;
                var pattern = match.Groups[1].Value;
                var loadAttributes = GenerateLoadAttribute(pattern, context);
                ReplaceMultiline(stringBuilder, str, loadAttributes.builder);
            }

            //< Store Attribute
            if (stringBuilder.ToString().Contains("${VFXStoreAttributes}"))
            {
                var storeAttribute = GenerateStoreAttribute(".*", context);
                ReplaceMultiline(stringBuilder, "${VFXStoreAttributes}", storeAttribute);
            }

            foreach (var match in GetUniqueMatches("\\${VFXStoreAttributes:{(.*?)}}", stringBuilder.ToString()))
            {
                var str = match.Groups[0].Value;
                var pattern = match.Groups[1].Value;
                var storeAttributes = GenerateStoreAttribute(pattern, context);
                ReplaceMultiline(stringBuilder, str, storeAttributes);
            }

            foreach (var addionalReplacement in context.additionalReplacements)
            {
                ReplaceMultiline(stringBuilder, addionalReplacement.Key, addionalReplacement.Value.builder);
            }

            // Replace defines
            SubstituteMacros(stringBuilder);

            Debug.LogFormat("GENERATED_OUTPUT_FILE_FOR : {0}\n{1}", context.ToString(), stringBuilder.ToString());
        }
    }
}
