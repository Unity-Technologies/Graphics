using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditorInternal;
using Debug = UnityEngine.Debug;
using System.Reflection;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using Object = System.Object;

namespace UnityEditor.ShaderGraph
{
    // a structure used to track active variable dependencies in the shader code
    // (i.e. the use of uv0 in the pixel shader means we need a uv0 interpolator, etc.)
    struct Dependency
    {
        public string name;             // the name of the thing
        public string dependsOn;        // the thing above depends on this -- it reads it / calls it / requires it to be defined

        public Dependency(string name, string dependsOn)
        {
            this.name = name;
            this.dependsOn = dependsOn;
        }
    };

    [System.AttributeUsage(System.AttributeTargets.Struct)]
    class InterpolatorPack : System.Attribute
    {
        public InterpolatorPack()
        {
        }
    }

    // attribute used to flag a field as needing an HLSL semantic applied
    // i.e.    float3 position : POSITION;
    //                           ^ semantic
    [System.AttributeUsage(System.AttributeTargets.Field)]
    class Semantic : System.Attribute
    {
        public string semantic;

        public Semantic(string semantic)
        {
            this.semantic = semantic;
        }
    }

    // attribute used to flag a field as being optional
    // i.e. if it is not active, then we can omit it from the struct
    [System.AttributeUsage(System.AttributeTargets.Field)]
    class Optional : System.Attribute
    {
        public Optional()
        {
        }
    }

    // attribute used to override the HLSL type of a field with a custom type string
    [System.AttributeUsage(System.AttributeTargets.Field)]
    class OverrideType : System.Attribute
    {
        public string typeName;

        public OverrideType(string typeName)
        {
            this.typeName = typeName;
        }
    }

    // attribute used to disable a field using a preprocessor #if
    [System.AttributeUsage(System.AttributeTargets.Field)]
    class PreprocessorIf : System.Attribute
    {
        public string conditional;

        public PreprocessorIf(string conditional)
        {
            this.conditional = conditional;
        }
    }

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

        private static bool ShouldSpliceField(System.Type parentType, FieldInfo field, HashSet<string> activeFields, out bool isOptional)
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

        public static void BuildType(System.Type t, HashSet<string> activeFields, ShaderGenerator result)
        {
            result.AddShaderChunk("struct " + t.Name + " {");
            result.Indent();

            foreach (FieldInfo field in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (field.MemberType == MemberTypes.Field)
                {
                    bool isOptional;
                    if (ShouldSpliceField(t, field, activeFields, out isOptional))
                    {
                        string semanticString = GetFieldSemantic(field);
                        int componentCount;
                        string fieldType = GetFieldType(field, out componentCount);
                        string conditional = GetFieldConditional(field);

                        if (conditional != null)
                        {
                            result.AddShaderChunk("#if " + conditional);
                        }
                        string fieldDecl = fieldType + " " + field.Name + semanticString + ";" + (isOptional ? " // optional" : string.Empty);
                        result.AddShaderChunk(fieldDecl);
                        if (conditional != null)
                        {
                            result.AddShaderChunk("#endif // " + conditional);
                        }
                    }
                }
            }
            result.Deindent();
            result.AddShaderChunk("};");

            object[] packAttributes = t.GetCustomAttributes(typeof(InterpolatorPack), false);
            if (packAttributes.Length > 0)
            {
                BuildPackedType(t, activeFields, result);
            }
        }

