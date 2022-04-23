using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public static class RectTestUtils
    {
        public static Rect RectAroundElements(params VisualElement[] elements)
        {
            Rect rect = elements[0].worldBound;
            for (int i = 1; i < elements.Length; i++)
            {
                rect = RectUtils.Encompass(rect, elements[i].worldBound);
            }
            rect = RectUtils.Inflate(rect, 1, 1, 1, 1);
            return rect;
        }
    }
}
