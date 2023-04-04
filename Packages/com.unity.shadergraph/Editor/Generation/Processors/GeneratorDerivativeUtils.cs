using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
using Pool = UnityEngine.Pool;
using UnityEngine.Profiling;

namespace UnityEditor.ShaderGraph
{
    internal class GeneratorDerivativeUtils
    {
        public static FieldDescriptor uv0Ddx = new FieldDescriptor(StructFields.SurfaceDescriptionInputs.name, "uv0Ddx", "", ShaderValueType.Float4,
            subscriptOptions: StructFieldOptions.Optional);
        public static FieldDescriptor uv0Ddy = new FieldDescriptor(StructFields.SurfaceDescriptionInputs.name, "uv0Ddy", "", ShaderValueType.Float4,
            subscriptOptions: StructFieldOptions.Optional);
        public static FieldDescriptor uv1Ddx = new FieldDescriptor(StructFields.SurfaceDescriptionInputs.name, "uv1Ddx", "", ShaderValueType.Float4,
            subscriptOptions: StructFieldOptions.Optional);
        public static FieldDescriptor uv1Ddy = new FieldDescriptor(StructFields.SurfaceDescriptionInputs.name, "uv1Ddy", "", ShaderValueType.Float4,
            subscriptOptions: StructFieldOptions.Optional);
        public static FieldDescriptor uv2Ddx = new FieldDescriptor(StructFields.SurfaceDescriptionInputs.name, "uv2Ddx", "", ShaderValueType.Float4,
            subscriptOptions: StructFieldOptions.Optional);
        public static FieldDescriptor uv2Ddy = new FieldDescriptor(StructFields.SurfaceDescriptionInputs.name, "uv2Ddy", "", ShaderValueType.Float4,
            subscriptOptions: StructFieldOptions.Optional);
        public static FieldDescriptor uv3Ddx = new FieldDescriptor(StructFields.SurfaceDescriptionInputs.name, "uv3Ddx", "", ShaderValueType.Float4,
            subscriptOptions: StructFieldOptions.Optional);
        public static FieldDescriptor uv3Ddy = new FieldDescriptor(StructFields.SurfaceDescriptionInputs.name, "uv3Ddy", "", ShaderValueType.Float4,
            subscriptOptions: StructFieldOptions.Optional);

        #region GeneratePassStructsAndInterpolators
        // This funcion is an exact copy of the lines in Generator.cs, except the the member value m_HumanReadable is replaced with the
        // variable isHumanReadable so that this function can be static.
        internal static void GeneratePassStructsAndInterpolators(out ShaderStringBuilder interpolatorBuilder, out ShaderStringBuilder passStructBuilder, ActiveFields activeFields, List<StructDescriptor> passStructs, bool isHumanReadable)
        {
            // -----------------------------
            // Generated structs and Packing code
            Profiler.BeginSample("StructsAndPacking");
            interpolatorBuilder = new ShaderStringBuilder(humanReadable: isHumanReadable);
            if (passStructs != null)
            {
                var packedStructs = new List<StructDescriptor>();
                foreach (var shaderStruct in passStructs)
                {
                    if (shaderStruct.packFields == false)
                        continue; //skip structs that do not need interpolator packs

                    List<int> packedCounts = new List<int>();
                    var packStruct = new StructDescriptor();

                    //generate packed functions
                    if (activeFields.permutationCount > 0)
                    {
                        var generatedPackedTypes = new Dictionary<string, (ShaderStringBuilder, List<int>)>();
                        foreach (var instance in activeFields.allPermutations.instances)
                        {
                            var instanceGenerator = new ShaderStringBuilder();
                            GenerationUtils.GenerateInterpolatorFunctions(shaderStruct, instance, isHumanReadable, out instanceGenerator);
                            var key = instanceGenerator.ToCodeBlock();
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
                                interpolatorBuilder.AppendLine(KeywordUtil.GetKeywordPermutationSetConditional(generated.Value.Item2));
                            }
                            else
                                interpolatorBuilder.AppendLine(KeywordUtil.GetKeywordPermutationSetConditional(generated.Value.Item2).Replace("#if", "#elif"));

                            //interpolatorBuilder.Concat(generated.Value.Item1);
                            interpolatorBuilder.AppendLines(generated.Value.Item1.ToString());
                        }
                        if (generatedPackedTypes.Count > 0)
                            interpolatorBuilder.AppendLine("#endif");
                    }
                    else
                    {
                        ShaderStringBuilder localInterpolatorBuilder; // GenerateInterpolatorFunctions do the allocation
                        GenerationUtils.GenerateInterpolatorFunctions(shaderStruct, activeFields.baseInstance, isHumanReadable, out localInterpolatorBuilder);
                        interpolatorBuilder.Concat(localInterpolatorBuilder);
                    }
                    //using interp index from functions, generate packed struct descriptor
                    GenerationUtils.GeneratePackedStruct(shaderStruct, activeFields, out packStruct);
                    packedStructs.Add(packStruct);
                }
                passStructs.AddRange(packedStructs);
            }
            if (interpolatorBuilder.length != 0) //hard code interpolators to float, TODO: proper handle precision
                interpolatorBuilder.ReplaceInCurrentMapping(PrecisionUtil.Token, ConcretePrecision.Single.ToShaderString());
            else
                interpolatorBuilder.AppendLine("//Interpolator Packs: <None>");
            Profiler.EndSample();

