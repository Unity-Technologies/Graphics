using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderFoundry
{
    internal class BlockMerger
    {
        ShaderContainer container;
        internal ShaderContainer Container => container;

        internal class Context
        {
            internal IEnumerable<BlockDescriptor> blockDescriptors = Enumerable.Empty<BlockDescriptor>();
            internal IEnumerable<BlockVariable> Inputs = Enumerable.Empty<BlockVariable>();
            internal IEnumerable<BlockVariable> Outputs = Enumerable.Empty<BlockVariable>();
            internal string ScopeName;
            internal string Name;
            internal string InputTypeName;
            internal string OutputTypeName;
            internal UnityEditor.Rendering.ShaderType StageType;
        }

        internal BlockMerger(ShaderContainer container)
        {
            this.container = container;
        }

        internal void LinkBlock(BlockDescriptor blockDescriptor, NamespaceScopes scopes, BlockLinkInstance mergedBlockLinkInstance, BlockLinkInstance blockLinkInstance)
        {
            var block = blockDescriptor.Block;
            var mergedInputInstance = mergedBlockLinkInstance.InputInstance;
            var mergedOutputInstance = mergedBlockLinkInstance.OutputInstance;
            var blockInputInstance = blockLinkInstance.InputInstance;
            var blockOutputInstance = blockLinkInstance.OutputInstance;

            // Begin a scope for the current block
            scopes.PushScope(block.Name);

            // Try matching all input fields
            foreach (var input in blockInputInstance.Fields)
            {
                // Find if there's a match for this input
                var inputNameData = blockInputInstance.FindLastVariableOverride(input.ReferenceName);
                bool allowNonExactMatch = inputNameData.Swizzle != 0;
                var matchingField = scopes.Find(inputNameData.Namespace, input.Type, inputNameData.Name, allowNonExactMatch);
                // If not, create a new input on the merged block to link to
                if (matchingField == null)
                {
                    // Make the new field name unique
                    var name = $"{input.ReferenceName}_{block.Name}";
                    // If this field already exists (duplicate blocks) use the existing field, otherwise create a new one
                    matchingField = mergedInputInstance.FindField(name);
                    if(matchingField == null)
                    {
                        matchingField = BlockVariableLinkInstance.Construct(input, name, mergedInputInstance);
                        mergedInputInstance.AddField(matchingField);
                    }
                    // Add the original name override so we can keep track of how to resolve this when linking later
                    mergedInputInstance.AddOverride(matchingField.ReferenceName, inputNameData);

                    // If this was a property, also mark this as a property on the block
                    if (matchingField.Attributes.IsProperty())
                        mergedBlockLinkInstance.AddProperty(matchingField);
                }

                // Add this as a resolved field match
                var matchLink = new ResolvedFieldMatch
                {
                    Source = matchingField,
                    Destination = input,
                    SourceSwizzle = inputNameData.Swizzle
                };
                blockInputInstance.AddResolvedField(input.ReferenceName, matchLink);
                // Mark this resolution on the matching field's owner (the owner has a match for the field)
                matchingField.Owner.AddResolvedField(matchingField.ReferenceName, matchLink);
            }

            // All properties get auto promoted to the merged block
            foreach(var prop in block.Properties)
            {
                mergedBlockLinkInstance.AddProperty(BlockVariableLinkInstance.Construct(prop, blockInputInstance, prop.Attributes));
            }

            foreach (var output in blockOutputInstance.Fields)
            {
                // Always create an output on the merged block since anyone could use the output later.
                var name = $"{output.ReferenceName}_{block.Name}";
                // If this field already exists (duplicate blocks) use the existing field, otherwise create a new one
                var availableOutput = mergedOutputInstance.FindField(name);
                if(availableOutput == null)
                {
                    availableOutput = BlockVariableLinkInstance.Construct(output, name, mergedOutputInstance);
                    mergedOutputInstance.AddField(availableOutput);
                }

                // Link the new output to the block's output
                var match = new ResolvedFieldMatch
                {
                    Source = output,
                    Destination = availableOutput,
                };
                mergedOutputInstance.AddResolvedField(availableOutput.ReferenceName, match);

                // Add a name override for the output so we can keep track of how to resolve this later when linking
                var outputNameDatas = blockOutputInstance.FindVariableOverrides(output.ReferenceName);
                foreach (var outputNameData in outputNameDatas)
                {
                    mergedOutputInstance.AddOverride(availableOutput.ReferenceName, outputNameData);

                    // Set both of these outputs as available in all of the current scopes
                    scopes.SetInCurrentScopeStack(output, outputNameData.Name);
                    scopes.SetInCurrentScopeStack(availableOutput, availableOutput.ReferenceName);
                }
            }

            scopes.PopScope();
        }

        internal void Merge(Context context, out BlockLinkInstance mergedBlockLinkInstance, out List<BlockLinkInstance> blockLinkInstances)
        {
            var scopes = new NamespaceScopes();
            blockLinkInstances = new List<BlockLinkInstance>();

            // Setup the merged block's input/output instance types
            mergedBlockLinkInstance = new BlockLinkInstance(Container);
            var mergedInputInstance = mergedBlockLinkInstance.InputInstance;
            var mergedOutputInstance = mergedBlockLinkInstance.OutputInstance;
            mergedInputInstance.DisplayName = mergedInputInstance.ReferenceName = context.InputTypeName.ToLower();
            mergedOutputInstance.DisplayName = mergedOutputInstance.ReferenceName = context.OutputTypeName.ToLower();

            scopes.PushScope(NamespaceScopes.GlobalScopeName);
            if(!string.IsNullOrEmpty(context.ScopeName))
                scopes.PushScope(context.ScopeName);
                
            scopes.PushScope(context.Name);

            // Add all available inputs to the input struct
            foreach(var input in context.Inputs)
            {
                var inputInstance = BlockVariableLinkInstance.Construct(input, mergedInputInstance);
                mergedInputInstance.AddField(inputInstance);
                scopes.SetInCurrentScopeStack(inputInstance);
            }

            scopes.PopScope();

            // Handle duplicate blocks by renaming the in/out struct if necessary
            var usedInstanceNames = new HashSet<string>();
            usedInstanceNames.Add(mergedInputInstance.ReferenceName);
            usedInstanceNames.Add(mergedOutputInstance.ReferenceName);
            void FixInstanceName(ref string name)
            {
                int count = 0;
                string testName = name;
                while(usedInstanceNames.Contains(testName))
                {
                    ++count;
                    testName = name + count;
                }
                usedInstanceNames.Add(testName);
                name = testName;
            }

            // Link all of the blocks
            foreach (var blockDesc in context.blockDescriptors)
            {
                var blockLinkInstance = new BlockLinkInstance(Container, blockDesc);
                FixInstanceName(ref blockLinkInstance.InputInstance.ReferenceName);
                FixInstanceName(ref blockLinkInstance.OutputInstance.ReferenceName);
                blockLinkInstances.Add(blockLinkInstance);
                LinkBlock(blockDesc, scopes, mergedBlockLinkInstance, blockLinkInstance);
            }

            // For all available outputs, create the output field and find out who writes out to this last
            foreach (var output in context.Outputs)
            {
                var instance = BlockVariableLinkInstance.Construct(output, mergedOutputInstance);
                mergedOutputInstance.AddField(instance);
                // Check if someone wrote out to this variable
                var matchingField = scopes.Find(output.Type, output.ReferenceName, false);
                if(matchingField != null)
                {
                    var match = new ResolvedFieldMatch
                    {
                        Source = matchingField,
                        Destination = instance,
                    };
                    mergedOutputInstance.AddResolvedField(instance.ReferenceName, match);
                }
            }
            if (!string.IsNullOrEmpty(context.ScopeName))
                scopes.PopScope();
            scopes.PopScope();
        }

        internal ShaderFunction GenerateEntryPointFunction(Context context, BlockVariableLinkInstance inputInstance, BlockVariableLinkInstance outputInstance, List<BlockLinkInstance> blockLinkInstances)
        {
            var fnBuilder = new ShaderFunction.Builder(Container, context.Name, outputInstance.Type);
            fnBuilder.AddInput(inputInstance.Type, inputInstance.ReferenceName);
            fnBuilder.Indent();

            void DeclareMatchAssignment(ResolvedFieldMatch match)
            {
                string sourceSizzle = match.SourceSwizzle != 0 ? $".{SwizzleUtils.ToString(match.SourceSwizzle)}" : "";
                fnBuilder.AddLine($"{match.Destination.Owner.ReferenceName}.{match.Destination.ReferenceName} = {match.Source.Owner.ReferenceName}.{match.Source.ReferenceName}{sourceSizzle};");
            }

            // For each block, construct the input's type, copy all of the input fields, and then call the entry point function
            foreach (var blockLinkInstance in blockLinkInstances)
            {
                var block = blockLinkInstance.Block;
                var blockInputInstance = blockLinkInstance.InputInstance;
                var blockOutputInstance = blockLinkInstance.OutputInstance;

                blockInputInstance.Type.AddVariableDeclarationStatement(fnBuilder, blockInputInstance.ReferenceName);
                foreach (var input in blockInputInstance.Fields)
                {
                    var match = blockInputInstance.FindResolvedField(input.ReferenceName);
                    if (match != null)
                        DeclareMatchAssignment(match);
                }
                block.EntryPointFunction.AddCallStatementWithNewReturn(fnBuilder, blockOutputInstance.ReferenceName, blockInputInstance.ReferenceName);

                // Add a newline between blocks for readability
                fnBuilder.NewLine();
            }

            // Generate the merged block's output type and copy all of the output fields over
            outputInstance.Type.AddVariableDeclarationStatement(fnBuilder, outputInstance.ReferenceName);
            foreach (var output in outputInstance.Fields)
            {
                var match = outputInstance.FindResolvedField(output.ReferenceName);
                if (match != null)
                    DeclareMatchAssignment(match);
            }
            fnBuilder.AddLine($"return {outputInstance.ReferenceName};");
            fnBuilder.Deindent();

            return fnBuilder.Build();
        }

        internal BlockDescriptor Build(Context context, BlockLinkInstance mergedBlockLinkInstance, List<BlockLinkInstance> blockLinkInstances)
        {
            var mergedInputInstance = mergedBlockLinkInstance.InputInstance;
            var mergedOutputInstance = mergedBlockLinkInstance.OutputInstance;
            var blockBuilder = new Block.Builder(Container, context.Name);

            // Merge all types, functions, and descriptors. Make sure to do this first so that all dependent types/functions are already declared
            foreach (var blockLinkInstance in blockLinkInstances)
                blockBuilder.MergeTypesFunctionsDescriptors(blockLinkInstance.Block);

            // Create the input/output types
            var inputTypeBuilder = TypeUtilities.BuildStructBuilder(Container, context.InputTypeName, mergedInputInstance.Fields);
            mergedInputInstance.Type = inputTypeBuilder.Build();
            blockBuilder.AddType(mergedInputInstance.Type);

            var outputTypeBuilder = TypeUtilities.BuildStructBuilder(Container, context.OutputTypeName, mergedOutputInstance.Fields);
            mergedOutputInstance.Type = outputTypeBuilder.Build();
            blockBuilder.AddType(mergedOutputInstance.Type);

            var entryPointFunction = GenerateEntryPointFunction(context, mergedInputInstance, mergedOutputInstance, blockLinkInstances);
            blockBuilder.SetEntryPointFunction(entryPointFunction);

            // Add all of the block input/output/property variables
            foreach(var field in mergedInputInstance.ResolvedFields)
                blockBuilder.AddInput(field.Build(Container));
            foreach (var field in mergedOutputInstance.ResolvedFields)
                blockBuilder.AddOutput(field.Build(Container));
            foreach (var field in mergedBlockLinkInstance.Properties)
                blockBuilder.AddProperty(field.Build(Container));

            var mergedBlock = blockBuilder.Build();

            var blockDescBuilder = new BlockDescriptor.Builder(Container, mergedBlock);
            // Create the input/output name overrides. These are used later to know how the sub-items were mapped originally
            foreach(var varOverride in mergedInputInstance.NameOverrides.Overrides)
            {
                var overrideBuilder = new BlockVariableNameOverride.Builder(Container);
                overrideBuilder.SourceNamespace = varOverride.Override.Namespace;
                overrideBuilder.SourceName = varOverride.Override.Name;
                overrideBuilder.SourceSwizzle = varOverride.Override.Swizzle;
                overrideBuilder.DestinationName = varOverride.Name;
                blockDescBuilder.AddInputOverride(overrideBuilder.Build());
            }
            foreach (var varOverride in mergedOutputInstance.NameOverrides.Overrides)
            {
                var overrideBuilder = new BlockVariableNameOverride.Builder(Container);

                overrideBuilder.DestinationNamespace = varOverride.Override.Namespace;
                overrideBuilder.DestinationName = varOverride.Override.Name;
                overrideBuilder.DestinationSwizzle = varOverride.Override.Swizzle;
                overrideBuilder.SourceName = varOverride.Name;
                
                blockDescBuilder.AddOutputOverride(overrideBuilder.Build());
            }

            return blockDescBuilder.Build();
        }

        internal BlockDescriptor Merge(Context context)
        {
            List<BlockLinkInstance> blockLinkInstances;
            BlockLinkInstance mergedBlockLinkInstance;
            Merge(context, out mergedBlockLinkInstance, out blockLinkInstances);
            return Build(context, mergedBlockLinkInstance, blockLinkInstances);
        }
    }
}

