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
        internal BlockTypeLinkInstance InputInstance { get; set; } = new BlockTypeLinkInstance();
        internal BlockTypeLinkInstance OutputInstance { get; set; } = new BlockTypeLinkInstance();
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

            BuildNameOverrides(blockDescriptor);
            if(block.EntryPointFunction.GetInOutTypes(out var inType, out var outType))
            {
                CreateTypeLinkInstance(inType.Name, block.Inputs, InputInstance);
                CreateTypeLinkInstance(outType.Name, block.Outputs, OutputInstance);
            }
        }

        void CreateTypeLinkInstance(string name, IEnumerable<BlockVariable> variables, BlockTypeLinkInstance typeInstance)
        {
            var newType = TypeUtilities.BuildType(Container, name, variables);
            typeInstance.Instance = BlockVariableLinkInstance.Construct(newType, name.ToLower(), name, null, typeInstance);
            // Extract all of the variables into instances
            foreach (var variable in variables)
            {
                var instance = BlockVariableLinkInstance.Construct(variable, typeInstance.Instance, typeInstance, variable.Attributes);
                typeInstance.AddField(instance);
            }
        }

        internal void BuildNameOverrides(BlockDescriptor blockDescriptor)
        {
            if (!blockDescriptor.IsValid)
                return;
            foreach (var inputOverride in blockDescriptor.InputOverrides)
                InputInstance.AddOverride(inputOverride.DestinationName, inputOverride.SourceNamespace, inputOverride.SourceName, inputOverride.SourceSwizzle);
            foreach (var outputOverride in blockDescriptor.OutputOverrides)
                OutputInstance.AddOverride(outputOverride.SourceName, outputOverride.DestinationNamespace, outputOverride.DestinationName, outputOverride.DestinationSwizzle);
        }

        internal void AddProperty(BlockVariableLinkInstance prop)
        {
            properties.Add(prop);
        }
    }
}
