using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class TargetActiveBlockContext
    {
        public List<BlockFieldDescriptor> activeBlocks { get; private set; }

        // this is only used by the unknown target to pretend it uses all the current blocks
        // it REALLY shouldn't be available to the other target types
        public List<BlockFieldDescriptor> currentBlocks { get; private set; }

        public TargetActiveBlockContext(List<BlockFieldDescriptor> currentBlocks, PassDescriptor? pass)
        {
            activeBlocks = new List<BlockFieldDescriptor>();
            this.currentBlocks = currentBlocks;
        }

        public void AddBlock(BlockFieldDescriptor block, bool conditional = true)
        {
            if (conditional == true)
            {
                activeBlocks.Add(block);
            }
        }
    }
}
