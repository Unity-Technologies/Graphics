using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using BlockProperty = UnityEditor.ShaderFoundry.BlockVariable;

namespace UnityEditor.ShaderFoundry
{
    internal class LegacyTemplateLinker : ITemplateLinker
    {
        internal AssetCollection m_assetCollection = new AssetCollection();

        ShaderContainer Container;

        UnityEditor.ShaderGraph.Target m_LegacyTarget;
        UnityEditor.ShaderGraph.SubShaderDescriptor m_LegacySubShader;

        internal LegacyTemplateLinker(AssetCollection assetCollection)
        {
            m_assetCollection = assetCollection;
        }

        internal void SetLegacy(UnityEditor.ShaderGraph.Target legacyTarget, UnityEditor.ShaderGraph.SubShaderDescriptor legacySubShader)
        {
            m_LegacyTarget = legacyTarget;
            m_LegacySubShader = legacySubShader;
        }

        internal bool FindLegacyPass(string referenceName, ref UnityEditor.ShaderGraph.PassDescriptor legacyPassDescriptor)
        {
            foreach (var legacyPass in m_LegacySubShader.passes)
            {
                if (legacyPass.descriptor.referenceName == referenceName)
                {
                    legacyPassDescriptor = legacyPass.descriptor;
                    return true;
                }
            }
            return false;
        }

        void ITemplateLinker.Link(ShaderBuilder builder, ShaderContainer container, TemplateInstance templateInstance)
        {
            Container = container;

            builder.AddLine("SubShader");
            using (builder.BlockScope())
            {
                GenerateSubShaderTags(m_LegacySubShader, templateInstance, builder);

                var template = templateInstance.Template;
                foreach (var pass in template.Passes)
                    GenerateShaderPass(template, pass, templateInstance.CustomizationPointInstances, builder);
            }
        }

        void GenerateSubShaderTags(UnityEditor.ShaderGraph.SubShaderDescriptor descriptor, TemplateInstance templateInstance, ShaderBuilder builder)
        {
            builder.AppendLine("Tags");
            using (builder.BlockScope())
            {
                // Custom shader tags.
                if (!string.IsNullOrEmpty(descriptor.customTags))
                    builder.AppendLine(descriptor.customTags);

                var template = templateInstance.Template;

                // Emit the template tags then the template instance tags
                foreach (var tagDescriptor in template.TagDescriptors)
                    builder.AppendLine($"\"{tagDescriptor.Name}\"=\"{tagDescriptor.Value}\"");
                foreach (var tagDescriptor in templateInstance.TagDescriptors)
                    builder.AppendLine($"\"{tagDescriptor.Name}\"=\"{tagDescriptor.Value}\"");
            }
        }

        void GenerateShaderPass(Template template, TemplatePass pass, IEnumerable<CustomizationPointInstance> customizationPointInstances, ShaderBuilder builder)
        {
            UnityEditor.ShaderGraph.PassDescriptor legacyPass = new UnityEditor.ShaderGraph.PassDescriptor();
            if (!FindLegacyPass(pass.ReferenceName, ref legacyPass))
                throw new Exception("Shouldn't happen");

            var passCustomizationPointInstances = FindCustomizationPointsForPass(pass, customizationPointInstances);

            var legacyBlockLinker = new SimpleLegacyBlockLinker(Container);
            var legacyEntryPoints = legacyBlockLinker.GenerateLegacyEntryPoints(template, pass, passCustomizationPointInstances);

            ActiveFields targetActiveFields, shaderGraphActiveFields;
            var customInterpolatorFields = new List<FieldDescriptor>();
            BuildLegacyActiveFields(legacyPass, legacyEntryPoints, out targetActiveFields, out shaderGraphActiveFields, customInterpolatorFields);

            GenerateShaderPass(builder, pass, legacyPass, targetActiveFields, shaderGraphActiveFields, legacyEntryPoints, customInterpolatorFields, new PropertyCollector());
        }

