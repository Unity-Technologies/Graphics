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

        private const int kDecalBlockSize = 128;

        // to work on Vulkan Mobile?
        // Core\CoreRP\ShaderLibrary\UnityInstancing.hlsl
        // #if defined(SHADER_API_VULKAN) && defined(SHADER_API_MOBILE)
        //      #define UNITY_INSTANCED_ARRAY_SIZE  250
        private const int kDrawIndexedBatchSize = 250; 

        // cube mesh bounds for decal
        static Vector4 kMin = new Vector4(-0.5f, -1.0f, -0.5f, 1.0f);
        static Vector4 kMax = new Vector4( 0.5f,  0.0f,  0.5f, 1.0f);

        static public Mesh m_DecalMesh = null;
        static public Matrix4x4[] m_InstanceMatrices = new Matrix4x4[kDrawIndexedBatchSize];

        private Dictionary<int, DecalSet> m_DecalSets = new Dictionary<int, DecalSet>();

        private class DecalSet
        {
            private BoundingSphere GetDecalProjectBoundingSphere(Matrix4x4 decalToWorld)
            {
                Vector4 min = new Vector4();
                Vector4 max = new Vector4();
                min = decalToWorld * kMin;
                max = decalToWorld * kMax;
                BoundingSphere res = new BoundingSphere();
                res.position = (max + min) / 2;
                res.radius = ((Vector3)(max - min)).magnitude / 2;
                return res;
            }

            public void UpdateBoundingSphere(DecalProjectorComponent decal)
            {
                m_CachedTransforms[decal.CullIndex] = decal.transform.localToWorldMatrix;
                m_BoundingSpheres[decal.CullIndex] = GetDecalProjectBoundingSphere(m_CachedTransforms[decal.CullIndex]);
            }

            public void AddDecal(DecalProjectorComponent decal)
            {
                // increase array size if no space left
                if (m_DecalsCount == m_Decals.Length)
                {
                    DecalProjectorComponent[] newDecals = new DecalProjectorComponent[m_DecalsCount + kDecalBlockSize];
                    BoundingSphere[] newSpheres = new BoundingSphere[m_DecalsCount + kDecalBlockSize];
                    Matrix4x4[] newCachedTransforms = new Matrix4x4[m_DecalsCount + kDecalBlockSize];
                    m_ResultIndices = new int[m_DecalsCount + kDecalBlockSize];

                    m_Decals.CopyTo(newDecals, 0);
                    m_BoundingSpheres.CopyTo(newSpheres, 0);
                    m_CachedTransforms.CopyTo(newCachedTransforms, 0);

                    m_Decals = newDecals;
                    m_BoundingSpheres = newSpheres;
                    m_CachedTransforms = newCachedTransforms;
                }

                m_Decals[m_DecalsCount] = decal;
                m_Decals[m_DecalsCount].CullIndex = m_DecalsCount;
                UpdateBoundingSphere(m_Decals[m_DecalsCount]);
                m_DecalsCount++;
            }

            public void RemoveDecal(DecalProjectorComponent decal)
            {
                int removeAtIndex = decal.CullIndex;
                // replace with last decal in the list and update index
                m_Decals[removeAtIndex] = m_Decals[m_DecalsCount - 1]; // move the last decal in list
                m_Decals[removeAtIndex].CullIndex = removeAtIndex;
                m_Decals[m_DecalsCount - 1] = null;

                // update the bounding spheres array
                m_BoundingSpheres[removeAtIndex] = m_BoundingSpheres[m_DecalsCount - 1];
                m_CachedTransforms[removeAtIndex] = m_CachedTransforms[m_DecalsCount - 1];
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

            public void Render(ScriptableRenderContext renderContext, HDCamera camera, CommandBuffer cmd)
            {
                int instanceCount = 0;
                for (int resultIndex = 0; resultIndex < m_NumResults; resultIndex++)
                {
                    int decalIndex = m_ResultIndices[resultIndex];

                    m_InstanceMatrices[instanceCount] = m_CachedTransforms[decalIndex];
                    instanceCount++;
                    if (instanceCount == kDrawIndexedBatchSize)
                    {
                        cmd.DrawMeshInstanced(m_DecalMesh, 0, m_Decals[0].m_Material, 0, m_InstanceMatrices, kDrawIndexedBatchSize);
                        instanceCount = 0;
                    }
                }
                if (instanceCount > 0)
                {
                    cmd.DrawMeshInstanced(m_DecalMesh, 0, m_Decals[0].m_Material, 0, m_InstanceMatrices, instanceCount);
                }
            }


            public Material KeyMaterial
            {
                get
                {
                    return this.m_Material;
                }
                set
                {
                    this.m_Material = value;
                }
            }

            public int Count
            {
                get
                {
                    return this.m_DecalsCount;
                }
            }

            private CullingGroup m_CullingGroup = null;
            private BoundingSphere[] m_BoundingSpheres = new BoundingSphere[kDecalBlockSize];
            private DecalProjectorComponent[] m_Decals = new DecalProjectorComponent[kDecalBlockSize];
            private int[] m_ResultIndices = new int[kDecalBlockSize];
            private int m_NumResults = 0;
            private int m_DecalsCount = 0;
            private Matrix4x4[] m_CachedTransforms = new Matrix4x4[kDecalBlockSize];
            private Material m_Material;
        }
        
        public void AddDecal(DecalProjectorComponent decal)
        {
			if (decal.CullIndex != DecalProjectorComponent.kInvalidIndex) //do not add the same decal more than once
				return;

            if(!decal.IsValid())
                return;

            DecalSet decalSet = null;
            int key = decal.m_Material.GetInstanceID();
            if (!m_DecalSets.TryGetValue(key, out decalSet))
            {
                decalSet = new DecalSet();
                decalSet.KeyMaterial = decal.m_Material;
                m_DecalSets.Add(key, decalSet);
            }
            decalSet.AddDecal(decal);
        }

        public void RemoveDecal(DecalProjectorComponent decal)
        {
			if (decal.CullIndex == DecalProjectorComponent.kInvalidIndex) // check if we have this decal
				return;

            DecalSet decalSet = null;
            int key = decal.m_Material.GetInstanceID();
            if (m_DecalSets.TryGetValue(key, out decalSet))
            {
                decalSet.RemoveDecal(decal);
                if (decalSet.Count == 0)
                {
                    m_DecalSets.Remove(key);
                }
            }
        }

        public void UpdateBoundingSphere(DecalProjectorComponent decal)
        {
            if (decal.CullIndex == DecalProjectorComponent.kInvalidIndex) // check if we have this decal
                return;

            DecalSet decalSet = null;
            int key = decal.m_Material.GetInstanceID();
            if (m_DecalSets.TryGetValue(key, out decalSet))
            {
                decalSet.UpdateBoundingSphere(decal);
            }
        }

        public void BeginCull(Camera camera)
        {
            foreach (var pair in m_DecalSets)
            {
                pair.Value.BeginCull(camera);
            }
        }

		public int QueryCullResults()
		{
		    int totalVisibleDecals = 0;
            foreach (var pair in m_DecalSets)
            {
                totalVisibleDecals += pair.Value.QueryCullResults();
            }
		    return totalVisibleDecals;
		}

        public void EndCull()
        {
            foreach (var pair in m_DecalSets)
            {
                pair.Value.EndCull();
            }
        }

        public void Render(ScriptableRenderContext renderContext, HDCamera camera, CommandBuffer cmd)
        {
            if (m_DecalMesh == null)
                m_DecalMesh = CoreUtils.CreateCubeMesh(kMin, kMax);

            foreach (var pair in m_DecalSets)
            {
                pair.Value.Render(renderContext, camera, cmd);
            }            
        }
    }
}
