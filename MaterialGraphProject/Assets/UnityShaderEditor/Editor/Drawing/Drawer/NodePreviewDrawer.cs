using UnityEngine;
using UnityEngine.RMGUI;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class NodePreviewDrawer : Image
    {
        public NodePreviewDrawData data;

        public NodePreviewDrawer() {}

        public override void DoRepaint(IStylePainter args)
        {
            image = data.Render(new Vector2(200, 200));
            base.DoRepaint(args);
        }
    }
}
