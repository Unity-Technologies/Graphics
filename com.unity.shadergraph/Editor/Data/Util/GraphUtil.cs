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
using UnityEngine;
using Debug = UnityEngine.Debug;
using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    // a structure used to track active variable dependencies in the shader code
    // (i.e. the use of uv0 in the pixel shader means we need a uv0 interpolator, etc.)
    public struct Dependency
    {
        public string name;             // the name of the thing
        public string dependsOn;        // the thing above depends on this -- it reads it / calls it / requires it to be defined

        public Dependency(string name, string dependsOn)
        {
            this.name = name;
            this.dependsOn = dependsOn;
        }
    };

    // attribute used to flag a field as needing an HLSL semantic applied
    // i.e.    float3 position : POSITION;
    //                           ^ semantic
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class Semantic : System.Attribute
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
    public class Optional : System.Attribute
    {
        public Optional()
        {
        }
    }

    // attribute used to override the HLSL type of a field with a custom type string
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class OverrideType : System.Attribute
    {
        public string typeName;

        public OverrideType(string typeName)
        {
            this.typeName = typeName;
        }
    }

    // attribute used to disable a field using a preprocessor #if
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class PreprocessorIf : System.Attribute
    {
        public string conditional;

        public PreprocessorIf(string conditional)
        {
            this.conditional = conditional;
        }
    }

    public static class ShaderSpliceUtil
    {
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

        private static string[] vectorTypeNames =
        {
            "unknown",
            "float",
            "float2",
            "float3",
            "float4"
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

        private static string GetFieldType(FieldInfo field, out int floatVectorCount)
        {
            string fieldType;
            object[] overrideType = field.GetCustomAttributes(typeof(OverrideType), false);
            if (overrideType.Length > 0)
            {
                OverrideType first = (OverrideType)overrideType[0];
                fieldType = first.typeName;
                floatVectorCount = 0;
            }
            else
            {
                // TODO: handle non-float types
                floatVectorCount = GetFloatVectorCount(field.FieldType.Name);
                fieldType = vectorTypeNames[floatVectorCount];
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
                        int floatVectorCount;
                        string fieldType = GetFieldType(field, out floatVectorCount);
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

        // an easier to use version of substring Append() -- explicit inclusion on each end, and checks for positive length
        private static void AppendSubstring(System.Text.StringBuilder target, string str, int start, bool includeStart, int end, bool includeEnd)
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
                target.Append(str, start, count);
            }
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

        public static System.Text.StringBuilder PreprocessShaderCode(string code, HashSet<string> activeFields, Dictionary<string, string> namedFragments, System.Text.StringBuilder result, bool debugOutput)
        {
            if (result == null)
            {
                result = new System.Text.StringBuilder();
            }

            int cur = 0;
            int end = code.Length;
            bool skipEndln = false;

            while (cur < end)
            {
                int dollar = code.IndexOf('$', cur);
                if (dollar < 0)
                {
                    // no escape sequence found -- just append the remaining part of the code verbatim
                    AppendSubstring(result, code, cur, true, end, false);
                    cur = end;
                }
                else
                {
                    // found $ escape sequence

                    // find the end of the line (or if none found, the end of the code)
                    int endln = code.IndexOf('\n', dollar + 1);
                    if (endln < 0)
                    {
                        endln = end;
                    }

                    // see if the character after '$' is '{', which would indicate a named fragment splice
                    if ((dollar + 1 < endln) && (code[dollar + 1] == '{'))
                    {
                        // named fragment splice
                        // search for the '}' within the current line
                        int curlystart = dollar + 1;
                        int curlyend = -1;
                        if (endln > curlystart + 1)
                        {
                            curlyend = code.IndexOf('}', curlystart + 1, endln - curlystart - 1);
                        }

                        int nameLength = curlyend - dollar + 1;
                        if ((curlyend < 0) || (nameLength <= 0))
                        {
                            // no } found, or zero length name                            

                            // append everything before the beginning of the escape sequence
                            AppendSubstring(result, code, cur, true, dollar, false);

                            if (curlyend < 0)
                            {
                                result.Append("// ERROR: unterminated escape sequence ('${' and '}' must be matched)\n");
                            }
                            else
                            {
                                result.Append("// ERROR: name '${}' is empty\n");
                            }

                            // append the line (commented out) for context
                            result.Append("//    ");
                            AppendSubstring(result, code, dollar, true, endln, false);
                            result.Append("\n");
                        }
                        else
                        {
                            // } found!
                            // ugh, this probably allocates memory -- wish we could do the name lookup direct from a substring
                            string name = code.Substring(dollar, nameLength);

                            // append everything before the beginning of the escape sequence
                            AppendSubstring(result, code, cur, true, dollar, false);

                            string fragment;
                            if ((namedFragments != null) && namedFragments.TryGetValue(name, out fragment))
                            {
                                // splice the fragment
                                result.Append(fragment);
                                // advance to just after the '}'
                                cur = curlyend + 1;
                            }
                            else
                            {
                                // no named fragment found
                                result.AppendFormat("/* Could not find named fragment '{0}' */", name);
                                cur = curlyend + 1;
                            }
                        }
                    }
                    else
                    {
                        // it's a predicate
                        // search for the colon within the current line
                        int colon = -1;
                        if (endln > dollar + 1)
                        {
                            colon = code.IndexOf(':', dollar + 1, endln - dollar - 1);
                        }

                        int predicateLength = colon - dollar - 1;
                        if ((colon < 0) || (predicateLength <= 0))
                        {
                            // no colon found... error!  Spit out error and context

                            // append everything before the beginning of the escape sequence
                            AppendSubstring(result, code, cur, true, dollar, false);

                            if (colon < 0)
                            {
                                result.Append("// ERROR: unterminated escape sequence ('$' and ':' must be matched)\n");
                            }
                            else
                            {
                                result.Append("// ERROR: predicate is zero length\n");
                            }

                            // append the line (commented out) for context
                            result.Append("//    ");
                            AppendSubstring(result, code, dollar, true, endln, false);
                        }
                        else
                        {
                            // colon found!
                            // ugh, this probably allocates memory -- wish we could do the field lookup direct from a substring
                            string predicate = code.Substring(dollar + 1, predicateLength);
                            int nonwhitespace = SkipWhitespace(code, colon + 1, endln);
                            if (activeFields.Contains(predicate))
                            {
                                // append everything before the beginning of the escape sequence
                                AppendSubstring(result, code, cur, true, dollar, false);

                                // predicate is active, append the line
                                AppendSubstring(result, code, nonwhitespace, true, endln, false);
                            }
                            else
                            {
                                // predicate is not active
                                if (debugOutput)
                                {
                                    // append everything before the beginning of the escape sequence
                                    AppendSubstring(result, code, cur, true, dollar, false);
                                    result.Append("// ");
                                    AppendSubstring(result, code, nonwhitespace, true, endln, false);
                                }
                                else
                                {
                                    skipEndln = true;
                                }
                            }
                        }
                        cur = endln + 1;
                    }
                }
            }

            if (!skipEndln)
            {
                result.AppendLine();
            }

            return result;
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

    public static class GraphUtil
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

        public static void GenerateApplicationVertexInputs(ShaderGraphRequirements graphRequiements, ShaderStringBuilder vertexInputs)
        {
            vertexInputs.AppendLine("struct GraphVertexInput");
            using (vertexInputs.BlockSemicolonScope())
            {
                vertexInputs.AppendLine("float4 vertex : POSITION;");
                vertexInputs.AppendLine("float3 normal : NORMAL;");
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

        static void Visit(List<INode> outputList, Dictionary<Guid, INode> unmarkedNodes, INode node)
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

        public static GenerationResults GetShader(this AbstractMaterialGraph graph, AbstractMaterialNode node, GenerationMode mode, string name)
        {
            // ----------------------------------------------------- //
            //                         SETUP                         //
            // ----------------------------------------------------- //

            // -------------------------------------
            // String builders

            var finalShader = new ShaderStringBuilder();
            var results = new GenerationResults();
            bool isUber = node == null;

            var shaderProperties = new PropertyCollector();
            var functionBuilder = new ShaderStringBuilder();
            var functionRegistry = new FunctionRegistry(functionBuilder);

            var vertexDescriptionFunction = new ShaderStringBuilder(0);

            var surfaceDescriptionInputStruct = new ShaderStringBuilder(0);
            var surfaceDescriptionStruct = new ShaderStringBuilder(0);
            var surfaceDescriptionFunction = new ShaderStringBuilder(0);

            var vertexInputs = new ShaderStringBuilder(0);

            // -------------------------------------
            // Get Slot and Node lists

            var activeNodeList = ListPool<INode>.Get();
            if (isUber)
            {
                var unmarkedNodes = graph.GetNodes<INode>().Where(x => !(x is IMasterNode)).ToDictionary(x => x.guid);
                while (unmarkedNodes.Any())
                {
                    var unmarkedNode = unmarkedNodes.FirstOrDefault();
                    Visit(activeNodeList, unmarkedNodes, unmarkedNode.Value);
                }
            }
            else
            {
                NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, node);
            }

            var slots = new List<MaterialSlot>();
            foreach (var activeNode in isUber ? activeNodeList.Where(n => ((AbstractMaterialNode)n).hasPreview) : ((INode)node).ToEnumerable())
            {
                if (activeNode is IMasterNode || activeNode is SubGraphOutputNode)
                    slots.AddRange(activeNode.GetInputSlots<MaterialSlot>());
                else
                    slots.AddRange(activeNode.GetOutputSlots<MaterialSlot>());
            }

            // -------------------------------------
            // Get Requirements

            var requirements = ShaderGraphRequirements.FromNodes(activeNodeList, ShaderStageCapability.Fragment);

            // -------------------------------------
            // Add preview shader output property

            results.outputIdProperty = new Vector1ShaderProperty
            {
                displayName = "OutputId",
                generatePropertyBlock = false,
                value = -1
            };
            if (isUber)
                shaderProperties.AddShaderProperty(results.outputIdProperty);

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

            surfaceDescriptionInputStruct.AppendLine("struct SurfaceDescriptionInputs");
            using (surfaceDescriptionInputStruct.BlockSemicolonScope())
            {
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresNormal, InterpolatorType.Normal, surfaceDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresTangent, InterpolatorType.Tangent, surfaceDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresBitangent, InterpolatorType.BiTangent, surfaceDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresViewDir, InterpolatorType.ViewDirection, surfaceDescriptionInputStruct);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresPosition, InterpolatorType.Position, surfaceDescriptionInputStruct);

                if (requirements.requiresVertexColor)
                    surfaceDescriptionInputStruct.AppendLine("float4 {0};", ShaderGeneratorNames.VertexColor);

                if (requirements.requiresScreenPosition)
                    surfaceDescriptionInputStruct.AppendLine("float4 {0};", ShaderGeneratorNames.ScreenPosition);

                if (requirements.requiresFaceSign)
                    surfaceDescriptionInputStruct.AppendLine("float {0};", ShaderGeneratorNames.FaceSign);

                results.previewMode = PreviewMode.Preview3D;
                if (!isUber)
                {
                    foreach (var pNode in activeNodeList.OfType<AbstractMaterialNode>())
                    {
                        if (pNode.previewMode == PreviewMode.Preview3D)
                        {
                            results.previewMode = PreviewMode.Preview3D;
                            break;
                        }
                    }
                }

                foreach (var channel in requirements.requiresMeshUVs.Distinct())
                    surfaceDescriptionInputStruct.AppendLine("half4 {0};", channel.GetUVName());
            }

            // -------------------------------------
            // Generate Output structure for Surface Description function

            GenerateSurfaceDescriptionStruct(surfaceDescriptionStruct, slots, !isUber);

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
                finalShader.AppendLine("Properties");
                using (finalShader.BlockScope())
                {
                    finalShader.AppendLines(shaderProperties.GetPropertiesBlock(0));
                }
                finalShader.AppendNewLine();

                finalShader.AppendLine(@"HLSLINCLUDE");
                finalShader.AppendLine("#define USE_LEGACY_UNITY_MATRIX_VARIABLES");
                finalShader.AppendLine(@"#include ""CoreRP/ShaderLibrary/Common.hlsl""");
                finalShader.AppendLine(@"#include ""CoreRP/ShaderLibrary/Packing.hlsl""");
                finalShader.AppendLine(@"#include ""CoreRP/ShaderLibrary/Color.hlsl""");
                finalShader.AppendLine(@"#include ""CoreRP/ShaderLibrary/UnityInstancing.hlsl""");
                finalShader.AppendLine(@"#include ""CoreRP/ShaderLibrary/EntityLighting.hlsl""");
                finalShader.AppendLine(@"#include ""ShaderGraphLibrary/ShaderVariables.hlsl""");
                finalShader.AppendLine(@"#include ""ShaderGraphLibrary/ShaderVariablesFunctions.hlsl""");
                finalShader.AppendLine(@"#include ""ShaderGraphLibrary/Functions.hlsl""");
                finalShader.AppendNewLine();

                finalShader.AppendLines(shaderProperties.GetPropertiesDeclaration(0));

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
                ListPool<INode>.Release(activeNodeList);
            }

            // -------------------------------------
            // Finalize

            results.configuredTextures = shaderProperties.GetConfiguredTexutres();
            ShaderSourceMap sourceMap;
            results.shader = finalShader.ToString(out sourceMap);
            results.sourceMap = sourceMap;
            return results;
        }

        public static void GenerateSurfaceDescriptionStruct(ShaderStringBuilder surfaceDescriptionStruct, List<MaterialSlot> slots, bool isMaster, string structName = "SurfaceDescription", HashSet<string> activeFields = null)
        {
            surfaceDescriptionStruct.AppendLine("struct {0}", structName);
            using (surfaceDescriptionStruct.BlockSemicolonScope())
            {
                if (isMaster)
                {
                    foreach (var slot in slots)
                    {
                        string hlslName = NodeUtils.GetHLSLSafeName(slot.shaderOutputName);
                        surfaceDescriptionStruct.AppendLine("{0} {1};",
                            NodeUtils.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, slot.concreteValueType),
                            hlslName);

                        if (activeFields != null)
                        {
                            activeFields.Add(structName + "." + hlslName);
                        }
                    }
                }
                else
                {
                    surfaceDescriptionStruct.AppendLine("float4 PreviewOutput;");
                    if (activeFields != null)
                    {
                        activeFields.Add(structName + ".PreviewOutput");
                    }
                }
            }
        }

        public static void GenerateSurfaceDescriptionFunction(
            List<INode> activeNodeList,
            AbstractMaterialNode masterNode,
            AbstractMaterialGraph graph,
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
                ShaderGenerator sg = new ShaderGenerator();
                surfaceDescriptionFunction.AppendLine("{0} surface = ({0})0;", surfaceDescriptionName);
                foreach (var activeNode in activeNodeList.OfType<AbstractMaterialNode>())
                {
                    if (activeNode is IGeneratesFunction)
                    {
                        functionRegistry.builder.currentNode = activeNode;
                        (activeNode as IGeneratesFunction).GenerateNodeFunction(functionRegistry, graphContext, mode);
                    }
                    if (activeNode is IGeneratesBodyCode)
                        (activeNode as IGeneratesBodyCode).GenerateNodeCode(sg, mode);
                    if (masterNode == null && activeNode.hasPreview)
                    {
                        var outputSlot = activeNode.GetOutputSlots<MaterialSlot>().FirstOrDefault();
                        if (outputSlot != null)
                            sg.AddShaderChunk(String.Format("if ({0} == {1}) {{ surface.PreviewOutput = {2}; return surface; }}", outputIdProperty.referenceName, activeNode.tempId.index, ShaderGenerator.AdaptNodeOutputForPreview(activeNode, outputSlot.id, activeNode.GetVariableNameForSlot(outputSlot.id))), false);
                    }

                    // In case of the subgraph output node, the preview is generated
                    // from the first input to the node.
                    if (activeNode is SubGraphOutputNode)
                    {
                        var inputSlot = activeNode.GetInputSlots<MaterialSlot>().FirstOrDefault();
                        if (inputSlot != null)
                        {
                            var foundEdges = graph.GetEdges(inputSlot.slotReference).ToArray();
                            string slotValue = foundEdges.Any() ? activeNode.GetSlotValue(inputSlot.id, mode) : inputSlot.GetDefaultValue(mode);
                            sg.AddShaderChunk(String.Format("if ({0} == {1}) {{ surface.PreviewOutput = {2}; return surface; }}", outputIdProperty.referenceName, activeNode.tempId.index, slotValue), false);
                        }
                    }

                    activeNode.CollectShaderProperties(shaderProperties, mode);
                }
                surfaceDescriptionFunction.AppendLines(sg.GetShaderString(0));
                functionRegistry.builder.currentNode = null;

                if (masterNode != null)
                {
                    if (masterNode is IMasterNode)
                    {
                        var usedSlots = slots ?? masterNode.GetInputSlots<MaterialSlot>();
                        foreach (var input in usedSlots)
                        {
                            if (input != null)
                            {
                                var foundEdges = graph.GetEdges(input.slotReference).ToArray();
                                if (foundEdges.Any())
                                {
                                    surfaceDescriptionFunction.AppendLine("surface.{0} = {1};", NodeUtils.GetHLSLSafeName(input.shaderOutputName), masterNode.GetSlotValue(input.id, mode));
                                }
                                else
                                {
                                    surfaceDescriptionFunction.AppendLine("surface.{0} = {1};", NodeUtils.GetHLSLSafeName(input.shaderOutputName), input.GetDefaultValue(mode));
                                }
                            }
                        }
                    }
                    else if (masterNode.hasPreview)
                    {
                        foreach (var slot in masterNode.GetOutputSlots<MaterialSlot>())
                            surfaceDescriptionFunction.AppendLine("surface.{0} = {1};", NodeUtils.GetHLSLSafeName(slot.shaderOutputName), masterNode.GetSlotValue(slot.id, mode));
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
                    builder.AppendLine("{0} {1};",
                        NodeUtils.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, slot.concreteValueType),
                        hlslName);

                    if (activeFields != null)
                    {
                        activeFields.Add(structName + "." + hlslName);
                    }
                }
            }
        }

        public static void GenerateVertexDescriptionFunction(
            AbstractMaterialGraph graph,
            ShaderStringBuilder builder,
            FunctionRegistry functionRegistry,
            PropertyCollector shaderProperties,
            GenerationMode mode,
            List<INode> nodes,
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
                ShaderGenerator sg = new ShaderGenerator();
                builder.AppendLine("{0} description = ({0})0;", graphOutputStructName);
                foreach (var node in nodes.OfType<AbstractMaterialNode>())
                {
                    var generatesFunction = node as IGeneratesFunction;
                    if (generatesFunction != null)
                    {
                        functionRegistry.builder.currentNode = node;
                        generatesFunction.GenerateNodeFunction(functionRegistry, graphContext, mode);
                    }
                    var generatesBodyCode = node as IGeneratesBodyCode;
                    if (generatesBodyCode != null)
                    {
                        generatesBodyCode.GenerateNodeCode(sg, mode);
                    }
                    node.CollectShaderProperties(shaderProperties, mode);
                }
                builder.AppendLines(sg.GetShaderString(0));
                foreach (var slot in slots)
                {
                    var isSlotConnected = slot.owner.owner.GetEdges(slot.slotReference).Any();
                    var slotName = NodeUtils.GetHLSLSafeName(slot.shaderOutputName);
                    var slotValue = isSlotConnected ? ((AbstractMaterialNode)slot.owner).GetSlotValue(slot.id, mode) : slot.GetDefaultValue(mode);
                    builder.AppendLine("description.{0} = {1};", slotName, slotValue);
                }
                builder.AppendLine("return description;");
            }
        }

        public static GenerationResults GetPreviewShader(this AbstractMaterialGraph graph, AbstractMaterialNode node)
        {
            return graph.GetShader(node, GenerationMode.Preview, String.Format("hidden/preview/{0}", node.GetVariableNameForNode()));
        }

        public static GenerationResults GetUberColorShader(this AbstractMaterialGraph graph)
        {
            return graph.GetShader(null, GenerationMode.Preview, "hidden/preview");
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

        public static void OpenFile(string path)
        {
            if (!File.Exists(Path.GetFullPath(path)))
            {
                Debug.LogError(string.Format("Path {0} doesn't exists", path));
                return;
            }

            string file = Path.GetFullPath(path);
            ProcessStartInfo pi = new ProcessStartInfo(file);
            pi.Arguments = Path.GetFileName(file);
            pi.UseShellExecute = true;
            pi.WorkingDirectory = Path.GetDirectoryName(file);
            pi.FileName = ScriptEditorUtility.GetExternalScriptEditor();
            pi.Verb = "OPEN";
            Process.Start(pi);
        }
    }
}
