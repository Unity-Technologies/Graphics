using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class TargetActiveBlockContext
    {
        public List<BlockFieldDescriptor> blocks { get; private set; }

        public TargetActiveBlockContext()
        {
            blocks = new List<BlockFieldDescriptor>();
        }

        public void AddBlock(BlockFieldDescriptor block, bool conditional = true)
        {
            if(conditional == true)
            {
                blocks.Add(block);
            }
        }
    }
}
