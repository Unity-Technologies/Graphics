using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class NodePreviewDrawer : Image
    {
        public NodePreviewPresenter data;

        public override void DoRepaint()
        {
            Debug.Log("DoRepaint");
            image = data.Render(new Vector2(200, 200));
            base.DoRepaint();
        }
    }
}