        List<CustomizationPointInstance> FindCustomizationPointsForPass(TemplatePass pass, IEnumerable<CustomizationPointInstance> customizationPointInstances)
        {
            var passCustomizationPointInstances = new List<CustomizationPointInstance>();
            foreach (var cpInst in customizationPointInstances)
            {
                // If there's no pass identifiers, then this is valid for all passes
                if (cpInst.PassIdentifiers == null || cpInst.PassIdentifiers.Count() == 0)
                    passCustomizationPointInstances.Add(cpInst);
                else
                {
                    // Otherwise check if there's a matching pass identifier
                    foreach (var passIdentifier in cpInst.PassIdentifiers)
                    {
                        if (passIdentifier == pass.PassIdentifier)
                            passCustomizationPointInstances.Add(cpInst);
                    }
                }
            }
            return passCustomizationPointInstances;
        }

        void GetTargetActiveFields(UnityEditor.ShaderGraph.PassDescriptor legacyPass, ActiveFields targetActiveFields)
        {
            // Try to get the active fields from the target. This requires getting the blocks currently.
            var targetActiveBlockContext = new TargetActiveBlockContext(new List<BlockFieldDescriptor>(), legacyPass);
            m_LegacyTarget.GetActiveBlocks(ref targetActiveBlockContext);
            var activeBlocks = new List<(BlockFieldDescriptor descriptor, bool isDefaultValue)>();
            foreach (var b in targetActiveBlockContext.activeBlocks)
                activeBlocks.Add((b, true));
            var context = new TargetFieldContext(legacyPass, activeBlocks, null, false);
            m_LegacyTarget.GetFields(ref context);

            var fields = GenerationUtils.GetActiveFieldsFromConditionals(context.conditionalFields.ToArray());
            foreach (var field in fields)
                targetActiveFields.baseInstance.Add(field);
        }

        void BuildLegacyActiveFields(UnityEditor.ShaderGraph.PassDescriptor legacyPass, LegacyEntryPoints legacyEntryPoints, out ActiveFields targetActiveFields, out ActiveFields shaderGraphActiveFields, List<FieldDescriptor> customInterpolatorFields)
        {
            FieldDescriptorLookupMap vertexInLookup, vertexOutLookup, fragmentInLookup, fragmentOutLookup;
            BuildLookups(legacyPass, out vertexInLookup, out vertexOutLookup, out fragmentInLookup, out fragmentOutLookup);

            targetActiveFields = new ActiveFields();
            if (legacyEntryPoints.vertexDescBlockInstance.IsValid)
                targetActiveFields.baseInstance.Add(Fields.GraphVertex);
            targetActiveFields.baseInstance.Add(Fields.GraphPixel);
            GetTargetActiveFields(legacyPass, targetActiveFields);
            GenerationUtils.AddRequiredFields(legacyPass.requiredFields, targetActiveFields.baseInstance);

            void AddFieldFromProperty(ActiveFields activeFields, BlockVariable prop, FieldDescriptorLookupMap lookups)
            {
                foreach (var descriptor in lookups.Find(prop.Name))
                    activeFields.baseInstance.Add(descriptor);
            }

            void AddFieldProperties(BlockInstance blockInst, ActiveFields activeFields, FieldDescriptorLookupMap inputLookups, FieldDescriptorLookupMap outputLookups)
            {
                if (!blockInst.IsValid)
                    return;

                foreach (var input in blockInst.Block.Inputs)
                    AddFieldFromProperty(activeFields, input, inputLookups);
                foreach (var output in blockInst.Block.Outputs)
                    AddFieldFromProperty(activeFields, output, outputLookups);
            }

            shaderGraphActiveFields = new ActiveFields();
            AddFieldProperties(legacyEntryPoints.vertexDescBlockInstance, shaderGraphActiveFields, vertexInLookup, vertexOutLookup);
            AddFieldProperties(legacyEntryPoints.fragmentDescBlockInstance, shaderGraphActiveFields, fragmentInLookup, fragmentOutLookup);

            foreach (var customInterpolant in legacyEntryPoints.customInterpolants)
            {
                var customInterpolatorField = new FieldDescriptor("", customInterpolant.Name, "", ShaderValueTypeFrom((int)customInterpolant.Type.VectorDimension), subscriptOptions: StructFieldOptions.Generated);
                shaderGraphActiveFields.baseInstance.Add(customInterpolatorField);
                customInterpolatorFields.Add(customInterpolatorField);
            }
        }

