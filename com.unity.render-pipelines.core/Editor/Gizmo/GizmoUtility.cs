using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Set of utilities for gizmos
    /// </summary>
    public static class GizmoUtility
    {
        /// <summary>Modifies the given <see cref="Color"/> for handles</summary>
        /// <param name="baseColor">The color to be modified</param>
        /// <returns>a <see cref="Color"/></returns>
        public static Color GetHandleColor(Color baseColor)
        {
            baseColor.a = 1f;
            return baseColor;
        }

        /// <summary>Modifies the given <see cref="Color"/> for wire frames</summary>
        /// <param name="baseColor">The color to be modified</param>
        /// <returns>a <see cref="Color"/></returns>
        public static Color GetWireframeColor(Color baseColor)
        {
            baseColor.a = .7f;
            return baseColor;
        }

        /// <summary>Modifies the given <see cref="Color"/> for wire frames behind objects</summary>
        /// <param name="baseColor">The color to be modified</param>
        /// <returns>a <see cref="Color"/></returns>
        public static Color GetWireframeColorBehindObjects(Color baseColor)
        {
            baseColor.a = .2f;
            return baseColor;
        }
    }
}
