using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class TargetFieldContext
    {
        public List<ConditionalField> conditionalFields { get; private set; }
        public PassDescriptor pass { get; private set; }
        public List<BlockFieldDescriptor> blocks { get; private set; }

        public TargetFieldContext(PassDescriptor pass, List<BlockFieldDescriptor> blocks)
        {
            conditionalFields = new List<ConditionalField>();
            this.pass = pass;
            this.blocks = blocks;
        }

        public void AddField(FieldDescriptor field, bool conditional = true)
        {
            conditionalFields.Add(new ConditionalField(field, conditional));
        }
    }
}
