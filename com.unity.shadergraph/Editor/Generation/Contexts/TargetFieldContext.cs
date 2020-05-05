using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class TargetFieldContext
    {
        public List<ConditionalField> conditionalFields { get; private set; }
        public PassDescriptor pass { get; private set; }
        public List<BlockFieldDescriptor> blocks { get; private set; }
        public List<BlockFieldDescriptor> connectedBlocks { get; private set; }
        public bool hasDotsProperties { get; private set; }

        public TargetFieldContext(PassDescriptor pass, List<BlockFieldDescriptor> blocks, List<BlockFieldDescriptor> connectedBlocks, bool hasDotsProperties)
        {
            conditionalFields = new List<ConditionalField>();
            this.pass = pass;
            this.blocks = blocks;
            this.connectedBlocks = connectedBlocks;
            this.hasDotsProperties = hasDotsProperties;
        }

        public void AddField(FieldDescriptor field, bool conditional = true)
        {
            conditionalFields.Add(new ConditionalField(field, conditional));
        }
    }
}