        internal class FieldDescriptorLookupMap
        {
            Dictionary<string, List<FieldDescriptor>> Lookups = new Dictionary<string, List<FieldDescriptor>>();
            internal void Add(string name, FieldDescriptor descriptor)
            {
                if (!Lookups.TryGetValue(name, out var descriptors))
                {
                    descriptors = new List<FieldDescriptor>();
                    Lookups.Add(name, descriptors);
                }
                descriptors.Add(descriptor);
            }

            internal IEnumerable<FieldDescriptor> Find(string name)
            {
                if (Lookups.TryGetValue(name, out var descriptors))
                    return descriptors;

                return Enumerable.Empty<FieldDescriptor>();
            }
        }

        void BuildLookups(UnityEditor.ShaderGraph.PassDescriptor legacyPass, out FieldDescriptorLookupMap vertexInLookups, out FieldDescriptorLookupMap vertexOutLookups, out FieldDescriptorLookupMap fragmentInLookups, out FieldDescriptorLookupMap fragmentOutLookups)
        {
            List<FieldDescriptor> preVertexFields = new List<FieldDescriptor>();
            preVertexFields.AddRange(UnityEditor.ShaderGraph.Structs.VertexDescriptionInputs.fields);

            var vertexOutFields = new List<FieldDescriptor>();
            var fragmentOutFields = new List<FieldDescriptor>();

            var targetActiveBlockContext = new TargetActiveBlockContext(new List<BlockFieldDescriptor>(), legacyPass);
            m_LegacyTarget.GetActiveBlocks(ref targetActiveBlockContext);
            foreach (var field in targetActiveBlockContext.activeBlocks)
            {
                if (field.shaderStage == UnityEditor.ShaderGraph.ShaderStage.Vertex)
                    vertexOutFields.Add(field);
                else if (field.shaderStage == UnityEditor.ShaderGraph.ShaderStage.Fragment)
                    fragmentOutFields.Add(field);
            }

            vertexInLookups = BuildLookup(preVertexFields);
            vertexOutLookups = BuildLookup(vertexOutFields);
            fragmentInLookups = BuildLookup(UnityEditor.ShaderGraph.Structs.SurfaceDescriptionInputs.fields);
            fragmentOutLookups = BuildLookup(fragmentOutFields);
        }

