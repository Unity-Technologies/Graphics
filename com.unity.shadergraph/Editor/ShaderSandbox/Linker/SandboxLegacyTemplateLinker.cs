using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderFoundry;
using BlockProperty = UnityEditor.ShaderFoundry.BlockVariable;

namespace UnityEditor.ShaderFoundry
{
    internal class SandboxLegacyTemplateLinker : ITemplateLinker
    {
        internal AssetCollection m_assetCollection = new AssetCollection();

        ShaderContainer Container;

        UnityEditor.ShaderGraph.Target m_LegacyTarget;
        UnityEditor.ShaderGraph.SubShaderDescriptor m_LegacySubShader;

        internal SandboxLegacyTemplateLinker(AssetCollection assetCollection)
        {
            m_assetCollection = assetCollection;
        }

        internal void SetLegacy(UnityEditor.ShaderGraph.Target legacyTarget, UnityEditor.ShaderGraph.SubShaderDescriptor legacySubShader)
        {
            m_LegacyTarget = legacyTarget;
            m_LegacySubShader = legacySubShader;
        }

        bool FindLegacyPass(string referenceName, ref UnityEditor.ShaderGraph.PassDescriptor legacyPassDescriptor)
        {
            foreach(var legacyPass in m_LegacySubShader.passes)
            {
                if (legacyPass.descriptor.referenceName == referenceName)
                {
                    legacyPassDescriptor = legacyPass.descriptor;
                    return true;
                }
            }
            return false;
        }

        void ITemplateLinker.Link(ShaderBuilder builder, ShaderContainer container, TemplateDescriptor templateDescriptor)
        {
            Container = container;

            builder.AddLine("SubShader");
            using (builder.BlockScope())
            {
                GenerateSubShaderTags(m_LegacySubShader, builder);

                var template = templateDescriptor.Template;
                foreach (var pass in template.Passes)
                    GenerateShaderPass(template, pass, templateDescriptor.CustomizationPointDescriptors, builder);
            }
        }

        void GenerateSubShaderTags(UnityEditor.ShaderGraph.SubShaderDescriptor descriptor, ShaderBuilder builder)
        {
            builder.AppendLine("Tags");
            using (builder.BlockScope())
            {
                // Pipeline tag
                if (!string.IsNullOrEmpty(descriptor.pipelineTag))
                    builder.AppendLine($"\"RenderPipeline\"=\"{descriptor.pipelineTag}\"");
                else
                    builder.AppendLine("// RenderPipeline: <None>");

                // Render Type
                if (!string.IsNullOrEmpty(descriptor.renderType))
                    builder.AppendLine($"\"RenderType\"=\"{descriptor.renderType}\"");
                else
                    builder.AppendLine("// RenderType: <None>");

                // Custom shader tags.
                if (!string.IsNullOrEmpty(descriptor.customTags))
                    builder.AppendLine(descriptor.customTags);

                // Render Queue
                if (!string.IsNullOrEmpty(descriptor.renderQueue))
                    builder.AppendLine($"\"Queue\"=\"{descriptor.renderQueue}\"");
                else
                    builder.AppendLine("// Queue: <None>");

                // ShaderGraphShader tag (so we can tell what shadergraph built)
                builder.AppendLine("\"ShaderGraphShader\"=\"true\"");
            }
        }
        
        void GenerateShaderPass(Template template, TemplatePass pass, IEnumerable<CustomizationPointDescriptor> customizationPointDescriptors, ShaderBuilder builder)
        {
            UnityEditor.ShaderGraph.PassDescriptor legacyPass = new UnityEditor.ShaderGraph.PassDescriptor();
            if (!FindLegacyPass(pass.ReferenceName, ref legacyPass))
                throw new Exception("Shouldn't happen");

            var passCustomizationPointDescriptors = FindCustomizationPointsForPass(pass, customizationPointDescriptors);

            var legacyBlockLinker = new SimpleLegacyBlockLinker(Container);
            var legacyEntryPoints = legacyBlockLinker.GenerateLegacyEntryPoints(template, pass, passCustomizationPointDescriptors);

            ActiveFields targetActiveFields, shaderGraphActiveFields;
            BuildLegacyActiveFields(legacyPass, legacyEntryPoints, out targetActiveFields, out shaderGraphActiveFields);

            GenerateShaderPass(builder, pass, legacyPass, targetActiveFields, shaderGraphActiveFields, legacyEntryPoints, new PropertyCollector());
        }

