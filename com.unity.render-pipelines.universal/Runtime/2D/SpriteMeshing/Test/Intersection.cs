using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace UnityEngine.Experimental.Rendering.Universal
{
    [ExecuteInEditMode]
    public class Intersection : MonoBehaviour
    {
        public Vector2 m_Line0Pt0;
        public Vector2 m_Line0Pt1;
        public Vector2 m_Line1Pt0;
        public Vector2 m_Line1Pt1;

        public bool m_Intersects;
        public Vector2 m_IntersectionPt;

        void DrawCross(Vector2 point, float size, Color color)
        {
            Debug.DrawLine(point + Vector2.up * size, point + Vector2.down * size, color);
            Debug.DrawLine(point + Vector2.left * size, point + Vector2.right * size, color);
        }

        float2 v2tof2(Vector2 vector2)
        {
            return new float2(vector2.x, vector2.y);
        }

        // Update is called once per frame
        void Update()
        {
            float2 intersectionPt;
            m_Intersects = OutlineUtility.GetIntersection(v2tof2(m_Line0Pt0), v2tof2(m_Line0Pt1), v2tof2(m_Line1Pt0), v2tof2(m_Line1Pt1), out intersectionPt);
            m_IntersectionPt.x = intersectionPt.x;
            m_IntersectionPt.y = intersectionPt.y;

            Debug.DrawLine(m_Line0Pt0, m_Line0Pt1, Color.red);
            Debug.DrawLine(m_Line1Pt0, m_Line1Pt1, Color.blue);
            Debug.DrawLine(m_Line0Pt1, m_IntersectionPt, Color.green);
            Debug.DrawLine(m_Line1Pt1, m_IntersectionPt, Color.green);


            DrawCross(m_Line0Pt1, 0.125f, Color.red);
            DrawCross(m_Line1Pt1, 0.125f, Color.blue);
            DrawCross(m_IntersectionPt, 0.25f, Color.green);


        }
    }
}
