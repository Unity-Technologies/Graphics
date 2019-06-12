using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Collections;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    public class Light2DReactorManager : MonoBehaviour
    {
        internal struct MeshInfo
        {
            public Mesh mesh;
            public NativeSlice<Vector3> vertices; // should these be native?
            public NativeArray<ushort>  triangles;
            public NativeSlice<Vector3> normals;
            public int id;
        }

        static GameObject m_LightReactorManagerGO;
        static List<MeshInfo> m_MeshesToProcess;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterSceneLoaded()
        {
            // Create an invisible game object which runs a coroutine if one doesn't exist
            if (m_LightReactorManagerGO == null)
            {
                m_LightReactorManagerGO = new GameObject("Light Reactor Manager");
                m_LightReactorManagerGO.AddComponent<Light2DReactorManager>();
            }
        }

        public static void CreateShadowMeshAsync(IRenderable2D renderable, Mesh mesh)
        {
            if (renderable != null && mesh != null)
            {
                MeshInfo meshInfo = new MeshInfo();
                meshInfo.mesh = mesh;
                meshInfo.triangles = renderable.GetTriangles();
                meshInfo.vertices = renderable.GetVertices();
                meshInfo.normals = renderable.GetNormals();
                meshInfo.id = renderable.GetId();

                if (m_MeshesToProcess == null)
                    m_MeshesToProcess = new List<MeshInfo>();

                m_MeshesToProcess.Add(meshInfo);

                // For testing. Won't be here..
                ShadowUtility.ProcessMeshesForShadows(m_MeshesToProcess);
            }
        }

        private void LateUpdate()
        {
            //m_MeshToProcess
        }
    }
}
