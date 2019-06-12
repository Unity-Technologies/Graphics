using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    public class Light2DReactor : MonoBehaviour
    {
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
            }
        }

        private void Update()
        {

        }
    }
}
