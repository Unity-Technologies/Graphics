using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    sealed partial class GraphData : ISerializationCallbackReceiver
    {
        public static class GraphValidation
        {
            public static void ValidateNode(AbstractMaterialNode node)
            {
                node.ValidateNode();
            }

            public static void ValidateGraph(GraphData graph)
            {
                GraphDataUtils.ApplyActionLeafFirst(graph, ValidateNode);
            }
        }
    }
}
