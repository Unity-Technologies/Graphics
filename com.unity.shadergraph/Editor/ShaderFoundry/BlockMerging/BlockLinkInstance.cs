using System.Collections.Generic;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderFoundry
{
    /// An instance of a block for linking. Keeps track of the link connections for the input and output types.
    internal class BlockLinkInstance
    {
        ShaderContainer container;
        BlockDescriptor blockDescriptor;
        Block block;
        List<BlockVariableLinkInstance> properties = new List<BlockVariableLinkInstance>();

        internal ShaderContainer Container => container;
        internal BlockDescriptor BlockDescriptor => blockDescriptor;
        internal Block Block => block;
        internal BlockVariableLinkInstance InputInstance { get; set; } = new BlockVariableLinkInstance();
        internal BlockVariableLinkInstance OutputInstance { get; set; } = new BlockVariableLinkInstance();
        internal IEnumerable<BlockVariableLinkInstance> Properties => properties;

        internal BlockLinkInstance(ShaderContainer container)
        {
            this.container = container;
        }

        internal BlockLinkInstance(ShaderContainer container, BlockDescriptor blockDescriptor)
        {
            this.container = container;
            Build(blockDescriptor);
        }

        internal void Build(BlockDescriptor blockDescriptor)
        {
            this.blockDescriptor = blockDescriptor;
            this.block = blockDescriptor.Block;

            if(!block.EntryPointFunction.GetInOutTypes(out var inType, out var outType))
            {
                throw new System.Exception($"Block {block.Name} doesn't have a valid entry point function");
            }

            InputInstance = CreateVariableInstance(inType, block.Inputs);
            OutputInstance = CreateVariableInstance(outType, block.Outputs);
        }

        BlockVariableLinkInstance CreateVariableInstance(ShaderType type, IEnumerable<BlockVariable> variables)
        {
            string name = type.Name;
            var instance = BlockVariableLinkInstance.Construct(type, name.ToLower(), name, null, null);
            // Extract all of the variables into instances
            foreach (var variable in variables)
            {
                var subInstance = BlockVariableLinkInstance.Construct(variable, instance, variable.Attributes);
                instance.AddField(subInstance);
            }
            return instance;
        }

        internal void AddProperty(BlockVariableLinkInstance prop)
        {
            properties.Add(prop);
        }

        internal BlockVariableLinkInstance FindProperty(string referenceName)
        {
            return properties.Find((p) => (p.ReferenceName == referenceName));
        }
    }
}
