using System;
using UnityEngine;

namespace UnityEditor.Rendering
{
    public static class GizmoUtility
    {
        public static Color GetHandleColor(Color baseColor)
        {
            baseColor.a = 1f;
            return baseColor;
        }

        public static Color GetWireframeColor(Color baseColor)
        {
            baseColor.a = .7f;
            return baseColor;
        }

        public static Color GetWireframeColorBehindObjects(Color baseColor)
        {
            baseColor.a = .2f;
            return baseColor;
        }
    }
}
