using UnityEngine;

namespace UnityEditor.GUIFramework
{
    /// <summary>
    /// Represents the layout of a GUI element in a custom editor.
    /// </summary>
    public struct LayoutData
    {
        /// <summary>
        /// The layout's index.
        /// </summary>
        public int index;
        /// <summary>
        /// The distance from the layout to the camera.
        /// </summary>
        public float distance;
        /// <summary>
        /// The layout's world-space position.
        /// </summary>
        public Vector3 position;
        /// <summary>
        /// The layout's world-space forward vector.
        /// </summary>
        public Vector3 forward;
        /// <summary>
        /// The layout's world-space up vector.
        /// </summary>
        public Vector3 up;
        /// <summary>
        /// The layout's world-space right vector.
        /// </summary>
        public Vector3 right;
        /// <summary>
        /// The layout's user data.
        /// </summary>
        public object userData;

        /// <summary>
        /// Zero definition of LayoutData.
        /// </summary>
        public static readonly LayoutData zero = new LayoutData() { index = 0, distance = float.MaxValue, position = Vector3.zero, forward = Vector3.forward, up = Vector3.up, right = Vector3.right };

        /// <summary>
        /// Gets the layout that is closest to the camera,
        /// </summary>
        /// <param name="currentData">The current layout.</param>
        /// <param name="newData">The new layout to compare with.</param>
        /// <returns>Returns the closest layout to the camera. If `currentData` is closest to the camera, returns `currentData`. Otherwise, if `newData` is closest to the camera, returns `newData`.</returns>
        public static LayoutData Nearest(LayoutData currentData, LayoutData newData)
        {
            if (newData.distance <= currentData.distance)
                return newData;

            return currentData;
        }
    }
}
