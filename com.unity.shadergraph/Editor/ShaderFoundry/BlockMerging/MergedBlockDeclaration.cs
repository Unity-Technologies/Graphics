using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderFoundry
{
    internal class MergedBlockDeclaration
    {
        ShaderContainer container;
        internal ShaderContainer Container => container;
        HashSet<string> UsedInstanceNames = new HashSet<string>();

        internal class Context
        {
            internal string BlockName;
            internal string EntryPointFunctionName;
            internal string InputTypeName;
            internal string OutputTypeName;
            internal BlockLinkInstance MergedBlockLinkInstance;
            internal IEnumerable<BlockLinkInstance> BlockLinkInstances = Enumerable.Empty<BlockLinkInstance>();
        }

        internal MergedBlockDeclaration(ShaderContainer container)
        {
            this.container = container;
        }

        void FixInstanceName(ref string name, string defaultName)
        {
            // If there's no name already, use the default name
            if (name == null)
                name = defaultName;

            // Add a number until we find a unique name
            int count = 0;
            string testName = name;
            while (UsedInstanceNames.Contains(testName))
            {
                ++count;
                testName = name + count;
            }
            UsedInstanceNames.Add(testName);
            name = testName;
        }

        internal ShaderFunction GenerateEntryPointFunction(Block.Builder blockBuilder, string fnName, VariableLinkInstance inputInstance, VariableLinkInstance outputInstance, IEnumerable<BlockLinkInstance> blockLinkInstances)
        {
            var fnBuilder = new ShaderFunction.Builder(blockBuilder, fnName, outputInstance.Type);
            fnBuilder.AddInput(inputInstance.Type, inputInstance.Name);
            fnBuilder.Indent();

            void DeclareMatchAssignment(VariableLinkInstance field)
            {
                var source = field.Source;
                if(source != null)
                    fnBuilder.AddLine($"{field.Parent.Name}.{field.Name} = {source.Parent.Name}.{source.Name};");
            }

            // For each block, construct the input's type, copy all of the input fields, and then call the entry point function
            foreach (var blockLinkInstance in blockLinkInstances)
            {
                var block = blockLinkInstance.Block;
                var blockInputInstance = blockLinkInstance.InputInstance;
                var blockOutputInstance = blockLinkInstance.OutputInstance;
                FixInstanceName(ref blockInputInstance.Name, blockInputInstance.Type.Name.ToLower());
                FixInstanceName(ref blockOutputInstance.Name, blockOutputInstance.Type.Name.ToLower());

                fnBuilder.AddVariableDeclarationStatement(blockBuilder, blockInputInstance.Type, blockInputInstance.Name);
                foreach (var input in blockInputInstance.Fields)
                {
                    if(input.Source != null)
                        DeclareMatchAssignment(input);
                }
                fnBuilder.AddCallStatementWithNewReturn(block.EntryPointFunction, blockOutputInstance.Name, blockInputInstance.Name);

                // Add a newline between blocks for readability
                fnBuilder.NewLine();
            }

            // Generate the merged block's output type and copy all of the output fields over
            fnBuilder.AddVariableDeclarationStatement(blockBuilder, outputInstance.Type, outputInstance.Name);
            foreach (var output in outputInstance.Fields)
            {
                if (output.Source != null)
                    DeclareMatchAssignment(output);
            }
            fnBuilder.AddLine($"return {outputInstance.Name};");
            fnBuilder.Deindent();

            return fnBuilder.Build();
        }

        ShaderType BuildType(Block.Builder blockBuilder, string name, VariableLinkInstance owner)
        {
            var builder = new ShaderType.StructBuilder(blockBuilder, name);
            foreach(var field in owner.Fields)
            {
                // Skip unused fields
                if (!field.IsUsed)
                    continue;

                // Add all of the original field attributes
                var fieldBuilder = new StructField.Builder(Container, field.Name, field.Type);
                foreach (var attribute in field.Attributes)
                    fieldBuilder.AddAttribute(attribute);
                // Also append all aliases as new attributes
                foreach(var alias in field.Aliases)
                {
                    var attBuilder = new ShaderAttribute.Builder(Container, CommonShaderAttributes.Alias);
                    attBuilder.Param(alias);
                    fieldBuilder.AddAttribute(attBuilder.Build());
                }
                builder.AddField(fieldBuilder.Build());
            }
            return builder.Build();
        }

        internal Block Build(Context context)
        {
            UsedInstanceNames.Clear();

            var mergedBlockLinkInstance = context.MergedBlockLinkInstance;
            var blockLinkInstances = context.BlockLinkInstances;
            var mergedInputInstance = mergedBlockLinkInstance.InputInstance;
            var mergedOutputInstance = mergedBlockLinkInstance.OutputInstance;
            var blockBuilder = new Block.Builder(Container, context.BlockName);

            // Merge all types, functions, and descriptors. Make sure to do this first so that all dependent types/functions are already declared
            foreach (var blockLinkInstance in blockLinkInstances)
                blockBuilder.MergeTypesFunctionsDescriptors(blockLinkInstance.Block);

            // Create the input/output types
            var inputType = BuildType(blockBuilder, context.InputTypeName, mergedInputInstance);
            mergedInputInstance.Type = inputType;
            FixInstanceName(ref mergedInputInstance.Name, mergedInputInstance.Type.Name.ToLower());
            blockBuilder.AddType(mergedInputInstance.Type);

            var outputType = BuildType(blockBuilder, context.OutputTypeName, mergedOutputInstance);
            mergedOutputInstance.Type = outputType;
            FixInstanceName(ref mergedOutputInstance.Name, mergedOutputInstance.Type.Name.ToLower());
            blockBuilder.AddType(mergedOutputInstance.Type);

            var entryPointFunction = GenerateEntryPointFunction(blockBuilder, context.EntryPointFunctionName, mergedInputInstance, mergedOutputInstance, blockLinkInstances);
            blockBuilder.SetEntryPointFunction(entryPointFunction);

            return blockBuilder.Build();
        }
    }
}

