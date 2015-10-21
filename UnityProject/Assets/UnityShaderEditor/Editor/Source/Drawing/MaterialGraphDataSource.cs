using System.Collections.Generic;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public class MaterialGraphDataSource : ICanvasDataSource
    {
        public MaterialGraph graph { get; set; }

        public CanvasElement[] FetchElements()
        {
            var elements = new List<CanvasElement>();
            Debug.Log("trying to convert");
            var pixelGraph = graph.currentGraph;
            foreach (var node in pixelGraph.nodes)
            {
                var bmn = node as BaseMaterialNode;
                elements.Add(new DrawableMaterialNode(bmn, 200.0f, typeof(Vector4), this));
            }
            
            Debug.LogFormat("REturning {0} nodes", elements.Count);
            return elements.ToArray();
        }

        public void DeleteElement(CanvasElement e)
        {
            //m_Elements.Remove(e);
        }

        public void Connect(NodeAnchor a, NodeAnchor b)
        {
            //m_Elements.Add(new Edge<NodeAnchor>(this, a, b));
        }
    }
}
