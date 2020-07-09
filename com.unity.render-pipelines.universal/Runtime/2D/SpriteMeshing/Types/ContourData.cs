using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal class ContourData
    {
        public bool m_UseReverseWinding;
        public List<Vector2> m_Vertices = new List<Vector2>();
        public List<Contour> m_Contours = new List<Contour>();   // Which contours use this data
    }
}
