using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.Universal
{
    [ExecuteInEditMode]
    public class MeshCompare : MonoBehaviour
    {
        public SpriteRenderer m_SpriteRenderer0;
        public SpriteRenderer m_SpriteRenderer1;
        public bool m_Compare;
        public int m_SpriteVertices0;
        public int m_SpriteTriangles0;
        public float m_SpriteArea0;
        public int m_SpriteVertices1;
        public int m_SpriteTriangles1;
        public float m_SpriteArea1;

        public float m_TrianglesReducedTo;
        public float m_VerticesReducedTo;
        public float m_AreaReducedTo;

        SpriteRenderer m_PrevSpriteRenderer0;
        SpriteRenderer m_PrevSpriteRenderer1;


        //area += (int) prevPoint.x * (int) curPoint.y - (int) curPoint.x* (int) prevPoint.y;

        float ComputeArea(Vector3 pt0, Vector3 pt1, Vector3 pt2)
        {
            float area = 0;

            area = pt2.x * pt0.y - pt0.x * pt2.y;
            area += pt0.x * pt1.y - pt1.x * pt0.y;
            area += pt1.x * pt2.y - pt2.x * pt1.y;

            return 0.5f * Mathf.Abs(area);
        }



        void GetSpriteStats(SpriteRenderer spriteRenderer, out int spriteVertices, out int spriteTriangles, out float spriteArea)
        {
            Vector2[] vertices = spriteRenderer.sprite.vertices;
            ushort[] triangles = spriteRenderer.sprite.triangles;

            spriteVertices = vertices.Length;
            spriteTriangles = triangles.Length / 3;

            spriteArea = 0;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                spriteArea += ComputeArea(vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]]);
            }
        }

        void Update()
        {
            if (m_SpriteRenderer0 != null && m_SpriteRenderer1 != null && (m_SpriteRenderer0 != m_PrevSpriteRenderer0 || m_SpriteRenderer1 != m_PrevSpriteRenderer1 || m_Compare))
            {
                m_Compare = false;

                GetSpriteStats(m_SpriteRenderer0, out m_SpriteVertices0, out m_SpriteTriangles0, out m_SpriteArea0);
                GetSpriteStats(m_SpriteRenderer1, out m_SpriteVertices1, out m_SpriteTriangles1, out m_SpriteArea1);

                m_VerticesReducedTo = (float)m_SpriteVertices1 / (float)m_SpriteVertices0;
                m_TrianglesReducedTo = (float)m_SpriteTriangles1 / (float)m_SpriteTriangles0;
                m_AreaReducedTo = m_SpriteArea1 / m_SpriteArea0;
            }

            m_PrevSpriteRenderer0 = m_SpriteRenderer0;
            m_PrevSpriteRenderer1 = m_SpriteRenderer1;

        }
    }
}
