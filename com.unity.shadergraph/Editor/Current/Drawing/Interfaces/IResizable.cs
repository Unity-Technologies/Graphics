using UnityEditor.Experimental.GraphView;

namespace UnityEditor.ShaderGraph.Drawing.Interfaces
{
    interface ISGResizable : IResizable
    {
        // Depending on the return value, the ElementResizer either allows resizing past parent view edge (like in case of StickyNote) or clamps the size at the edges of parent view (like for GraphSubWindows)
        bool CanResizePastParentBounds();
    }
}
