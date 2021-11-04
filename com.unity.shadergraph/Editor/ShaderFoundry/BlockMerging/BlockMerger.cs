using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderFoundry
{
    internal class MergeResult
    {
        internal BlockInstance BlockInstance;
        internal List<FieldOverride> InputOverrides = new List<FieldOverride>();
        internal List<FieldOverride> OutputOverrides = new List<FieldOverride>();
    }

    internal class BlockMerger
    {
        ShaderContainer container;
        internal ShaderContainer Container => container;

        internal class Context
        {
            internal IEnumerable<BlockInstance> blockInstances = Enumerable.Empty<BlockInstance>();
            internal IEnumerable<BlockVariable> Inputs = Enumerable.Empty<BlockVariable>();
            internal IEnumerable<BlockVariable> Outputs = Enumerable.Empty<BlockVariable>();
            internal string ScopeName;
            internal string Name;
            internal string InputTypeName;
            internal string OutputTypeName;
            internal UnityEditor.Rendering.ShaderType StageType;
        }

        class PropertyTuple
        {
            internal BlockVariable Variable;
            internal bool DeclareAsProperty = true;
        }

        internal BlockMerger(ShaderContainer container)
        {
            this.container = container;
        }

        internal static string BuildVariableName(Block block, BlockVariableLinkInstance variable)
        {
            return $"{block.Name}_{variable.ReferenceName}";
        }

        BlockVariableLinkInstance FindOrCreateVariableInstance(BlockVariableLinkInstance ownerInstance, BlockVariableLinkInstance variable, string referenceName, string displayName)
        {
            // If this field already exists on the owner (duplicate blocks) use the existing field, otherwise create a new one
            var matchingField = ownerInstance.FindField(referenceName);
            if (matchingField == null)
            {
                matchingField = BlockVariableLinkInstance.Construct(variable, referenceName, displayName, ownerInstance);
                ownerInstance.AddField(matchingField);
            }
            return matchingField;
        }

        internal void LinkBlock(BlockInstance blockInstance, NamespaceScopes scopes, BlockLinkInstance mergedBlockLinkInstance, BlockLinkInstance blockLinkInstance)
        {
            var block = blockInstance.Block;
            var mergedInputInstance = mergedBlockLinkInstance.InputInstance;
            var mergedOutputInstance = mergedBlockLinkInstance.OutputInstance;
            var blockInputInstance = blockLinkInstance.InputInstance;
            var blockOutputInstance = blockLinkInstance.OutputInstance;

            // Begin a scope for the current block
            scopes.PushScope(block.Name);

            var propertyVariableMap = new Dictionary<string, PropertyTuple>();
            // Keep track of all properties this block declared. These are used along with
            // the inputs to figure out how to match and whether a property is actually declared
            foreach (var prop in block.Properties)
            {
                var propName = prop.ReferenceName;
                if (!propertyVariableMap.ContainsKey(propName))
                    propertyVariableMap.Add(propName, new PropertyTuple { Variable = prop, DeclareAsProperty = true } );
            }

            // Try matching all input fields
            foreach (var input in blockInputInstance.Fields)
            {
                var inputName = input.ReferenceName;

                // Find if there's a backing property (always use the reference name)
                bool hasBackingProperty = propertyVariableMap.TryGetValue(input.ReferenceName, out var backingProperty);

                // Find if there's a matching field using the input name
                var matchingField = scopes.Find(input.Type, inputName, false);

                // By default, always create a new variable if there is no matching field
                bool createNewVariable = (matchingField == null);
                
                // However, if there's a backing property the rules change a bit.
                // An input with a backing property is always a property.
                if (hasBackingProperty)
                    createNewVariable = true;

                // If we need to create a new variable on the merged block to link to
                if (createNewVariable)
                {
                    // If the field has a backing property then use the original reference name, otherwise build a unique one
                    var referenceName = input.ReferenceName;
                    if(!hasBackingProperty)
                        referenceName = BuildVariableName(block, input);

                    // Make sure to propagate the original display name if it exists. If it doesn't use the reference name.
                    var displayName = input.DisplayName;
                    if (string.IsNullOrEmpty(input.DisplayName))
                        displayName = referenceName;

                    // Make sure a field exists on the merged input instance
                    matchingField = FindOrCreateVariableInstance(mergedInputInstance, input, referenceName, displayName);

                    // Propagate the alias up to the new variable. This alias is used to know know how the linking is supposed to work at subsequent merges.
                    // Note: Don't do this if there's a backing property as the override would provide no new information
                    if(!hasBackingProperty)
                        mergedInputInstance.AddAlias(matchingField.ReferenceName, inputName);

                    // If this was a property, also mark this as a property on the block. (Should this exist or be all on the front-end?)
                    if (matchingField.Attributes.IsProperty())
                        mergedBlockLinkInstance.AddProperty(matchingField);
                }

                // Add this as a resolved field match
                var matchLink = new ResolvedFieldMatch
                {
                    Source = matchingField,
                    Destination = input,
                };
                blockInputInstance.AddResolvedField(input.ReferenceName, matchLink);
                // Mark this resolution on the matching field's owner (the owner has a match for the field)
                // Is this actually needed? I don't think the source ever needs the match...
                matchingField.Owner.AddResolvedField(matchingField.ReferenceName, matchLink);
            }

            // Try to promote all properties.
            // Not all properties are promoted, depending on the input linking.
            // Note: Currently all properties are indeed promoted, but long term this may not be true.
            foreach(var prop in block.Properties)
            {
                propertyVariableMap.TryGetValue(prop.ReferenceName, out var trackedProperty);
                if (!trackedProperty.DeclareAsProperty)
                    continue;

                var existingProperty = mergedBlockLinkInstance.FindProperty(prop.ReferenceName);
                if(existingProperty == null)
                    mergedBlockLinkInstance.AddProperty(BlockVariableLinkInstance.Construct(prop, blockInputInstance, prop.Attributes));
            }

            foreach (var output in blockOutputInstance.Fields)
            {
                // Always create an output on the merged block since anyone could use the output later.
                var name = BuildVariableName(block, output);

                // Make sure a field exists on the merged output instance
                var availableOutput = FindOrCreateVariableInstance(mergedOutputInstance, output, name, name);

                // Link the new output to the block's output
                var match = new ResolvedFieldMatch
                {
                    Source = output,
                    Destination = availableOutput,
                };
                mergedOutputInstance.AddResolvedField(availableOutput.ReferenceName, match);

                // Always set in the block's explicit scope the original name.
                // This is so explicit scope access can work on the original name
                scopes.SetScope(block.Name, output, output.ReferenceName);

                // Make all override names available for matching and also propagate those overrides onto the new variable.
                // This includes the original name if there are no overrides.
                var outputName = output.ReferenceName;
                mergedOutputInstance.AddAlias(name, outputName);
                scopes.SetInCurrentScopeStack(output, outputName);
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
            foreach (var blockInst in context.blockInstances)
            {
                var blockLinkInstance = new BlockLinkInstance(Container, blockInst);
                FixInstanceName(ref blockLinkInstance.InputInstance.ReferenceName);
                FixInstanceName(ref blockLinkInstance.OutputInstance.ReferenceName);
                blockLinkInstances.Add(blockLinkInstance);
                LinkBlock(blockInst, scopes, mergedBlockLinkInstance, blockLinkInstance);
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

                fnBuilder.AddVariableDeclarationStatement(blockInputInstance.Type, blockInputInstance.ReferenceName);
                foreach (var input in blockInputInstance.Fields)
                {
                    var match = blockInputInstance.FindResolvedField(input.ReferenceName);
                    if (match != null)
                        DeclareMatchAssignment(match);
                }
                fnBuilder.AddCallStatementWithNewReturn(block.EntryPointFunction, blockOutputInstance.ReferenceName, blockInputInstance.ReferenceName);

                // Add a newline between blocks for readability
                fnBuilder.NewLine();
            }

            // Generate the merged block's output type and copy all of the output fields over
            fnBuilder.AddVariableDeclarationStatement(outputInstance.Type, outputInstance.ReferenceName);
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

        internal MergeResult Build(Context context, BlockLinkInstance mergedBlockLinkInstance, List<BlockLinkInstance> blockLinkInstances)
        {
            var mergedInputInstance = mergedBlockLinkInstance.InputInstance;
            var mergedOutputInstance = mergedBlockLinkInstance.OutputInstance;
            var blockBuilder = new Block.Builder(Container, context.Name);

            // Merge all types, functions, and descriptors. Make sure to do this first so that all dependent types/functions are already declared
            foreach (var blockLinkInstance in blockLinkInstances)
                blockBuilder.MergeTypesFunctionsDescriptors(blockLinkInstance.Block);

            // Create the input/output types
            var inputTypeBuilder = TypeUtilities.BuildStructBuilder(Container, context.InputTypeName, mergedInputInstance.ResolvedFields);
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
            foreach (var field in mergedOutputInstance.Fields)
                blockBuilder.AddOutput(field.Build(Container));
            foreach (var field in mergedBlockLinkInstance.Properties)
                blockBuilder.AddProperty(field.Build(Container));

            var mergedBlock = blockBuilder.Build();

            var result = new MergeResult();

            var blockInstBuilder = new BlockInstance.Builder(Container, mergedBlock);
            result.BlockInstance = blockInstBuilder.Build();

            // Create the input/output name overrides. These are used later to know how the sub-items were mapped originally
            foreach (var fieldOverride in mergedInputInstance.FieldOverrides)
                result.InputOverrides.Add(fieldOverride);
            foreach (var fieldOverride in mergedOutputInstance.FieldOverrides)
                result.OutputOverrides.Add(fieldOverride);

            return result;
        }

        internal MergeResult Merge(Context context)
        {
            List<BlockLinkInstance> blockLinkInstances;
            BlockLinkInstance mergedBlockLinkInstance;
            Merge(context, out mergedBlockLinkInstance, out blockLinkInstances);
            return Build(context, mergedBlockLinkInstance, blockLinkInstances);
        }
    }
}

