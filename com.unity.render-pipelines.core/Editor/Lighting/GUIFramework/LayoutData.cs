using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// Layout data
    /// </summary>
    public struct LayoutData
    {
        /// <summary>
        /// Index
        /// </summary>
        public int index;
        /// <summary>
        /// Distance
        /// </summary>
        public float distance;
        /// <summary>
        /// Position vector in world space
        /// </summary>
        public Vector3 position;
        /// <summary>
        /// Forward vector in world space
        /// </summary>
        public Vector3 forward;
        /// <summary>
        /// Up vector in world space
        /// </summary>
        public Vector3 up;
        /// <summary>
        /// Right vector in world space
        /// </summary>
        public Vector3 right;
        /// <summary>
        /// User data
        /// </summary>
        public object userData;

        /// <summary>
        /// Zero definition of LayoutData
        /// </summary>
        public static readonly LayoutData zero = new LayoutData() { index = 0, distance = float.MaxValue, position = Vector3.zero, forward = Vector3.forward, up = Vector3.up, right = Vector3.right };

        /// <summary>
        /// Nearest between currentData and newData
        /// </summary>
        /// <param name="currentData">Current Data</param>
        /// <param name="newData">New Data</param>
        /// <returns>Return the nearest layoutData between currentData and newData</returns>
        public static LayoutData Nearest(LayoutData currentData, LayoutData newData)
        {
            if (newData.distance <= currentData.distance)
                return newData;

            return currentData;
        }
    }
}
