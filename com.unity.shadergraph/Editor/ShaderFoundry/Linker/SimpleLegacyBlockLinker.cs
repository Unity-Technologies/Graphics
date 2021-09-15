using System.Collections.Generic;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderFoundry
{
    /// Handles linking together the vertex and fragment stages for the legacy linker.
    /// This helps to separate out logic from the LegacyTemplateLinker and potentially make this piece unit testable.
    internal class SimpleLegacyBlockLinker
    {
        ShaderContainer Container;

        class BlockGroup
        {
            internal string CustomizationPointName = null;
            internal List<BlockDescriptor> BlockDescriptors = new List<BlockDescriptor>();
            internal BlockLinkInstance LinkingData;
            internal List<BlockLinkInstance> BlocksLinkingData = new List<BlockLinkInstance>();
            internal BlockMerger.Context Context;
        }

        class LegacyBlockBuildingContext
        {
            internal BlockDescriptor BlockDescriptor;
            internal IEnumerable<BlockVariable> Inputs;
            internal IEnumerable<BlockVariable> Outputs;
            internal string FunctionName;
            internal string InputTypeName;
            internal string OutputTypeName;
            internal int CPIndex;
            internal List<BlockGroup> BlockGroups;
        }


        internal class VariableInstance
        {
            internal string Name = null;
            internal BlockVariable Variable = BlockVariable.Invalid;
            internal VariableInstance Parent = null;
            internal string DefaultExpression = null;
        }

        internal class VariableInstanceWithFields : VariableInstance
        {
            internal VariableInstance Instance;
            internal List<BlockVariableMatch> Fields = new List<BlockVariableMatch>();
        }

        internal class BlockVariableMatch
        {
            internal VariableInstance Source;
            internal VariableInstance Destination;
        }

        internal SimpleLegacyBlockLinker(ShaderContainer container)
        {
            this.Container = container;
        }

        internal LegacyEntryPoints GenerateLegacyEntryPoints(Template template, TemplatePass templatePass, List<CustomizationPointDescriptor> customizationPointDescriptors)
        {
            var vertexGroups = BuildBlockGroups(template, templatePass, templatePass.VertexBlocks, customizationPointDescriptors);
            var fragmentGroups = BuildBlockGroups(template, templatePass, templatePass.FragmentBlocks, customizationPointDescriptors);
            return GenerateMergerLegacyEntryPoints(template, templatePass, vertexGroups, fragmentGroups);
        }

        List<BlockGroup> BuildBlockGroups(Template template, TemplatePass templatePass, IEnumerable<BlockDescriptor> passBlocksDescriptors, List<CustomizationPointDescriptor> customizationPointDescriptors)
        {
            var results = new List<BlockGroup>();
            BlockGroup currentGroup = null;
            foreach (var templateBlockDesc in passBlocksDescriptors)
            {
                // Add the template block's data to the correct group based on the customization point
                var customizationPoint = templatePass.GetCustomizationPointForBlock(templateBlockDesc);
                // If the customization point has changed then the group changes (or if we didn't already have a group)
                if (currentGroup == null || currentGroup.CustomizationPointName != customizationPoint.Name)
                {
                    currentGroup = new BlockGroup { CustomizationPointName = customizationPoint.Name };
                    results.Add(currentGroup);
                }

                // Add all blocks that correspond to this customization point
                currentGroup.BlockDescriptors.Add(templateBlockDesc);
                var cpDesc = customizationPointDescriptors.Find((cpd) => (cpd.CustomizationPoint == customizationPoint));
                if (cpDesc.IsValid)
                {
                    foreach (var blockDesc in cpDesc.BlockDescriptors)
                        currentGroup.BlockDescriptors.Add(blockDesc);
                }
            }

            return results;
        }

        LegacyEntryPoints GenerateMergerLegacyEntryPoints(Template template, TemplatePass templatePass, List<BlockGroup> vertexGroups, List<BlockGroup> fragmentGroups)
        {
            // Without changing SRP we can't easily link multiple customization points as this would require
            // changing how the values are passed around and would currently require setting them into globals.

            var merger = new BlockMerger(Container);
            var vertexCPIndex = vertexGroups.FindIndex((g) => (g.CustomizationPointName == LegacyCustomizationPoints.VertexDescriptionCPName));
            var fragmentCPIndex = fragmentGroups.FindIndex((g) => (g.CustomizationPointName == LegacyCustomizationPoints.SurfaceDescriptionCPName));

            var vertexGroup = vertexGroups[vertexCPIndex];
            var fragmentGroup = fragmentGroups[fragmentCPIndex];
            void Build(BlockGroup group, UnityEditor.Rendering.ShaderType stageType)
            {
                group.Context = BuilderMergerContext(template, templatePass, group, stageType);
                merger.Merge(group.Context, out group.LinkingData, out group.BlocksLinkingData);
            }
            Build(vertexGroup, UnityEditor.Rendering.ShaderType.Vertex);
            Build(fragmentGroup, UnityEditor.Rendering.ShaderType.Fragment);

            // Check and annotate any variables that should be varyings
            FindAndAnnotateVaryings(vertexGroup.LinkingData, fragmentGroup.LinkingData);

            // Actually build the block descriptors for each stage's merged blocks
            var vertexDesc = merger.Build(vertexGroup.Context, vertexGroup.LinkingData, vertexGroup.BlocksLinkingData);
            var fragmentDesc = merger.Build(fragmentGroup.Context, fragmentGroup.LinkingData, fragmentGroup.BlocksLinkingData);

            // Now build the actual legacy descriptor functions. These actually access the globals to feed into the merged blocks
            var vertexLegacyContext = new LegacyBlockBuildingContext
            {
                BlockDescriptor = vertexDesc,
                Inputs = vertexGroup.Context.Inputs,
                Outputs = vertexGroup.Context.Outputs,
                FunctionName = LegacyCustomizationPoints.VertexDescriptionFunctionName,
                InputTypeName = LegacyCustomizationPoints.VertexEntryPointInputName,
                OutputTypeName = LegacyCustomizationPoints.VertexEntryPointOutputName,
                CPIndex = vertexCPIndex,
                BlockGroups = vertexGroups,
            };
            var vertexBlock = BuildLegacyBlock(vertexLegacyContext);
            var fragmentLegacyContext = new LegacyBlockBuildingContext
            {
                BlockDescriptor = fragmentDesc,
                Inputs = fragmentGroup.Context.Inputs,
                Outputs = fragmentGroup.Context.Outputs,
                FunctionName = LegacyCustomizationPoints.SurfaceDescriptionFunctionName,
                InputTypeName = LegacyCustomizationPoints.SurfaceEntryPointInputName,
                OutputTypeName = LegacyCustomizationPoints.SurfaceEntryPointOutputName,
                CPIndex = fragmentCPIndex,
                BlockGroups = fragmentGroups,
            };
            var fragmentBlock = BuildLegacyBlock(fragmentLegacyContext);

            var legacyEntryPoints = new LegacyEntryPoints();
            legacyEntryPoints.vertexDescBlockDesc = BuildSimpleBlockDescriptor(vertexBlock);
            legacyEntryPoints.fragmentDescBlockDesc = BuildSimpleBlockDescriptor(fragmentBlock);
            return legacyEntryPoints;
        }

        BlockMerger.Context BuilderMergerContext(Template template, TemplatePass templatePass, BlockGroup group, UnityEditor.Rendering.ShaderType stageType)
        {
            var customizationPoint = templatePass.FindCustomizationPoint(template, group.CustomizationPointName);
            return new BlockMerger.Context
            {
                ScopeName = group.CustomizationPointName,
                Name = $"{group.CustomizationPointName}CPFunction",
                InputTypeName = $"{group.CustomizationPointName}CPInput",
                OutputTypeName = $"{group.CustomizationPointName}CPOutput",
                Inputs = customizationPoint.Inputs,
                Outputs = customizationPoint.Outputs,
                blockDescriptors = group.BlockDescriptors,
                StageType = stageType,
            };
        }

        void FindAndAnnotateVaryings(BlockLinkInstance block0Desc, BlockLinkInstance block1Desc)
        {
            var outputTypeInstance = block0Desc.OutputInstance;
            var inputTypeInstance = block1Desc.InputInstance;
            var availableOutputs = new Dictionary<string, BlockVariableLinkInstance>();
            // Find if any input/output have matching names. If so then mark them as varyings
            foreach (var output in outputTypeInstance.ResolvedFields)
            {
                var varOverride = outputTypeInstance.FindLastVariableOverride(output.ReferenceName);
                availableOutputs[varOverride.Name] = output;
            }
            foreach (var input in inputTypeInstance.ResolvedFields)
            {
                var varOverride = inputTypeInstance.FindLastVariableOverride(input.ReferenceName);
                string name = varOverride.Name;
                if (availableOutputs.TryGetValue(name, out var matchingOutput))
                {
                    var builder = new ShaderAttribute.Builder(Container, CommonShaderAttributes.Varying);
                    input.Attributes.Add(builder.Build());
                    // Explicitly mark these as reading/writing to the stage scope. This allows easier generation later.
                    inputTypeInstance.AddOverride(input.ReferenceName, NamespaceScopes.StageScopeName, name, 0);
                    outputTypeInstance.AddOverride(matchingOutput.ReferenceName, NamespaceScopes.StageScopeName, name, 0);
                }
            }
        }

        BlockDescriptor BuildSimpleBlockDescriptor(Block block)
        {
            var builder = new BlockDescriptor.Builder(Container, block);
            return builder.Build();
        }

        Block BuildLegacyBlock(LegacyBlockBuildingContext buildingContext)
        {
            var block = buildingContext.BlockDescriptor.Block;
            var entryPointFn = block.EntryPointFunction;
            entryPointFn.GetInOutTypes(out var subBlockInputType, out var subBlockOutputType);
            
            var inputs = new List<BlockVariableMatch>();
            var outputs = new List<BlockVariableMatch>();
            var properties = new List<BlockVariable>();
            var inputsInstance = new VariableInstanceWithFields { Instance = new VariableInstance { Name = buildingContext.InputTypeName.ToLower() }};
            var outputsInstance = new VariableInstanceWithFields { Instance = new VariableInstance { Name = buildingContext.OutputTypeName.ToLower() }};
            var blockInputInstance = new VariableInstance { Name = subBlockInputType.Name.ToLower() };
            var blockOutputInstance = new VariableInstance { Name = subBlockOutputType.Name.ToLower() };
            CollectLegacyVariables(buildingContext, inputsInstance, outputsInstance, properties, blockInputInstance, blockOutputInstance);
            return BuildBlock(buildingContext, inputsInstance, outputsInstance, properties, blockInputInstance, blockOutputInstance);
        }

        void CollectLegacyVariables(LegacyBlockBuildingContext buildingContext, VariableInstanceWithFields inputsInstance, VariableInstanceWithFields outputsInstance,
            List<BlockVariable> properties, VariableInstance blockInputInstance, VariableInstance blockOutputInstance)
        {
            var blockDescriptor = buildingContext.BlockDescriptor;
            var block = blockDescriptor.Block;

            var allowedInputInstances = new Dictionary<string, VariableInstance>();
            var allowedOutputInstances = new Dictionary<string, VariableInstance>();
            foreach (var input in buildingContext.Inputs)
                allowedInputInstances.Add(input.ReferenceName, new VariableInstance { Variable = input, Parent = inputsInstance.Instance });
            foreach (var output in buildingContext.Outputs)
                allowedOutputInstances.Add(output.ReferenceName, new VariableInstance { Variable = output, Parent = outputsInstance.Instance });

            var inputOverrides = new Dictionary<string, BlockVariableNameOverride>();
            foreach (var varOverride in blockDescriptor.InputOverrides)
                inputOverrides[varOverride.DestinationName] = varOverride;
            var outputOverrides = new Dictionary<string, BlockVariableNameOverride>();
            foreach (var varOverride in blockDescriptor.OutputOverrides)
                outputOverrides[varOverride.SourceName] = varOverride;

            var inputBlocks = new Dictionary<string, BlockDescriptor>();
            if (buildingContext.CPIndex != -1 && buildingContext.BlockGroups != null)
            {
                for (var i = 0; i < buildingContext.CPIndex; ++i)
                {
                    var group = buildingContext.BlockGroups[i];
                    foreach (var blockDesc in group.BlockDescriptors)
                    {
                        inputBlocks.Add(blockDesc.Block.Name, blockDesc);
                    }
                }
            }

            var propertyVariableMap = new Dictionary<string, BlockVariable>();
            // Collect all properties from the sub-block. Also track what properties there
            // are by name so we can try to match inputs to these properties.
            foreach (var prop in block.Properties)
            {
                var propName = prop.ReferenceName;
                // Find the original property name. When block properties are merged, they may get mangled
                // so each input is unique. These overrides track the original reference name.
                if (inputOverrides.TryGetValue(prop.ReferenceName, out var varOverride))
                    propName = varOverride.SourceName;

                var source = prop.Clone(Container, propName);
                // Handle a property being declared multiple times. Only one per referenceName
                if (!propertyVariableMap.ContainsKey(propName))
                    propertyVariableMap.Add(propName, source);

                properties.Add(source);
            }

            // Copy all inputs into the sub-block struct
            foreach (var input in block.Inputs)
            {
                // We don't want to always copy this input, we have to match it against the legacy interface.
                // The allowed types are either the legacy API variables or stage varyings.
                // Legacy is a little tricky though, as a specific block may have been referenced (e.g. the pre-block)
                // in which case we have to handle and re-route that.
                VariableInstance sourceInstance = null;
                string name = input.ReferenceName;

                // Check if there's a name override, if so there's some extra searching to do
                if (inputOverrides.TryGetValue(input.ReferenceName, out var varOverride))
                {
                    // Always update the source name based upon an override
                    name = varOverride.SourceName;

                    // Check if this is matching against a template specific block
                    if (!string.IsNullOrEmpty(varOverride.SourceNamespace) && inputBlocks.TryGetValue(varOverride.SourceNamespace, out var matchingBlock))
                    {
                        foreach (var output in matchingBlock.Block.Outputs)
                        {
                            // We found an output match, create a new match (redirect to the inputs instance since these template blocks just define the input interface).
                            if (output.ReferenceName == varOverride.SourceName)
                            {
                                name = output.ReferenceName;
                                sourceInstance = new VariableInstance { Variable = output, Parent = inputsInstance.Instance };
                                break;
                            }
                        }
                    }
                    // If the scope name is the stage then force this to match to the inputs instance
                    else if (varOverride.SourceNamespace == NamespaceScopes.StageScopeName)
                    {
                        name = varOverride.SourceName;
                        var stageInput = input.Clone(Container, name);
                        sourceInstance = new VariableInstance { Variable = stageInput, Parent = inputsInstance.Instance };
                    }
                }

                // Search for a property by the given source name, if so then connect to a global variable for the property
                if (sourceInstance == null && propertyVariableMap.TryGetValue(name, out var propMatch))
                {
                    var newInput = input.Clone(Container, name);
                    sourceInstance = new VariableInstance { Variable = newInput, Parent = null };
                }

                // Try the provided inputs
                if (sourceInstance == null && allowedInputInstances.TryGetValue(input.ReferenceName, out var instanceMatch))
                {
                    name = input.ReferenceName;
                    sourceInstance = instanceMatch;
                }

                // If no source variable was found but there is a default expression, use that as the source
                if(sourceInstance == null && !string.IsNullOrEmpty(input.DefaultExpression))
                {
                    sourceInstance = new VariableInstance { DefaultExpression = input.DefaultExpression };
                }

                // No match, can't connect this to anything
                if (sourceInstance == null)
                    continue;

                var destInstance = new VariableInstance { Variable = input, Parent = blockInputInstance };
                inputsInstance.Fields.Add(new BlockVariableMatch { Source = sourceInstance, Destination = destInstance });
            }

            foreach (var output in block.Outputs)
            {
                string name = output.ReferenceName;
                // If the variable is either an allowed output or it's from the stage scope
                // then add the copy line and make it an output, otherwise skip it
                if (!allowedOutputInstances.ContainsKey(output.ReferenceName))
                {
                    if (outputOverrides.TryGetValue(output.ReferenceName, out var varOverride) && varOverride.DestinationNamespace == NamespaceScopes.StageScopeName)
                        name = varOverride.DestinationName;
                    else
                        continue;
                }

                var sourceInstance = new VariableInstance {Variable = output, Parent = blockOutputInstance };
                var destVariable = output.Clone(Container, name);
                var destinationInstance = new VariableInstance { Variable = destVariable, Parent = outputsInstance.Instance };
                outputsInstance.Fields.Add(new BlockVariableMatch { Source = sourceInstance, Destination = destinationInstance });
            }
        }

        Block BuildBlock(LegacyBlockBuildingContext buildingContext, VariableInstanceWithFields inputsInstance, VariableInstanceWithFields outputsInstance,
            List<BlockVariable> properties, VariableInstance blockInputInstance, VariableInstance blockOutputInstance)
        {
            var blockDescriptor = buildingContext.BlockDescriptor;
            var block = blockDescriptor.Block;
            var subEntryPointFn = block.EntryPointFunction;
            subEntryPointFn.GetInOutTypes(out var subBlockInputType, out var subBlockOutputType);

            var blockBuilder = new Block.Builder(Container, buildingContext.FunctionName);
            // Copy over all of the base block data
            blockBuilder.MergeTypesFunctionsDescriptors(block);

            // Build the input/output types from the collected inputs and outputs.
            // Do not put these types in the block's context, these need to be in the global namespace due to legacy reasons.
            var inputBuilder = new ShaderType.StructBuilder(Container, buildingContext.InputTypeName);
            inputBuilder.DeclaredExternally();
            foreach (var input in inputsInstance.Fields)
                inputBuilder.AddField(input.Source.Variable.Type, input.Source.Variable.ReferenceName);
            var inputType = inputBuilder.Build();
            blockBuilder.AddType(inputType);

            var outputBuilder = new ShaderType.StructBuilder(Container, buildingContext.OutputTypeName);
            foreach (var output in outputsInstance.Fields)
                outputBuilder.AddField(output.Destination.Variable.Type, output.Destination.Variable.ReferenceName);
            var outputType = outputBuilder.Build();
            blockBuilder.AddType(outputType);

            // Build up the actual description functions
            var fnBuilder = new ShaderFunction.Builder(Container, buildingContext.FunctionName, outputType);
            fnBuilder.AddInput(inputType, inputsInstance.Instance.Name);

            var allowedInputs = new HashSet<string>();
            foreach (var input in buildingContext.Inputs)
                allowedInputs.Add(input.ReferenceName);
            
            BlockVariableLinkInstance subBlockInputInstance = new BlockVariableLinkInstance { ReferenceName = blockInputInstance.Name };
            void DeclareMatch(ShaderBuilder builder, BlockVariableMatch match)
            {
                var source = match.Source;
                var dest = match.Destination;

                // If the source is another block (e.g. the parent isn't null)
                if (source.Parent != null)
                {
                    builder.AddLine($"{dest.Parent.Name}.{dest.Variable.ReferenceName} = {source.Parent.Name}.{source.Variable.ReferenceName};");
                }
                else
                {
                    // This has a default expression, use that to initialize the variable
                    if(source.DefaultExpression != null)
                    {
                        builder.AddLine($"{dest.Parent.Name}.{dest.Variable.ReferenceName} = {source.DefaultExpression};");
                    }
                    // This should fall back to trying to initialize from a uniform
                    else
                    {
                        var propData = dest.Variable;
                        var owningVar = BlockVariableLinkInstance.Construct(propData, subBlockInputInstance, null);
                        source.Variable.CopyPassPassProperty(fnBuilder, owningVar);
                    }
                }
            }


            subBlockInputType.AddVariableDeclarationStatement(fnBuilder, blockInputInstance.Name);
            var visitedInputs = new HashSet<string>();
            // Copy all inputs into the sub-block struct
            foreach (var inputData in inputsInstance.Fields)
            {
                DeclareMatch(fnBuilder, inputData);
                blockBuilder.AddInput(inputData.Source.Variable);
            }
            // Copy all properties into the sub-block struct
            foreach (var propData in properties)
                blockBuilder.AddProperty(propData);

            // Call the sub-block entry point
            subEntryPointFn.AddCallStatementWithNewReturn(fnBuilder, blockOutputInstance.Name, blockInputInstance.Name);

            // Copy all outputs into the legacy description type
            outputType.AddVariableDeclarationStatement(fnBuilder, outputsInstance.Instance.Name);
            foreach (var outputData in outputsInstance.Fields)
            {
                blockBuilder.AddOutput(outputData.Destination.Variable);
                DeclareMatch(fnBuilder, outputData);
            }

            fnBuilder.AddLine($"return {outputsInstance.Instance.Name};");
            var entryPointFn = fnBuilder.Build();
            blockBuilder.SetEntryPointFunction(entryPointFn);

            return blockBuilder.Build();
        }
    }
}
