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

        internal List<DecalProjectorComponent> m_Decals = new List<DecalProjectorComponent>();
        Mesh m_DecalMesh;
        CullingGroup m_CullingGroup;
        private BoundingSphere[] m_BoundingSpheres;
		private int[] m_ResultIndices;
		private int m_NumResults = 0;
     
        public DecalSystem()
        {
            m_DecalMesh = CoreUtils.CreateDecalMesh();            
        }

        // updates bounding spheres for all decals, also refreshes cull indices
        void UpdateCulling()
        {
            m_BoundingSpheres = new BoundingSphere[m_Decals.Count];
			m_ResultIndices = new int[m_Decals.Count];
            int cullIndex = 0;
            foreach (var decal in m_Decals)
            {
                decal.CullIndex = cullIndex;
                m_BoundingSpheres[cullIndex] = CoreUtils.GetDecalMeshBoundingSphere(decal.transform.localToWorldMatrix);
                cullIndex++;
            }           
        }

        // update bounding sphere for a single decal
        public void UpdateCulling(DecalProjectorComponent decal)
        {
            int cullIndex = decal.CullIndex;
            m_BoundingSpheres[cullIndex] = CoreUtils.GetDecalMeshBoundingSphere(decal.transform.localToWorldMatrix);
        }

        public void AddDecal(DecalProjectorComponent d)
        {
            if (d.CullIndex != DecalProjectorComponent.kInvalidIndex)
            {
                RemoveDecal(d);
            }
            d.CullIndex = m_Decals.Count;
            m_Decals.Add(d);
            UpdateCulling();
        }

        public void RemoveDecal(DecalProjectorComponent d)
        {
            m_Decals.Remove(d);
            UpdateCulling();
        }

        public void Cull(Camera camera)
        {
            m_CullingGroup = new CullingGroup();
            m_CullingGroup.targetCamera = camera;
            m_CullingGroup.SetBoundingSpheres(m_BoundingSpheres);            
        }

		public int QueryCullResults()
		{
			m_NumResults = m_CullingGroup.QueryIndices(true, m_ResultIndices, 0);
			return m_NumResults;
		}

        public void Render(ScriptableRenderContext renderContext, HDCamera camera, CommandBuffer cmd)
        {
            if (m_DecalMesh == null)
                m_DecalMesh = CoreUtils.CreateDecalMesh();

            for (int resultIndex = 0; resultIndex < m_NumResults; resultIndex++)
            {
                int decalIndex = m_ResultIndices[resultIndex];

                // If no decal material assigned, don't draw it
                if (m_Decals[decalIndex].m_Material == null)
                    continue;

                // don't draw decals that do not have textures assigned
                if (!m_Decals[decalIndex].m_Material.GetTexture("_BaseColorMap") && !m_Decals[decalIndex].m_Material.GetTexture("_NormalMap") &&
                    !m_Decals[decalIndex].m_Material.GetTexture("_MaskMap"))
                    continue;

                m_Decals[decalIndex].UpdatePropertyBlock(camera.cameraPos);
                cmd.DrawMesh(m_DecalMesh, m_Decals[decalIndex].transform.localToWorldMatrix, m_Decals[decalIndex].m_Material, 0, 0,
                    m_Decals[decalIndex].GetPropertyBlock());
            }

            m_CullingGroup.Dispose();
            m_CullingGroup = null;
        }
    }
}
