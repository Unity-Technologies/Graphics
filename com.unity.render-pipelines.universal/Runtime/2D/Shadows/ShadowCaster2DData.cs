using System;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.Universal
{
    [Serializable]
    public class ShadowCaster2DData
    {
        [SerializeField] Vector3[] m_ShapePath;
        int m_PreviousShapePathHash = -1;

        public IShadowCasterGroup2D m_ShadowCasterGroup = null;

        Mesh m_Mesh;

        int m_PreviousSides = 6;
        float m_PreviousAngle = 0;
        float m_PreviousRadius = 1;
    }
}
