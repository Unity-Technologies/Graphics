using UnityEditor.Experimental.Graph;
using UnityEditor.Experimental;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    internal class DrawableEdge<T> : Edge<T> where T : CanvasElement, IConnect
    {
        public readonly IEdge m_Edge;

        public DrawableEdge(IEdge edge, ICanvasDataSource data, T left, T right) : base(data, left, right)
        {
            m_Edge = edge;
        }
    }
}
