using System.Linq;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    sealed partial class GraphData : ISerializationCallbackReceiver
    {
        public static class GraphConcretization
        {
            public static void ConcretizeNode(AbstractMaterialNode node)
            {
                node.Concretize();
            }
            public static void ConcretizeProperties(GraphData graph)
            {
                var propertyNodes = graph.GetNodes<PropertyNode>().Where(n => !graph.m_Properties.Any(p => p.guid == n.propertyGuid)).ToArray();
                foreach (var pNode in propertyNodes)
                    graph.ReplacePropertyNodeWithConcreteNodeNoValidate(pNode);
            }
            public static void ConcretizeGraph(GraphData graph)
            {
                ConcretizeProperties(graph);
                GraphDataUtils.ApplyActionLeafFirst(graph, ConcretizeNode);
            }
        }
    }
}
