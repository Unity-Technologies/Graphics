using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Data.Util;

namespace UnityEditor.ShaderGraph
{
    static class ShaderSpliceUtil
    {
        enum BaseFieldType
        {
            Invalid,
            Float,
            Uint,
        };

        private static BaseFieldType GetBaseFieldType(string typeName)
        {
            if (typeName.StartsWith("Vector") || typeName.Equals("Single"))
            {
                return BaseFieldType.Float;
            }
            if (typeName.StartsWith("UInt32")) // We don't have proper support for uint (Uint, Uint2, Uint3, Uint4). Need these types, for now just supporting instancing via a single uint.
            {
                return BaseFieldType.Uint;
            }
            return BaseFieldType.Invalid;
        }

        private static int GetComponentCount(string typeName)
        {
            switch (GetBaseFieldType(typeName))
            {
                case BaseFieldType.Float:
                    return GetFloatVectorCount(typeName);
                case BaseFieldType.Uint:
                    return GetUintCount(typeName);
                default:
                    return 0;
            }
        }

        private static int GetFloatVectorCount(string typeName)
        {
            if (typeName.Equals("Vector4"))
            {
                return 4;
            }
            else if (typeName.Equals("Vector3"))
            {
                return 3;
            }
            else if (typeName.Equals("Vector2"))
            {
                return 2;
            }
            else if (typeName.Equals("Single"))
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        // Need uint types
        private static int GetUintCount(string typeName)
        {
            if (typeName.Equals("UInt32"))
            {
                return 1;
            }
            if(typeName.Equals("UInt32_4"))
            {
                return 4;
            }
            else
            {
                return 0;
            }
        }

        private static string[] vectorTypeNames =
        {
            "unknown",
            "float",
            "float2",
            "float3",
            "float4"
        };

        private static string[] uintTypeNames =
        {
            "unknown",
            "uint",
            "uint2",
            "uint3",
            "uint4",
        };

        private static char[] channelNames =
        { 'x', 'y', 'z', 'w' };

        private static string GetChannelSwizzle(int firstChannel, int channelCount)
        {
            System.Text.StringBuilder result = new System.Text.StringBuilder();
            int lastChannel = System.Math.Min(firstChannel + channelCount - 1, 4);
            for (int index = firstChannel; index <= lastChannel; index++)
            {
                result.Append(channelNames[index]);
            }
            return result.ToString();
        }

        private static bool ShouldSpliceField(System.Type parentType, FieldInfo field, IActiveFields activeFields, out bool isOptional)
        {
            bool fieldActive = true;
            isOptional = field.IsDefined(typeof(Optional), false);
            if (isOptional)
            {
                string fullName = parentType.Name + "." + field.Name;
                if (!activeFields.Contains(fullName))
                {
                    // not active, skip the optional field
                    fieldActive = false;
                }
            }
            return fieldActive;
        }

        private static string GetFieldSemantic(FieldInfo field)
        {
            string semanticString = null;
            object[] semantics = field.GetCustomAttributes(typeof(Semantic), false);
            if (semantics.Length > 0)
            {
                Semantic firstSemantic = (Semantic)semantics[0];
                semanticString = " : " + firstSemantic.semantic;
            }
            return semanticString;
        }

        private static string GetFieldType(FieldInfo field, out int componentCount)
        {
            string fieldType;
            object[] overrideType = field.GetCustomAttributes(typeof(OverrideType), false);
            if (overrideType.Length > 0)
            {
                OverrideType first = (OverrideType)overrideType[0];
                fieldType = first.typeName;
                componentCount = 0;
            }
            else
            {
                // TODO: handle non-float types
                componentCount = GetComponentCount(field.FieldType.Name);
                switch (GetBaseFieldType(field.FieldType.Name))
                {
                    case BaseFieldType.Float:
                        fieldType = vectorTypeNames[componentCount];
                        break;
                    case BaseFieldType.Uint:
                        fieldType = uintTypeNames[componentCount];
                        break;
                    default:
                        fieldType = "unknown";
                        break;
                }
            }
            return fieldType;
        }

        private static bool IsFloatVectorType(string type)
        {
            return GetFloatVectorCount(type) != 0;
        }

        private static string GetFieldConditional(FieldInfo field)
        {
            string conditional = null;
            object[] overrideType = field.GetCustomAttributes(typeof(PreprocessorIf), false);
            if (overrideType.Length > 0)
            {
                PreprocessorIf first = (PreprocessorIf)overrideType[0];
                conditional = first.conditional;
            }
            return conditional;
        }

        public static void BuildType(System.Type t, ActiveFields activeFields, ShaderGenerator result, bool isDebug)
        {
            result.AddShaderChunk("struct " + t.Name);
            result.AddShaderChunk("{");
            result.Indent();

            foreach (FieldInfo field in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (field.MemberType == MemberTypes.Field)
                {
                    bool isOptional = false;

                    var fieldIsActive = false;
                    var keywordIfdefs = string.Empty;

                    if (activeFields.permutationCount > 0)
                    {
                        // Evaluate all activeFields instance
                        var instances = activeFields
                            .allPermutations.instances
                            .Where(i => ShouldSpliceField(t, field, i, out isOptional))
                            .ToList();

                        fieldIsActive = instances.Count > 0;
                        if (fieldIsActive)
                            keywordIfdefs = KeywordUtil.GetKeywordPermutationSetConditional(instances
                                .Select(i => i.permutationIndex).ToList());
                    }
                    else
                    {
                        fieldIsActive = ShouldSpliceField(t, field, activeFields.baseInstance, out isOptional);
                    }


                    if (fieldIsActive)
                    {
                        // The field is used, so generate it
                        var semanticString = GetFieldSemantic(field);
                        int componentCount;
                        var fieldType = GetFieldType(field, out componentCount);
                        var conditional = GetFieldConditional(field);

                        if (conditional != null)
                            result.AddShaderChunk("#if " + conditional);
                        if (!string.IsNullOrEmpty(keywordIfdefs))
                            result.AddShaderChunk(keywordIfdefs);

                        var fieldDecl = fieldType + " " + field.Name + semanticString + ";" + (isOptional & isDebug ? " // optional" : string.Empty);
                        result.AddShaderChunk(fieldDecl);

                        if (!string.IsNullOrEmpty(keywordIfdefs))
                            result.AddShaderChunk("#endif" + (isDebug ? " // Shader Graph Keywords" : string.Empty));
                        if (conditional != null)
                            result.AddShaderChunk("#endif" + (isDebug ? $" // {conditional}" : string.Empty));
                    }
                }
            }
            result.Deindent();
            result.AddShaderChunk("};");

            object[] packAttributes = t.GetCustomAttributes(typeof(InterpolatorPack), false);
            if (packAttributes.Length > 0)
            {
                result.AddNewLine();

                if (activeFields.permutationCount > 0)
                {
                    var generatedPackedTypes = new Dictionary<string, (ShaderGenerator, List<int>)>();
                    foreach (var instance in activeFields.allPermutations.instances)
                    {
                        var instanceGenerator = new ShaderGenerator();
                        BuildPackedType(t, instance, instanceGenerator, isDebug);
                        var key = instanceGenerator.GetShaderString(0);
                        if (generatedPackedTypes.TryGetValue(key, out var value))
                            value.Item2.Add(instance.permutationIndex);
                        else
                            generatedPackedTypes.Add(key, (instanceGenerator, new List<int> { instance.permutationIndex }));
                    }

                    var isFirst = true;
                    foreach (var generated in generatedPackedTypes)
                    {
                        if (isFirst)
                        {
                            isFirst = false;
                            result.AddShaderChunk(KeywordUtil.GetKeywordPermutationSetConditional(generated.Value.Item2));
                        }
                        else
                            result.AddShaderChunk(KeywordUtil.GetKeywordPermutationSetConditional(generated.Value.Item2).Replace("#if", "#elif"));

                        result.AddGenerator(generated.Value.Item1);
                    }
                    if (generatedPackedTypes.Count > 0)
                        result.AddShaderChunk("#endif");
                }
                else
                {
                    BuildPackedType(t, activeFields.baseInstance, result, isDebug);
                }
            }
        }

        public static void BuildPackedType(System.Type unpacked, IActiveFields activeFields, ShaderGenerator result, bool isDebug)
        {
            // for each interpolator, the number of components used (up to 4 for a float4 interpolator)
            List<int> packedCounts = new List<int>();
            ShaderGenerator packingStruct = new ShaderGenerator();
            ShaderGenerator packer = new ShaderGenerator();
            ShaderGenerator unpacker = new ShaderGenerator();
            ShaderGenerator systemGenerated = new ShaderGenerator();

            string unpackedStruct = unpacked.Name.ToString();
            string packedStruct = "Packed" + unpacked.Name;
            string packerFunction = "Pack" + unpacked.Name;
            string unpackerFunction = "Unpack" + unpacked.Name;

            // declare struct header:
            //   struct packedStruct {
            packingStruct.AddShaderChunk("// Generated Type: Packed" + unpacked.Name);
            packingStruct.AddShaderChunk("struct " + packedStruct);
            packingStruct.AddShaderChunk("{");
            packingStruct.Indent();

            // declare function headers:
            //   packedStruct packerFunction(unpackedStruct input)
            //   {
            //      packedStruct output;
            packer.AddShaderChunk("// Packed Type: " + unpacked.Name);
            packer.AddShaderChunk(packedStruct + " " + packerFunction + "(" + unpackedStruct + " input)");
            packer.AddShaderChunk("{");
            packer.Indent();
            packer.AddShaderChunk(packedStruct + " output = (" + packedStruct + ")0;");

            //   unpackedStruct unpackerFunction(packedStruct input)
            //   {
            //      unpackedStruct output;
            unpacker.AddShaderChunk("// Unpacked Type: " + unpacked.Name);
            unpacker.AddShaderChunk(unpackedStruct + " " + unpackerFunction + "(" + packedStruct + " input)");
            unpacker.AddShaderChunk("{");
            unpacker.Indent();
            unpacker.AddShaderChunk(unpackedStruct + " output = (" + unpackedStruct + ")0;");

            // TODO: this could do a better job packing
            // especially if we allowed breaking up fields across multiple interpolators (to pack them into remaining space...)
            // though we would want to only do this if it improves final interpolator count, and is worth it on the target machine
            foreach (FieldInfo field in unpacked.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (field.MemberType == MemberTypes.Field)
                {
                    bool isOptional;
                    if (ShouldSpliceField(unpacked, field, activeFields, out isOptional))
                    {
                        string semanticString = GetFieldSemantic(field);
                        int floatVectorCount;
                        string fieldType = GetFieldType(field, out floatVectorCount);
                        string conditional = GetFieldConditional(field);

                        // System generated fields must appear last in struct definitions
                        bool isSystemGenerated = field.IsDefined(typeof(SystemGenerated), false);

                        if (conditional != null)
                        {
                            if(isSystemGenerated)
                            {
                                systemGenerated.AddShaderChunk("#if " + conditional);
                            }
                            else
                            {
                                packingStruct.AddShaderChunk("#if " + conditional);
                            }
                            packer.AddShaderChunk("#if " + conditional);
                            unpacker.AddShaderChunk("#if " + conditional);
                        }
                        if ((semanticString != null) || (floatVectorCount == 0))
                        {
                            if(isSystemGenerated)
                            {
                                systemGenerated.AddShaderChunk(fieldType + " " + field.Name + semanticString + ";" + (isDebug ? " // unpacked" : string.Empty));
                            }
                            else
                            {
                                packingStruct.AddShaderChunk(fieldType + " " + field.Name + semanticString + ";" + (isDebug ? " // unpacked" : string.Empty));
                            }
                            packer.AddShaderChunk("output." + field.Name + " = input." + field.Name + ";");
                            unpacker.AddShaderChunk("output." + field.Name + " = input." + field.Name + ";");
                        }
                        else
                        {
                            // pack float field

                            // super simple packing: use the first interpolator that has room for the whole value
                            int interpIndex = packedCounts.FindIndex(x => (x + floatVectorCount <= 4));
                            int firstChannel;
                            if (interpIndex < 0)
                            {
                                // allocate a new interpolator
                                interpIndex = packedCounts.Count;
                                firstChannel = 0;
                                packedCounts.Add(floatVectorCount);
                            }
                            else
                            {
                                // pack into existing interpolator
                                firstChannel = packedCounts[interpIndex];
                                packedCounts[interpIndex] += floatVectorCount;
                            }

                            // add code to packer and unpacker -- packed data declaration is handled later
                            string packedChannels = GetChannelSwizzle(firstChannel, floatVectorCount);
                            packer.AddShaderChunk(string.Format("output.interp{0:00}.{1} = input.{2};", interpIndex, packedChannels, field.Name));
                            unpacker.AddShaderChunk(string.Format("output.{0} = input.interp{1:00}.{2};", field.Name, interpIndex, packedChannels));
                        }
                        if (conditional != null)
                        {
                            if(isSystemGenerated)
                            {
                                systemGenerated.AddShaderChunk("#endif" + (isDebug ? $" // conditional" : string.Empty));
                            }
                            else
                            {
                                packingStruct.AddShaderChunk("#endif" + (isDebug ? $" // conditional" : string.Empty));
                            }
                            packer.AddShaderChunk("#endif" + (isDebug ? $" // conditional" : string.Empty));
                            unpacker.AddShaderChunk("#endif" + (isDebug ? $" // conditional" : string.Empty));
                        }
                    }
                }
            }

            // add packed data declarations to struct, using the packedCounts
            for (int index = 0; index < packedCounts.Count; index++)
            {
                int count = packedCounts[index];
                packingStruct.AddShaderChunk(string.Format("{0} interp{1:00} : TEXCOORD{1};" + (isDebug ? " // auto-packed" : string.Empty), vectorTypeNames[count], index));
            }

            // Add system generated fields at the end
            if(systemGenerated.numberOfChunks > 0)
            {
                packingStruct.AddGenerator(systemGenerated);
            }

            // close declarations
            packingStruct.Deindent();
            packingStruct.AddShaderChunk("};");
            packingStruct.AddNewLine();
            packer.AddShaderChunk("return output;");
            packer.Deindent();
            packer.AddShaderChunk("}");
            packer.AddNewLine();
            unpacker.AddShaderChunk("return output;");
            unpacker.Deindent();
            unpacker.AddShaderChunk("}");

            // combine all of the code into the result
            result.AddGenerator(packingStruct);
            result.AddGenerator(packer);
            result.AddGenerator(unpacker);
        }

        // returns the offset of the first non-whitespace character, in the range [start, end] inclusive ... will return end if none found
        private static int SkipWhitespace(string str, int start, int end)
        {
            int index = start;

            while (index < end)
            {
                char c = str[index];
                if (!Char.IsWhiteSpace(c))
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
            string templatePath;
            bool isDebug;
            string assemblyName;
            string resourceClassName;

            // intermediates
            HashSet<string> includedFiles;

            // outputs
            ShaderStringBuilder result;
            List<string> sourceAssetDependencyPaths;

            public TemplatePreprocessor(ActiveFields activeFields, Dictionary<string, string> namedFragments, bool isDebug, string templatePath, List<string> sourceAssetDependencyPaths, string assemblyName, string resourceClassName, ShaderStringBuilder outShaderCodeResult = null)
            {
                this.activeFields = activeFields;
                this.namedFragments = namedFragments;
                this.isDebug = isDebug;
                this.templatePath = templatePath;
                this.sourceAssetDependencyPaths = sourceAssetDependencyPaths;
                this.assemblyName = assemblyName;
                this.resourceClassName = resourceClassName;
                this.result = outShaderCodeResult ?? new ShaderStringBuilder();
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

                    if (sourceAssetDependencyPaths != null)
                        sourceAssetDependencyPaths.Add(filePath);

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
                    return (other.Length == len) && (0 == string.Compare(s, start, other, 0, len));
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
                        Token command = ParseIdentifier(line, dollar+1, end);
                        if (!command.IsValid())
                        {
                            Error("ERROR: $ must be followed by a command string (if, splice, or include)", line, dollar+1);
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
                            else if (command.Is("buildType"))
                            {
                                ProcessBuildTypeCommand(command, end);
                                appendEndln = false;
                                break;      // buildType command always ignores the rest of the line, error or not
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
                        var includeLocation = Path.Combine(templatePath, param.GetString());
                        if (!File.Exists(includeLocation))
                        {
                            Error("ERROR: $include cannot find file : " + includeLocation, includeCommand.s, param.start);
                        }
                        else
                        {
                            int endIndex = result.length;
                            using(var temp = new ShaderStringBuilder())
                            {
                                // Wrap in debug mode
                                if(isDebug)
                                {
                                    result.AppendLine("//-------------------------------------------------------------------------------------");
                                    result.AppendLine("// TEMPLATE INCLUDE : " + param.GetString());
                                    result.AppendLine("//-------------------------------------------------------------------------------------");
                                    result.AppendNewLine();
                                }

                                // Recursively process templates
                                ProcessTemplateFile(includeLocation);

                                // Wrap in debug mode
                                if(isDebug)
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
                                result.AppendLines(temp.ToCodeBlack());
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
                        AppendSubstring(spliceCommand.s, cur, true, spliceCommand.start-1, false);

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

            private void ProcessBuildTypeCommand(Token command, int endLine)
            {
                if (Expect(command.s, command.end, '('))
                {
                    Token param = ParseUntil(command.s, command.end + 1, endLine, ')');
                    if (!param.IsValid())
                    {
                        Error("ERROR: buildType command is missing a ')'", command.s, command.start);
                    }
                    else
                    {
                        string typeName = param.GetString();
                        Type type = GenerationUtils.GetTypeForStruct(typeName, resourceClassName, assemblyName);
                        if (type == null)
                        {
                            Error("ERROR: buildType could not find type : " + typeName, command.s, param.start);
                        }
                        else
                        {
                            ShaderGenerator temp = new ShaderGenerator();
                            temp.Indent();
                            temp.AddShaderChunk("// Generated Type: " + typeName);
                            BuildType(type, activeFields, temp, isDebug);
                            result.AppendLine(temp.GetShaderString(0, false));
                        }
                    }
                }
            }

            private bool ProcessPredicate(Token predicate, int endLine, ref int cur, ref bool appendEndln)
            {
                // eval if(param)
                var fieldName = predicate.GetString();
                var nonwhitespace = SkipWhitespace(predicate.s, predicate.end + 1, endLine);

                if (!fieldName.StartsWith("features") && activeFields.permutationCount > 0)
                {
                    var passedPermutations = activeFields.allPermutations.instances
                        .Where(i => i.Contains(fieldName))
                        .ToList();

                    if (passedPermutations.Count > 0)
                    {
                        var ifdefs = KeywordUtil.GetKeywordPermutationSetConditional(
                            passedPermutations.Select(i => i.permutationIndex).ToList()
                        );
                        result.AppendLine(ifdefs);
                        // Append the rest of the line
                        AppendSubstring(predicate.s, nonwhitespace, true, endLine, false);
                        result.AppendLine("");
                        result.AppendLine("#endif");

                        return false;
                    }

                    return false;
                }
                else
                {
                    // eval if(param)
                    if (activeFields.baseInstance.Contains(fieldName))
                    {
                        // predicate is active
                        // append everything before the beginning of the escape sequence
                        AppendSubstring(predicate.s, cur, true, predicate.start-1, false);

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
                            AppendSubstring(predicate.s, cur, true, predicate.start-1, false);
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

            private Token ParseIdentifier(string code, int start, int end)
            {
                if (start < end)
                {
                    char c = code[start];
                    if (Char.IsLetter(c) || (c == '_'))
                    {
                        int cur = start + 1;
                        while (cur < end)
                        {
                            c = code[cur];
                            if (!(Char.IsLetterOrDigit(c) || (c == '_')))
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

        public static void ApplyDependencies(IActiveFields activeFields, List<Dependency[]> dependsList)
        {
            // add active fields to queue
            Queue<string> fieldsToPropagate = new Queue<string>();
            foreach (var f in activeFields.fields)
            {
                fieldsToPropagate.Enqueue(f);
            }

            // foreach field in queue:
            while (fieldsToPropagate.Count > 0)
            {
                string field = fieldsToPropagate.Dequeue();
                if (activeFields.Contains(field))           // this should always be true
                {
                    // find all dependencies of field that are not already active
                    foreach (Dependency[] dependArray in dependsList)
                    {
                        foreach (Dependency d in dependArray.Where(d => (d.name == field) && !activeFields.Contains(d.dependsOn)))
                        {
                            // activate them and add them to the queue
                            activeFields.Add(d.dependsOn);
                            fieldsToPropagate.Enqueue(d.dependsOn);
                        }
                    }
                }
            }
        }
    }
}
