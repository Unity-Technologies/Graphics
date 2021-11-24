using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    /// An instance of a block for linking. Keeps track of the link connections for the input and output types.
    internal class BlockLinkInstance
    {
        ShaderContainer container;
        BlockInstance blockInstance;
        Block block;

        internal ShaderContainer Container => container;
        internal BlockInstance BlockInstance => blockInstance;
        internal Block Block => block;
        internal VariableLinkInstance InputInstance { get; set; } = new VariableLinkInstance();
        internal VariableLinkInstance OutputInstance { get; set; } = new VariableLinkInstance();

        internal BlockLinkInstance(ShaderContainer container)
        {
            this.container = container;
            this.InputInstance.Container = container;
            this.OutputInstance.Container = container;
        }

        internal BlockLinkInstance(ShaderContainer container, BlockInstance blockInstance)
        {
            this.container = container;
            Build(blockInstance);
        }

        internal void Build(BlockInstance blockInstance)
        {
            this.blockInstance = blockInstance;
            this.block = blockInstance.Block;

            if(!block.EntryPointFunction.GetInOutTypes(out var inType, out var outType))
            {
                throw new System.Exception($"Block {block.Name} doesn't have a valid entry point function");
            }

            InputInstance = CreateVariableInstance(inType, block.Inputs);
            OutputInstance = CreateVariableInstance(outType, block.Outputs);
        }

        VariableLinkInstance CreateVariableInstance(ShaderType type, IEnumerable<BlockVariable> variables)
        {
            string name = type.Name;
            var instance = new VariableLinkInstance { Type = type, Name = name.ToLower(), Container = type.Container };
            // Extract all of the variables into instances
            foreach (var variable in variables)
                instance.CreateSubField(variable.Type, variable.Name, variable.Attributes);
            return instance;
        }
    }
}