        List<CustomizationPointDescriptor> FindCustomizationPointsForPass(TemplatePass pass, IEnumerable<CustomizationPointDescriptor> customizationPointDescriptors)
        {
            var passCustomizationPointDescriptors = new List<CustomizationPointDescriptor>();
            foreach (var cpDesc in customizationPointDescriptors)
            {
                // If there's no pass identifiers, then this is valid for all passes
                if (cpDesc.PassIdentifiers == null || cpDesc.PassIdentifiers.Count() == 0)
                    passCustomizationPointDescriptors.Add(cpDesc);
                else
                {
                    // Otherwise check if there's a matching pass identifier
                    foreach (var passIdentifier in cpDesc.PassIdentifiers)
                    {
                        if (passIdentifier.m_SubShaderIndex == pass.PassIdentifier.m_SubShaderIndex && passIdentifier.m_PassIndex == pass.PassIdentifier.m_PassIndex)
                            passCustomizationPointDescriptors.Add(cpDesc);
                    }
                }
            }
            return passCustomizationPointDescriptors;
        }        

        void BuildLegacyActiveFields(UnityEditor.ShaderGraph.PassDescriptor legacyPass, LegacyEntryPoints legacyEntryPoints, out ActiveFields targetActiveFields, out ActiveFields shaderGraphActiveFields)
        {
            Dictionary<string, FieldDescriptor> vertexInLookup, vertexOutLookup, fragmentInLookup, fragmentOutLookup;
            BuildLookups(legacyPass, out vertexInLookup, out vertexOutLookup, out fragmentInLookup, out fragmentOutLookup);

            targetActiveFields = new ActiveFields();
            if(legacyEntryPoints.vertexDescBlockDesc.IsValid)
                targetActiveFields.baseInstance.Add(Fields.GraphVertex);
            targetActiveFields.baseInstance.Add(Fields.GraphPixel);
            GenerationUtils.AddRequiredFields(legacyPass.requiredFields, targetActiveFields.baseInstance);

            void AddFieldFromProperty(ActiveFields activeFields, BlockVariable prop, Dictionary<string, FieldDescriptor> lookups)
            {
                FieldDescriptor activeField;
                if (lookups.TryGetValue(prop.ReferenceName, out activeField))
                    activeFields.baseInstance.Add(activeField);
                else if (prop.Attributes.FindFirst(CommonShaderAttributes.Varying).IsValid)
                {
                    var customInterpolatorField = new FieldDescriptor("", prop.ReferenceName, "", prop.Type.Name);
                    activeFields.baseInstance.Add(customInterpolatorField);
                }
            }

            void AddFieldProperties(BlockDescriptor blockDesc, ActiveFields activeFields, Dictionary<string, FieldDescriptor> inputLookups, Dictionary<string, FieldDescriptor> outputLookups)
            {
                if (!blockDesc.IsValid)
                    return;

                foreach (var input in blockDesc.Block.Inputs)
                    AddFieldFromProperty(activeFields, input, inputLookups);
                foreach (var output in blockDesc.Block.Outputs)
                    AddFieldFromProperty(activeFields, output, outputLookups);
            }

            shaderGraphActiveFields = new ActiveFields();
            AddFieldProperties(legacyEntryPoints.vertexDescBlockDesc, shaderGraphActiveFields, vertexInLookup, vertexOutLookup);
            AddFieldProperties(legacyEntryPoints.fragmentDescBlockDesc, shaderGraphActiveFields, fragmentInLookup, fragmentOutLookup);
        }

        void BuildLookups(UnityEditor.ShaderGraph.PassDescriptor legacyPass, out Dictionary<string, FieldDescriptor> vertexInLookups, out Dictionary<string, FieldDescriptor> vertexOutLookups, out Dictionary<string, FieldDescriptor> fragmentInLookups, out Dictionary<string, FieldDescriptor> fragmentOutLookups)
        {
            List<FieldDescriptor> preVertexFields = new List<FieldDescriptor>();
            preVertexFields.AddRange(UnityEditor.ShaderGraph.Structs.Attributes.fields);
            preVertexFields.AddRange(UnityEditor.ShaderGraph.Structs.VertexDescriptionInputs.fields);

            var vertexOutFields = new List<FieldDescriptor>();
            var fragmentOutFields = new List<FieldDescriptor>();

            var targetActiveBlockContext = new TargetActiveBlockContext(new List<BlockFieldDescriptor>(), legacyPass);
            m_LegacyTarget.GetActiveBlocks(ref targetActiveBlockContext);
            foreach (var field in targetActiveBlockContext.activeBlocks)
            {
                if (field.shaderStage == ShaderStage.Vertex)
                    vertexOutFields.Add(field);
                else if (field.shaderStage == ShaderStage.Fragment)
                    fragmentOutFields.Add(field);
            }

            vertexInLookups = BuildLookup(preVertexFields);
            vertexOutLookups = BuildLookup(vertexOutFields);
            fragmentInLookups = BuildLookup(UnityEditor.ShaderGraph.Structs.SurfaceDescriptionInputs.fields);
            fragmentOutLookups = BuildLookup(fragmentOutFields);
        }

