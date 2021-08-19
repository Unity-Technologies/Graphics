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
            var mergedInputType = mergedBlockLinkInstance.InputInstance;
            var mergedOutputType = mergedBlockLinkInstance.OutputInstance;
            var blockInputType = blockLinkInstance.InputInstance;
            var blockOutputType = blockLinkInstance.OutputInstance;

            // Begin a scope for the current block
            scopes.PushScope(block.Name);

            // Try matching all input fields
            foreach (var input in blockInputType.Fields)
            {
                // Find if there's a match for this input
                var inputNameData = blockInputType.FindVariableOverride(input.ReferenceName);
                bool allowNonExactMatch = inputNameData.Swizzle != 0;
                var matchingField = scopes.Find(inputNameData.Namespace, input.Type, inputNameData.Name, allowNonExactMatch);
                // If not, create a new input on the merged block to link to
                if (matchingField == null)
                {
                    var mergedInputInstance = mergedInputType.Instance;
                    // Make the new field name unique
                    var name = $"{input.ReferenceName}_{block.Name}";
                    var availableInput = BlockVariableLinkInstance.Construct(input.Type, name, name, mergedInputInstance, mergedInputType, input.Attributes);
                    mergedInputType.AddField(availableInput);
                    matchingField = availableInput;
                    // Add the original name override so we can keep track of how to resolve this when linking later
                    mergedInputType.AddOverride(availableInput.ReferenceName, inputNameData);

                    // If this was a property, also mark this as a property on the block
                    if (availableInput.Attributes.IsProperty())
                        mergedBlockLinkInstance.AddProperty(availableInput);
                }

                // Add this as a resolved field
                var matchLink = new ResolvedFieldMatch
                {
                    Source = matchingField,
                    Destination = input,
                    SourceSwizzle = inputNameData.Swizzle
                };
                blockInputType.AddResolvedField(input.ReferenceName, matchLink);
                matchingField.TypeLinkInstance.AddResolvedField(matchingField.ReferenceName, matchLink);
            }

            foreach (var output in blockOutputType.Fields)
            {
                // Always create an output on the merged block since anyone could use the output later.
                var mergedOutputInstance = mergedOutputType.Instance;
                var name = $"{output.ReferenceName}_{block.Name}";
                var availableOutput = BlockVariableLinkInstance.Construct(output.Type, name, name, mergedOutputInstance, mergedOutputType, output.Attributes);
                mergedOutputType.AddField(availableOutput);

                // Link the new output to the block's output
                var match = new ResolvedFieldMatch
                {
                    Source = output,
                    Destination = availableOutput,
                };
                mergedOutputType.AddResolvedField(availableOutput.ReferenceName, match);

                // Add a name override for the output so we can keep track of how to resolve this later when linking
                var outputNameData = blockOutputType.FindVariableOverride(output.ReferenceName);
                mergedOutputType.AddOverride(availableOutput.ReferenceName, outputNameData);

                // Set both of these outputs as available in all of the current scopes
                scopes.SetInCurrentScopeStack(output, outputNameData.Name);
                scopes.SetInCurrentScopeStack(availableOutput, availableOutput.ReferenceName);
            }

            scopes.PopScope();
        }

        internal void Merge(Context context, out BlockLinkInstance mergedBlockLinkInstance, out List<BlockLinkInstance> blockLinkInstances)
        {
            var scopes = new NamespaceScopes();
            blockLinkInstances = new List<BlockLinkInstance>();

            // Setup the merged block's input/output instance types
            mergedBlockLinkInstance = new BlockLinkInstance(Container);
            var mergedInputType = mergedBlockLinkInstance.InputInstance;
            var mergedOutputType = mergedBlockLinkInstance.OutputInstance;
            mergedInputType.Instance.DisplayName = mergedInputType.Instance.ReferenceName = context.InputTypeName.ToLower();
            mergedOutputType.Instance.DisplayName = mergedOutputType.Instance.ReferenceName = context.OutputTypeName.ToLower();

            scopes.PushScope(NamespaceScopes.GlobalScopeName);
            if(!string.IsNullOrEmpty(context.ScopeName))
                scopes.PushScope(context.ScopeName);
                
            scopes.PushScope(context.Name);

            // Add all available inputs to the input struct
            foreach(var input in context.Inputs)
            {
                var inputInstance = BlockVariableLinkInstance.Construct(input, mergedInputType.Instance, mergedInputType);
                mergedInputType.AddField(inputInstance);
                scopes.SetInCurrentScopeStack(inputInstance);
            }

            scopes.PopScope();

            // Link all of the blocks
            foreach (var blockDesc in context.blockDescriptors)
            {
                var blockLinkInstance = new BlockLinkInstance(Container, blockDesc);
                blockLinkInstances.Add(blockLinkInstance);
                LinkBlock(blockDesc, scopes, mergedBlockLinkInstance, blockLinkInstance);
            }

            // For all available outputs, create the output field and find out who writes out to this last
            foreach (var output in context.Outputs)
            {
                var instance = BlockVariableLinkInstance.Construct(output, mergedOutputType.Instance, mergedOutputType);
                mergedOutputType.AddField(instance);
                // Check if someone wrote out to this variable
                var matchingField = scopes.Find(output.Type, output.ReferenceName, false);
                if(matchingField != null)
                {
                    var match = new ResolvedFieldMatch
                    {
                        Source = matchingField,
                        Destination = instance,
                    };
                    mergedOutputType.AddResolvedField(instance.ReferenceName, match);
                }
            }
            if (!string.IsNullOrEmpty(context.ScopeName))
                scopes.PopScope();
            scopes.PopScope();
        }

        internal ShaderFunction GenerateEntryPointFunction(Context context, BlockTypeLinkInstance inputTypeInstance, BlockTypeLinkInstance outputTypeInstance, List<BlockLinkInstance> blockLinkInstances)
        {
            var inputInstance = inputTypeInstance.Instance;
            var outputInstance = outputTypeInstance.Instance;
            var fnBuilder = new ShaderFunction.Builder(context.Name, outputInstance.Type);
            fnBuilder.AddInput(inputInstance.Type, inputInstance.ReferenceName);
            fnBuilder.Indent();

            // For each block, construct the input's type, copy all of the input fields, and then call the entry point function
            foreach (var blockLinkInstance in blockLinkInstances)
            {
                var block = blockLinkInstance.Block;
                var blockInputType = blockLinkInstance.InputInstance;
                var blockOutputType = blockLinkInstance.OutputInstance;
                var blockInputInstance = blockInputType.Instance;
                var blockOutputInstance = blockOutputType.Instance;

                fnBuilder.AddLine($"{blockInputInstance.Type.Name} {blockInputInstance.ReferenceName};");
                foreach (var input in blockInputType.Fields)
                {
                    var match = blockInputType.FindResolvedField(input.ReferenceName);
                    if (match != null)
                    {
                        string sourceSizzle = match.SourceSwizzle != 0 ? $".{SwizzleUtils.ToString(match.SourceSwizzle)}" : "";
                        fnBuilder.AddLine($"{match.Destination.Owner.ReferenceName}.{match.Destination.ReferenceName} = {match.Source.Owner.ReferenceName}.{match.Source.ReferenceName}{sourceSizzle};");
                    }
                }
                fnBuilder.AddLine($"{blockOutputInstance.Type.Name} {blockOutputInstance.ReferenceName} = {block.EntryPointFunction.Name}({blockInputInstance.ReferenceName});");
                // Add a newline between blocks for readability
                fnBuilder.NewLine();
            }

            // Generate the merged block's output type and copy all of the output fields over
            fnBuilder.AddLine($"{outputInstance.Type.Name} {outputInstance.ReferenceName};");
            foreach (var output in outputTypeInstance.Fields)
            {
                var match = outputTypeInstance.FindResolvedField(output.ReferenceName);
                if (match != null)
                    fnBuilder.AddLine($"{match.Destination.Owner.ReferenceName}.{match.Destination.ReferenceName} = {match.Source.Owner.ReferenceName}.{match.Source.ReferenceName};");
            }
            fnBuilder.AddLine($"return {outputInstance.ReferenceName};");
            fnBuilder.Deindent();

            return fnBuilder.Build(Container);
        }

        internal BlockDescriptor Build(Context context, BlockLinkInstance mergedBlockLinkInstance, List<BlockLinkInstance> blockLinkInstances)
        {
            var mergedInputType = mergedBlockLinkInstance.InputInstance;
            var mergedOutputType = mergedBlockLinkInstance.OutputInstance;
            var blockBuilder = new Block.Builder(context.Name);

            // Merge all types, functions, and descriptors. Make sure to do this first so that all dependent types/functions are already declared
            foreach (var blockLinkInstance in blockLinkInstances)
                blockBuilder.MergeTypesFunctionsDescriptors(blockLinkInstance.Block);

            // Create the input/output types
            var inputTypeBuilder = TypeUtilities.BuildStructBuilder(context.InputTypeName, mergedInputType.Fields);
            mergedInputType.Instance.Type = inputTypeBuilder.Build(Container);
            blockBuilder.AddType(mergedInputType.Instance.Type);

            var outputTypeBuilder = TypeUtilities.BuildStructBuilder(context.OutputTypeName, mergedOutputType.Fields);
            mergedOutputType.Instance.Type = outputTypeBuilder.Build(Container);
            blockBuilder.AddType(mergedOutputType.Instance.Type);

            var entryPointFunction = GenerateEntryPointFunction(context, mergedInputType, mergedOutputType, blockLinkInstances);
            blockBuilder.SetEntryPointFunction(entryPointFunction);

            // Add all of the block input/output/property variables
            foreach(var field in mergedInputType.ResolvedFields)
                blockBuilder.AddInput(field.Build(Container));
            foreach (var field in mergedOutputType.ResolvedFields)
                blockBuilder.AddOutput(field.Build(Container));
            foreach (var field in mergedBlockLinkInstance.Properties)
                blockBuilder.AddProperty(field.Build(Container));

            var mergedBlock = blockBuilder.Build(Container);

            var blockDescBuilder = new BlockDescriptor.Builder(mergedBlock);
            // Create the input/output name overrides. These are used later to know how the sub-items were mapped originally
            foreach(var varOverride in mergedInputType.NameOverrides.Overrides)
            {
                var overrideBuilder = new BlockVariableNameOverride.Builder();
                overrideBuilder.SourceNamespace = varOverride.Override.Namespace;
                overrideBuilder.SourceName = varOverride.Override.Name;
                overrideBuilder.SourceSwizzle = varOverride.Override.Swizzle;
                overrideBuilder.DestinationName = varOverride.Name;
                blockDescBuilder.AddInputOverride(overrideBuilder.Build(Container));
            }
            foreach (var varOverride in mergedOutputType.NameOverrides.Overrides)
            {
                var overrideBuilder = new BlockVariableNameOverride.Builder();

                overrideBuilder.DestinationNamespace = varOverride.Override.Namespace;
                overrideBuilder.DestinationName = varOverride.Override.Name;
                overrideBuilder.DestinationSwizzle = varOverride.Override.Swizzle;
                overrideBuilder.SourceName = varOverride.Name;
                
                blockDescBuilder.AddOutputOverride(overrideBuilder.Build(Container));
            }

            return blockDescBuilder.Build(Container);
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

