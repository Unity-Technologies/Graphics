using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    /// An instance of a block for linking. Keeps track of the link connections for the input and output types.
    internal class BlockLinkInstance
    {
        ShaderContainer container;
        BlockSequenceElement blockSequenceElement;
        Block block;

        internal ShaderContainer Container => container;
        internal BlockSequenceElement BlockSequenceElement => blockSequenceElement;
        internal Block Block => block;
        internal VariableLinkInstance InputInstance { get; set; } = new VariableLinkInstance();
        internal VariableLinkInstance OutputInstance { get; set; } = new VariableLinkInstance();
        internal bool IsLegacy = true;

        internal BlockLinkInstance(ShaderContainer container)
        {
            this.container = container;
            this.InputInstance.Container = container;
            this.OutputInstance.Container = container;
        }

        internal BlockLinkInstance(ShaderContainer container, BlockSequenceElement blockSequenceElement)
        {
            this.container = container;
            Build(blockSequenceElement);
        }

        internal void Build(BlockSequenceElement blockSequenceElement)
        {
            this.blockSequenceElement = blockSequenceElement;
            this.block = blockSequenceElement.Block;

            if (!block.EntryPointFunction.GetInOutTypes(out var inType, out var outType))
            {
                throw new System.Exception($"Block {block.Name} doesn't have a valid entry point function");
            }

            InputInstance = CreateVariableInstance(inType, block.Inputs);
            OutputInstance = CreateVariableInstance(outType, block.Outputs);
            if (outType.IsVoid)
            {
                InputInstance.Name = $"{block.Name}Instance";
                OutputInstance.Name = InputInstance.Name;
                IsLegacy = false;
            }
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