        Dictionary<string, FieldDescriptor> BuildLookup(IEnumerable<FieldDescriptor> fields)
        {
            var result = new Dictionary<string, FieldDescriptor>();
            foreach (var field in fields)
            {
                if (!result.ContainsKey(field.name))
                    result.Add(field.name, field);
            }
            return result;
        }

        class VisitedRegistry
        {
            internal HashSet<ShaderType> visitedTypes = new HashSet<ShaderType>();
            internal HashSet<ShaderFunction> visitedFunctions = new HashSet<ShaderFunction>();

            internal bool TryVisit(ShaderType type)
            {
                if (visitedTypes.Contains(type))
                    return false;
                visitedTypes.Add(type);
                return true;
            }

            internal bool TryVisit(ShaderFunction function)
            {
                if (visitedFunctions.Contains(function))
                    return false;
                visitedFunctions.Add(function);
                return true;
            }
        }

        void GenerateBlockLinkSpliceCode(ShaderBuilder builder, BlockDescriptor blockDesc, string outputStructName, VisitedRegistry visitedRegistry)
        {
            var block = blockDesc.Block;

            foreach (var type in block.Types)
            {
                if (!visitedRegistry.TryVisit(type))
                    continue;

                // The input types are generated by the legacy code path
                if (type.Name != LegacyCustomizationPoints.SurfaceEntryPointInputName &&
                    type.Name != LegacyCustomizationPoints.VertexEntryPointInputName)
                    type.AddTypeDeclarationString(builder);
            }
            foreach(var function in block.Functions)
            {
                if (!visitedRegistry.TryVisit(function))
                    continue;
            
                function.AddDeclarationString(builder);
            }
        }

        void WriteCommands(IEnumerable<UnityEditor.ShaderFoundry.CommandDescriptor> descriptors, ShaderStringBuilder builder)
        {
            foreach (var commandDesc in descriptors)
            {
                builder.AppendIndentation();
                builder.Append(commandDesc.Name);
                foreach (var op in commandDesc.Ops)
                    builder.Append($" {op}");
                builder.AppendNewLine();
            }
        }

        void WriteDefines(IEnumerable<UnityEditor.ShaderFoundry.DefineDescriptor> descriptors, ShaderStringBuilder builder)
        {
            foreach (var defineDesc in descriptors)
            {
                if (string.IsNullOrEmpty(defineDesc.Value))
                    builder.AppendLine($"#define {defineDesc.Name}");
                else
                    builder.AppendLine($"#define {defineDesc.Name} {defineDesc.Value}");
            }
        }

        void WriteIncludes(IEnumerable<UnityEditor.ShaderFoundry.IncludeDescriptor> descriptors, ShaderStringBuilder builder)
        {
            foreach (var includeDesc in descriptors)
            {
                builder.AppendLine($"#include {includeDesc.Value}");
            }
        }

        void WriteKeywords(IEnumerable<UnityEditor.ShaderFoundry.KeywordDescriptor> descriptors, ShaderStringBuilder builder)
        {
            foreach (var keywordDesc in descriptors)
            {
                builder.AppendIndentation();
                builder.Append("#pragma ");
                builder.Append(keywordDesc.Definition);
                if (!string.IsNullOrEmpty(keywordDesc.Scope))
                    builder.Append($"_{keywordDesc.Scope}");
                if (!string.IsNullOrEmpty(keywordDesc.Stage))
                    builder.Append($"_{keywordDesc.Stage}");
                if (!string.IsNullOrEmpty(keywordDesc.Name))
                    builder.Append($" {keywordDesc.Name}");
                foreach (var op in keywordDesc.Ops)
                    builder.Append($" {op}");
                builder.AppendNewLine();
            }
        }

        void WritePragmas(IEnumerable<UnityEditor.ShaderFoundry.PragmaDescriptor> descriptors, ShaderStringBuilder builder)
        {
            foreach (var pragmaDesc in descriptors)
            {
                builder.AppendIndentation();
                builder.Append($"#pragma {pragmaDesc.Name}");
                foreach (var op in pragmaDesc.Ops)
                    builder.Append($" {op}");
                builder.AppendNewLine();
            }
        }