        FieldDescriptorLookupMap BuildLookup(IEnumerable<FieldDescriptor> fields)
        {
            var result = new FieldDescriptorLookupMap();
            foreach (var field in fields)
                result.Add(field.name, field);
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

        void BuildTypeAndFunctionGroups(Block block,  VisitedRegistry visitedRegistry, out List<(Block Block, List<ShaderType> Types)> typeGroups, out List<(Block Block, List<ShaderFunction> Functions)> functionGroups)
        {
            List<ShaderType> allTypes = new List<ShaderType>();
            List<ShaderFunction> allFunctions = new List<ShaderFunction>();
            void TraverseType(ShaderType type)
            {
                if (!visitedRegistry.TryVisit(type))
                    return;
                if (!type.IsStruct || type.IsDeclaredExternally)
                    return;

                foreach (var field in type.StructFields)
                    TraverseType(field.Type);

                allTypes.Add(type);
            }

            void TraverseTypes(IEnumerable<ShaderType> types)
            {
                foreach (var type in types)
                    TraverseType(type);
            }

            void TraverseFunction(ShaderFunction function)
            {
                if (!visitedRegistry.TryVisit(function))
                    return;

                TraverseType(function.ReturnType);
                foreach (var param in function.Parameters)
                    TraverseType(param.Type);
                allFunctions.Add(function);
            }

            void TraverseFunctions(IEnumerable<ShaderFunction> functions)
            {
                foreach (var function in functions)
                    TraverseFunction(function);
            }

            TraverseTypes(block.ReferencedTypes);
            TraverseTypes(block.Types);
            TraverseFunctions(block.ReferencedFunctions);
            TraverseFunctions(block.Functions);

            typeGroups = new List<(Block Block, List<ShaderType> Types)>();
            foreach (var type in allTypes)
            {
                if (typeGroups.Count == 0)
                    typeGroups.Add((type.ParentBlock, new List<ShaderType>()));
                var currentContext = typeGroups[typeGroups.Count - 1];
                if (currentContext.Block != type.ParentBlock)
                {
                    typeGroups.Add((type.ParentBlock, new List<ShaderType>()));
                    currentContext = typeGroups[typeGroups.Count - 1];
                }
                currentContext.Types.Add(type);
            }

            functionGroups = new List<(Block Block, List<ShaderFunction> Functions)>();
            foreach (var function in allFunctions)
            {
                if (functionGroups.Count == 0)
                    functionGroups.Add((function.ParentBlock, new List<ShaderFunction>()));
                var currentContext = functionGroups[functionGroups.Count - 1];
                if (currentContext.Block != function.ParentBlock)
                {
                    functionGroups.Add((function.ParentBlock, new List<ShaderFunction>()));
                    currentContext = functionGroups[functionGroups.Count - 1];
                }
                currentContext.Functions.Add(function);
            }
        }

        void GenerateBlockLinkSpliceCode(ShaderBuilder builder, BlockInstance blockInst, VisitedRegistry visitedRegistry)
        {
            void DeclareTypes(ShaderBuilder builder, IEnumerable<ShaderType> types)
            {
                foreach (var type in types)
                    builder.AddTypeDeclarationString(type);
            }

            void DeclareFunctions(ShaderBuilder builder, IEnumerable<ShaderFunction> functions)
            {
                foreach (var function in functions)
                    builder.AddDeclarationString(function);
            }

            BuildTypeAndFunctionGroups(blockInst.Block, visitedRegistry, out var typeGroups, out var functionGroups);
            foreach (var groupContext in typeGroups)
            {
                if (!groupContext.Block.IsValid)
                {
                    DeclareTypes(builder, groupContext.Types);
                    continue;
                }
                builder.Add($"namespace ");
                builder.AppendScopeName(groupContext.Block);
                builder.NewLine();
                using (var s = builder.BlockScope())
                {
                    DeclareTypes(builder, groupContext.Types);
                }
            }

            foreach (var groupContext in functionGroups)
            {
                if (!groupContext.Block.IsValid)
                {
                    DeclareFunctions(builder, groupContext.Functions);
                    continue;
                }
                builder.Add($"namespace ");
                builder.AppendScopeName(groupContext.Block);
                builder.NewLine();
                using (var s = builder.BlockScope())
                {
                    DeclareFunctions(builder, groupContext.Functions);
                }
            }
        }

        void ExtractKeywordDescriptors(Block block, List<UnityEditor.ShaderFoundry.KeywordDescriptor> shaderKeywords)
        {
            // Check all inputs for any keywords
            foreach (var input in block.Inputs)
            {
                // Skip anything that isn't a property (needed for the uniform name)
                var propertyAttribute = PropertyAttribute.FindFirst(input.Attributes);
                if (propertyAttribute == null)
                    continue;

                var uniformName = propertyAttribute.UniformName ?? input.Name;

                var boolKeywordAttribute = BoolKeywordAttribute.FindFirst(input.Attributes);
                if (boolKeywordAttribute != null)
                    shaderKeywords.Add(boolKeywordAttribute.BuildDescriptor(Container, uniformName));

                var enumKeywordAttribute = EnumKeywordAttribute.FindFirst(input.Attributes);
                if (enumKeywordAttribute != null)
                    shaderKeywords.Add(enumKeywordAttribute.BuildDescriptor(Container, uniformName));
            }
        }

        void WriteCommands(IEnumerable<UnityEditor.ShaderFoundry.CommandDescriptor> descriptors, ShaderStringBuilder builder)
        {
            foreach (var commandDesc in descriptors)
            {
                builder.TryAppendIndentation();
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
                builder.TryAppendIndentation();
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
                builder.TryAppendIndentation();
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

        void GenerateShaderPass(ShaderBuilder subPassBuilder, TemplatePass templatePass, UnityEditor.ShaderGraph.PassDescriptor pass, ActiveFields targetActiveFields, ActiveFields blockActiveFields, LegacyEntryPoints legacyEntryPoints, List<FieldDescriptor> customInterpolatorFields, PropertyCollector subShaderProperties)
        {
            string vertexCode = "// GraphVertex: <None>";
            string fragmentCode = "// GraphPixel: <None>";
            var sharedFunctions = "// GraphFunctions: <None>";
            var shaderProperties = new List<BlockProperty>();
            var shaderCommands = new List<CommandDescriptor>();
            var shaderDefines = new List<DefineDescriptor>();
            var shaderIncludes = new List<UnityEditor.ShaderFoundry.IncludeDescriptor>();
            var shaderKeywords = new List<UnityEditor.ShaderFoundry.KeywordDescriptor>();
            var shaderPragmas = new List<UnityEditor.ShaderFoundry.PragmaDescriptor>();

            void ProcessBlockInstance(BlockInstance blockInstance, VisitedRegistry visitedRegistry, string entryPointOutputName, ref string code)
            {
                if (blockInstance.IsValid)
                {
                    var blockBuilder = new ShaderBuilder();
                    GenerateBlockLinkSpliceCode(blockBuilder, blockInstance, visitedRegistry);
                    code = blockBuilder.ToString();

                    var block = blockInstance.Block;
                    shaderProperties.AddRange(block.Properties());
                    shaderCommands.AddRange(block.Commands);
                    shaderDefines.AddRange(block.Defines);
                    shaderIncludes.AddRange(block.Includes);
                    shaderKeywords.AddRange(block.Keywords);
                    shaderPragmas.AddRange(block.Pragmas);
                    ExtractKeywordDescriptors(block, shaderKeywords);
                }
            }

            VisitedRegistry visitedRegistry = new VisitedRegistry();
            ProcessBlockInstance(legacyEntryPoints.vertexDescBlockInstance, visitedRegistry, LegacyCustomizationPoints.VertexEntryPointOutputName, ref vertexCode);
            ProcessBlockInstance(legacyEntryPoints.fragmentDescBlockInstance, visitedRegistry, LegacyCustomizationPoints.SurfaceEntryPointOutputName, ref fragmentCode);

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
            foreach (var customInterpolatorField in customInterpolatorFields)
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
            using (var tagBuilder = new ShaderStringBuilder())
            {
                if (!string.IsNullOrEmpty(pass.lightMode))
                {
                    tagBuilder.AddLine($"\"LightMode\" = \"{pass.lightMode}\"");
                }
                else
                {
                    tagBuilder.AddLine("// LightMode: <None>");
                }

                // Currently there is no location to insert pass tags. For now, insert all of the pass tags into the "LightMode" splice point.
                foreach (var tagDescriptor in templatePass.TagDescriptors)
                    tagBuilder.AppendLine($"\"{tagDescriptor.Name}\"=\"{tagDescriptor.Value}\"");
                
                spliceCommands.Add("LightMode", tagBuilder.ToString());
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
                            GenerationUtils.GenerateInterpolatorFunctions(shaderStruct, instance, true, out instanceGenerator);
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
                        GenerationUtils.GenerateInterpolatorFunctions(shaderStruct, blockActiveFields.baseInstance, true, out localInterpolatorBuilder);
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
                    GenerationUtils.GenerateShaderStruct(shaderStruct, blockActiveFields, true, out structBuilder);
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

            {
                var propertyBuilder = new ShaderBuilder();
                UniformDeclarationContext context = new UniformDeclarationContext
                {
                    PerMaterialBuilder = new ShaderBuilder(),
                    GlobalBuilder = new ShaderBuilder(),
                };

                var visitedProperties = new HashSet<string>();
                foreach (var prop in shaderProperties)
                {
                    if (visitedProperties.Contains(prop.Name))
                        continue;
                    visitedProperties.Add(prop.Name);
                    UniformDeclaration.Declare(context, prop);
                }

                //if (m_Mode == GenerationMode.VFX)
                //{
                //    const string k_GraphPropertiesStruct = "GraphProperties";
                //    propertyBuilder.AppendLine($"struct {k_GraphPropertiesStruct}");
                //    using (propertyBuilder.BlockSemicolonScope())
                //    {
                //        m_GraphData.ForeachHLSLProperty(h =>
                //        {
                //            if (!h.IsObjectType())
                //                h.AppendTo(propertyBuilder);
                //        });
                //    }
                //}


                propertyBuilder.AppendLine("CBUFFER_START(UnityPerMaterial)");
                propertyBuilder.Append(context.PerMaterialBuilder.ToString());
                propertyBuilder.AppendLine("CBUFFER_END");
                propertyBuilder.Append(context.GlobalBuilder.ToString());

                var propertiesStr = propertyBuilder.ToString();
                if (string.IsNullOrEmpty(propertiesStr))
                    propertiesStr = "// GraphProperties: <None>";
                spliceCommands.Add("GraphProperties", propertiesStr);
            }

            // --------------------------------------------------
            // Dots Instanced Graph Properties

            bool hasDotsProperties = false;
            {
                foreach (var h in shaderProperties)
                {
                    if (h.Attributes.GetDeclaration() == HLSLDeclaration.HybridPerInstance)
                        hasDotsProperties = true;
                }
            }
            //subShaderProperties.HasDotsProperties();

            using (var dotsInstancedPropertyBuilder = new ShaderStringBuilder())
            {
                if (hasDotsProperties)
                {
                    if (hasDotsProperties)
                    {
                        dotsInstancedPropertyBuilder.AppendLine("#if defined(UNITY_HYBRID_V1_INSTANCING_ENABLED)");
                        dotsInstancedPropertyBuilder.AppendLine("#define HYBRID_V1_CUSTOM_ADDITIONAL_MATERIAL_VARS \\");

                        int count = 0;
                        foreach (var prop in shaderProperties)
                        {
                            if (prop.Attributes.GetDeclaration() != HLSLDeclaration.HybridPerInstance)
                                continue;

                            // Combine multiple UNITY_DEFINE_INSTANCED_PROP lines with \ so the generated
                            // macro expands into multiple definitions if there are more than one.
                            if (count > 0)
                            {
                                dotsInstancedPropertyBuilder.Append("\\");
                                dotsInstancedPropertyBuilder.AppendNewLine();
                            }
                            dotsInstancedPropertyBuilder.Append("UNITY_DEFINE_INSTANCED_PROP(");
                            dotsInstancedPropertyBuilder.Append(prop.Type.Name);
                            dotsInstancedPropertyBuilder.Append(", ");
                            dotsInstancedPropertyBuilder.Append(prop.Name);
                            dotsInstancedPropertyBuilder.Append(")");
                            count++;
                        }
                        dotsInstancedPropertyBuilder.AppendNewLine();
                    }
                    dotsInstancedPropertyBuilder.AppendLine("#define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) UNITY_ACCESS_INSTANCED_PROP(unity_Builtins0, var)");
                    dotsInstancedPropertyBuilder.AppendLine("#else");
                    dotsInstancedPropertyBuilder.AppendLine("#define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) var");
                    dotsInstancedPropertyBuilder.AppendLine("#endif");
                }
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

                if (pass.defines != null)
                {
                    foreach (var define in pass.defines)
                    {
                        if (define.TestActive(blockActiveFields))
                            graphDefines.AppendLine(define.value);
                    }
                }
                WriteDefines(shaderDefines, graphDefines);

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
                isDebug, sharedTemplateDirectories, m_assetCollection, true);
            templatePreprocessor.ProcessTemplateFile(passTemplatePath);
            subPassBuilder.AppendLines(templatePreprocessor.GetShaderCode().ToString());

            // Turn off the skip flag so other passes behave correctly correctly.
            CustomInterpolatorUtils.generatorSkipFlag = false;
            CustomInterpolatorUtils.generatorNodeOnly = false;
        }
    }
}
