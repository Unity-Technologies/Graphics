using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    public class Light2DReactor : MonoBehaviour
    {
        // Debug stuff
        public MeshFilter debug_MeshFilter;

        delegate IRenderable2D InstancingFunction(GameObject go);
        static InstancingFunction[] m_InstancingFunctions;
        List<IRenderable2D> m_Renderables;
        List<Mesh> m_ShadowMeshes;

        void TryToInitializeInstancingFunctions()
        {
            if (m_InstancingFunctions == null)
                m_InstancingFunctions = new InstancingFunction[1] { SpriteRenderable.GetSpriteRenderable };
        }

        private void Awake()
        {
            TryToInitializeInstancingFunctions();

            m_ShadowMeshes = new List<Mesh>();
            m_Renderables = new List<IRenderable2D>();

            // Add our renderables
            for (int i = 0; i < m_InstancingFunctions.Length; i++)
            {
                IRenderable2D renderable = m_InstancingFunctions[i](gameObject);
                m_Renderables.Add(renderable);
            }
            
            // Create shadow meshes for our renderables
            for (int i = 0; i < m_Renderables.Count; i++)
            {
                Mesh mesh = new Mesh();
                m_ShadowMeshes.Add(mesh);
                Light2DReactorManager.CreateShadowMeshAsync(m_Renderables[i], mesh);
                debug_MeshFilter.sharedMesh = mesh;
            }
        }

        private void Update()
        {
            // Some debug stuff...
            //for(int meshIndex=0; meshIndex < m_ShadowMeshes.Count; meshIndex++)
            //{
            //    Mesh mesh = m_ShadowMeshes[meshIndex];

            //    int[] triangles = mesh.triangles;
            //    Vector3[] vertices = mesh.vertices;
            //    Vector4[] tangents = mesh.tangents;
            //    // Iterate through the triangles and if they have a non-zero tangent, simulate shadows
            //    for (int triIndex=0;triIndex<mesh.triangles.Length;triIndex+=3)
            //    {
            //        int tri0 = triangles[triIndex];
            //        int tri1 = triangles[triIndex + 1];
            //        int tri2 = triangles[triIndex + 2];
            //        Vector3 vert0 = vertices[tri0];
            //        Vector3 vert1 = vertices[tri1];
            //        Vector3 vert2 = vertices[tri2];
            //        Vector4 tan0 = tangents[tri0];
            //        Vector4 tan1 = tangents[tri1];
            //        Vector4 tan2 = tangents[tri2];

            //        Color color = Color.blue;
            //        Color tanColor = Color.red;

            //        Debug.DrawLine(vert0, vert1, color);
            //        Debug.DrawLine(vert1, vert2, color);
            //        Debug.DrawLine(vert2, vert0, color);

            //        Debug.DrawLine(vert0, vert0 + new Vector3(tan0.x, tan0.y, tan0.z), color);
            //        Debug.DrawLine(vert1, vert1 + new Vector3(tan1.x, tan1.y, tan1.z), color);
            //        Debug.DrawLine(vert2, vert2 + new Vector3(tan2.x, tan2.y, tan2.z), color);

            //        //if (tan0.sqrMagnitude > 0 || tan1.sqrMagnitude > 0 || tan2.sqrMagnitude > 0)
            //        //{
            //        //    Vector3 lightDir0 = Vector3.Normalize(LightTest.position - vert0);
            //        //    Vector3 lightDir1 = Vector3.Normalize(LightTest.position - vert1);
            //        //    Vector3 lightDir2 = Vector3.Normalize(LightTest.position - vert2);

            //        //    Color lightColor = Color.white;
            //        //    Color colorOriginal = Color.green;

            //        //    Debug.DrawLine(vert0, vert1, colorOriginal);
            //        //    Debug.DrawLine(vert1, vert2, colorOriginal);
            //        //    Debug.DrawLine(vert2, vert0, colorOriginal);


            //        //    float lightDistance = 5;
            //        //    float castsShadows = Mathf.Clamp01(Mathf.Ceil(Vector3.Dot(lightDir0, tan0)));
            //        //    if (castsShadows > 0)
            //        //        Debug.DrawLine(LightTest.position, vert0, lightColor);
            //        //    Debug.DrawLine(vert0, vert0 + new Vector3(tan0.x, tan0.y, tan0.z), tanColor);
            //        //    vert0 = vert0 + -lightDistance * lightDir0 * castsShadows;

            //        //    castsShadows = Mathf.Clamp01(Mathf.Ceil(Vector3.Dot(lightDir1, tan1)));
            //        //    if (castsShadows > 0)
            //        //        Debug.DrawLine(LightTest.position, vert1, lightColor);
            //        //    Debug.DrawLine(vert1, vert1 + new Vector3(tan1.x, tan1.y, tan1.z), tanColor);
            //        //    vert1 = vert1 + -lightDistance * lightDir1 * castsShadows;

            //        //    castsShadows = Mathf.Clamp01(Mathf.Ceil(Vector3.Dot(lightDir2, tan2)));
            //        //    if (castsShadows > 0)
            //        //        Debug.DrawLine(LightTest.position, vert2, lightColor);
            //        //    Debug.DrawLine(vert2, vert2 + new Vector3(tan2.x, tan2.y, tan2.z), tanColor);
            //        //    vert2 = vert2 + -lightDistance * lightDir2 * castsShadows;

            //        //    Debug.DrawLine(vert0, vert1, color);
            //        //    Debug.DrawLine(vert1, vert2, color);
            //        //    Debug.DrawLine(vert2, vert0, color);
            //        //}
            //    }
            //}
        }
    }
}