        ShaderValueType ShaderValueTypeFrom(int width)
        {
            switch (width)
            {
                case 1:
                    return ShaderValueType.Float;
                case 2:
                    return ShaderValueType.Float2;
                case 3:
                    return ShaderValueType.Float3;
                default:
                    return ShaderValueType.Float4;
            }
        }

        void GenerateShaderPass(ShaderBuilder subPassBuilder, TemplatePass templatePass, UnityEditor.ShaderGraph.PassDescriptor pass, ActiveFields targetActiveFields, ActiveFields blockActiveFields, LegacyEntryPoints legacyEntryPoints, PropertyCollector subShaderProperties)
        {
            string vertexCode = "// GraphVertex: <None>";
            string fragmentCode = "// GraphPixel: <None>";
            var sharedFunctions = "// GraphFunctions: <None>";
            var shaderProperties = Enumerable.Empty<BlockProperty>();
            var shaderCommands = Enumerable.Empty<CommandDescriptor>();
            var shaderDefines = Enumerable.Empty<DefineDescriptor>();
            var shaderIncludes = Enumerable.Empty<UnityEditor.ShaderFoundry.IncludeDescriptor>();
            var shaderKeywords = Enumerable.Empty<UnityEditor.ShaderFoundry.KeywordDescriptor>();
            var shaderPragmas = Enumerable.Empty<UnityEditor.ShaderFoundry.PragmaDescriptor>();
            
            void ProcessBlockDescriptor(BlockDescriptor blockDescriptor, VisitedRegistry visitedRegistry, string entryPointOutputName, ref string code)
            {
                if (blockDescriptor.IsValid)
                {
                    var builderNew = new ShaderBuilder();
                    GenerateBlockLinkSpliceCode(builderNew, blockDescriptor, entryPointOutputName, visitedRegistry);
                    var builderOld = new ShaderStringBuilder();
                    builderOld.Append(builderNew.ToString());
                    builderOld.ReplaceInCurrentMapping(PrecisionUtil.Token, ConcretePrecision.Single.ToShaderString());
                    code = builderOld.ToCodeBlock();

                    var block = blockDescriptor.Block;
                    shaderProperties = shaderProperties.Concat(block.Properties);
                    shaderCommands = shaderCommands.Concat(block.Commands);
                    shaderDefines = shaderDefines.Concat(block.Defines);
                    shaderIncludes = shaderIncludes.Concat(block.Includes);
                    shaderKeywords = shaderKeywords.Concat(block.Keywords);
                    shaderPragmas = shaderPragmas.Concat(block.Pragmas);
                }
            }

            VisitedRegistry visitedRegistry = new VisitedRegistry();
            ProcessBlockDescriptor(legacyEntryPoints.vertexDescBlockDesc, visitedRegistry, LegacyCustomizationPoints.VertexEntryPointOutputName, ref vertexCode);
            ProcessBlockDescriptor(legacyEntryPoints.fragmentDescBlockDesc, visitedRegistry, LegacyCustomizationPoints.SurfaceEntryPointOutputName, ref fragmentCode);

            // Handle block custom interpolators. Do this by checking if any fragment input is marked as a varying
            var blockVaryings = new List<FieldDescriptor>();
            if (legacyEntryPoints.fragmentDescBlockDesc.IsValid && legacyEntryPoints.fragmentDescBlockDesc.Block.IsValid)
            {
                var block = legacyEntryPoints.fragmentDescBlockDesc.Block;
                foreach (var field in block.Inputs)
                {
                    if (field.Attributes.FindFirst(CommonShaderAttributes.Varying).IsValid)
                    {
                        var customInterpolatorField = new FieldDescriptor("", field.ReferenceName, "", ShaderValueTypeFrom((int)field.Type.VectorDimension), subscriptOptions: StructFieldOptions.Generated);
                        blockVaryings.Add(customInterpolatorField);
                    }
                }
            }

            GenerationMode m_Mode = GenerationMode.ForReals;
            // Early exit if pass is not used in preview
            if (m_Mode == GenerationMode.Preview && !pass.useInPreview)
                return;

            // --------------------------------------------------
            // Debug

            // Get scripting symbols
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

            const string kDebugSymbol = "SHADERGRAPH_DEBUG";
            bool isDebug = defines.Contains(kDebugSymbol);

            // --------------------------------------------------
            // Setup

            // Custom Interpolator Global flags (see definition for details).
            AbstractMaterialNode m_OutputNode = null;
            CustomInterpolatorUtils.generatorNodeOnly = m_OutputNode != null;
            CustomInterpolatorUtils.generatorSkipFlag = m_LegacyTarget.ignoreCustomInterpolators ||
                !CustomInterpolatorUtils.generatorNodeOnly && (pass.customInterpolators == null || pass.customInterpolators.Count() == 0);

            // Initialize custom interpolator sub generator
            CustomInterpSubGen customInterpSubGen = new CustomInterpSubGen(m_OutputNode != null);
            foreach(var customInterpolatorField in blockVaryings)
                customInterpSubGen.AddCustomInterpolant(customInterpolatorField);

            // Initiailize Collectors
            // NOTE: propertyCollector is not really used anymore -- we use the subshader PropertyCollector instead
            var propertyCollector = new PropertyCollector();
            var keywordCollector = new KeywordCollector();

            // Moved this up so that we can reuse the information to figure out which struct Descriptors
            // should be populated by custom interpolators.
            var passStructs = new List<StructDescriptor>();
            passStructs.AddRange(pass.structs.Select(x => x.descriptor));

            // GET CUSTOM ACTIVE FIELDS HERE!

            // inject custom interpolator fields into the pass structs
            passStructs = customInterpSubGen.CopyModifyExistingPassStructs(passStructs, blockActiveFields.baseInstance);

            // Get active fields from ShaderPass
            GenerationUtils.AddRequiredFields(pass.requiredFields, blockActiveFields.baseInstance);

            // Function Registry
            var functionBuilder = new ShaderStringBuilder();
            var graphIncludes = new IncludeCollection();
            var functionRegistry = new FunctionRegistry(functionBuilder, graphIncludes, true);

            // Hash table of named $splice(name) commands
            // Key: splice token
            // Value: string to splice
            Dictionary<string, string> spliceCommands = new Dictionary<string, string>();

            // populate splice commands from the pass's customInterpolator descriptors.
            if (pass.customInterpolators != null)
                customInterpSubGen.ProcessDescriptors(pass.customInterpolators.Select(item => item.descriptor));
            customInterpSubGen.AppendToSpliceCommands(spliceCommands);

            // --------------------------------------------------
            // Dependencies

            // Propagate active field requirements using dependencies
            // Must be executed before types are built
            foreach (var instance in blockActiveFields.all.instances)
            {
                GenerationUtils.ApplyFieldDependencies(instance, pass.fieldDependencies);
            }

            // --------------------------------------------------
            // Pass Setup

            // Name
            if (!string.IsNullOrEmpty(pass.displayName))
            {
                spliceCommands.Add("PassName", $"Name \"{pass.displayName}\"");
            }
            else
            {
                spliceCommands.Add("PassName", "// Name: <None>");
            }

            // Tags
            if (!string.IsNullOrEmpty(pass.lightMode))
            {
                spliceCommands.Add("LightMode", $"\"LightMode\" = \"{pass.lightMode}\"");
            }
            else
            {
                spliceCommands.Add("LightMode", "// LightMode: <None>");
            }

            // --------------------------------------------------
            // Pass Code

            // Render State
            using (var renderStateBuilder = new ShaderStringBuilder())
            {
                // Render states need to be separated by RenderState.Type
                // The first passing ConditionalRenderState of each type is inserted
                foreach (RenderStateType type in Enum.GetValues(typeof(RenderStateType)))
                {
                    var renderStates = pass.renderStates?.Where(x => x.descriptor.type == type);
                    if (renderStates != null)
                    {
                        foreach (RenderStateCollection.Item renderState in renderStates)
                        {
                            if (renderState.TestActive(targetActiveFields))
                            {
                                renderStateBuilder.AppendLine(renderState.value);

                                // Cull is the only render state type that causes a compilation error
                                // when there are multiple Cull directive with different values in a pass.
                                if (type == RenderStateType.Cull)
                                    break;
                            }
                        }
                    }
                }
                WriteCommands(shaderCommands, renderStateBuilder);

                string command = GenerationUtils.GetSpliceCommand(renderStateBuilder.ToCodeBlock(), "RenderState");
                spliceCommands.Add("RenderState", command);
            }

            // Pragmas
            using (var passPragmaBuilder = new ShaderStringBuilder())
            {
                if (pass.pragmas != null)
                {
                    foreach (PragmaCollection.Item pragma in pass.pragmas)
                    {
                        if (pragma.TestActive(targetActiveFields))
                            passPragmaBuilder.AppendLine(pragma.value);
                    }
                }
                WritePragmas(shaderPragmas, passPragmaBuilder);

                // Enable this to turn on shader debugging
                bool debugShader = false;
                if (debugShader)
                {
                    passPragmaBuilder.AppendLine("#pragma enable_d3d11_debug_symbols");
                }

                string command = GenerationUtils.GetSpliceCommand(passPragmaBuilder.ToCodeBlock(), "PassPragmas");
                spliceCommands.Add("PassPragmas", command);
            }

            // Keywords
            using (var passKeywordBuilder = new ShaderStringBuilder())
            {
                if (pass.keywords != null)
                {
                    List<KeywordShaderStage> stages = new List<KeywordShaderStage>();
                    foreach (KeywordCollection.Item keyword in pass.keywords)
                    {
                        if (keyword.TestActive(targetActiveFields))
                        {
                            keyword.descriptor.AppendKeywordDeclarationStrings(passKeywordBuilder);
                        }
                    }
                }
                WriteKeywords(shaderKeywords, passKeywordBuilder);

                string command = GenerationUtils.GetSpliceCommand(passKeywordBuilder.ToCodeBlock(), "PassKeywords");
                spliceCommands.Add("PassKeywords", command);
            }

            // -----------------------------
            // Generated structs and Packing code
            var interpolatorBuilder = new ShaderStringBuilder();

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
                    if (blockActiveFields.permutationCount > 0)
                    {
                        var generatedPackedTypes = new Dictionary<string, (ShaderStringBuilder, List<int>)>();
                        foreach (var instance in blockActiveFields.allPermutations.instances)
                        {
                            var instanceGenerator = new ShaderStringBuilder();
                            GenerationUtils.GenerateInterpolatorFunctions(shaderStruct, instance, out instanceGenerator);
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
                        GenerationUtils.GenerateInterpolatorFunctions(shaderStruct, blockActiveFields.baseInstance, out localInterpolatorBuilder);
                        interpolatorBuilder.Concat(localInterpolatorBuilder);
                    }
                    //using interp index from functions, generate packed struct descriptor
                    GenerationUtils.GeneratePackedStruct(shaderStruct, blockActiveFields, out packStruct);
                    packedStructs.Add(packStruct);
                }
                passStructs.AddRange(packedStructs);
            }
            if (interpolatorBuilder.length != 0) //hard code interpolators to float, TODO: proper handle precision
                interpolatorBuilder.ReplaceInCurrentMapping(PrecisionUtil.Token, ConcretePrecision.Single.ToShaderString());
            else
                interpolatorBuilder.AppendLine("//Interpolator Packs: <None>");
            spliceCommands.Add("InterpolatorPack", interpolatorBuilder.ToCodeBlock());

