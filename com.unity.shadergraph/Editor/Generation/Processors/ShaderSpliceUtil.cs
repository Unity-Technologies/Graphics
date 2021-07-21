using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Profiling;

namespace UnityEditor.ShaderGraph
{
    static class ShaderSpliceUtil
    {
        private static char[] channelNames =
        { 'x', 'y', 'z', 'w' };


        private static char[] whitespace =
        { ' ', '\t', '\r', '\n', '\f'};
        public static string GetChannelSwizzle(int firstChannel, int channelCount)
        {
            System.Text.StringBuilder result = new System.Text.StringBuilder();
            int lastChannel = System.Math.Min(firstChannel + channelCount - 1, 4);
            for (int index = firstChannel; index <= lastChannel; index++)
            {
                result.Append(channelNames[index]);
            }
            return result.ToString();
        }

        // returns the offset of the first non-whitespace character, in the range [start, end] inclusive ... will return end if none found
        private static int SkipWhitespace(string str, int start, int end)
        {
            int index = start;
            while (index < end)
            {
                char c = str[index];
                if (!whitespace.Contains(c))
                {
                    break;
                }
                index++;
            }
            return index;
        }

        public class TemplatePreprocessor
        {
            // inputs
            ActiveFields activeFields;
            Dictionary<string, string> namedFragments;
            string[] templatePaths;
            bool isDebug;

            // intermediates
            HashSet<string> includedFiles;

            // outputs
            ShaderStringBuilder result;
            AssetCollection assetCollection;

            public TemplatePreprocessor(ActiveFields activeFields, Dictionary<string, string> namedFragments, bool isDebug, string[] templatePaths, AssetCollection assetCollection, bool humanReadable, ShaderStringBuilder outShaderCodeResult = null)
            {
                this.activeFields = activeFields;
                this.namedFragments = namedFragments;
                this.isDebug = isDebug;
                this.templatePaths = templatePaths;
                this.assetCollection = assetCollection;
                this.result = outShaderCodeResult ?? new ShaderStringBuilder(humanReadable: humanReadable);
                includedFiles = new HashSet<string>();
            }

            public ShaderStringBuilder GetShaderCode()
            {
                return result;
            }

            public void ProcessTemplateFile(string filePath)
            {
                if (File.Exists(filePath) &&
                    !includedFiles.Contains(filePath))
                {
                    includedFiles.Add(filePath);

                    if (assetCollection != null)
                    {
                        GUID guid = AssetDatabase.GUIDFromAssetPath(filePath);
                        if (!guid.Empty())
                            assetCollection.AddAssetDependency(guid, AssetCollection.Flags.SourceDependency);
                    }

                    string[] templateLines = File.ReadAllLines(filePath);
                    foreach (string line in templateLines)
                    {
                        ProcessTemplateLine(line, 0, line.Length);
                    }
                }
            }

            private struct Token
            {
                public string s;
                public int start;
                public int end;

                public Token(string s, int start, int end)
                {
                    this.s = s;
                    this.start = start;
                    this.end = end;
                }

                public static Token Invalid()
                {
                    return new Token(null, 0, 0);
                }

                public bool IsValid()
                {
                    return (s != null);
                }

                public bool Is(string other)
                {
                    int len = end - start;
                    return (other.Length == len) && (0 == string.CompareOrdinal(s, start, other, 0, len));
                }

                public string GetString()
                {
                    int len = end - start;
                    if (len > 0)
                    {
                        return s.Substring(start, end - start);
                    }
                    return null;
                }
            }

