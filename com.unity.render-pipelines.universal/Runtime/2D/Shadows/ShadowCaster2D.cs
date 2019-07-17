using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityEngine.Experimental.Rendering.Universal
{
    [ExecuteAlways, DisallowMultipleComponent]
    [AddComponentMenu("Rendering/2D/Shadow Caster 2D (Experimental)")]
    public class ShadowCaster2D : MonoBehaviour
    {
        public float m_Radius = 1;
        public int m_Sides = 6;
        public float m_Angle = 0;
        Mesh m_Mesh;

        int m_PreviousSides = 6;
        float m_PreviousRadius = 1;
        public MeshFilter m_DebugMeshFilter;



        private static List<ShadowCaster2D> s_ShadowCasters = null;

        public static List<ShadowCaster2D> shadowCasters { get => s_ShadowCasters; }


        public float radius { get => m_Radius; }
        internal Mesh mesh { get => m_Mesh; }

        public enum CasterType
        {
            Capsule,
            Polygon
        }

        Mesh m_ShadowMesh;

        void CreateShadowPolygon(Vector3 position, float radius, float angle, int sides, ref Mesh mesh)
        {
            if (mesh == null)
                mesh = new Mesh();

            float angleOffset = Mathf.PI / 2.0f + Mathf.Deg2Rad * angle;
            if (sides < 3)
            {
                radius = 0.70710678118654752440084436210485f * radius;
                sides = 4;
            }

            if (sides == 4)
            {
                angleOffset = Mathf.PI / 4.0f + Mathf.Deg2Rad * angle;
            }

            Vector3[] vertices;
            Vector4[] tangents;
            int[] triangles;
            Color[] colors;

            int extraTriangles = sides; // 1 new triangle for the hard shadow.
            int extraVertices = sides;  // 1 new vertex per side for the hard shadow.

            //int extraTriangles = 2 * sides; // 1 new triangle for the hard shadow. 1 new triangle for the soft shadow.
            //int extraVertices = 4 * sides;  // 1 new vertex per side for the hard shadow. 3 new vertices for the soft shadow.

            vertices = new Vector3[1 + sides + extraVertices];
            tangents = new Vector4[1 + sides + extraVertices];
            triangles = new int[3 * (sides + extraTriangles)];

            colors = new Color[1 + sides + extraVertices];

            int centerIndex = sides + extraVertices;
            int lastVertexIndex = 0;

            vertices[centerIndex] = position;
            tangents[centerIndex] = Vector4.zero;
            colors[centerIndex] = Color.clear;
            float radiansPerSide = 2 * Mathf.PI / sides;
            Vector3 lastEndPoint = radius * new Vector3(Mathf.Cos(angleOffset), Mathf.Sin(angleOffset), 0);
            for (int i = 0; i < sides; i++)
            {
                float endAngle = (i + 1) * radiansPerSide;
                float nextEndAngle = (i + 2) * radiansPerSide;
                Vector3 endPoint = radius * new Vector3(Mathf.Cos(endAngle + angleOffset), Mathf.Sin(endAngle + angleOffset), 0); ;
                Vector3 nextEndPoint = radius * new Vector3(Mathf.Cos(nextEndAngle + angleOffset), Mathf.Sin(nextEndAngle + angleOffset), 0); ;

                Vector3 curCross = -Vector3.Normalize(Vector3.Cross((endPoint - lastEndPoint), Vector3.forward));
                Vector3 nextCross = -Vector3.Normalize(Vector3.Cross((nextEndPoint - endPoint), Vector3.forward));

                // Create triangle
                int vertexIndex;

                vertexIndex = (i + 1) % (sides);
                vertices[vertexIndex] = endPoint;
                tangents[vertexIndex] = new Vector4(nextCross.x, nextCross.y, 0, 0);
                tangents[vertexIndex].z = 0;
                tangents[vertexIndex].w = 0;
                colors[vertexIndex] = Color.clear;
                //Debug.DrawLine(endPoint, endPoint + -nextCross, Color.blue, 60);

                int triangleIndex = 3 * i;
                triangles[triangleIndex] = vertexIndex;
                triangles[triangleIndex + 1] = lastVertexIndex;
                triangles[triangleIndex + 2] = centerIndex;

                // Create extra shadow triangle
                int extraVertexIndex = vertexIndex + sides;
                vertices[extraVertexIndex] = endPoint;
                tangents[extraVertexIndex] = new Vector4(curCross.x, curCross.y, 0, 0);
                colors[extraVertexIndex] = Color.clear;
                //Debug.DrawLine(endPoint, endPoint + -curCross, Color.red, 60);

                int extraTriangleIndex = 3 * (i + sides);
                triangles[extraTriangleIndex] = vertexIndex;
                triangles[extraTriangleIndex + 1] = lastVertexIndex;
                triangles[extraTriangleIndex + 2] = extraVertexIndex;

                // Create extra soft shadow triangles
                //int softVertexIndex = 3 * vertexIndex + 2 * sides;
                //int softTriangleIndex = extraTriangleIndex + 3 * sides;

                //vertices[softVertexIndex] = endPoint;
                //vertices[softVertexIndex + 1] = endPoint;
                //vertices[softVertexIndex + 2] = endPoint;

                //tangents[softVertexIndex] = Vector4.zero;
                //tangents[softVertexIndex + 1] = new Vector4(curCross.x, curCross.y, 0, 0);
                //tangents[softVertexIndex + 2] = new Vector4(curCross.x, curCross.y, nextCross.x, nextCross.y);

                //colors[softVertexIndex] = Color.grey;
                //colors[softVertexIndex + 1] = Color.grey;
                //colors[softVertexIndex + 2] = Color.grey;

                //triangles[softTriangleIndex] = softVertexIndex;
                //triangles[softTriangleIndex + 1] = softVertexIndex + 1;
                //triangles[softTriangleIndex + 2] = softVertexIndex + 2;

                lastEndPoint = endPoint;
                lastVertexIndex = vertexIndex;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.tangents = tangents;
            mesh.colors = colors;

            m_Mesh = mesh;
        }

        internal static void Initialize()
        {
            if (s_ShadowCasters == null)
                s_ShadowCasters = new List<ShadowCaster2D>();
            else
                s_ShadowCasters.Clear();
        }

        private void OnEnable()
        {
            if (s_ShadowCasters == null)
                s_ShadowCasters = new List<ShadowCaster2D>();

            s_ShadowCasters.Add(this);

            CreateShadowPolygon(Vector3.zero, m_Radius, m_Angle, m_Sides, ref m_ShadowMesh);

            if(m_DebugMeshFilter)
                m_DebugMeshFilter.sharedMesh = m_ShadowMesh;
        }

        private void OnDisable()
        {
            s_ShadowCasters.Remove(this);
        }

        private void Update()
        {
            bool rebuildMesh = false;
            rebuildMesh |= LightUtility.CheckForChange(m_Radius, ref m_PreviousRadius);
            rebuildMesh |= LightUtility.CheckForChange(m_Sides, ref m_PreviousSides);

            if (rebuildMesh)
                CreateShadowPolygon(Vector3.zero, m_Radius, m_Angle, m_Sides, ref m_Mesh);
        }
    }
}   
