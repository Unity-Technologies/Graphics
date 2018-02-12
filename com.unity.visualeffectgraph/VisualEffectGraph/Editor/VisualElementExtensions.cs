using UnityEngine;
using UnityEngine.Experimental.UIElements;


static class VisualElementExtensions
{
    public static Vector2 GlobalToBound(this VisualElement visualElement, Vector2 position)
    {
        return visualElement.worldTransform.inverse.MultiplyPoint3x4(position);
    }

    public static Vector2 BoundToGlobal(this VisualElement visualElement, Vector2 position)
    {
        /*do
        {*/
        position = visualElement.worldTransform.MultiplyPoint3x4(position);
        /*
        visualElement = visualElement.parent;
    }
    while (visualElement != null;)*/

        return position;
    }
}
