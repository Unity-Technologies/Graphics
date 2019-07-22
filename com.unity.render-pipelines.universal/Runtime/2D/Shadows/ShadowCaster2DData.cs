using System;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.Universal
{
    [Serializable]
    public class ShadowCaster2DData
    {
        public float m_Radius = 1;
        public int m_Sides = 6;
        public float m_Angle = 0;
        public IShadowCasterGroup2D m_ShadowCasterGroup = null;

        Mesh m_Mesh;

        int m_PreviousSides = 6;
        float m_PreviousAngle = 0;
        float m_PreviousRadius = 1;
    }
}
