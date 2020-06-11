using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal class Shape
    {
        public List<Contour> m_Contours = new List<Contour>();
        public bool m_IsOpaque;
    }
}