            // Generated String Builders for all struct types
            var passStructBuilder = new ShaderStringBuilder();
            if (passStructs != null)
            {
                var structBuilder = new ShaderStringBuilder();
                foreach (StructDescriptor shaderStruct in passStructs)
                {
                    GenerationUtils.GenerateShaderStruct(shaderStruct, blockActiveFields, out structBuilder);
                    structBuilder.ReplaceInCurrentMapping(PrecisionUtil.Token, ConcretePrecision.Single.ToShaderString()); //hard code structs to float, TODO: proper handle precision
                    passStructBuilder.Concat(structBuilder);
                }
            }
            if (passStructBuilder.length == 0)
                passStructBuilder.AppendLine("//Pass Structs: <None>");
            spliceCommands.Add("PassStructs", passStructBuilder.ToCodeBlock());

            // --------------------------------------------------
            // Graph Vertex
            spliceCommands.Add("GraphVertex", vertexCode);

            // --------------------------------------------------
            // Graph Pixel
            spliceCommands.Add("GraphPixel", fragmentCode);

            // --------------------------------------------------
            // Graph Functions
            spliceCommands.Add("GraphFunctions", sharedFunctions);

            // --------------------------------------------------
            // Graph Keywords

            using (var keywordBuilder = new ShaderStringBuilder())
            {
                keywordCollector.GetKeywordsDeclaration(keywordBuilder, m_Mode);
                if (keywordBuilder.length == 0)
                    keywordBuilder.AppendLine("// GraphKeywords: <None>");
                spliceCommands.Add("GraphKeywords", keywordBuilder.ToCodeBlock());
            }

