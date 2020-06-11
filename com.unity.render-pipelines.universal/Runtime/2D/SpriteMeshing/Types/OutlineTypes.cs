using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.Universal
{
    public class OutlineTypes
    {
        public enum AlphaType
        {
            NA,
            Transparent,
            Translucent,
            Opaque
        }

        public enum DebugOutputType
        {
            NA,
            Pixels,
            Errors,
            SpeckleHoleMap,
        }


        public delegate void SaveHandler(List<Vector2> points, int shapeIndex, int contourIndex, bool isOuterEdge);
    }
}
