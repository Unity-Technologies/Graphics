using System;
using RMGUI.GraphView;

namespace UnityEditor.Graphing.Drawing
{
    [Serializable]
    public class EdgeDrawData : EdgePresenter
    {
        protected EdgeDrawData()
        {}

        public UnityEngine.Graphing.IEdge edge { get; private set; }

        public void Initialize(UnityEngine.Graphing.IEdge inEdge)
        {
            edge = inEdge;
        }
    }
}
