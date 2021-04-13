using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.Path2D
{
    internal enum ShapeType
    {
        Polygon,
        Spline
    }

    internal interface IShape
    {
        ShapeType type { get; }
        bool isOpenEnded { get; }
        ControlPoint[] ToControlPoints();
    }
}
