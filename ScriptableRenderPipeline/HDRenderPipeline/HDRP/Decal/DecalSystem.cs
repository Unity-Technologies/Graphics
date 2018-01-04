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

        private Mesh m_DecalMesh = null;
        private CullingGroup m_CullingGroup = null;
		private const int kDecalBlockSize = 128;
		private BoundingSphere[] m_BoundingSpheres = new BoundingSphere[kDecalBlockSize];
		private DecalProjectorComponent[] m_Decals = new DecalProjectorComponent[kDecalBlockSize];
		private int[] m_ResultIndices = new int[kDecalBlockSize];
		private int m_NumResults = 0;
		private int m_DecalsCount = 0;

        public DecalSystem()
        {
        }

        // update bounding sphere for a single decal
        public void UpdateBoundingSphere(DecalProjectorComponent decal)
        {
			m_BoundingSpheres[decal.CullIndex] = GetDecalProjectBoundingSphere(decal.transform.localToWorldMatrix);
        }

        public void AddDecal(DecalProjectorComponent decal)
        {
			if (decal.CullIndex != DecalProjectorComponent.kInvalidIndex) //do not add the same decal more than once
				return;

			// increase array size if no space left
            if(m_DecalsCount == m_Decals.Length)
            {
				DecalProjectorComponent[] newDecals = new DecalProjectorComponent[m_DecalsCount + kDecalBlockSize];
				BoundingSphere[] newSpheres = new BoundingSphere[m_DecalsCount + kDecalBlockSize];
				m_ResultIndices = new int[m_DecalsCount + kDecalBlockSize];

                m_Decals.CopyTo(newDecals, 0);
				m_BoundingSpheres.CopyTo(newSpheres, 0);

				m_Decals = newDecals;
				m_BoundingSpheres = newSpheres;
			}

			m_Decals[m_DecalsCount] = decal;
			m_Decals[m_DecalsCount].CullIndex = m_DecalsCount;
			UpdateBoundingSphere(m_Decals[m_DecalsCount]);
			m_DecalsCount++;
        }

        public void RemoveDecal(DecalProjectorComponent decal)
        {
			if (decal.CullIndex == DecalProjectorComponent.kInvalidIndex) //do not remove decal that has not been added
				return;

			int removeAtIndex = decal.CullIndex;
			// replace with last decal in the list and update index
			m_Decals[removeAtIndex] = m_Decals[m_DecalsCount - 1]; // move the last decal in list
			m_Decals[removeAtIndex].CullIndex = removeAtIndex;
			m_Decals[m_DecalsCount - 1] = null;

			// update the bounding spheres array
			m_BoundingSpheres[removeAtIndex] = m_BoundingSpheres[m_DecalsCount - 1];
			m_DecalsCount--;
			decal.CullIndex = DecalProjectorComponent.kInvalidIndex;
        }

        public void BeginCull(Camera camera)
        {
            if (m_CullingGroup != null)
            {
                Debug.LogError("Begin/EndCull() called out of sequence for decal projectors.");
            }
			m_NumResults = 0;
            m_CullingGroup = new CullingGroup();
            m_CullingGroup.targetCamera = camera;
            m_CullingGroup.SetBoundingSpheres(m_BoundingSpheres);
			m_CullingGroup.SetBoundingSphereCount(m_DecalsCount);
        }

		public int QueryCullResults()
		{
			m_NumResults = m_CullingGroup.QueryIndices(true, m_ResultIndices, 0);
			return m_NumResults;
		}

        public void Render(ScriptableRenderContext renderContext, HDCamera camera, CommandBuffer cmd)
        {
            if (m_DecalMesh == null)
                m_DecalMesh = CoreUtils.CreateCubeMesh(new Vector3(-0.5f, -1.0f, -0.5f), new Vector3(0.5f, 0.0f, 0.5f));

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
        }

        public void EndCull()
        {
            if (m_CullingGroup == null)
            {
                Debug.LogError("Begin/EndCull() called out of sequence for decal projectors.");
            }
            else
            {
                m_CullingGroup.Dispose();
                m_CullingGroup = null;
            }
        }

        // Decal are assume to use a CubeMesh with bounds min(-0.5f, -1.0f, -0.5f) max(0.5f, 0.0f, 0.5f)
        public BoundingSphere GetDecalProjectBoundingSphere(Matrix4x4 decalToWorld)
        {
            Vector4 min = new Vector4(-0.5f, -1.0f, -0.5f, 1.0f);
            Vector4 max = new Vector4(0.5f, 0.0f, 0.5f, 1.0f);
            min = decalToWorld * min;
            max = decalToWorld * max;
            BoundingSphere res = new BoundingSphere();
            res.position = (max + min) / 2;
            res.radius = ((Vector3)(max - min)).magnitude / 2;
            return res;
        }
    }
}
