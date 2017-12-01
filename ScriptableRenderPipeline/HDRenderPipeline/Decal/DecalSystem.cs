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

        internal HashSet<DecalComponent> m_Decals = new HashSet<DecalComponent>();
        Mesh m_CubeMesh;

        private static readonly int m_WorldToDecal = Shader.PropertyToID("_WorldToDecal");
        private static readonly int m_DecalToWorldR = Shader.PropertyToID("_DecalToWorldR");

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

        public void AddDecal(DecalComponent d)
        {
            if (d.m_Material.GetTexture("_BaseColorMap") || d.m_Material.GetTexture("_NormalMap"))
            {
                RemoveDecal(d);
                m_Decals.Add(d);                
            }
        }

        public void RemoveDecal(DecalComponent d)
        {
            m_Decals.Remove(d);
        }

        public void Render(ScriptableRenderContext renderContext, Camera camera, CommandBuffer cmd)
        {
            if (m_CubeMesh == null)
                CreateCubeMesh();
			Matrix4x4 CRWStoAWS = new Matrix4x4();
			if (ShaderConfig.s_CameraRelativeRendering == 1)
			{
				Vector4 worldSpaceCameraPos = Shader.GetGlobalVector(HDShaderIDs._WorldSpaceCameraPos);
				CRWStoAWS = Matrix4x4.Translate(worldSpaceCameraPos);
			}
			else
			{
				CRWStoAWS = Matrix4x4.identity;
			}
            foreach (var decal in m_Decals)
            {              
                Matrix4x4 final = decal.transform.localToWorldMatrix;
                Matrix4x4 decalToWorldR = Matrix4x4.Rotate(decal.transform.localRotation);
                Matrix4x4 worldToDecal = Matrix4x4.Translate(new Vector3(0.5f, 0.0f, 0.5f)) * Matrix4x4.Scale(new Vector3(1.0f, -1.0f, 1.0f)) * final.inverse;           
                cmd.SetGlobalMatrix(m_WorldToDecal, worldToDecal * CRWStoAWS);
                cmd.SetGlobalMatrix(m_DecalToWorldR, decalToWorldR);
                //DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int submeshIndex, int shaderPass);
                cmd.DrawMesh(m_CubeMesh, final, decal.m_Material, 0, 0);
            }
        }
    }
}