            public void ProcessTemplateLine(string line, int start, int end)
            {
                bool appendEndln = true;

                int cur = start;
                while (cur < end)
                {
                    // find an escape code '$'
                    int dollar = line.IndexOf('$', cur, end - cur);
                    if (dollar < 0)
                    {
                        // no escape code found in the remaining code -- just append the rest verbatim
                        AppendSubstring(line, cur, true, end, false);
                        break;
                    }
                    else
                    {
                        // found $ escape sequence
                        Token command = ParseIdentifier(line, dollar + 1, end);
                        if (!command.IsValid())
                        {
                            Error("ERROR: $ must be followed by a command string (if, splice, or include)", line, dollar + 1);
                            break;
                        }
                        else
                        {
                            if (command.Is("include"))
                            {
                                ProcessIncludeCommand(command, end);
                                appendEndln = false;
                                break;      // include command always ignores the rest of the line, error or not
                            }
                            else if (command.Is("splice"))
                            {
                                if (!ProcessSpliceCommand(command, end, ref cur))
                                {
                                    // error, skip the rest of the line
                                    break;
                                }
                            }
                            else
                            {
                                // let's see if it is a predicate
                                Token predicate = ParseUntil(line, dollar + 1, end, ':');
                                if (!predicate.IsValid())
                                {
                                    Error("ERROR: unrecognized command: " + command.GetString(), line, command.start);
                                    break;
                                }
                                else
                                {
                                    if (!ProcessPredicate(predicate, end, ref cur, ref appendEndln))
                                    {
                                        break;  // skip the rest of the line
                                    }
                                }
                            }
                        }
                    }
                }

                if (appendEndln)
                {
                    result.AppendNewLine();
                }
            }

            private void ProcessIncludeCommand(Token includeCommand, int lineEnd)
            {
                if (Expect(includeCommand.s, includeCommand.end, '('))
                {
                    Token param = ParseString(includeCommand.s, includeCommand.end + 1, lineEnd);

                    if (!param.IsValid())
                    {
                        Error("ERROR: $include expected a string file path parameter", includeCommand.s, includeCommand.end + 1);
                    }
                    else
                    {
                        bool found = false;
                        string includeLocation = null;

                        // Use reverse order in the array, higher number element have higher priority in case $include exist in several directories
                        for (int i = templatePaths.Length - 1; i >= 0; i--)
                        {
                            string templatePath = templatePaths[i];
                            includeLocation = Path.Combine(templatePath, param.GetString());
                            if (File.Exists(includeLocation))
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            string errorStr = "ERROR: $include cannot find file : " + param.GetString() + ". Looked into:\n";

                            foreach (string templatePath in templatePaths)
                            {
                                errorStr += "// " + templatePath + "\n";
                            }

                            Error(errorStr, includeCommand.s, param.start);
                        }
                        else
                        {
                            int endIndex = result.length;
                            using (var temp = new ShaderStringBuilder(humanReadable: true))
                            {
                                // Wrap in debug mode
                                if (isDebug)
                                {
                                    result.AppendLine("//-------------------------------------------------------------------------------------");
                                    result.AppendLine("// TEMPLATE INCLUDE : " + param.GetString());
                                    result.AppendLine("//-------------------------------------------------------------------------------------");
                                    result.AppendNewLine();
                                }

                                // Recursively process templates
                                ProcessTemplateFile(includeLocation);

                                // Wrap in debug mode
                                if (isDebug)
                                {
                                    result.AppendNewLine();
                                    result.AppendLine("//-------------------------------------------------------------------------------------");
                                    result.AppendLine("// END TEMPLATE INCLUDE : " + param.GetString());
                                    result.AppendLine("//-------------------------------------------------------------------------------------");
                                }

                                result.AppendNewLine();

                                // Required to enforce indentation rules
                                // Append lines from this include into temporary StringBuilder
                                // Reduce result length to remove this include
                                temp.AppendLines(result.ToString(endIndex, result.length - endIndex));
                                result.length = endIndex;
                                result.AppendLines(temp.ToCodeBlock());
                            }
                        }
                    }
                }
            }

            private bool ProcessSpliceCommand(Token spliceCommand, int lineEnd, ref int cur)
            {
                if (!Expect(spliceCommand.s, spliceCommand.end, '('))
                {
                    return false;
                }
                else
                {
                    Token param = ParseUntil(spliceCommand.s, spliceCommand.end + 1, lineEnd, ')');
                    if (!param.IsValid())
                    {
                        Error("ERROR: splice command is missing a ')'", spliceCommand.s, spliceCommand.start);
                        return false;
                    }
                    else
                    {
                        // append everything before the beginning of the escape sequence
                        AppendSubstring(spliceCommand.s, cur, true, spliceCommand.start - 1, false);

                        // find the named fragment
                        string name = param.GetString();     // unfortunately this allocates a new string
                        string fragment;
                        if ((namedFragments != null) && namedFragments.TryGetValue(name, out fragment))
                        {
                            // splice the fragment
                            result.Append(fragment);
                        }
                        else
                        {
                            // no named fragment found
                            result.Append("/* WARNING: $splice Could not find named fragment '{0}' */", name);
                        }

                        // advance to just after the ')' and continue parsing
                        cur = param.end + 1;
                    }
                }
                return true;
            }