            // Generated String Builders for all struct types
            Profiler.BeginSample("StructTypes");
            {
                passStructBuilder = new ShaderStringBuilder(humanReadable: isHumanReadable);
                if (passStructs != null)
                {
                    var structBuilder = new ShaderStringBuilder(humanReadable: isHumanReadable);
                    foreach (StructDescriptor shaderStruct in passStructs)
                    {
                        GenerationUtils.GenerateShaderStruct(shaderStruct, activeFields, isHumanReadable, out structBuilder);
                        structBuilder.ReplaceInCurrentMapping(PrecisionUtil.Token, ConcretePrecision.Single.ToShaderString()); //hard code structs to float, TODO: proper handle precision
                        passStructBuilder.Concat(structBuilder);
                    }
                }
                if (passStructBuilder.length == 0)
                    passStructBuilder.AppendLine("//Pass Structs: <None>");
            }
            Profiler.EndSample();
        }
        #endregion

        // Fundamentally, this function takes as input the varios in-progress structs/code, parses the results, and splices in the partial derivatives.
        // The "outputs" override the existing values where necessary.
        // Outputs:
        //    1. spliceCommands
        //         - Overwrites the "InterpolatorPack" and "PassStructs" splice points.
        //    2. neededUvDerivatives
        //         - Specificies which uvs need derivatives which will be put into the requirements structure so that we can output
        //           the correct #defines which cause the ddx/ddy interpolators to be generated.
        internal static void ParseAndModifyForAnalyticDerivatives(
            Target target,
            Dictionary<string, string> spliceCommands,
            bool[] neededUvDerivs,
            PassDescriptor pass,
            ActiveFields activeFields,
            PropertyCollector subShaderProperties,
            PropertyCollector propertyCollector,
            KeywordCollector keywordCollector,
            List<AbstractMaterialNode> vertexNodes,
            List<AbstractMaterialNode> pixelNodes,
            List<int>[] vertexNodePermutations,
            List<int>[] pixelNodePermutations,
            List<StructDescriptor> originalPassStructs,
            bool applyEmulatedDerivatives,
            bool isHumanReadable,
            string primaryShaderFullName,
            ConcretePrecision graphDefaultConcretePrecision)
        {
            // we need a list of the user custom funcs in order to ignore them during parsing
            List<string> customFuncs = new List<string>();
            for (int i = 0; i < pixelNodes.Count; i++)
            {
                if (pixelNodes[i] is CustomFunctionNode customNode)
                {
                    string[] precNames = { "float", "half" };

                    for (int precIter = 0; precIter < 2; precIter++)
                    {
                        string currFunc = customNode.hlslFunctionName;
                        currFunc = currFunc.Replace("$precision", precNames[precIter]);
                        customFuncs.Add(currFunc);
                    }
                }
            }

            // preview mode so that we get the global structs directly instead of as dots macros
            var propertyBuilder = new ShaderStringBuilder(humanReadable: isHumanReadable);
            subShaderProperties.GetPropertiesDeclaration(propertyBuilder, GenerationMode.Preview, graphDefaultConcretePrecision);

            string propStr = propertyBuilder.ToString();

            string surfaceDescStr;
            {
                StructDescriptor structDesc = new StructDescriptor();
                foreach (var item in pass.structs)
                {
                    if (item.descriptor.name == "SurfaceDescriptionInputs")
                    {
                        structDesc = item.descriptor;
                    }
                }

                // follow the same rules as above
                var structBuilder = new ShaderStringBuilder(humanReadable: isHumanReadable);
                GenerationUtils.GenerateShaderStruct(structDesc, activeFields, isHumanReadable, out structBuilder);
                structBuilder.ReplaceInCurrentMapping(PrecisionUtil.Token, ConcretePrecision.Single.ToShaderString()); //hard code structs to float, TODO: proper handle precision

                surfaceDescStr = structBuilder.ToCodeBlock();
            }

            string graphFuncStr = spliceCommands["GraphFunctions"];
            string graphPixelStr = spliceCommands["GraphPixel"];

            string dstGraphFunctions;
            string dstGraphPixel;
            bool[] adjustedUvDerivs;

            bool success = target.DerivativeModificationCallback(
                out dstGraphFunctions,
                out dstGraphPixel,
                out adjustedUvDerivs,
                primaryShaderFullName,
                pass.displayName,
                propStr,
                surfaceDescStr,
                graphFuncStr,
                graphPixelStr,
                customFuncs,
                applyEmulatedDerivatives);

            if (success)
            {
                // use the new uv derivative needs to produce text
                ShaderGraphRequirementsPerKeyword graphRequirements = new ShaderGraphRequirementsPerKeyword();
                GenerationUtils.GetActiveFieldsAndPermutationsForNodes(pass, keywordCollector, vertexNodes, pixelNodes, adjustedUvDerivs,
                    vertexNodePermutations, pixelNodePermutations, activeFields, out graphRequirements);

                ShaderStringBuilder interpolatorBuilder = new ShaderStringBuilder();
                ShaderStringBuilder passStructBuilder = new ShaderStringBuilder();

                // start with the original structs, and add interpolators, which might include uv0Ddx, uv0Ddy, etc.
                List<StructDescriptor> adjustedPassStructs = new List<StructDescriptor>(originalPassStructs);
                GeneratePassStructsAndInterpolators(out interpolatorBuilder, out passStructBuilder, activeFields, adjustedPassStructs, isHumanReadable);

                string dstInterpolatorPack = interpolatorBuilder.ToCodeBlock();
                string dstPassStructs = passStructBuilder.ToCodeBlock();

                System.Array.Copy(adjustedUvDerivs, neededUvDerivs, neededUvDerivs.Length);
                spliceCommands["InterpolatorPack"] = dstInterpolatorPack;
                spliceCommands["PassStructs"] = dstPassStructs;
                spliceCommands["GraphFunctions"] = dstGraphFunctions;
                spliceCommands["GraphPixel"] = dstGraphPixel;
            }
        }

