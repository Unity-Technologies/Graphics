using UnityEngine;
using UnityEditor;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal class PointRectSelector : RectSelector<Vector3>
    {
        protected override bool Select(Vector3 element)
        {
            return guiRect.Contains(HandleUtility.WorldToGUIPoint(element), true);
        }
    }
}
