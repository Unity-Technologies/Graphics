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
            internal CustomizationPoint CustomizationPoint = CustomizationPoint.Invalid;
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
            internal VariableInstance Parent = null;
            internal BlockVariable Variable = BlockVariable.Invalid;
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
            // The default expression to initialize the destination to if the source is null
            internal string DefaultExpression = null;
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

        List<BlockGroup> BuildBlockGroups(Template template, TemplatePass templatePass, IEnumerable<BlockDescriptor> passBlocksDescriptors, IEnumerable<CustomizationPointDescriptor> customizationPointDescriptors)
        {
            // The blocks for a pass are in two different places: the pass defaults and the
            // customization point descriptors. For block merging, we'll need to know what blocks to merge together.
            // To do this, first group neighboring blocks together if they share a customization point.
            var results = new List<BlockGroup>();
            BlockGroup currentGroup = null;
            foreach (var templateBlockDesc in passBlocksDescriptors)
            {
                var customizationPoint = templatePass.GetCustomizationPointForBlock(templateBlockDesc);
                // If the customization point has changed then the group changes (or if we didn't already have a group)
                if (currentGroup == null || currentGroup.CustomizationPoint != customizationPoint)
                {
                    currentGroup = new BlockGroup { CustomizationPoint = customizationPoint };
                    results.Add(currentGroup);
                }
                currentGroup.BlockDescriptors.Add(templateBlockDesc);
            }

            // Once pass blocks are merged, we can append each group with the CustomizationPointDescriptor's blocks
            foreach(var group in results)
            {
                if (group.CustomizationPoint.IsValid == false)
                    continue;

                // Find the CustomizationPointDescriptor matching the group's CustomizationPoint and append all of it's block descriptors
                foreach(var cpDesc in customizationPointDescriptors)
                {
                    if (cpDesc.CustomizationPoint != group.CustomizationPoint)
                        continue;

                    foreach (var blockDesc in cpDesc.BlockDescriptors)
                        group.BlockDescriptors.Add(blockDesc);
                    // Should this break? Does it make sense for a user to have two CustomizationPointDescriptors with the same CustomizationPoint?
                    break;
                }
            }

            return results;
        }

        LegacyEntryPoints GenerateMergerLegacyEntryPoints(Template template, TemplatePass templatePass, List<BlockGroup> vertexGroups, List<BlockGroup> fragmentGroups)
        {
            // Without changing SRP we can't easily link multiple customization points as this would require
            // changing how the values are passed around and would currently require setting them into globals.

            var merger = new BlockMerger(Container);
            var vertexCPIndex = vertexGroups.FindIndex((g) => (g.CustomizationPoint.Name == LegacyCustomizationPoints.VertexDescriptionCPName));
            var fragmentCPIndex = fragmentGroups.FindIndex((g) => (g.CustomizationPoint.Name == LegacyCustomizationPoints.SurfaceDescriptionCPName));

            var vertexGroup = vertexGroups[vertexCPIndex];
            var fragmentGroup = fragmentGroups[fragmentCPIndex];
            BlockDescriptor Build(BlockGroup group, UnityEditor.Rendering.ShaderType stageType)
            {
                group.Context = BuilderMergerContext(template, templatePass, group, stageType);
                merger.Merge(group.Context, out group.LinkingData, out group.BlocksLinkingData);
                return merger.Build(group.Context, group.LinkingData, group.BlocksLinkingData);
            }
            var vertexDesc = Build(vertexGroup, UnityEditor.Rendering.ShaderType.Vertex);
            var fragmentDesc = Build(fragmentGroup, UnityEditor.Rendering.ShaderType.Fragment);

            // Find user custom varyings from the interface between the vertex/fragment stage
            List<VaryingVariable> customVaryings = new List<VaryingVariable>();
            FindVaryings(vertexDesc, fragmentDesc, vertexGroup.Context.Outputs, fragmentGroup.Context.Inputs, customVaryings);

            // Make new block variables for the varyings
            List<BlockVariable> varyingBlockVariables = new List<BlockVariable>();
            foreach(var varying in customVaryings)
            {
                var builder = new BlockVariable.Builder(Container);
                builder.ReferenceName = builder.DisplayName = varying.Name;
                builder.Type = varying.Type;
                varyingBlockVariables.Add(builder.Build());
            }

            // Append the stage interface's allowed variables to include the varyings
            var vertexOutputs = new List<BlockVariable>();
            vertexOutputs.AddRange(vertexGroup.Context.Outputs);
            vertexOutputs.AddRange(varyingBlockVariables);

            var fragmentInputs = new List<BlockVariable>();
            fragmentInputs.AddRange(fragmentGroup.Context.Inputs);
            fragmentInputs.AddRange(varyingBlockVariables);

            // Now build the actual legacy descriptor functions. These actually access the globals to feed into the merged blocks
            var vertexLegacyContext = new LegacyBlockBuildingContext
            {
                BlockDescriptor = vertexDesc,
                Inputs = vertexGroup.Context.Inputs,
                Outputs = vertexOutputs,
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
                Inputs = fragmentInputs,
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
            legacyEntryPoints.customInterpolants = customVaryings;
            return legacyEntryPoints;
        }

        BlockMerger.Context BuilderMergerContext(Template template, TemplatePass templatePass, BlockGroup group, UnityEditor.Rendering.ShaderType stageType)
        {
            var customizationPointName = group.CustomizationPoint.Name;
            var customizationPoint = templatePass.FindCustomizationPoint(template, customizationPointName);
            return new BlockMerger.Context
            {
                ScopeName = customizationPointName,
                Name = $"{customizationPointName}CPFunction",
                InputTypeName = $"{customizationPointName}CPInput",
                OutputTypeName = $"{customizationPointName}CPOutput",
                Inputs = customizationPoint.Inputs,
                Outputs = customizationPoint.Outputs,
                blockDescriptors = group.BlockDescriptors,
                StageType = stageType,
            };
        }

        void FindVaryings(BlockDescriptor block0Desc, BlockDescriptor block1Desc, IEnumerable<BlockVariable> existingOutputs,  IEnumerable<BlockVariable> existingInputs, List<VaryingVariable> customInterpolants)
        {
            var existingOutputNames = new HashSet<string>();
            foreach (var output in existingOutputs)
                existingOutputNames.Add(output.ReferenceName);
            var existingInputNames = new HashSet<string>();
            foreach (var input in existingInputs)
                existingInputNames.Add(input.ReferenceName);

            var outputOverrides = new VariableOverrideSet();
            outputOverrides.BuildOutputOverrides(block0Desc.OutputOverrides);
            var inputOverrides = new VariableOverrideSet();
            inputOverrides.BuildInputOverrides(block1Desc.InputOverrides);

            var availableOutputs = new Dictionary<string, BlockVariable>();
            // Find if any input/output have matching names. If so then create a varying
            foreach (var output in block0Desc.Block.Outputs)
            {
                // Make sure to check all name output overrides
                var overrides = outputOverrides.FindVariableOverrides(output.ReferenceName);
                foreach (var varOverride in overrides)
                {
                    // If this is already an available output ignore this as a custom interpolant (already part of the api)
                    if(!existingOutputNames.Contains(varOverride.Name))
                        availableOutputs[varOverride.Name] = output;
                }
            }
            foreach (var input in block1Desc.Block.Inputs)
            {
                var varOverride = inputOverrides.FindLastVariableOverride(input.ReferenceName);
                string name = varOverride.Name;
                if (!existingInputNames.Contains(name) && availableOutputs.TryGetValue(name, out var matchingOutput))
                    customInterpolants.Add(new VaryingVariable { Type = input.Type, Name = name });
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

            var inputOverrides = new VariableOverrideSet();
            foreach (var varOverride in blockDescriptor.InputOverrides)
                inputOverrides.Add(varOverride.DestinationName, varOverride.SourceNamespace, varOverride.SourceName, varOverride.SourceSwizzle);
            var outputOverrides = new VariableOverrideSet();
            foreach (var varOverride in blockDescriptor.OutputOverrides)
                outputOverrides.Add(varOverride.SourceName, varOverride.DestinationNamespace, varOverride.DestinationName, varOverride.DestinationSwizzle);

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
                if (inputOverrides.FindLastVariableOverride(prop.ReferenceName, out var varOverride))
                    propName = varOverride.Name;

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

                // Check if there's a name override, if so there's some extra searching to do.
                // Should this not take the last, but the last matching?
                if (inputOverrides.FindLastVariableOverride(input.ReferenceName, out var varOverride))
                {
                    // Always update the source name based upon an override
                    name = varOverride.Name;

                    // If the scope name is the stage then check the allowed inputs to see if there's a match
                    if (varOverride.Namespace == NamespaceScopes.StageScopeName && allowedInputInstances.TryGetValue(name, out var stageMatch))
                    {
                        name = input.ReferenceName;
                        sourceInstance = stageMatch;
                    }
                    // Check if this is matching against a template specific block
                    else if (!string.IsNullOrEmpty(varOverride.Namespace) && inputBlocks.TryGetValue(varOverride.Namespace, out var matchingBlock))
                    {
                        foreach (var output in matchingBlock.Block.Outputs)
                        {
                            // We found an output match, create a new match (redirect to the inputs instance since these template blocks just define the input interface).
                            if (output.ReferenceName == varOverride.Name)
                            {
                                name = output.ReferenceName;
                                sourceInstance = new VariableInstance { Variable = output, Parent = inputsInstance.Instance };
                                break;
                            }
                        }
                    }
                }

                // Search for a property by the given source name, if so then connect to a global variable for the property
                if (sourceInstance == null && propertyVariableMap.TryGetValue(name, out var propMatch))
                {
                    var newInput = input.Clone(Container, name);
                    sourceInstance = new VariableInstance { Variable = newInput, Parent = null };
                }

                // Try the provided inputs
                if (sourceInstance == null && allowedInputInstances.TryGetValue(name, out var instanceMatch))
                {
                    name = input.ReferenceName;
                    sourceInstance = instanceMatch;
                }

                // If no source variable was found but there is a default expression
                string defaultExpression = null;
                if(sourceInstance == null && !string.IsNullOrEmpty(input.DefaultExpression))
                {
                    defaultExpression = input.DefaultExpression;
                }

                // If there's no match and no default expression, this can't be connected to anything.
                // Should this zero-out the value if it's not an opaque type?
                if (sourceInstance == null && defaultExpression == null)
                    continue;

                var destInstance = new VariableInstance { Variable = input, Parent = blockInputInstance };
                var match = new BlockVariableMatch { Source = sourceInstance, Destination = destInstance, DefaultExpression = defaultExpression };
                inputsInstance.Fields.Add(match);
            }

            var outputSourcesByDestinationName = new Dictionary<string, VariableInstance>();
            foreach (var output in block.Outputs)
            {
                // Walk all overrides (a single out can have multiple names that can be matched against)
                var overrides = outputOverrides.FindVariableOverrides(output.ReferenceName);
                foreach (var varOverride in overrides)
                {
                    string name = varOverride.Name;

                    // Only write to allowed outputs (which includes stage variables)
                    VariableInstance destinationInstance;
                    if (!allowedOutputInstances.TryGetValue(name, out destinationInstance))
                        continue;

                    // Check if we've already created an output, if so then replace the source's variable to now point at this block's output
                    if(outputSourcesByDestinationName.TryGetValue(name, out var oldInstance))
                    {
                        oldInstance.Variable = output;
                        continue;
                    }

                    // Otherwise this is a new match. Create a new variable as the destination and then hook up instances for a match
                    var sourceInstance = new VariableInstance {Variable = output, Parent = blockOutputInstance };
                    outputsInstance.Fields.Add(new BlockVariableMatch { Source = sourceInstance, Destination = destinationInstance });
                    outputSourcesByDestinationName[name] = sourceInstance;
                }
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
            // Mark the input as externally declared. This is currently built in the legacy linker
            // and does some special logic that we don't want to replicate here yet.
            inputBuilder.DeclaredExternally();
            foreach (var input in inputsInstance.Fields)
            {
                // Inputs without a source aren't promoted to the block's inputs.
                // They might still initialize a sub-block input though.
                if (input.Source == null)
                    continue;
                inputBuilder.AddField(input.Source.Variable.Type, input.Source.Variable.ReferenceName);
                blockBuilder.AddInput(input.Source.Variable);
            }
            var inputType = inputBuilder.Build();
            blockBuilder.AddType(inputType);

            var outputBuilder = new ShaderType.StructBuilder(Container, buildingContext.OutputTypeName);
            foreach (var output in outputsInstance.Fields)
            {
                // Outputs without a destination aren't promoted to the block's outputs.
                if (output.Destination == null)
                    continue;
                outputBuilder.AddField(output.Destination.Variable.Type, output.Destination.Variable.ReferenceName);
                blockBuilder.AddOutput(output.Destination.Variable);
            }
            var outputType = outputBuilder.Build();
            blockBuilder.AddType(outputType);

            // Copy all properties into the sub-block struct
            foreach (var propData in properties)
                blockBuilder.AddProperty(propData);

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

                // If the block has a source then initialize it from the source
                if(source != null)
                {
                    // If the source is another block (i.e. the parent is valid)
                    if (source.Parent != null)
                    {
                        builder.AddLine($"{dest.Parent.Name}.{dest.Variable.ReferenceName} = {source.Parent.Name}.{source.Variable.ReferenceName};");
                    }
                    // If the source's parent is null, then this connects to some globals
                    else
                    {
                        var propData = dest.Variable;
                        var owningVar = BlockVariableLinkInstance.Construct(propData, subBlockInputInstance, null);
                        source.Variable.CopyPassPassProperty(fnBuilder, owningVar);
                    }
                }
                // If there's no source but there is a default expression
                else if(match.DefaultExpression != null)
                {
                    builder.AddLine($"{dest.Parent.Name}.{dest.Variable.ReferenceName} = {match.DefaultExpression};");
                }
            }


            subBlockInputType.AddVariableDeclarationStatement(fnBuilder, blockInputInstance.Name);
            // Copy all inputs into the sub-block struct
            foreach (var inputData in inputsInstance.Fields)
                DeclareMatch(fnBuilder, inputData);

            // Call the sub-block entry point
            subEntryPointFn.AddCallStatementWithNewReturn(fnBuilder, blockOutputInstance.Name, blockInputInstance.Name);

            // Copy all outputs into the legacy description type
            outputType.AddVariableDeclarationStatement(fnBuilder, outputsInstance.Instance.Name);
            foreach (var outputData in outputsInstance.Fields)
                DeclareMatch(fnBuilder, outputData);

            fnBuilder.AddLine($"return {outputsInstance.Instance.Name};");
            var entryPointFn = fnBuilder.Build();
            blockBuilder.SetEntryPointFunction(entryPointFn);

            return blockBuilder.Build();
        }
    }
}
