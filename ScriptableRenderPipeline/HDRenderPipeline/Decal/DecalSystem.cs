using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
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

        internal HashSet<DecalProjectorComponent> m_Decals = new HashSet<DecalProjectorComponent>();
        Mesh m_CubeMesh;

        public DecalSystem()
        {
            CreateCubeMesh();
        }

        void CreateCubeMesh()
        {
            m_CubeMesh = new Mesh();

            Vector3[] vertices = new Vector3[8];

            vertices[0] = new Vector3(-0.5f, -1.0f, -0.5f);
            vertices[1] = new Vector3( 0.5f, -1.0f, -0.5f);
            vertices[2] = new Vector3( 0.5f, 0.0f, -0.5f);
            vertices[3] = new Vector3(-0.5f, 0.0f, -0.5f);
            vertices[4] = new Vector3(-0.5f, -1.0f,  0.5f);
            vertices[5] = new Vector3( 0.5f, -1.0f,  0.5f);
            vertices[6] = new Vector3( 0.5f, 0.0f,  0.5f);
            vertices[7] = new Vector3(-0.5f, 0.0f,  0.5f);

            m_CubeMesh.vertices = vertices;

            int[] triangles = new int[36];

            triangles[0] = 0; triangles[1] = 2; triangles[2] = 1;
            triangles[3] = 0; triangles[4] = 3; triangles[5] = 2;
            triangles[6] = 1; triangles[7] = 6; triangles[8] = 5;
            triangles[9] = 1; triangles[10] = 2; triangles[11] = 6;
            triangles[12] = 5; triangles[13] = 7; triangles[14] = 4;
            triangles[15] = 5; triangles[16] = 6; triangles[17] = 7;
            triangles[18] = 4; triangles[19] = 3; triangles[20] = 0;
            triangles[21] = 4; triangles[22] = 7; triangles[23] = 3;
            triangles[24] = 3; triangles[25] = 6; triangles[26] = 2;
            triangles[27] = 3; triangles[28] = 7; triangles[29] = 6;
            triangles[30] = 4; triangles[31] = 1; triangles[32] = 5;
            triangles[33] = 4; triangles[34] = 0; triangles[35] = 1;

            m_CubeMesh.triangles = triangles;
        }

        public void AddDecal(DecalProjectorComponent d)
        {
            // If no decal material assign, don't add it
            if (d.m_Material == null)
                return;

            if (d.m_Material.GetTexture("_BaseColorMap") || d.m_Material.GetTexture("_NormalMap"))
            {
                RemoveDecal(d);
                m_Decals.Add(d);
            }
        }

        public void RemoveDecal(DecalProjectorComponent d)
        {
            m_Decals.Remove(d);
        }

        public void Render(ScriptableRenderContext renderContext, Vector3 cameraPos, CommandBuffer cmd)
        {
            if (m_CubeMesh == null)
                CreateCubeMesh();
            foreach (var decal in m_Decals)
            {
				decal.UpdatePropertyBlock(cameraPos);
                cmd.DrawMesh(m_CubeMesh, decal.transform.localToWorldMatrix, decal.m_Material, 0, 0, decal.GetPropertyBlock());
            }
        }
    }
}
