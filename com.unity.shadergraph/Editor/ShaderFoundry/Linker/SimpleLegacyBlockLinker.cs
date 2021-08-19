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

        class BlockVariableMatch
        {
            internal BlockVariable Source;
            internal BlockVariable Destination;
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
                var cpDesc = customizationPointDescriptors.Find((cpd) => (cpd.CustomizationPoint.Name == customizationPoint.Name));
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
                var varOverride = outputTypeInstance.FindVariableOverride(output.ReferenceName);
                availableOutputs[varOverride.Name] = output;
            }
            foreach (var input in inputTypeInstance.ResolvedFields)
            {
                var varOverride = inputTypeInstance.FindVariableOverride(input.ReferenceName);
                string name = varOverride.Name;
                if (availableOutputs.TryGetValue(name, out var matchingOutput))
                {
                    var builder = new ShaderAttribute.Builder(CommonShaderAttributes.Varying);
                    input.Attributes.Add(builder.Build(Container));
                    // Explicitly mark these as reading/writing to the stage scope. This allows easier generation later.
                    inputTypeInstance.AddOverride(input.ReferenceName, NamespaceScopes.StageScopeName, name, 0);
                    outputTypeInstance.AddOverride(matchingOutput.ReferenceName, NamespaceScopes.StageScopeName, name, 0);
                }
            }
        }

        BlockDescriptor BuildSimpleBlockDescriptor(Block block)
        {
            var builder = new BlockDescriptor.Builder(block);
            return builder.Build(Container);
        }

        Block BuildLegacyBlock(LegacyBlockBuildingContext buildingContext)
        {
            var inputs = new List<BlockVariableMatch>();
            var outputs = new List<BlockVariableMatch>();
            var properties = new List<BlockVariableMatch>();
            CollectLegacyVariables(buildingContext, inputs, outputs, properties);
            return BuildBlock(buildingContext, inputs, outputs, properties);
        }

        void CollectLegacyVariables(LegacyBlockBuildingContext buildingContext, List<BlockVariableMatch> inputs, List<BlockVariableMatch> outputs, List<BlockVariableMatch> properties)
        {
            var blockDescriptor = buildingContext.BlockDescriptor;
            var block = blockDescriptor.Block;

            var allowedInputs = new HashSet<string>();
            foreach (var input in buildingContext.Inputs)
                allowedInputs.Add(input.ReferenceName);
            var allowedOutputs = new HashSet<string>();
            foreach (var output in buildingContext.Outputs)
                allowedOutputs.Add(output.ReferenceName);

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

            // Copy all inputs into the sub-block struct
            foreach (var input in block.Inputs)
            {
                // This is a bit of a hack. Currently the generated block from the block linker has properties both as inputs and properties.
                // They're inputs because they're in the input struct, but that means we need to filter these out of the "Inputs" struct for legacy.
                // Do this currently based upon the attributes.
                if (input.Attributes.IsProperty())
                    continue;

                // We don't want to always copy this input, we have to match it against the legacy interface.
                // The allowed types are either the legacy API variables or stage varyings.
                // Legacy is a little tricky though, as a specific block may have been referenced (e.g. the pre-block)
                // in which case we have to handle and re-route that.
                string name = null;
                
                if (allowedInputs.Contains(input.ReferenceName))
                    name = input.ReferenceName;

                // Check if there's a name override, if so there's some extra searching to do
                if (inputOverrides.TryGetValue(input.ReferenceName, out var varOverride))
                {
                    // Check if this is matching against a template specific block
                    if (!string.IsNullOrEmpty(varOverride.SourceNamespace) && inputBlocks.TryGetValue(varOverride.SourceNamespace, out var matchingBlock))
                    {
                        foreach (var output in matchingBlock.Block.Outputs)
                        {
                            if (output.ReferenceName == varOverride.SourceName)
                            {
                                name = output.ReferenceName;
                                break;
                            }
                        }
                    }
                    else if (varOverride.SourceNamespace == NamespaceScopes.StageScopeName)
                        name = varOverride.SourceName;
                }

                if (name == null)
                    continue;

                var newInput = input.Clone(Container, name);
                inputs.Add(new BlockVariableMatch { Source = newInput, Destination = input });
            }
            // Copy all properties into the sub-block struct
            foreach (var prop in block.Properties)
            {
                var propName = prop.ReferenceName;
                // Get the override name for the property if it exists
                if (inputOverrides.TryGetValue(prop.ReferenceName, out var varOverride))
                    propName = varOverride.SourceName;

                var source = prop.Clone(Container, propName);
                properties.Add(new BlockVariableMatch { Source = source, Destination = prop });
            }
            foreach (var output in block.Outputs)
            {
                string name = output.ReferenceName;
                // If the variable is either an allowed output or it's from the stage scope
                // then add the copy line and make it an output, otherwise skip it
                if (!allowedOutputs.Contains(output.ReferenceName))
                {
                    if (outputOverrides.TryGetValue(output.ReferenceName, out var varOverride) && varOverride.DestinationNamespace == NamespaceScopes.StageScopeName)
                        name = varOverride.DestinationName;
                    else
                        continue;
                }

                var newOutput = output.Clone(Container, name);
                outputs.Add(new BlockVariableMatch { Source = output, Destination = newOutput });
            }
        }

        Block BuildBlock(LegacyBlockBuildingContext buildingContext, List<BlockVariableMatch> inputs, List<BlockVariableMatch> outputs, List<BlockVariableMatch> properties)
        {
            var blockDescriptor = buildingContext.BlockDescriptor;
            var block = blockDescriptor.Block;
            var subEntryPointFn = block.EntryPointFunction;

            var blockBuilder = new Block.Builder(buildingContext.FunctionName);
            // Copy over all of the base block data
            blockBuilder.MergeTypesFunctionsDescriptors(block);

            // Build the input/output types from the collected inputs and outputs
            var inputBuilder = new ShaderType.StructBuilder(buildingContext.InputTypeName);
            foreach (var input in inputs)
                inputBuilder.AddField(input.Source.Type, input.Source.ReferenceName);
            var inputType = inputBuilder.Build(Container);
            blockBuilder.AddType(inputType);
            var inputInstanceName = buildingContext.InputTypeName.ToLower();

            var outputBuilder = new ShaderType.StructBuilder(buildingContext.OutputTypeName);
            foreach (var output in outputs)
                outputBuilder.AddField(output.Destination.Type, output.Destination.ReferenceName);
            var outputType = outputBuilder.Build(Container);
            blockBuilder.AddType(outputType);
            var outputInstanceName = buildingContext.OutputTypeName.ToLower();

            subEntryPointFn.GetInOutTypes(out var subBlockInputType, out var subBlockOutputType);
            var subBlockInputInstanceName = subBlockInputType.Name.ToLower();
            var subBlockOutputInstanceName = subBlockOutputType.Name.ToLower();

            // Build up the actual description functions
            var fnBuilder = new ShaderFunction.Builder(buildingContext.FunctionName, outputType);
            fnBuilder.AddInput(inputType, inputInstanceName);

            fnBuilder.AddLine($"{subBlockInputType.Name} {subBlockInputInstanceName};");
            var visitedInputs = new HashSet<string>();
            // Copy all inputs into the sub-block struct
            foreach (var inputData in inputs)
            {
                fnBuilder.AddLine($"{subBlockInputInstanceName}.{inputData.Destination.ReferenceName} = {inputInstanceName}.{inputData.Source.ReferenceName};");
                blockBuilder.AddInput(inputData.Source);
            }
            // Copy all properties into the sub-block struct
            BlockVariableLinkInstance subBlockInputInstance = new BlockVariableLinkInstance { ReferenceName = subBlockInputInstanceName };
            foreach (var propData in properties)
            {
                var dest = BlockVariableLinkInstance.Construct(propData.Destination, subBlockInputInstance, null);
                propData.Source.CopyPassPassProperty(fnBuilder, dest);
                blockBuilder.AddProperty(propData.Source);
            }
            // Call the sub-block entry point
            fnBuilder.AddLine($"{subBlockOutputType.Name} {subBlockOutputInstanceName} = {subEntryPointFn.Name}({subBlockInputInstanceName});");
            // Copy all outputs into the legacy description type
            fnBuilder.AddLine($"{outputType.Name} {outputInstanceName};");
            foreach (var outputData in outputs)
            {
                blockBuilder.AddOutput(outputData.Destination);
                fnBuilder.AddLine($"{outputInstanceName}.{outputData.Destination.ReferenceName} = {subBlockOutputInstanceName}.{outputData.Source.ReferenceName};");
            }

            fnBuilder.AddLine($"return {outputInstanceName};");
            var entryPointFn = fnBuilder.Build(Container);
            blockBuilder.SetEntryPointFunction(entryPointFn);

            return blockBuilder.Build(Container);
        }
    }
}