        internal static void ApplyAnalyticDerivatives(
            Target target,
            Dictionary<string, string> spliceCommands,
            PassDescriptor pass,
            ActiveFields activeFields,
            PropertyCollector subShaderProperties,
            PropertyCollector propertyCollector,
            KeywordCollector keywordCollector,
            List<AbstractMaterialNode> vertexNodes,
            List<AbstractMaterialNode> pixelNodes,
            List<int>[] vertexNodePermutations,
            List<int>[] pixelNodePermutations,
            List<StructDescriptor> originalPassStructs,
            bool applyEmulatedDerivatives,
            bool isHumanReadable,
            string primaryShaderFullName,
            ConcretePrecision graphDefaultConcretePrecision)
        {
            bool[] neededUvDerivs = new bool[4];
            // if we have any keywords per-node, then the generated code has #ifdefs and is unparsable
            bool isValidForDerivatives = (keywordCollector.keywords.Count == 0 && keywordCollector.permutations.Count == 0);
            if (isValidForDerivatives && pass.analyticDerivativesEnabled)
            {
                GeneratorDerivativeUtils.ParseAndModifyForAnalyticDerivatives(
                    target,
                    spliceCommands,
                    neededUvDerivs,
                    pass,
                    activeFields,
                    subShaderProperties,
                    propertyCollector,
                    keywordCollector,
                    vertexNodes,
                    pixelNodes,
                    vertexNodePermutations,
                    pixelNodePermutations,
                    originalPassStructs,
                    pass.analyticDerivativesApplyEmulate,
                    isHumanReadable,
                    primaryShaderFullName,
                    graphDefaultConcretePrecision);
            }

            Profiler.BeginSample("InputDerivativeDefines");
            using (var passTokenBuilder = new ShaderStringBuilder(humanReadable: isHumanReadable))
            {
                // check for needed derivatives
                for (int i = 0; i < neededUvDerivs.Length; i++)
                {
                    if (neededUvDerivs[i])
                    {
                        passTokenBuilder.AppendLine("#define FRAG_INPUTS_USE_TEXCOORD{0}_DERIV", i.ToString());
                    }
                }

                string command = GenerationUtils.GetSpliceCommand(passTokenBuilder.ToCodeBlock(), "InputDerivativeDefines");
                spliceCommands.Add("InputDerivativeDefines", command);
            }
            Profiler.EndSample();
        }
    }
}