            // --------------------------------------------------
            // Graph Properties

            //using ()
            {
                var propertyBuilder = new ShaderBuilder();
                var perMaterialBuilder = new ShaderBuilder();
                var globalBuilder = new ShaderBuilder();

                var visitedProperties = new HashSet<string>();
                foreach(var prop in shaderProperties)
                {
                    if (visitedProperties.Contains(prop.ReferenceName))
                        continue;
                    visitedProperties.Add(prop.ReferenceName);
                    prop.DeclarePassProperty(perMaterialBuilder, globalBuilder);
                }

                propertyBuilder.AppendLine("CBUFFER_START(UnityPerMaterial)");
                propertyBuilder.Append(perMaterialBuilder.ToString());
                propertyBuilder.AppendLine("CBUFFER_END");
                propertyBuilder.Append(globalBuilder.ToString());

                var propertiesStr = propertyBuilder.ToString();
                if(string.IsNullOrEmpty(propertiesStr))
                    propertiesStr = "// GraphProperties: <None>";
                spliceCommands.Add("GraphProperties", propertiesStr);
            }

            // --------------------------------------------------
            // Dots Instanced Graph Properties

            bool hasDotsProperties = subShaderProperties.HasDotsProperties();

            using (var dotsInstancedPropertyBuilder = new ShaderStringBuilder())
            {
                if (hasDotsProperties)
                    dotsInstancedPropertyBuilder.AppendLines(subShaderProperties.GetDotsInstancingPropertiesDeclaration(m_Mode));
                else
                    dotsInstancedPropertyBuilder.AppendLine("// HybridV1InjectedBuiltinProperties: <None>");
                spliceCommands.Add("HybridV1InjectedBuiltinProperties", dotsInstancedPropertyBuilder.ToCodeBlock());
            }

