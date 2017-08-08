using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine;


static class VisualElementExtensions
{
    public static Vector2 GlobalToBound(this VisualElement visualElement, Vector2 position)
    {
        return visualElement.transform.matrix.inverse.MultiplyPoint3x4(position);
    }
    public static Vector2 BoundToGlobal(this VisualElement visualElement, Vector2 position)
    {
        return visualElement.transform.matrix.MultiplyPoint3x4(position);
    }
}