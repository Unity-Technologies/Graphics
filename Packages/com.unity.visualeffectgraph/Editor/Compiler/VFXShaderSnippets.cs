using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UnityEditor.VFX
{
    internal class VFXShaderSnippets
    {
        internal static StringBuilder GenerateShaderCode(string templatePath, ShaderGenerationData shaderGenerationData)
        {
            VFXSnippetNodeInclude rootNode = new VFXSnippetNodeInclude(templatePath, shaderGenerationData, "");
            StringBuilder shaderStringSb = new StringBuilder();
            rootNode.CollectChildren();
            rootNode.AppendContent(shaderStringSb);
            return shaderStringSb;
        }

        //This function insure to keep padding while replacing a specific string
        private static string GetIndent(string src, int index)
        {
            int indentLength = 0;
            index--;
            while (index > 0 && (src[index] == ' ' || src[index] == '\t'))
            {
                index--;
                indentLength++;
            }

            if (indentLength > 0)
                return src.Substring(index + 1, indentLength);
            return string.Empty;
        }

        private static string FormatPath(string path)
        {
            return Path.GetFullPath(path)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
#if !UNITY_EDITOR_LINUX
                    .ToLowerInvariant()
#endif
                ;
        }

        internal class ShaderGenerationData
        {
            internal Dictionary<VFXExpression, string> m_ExpressionToName;
            internal VFXNamedExpression[] m_MainParameters;
            internal VFXContext m_Context;
            internal VFXTaskCompiledData m_TaskData;
            internal HashSet<string> m_Defines;
            internal HashSet<string> m_Dependencies;
            internal VFXCodeGenerator.Cache m_CodeGeneratorCache;
            internal bool m_HumanReadable;
        }

        internal class VFXSnippetNode
        {
            private static readonly char[] kDefineSeparator = { ',', ' ', '\t' };
            private const string kVFXBeginStr = "${VFXBegin";
            private const string kVFXEndStr = "${VFXEnd";
            private const string kVFXIncludeStr = "${VFXInclude";
            private const string kVFXLoadParameterStr = "${VFXLoadParameter";
            private const string kVFXLoadAttributesStr = "${VFXLoadAttributes";
            private const string kVFXStoreAttributesStr = "${VFXStoreAttributes";
            protected const string kEndLineStr = "\n";

            private List<VFXSnippetNode> m_Children;
            protected string m_CurrentIndent;
            protected ShaderGenerationData m_ShaderGenerationData;

            public VFXSnippetNode(ShaderGenerationData shaderGenerationData, string indent)
            {
                this.m_ShaderGenerationData = shaderGenerationData;
                this.m_CurrentIndent = indent;
            }

            public virtual void AppendContent(StringBuilder sb)
            {
                if (m_Children != null)
                {
                    foreach (var snippet in m_Children)
                    {
                        snippet.AppendContent(sb);
                    }
                }
            }

            //Returns endIndex
            protected int CollectChildrenFromString(string templateContent, int startIndex)
            {
                int index;
                while (true)
                {
                    index = -1;
                    if (startIndex <= templateContent.Length)
                        index = templateContent.IndexOf("${", startIndex, StringComparison.Ordinal);
                    if (index == -1)
                    {
                        AddRawContent(templateContent, startIndex, templateContent.Length);
                        break;
                    }

                    AddRawContent(templateContent, startIndex, index);

                    ExtractSnippet(templateContent, index, out string snippetWithDecoration, out string snippetIndent);

                    if (m_ShaderGenerationData.m_CodeGeneratorCache.TryGetSnippet(snippetWithDecoration, out StringBuilder snippetContent))
                    {
                        VFXSnippetNodeLeaf snippetLeaf = new VFXSnippetNodeLeaf(snippetContent, m_ShaderGenerationData,  m_CurrentIndent + snippetIndent);
                        AddChildNode(snippetLeaf);
                        startIndex = index + snippetWithDecoration.Length;
                    }
                    else if (snippetWithDecoration.StartsWith(kVFXLoadParameterStr, StringComparison.Ordinal))
                        startIndex = HandleLoadParameter(templateContent, index, snippetIndent);
                    else if (snippetWithDecoration.StartsWith(kVFXLoadAttributesStr, StringComparison.Ordinal))
                        startIndex = HandleLoadAttribute(templateContent, index, snippetIndent);
                    else if (snippetWithDecoration.StartsWith(kVFXStoreAttributesStr, StringComparison.Ordinal))
                        startIndex = HandleStoreAttribute(templateContent, index, snippetIndent);
                    else if (snippetWithDecoration.StartsWith(kVFXBeginStr, StringComparison.Ordinal))
                        startIndex = HandleBeginTag(templateContent, snippetWithDecoration, index);
                    else if (snippetWithDecoration.StartsWith(kVFXEndStr, StringComparison.Ordinal))
                        return index + snippetWithDecoration.Length + 1;
                    else if (snippetWithDecoration.StartsWith(kVFXIncludeStr, StringComparison.Ordinal))
                        startIndex =
                            HandleTemplateInclude(templateContent, index, snippetIndent, snippetWithDecoration);
                    else
                        startIndex = index + snippetWithDecoration.Length + 1;
                }

                return index;
            }
            private void AddChildNode(VFXSnippetNode node)
            {
                if (m_Children == null)
                    m_Children = new List<VFXSnippetNode>();
                m_Children.Add(node);
            }

            private void ExtractSnippet(string templateContent, int startIndex, out string snippetWithDecoration, out string snippetIndent)
            {
                int endIndex = templateContent.IndexOf("}", startIndex, StringComparison.Ordinal);
                snippetWithDecoration = templateContent.Substring(startIndex, endIndex - startIndex + 1);
                snippetIndent = GetIndent(templateContent, startIndex);
            }

            private void AddRawContent(string templateContent, int startIndex, int index)
            {
                string leafContent = templateContent.Substring(startIndex, index - startIndex);
                if (!string.IsNullOrEmpty(leafContent))
                {
                    VFXSnippetNodeLeaf leaf = new VFXSnippetNodeLeaf(leafContent, m_ShaderGenerationData, m_CurrentIndent);
                    AddChildNode(leaf);
                }
            }
            private void MatchTemplateIncludePattern(string templateContent, int index, out string includePath, out string includeDefinesRaw, out bool renderPipelineInclude)
            {
                int includeNameBeginIndex = templateContent.IndexOf("\"", index, StringComparison.Ordinal) + 1;
                int includeNameEndIndex = templateContent.IndexOf("\"", includeNameBeginIndex + 1, StringComparison.Ordinal);
                includePath =
                    templateContent.Substring(includeNameBeginIndex, includeNameEndIndex - includeNameBeginIndex);

                renderPipelineInclude = templateContent.IndexOf("RP", index, includeNameBeginIndex - index, StringComparison.Ordinal) != -1;

                int definesBeginIndex = includeNameEndIndex + 2;
                int definesEndIndex = templateContent.IndexOf("}", definesBeginIndex, StringComparison.Ordinal);
                includeDefinesRaw = templateContent.Substring(definesBeginIndex, definesEndIndex - definesBeginIndex);
            }

            private int HandleTemplateInclude(string templateContent, int index, string snippetIndent, string snippetWithDecoration)
            {
                bool AcceptInclude(string includeDefinesFused)
                {
                    var includeDefines = includeDefinesFused.Split(kDefineSeparator, StringSplitOptions.RemoveEmptyEntries);
                    foreach(var define in includeDefines)
                    {
                        if (define[0] != '!')
                        {
                            if (!m_ShaderGenerationData.m_Defines.Contains(define))
                                return false;
                        }
                        else if (m_ShaderGenerationData.m_Defines.Contains(define.Substring(1)))
                            return false;
                    }

                    return true;
                }

                MatchTemplateIncludePattern(templateContent, index, out string includePath,
                    out string includeDefinesRaw, out bool renderPipelineInclude);

                if(string.IsNullOrEmpty(includeDefinesRaw) || AcceptInclude(includeDefinesRaw))
                {
                    string absolutePath = $"{(renderPipelineInclude ? VFXLibrary.currentSRPBinder.templatePath : VisualEffectGraphPackageInfo.assetPackagePath)}/{includePath}";
                    VFXSnippetNodeInclude includeSnippetNode = new VFXSnippetNodeInclude(absolutePath, m_ShaderGenerationData, m_CurrentIndent + snippetIndent);
                    includeSnippetNode.CollectChildren();
                    AddChildNode(includeSnippetNode);
                }

                var startIndex = index + snippetWithDecoration.Length;
                return startIndex;
            }

            private int HandleBeginTag(string templateContent, string snippetWithDecoration, int index)
            {
                string macroName = snippetWithDecoration.Substring(kVFXBeginStr.Length + 1,
                    snippetWithDecoration.Length - kVFXBeginStr.Length - 2);
                int startIndex = index + snippetWithDecoration.Length;
                if (templateContent[startIndex] == '\n')
                    startIndex += 1;
                VFXSnippetNodeMacroDefinition macroDefinitionNode =
                    new VFXSnippetNodeMacroDefinition(macroName, m_ShaderGenerationData, "");
                startIndex = macroDefinitionNode.CollectChildrenFromString(templateContent, startIndex);
                macroDefinitionNode.ExpandMacroAndStore();
                AddChildNode(macroDefinitionNode);
                return startIndex;
            }

            private void MatchSnippetPattern(string templateContent, int index, out string key, out string pattern)
            {
                int endIndex = templateContent.IndexOf("}}", index, StringComparison.Ordinal) + 2;
                int patternIndex = templateContent.IndexOf(":{", index, StringComparison.Ordinal) + 2;
                key = templateContent.Substring(index, endIndex - index);
                pattern = templateContent.Substring(patternIndex, endIndex - patternIndex - 2);
            }

            private void AddLeafNodeFromShaderWriter(string snippetIndent, VFXShaderWriter shaderWriter, string str)
            {
                VFXSnippetNodeLeaf snippetNodeLeaf = new VFXSnippetNodeLeaf(shaderWriter.builder,
                    m_ShaderGenerationData, m_CurrentIndent + snippetIndent);
                AddChildNode(snippetNodeLeaf);
                m_ShaderGenerationData.m_CodeGeneratorCache.TryAddSnippet(str, shaderWriter.builder);
            }

            private int HandleStoreAttribute(string templateContent, int index, string snippetIndent)
            {
                MatchSnippetPattern(templateContent, index, out string key, out string pattern);
                var storeAttribute = VFXCodeGenerator.GenerateStoreAttribute(pattern, m_ShaderGenerationData.m_Context,
                    (uint)m_ShaderGenerationData.m_TaskData.linkedEventOut.Length);
                AddLeafNodeFromShaderWriter(snippetIndent, storeAttribute, key);
                int startIndex = index + key.Length + 1;
                return startIndex;
            }

            private int HandleLoadAttribute(string templateContent, int index, string snippetIndent)
            {
                MatchSnippetPattern(templateContent, index, out string key, out string pattern);
                var loadAttribute = VFXCodeGenerator.GenerateLoadAttribute(pattern, m_ShaderGenerationData.m_Context,
                    m_ShaderGenerationData.m_TaskData);
                AddLeafNodeFromShaderWriter(snippetIndent, loadAttribute, key);
                int startIndex = index + key.Length + 1;
                return startIndex;
            }

            private int HandleLoadParameter(string templateContent, int index, string snippetIndent)
            {
                MatchSnippetPattern(templateContent, index, out string key, out string pattern);
                var loadParameters = VFXCodeGenerator.GenerateLoadParameter(pattern,
                    m_ShaderGenerationData.m_MainParameters, m_ShaderGenerationData.m_ExpressionToName);
                if (string.IsNullOrEmpty(loadParameters.ToString()))
                    loadParameters.builder.AppendLine();

                AddLeafNodeFromShaderWriter(snippetIndent, loadParameters, key);
                int startIndex = index + key.Length;
                return startIndex;
            }
        }

        class VFXSnippetNodeLeaf : VFXSnippetNode
        {
            private StringBuilder m_LeafContentSb;
            private string m_LeafContentStr;

            internal VFXSnippetNodeLeaf(StringBuilder leafContentSb, ShaderGenerationData shaderGenerationData, string indent) : base(shaderGenerationData,  indent)
            {
                //Copy to not accumulate indent
                this.m_LeafContentSb = new StringBuilder();
                this.m_LeafContentSb.Append(leafContentSb);
            }

            internal VFXSnippetNodeLeaf(string leafContentStr, ShaderGenerationData shaderGenerationData, string indent) : base(shaderGenerationData, indent)
            {
                this.m_LeafContentStr = leafContentStr;
            }

            public override void AppendContent(StringBuilder sb)
            {
                if (m_ShaderGenerationData.m_HumanReadable && m_CurrentIndent.Length > 0)
                {
                    if (m_LeafContentSb != null)
                    {
                        m_LeafContentSb.Replace(kEndLineStr, kEndLineStr + m_CurrentIndent);
                        sb.Append(m_LeafContentSb);
                    }
                    else
                    {
                        var lines = m_LeafContentStr.Split(kEndLineStr, StringSplitOptions.None);
                        int i = 0;
                        foreach (var line in lines)
                        {
                            if (i > 0)
                                sb.Append(kEndLineStr + m_CurrentIndent);
                            sb.Append(line);
                            i++;
                        }
                    }
                }
                else
                {
                    if (m_LeafContentSb != null)
                        sb.Append(m_LeafContentSb);
                    else
                        sb.Append(m_LeafContentStr);
                }
            }
        }

        internal class VFXSnippetNodeInclude : VFXSnippetNode
        {
            private string m_IncludePath;

            internal VFXSnippetNodeInclude(string includePath, ShaderGenerationData shaderGenerationData, string indent) : base(shaderGenerationData, indent)
            {
                this.m_IncludePath = includePath;
            }

            public void CollectChildren()
            {
                if (!m_ShaderGenerationData.m_CodeGeneratorCache.TryGetTemplateCache(m_IncludePath, out var templateContent))
                {
                    m_ShaderGenerationData.m_Dependencies.Add(AssetDatabase.AssetPathToGUID(m_IncludePath));
                    var formattedPath = FormatPath(m_IncludePath);
                    templateContent = File.ReadAllText(formattedPath);
                    m_ShaderGenerationData.m_CodeGeneratorCache.AddTemplateCache(m_IncludePath, templateContent);
                }
                CollectChildrenFromString(templateContent, 0);
            }
        }

        class VFXSnippetNodeMacroDefinition : VFXSnippetNode
        {
            private string m_MacroName;
            private StringBuilder m_ExpandedMacro;
            private bool m_Expanded;

            internal VFXSnippetNodeMacroDefinition(string macroName, ShaderGenerationData shaderGenerationData,
                string indent) : base(shaderGenerationData, indent)
            {
                this.m_MacroName = macroName;
            }

            public void ExpandMacroAndStore()
            {
                StringBuilder sb = new StringBuilder();
                AppendContent(sb);
                m_ExpandedMacro = sb;
                string snippetizedMacroName = $"${{{m_MacroName}}}";
                if (!m_ShaderGenerationData.m_CodeGeneratorCache.TryAddSnippet(snippetizedMacroName, m_ExpandedMacro))
                {
                    m_ShaderGenerationData.m_CodeGeneratorCache.SetSnippet(snippetizedMacroName,m_ExpandedMacro);
                }

                m_Expanded = true;
            }

            public override void AppendContent(StringBuilder sb)
            {
                if (!m_Expanded)
                    base.AppendContent(sb);
            }
        }
    }
}