            // --------------------------------------------------
            // Dots Instancing Options

            using (var dotsInstancingOptionsBuilder = new ShaderStringBuilder())
            {
                // Hybrid Renderer V1 requires some magic defines to work, which we enable
                // if the shader graph has a nonzero amount of DOTS instanced properties.
                // This can be removed once Hybrid V1 is removed.
    #if !ENABLE_HYBRID_RENDERER_V2
                if (hasDotsProperties)
                {
                    dotsInstancingOptionsBuilder.AppendLine("#if SHADER_TARGET >= 35 && (defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_XBOXONE)  || defined(SHADER_API_GAMECORE) || defined(SHADER_API_PSSL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_METAL))");
                    dotsInstancingOptionsBuilder.AppendLine("    #define UNITY_SUPPORT_INSTANCING");
                    dotsInstancingOptionsBuilder.AppendLine("#endif");
                    dotsInstancingOptionsBuilder.AppendLine("#if defined(UNITY_SUPPORT_INSTANCING) && defined(INSTANCING_ON)");
                    dotsInstancingOptionsBuilder.AppendLine("    #define UNITY_HYBRID_V1_INSTANCING_ENABLED");
                    dotsInstancingOptionsBuilder.AppendLine("#endif");
                }
    #endif

                if (dotsInstancingOptionsBuilder.length == 0)
                    dotsInstancingOptionsBuilder.AppendLine("// DotsInstancingOptions: <None>");
                spliceCommands.Add("DotsInstancingOptions", dotsInstancingOptionsBuilder.ToCodeBlock());
            }

            // --------------------------------------------------
            // Graph Defines

            using (var graphDefines = new ShaderStringBuilder())
            {
                graphDefines.AppendLine("#define SHADERPASS {0}", pass.referenceName);

                void AddDefines(DefineCollection defines, IEnumerable<DefineDescriptor> defineDescriptors, ShaderStringBuilder graphDefines)
                {
                    if (defines != null)
                    {
                        foreach (var define in defines)
                        {
                            if (define.TestActive(blockActiveFields))
                                graphDefines.AppendLine(define.value);
                        }
                    }
                }

                AddDefines(pass.defines, shaderDefines, graphDefines);
                WriteDefines(shaderDefines, graphDefines);
                //if (graphRequirements.permutationCount > 0)
                //{
                //    List<int> activePermutationIndices;

                //    // Depth Texture
                //    activePermutationIndices = graphRequirements.allPermutations.instances
                //        .Where(p => p.requirements.requiresDepthTexture)
                //        .Select(p => p.permutationIndex)
                //        .ToList();
                //    if (activePermutationIndices.Count > 0)
                //    {
                //        graphDefines.AppendLine(KeywordUtil.GetKeywordPermutationSetConditional(activePermutationIndices));
                //        graphDefines.AppendLine("#define REQUIRE_DEPTH_TEXTURE");
                //        graphDefines.AppendLine("#endif");
                //    }

                //    // Opaque Texture
                //    activePermutationIndices = graphRequirements.allPermutations.instances
                //        .Where(p => p.requirements.requiresCameraOpaqueTexture)
                //        .Select(p => p.permutationIndex)
                //        .ToList();
                //    if (activePermutationIndices.Count > 0)
                //    {
                //        graphDefines.AppendLine(KeywordUtil.GetKeywordPermutationSetConditional(activePermutationIndices));
                //        graphDefines.AppendLine("#define REQUIRE_OPAQUE_TEXTURE");
                //        graphDefines.AppendLine("#endif");
                //    }
                //}
                //else
                //{
                //    // Depth Texture
                //    if (graphRequirements.baseInstance.requirements.requiresDepthTexture)
                //        graphDefines.AppendLine("#define REQUIRE_DEPTH_TEXTURE");

                //    // Opaque Texture
                //    if (graphRequirements.baseInstance.requirements.requiresCameraOpaqueTexture)
                //        graphDefines.AppendLine("#define REQUIRE_OPAQUE_TEXTURE");
                //}

                // Add to splice commands
                spliceCommands.Add("GraphDefines", graphDefines.ToCodeBlock());
            }

