using UnityEngine;

namespace UnityEditor.Rendering.Universal.Path2D.GUIFramework
{
    internal struct LayoutData
    {
        public int index;
        public float distance;
        public Vector3 position;
        public Vector3 forward;
        public Vector3 up;
        public Vector3 right;
        public object userData;

        public static readonly LayoutData zero = new LayoutData() { index = 0, distance = float.MaxValue, position = Vector3.zero, forward = Vector3.forward, up = Vector3.up, right = Vector3.right };

        public static LayoutData Nearest(LayoutData currentData, LayoutData newData)
        {
            if (newData.distance <= currentData.distance)
                return newData;

            return currentData;
        }
    }
}
