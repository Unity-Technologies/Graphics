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

        private const int kDefaultDrawDistance = 1000;
        public int DrawDistance
        {
            get
            {
                HDRenderPipelineAsset hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
                if (hdrp != null)
                {
                    return hdrp.renderPipelineSettings.decalSettings.drawDistance;
                }
                return kDefaultDrawDistance;
            }
        }

        public TextureCache2D TextureAtlas
        {
            get
            {
                if (m_DecalAtlas == null)
                {
                    m_DecalAtlas = new TextureCache2D();
                    m_DecalAtlas.AllocTextureArray(2048, 128, 128, TextureFormat.RGBA32, true);
                }
                return m_DecalAtlas;
            }
        }


        public Camera CurrentCamera
        {
            get
            {
                return m_Camera;
            }
            set
            {
                m_Camera = value;
            }
        }

        private static MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();

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

        // clustered draw data
        static public DecalData[] m_DecalDatas = new DecalData[kDecalBlockSize];
        static public SFiniteLightBound[] m_Bounds = new SFiniteLightBound[kDecalBlockSize];
        static public LightVolumeData[] m_LightVolumes = new LightVolumeData[kDecalBlockSize];
        static public int m_DecalDatasCount = 0;

		static public float[] m_BoundingDistances = new float[1];

        private Dictionary<int, DecalSet> m_DecalSets = new Dictionary<int, DecalSet>();
        private TextureCache2D m_DecalAtlas = null;

        // current camera
        private Camera m_Camera;

        static public int m_DecalsVisibleThisFrame = 0;

        private class DecalSet
        {
            private void InitializeMaterialValues()
            {
                m_DiffuseTexture = m_Material.GetTexture("_BaseColorMap");
                m_NormalTexture = m_Material.GetTexture("_NormalMap");
                m_MaskTexture = m_Material.GetTexture("_MaskMap");
                m_Blend = m_Material.GetFloat("_DecalBlend");
            }

            public DecalSet(Material material)
            {
                m_Material = material;
                InitializeMaterialValues();
            }

            private BoundingSphere GetDecalProjectBoundingSphere(Matrix4x4 decalToWorld)
            {
                Vector4 min = new Vector4();
                Vector4 max = new Vector4();
                min = decalToWorld * kMin;
                max = decalToWorld * kMax;
                BoundingSphere res = new BoundingSphere();
                res.position = (max + min) / 2;
                res.radius = ((Vector3) (max - min)).magnitude / 2;
                return res;
            }

            public void UpdateCachedData(DecalProjectorComponent decal)
            {
                m_CachedDecalToWorld[decal.CullIndex] = decal.transform.localToWorldMatrix;

                Matrix4x4 decalRotation = Matrix4x4.Rotate(decal.transform.rotation);
                // z/y axis swap for normal to decal space, Unity is column major
                float y0 = decalRotation.m01;
                float y1 = decalRotation.m11;
                float y2 = decalRotation.m21;
                decalRotation.m01 = decalRotation.m02;
                decalRotation.m11 = decalRotation.m12;
                decalRotation.m21 = decalRotation.m22;
                decalRotation.m02 = y0;
                decalRotation.m12 = y1;
                decalRotation.m22 = y2;

                m_CachedNormalToWorld[decal.CullIndex] = decalRotation;
                // draw distance can't be more than global draw distance
                m_CachedDrawDistances[decal.CullIndex].x = decal.m_DrawDistance < instance.DrawDistance
                    ? decal.m_DrawDistance
                    : instance.DrawDistance;
                m_CachedDrawDistances[decal.CullIndex].y = decal.m_FadeScale;
                m_BoundingSpheres[decal.CullIndex] = GetDecalProjectBoundingSphere(m_CachedDecalToWorld[decal.CullIndex]);
            }

            public void AddDecal(DecalProjectorComponent decal)
            {
                // increase array size if no space left
                if (m_DecalsCount == m_Decals.Length)
                {
                    DecalProjectorComponent[] newDecals = new DecalProjectorComponent[m_DecalsCount + kDecalBlockSize];
                    BoundingSphere[] newSpheres = new BoundingSphere[m_DecalsCount + kDecalBlockSize];
                    Matrix4x4[] newCachedTransforms = new Matrix4x4[m_DecalsCount + kDecalBlockSize];
                    Matrix4x4[] newCachedNormalToWorld = new Matrix4x4[m_DecalsCount + kDecalBlockSize];
                    Vector2[] newCachedDrawDistances = new Vector2[m_DecalsCount + kDecalBlockSize];
                    m_ResultIndices = new int[m_DecalsCount + kDecalBlockSize];

                    m_Decals.CopyTo(newDecals, 0);
                    m_BoundingSpheres.CopyTo(newSpheres, 0);
                    m_CachedDecalToWorld.CopyTo(newCachedTransforms, 0);
                    m_CachedNormalToWorld.CopyTo(newCachedNormalToWorld, 0);
                    m_CachedDrawDistances.CopyTo(newCachedDrawDistances, 0);

                    m_Decals = newDecals;
                    m_BoundingSpheres = newSpheres;
                    m_CachedDecalToWorld = newCachedTransforms;
                    m_CachedNormalToWorld = newCachedNormalToWorld;
                    m_CachedDrawDistances = newCachedDrawDistances;
                }

                m_Decals[m_DecalsCount] = decal;
                m_Decals[m_DecalsCount].CullIndex = m_DecalsCount;
                UpdateCachedData(m_Decals[m_DecalsCount]);
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
                m_CachedDecalToWorld[removeAtIndex] = m_CachedDecalToWorld[m_DecalsCount - 1];
                m_CachedNormalToWorld[removeAtIndex] = m_CachedNormalToWorld[m_DecalsCount - 1];
                m_CachedDrawDistances[removeAtIndex] = m_CachedDrawDistances[m_DecalsCount - 1];
                m_DecalsCount--;
                decal.CullIndex = DecalProjectorComponent.kInvalidIndex;
            }

            public void BeginCull()
            {
                if (m_CullingGroup != null)
                {
                    Debug.LogError("Begin/EndCull() called out of sequence for decal projectors.");
                }

                // let the culling group code do some of the heavy lifting for global draw distance
                m_BoundingDistances[0] = DecalSystem.instance.DrawDistance;
                m_NumResults = 0;
                m_CullingGroup = new CullingGroup();
                m_CullingGroup.targetCamera = instance.CurrentCamera;
                m_CullingGroup.SetDistanceReferencePoint( m_CullingGroup.targetCamera.transform.position);
                m_CullingGroup.SetBoundingDistances(m_BoundingDistances);
                m_CullingGroup.SetBoundingSpheres(m_BoundingSpheres);
                m_CullingGroup.SetBoundingSphereCount(m_DecalsCount);
            }

            public int QueryCullResults()
            {
                m_NumResults = m_CullingGroup.QueryIndices(true, m_ResultIndices, 0);
                return m_NumResults;
            }


            private void GetDecalVolumeDataAndBound(Matrix4x4 decalToWorld, Matrix4x4 worldToView)
            {

                var influenceX = decalToWorld.GetColumn(0) * 0.5f;
                var influenceY = decalToWorld.GetColumn(1) * 0.5f;
                var influenceZ = decalToWorld.GetColumn(2) * 0.5f;
                var pos = decalToWorld.GetColumn(3) - influenceY; // decal cube mesh pivot is at 0,0,0, with bottom face at -1 on the y plane

                Vector3 influenceExtents = new Vector3();
                influenceExtents.x = influenceX.magnitude;
                influenceExtents.y = influenceY.magnitude;
                influenceExtents.z = influenceZ.magnitude;

                // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
                var influenceRightVS = worldToView.MultiplyVector(influenceX / influenceExtents.x);
                var influenceUpVS = worldToView.MultiplyVector(influenceY / influenceExtents.y);
                var influenceForwardVS = worldToView.MultiplyVector(influenceZ / influenceExtents.z);
                var influencePositionVS = worldToView.MultiplyPoint(pos); // place the mesh pivot in the center

                m_Bounds[m_DecalDatasCount].center = influencePositionVS;
                m_Bounds[m_DecalDatasCount].boxAxisX = influenceRightVS * influenceExtents.x;
                m_Bounds[m_DecalDatasCount].boxAxisY = influenceUpVS * influenceExtents.y;
                m_Bounds[m_DecalDatasCount].boxAxisZ = influenceForwardVS * influenceExtents.z;
                m_Bounds[m_DecalDatasCount].scaleXY.Set(1.0f, 1.0f);
                m_Bounds[m_DecalDatasCount].radius = influenceExtents.magnitude;

                // The culling system culls pixels that are further
                //   than a threshold to the box influence extents.
                // So we use an arbitrary threshold here (k_BoxCullingExtentOffset)
                m_LightVolumes[m_DecalDatasCount].lightCategory = (uint)LightCategory.Decal;
                m_LightVolumes[m_DecalDatasCount].lightVolume = (uint)LightVolumeType.Box;
                m_LightVolumes[m_DecalDatasCount].featureFlags = (uint)LightFeatureFlags.Env;
                m_LightVolumes[m_DecalDatasCount].lightPos = influencePositionVS;
                m_LightVolumes[m_DecalDatasCount].lightAxisX = influenceRightVS;
                m_LightVolumes[m_DecalDatasCount].lightAxisY = influenceUpVS;
                m_LightVolumes[m_DecalDatasCount].lightAxisZ = influenceForwardVS;
                m_LightVolumes[m_DecalDatasCount].boxInnerDist = influenceExtents - LightLoop.k_BoxCullingExtentThreshold;
                m_LightVolumes[m_DecalDatasCount].boxInvRange.Set(1.0f / LightLoop.k_BoxCullingExtentThreshold.x, 1.0f /LightLoop. k_BoxCullingExtentThreshold.y, 1.0f / LightLoop.k_BoxCullingExtentThreshold.z);
            }

            private void AssignCurrentBatches(ref Matrix4x4[] decalToWorldBatch, ref Matrix4x4[] normalToWorldBatch, int batchCount)
            {
                if (m_DecalToWorld.Count == batchCount)
                {
                    decalToWorldBatch = new Matrix4x4[kDrawIndexedBatchSize];
                    m_DecalToWorld.Add(decalToWorldBatch);
                    normalToWorldBatch = new Matrix4x4[kDrawIndexedBatchSize];
                    m_NormalToWorld.Add(normalToWorldBatch);
                }
                else
                {
                    decalToWorldBatch = m_DecalToWorld[batchCount];
                    normalToWorldBatch = m_NormalToWorld[batchCount];
                }
            }

            public void CreateDrawData()
            {
                if (m_NumResults == 0)
                    return;

                int instanceCount = 0;
                int batchCount = 0;
                Matrix4x4[] decalToWorldBatch = null;
                Matrix4x4[] normalToWorldBatch = null;

                AssignCurrentBatches(ref decalToWorldBatch, ref normalToWorldBatch, batchCount);

                Vector3 cameraPos = instance.CurrentCamera.transform.position;
                Matrix4x4 worldToView = LightLoop.WorldToCamera(instance.CurrentCamera);
                for (int resultIndex = 0; resultIndex < m_NumResults; resultIndex++)
                {
                    int decalIndex = m_ResultIndices[resultIndex];
                    // do additional culling based on individual decal draw distances
                    float distanceToDecal = (cameraPos - m_BoundingSpheres[decalIndex].position).magnitude;
                    float cullDistance = m_CachedDrawDistances[decalIndex].x + m_BoundingSpheres[decalIndex].radius;
                    if (distanceToDecal < cullDistance)
                    {
                        // d-buffer data
                        decalToWorldBatch[instanceCount] = m_CachedDecalToWorld[decalIndex];
                        normalToWorldBatch[instanceCount] = m_CachedNormalToWorld[decalIndex];
                        float fadeFactor = Mathf.Clamp((cullDistance - distanceToDecal) / (cullDistance * (1.0f - m_CachedDrawDistances[decalIndex].y)), 0.0f, 1.0f);
                        normalToWorldBatch[instanceCount].m03 = fadeFactor * m_Blend;   // vector3 rotation matrix so bottom row and last column can be used for other data to save space
                        normalToWorldBatch[instanceCount].m13 = m_DiffuseTexIndex;      // texture atlas indices needed for clustered
                        normalToWorldBatch[instanceCount].m23 = m_NormalTexIndex;
                        normalToWorldBatch[instanceCount].m33 = m_MaskTexIndex;


                        // clustered forward data
                        m_DecalDatas[m_DecalDatasCount].worldToDecal = decalToWorldBatch[instanceCount].inverse;
                        m_DecalDatas[m_DecalDatasCount].normalToWorld = normalToWorldBatch[instanceCount];
                        GetDecalVolumeDataAndBound(decalToWorldBatch[instanceCount], worldToView);
                        m_DecalDatasCount++;

                        instanceCount++;
                        if (instanceCount == kDrawIndexedBatchSize)
                        {
                            instanceCount = 0;
                            batchCount++;
                            AssignCurrentBatches(ref decalToWorldBatch, ref normalToWorldBatch, batchCount);
                        }
                    }
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

            void UpdateTextureCache(CommandBuffer cmd)
            {
                if (m_DiffuseTexture != null)
                {
                    m_DiffuseTexIndex = instance.TextureAtlas.FetchSlice(cmd, m_DiffuseTexture);
                }
                else
                {
                    m_DiffuseTexIndex = -1;
                }

                if (m_NormalTexture != null)
                {
                    m_NormalTexIndex = instance.TextureAtlas.FetchSlice(cmd, m_NormalTexture);
                }
                else
                {
                    m_NormalTexIndex = -1;
                }

                if (m_MaskTexture != null)
                {
                    m_MaskTexIndex = instance.TextureAtlas.FetchSlice(cmd, m_MaskTexture);
                }
                else
                {
                    m_MaskTexIndex = -1;
                }
            }

            public void RemoveFromTextureCache()
            {
                if (m_DiffuseTexture != null)
                {
                    instance.TextureAtlas.RemoveEntryFromSlice(m_DiffuseTexture);
                }
                if (m_NormalTexture != null)
                {
                    instance.TextureAtlas.RemoveEntryFromSlice(m_NormalTexture);
                }
                if (m_MaskTexture != null)
                {
                    instance.TextureAtlas.RemoveEntryFromSlice(m_MaskTexture);
                }
            }

            public void UpdateCachedMaterialData(CommandBuffer cmd)
            {
                InitializeMaterialValues(); // refresh in case they changed in the UI
                UpdateTextureCache(cmd);
            }

            public void RenderIntoDBuffer(CommandBuffer cmd)
            {
                if(m_NumResults == 0)
                    return;
                int batchIndex = 0;
                int totalToDraw = m_NumResults;
                for (; batchIndex < m_NumResults / kDrawIndexedBatchSize; batchIndex++)
                {
                    m_PropertyBlock.SetMatrixArray(HDShaderIDs._NormalToWorldID, m_NormalToWorld[batchIndex]);
                    cmd.DrawMeshInstanced(m_DecalMesh, 0, KeyMaterial, 0, m_DecalToWorld[batchIndex], kDrawIndexedBatchSize, m_PropertyBlock);
                    totalToDraw -= kDrawIndexedBatchSize;
                }

                if(totalToDraw > 0)
                {
                    m_PropertyBlock.SetMatrixArray(HDShaderIDs._NormalToWorldID, m_NormalToWorld[batchIndex]);
                    cmd.DrawMeshInstanced(m_DecalMesh, 0, KeyMaterial, 0, m_DecalToWorld[batchIndex], totalToDraw, m_PropertyBlock);
                }
            }

            public Material KeyMaterial
            {
                get
                {
                    return this.m_Material;
                }
            }

            public int Count
            {
                get
                {
                    return this.m_DecalsCount;
                }
            }

            private List<Matrix4x4[]> m_DecalToWorld = new List<Matrix4x4[]>();
            private List<Matrix4x4[]> m_NormalToWorld = new List<Matrix4x4[]>();

            private CullingGroup m_CullingGroup = null;
            private BoundingSphere[] m_BoundingSpheres = new BoundingSphere[kDecalBlockSize];
            private DecalProjectorComponent[] m_Decals = new DecalProjectorComponent[kDecalBlockSize];
            private int[] m_ResultIndices = new int[kDecalBlockSize];
            private int m_NumResults = 0;
            private int m_DecalsCount = 0;
            private Matrix4x4[] m_CachedDecalToWorld = new Matrix4x4[kDecalBlockSize];
            private Matrix4x4[] m_CachedNormalToWorld = new Matrix4x4[kDecalBlockSize];
			private Vector2[] m_CachedDrawDistances = new Vector2[kDecalBlockSize]; // x - draw distance, y - fade scale
            private Material m_Material;
            private Texture m_DiffuseTexture = null;
            private Texture m_NormalTexture = null;
            private Texture m_MaskTexture = null;
            private float m_Blend = 0;
            private int m_DiffuseTexIndex = -1;
            private int m_NormalTexIndex = -1;
            private int m_MaskTexIndex = -1;
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
                decalSet = new DecalSet(decal.m_Material);
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
                    decalSet.RemoveFromTextureCache();
                    m_DecalSets.Remove(key);
                }
            }
        }

        public void UpdateCachedData(DecalProjectorComponent decal)
        {
            if (decal.CullIndex == DecalProjectorComponent.kInvalidIndex) // check if we have this decal
                return;

            DecalSet decalSet = null;
            int key = decal.m_Material.GetInstanceID();
            if (m_DecalSets.TryGetValue(key, out decalSet))
            {
                decalSet.UpdateCachedData(decal);
            }
        }

        public void BeginCull()
        {
            foreach (var pair in m_DecalSets)
            {
                pair.Value.BeginCull();
            }
        }

		private int QueryCullResults()
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
            m_DecalsVisibleThisFrame = QueryCullResults();
            foreach (var pair in m_DecalSets)
            {
                pair.Value.EndCull();
            }
        }

        // need a better way than passing light loop here
        public void RenderIntoDBuffer(CommandBuffer cmd)
        {
            if (m_DecalMesh == null)
                m_DecalMesh = CoreUtils.CreateCubeMesh(kMin, kMax);

            foreach (var pair in m_DecalSets)
            {
                pair.Value.RenderIntoDBuffer(cmd);
            }
        }

        public void SetAtlas(CommandBuffer cmd)
        {
			cmd.SetGlobalTexture(HDShaderIDs._DecalAtlasID, TextureAtlas.GetTexCache());
        }

        // updates textures, texture atlas indices and blend value
        public void UpdateCachedMaterialData(CommandBuffer cmd)
        {
            foreach (var pair in m_DecalSets)
            {
                pair.Value.UpdateCachedMaterialData(cmd);
            }
        }

        public void CreateDrawData()
        {
            m_DecalDatasCount = 0;
            // reallocate if needed
            if (m_DecalsVisibleThisFrame > m_DecalDatas.Length)
            {
                int newDecalDatasSize = ((m_DecalsVisibleThisFrame + kDecalBlockSize - 1) / kDecalBlockSize) * kDecalBlockSize;
                m_DecalDatas = new DecalData[newDecalDatasSize];
                m_Bounds = new  SFiniteLightBound[newDecalDatasSize];
                m_LightVolumes = new LightVolumeData[newDecalDatasSize];
            }
            foreach (var pair in m_DecalSets)
            {
                pair.Value.CreateDrawData();
            }
        }

        public void Cleanup()
        {
            if (m_DecalAtlas != null)
                m_DecalAtlas.Release();
            CoreUtils.Destroy(m_DecalMesh);
            // set to null so that they get recreated
            m_DecalAtlas = null;
            m_DecalMesh = null;
        }
    }
}
