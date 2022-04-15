using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed partial class TerrainLitSubTarget
    {
        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            return false;
        }
    }
}