            private bool ProcessPredicate(Token predicate, int endLine, ref int cur, ref bool appendEndln)
            {
                // eval if(param)
                var fieldName = predicate.GetString();
                var nonwhitespace = SkipWhitespace(predicate.s, predicate.end + 1, endLine);

                if (!fieldName.StartsWith("features", StringComparison.Ordinal) && activeFields.permutationCount > 0)
                {
                    var passedPermutations = activeFields.allPermutations.instances.Where(i => i.Contains(fieldName)).ToList();
                    if (passedPermutations.Count > 0)
                    {
                        var ifdefs = KeywordUtil.GetKeywordPermutationSetConditional(
                            passedPermutations.Select(i => i.permutationIndex).ToList()
                        );
                        result.AppendLine(ifdefs);
                        //Append the rest of the line
                        AppendSubstring(predicate.s, nonwhitespace, true, endLine, false);
                        result.AppendNewLine();
                        result.AppendLine("#endif");
                        return false;
                    }
                    else
                    {
                        appendEndln = false; //if line isn't active, remove whitespace
                    }
                    return false;
                }
                else
                {
                    // eval if(param)
                    bool contains = activeFields.baseInstance.Contains(fieldName);
                    if (contains)
                    {
                        // predicate is active
                        // append everything before the beginning of the escape sequence
                        AppendSubstring(predicate.s, cur, true, predicate.start - 1, false);

                        // continue parsing the rest of the line, starting with the first nonwhitespace character
                        cur = nonwhitespace;
                        return true;
                    }
                    else
                    {
                        // predicate is not active
                        if (isDebug)
                        {
                            // append everything before the beginning of the escape sequence
                            AppendSubstring(predicate.s, cur, true, predicate.start - 1, false);
                            // append the rest of the line, commented out
                            result.Append("// ");
                            AppendSubstring(predicate.s, nonwhitespace, true, endLine, false);
                        }
                        else
                        {
                            // don't append anything
                            appendEndln = false;
                        }
                        return false;
                    }
                }
            }

            private static bool IsLetter(char c)
            {
                return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
            }

            private static bool IsLetterOrDigit(char c)
            {
                return IsLetter(c) || Char.IsDigit(c);
            }

            private Token ParseIdentifier(string code, int start, int end)
            {
                if (start < end)
                {
                    char c = code[start];
                    if (IsLetter(c) || (c == '_'))
                    {
                        int cur = start + 1;
                        while (cur < end)
                        {
                            c = code[cur];
                            if (!(IsLetterOrDigit(c) || (c == '_')))
                                break;
                            cur++;
                        }
                        return new Token(code, start, cur);
                    }
                }
                return Token.Invalid();
            }

            private Token ParseString(string line, int start, int end)
            {
                if (Expect(line, start, '"'))
                {
                    return ParseUntil(line, start + 1, end, '"');
                }
                return Token.Invalid();
            }

            private Token ParseUntil(string line, int start, int end, char endChar)
            {
                int cur = start;
                while (cur < end)
                {
                    if (line[cur] == endChar)
                    {
                        return new Token(line, start, cur);
                    }
                    cur++;
                }
                return Token.Invalid();
            }

            private bool Expect(string line, int location, char expected)
            {
                if ((location < line.Length) && (line[location] == expected))
                {
                    return true;
                }
                Error("Expected '" + expected + "'", line, location);
                return false;
            }

            private void Error(string error, string line, int location)
            {
                // append the line for context
                result.Append("\n");
                result.Append("// ");
                AppendSubstring(line, 0, true, line.Length, false);
                result.Append("\n");

                // append the location marker, and error description
                result.Append("// ");
                result.AppendSpaces(location);
                result.Append("^ ");
                result.Append(error);
                result.Append("\n");
            }

            // an easier to use version of substring Append() -- explicit inclusion on each end, and checks for positive length
            private void AppendSubstring(string str, int start, bool includeStart, int end, bool includeEnd)
            {
                if (!includeStart)
                {
                    start++;
                }
                if (!includeEnd)
                {
                    end--;
                }
                int count = end - start + 1;
                if (count > 0)
                {
                    result.Append(str, start, count);
                }
            }
        }
    }
}
