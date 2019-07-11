using System.Collections.Generic;
using UnityEditor.ShaderGraph;

namespace UnityEditor.CodeGeneration
{
    class GraphCode
    {
        public string functionName;
        public string inputStructName;
        public string outputStructName;
    }
    
    class GraphCodeGenerator
    {
        public MasterNode masterNode;
        public List<int> slotIds;
        public ShaderStageCapability stageCapability;
        
        public GraphCode Generate()
        {
            var graph = masterNode.owner;
            return new GraphCode();
        }
    }
}
