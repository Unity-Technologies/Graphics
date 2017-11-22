using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline.Decal
{
    public class DecalSystem
    {
        static DecalSystem m_Instance;
        static public DecalSystem instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new DecalSystem();
                return m_Instance;
            }
        }

        internal HashSet<Decal> m_DecalsDiffuse = new HashSet<Decal>();
        internal HashSet<Decal> m_DecalsNormals = new HashSet<Decal>();
        internal HashSet<Decal> m_DecalsBoth = new HashSet<Decal>();
        Mesh m_CubeMesh;

        private readonly static int m_WorldToDecal = Shader.PropertyToID("_WorldToDecal");

        public DecalSystem()
        {
            CreateCubeMesh();       
        }

        void CreateCubeMesh()
        {
            m_CubeMesh = new Mesh();

            Vector3[] vertices = new Vector3[8];

            vertices[0] = new Vector3(-0.5f, -0.5f, -0.5f);
            vertices[1] = new Vector3(0.5f, -0.5f, -0.5f);
            vertices[2] = new Vector3(0.5f, 0.5f, -0.5f);
            vertices[3] = new Vector3(-0.5f, 0.5f, -0.5f);
            vertices[4] = new Vector3(-0.5f, -0.5f, 0.5f);
            vertices[5] = new Vector3(0.5f, -0.5f, 0.5f);
            vertices[6] = new Vector3(0.5f, 0.5f, 0.5f);
            vertices[7] = new Vector3(-0.5f, 0.5f, 0.5f);

            m_CubeMesh.vertices = vertices;

            int[] triangles = new int[36];

            triangles[0] = 0; triangles[1] = 1; triangles[2] = 2;
            triangles[3] = 0; triangles[4] = 2; triangles[5] = 3;
            triangles[6] = 1; triangles[7] = 5; triangles[8] = 6;
            triangles[9] = 1; triangles[10] = 6; triangles[11] = 2;
            triangles[12] = 5; triangles[13] = 4; triangles[14] = 7;
            triangles[15] = 5; triangles[16] = 7; triangles[17] = 6;
            triangles[18] = 4; triangles[19] = 0; triangles[20] = 3;
            triangles[21] = 4; triangles[22] = 3; triangles[23] = 7;
            triangles[24] = 3; triangles[25] = 2; triangles[26] = 6;
            triangles[27] = 3; triangles[28] = 6; triangles[29] = 7;
            triangles[30] = 4; triangles[31] = 5; triangles[32] = 1;
            triangles[33] = 4; triangles[34] = 1; triangles[35] = 0;

            m_CubeMesh.triangles = triangles;
        }

        public void AddDecal(Decal d)
        {
            RemoveDecal(d);
            if (d.m_Kind == Decal.Kind.DiffuseOnly)
                m_DecalsDiffuse.Add(d);
            if (d.m_Kind == Decal.Kind.NormalsOnly)
                m_DecalsNormals.Add(d);
            if (d.m_Kind == Decal.Kind.Both)
                m_DecalsBoth.Add(d);
        }
        public void RemoveDecal(Decal d)
        {
            m_DecalsDiffuse.Remove(d);
            m_DecalsNormals.Remove(d);
            m_DecalsBoth.Remove(d);
        }

        public void Render(ScriptableRenderContext renderContext, Camera camera, CommandBuffer cmd)
        {
            if (m_CubeMesh == null)
                CreateCubeMesh();
            foreach (var decal in m_DecalsDiffuse)
            {
                Matrix4x4 offset = Matrix4x4.Translate(new Vector3(0.0f, -0.5f, 0.0f));
                Matrix4x4 final = decal.transform.localToWorldMatrix * offset;                
                Vector4 positionWS = new Vector4(0,5,0,1);
                Vector4 positionDS = final.inverse * positionWS;
                cmd.SetGlobalMatrix(m_WorldToDecal, final.inverse);
                //DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int submeshIndex, int shaderPass);
                cmd.DrawMesh(m_CubeMesh, final, decal.m_Material, 0, 0);
            }
        }
    }
}
