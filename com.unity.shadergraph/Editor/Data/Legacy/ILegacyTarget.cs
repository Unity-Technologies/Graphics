using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Legacy
{
    internal interface ILegacyTarget
    {
        bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap);
    }
}
