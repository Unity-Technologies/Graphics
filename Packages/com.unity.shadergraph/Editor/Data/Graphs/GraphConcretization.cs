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
                // get all property nodes whose property doesn't exist?
                var propertyNodes = graph.GetNodes<PropertyNode>().Where(n => !graph.m_Properties.Any(p => p.value == n.property || n.property != null && p.value.objectId == n.property.objectId)).ToArray();
                foreach (var pNode in propertyNodes)
                    graph.ReplacePropertyNodeWithConcreteNodeNoValidate(pNode);
            }

            public static void ConcretizeGraph(GraphData graph)
            {
                graph.graphIsConcretizing = true;
                try
                {
                    ConcretizeProperties(graph);
                    GraphDataUtils.ApplyActionLeafFirst(graph, ConcretizeNode);
                }
                catch (System.Exception e)
                {
                    graph.graphIsConcretizing = false;
                    throw e;
                }
                graph.graphIsConcretizing = false;
            }
        }
    }
}
