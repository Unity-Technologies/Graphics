using UnityEngine;

namespace UnityEditor.Experimental.Graph
{
    public interface IConnect
    {
        Direction GetDirection();
        Orientation GetOrientation();
        void Highlight(bool highlighted);
        void RenderOverlay(Canvas2D canvas);
        object Source();
        Vector3 ConnectPosition();
        void OnConnect(IConnect other);
    };
}