            // --------------------------------------------------
            // Includes

            var allIncludes = new IncludeCollection();
            allIncludes.Add(pass.includes);
            allIncludes.Add(graphIncludes);

            using (var preGraphIncludeBuilder = new ShaderStringBuilder())
            {
                foreach (var include in allIncludes.Where(x => x.location == IncludeLocation.Pregraph))
                {
                    if (include.TestActive(blockActiveFields))
                        preGraphIncludeBuilder.AppendLine(include.value);
                }

                string command = GenerationUtils.GetSpliceCommand(preGraphIncludeBuilder.ToCodeBlock(), "PreGraphIncludes");
                spliceCommands.Add("PreGraphIncludes", command);
            }

            using (var graphIncludeBuilder = new ShaderStringBuilder())
            {
                foreach (var include in allIncludes.Where(x => x.location == IncludeLocation.Graph))
                {
                    if (include.TestActive(blockActiveFields))
                        graphIncludeBuilder.AppendLine(include.value);
                }
                WriteIncludes(shaderIncludes, graphIncludeBuilder);

                string command = GenerationUtils.GetSpliceCommand(graphIncludeBuilder.ToCodeBlock(), "GraphIncludes");
                spliceCommands.Add("GraphIncludes", command);
            }

            using (var postGraphIncludeBuilder = new ShaderStringBuilder())
            {
                foreach (var include in allIncludes.Where(x => x.location == IncludeLocation.Postgraph))
                {
                    if (include.TestActive(blockActiveFields))
                        postGraphIncludeBuilder.AppendLine(include.value);
                }

                string command = GenerationUtils.GetSpliceCommand(postGraphIncludeBuilder.ToCodeBlock(), "PostGraphIncludes");
                spliceCommands.Add("PostGraphIncludes", command);
            }

            // --------------------------------------------------
            // Debug

            // Debug output all active fields

            using (var debugBuilder = new ShaderStringBuilder())
            {
                if (isDebug)
                {
                    // Active fields
                    debugBuilder.AppendLine("// ACTIVE FIELDS:");
                    foreach (FieldDescriptor field in blockActiveFields.baseInstance.fields)
                    {
                        debugBuilder.AppendLine($"//{field.tag}.{field.name}");
                    }
                }
                if (debugBuilder.length == 0)
                    debugBuilder.AppendLine("// <None>");

                // Add to splice commands
                spliceCommands.Add("Debug", debugBuilder.ToCodeBlock());
            }

            // --------------------------------------------------
            // Additional Commands

            if (pass.additionalCommands != null)
            {
                foreach (AdditionalCommandCollection.Item additionalCommand in pass.additionalCommands)
                {
                    spliceCommands.Add(additionalCommand.field.token, additionalCommand.field.content);
                }
            }

            // --------------------------------------------------
            // Finalize

            // Pass Template
            string passTemplatePath = pass.passTemplatePath;

            // Shared Templates
            string[] sharedTemplateDirectories = pass.sharedTemplateDirectories;

            if (!File.Exists(passTemplatePath))
                return;

            // Process Template
            foreach (var field in targetActiveFields.baseInstance.fields)
                blockActiveFields.baseInstance.Add(field);
            var templatePreprocessor = new ShaderSpliceUtil.TemplatePreprocessor(blockActiveFields, spliceCommands,
                    isDebug, sharedTemplateDirectories, m_assetCollection);
            templatePreprocessor.ProcessTemplateFile(passTemplatePath);
            subPassBuilder.Add(templatePreprocessor.GetShaderCode().ToString());

            // Turn off the skip flag so other passes behave correctly correctly.
            CustomInterpolatorUtils.generatorSkipFlag = false;
            CustomInterpolatorUtils.generatorNodeOnly = false;
        }
    }
}
