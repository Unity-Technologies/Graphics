using System.Collections.Generic;

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
            internal List<BlockSequenceElement> BlockSequenceElements = new List<BlockSequenceElement>();
            internal BlockLinkInstance LinkingData;
            internal List<BlockLinkInstance> BlocksLinkingData = new List<BlockLinkInstance>();
            internal BlockMerger.Context Context;
        }

        class LegacyBlockBuildingContext
        {
            internal BlockSequenceElement BlockSequenceElement;
            internal IEnumerable<BlockVariable> Inputs;
            internal IEnumerable<BlockVariable> Outputs;
            internal string FunctionName;
            internal string InputTypeName;
            internal string OutputTypeName;
            internal int CPIndex;
            internal List<BlockGroup> BlockGroups;
        }

        internal SimpleLegacyBlockLinker(ShaderContainer container)
        {
            this.Container = container;
        }

        internal LegacyEntryPoints GenerateLegacyEntryPoints(Template template, TemplatePass templatePass, List<CustomizationPointImplementation> customizationPointImplementations)
        {
            var vertexGroups = BuildBlockGroups(templatePass.GetStageDescription(PassStageType.Vertex), customizationPointImplementations);
            var fragmentGroups = BuildBlockGroups(templatePass.GetStageDescription(PassStageType.Fragment), customizationPointImplementations);
            return GenerateMergerLegacyEntryPoints(template, templatePass, vertexGroups, fragmentGroups);
        }

        List<BlockGroup> BuildBlockGroups(StageDescription stageDescription, IEnumerable<CustomizationPointImplementation> customizationPointImplementations)
        {
            // Build block groups based upon customization points. All neighboring blocks not in a customization point
            // will be grouped together. Customization points will have their own group.
            var results = new List<BlockGroup>();
            BlockGroup currentGroup = null;
            foreach (var blockSequenceElement in stageDescription.Elements)
            {
                var customizationPoint = blockSequenceElement.CustomizationPoint;
                // If the customization point has changed then the group changes (or if we didn't already have a group)
                if (currentGroup == null || currentGroup.CustomizationPoint != customizationPoint)
                {
                    currentGroup = new BlockGroup { CustomizationPoint = customizationPoint };
                    results.Add(currentGroup);
                }

                if (blockSequenceElement.Block.IsValid)
                    currentGroup.BlockSequenceElements.Add(blockSequenceElement);
            }
            // Now add fill out each group that has a customization point. To do this, we append the default block elements
            // of the customization point and all of the block elements in each customization point implementation.
            foreach (var group in results)
            {
                if (group.CustomizationPoint.IsValid == false)
                    continue;

                foreach (var blockElement in group.CustomizationPoint.DefaultBlockSequenceElements)
                    group.BlockSequenceElements.Add(blockElement);

                // Find the CustomizationPointImplementations matching the group's CustomizationPoint and append all of its block elements
                foreach (var customizationPointImplementation in customizationPointImplementations)
                {
                    if (customizationPointImplementation.CustomizationPoint != group.CustomizationPoint)
                        continue;

                    foreach (var blockSequenceElement in customizationPointImplementation.BlockSequenceElements)
                        group.BlockSequenceElements.Add(blockSequenceElement);
                    // Should this break? Does it make sense for a user to have two CustomizationPointImplementations with the same CustomizationPoint?
                    break;
                }
            }

            return results;
        }

        Block BuildMergedBlock(BlockMerger merger, BlockGroup group, UnityEditor.Rendering.ShaderType stageType)
        {
            var customizationPoint = group.CustomizationPoint;
            var customizationPointName = customizationPoint.Name;
            // Build all of the block link instances
            List<BlockLinkInstance> blockLinkInstances = new List<BlockLinkInstance>();
            foreach (var blockSequenceElement in group.BlockSequenceElements)
            {
                var blockLinkInstance = new BlockLinkInstance(Container);
                blockLinkInstance.Build(blockSequenceElement);
                blockLinkInstances.Add(blockLinkInstance);
            }
            // Run the merger to generate linking information
            group.Context = new BlockMerger.Context
            {
                Inputs = customizationPoint.Inputs,
                Outputs = customizationPoint.Outputs,
                BlockLinkInstances = blockLinkInstances,
            };
            var mergedBlockLinkInstance = merger.Link(group.Context);

            // Generate the actual block from the linked results
            var declarationContext = new MergedBlockDeclaration.Context
            {
                BlockLinkInstances = blockLinkInstances,
                BlockName = customizationPointName,
                InputTypeName = $"{customizationPointName}CPInput",
                OutputTypeName = $"{customizationPointName}CPOutput",
                EntryPointFunctionName = "Apply",
                MergedBlockLinkInstance = mergedBlockLinkInstance,
            };
            var generator = new MergedBlockDeclaration(Container);
            return generator.Build(declarationContext);
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

            var mergedVertexBlock = BuildMergedBlock(merger, vertexGroup, UnityEditor.Rendering.ShaderType.Vertex);
            var mergedFragmentBlock = BuildMergedBlock(merger, fragmentGroup, UnityEditor.Rendering.ShaderType.Fragment);

            // Find user custom varyings from the interface between the vertex/fragment stage
            List<VaryingVariable> customVaryings = new List<VaryingVariable>();
            FindVaryings(mergedVertexBlock, mergedFragmentBlock, vertexGroup.Context.Outputs, fragmentGroup.Context.Inputs, customVaryings);

            // Make new block variables for the varyings
            List<BlockVariable> varyingBlockVariables = new List<BlockVariable>();
            foreach (var varying in customVaryings)
            {
                var builder = new BlockVariable.Builder(Container);
                builder.Name = varying.Name;
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
                BlockSequenceElement = BuildSimpleBlockSequenceElement(mergedVertexBlock),
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
                BlockSequenceElement = BuildSimpleBlockSequenceElement(mergedFragmentBlock),
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
            legacyEntryPoints.vertexDescBlockSequenceElement = BuildSimpleBlockSequenceElement(vertexBlock);
            legacyEntryPoints.fragmentDescBlockSequenceElement = BuildSimpleBlockSequenceElement(fragmentBlock);
            legacyEntryPoints.customInterpolants = customVaryings;
            return legacyEntryPoints;
        }

        void FindVaryings(Block block0, Block block1, IEnumerable<BlockVariable> existingOutputs, IEnumerable<BlockVariable> existingInputs, List<VaryingVariable> customInterpolants)
        {
            var existingOutputNames = new HashSet<string>();
            foreach (var output in existingOutputs)
                existingOutputNames.Add(output.Name);
            var existingInputNames = new HashSet<string>();
            foreach (var input in existingInputs)
                existingInputNames.Add(input.Name);

            var availableOutputs = new Dictionary<string, BlockVariable>();
            void TryAddOutput(BlockVariable output, string fieldName)
            {
                if (!existingOutputNames.Contains(fieldName))
                    availableOutputs[fieldName] = output;
            }

            // Find if any input/output have matching names. If so then create a varying
            foreach (var output in block0.Outputs)
            {
                var fieldName = output.Name;
                TryAddOutput(output, fieldName);

                foreach (var aliasAtt in AliasAttribute.ForEach(output.Attributes))
                    TryAddOutput(output, aliasAtt.AliasName);
            }

            void TryLinkInput(BlockVariable input, string fieldName)
            {
                if (!existingInputNames.Contains(fieldName) && availableOutputs.TryGetValue(fieldName, out var matchingOutput))
                    customInterpolants.Add(new VaryingVariable { Type = input.Type, Name = fieldName });
            }

            foreach (var input in block1.Inputs)
            {
                string fieldName = input.Name;
                TryLinkInput(input, fieldName);

                foreach (var aliasAtt in AliasAttribute.ForEach(input.Attributes))
                    TryLinkInput(input, aliasAtt.AliasName);
            }
        }

        BlockSequenceElement BuildSimpleBlockSequenceElement(Block block)
        {
            var builder = new BlockSequenceElement.Builder(Container, block);
            return builder.Build();
        }

        Block BuildLegacyBlock(LegacyBlockBuildingContext buildingContext)
        {
            // Build a block link instance for the block
            var blockLinkInstance = new BlockLinkInstance(Container);
            blockLinkInstance.Build(buildingContext.BlockSequenceElement);

            // Invoke the merger to find connections
            var mergerContext = new BlockMerger.Context
            {
                BlockLinkInstances = new List<BlockLinkInstance> { blockLinkInstance },
                Inputs = buildingContext.Inputs,
                Outputs = buildingContext.Outputs,
            };
            var merger = new BlockMerger(Container);
            merger.Mode = BlockMerger.MergeMode.Strict;
            var mergedBlockLinkInstance = merger.Link(mergerContext);
            // Manually name the input/output instance variables
            mergedBlockLinkInstance.InputInstance.Name = buildingContext.InputTypeName.ToLower();
            mergedBlockLinkInstance.OutputInstance.Name = buildingContext.OutputTypeName.ToLower();
            // Generate the actual merged block
            return BuildBlock(buildingContext, mergedBlockLinkInstance.InputInstance, mergedBlockLinkInstance.OutputInstance, blockLinkInstance.InputInstance, blockLinkInstance.OutputInstance);
        }

        Block BuildBlock(LegacyBlockBuildingContext buildingContext, VariableLinkInstance inputsInstance, VariableLinkInstance outputsInstance,
            VariableLinkInstance blockInputInstance, VariableLinkInstance blockOutputInstance)
        {
            var blockSequenceElement = buildingContext.BlockSequenceElement;
            var block = blockSequenceElement.Block;

            var blockBuilder = new Block.Builder(Container, buildingContext.FunctionName);
            // Copy over all of the base block data
            blockBuilder.MergeTypesFunctionsDescriptors(block);

            // The private blocks currently don't get merged in because they don't really exist (they're basically stub for generation currently),
            // however these blocks may contain descriptors for defines, includes, etc... that need to be included.
            // Note: This probably needs to handle properties eventually too.
            foreach (var group in buildingContext.BlockGroups)
            {
                if (!group.CustomizationPoint.IsValid)
                {
                    foreach (var groupBlockSequenceElement in group.BlockSequenceElements)
                        blockBuilder.MergeDescriptors(groupBlockSequenceElement.Block);
                }
            }

            var inputType = BuildInputType(buildingContext.InputTypeName, blockInputInstance.Fields);
            inputsInstance.Type = inputType;
            blockBuilder.AddType(inputType);

            var outputType = BuildOutputType(buildingContext.OutputTypeName, outputsInstance.Fields);
            outputsInstance.Type = outputType;
            blockBuilder.AddType(outputType);

            var entryPointFn = BuildEntryPointFunction(buildingContext, inputsInstance, outputsInstance, blockInputInstance, blockOutputInstance);
            //fnBuilder.Build();
            blockBuilder.SetLegacyEntryPointFunction(entryPointFn);

            return blockBuilder.Build();
        }

        ShaderType BuildInputType(string typeName, IEnumerable<VariableLinkInstance> fields)
        {
            // Build the input/output types from the collected inputs and outputs.
            // Do not put these types in the block's context, these need to be in the global namespace due to legacy reasons.
            var inputBuilder = new ShaderType.StructBuilder(Container, typeName);
            // Mark the input as externally declared. This is currently built in the legacy linker
            // and does some special logic that we don't want to replicate here yet.
            inputBuilder.DeclaredExternally();
            foreach (var input in fields)
            {
                // Inputs without a source aren't promoted to the block's inputs.
                // They might still initialize a sub-block input though.
                if (input.Source == null)
                    continue;

                var sourceVar = input.Source;
                inputBuilder.AddField(BuildField(sourceVar));
            }
            return inputBuilder.Build();
        }

        ShaderType BuildOutputType(string typeName, IEnumerable<VariableLinkInstance> fields)
        {
            var outputBuilder = new ShaderType.StructBuilder(Container, typeName);
            foreach (var output in fields)
            {
                if (output.Source == null)
                    continue;

                var destVar = output;
                outputBuilder.AddField(BuildField(destVar));
            }
            return outputBuilder.Build();
        }

        StructField BuildField(VariableLinkInstance field)
        {
            var fieldBuilder = new StructField.Builder(Container, field.Name, field.Type);
            foreach (var attribute in field.Attributes)
                fieldBuilder.AddAttribute(attribute);
            return fieldBuilder.Build();
        }

        ShaderFunction BuildEntryPointFunction(LegacyBlockBuildingContext buildingContext,
            VariableLinkInstance inputsInstance, VariableLinkInstance outputsInstance,
            VariableLinkInstance blockInputInstance, VariableLinkInstance blockOutputInstance)
        {
            var blockSequenceElement = buildingContext.BlockSequenceElement;
            var block = blockSequenceElement.Block;
            var subEntryPointFn = block.EntryPointFunction;

            // Build up the actual description functions
            var fnBuilder = new ShaderFunction.Builder(Container, buildingContext.FunctionName, outputsInstance.Type);
            fnBuilder.AddInput(inputsInstance.Type, inputsInstance.Name);


            var subBlockInputInstance = new VariableLinkInstance { Container = Container, Name = blockInputInstance.Name };
            fnBuilder.AddVariableDeclarationStatement(blockInputInstance.Type, blockInputInstance.Name);
            // Copy all inputs into the sub-block struct
            foreach (var inputData in blockInputInstance.Fields)
                DeclareMatch(fnBuilder, inputData, subBlockInputInstance);

            // Call the sub-block entry point
            fnBuilder.CallFunction(subEntryPointFn, blockInputInstance.Name);

            // Copy all outputs into the legacy description type
            fnBuilder.AddVariableDeclarationStatement(outputsInstance.Type, outputsInstance.Name);
            foreach (var outputData in outputsInstance.Fields)
                DeclareMatch(fnBuilder, outputData, subBlockInputInstance);

            fnBuilder.AddLine($"return {outputsInstance.Name};");
            return fnBuilder.Build();
        }

        void DeclareMatch(ShaderFunction.Builder builder, VariableLinkInstance match, VariableLinkInstance subBlockInputInstance)
        {
            var source = match.Source;
            var dest = match;

            if (match.IsUniform)
            {
                var propData = dest;
                var owningVar = subBlockInputInstance.CreateSubField(propData.Type, propData.Name, propData.Attributes);
                UniformDeclaration.Copy(builder, match, owningVar);
            }
            // If the source is another block (i.e. the parent is valid)
            else if (source != null && source.Parent != null)
            {
                builder.AddLine($"{dest.Parent.Name}.{dest.Name} = {source.Parent.Name}.{source.Name};");
            }
            else
                RecursivelyBuildDefaultValues(builder, match);
        }

        bool RecursivelyBuildDefaultValues(ShaderFunction.Builder builder, VariableLinkInstance variable)
        {
            // If this field has a default value, emit that
            var defaultValueAtt = DefaultValueAttribute.Find(variable.Attributes);
            if (defaultValueAtt != null)
            {
                builder.AddLine($"{variable.GetDeclarationString()} = {defaultValueAtt.DefaultValue};");
                return true;
            }
            // Otherwise, recurse into each sub-field to see if it has a default
            else
            {
                bool allHaveDefaults = true;
                foreach (var subField in variable.Type.StructFields)
                {
                    var subFieldVar = variable.CreateSubField(subField.Type, subField.Name, subField.Attributes);
                    allHaveDefaults &= RecursivelyBuildDefaultValues(builder, subFieldVar);
                }
                return allHaveDefaults;
            }
        }
    }
}