        public static void BuildPackedType(System.Type unpacked, HashSet<string> activeFields, ShaderGenerator result)
        {
            // for each interpolator, the number of components used (up to 4 for a float4 interpolator)
            List<int> packedCounts = new List<int>();
            ShaderGenerator packer = new ShaderGenerator();
            ShaderGenerator unpacker = new ShaderGenerator();
            ShaderGenerator structEnd = new ShaderGenerator();

            string unpackedStruct = unpacked.Name.ToString();
            string packedStruct = "Packed" + unpacked.Name;
            string packerFunction = "Pack" + unpacked.Name;
            string unpackerFunction = "Unpack" + unpacked.Name;

            // declare struct header:
            //   struct packedStruct {
            result.AddShaderChunk("struct " + packedStruct + " {");
            result.Indent();

            // declare function headers:
            //   packedStruct packerFunction(unpackedStruct input)
            //   {
            //      packedStruct output;
            packer.AddShaderChunk(packedStruct + " " + packerFunction + "(" + unpackedStruct + " input)");
            packer.AddShaderChunk("{");
            packer.Indent();
            packer.AddShaderChunk(packedStruct + " output;");

            //   unpackedStruct unpackerFunction(packedStruct input)
            //   {
            //      unpackedStruct output;
            unpacker.AddShaderChunk(unpackedStruct + " " + unpackerFunction + "(" + packedStruct + " input)");
            unpacker.AddShaderChunk("{");
            unpacker.Indent();
            unpacker.AddShaderChunk(unpackedStruct + " output;");

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

                        if ((semanticString != null) || (conditional != null) || (floatVectorCount == 0))
                        {
                            // not a packed value
                            if (conditional != null)
                            {
                                structEnd.AddShaderChunk("#if " + conditional);
                                packer.AddShaderChunk("#if " + conditional);
                                unpacker.AddShaderChunk("#if " + conditional);
                            }
                            structEnd.AddShaderChunk(fieldType + " " + field.Name + semanticString + "; // unpacked");
                            packer.AddShaderChunk("output." + field.Name + " = input." + field.Name + ";");
                            unpacker.AddShaderChunk("output." + field.Name + " = input." + field.Name + ";");
                            if (conditional != null)
                            {
                                structEnd.AddShaderChunk("#endif // " + conditional);
                                packer.AddShaderChunk("#endif // " + conditional);
                                unpacker.AddShaderChunk("#endif // " + conditional);
                            }
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
                    }
                }
            }

            // add packed data declarations to struct, using the packedCounts
            for (int index = 0; index < packedCounts.Count; index++)
            {
                int count = packedCounts[index];
                result.AddShaderChunk(string.Format("{0} interp{1:00} : TEXCOORD{1}; // auto-packed", vectorTypeNames[count], index));
            }

            // add unpacked data declarations to struct (must be at end)
            result.AddGenerator(structEnd);

            // close declarations
            result.Deindent();
            result.AddShaderChunk("};");
            packer.AddShaderChunk("return output;");
            packer.Deindent();
            packer.AddShaderChunk("}");
            unpacker.AddShaderChunk("return output;");
            unpacker.Deindent();
            unpacker.AddShaderChunk("}");

            // combine all of the code into the result
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
            HashSet<string> activeFields;
            Dictionary<string, string> namedFragments;
            string templatePath;
            bool debugOutput;
            string buildTypeAssemblyNameFormat;

            // intermediates
            HashSet<string> includedFiles;

            // outputs
            ShaderStringBuilder result;
            List<string> sourceAssetDependencyPaths;

            public TemplatePreprocessor(HashSet<string> activeFields, Dictionary<string, string> namedFragments, bool debugOutput, string templatePath, List<string> sourceAssetDependencyPaths, string buildTypeAssemblyNameFormat, ShaderStringBuilder outShaderCodeResult = null)
            {
                this.activeFields = activeFields;
                this.namedFragments = namedFragments;
                this.debugOutput = debugOutput;
                this.templatePath = templatePath;
                this.sourceAssetDependencyPaths = sourceAssetDependencyPaths;
                this.buildTypeAssemblyNameFormat = buildTypeAssemblyNameFormat;
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
                            // skip a line, just to be sure we've cleaned up the current line
                            result.AppendNewLine();
                            result.AppendLine("//-------------------------------------------------------------------------------------");
                            result.AppendLine("// TEMPLATE INCLUDE : " + param.GetString());
                            result.AppendLine("//-------------------------------------------------------------------------------------");
                            ProcessTemplateFile(includeLocation);
                            result.AppendNewLine();
                            result.AppendLine("//-------------------------------------------------------------------------------------");
                            result.AppendLine("// END TEMPLATE INCLUDE : " + param.GetString());
                            result.AppendLine("//-------------------------------------------------------------------------------------");
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
                        string assemblyQualifiedTypeName = string.Format(buildTypeAssemblyNameFormat, typeName);
                        Type type = Type.GetType(assemblyQualifiedTypeName);
                        if (type == null)
                        {
                            Error("ERROR: buildType could not find type : " + typeName, command.s, param.start);
                        }
                        else
                        {
                            result.AppendLine("// Generated Type: " + typeName);
                            ShaderGenerator temp = new ShaderGenerator();
                            BuildType(type, activeFields, temp);
                            result.AppendLine(temp.GetShaderString(0, false));
                        }
                    }
                }
            }

            private bool ProcessPredicate(Token predicate, int endLine, ref int cur, ref bool appendEndln)
            {
                // eval if(param)
                string fieldName = predicate.GetString();
                int nonwhitespace = SkipWhitespace(predicate.s, predicate.end + 1, endLine);
                if (activeFields.Contains(fieldName))
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
                    if (debugOutput)
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

        public static void ApplyDependencies(HashSet<string> activeFields, List<Dependency[]> dependsList)
        {
            // add active fields to queue
            Queue<string> fieldsToPropagate = new Queue<string>();
            foreach (string f in activeFields)
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
    };



    class NewGraphAction : EndNameEditAction
    {
        AbstractMaterialNode m_Node;
        public AbstractMaterialNode node
        {
            get { return m_Node; }
            set { m_Node = value; }
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var graph = new GraphData();
            graph.AddNode(node);
            graph.path = "Shader Graphs";
            FileUtilities.WriteShaderGraphToDisk(pathName, graph);
            AssetDatabase.Refresh();

            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<Shader>(pathName);
            Selection.activeObject = obj;
        }
    }

    static class GraphUtil
    {
        internal static string ConvertCamelCase(string text, bool preserveAcronyms)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            StringBuilder newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]))
                    if ((text[i - 1] != ' ' && !char.IsUpper(text[i - 1])) ||
                        (preserveAcronyms && char.IsUpper(text[i - 1]) &&
                         i < text.Length - 1 && !char.IsUpper(text[i + 1])))
                        newText.Append(' ');
                newText.Append(text[i]);
            }
            return newText.ToString();
        }

        public static void CreateNewGraph(AbstractMaterialNode node)
        {
            var graphItem = ScriptableObject.CreateInstance<NewGraphAction>();
            graphItem.node = node;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, graphItem,
                string.Format("New Shader Graph.{0}", ShaderGraphImporter.Extension), null, null);
        }

        public static Type GetOutputNodeType(string path)
        {
            var textGraph = File.ReadAllText(path, Encoding.UTF8);
            var graph = JsonUtility.FromJson<GraphData>(textGraph);
            return graph.outputNode.GetType();
        }

        public static bool IsShaderGraph(this Shader shader)
        {
            var path = AssetDatabase.GetAssetPath(shader);
            var importer = AssetImporter.GetAtPath(path);
            return importer is ShaderGraphImporter;
        }

        public static void GeneratePropertiesBlock(ShaderStringBuilder sb, PropertyCollector propertyCollector, GenerationMode mode)
        {
            sb.AppendLine("Properties");
            using (sb.BlockScope())
            {
                foreach (var prop in propertyCollector.properties.Where(x => x.generatePropertyBlock))
                {
                    sb.AppendLine(prop.GetPropertyBlockString());
                }
            }
        }

        public static void GenerateApplicationVertexInputs(ShaderGraphRequirements graphRequiements, ShaderStringBuilder vertexInputs)
        {
            vertexInputs.AppendLine("struct GraphVertexInput");
            using (vertexInputs.BlockSemicolonScope())
            {
                vertexInputs.AppendLine("float4 vertex : POSITION;");
                if(graphRequiements.requiresNormal != NeededCoordinateSpace.None || graphRequiements.requiresBitangent != NeededCoordinateSpace.None)
                    vertexInputs.AppendLine("float3 normal : NORMAL;");
                if(graphRequiements.requiresTangent != NeededCoordinateSpace.None || graphRequiements.requiresBitangent != NeededCoordinateSpace.None)
                    vertexInputs.AppendLine("float4 tangent : TANGENT;");
                if (graphRequiements.requiresVertexColor)
                {
                    vertexInputs.AppendLine("float4 color : COLOR;");
                }
                foreach (var channel in graphRequiements.requiresMeshUVs.Distinct())
                    vertexInputs.AppendLine("float4 texcoord{0} : TEXCOORD{0};", (int)channel);
                vertexInputs.AppendLine("UNITY_VERTEX_INPUT_INSTANCE_ID");
            }
        }

        static void Visit(List<AbstractMaterialNode> outputList, Dictionary<Guid, AbstractMaterialNode> unmarkedNodes, AbstractMaterialNode node)
        {
            if (!unmarkedNodes.ContainsKey(node.guid))
                return;
            foreach (var slot in node.GetInputSlots<ISlot>())
            {
                foreach (var edge in node.owner.GetEdges(slot.slotReference))
                {
                    var inputNode = node.owner.GetNodeFromGuid(edge.outputSlot.nodeGuid);
                    Visit(outputList, unmarkedNodes, inputNode);
                }
            }
            unmarkedNodes.Remove(node.guid);
            outputList.Add(node);
        }

        public static GenerationResults GetShader(this GraphData graph, AbstractMaterialNode node, GenerationMode mode, string name)
        {
            // ----------------------------------------------------- //
            //                         SETUP                         //
            // ----------------------------------------------------- //

            // -------------------------------------
            // String builders

            var finalShader = new ShaderStringBuilder();
            var results = new GenerationResults();

            var shaderProperties = new PropertyCollector();
            var shaderPropertyUniforms = new ShaderStringBuilder();
            var functionBuilder = new ShaderStringBuilder();
            var functionRegistry = new FunctionRegistry(functionBuilder);

            var vertexDescriptionFunction = new ShaderStringBuilder(0);

            var surfaceDescriptionInputStruct = new ShaderStringBuilder(0);
            var surfaceDescriptionStruct = new ShaderStringBuilder(0);
            var surfaceDescriptionFunction = new ShaderStringBuilder(0);

            var vertexInputs = new ShaderStringBuilder(0);

            // -------------------------------------
            // Get Slot and Node lists

            var activeNodeList = ListPool<AbstractMaterialNode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, node);

            var slots = new List<MaterialSlot>();
            if (node is IMasterNode || node is SubGraphOutputNode)
                slots.AddRange(node.GetInputSlots<MaterialSlot>());
            else
            {
                var outputSlots = node.GetOutputSlots<MaterialSlot>().ToList();
                if (outputSlots.Count > 0)
                    slots.Add(outputSlots[0]);
            }

            // -------------------------------------
            // Get Requirements

            var requirements = ShaderGraphRequirements.FromNodes(activeNodeList, ShaderStageCapability.Fragment);

            // ----------------------------------------------------- //
            //                START VERTEX DESCRIPTION               //
            // ----------------------------------------------------- //

            // -------------------------------------
            // Generate Vertex Description function

            vertexDescriptionFunction.AppendLine("GraphVertexInput PopulateVertexData(GraphVertexInput v)");
            using (vertexDescriptionFunction.BlockScope())
            {
                vertexDescriptionFunction.AppendLine("return v;");
            }

            // ----------------------------------------------------- //
            //               START SURFACE DESCRIPTION               //
            // ----------------------------------------------------- //

            // -------------------------------------
            // Generate Input structure for Surface Description function
            // Surface Description Input requirements are needed to exclude intermediate translation spaces

            GenerateSurfaceInputStruct(surfaceDescriptionInputStruct, requirements, "SurfaceDescriptionInputs");

            results.previewMode = PreviewMode.Preview2D;
            foreach (var pNode in activeNodeList)
            {
                if (pNode.previewMode == PreviewMode.Preview3D)
                {
                    results.previewMode = PreviewMode.Preview3D;
                    break;
                }
            }

            // -------------------------------------
            // Generate Output structure for Surface Description function

            GenerateSurfaceDescriptionStruct(surfaceDescriptionStruct, slots, useIdsInNames: !(node is IMasterNode));

            // -------------------------------------
            // Generate Surface Description function

            GenerateSurfaceDescriptionFunction(
                activeNodeList,
                node,
                graph,
                surfaceDescriptionFunction,
                functionRegistry,
                shaderProperties,
                requirements,
                mode,
                outputIdProperty: results.outputIdProperty);

            // ----------------------------------------------------- //
            //           GENERATE VERTEX > PIXEL PIPELINE            //
            // ----------------------------------------------------- //

            // -------------------------------------
            // Property uniforms

            shaderProperties.GetPropertiesDeclaration(shaderPropertyUniforms, mode, graph.concretePrecision);

            // -------------------------------------
            // Generate Input structure for Vertex shader

            GenerateApplicationVertexInputs(requirements, vertexInputs);

            // ----------------------------------------------------- //
            //                      FINALIZE                         //
            // ----------------------------------------------------- //

            // -------------------------------------
            // Build final shader

            finalShader.AppendLine(@"Shader ""{0}""", name);
            using (finalShader.BlockScope())
            {
                GraphUtil.GeneratePropertiesBlock(finalShader, shaderProperties, mode);
                finalShader.AppendNewLine();

                finalShader.AppendLine(@"HLSLINCLUDE");
                finalShader.AppendLine(@"#include ""Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl""");
                finalShader.AppendLine(@"#include ""Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl""");
                finalShader.AppendLine(@"#include ""Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl""");
                finalShader.AppendLine(@"#include ""Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl""");
                finalShader.AppendLine(@"#include ""Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl""");
                finalShader.AppendLine(@"#include ""Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl""");
                finalShader.AppendLine(@"#include ""Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariables.hlsl""");
                finalShader.AppendLine(@"#include ""Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl""");
                finalShader.AppendLine(@"#include ""Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl""");
                finalShader.AppendLine(@"#define SHADERGRAPH_PREVIEW 1");
                finalShader.AppendNewLine();

                finalShader.AppendLines(shaderPropertyUniforms.ToString());
                finalShader.AppendNewLine();

                finalShader.AppendLines(surfaceDescriptionInputStruct.ToString());
                finalShader.AppendNewLine();

                finalShader.Concat(functionBuilder);
                finalShader.AppendNewLine();

                finalShader.AppendLines(surfaceDescriptionStruct.ToString());
                finalShader.AppendNewLine();
                finalShader.AppendLines(surfaceDescriptionFunction.ToString());
                finalShader.AppendNewLine();

                finalShader.AppendLines(vertexInputs.ToString());
                finalShader.AppendNewLine();
                finalShader.AppendLines(vertexDescriptionFunction.ToString());
                finalShader.AppendNewLine();

                finalShader.AppendLine(@"ENDHLSL");

                finalShader.AppendLines(ShaderGenerator.GetPreviewSubShader(node, requirements));
                ListPool<AbstractMaterialNode>.Release(activeNodeList);
            }

            // -------------------------------------
            // Finalize

            results.configuredTextures = shaderProperties.GetConfiguredTexutres();
            ShaderSourceMap sourceMap;
            results.shader = finalShader.ToString(out sourceMap);
            results.sourceMap = sourceMap;
            return results;
        }

        public static void GenerateSurfaceInputStruct(ShaderStringBuilder sb, ShaderGraphRequirements requirements, string structName)
        {
            sb.AppendLine($"struct {structName}");
            using (sb.BlockSemicolonScope())
            {
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresNormal, InterpolatorType.Normal, sb);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresTangent, InterpolatorType.Tangent, sb);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresBitangent, InterpolatorType.BiTangent, sb);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresViewDir, InterpolatorType.ViewDirection, sb);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresPosition, InterpolatorType.Position, sb);

                if (requirements.requiresVertexColor)
                    sb.AppendLine("float4 {0};", ShaderGeneratorNames.VertexColor);

                if (requirements.requiresScreenPosition)
                    sb.AppendLine("float4 {0};", ShaderGeneratorNames.ScreenPosition);

                if (requirements.requiresFaceSign)
                    sb.AppendLine("float {0};", ShaderGeneratorNames.FaceSign);

                foreach (var channel in requirements.requiresMeshUVs.Distinct())
                    sb.AppendLine("half4 {0};", channel.GetUVName());

                if (requirements.requiresTime)
                {
                    sb.AppendLine("float3 {0};", ShaderGeneratorNames.TimeParameters);
                }
            }
        }

        public static void GenerateSurfaceInputTransferCode(ShaderStringBuilder sb, ShaderGraphRequirements requirements, string structName, string variableName)
        {
            sb.AppendLine($"{structName} {variableName};");
            
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresNormal, InterpolatorType.Normal, sb, $"{variableName}.{{0}} = IN.{{0}};");
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresTangent, InterpolatorType.Tangent, sb, $"{variableName}.{{0}} = IN.{{0}};");
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresBitangent, InterpolatorType.BiTangent, sb, $"{variableName}.{{0}} = IN.{{0}};");
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresViewDir, InterpolatorType.ViewDirection, sb, $"{variableName}.{{0}} = IN.{{0}};");
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresPosition, InterpolatorType.Position, sb, $"{variableName}.{{0}} = IN.{{0}};");
            
            if (requirements.requiresVertexColor)
                sb.AppendLine($"{variableName}.{ShaderGeneratorNames.VertexColor} = IN.{ShaderGeneratorNames.VertexColor};");

            if (requirements.requiresScreenPosition)
                sb.AppendLine($"{variableName}.{ShaderGeneratorNames.ScreenPosition} = IN.{ShaderGeneratorNames.ScreenPosition};");

            if (requirements.requiresFaceSign)
                sb.AppendLine($"{variableName}.{ShaderGeneratorNames.FaceSign} = IN.{ShaderGeneratorNames.FaceSign};");

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
                sb.AppendLine($"{variableName}.{channel.GetUVName()} = IN.{channel.GetUVName()};");

            if (requirements.requiresTime)
            {
                sb.AppendLine($"{variableName}.{ShaderGeneratorNames.TimeParameters} = IN.{ShaderGeneratorNames.TimeParameters};");
            }
        }

        public static void GenerateSurfaceDescriptionStruct(ShaderStringBuilder surfaceDescriptionStruct, List<MaterialSlot> slots, string structName = "SurfaceDescription", HashSet<string> activeFields = null, bool useIdsInNames = false)
        {
            surfaceDescriptionStruct.AppendLine("struct {0}", structName);
            using (surfaceDescriptionStruct.BlockSemicolonScope())
            {
                foreach (var slot in slots)
                {
                    string hlslName = NodeUtils.GetHLSLSafeName(slot.shaderOutputName);
                    if (useIdsInNames)
                    {
                        hlslName = $"{hlslName}_{slot.id}";
                    }
                  
                    surfaceDescriptionStruct.AppendLine("{0} {1};", slot.concreteValueType.ToShaderString(slot.owner.concretePrecision), hlslName);

                    if (activeFields != null)
                    {
                        activeFields.Add(structName + "." + hlslName);
                    }
                }
            }
        }

        public static void GenerateSurfaceDescriptionFunction(
            List<AbstractMaterialNode> activeNodeList,
            AbstractMaterialNode rootNode,
            GraphData graph,
            ShaderStringBuilder surfaceDescriptionFunction,
            FunctionRegistry functionRegistry,
            PropertyCollector shaderProperties,
            ShaderGraphRequirements requirements,
            GenerationMode mode,
            string functionName = "PopulateSurfaceData",
            string surfaceDescriptionName = "SurfaceDescription",
            Vector1ShaderProperty outputIdProperty = null,
            IEnumerable<MaterialSlot> slots = null,
            string graphInputStructName = "SurfaceDescriptionInputs")
        {
            if (graph == null)
                return;

            GraphContext graphContext = new GraphContext(graphInputStructName);

            graph.CollectShaderProperties(shaderProperties, mode);

            surfaceDescriptionFunction.AppendLine(String.Format("{0} {1}(SurfaceDescriptionInputs IN)", surfaceDescriptionName, functionName), false);
            using (surfaceDescriptionFunction.BlockScope())
            {
                surfaceDescriptionFunction.AppendLine("{0} surface = ({0})0;", surfaceDescriptionName);
                foreach (var activeNode in activeNodeList)
                {
                    if (activeNode is IGeneratesFunction functionNode)
                    {
                        functionRegistry.builder.currentNode = activeNode;
                        functionNode.GenerateNodeFunction(functionRegistry, graphContext, mode);
                        functionRegistry.builder.ReplaceInCurrentMapping(PrecisionUtil.Token, activeNode.concretePrecision.ToShaderString());
                    }

                    if (activeNode is IGeneratesBodyCode bodyNode)
                    {
                        surfaceDescriptionFunction.currentNode = activeNode;
                        bodyNode.GenerateNodeCode(surfaceDescriptionFunction, graphContext, mode);
                        surfaceDescriptionFunction.ReplaceInCurrentMapping(PrecisionUtil.Token, activeNode.concretePrecision.ToShaderString());
                    }

                    activeNode.CollectShaderProperties(shaderProperties, mode);
                }                

                functionRegistry.builder.currentNode = null;
                surfaceDescriptionFunction.currentNode = null;

                if (rootNode is IMasterNode || rootNode is SubGraphOutputNode)
                {
                    var usedSlots = slots ?? rootNode.GetInputSlots<MaterialSlot>();
                    foreach (var input in usedSlots)
                    {
                        if (input != null)
                        {
                            var foundEdges = graph.GetEdges(input.slotReference).ToArray();
                            var hlslName = NodeUtils.GetHLSLSafeName(input.shaderOutputName);
                            if (rootNode is SubGraphOutputNode)
                            {
                                hlslName = $"{hlslName}_{input.id}";
                            }
                            if (foundEdges.Any())
                            {
                                surfaceDescriptionFunction.AppendLine("surface.{0} = {1};",
                                    hlslName,
                                    rootNode.GetSlotValue(input.id, mode, rootNode.concretePrecision));
                            }
                            else
                            {
                                surfaceDescriptionFunction.AppendLine("surface.{0} = {1};",
                                    hlslName, input.GetDefaultValue(mode, rootNode.concretePrecision));
                            }
                        }
                    }
                }
                else if (rootNode.hasPreview)
                {
                    var slot = rootNode.GetOutputSlots<MaterialSlot>().FirstOrDefault();
                    if (slot != null)
                    {
                        var hlslSafeName = $"{NodeUtils.GetHLSLSafeName(slot.shaderOutputName)}_{slot.id}";
                        surfaceDescriptionFunction.AppendLine("surface.{0} = {1};",
                            hlslSafeName, rootNode.GetSlotValue(slot.id, mode, rootNode.concretePrecision));
                    }
                }

                surfaceDescriptionFunction.AppendLine("return surface;");
            }
        }

        const string k_VertexDescriptionStructName = "VertexDescription";
        public static void GenerateVertexDescriptionStruct(ShaderStringBuilder builder, List<MaterialSlot> slots, string structName = k_VertexDescriptionStructName, HashSet<string> activeFields = null)
        {
            builder.AppendLine("struct {0}", structName);
            using (builder.BlockSemicolonScope())
            {
                foreach (var slot in slots)
                {
                    string hlslName = NodeUtils.GetHLSLSafeName(slot.shaderOutputName);
                    builder.AppendLine("{0} {1};", slot.concreteValueType.ToShaderString(slot.owner.concretePrecision), hlslName);

                    if (activeFields != null)
                    {
                        activeFields.Add(structName + "." + hlslName);
                    }
                }
            }
        }

        public static void GenerateVertexDescriptionFunction(
            GraphData graph,
            ShaderStringBuilder builder,
            FunctionRegistry functionRegistry,
            PropertyCollector shaderProperties,
            GenerationMode mode,
            List<AbstractMaterialNode> nodes,
            List<MaterialSlot> slots,
            string graphInputStructName = "VertexDescriptionInputs",
            string functionName = "PopulateVertexData",
            string graphOutputStructName = k_VertexDescriptionStructName)
        {
            if (graph == null)
                return;

            GraphContext graphContext = new GraphContext(graphInputStructName);

            graph.CollectShaderProperties(shaderProperties, mode);

            builder.AppendLine("{0} {1}({2} IN)", graphOutputStructName, functionName, graphInputStructName);
            using (builder.BlockScope())
            {
                builder.AppendLine("{0} description = ({0})0;", graphOutputStructName);
                foreach (var node in nodes)
                {
                    if (node is IGeneratesFunction generatesFunction)
                    {
                        functionRegistry.builder.currentNode = node;
                        generatesFunction.GenerateNodeFunction(functionRegistry, graphContext, mode);
                        functionRegistry.builder.ReplaceInCurrentMapping(PrecisionUtil.Token, node.concretePrecision.ToShaderString());
                    }

                    if (node is IGeneratesBodyCode generatesBodyCode)
                    {
                        builder.currentNode = node;
                        generatesBodyCode.GenerateNodeCode(builder, graphContext, mode);
                        builder.ReplaceInCurrentMapping(PrecisionUtil.Token, node.concretePrecision.ToShaderString());
                    }
                    node.CollectShaderProperties(shaderProperties, mode);
                }

                functionRegistry.builder.currentNode = null;
                builder.currentNode = null; 

                if(slots.Count != 0)
                {
                    foreach (var slot in slots)
                    {
                        var isSlotConnected = slot.owner.owner.GetEdges(slot.slotReference).Any();
                        var slotName = NodeUtils.GetHLSLSafeName(slot.shaderOutputName);
                        var slotValue = isSlotConnected ? 
                            ((AbstractMaterialNode)slot.owner).GetSlotValue(slot.id, mode, slot.owner.concretePrecision) : slot.GetDefaultValue(mode, slot.owner.concretePrecision);
                        builder.AppendLine("description.{0} = {1};", slotName, slotValue);
                    }
                }

                builder.AppendLine("return description;");
            }
        }

        public static GenerationResults GetPreviewShader(this GraphData graph, AbstractMaterialNode node)
        {
            return graph.GetShader(node, GenerationMode.Preview, String.Format("hidden/preview/{0}", node.GetVariableNameForNode()));
        }

        static Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo> s_LegacyTypeRemapping;

        public static Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo> GetLegacyTypeRemapping()
        {
            if (s_LegacyTypeRemapping == null)
            {
                s_LegacyTypeRemapping = new Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo>();
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in assembly.GetTypesOrNothing())
                    {
                        if (type.IsAbstract)
                            continue;
                        foreach (var attribute in type.GetCustomAttributes(typeof(FormerNameAttribute), false))
                        {
                            var legacyAttribute = (FormerNameAttribute)attribute;
                            var serializationInfo = new SerializationHelper.TypeSerializationInfo { fullName = legacyAttribute.fullName };
                            s_LegacyTypeRemapping[serializationInfo] = SerializationHelper.GetTypeSerializableAsString(type);
                        }
                    }
                }
            }

            return s_LegacyTypeRemapping;
        }

        /// <summary>
        /// Sanitizes a supplied string such that it does not collide
        /// with any other name in a collection.
        /// </summary>
        /// <param name="existingNames">
        /// A collection of names that the new name should not collide with.
        /// </param>
        /// <param name="duplicateFormat">
        /// The format applied to the name if a duplicate exists.
        /// This must be a format string that contains `{0}` and `{1}`
        /// once each. An example could be `{0} ({1})`, which will append ` (n)`
        /// to the name for the n`th duplicate.
        /// </param>
        /// <param name="name">
        /// The name to be sanitized.
        /// </param>
        /// <returns>
        /// A name that is distinct form any name in `existingNames`.
        /// </returns>
        internal static string SanitizeName(IEnumerable<string> existingNames, string duplicateFormat, string name)
        {
            if (!existingNames.Contains(name))
                return name;

            string escapedDuplicateFormat = Regex.Escape(duplicateFormat);

            // Escaped format will escape string interpolation, so the escape caracters must be removed for these.
            escapedDuplicateFormat = escapedDuplicateFormat.Replace(@"\{0}", @"{0}");
            escapedDuplicateFormat = escapedDuplicateFormat.Replace(@"\{1}", @"{1}");

            var baseRegex = new Regex(string.Format(escapedDuplicateFormat, @"^(.*)", @"(\d+)"));

            var baseMatch = baseRegex.Match(name);
            if (baseMatch.Success)
                name = baseMatch.Groups[1].Value;

            string baseNameExpression = string.Format(@"^{0}", Regex.Escape(name));
            var regex = new Regex(string.Format(escapedDuplicateFormat, baseNameExpression, @"(\d+)") + "$");

            var existingDuplicateNumbers = existingNames.Select(existingName => regex.Match(existingName)).Where(m => m.Success).Select(m => int.Parse(m.Groups[1].Value)).Where(n => n > 0).Distinct().ToList();

            var duplicateNumber = 1;
            existingDuplicateNumbers.Sort();
            if (existingDuplicateNumbers.Any() && existingDuplicateNumbers.First() == 1)
            {
                duplicateNumber = existingDuplicateNumbers.Last() + 1;
                for (var i = 1; i < existingDuplicateNumbers.Count; i++)
                {
                    if (existingDuplicateNumbers[i - 1] != existingDuplicateNumbers[i] - 1)
                    {
                        duplicateNumber = existingDuplicateNumbers[i - 1] + 1;
                        break;
                    }
                }
            }

            return string.Format(duplicateFormat, name, duplicateNumber);
        }

        public static bool WriteToFile(string path, string content)
        {
            try
            {
                File.WriteAllText(path, content);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }

        static ProcessStartInfo CreateProcessStartInfo(string filePath)
        {
            string externalScriptEditor = ScriptEditorUtility.GetExternalScriptEditor();

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.UseShellExecute = false;


        #if UNITY_EDITOR_OSX
            string arg = string.Format("-a \"{0}\" -n --args \"{1}\"", externalScriptEditor, Path.GetFullPath(filePath));
            psi.FileName = "open";
            psi.Arguments = arg;
        #else
            psi.Arguments = Path.GetFileName(filePath);
            psi.WorkingDirectory = Path.GetDirectoryName(filePath);
            psi.FileName = externalScriptEditor;
        #endif
            return psi;
        }

        public static void OpenFile(string path)
        {
            string filePath = Path.GetFullPath(path);
            if (!File.Exists(filePath))
            {
                Debug.LogError(string.Format("Path {0} doesn't exists", path));
                return;
            }

            string externalScriptEditor = ScriptEditorUtility.GetExternalScriptEditor();
            if (externalScriptEditor != "internal")
            {
                ProcessStartInfo psi = CreateProcessStartInfo(filePath);
                Process.Start(psi);
            }
            else
            {
                Process p = new Process();
                p.StartInfo.FileName = filePath;
                p.EnableRaisingEvents = true;
                p.Exited += (Object obj, EventArgs args) =>
                {
                    if(p.ExitCode != 0)
                        Debug.LogWarningFormat("Unable to open {0}: Check external editor in preferences", filePath);
                };
                p.Start();
            }
        }
    }
}
