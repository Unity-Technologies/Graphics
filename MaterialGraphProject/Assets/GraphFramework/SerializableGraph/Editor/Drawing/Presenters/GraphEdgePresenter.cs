using System;
using RMGUI.GraphView;

namespace UnityEditor.Graphing.Drawing
{
    [Serializable]
    public class GraphEdgePresenter : EdgePresenter
    {
        protected GraphEdgePresenter()
        {}

        public UnityEngine.Graphing.IEdge edge { get; private set; }

        public void Initialize(UnityEngine.Graphing.IEdge inEdge)
        {
            edge = inEdge;
        }
    }
}
