using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityEngine.Experimental.Rendering.Universal
{
    public abstract class ShadowCaster2D : MonoBehaviour
    {
        public float m_Radius = 1;
        public int m_Sides = 6;
        public float m_Angle = 0;
        
        internal IShadowCasterGroup2D m_ShadowCasterGroup = null;

        int m_PreviousSides = 6;
        float m_PreviousAngle = 0;
        float m_PreviousRadius = 1;

        [SerializeField] Vector3[] m_ShapePath;
        [SerializeField] int m_ShapePathHash = 0;
        [SerializeField] int m_PreviousPathHash = 0;
        [SerializeField] Mesh m_Mesh;

        public float radius => m_Radius;
        internal Mesh mesh => m_Mesh; 
        internal Vector3[] shapePath => m_ShapePath;
        internal int shapePathHash { get { return m_ShapePathHash; } set { m_ShapePathHash = value; } }



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

            int extraTriangles = sides; // 1 new triangle for the hard shadow.
            int extraVertices = sides;  // 1 new vertex per side for the hard shadow.

            vertices = new Vector3[1 + sides + extraVertices];
            tangents = new Vector4[1 + sides + extraVertices];
            triangles = new int[3 * (sides + extraTriangles)];
            

            int centerIndex = sides + extraVertices;
            int lastVertexIndex = 0;

            vertices[centerIndex] = position;
            tangents[centerIndex] = Vector4.zero;
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

                int triangleIndex = 3 * i;
                triangles[triangleIndex] = vertexIndex;
                triangles[triangleIndex + 1] = lastVertexIndex;
                triangles[triangleIndex + 2] = centerIndex;

                // Create extra shadow triangle
                int extraVertexIndex = vertexIndex + sides;
                vertices[extraVertexIndex] = endPoint;
                tangents[extraVertexIndex] = new Vector4(curCross.x, curCross.y, 0, 0);


                int extraTriangleIndex = 3 * (i + sides);
                triangles[extraTriangleIndex] = vertexIndex;
                triangles[extraTriangleIndex + 1] = lastVertexIndex;
                triangles[extraTriangleIndex + 2] = extraVertexIndex;


                lastEndPoint = endPoint;
                lastVertexIndex = vertexIndex;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.tangents = tangents;


            m_Mesh = mesh;
        }

        void CreateShadowMesh(Vector3[] shapePath, ref Mesh mesh)
        {
            if(mesh != null)
            {

            }
        }

        private void Awake()
        {
            if (m_ShapePath == null || m_ShapePath.Length == 0)
                m_ShapePath = new Vector3[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(0.5f, 0.5f), new Vector3(-0.5f, 0.5f) };
        }

        private void OnEnable()
        {
            CreateShadowPolygon(Vector3.zero, m_Radius, m_Angle, m_Sides, ref m_ShadowMesh);

            if (m_Mesh == null)
            {
                m_Mesh = new Mesh();
                CreateShadowMesh(m_ShapePath, ref m_Mesh);
                m_PreviousPathHash = m_ShapePathHash;
            }

            LightUtility.AddToShadowCasterToGroup(this, out m_ShadowCasterGroup);
        }

        private void OnDisable()
        {
            LightUtility.RemoveShadowCasterFromGroup(this, m_ShadowCasterGroup);
        }

        private void Update()
        {
            bool rebuildMesh = false;
            rebuildMesh |= LightUtility.CheckForChange(m_Radius, ref m_PreviousRadius);
            rebuildMesh |= LightUtility.CheckForChange(m_Sides, ref m_PreviousSides);
            rebuildMesh |= LightUtility.CheckForChange(m_Angle, ref m_PreviousAngle);

            if (rebuildMesh)
                CreateShadowPolygon(Vector3.zero, m_Radius, m_Angle, m_Sides, ref m_Mesh);
        }
    }
}
