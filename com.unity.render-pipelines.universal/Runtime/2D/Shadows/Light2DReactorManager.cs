using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Collections;

namespace UnityEngine.Experimental.Rendering.Universal
{
    public class Light2DReactorManager : MonoBehaviour
    {
        static GameObject m_LightReactorManagerGO;
        static List<ShadowGenerationInfo> m_MeshesToProcess;

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
                ShadowGenerationInfo meshInfo = new ShadowGenerationInfo();
                meshInfo.mesh = mesh;
                meshInfo.triangles = renderable.GetTriangles();
                meshInfo.vertices = renderable.GetVertices();
                meshInfo.normals = renderable.GetNormals();
                meshInfo.id = renderable.GetId();

                if (m_MeshesToProcess == null)
                    m_MeshesToProcess = new List<ShadowGenerationInfo>();

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
