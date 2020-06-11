using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.Universal
{
    [ExecuteInEditMode]
    public class MeshCompare : MonoBehaviour
    {
        public SpriteRenderer m_SpriteRenderer;
        public MeshRenderer m_MeshRenderer;
        public bool m_Compare;
        public int m_SpriteVertices;
        public int m_SpriteTriangles;
        public float m_SpriteArea;
        public int m_MeshVertices;
        public int m_MeshTriangles;
        public float m_MeshArea;

        public float m_TrianglesReducedTo;
        public float m_VerticesReducedTo;
        public float m_AreaReducedTo;

        SpriteRenderer m_PrevSpriteRenderer;
        MeshRenderer m_PrevMeshRenderer;


        //area += (int) prevPoint.x * (int) curPoint.y - (int) curPoint.x* (int) prevPoint.y;

        float ComputeArea(Vector3 pt0, Vector3 pt1, Vector3 pt2)
        {
            float area = 0;

            area = pt2.x * pt0.y - pt0.x * pt2.y;
            area += pt0.x * pt1.y - pt1.x * pt0.y;
            area += pt1.x * pt2.y - pt2.x * pt1.y;

            return 0.5f * Mathf.Abs(area);
        }



        void GetSpriteStats(out int spriteVertices, out int spriteTriangles, out float spriteArea)
        {
            Vector2[] vertices = m_SpriteRenderer.sprite.vertices;
            ushort[] triangles = m_SpriteRenderer.sprite.triangles;

            spriteVertices = vertices.Length;
            spriteTriangles = triangles.Length / 3;

            spriteArea = 0;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                spriteArea += ComputeArea(vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]]);
            }
        }

        void GetMeshStats(out int meshVertices, out int meshTriangles, out float meshArea)
        {
            MeshFilter mf = m_MeshRenderer.GetComponent<MeshFilter>();
            Vector3[] vertices = mf.sharedMesh.vertices;
            int[] triangles = mf.sharedMesh.triangles;

            meshVertices = vertices.Length;
            meshTriangles = triangles.Length / 3;

            meshArea = 0;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                meshArea += ComputeArea(vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]]);
            }
        }

        void Update()
        {
            if (m_SpriteRenderer != null && m_MeshRenderer != null && (m_SpriteRenderer != m_PrevSpriteRenderer || m_MeshRenderer != m_PrevMeshRenderer || m_Compare))
            {
                m_Compare = false;

                GetSpriteStats(out m_SpriteVertices, out m_SpriteTriangles, out m_SpriteArea);
                GetMeshStats(out m_MeshVertices, out m_MeshTriangles, out m_MeshArea);

                m_VerticesReducedTo = (float)m_MeshVertices / (float)m_SpriteVertices;
                m_TrianglesReducedTo = (float)m_MeshTriangles / (float)m_SpriteTriangles;
                m_AreaReducedTo = m_MeshArea / m_SpriteArea;
            }

            m_PrevSpriteRenderer = m_SpriteRenderer;
            m_PrevMeshRenderer = m_MeshRenderer;

        }
    }
}
