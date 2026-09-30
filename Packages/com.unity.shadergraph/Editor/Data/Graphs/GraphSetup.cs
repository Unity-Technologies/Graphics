using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    sealed partial class GraphData
    {
        public static class GraphSetup
        {
            public static void SetupNode(AbstractMaterialNode node)
            {
                node.Setup();
            }

            public static void SetupGraph(GraphData graph)
            {
                GraphDataUtils.ApplyActionLeafFirst(graph, SetupNode);
            }
        }
    }
}
