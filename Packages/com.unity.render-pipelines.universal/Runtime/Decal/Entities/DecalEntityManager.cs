using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Jobs;

namespace UnityEngine.Rendering.Universal
{
    internal class DecalEntityIndexer
    {
        public struct DecalEntityItem
        {
            public int chunkIndex;
            public int arrayIndex;
            public int version;
        }

        private List<DecalEntityItem> m_Entities = new List<DecalEntityItem>();
        private Queue<int> m_FreeIndices = new Queue<int>();

        public bool IsValid(DecalEntity decalEntity)
        {
            if (m_Entities.Count <= decalEntity.index)
                return false;

            return m_Entities[decalEntity.index].version == decalEntity.version;
        }

        public DecalEntity CreateDecalEntity(int arrayIndex, int chunkIndex)
        {
            // Reuse
            if (m_FreeIndices.Count != 0)
            {
                int entityIndex = m_FreeIndices.Dequeue();
                int newVersion = m_Entities[entityIndex].version + 1;

                m_Entities[entityIndex] = new DecalEntityItem()
                {
                    arrayIndex = arrayIndex,
                    chunkIndex = chunkIndex,
                    version = newVersion,
                };

                return new DecalEntity()
                {
                    index = entityIndex,
                    version = newVersion,
                };
            }

            // Create new one
            {
                int entityIndex = m_Entities.Count;
                int version = 1;

                m_Entities.Add(new DecalEntityItem()
                {
                    arrayIndex = arrayIndex,
                    chunkIndex = chunkIndex,
                    version = version,
                });


                return new DecalEntity()
                {
                    index = entityIndex,
                    version = version,
                };
            }
        }

        public void DestroyDecalEntity(DecalEntity decalEntity)
        {
            Assert.IsTrue(IsValid(decalEntity));
            m_FreeIndices.Enqueue(decalEntity.index);

            // Update version that everything that points to it will have outdated version
            var item = m_Entities[decalEntity.index];
            item.version++;
            m_Entities[decalEntity.index] = item;
        }

        public DecalEntityItem GetItem(DecalEntity decalEntity)
        {
            Assert.IsTrue(IsValid(decalEntity));
            return m_Entities[decalEntity.index];
        }

        public void UpdateIndex(DecalEntity decalEntity, int newArrayIndex)
        {
            Assert.IsTrue(IsValid(decalEntity));
            var item = m_Entities[decalEntity.index];
            item.arrayIndex = newArrayIndex;
            item.version = decalEntity.version;
            m_Entities[decalEntity.index] = item;
        }

        public void RemapChunkIndices(List<int> remaper)
        {
            for (int i = 0; i < m_Entities.Count; ++i)
            {
                int newChunkIndex = remaper[m_Entities[i].chunkIndex];
                var item = m_Entities[i];
                item.chunkIndex = newChunkIndex;
                m_Entities[i] = item;
            }
        }

        public void Clear()
        {
            m_Entities.Clear();
            m_FreeIndices.Clear();
        }
    }

    internal struct DecalEntity
    {
        public int index;
        public int version;
    }

    /// <summary>
    /// Contains <see cref="DecalEntity"/> and shared material.
    /// </summary>
    internal class DecalEntityChunk : DecalChunk
    {
        public Material material;
        public NativeArray<DecalEntity> decalEntities;
        public DecalProjector[] decalProjectors;
        public TransformAccessArray transformAccessArray;

        public override void Push()
        {
            count++;
        }

        public override void RemoveAtSwapBack(int entityIndex)
        {
            RemoveAtSwapBack(ref decalEntities, entityIndex, count);
            RemoveAtSwapBack(ref decalProjectors, entityIndex, count);
            transformAccessArray.RemoveAtSwapBack(entityIndex);
            count--;
        }

        public override void SetCapacity(int newCapacity)
        {
            decalEntities.ResizeArray(newCapacity);
            ResizeNativeArray(ref transformAccessArray, decalProjectors, newCapacity);
            ArrayExtensions.ResizeArray(ref decalProjectors, newCapacity);
            capacity = newCapacity;
        }

        public override void Dispose()
        {
            if (capacity == 0)
                return;

            decalEntities.Dispose();
            transformAccessArray.Dispose();
            decalProjectors = null;
            count = 0;
            capacity = 0;
        }
    }

    /// <summary>
    /// Manages lifetime between <see cref="DecalProjector"></see> and <see cref="DecalEntity"/>.
    /// Contains all <see cref="DecalChunk"/>.
    /// </summary>
    internal class DecalEntityManager : IDisposable
    {
        public List<DecalEntityChunk> entityChunks = new List<DecalEntityChunk>();
        public List<DecalCachedChunk> cachedChunks = new List<DecalCachedChunk>();
        public List<DecalCulledChunk> culledChunks = new List<DecalCulledChunk>();
        public List<DecalDrawCallChunk> drawCallChunks = new List<DecalDrawCallChunk>();
        public int chunkCount;

        private ProfilingSampler m_AddDecalSampler;
        private ProfilingSampler m_ResizeChunks;
        private ProfilingSampler m_SortChunks;

        private DecalEntityIndexer m_DecalEntityIndexer = new DecalEntityIndexer();
        private Dictionary<Material, int> m_MaterialToChunkIndex = new Dictionary<Material, int>();

        private struct CombinedChunks
        {
            public DecalEntityChunk entityChunk;
            public DecalCachedChunk cachedChunk;
            public DecalCulledChunk culledChunk;
            public DecalDrawCallChunk drawCallChunk;
            public int previousChunkIndex;
            public bool valid;
        }
        private List<CombinedChunks> m_CombinedChunks = new List<CombinedChunks>();
        private List<int> m_CombinedChunkRemmap = new List<int>();

        private Material m_ErrorMaterial;
        public Material errorMaterial
        {
            get
            {
                if (m_ErrorMaterial == null)
                    m_ErrorMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/InternalErrorShader"));
                return m_ErrorMaterial;
            }
        }

        private Mesh m_DecalProjectorMesh;
        public Mesh decalProjectorMesh
        {
            get
            {
                if (m_DecalProjectorMesh == null)
                    m_DecalProjectorMesh = CoreUtils.CreateCubeMesh(new Vector4(-0.5f, -0.5f, -0.5f, 1.0f), new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
                return m_DecalProjectorMesh;
            }
        }

        public DecalEntityManager()
        {
            m_AddDecalSampler = new ProfilingSampler("DecalEntityManager.CreateDecalEntity");
            m_ResizeChunks = new ProfilingSampler("DecalEntityManager.ResizeChunks");
            m_SortChunks = new ProfilingSampler("DecalEntityManager.SortChunks");
        }

        public bool IsValid(DecalEntity decalEntity)
        {
            return m_DecalEntityIndexer.IsValid(decalEntity);
        }

        public DecalEntity CreateDecalEntity(DecalProjector decalProjector)
        {
            var material = decalProjector.material;
            if (material == null)
                material = errorMaterial;

            using (new ProfilingScope(m_AddDecalSampler))
            {
                int chunkIndex = CreateChunkIndex(material);
                int entityIndex = entityChunks[chunkIndex].count;

                DecalEntity entity = m_DecalEntityIndexer.CreateDecalEntity(entityIndex, chunkIndex);

                DecalEntityChunk entityChunk = entityChunks[chunkIndex];
                DecalCachedChunk cachedChunk = cachedChunks[chunkIndex];
                DecalCulledChunk culledChunk = culledChunks[chunkIndex];
                DecalDrawCallChunk drawCallChunk = drawCallChunks[chunkIndex];

                // Make sure we have space to add new entity
                if (entityChunks[chunkIndex].capacity == entityChunks[chunkIndex].count)
                {
                    using (new ProfilingScope(m_ResizeChunks))
                    {
                        int newCapacity = entityChunks[chunkIndex].capacity + entityChunks[chunkIndex].capacity;
                        newCapacity = math.max(8, newCapacity);

                        entityChunk.SetCapacity(newCapacity);
                        cachedChunk.SetCapacity(newCapacity);
                        culledChunk.SetCapacity(newCapacity);
                        drawCallChunk.SetCapacity(newCapacity);
                    }
                }

                entityChunk.Push();
                cachedChunk.Push();
                culledChunk.Push();
                drawCallChunk.Push();

                entityChunk.decalProjectors[entityIndex] = decalProjector;
                entityChunk.decalEntities[entityIndex] = entity;
                entityChunk.transformAccessArray.Add(decalProjector.transform);

                UpdateDecalEntityData(entity, decalProjector);

                return entity;
            }
        }

        private int CreateChunkIndex(Material material)
        {
            if (!m_MaterialToChunkIndex.TryGetValue(material, out int chunkIndex))
            {
                var propertyBlock = new MaterialPropertyBlock();

                // In order instanced and non instanced rendering to work with _NormalToWorld
                // We need to make sure array is created with maximum size
                propertyBlock.SetMatrixArray("_NormalToWorld", new Matrix4x4[250]);

                entityChunks.Add(new DecalEntityChunk() { material = material });
                cachedChunks.Add(new DecalCachedChunk()
                {
                    propertyBlock = propertyBlock,
                });

                culledChunks.Add(new DecalCulledChunk());
                drawCallChunks.Add(new DecalDrawCallChunk() { subCallCounts = new NativeArray<int>(1, Allocator.Persistent) });

                m_CombinedChunks.Add(new CombinedChunks());
                m_CombinedChunkRemmap.Add(0);

                m_MaterialToChunkIndex.Add(material, chunkCount);
                return chunkCount++;
            }

            return chunkIndex;
        }

        public void UpdateAllDecalEntitiesData()
        {
            foreach (var entityChunk in entityChunks)
            {
                for (int i = 0; i < entityChunk.count; ++i)
                {
                    var decalProjector = entityChunk.decalProjectors[i];
                    if (decalProjector == null)
                        continue;

                    var entity = entityChunk.decalEntities[i];
                    if (!IsValid(entity))
                        continue;

                    UpdateDecalEntityData(entity, decalProjector);
                }
            }
        }

        public void UpdateDecalEntityData(DecalEntity decalEntity, DecalProjector decalProjector)
        {
            var decalItem = m_DecalEntityIndexer.GetItem(decalEntity);

            int chunkIndex = decalItem.chunkIndex;
            int arrayIndex = decalItem.arrayIndex;

            DecalCachedChunk cachedChunk = cachedChunks[chunkIndex];

            cachedChunk.sizeOffsets[arrayIndex] = Matrix4x4.Translate(decalProjector.decalOffset) * Matrix4x4.Scale(decalProjector.decalSize);

            float drawDistance = decalProjector.drawDistance;
            float fadeScale = decalProjector.fadeScale;
            float startAngleFade = decalProjector.startAngleFade;
            float endAngleFade = decalProjector.endAngleFade;
            Vector4 uvScaleBias = decalProjector.uvScaleBias;
            int layerMask = decalProjector.gameObject.layer;
            ulong sceneLayerMask = decalProjector.gameObject.sceneCullingMask;
            float fadeFactor = decalProjector.fadeFactor;

            cachedChunk.drawDistances[arrayIndex] = new Vector2(drawDistance, fadeScale);
            // In the shader to remap from cosine -1 to 1 to new range 0..1  (with 0 - 0 degree and 1 - 180 degree)
            // we do 1.0 - (dot() * 0.5 + 0.5) => 0.5 * (1 - dot())
            // we actually square that to get smoother result => x = (0.5 - 0.5 * dot())^2
            // Do a remap in the shader. 1.0 - saturate((x - start) / (end - start))
            // After simplification => saturate(a + b * dot() * (dot() - 2.0))
            // a = 1.0 - (0.25 - start) / (end - start), y = - 0.25 / (end - start)
            if (startAngleFade == 180.0f) // angle fade is disabled
            {
                cachedChunk.angleFades[arrayIndex] = new Vector2(0.0f, 0.0f);
            }
            else
            {
                float angleStart = startAngleFade / 180.0f;
                float angleEnd = endAngleFade / 180.0f;
                var range = Mathf.Max(0.0001f, angleEnd - angleStart);
                cachedChunk.angleFades[arrayIndex] = new Vector2(1.0f - (0.25f - angleStart) / range, -0.25f / range);
            }
            cachedChunk.uvScaleBias[arrayIndex] = uvScaleBias;
            cachedChunk.layerMasks[arrayIndex] = layerMask;
            cachedChunk.sceneLayerMasks[arrayIndex] = sceneLayerMask;
            cachedChunk.fadeFactors[arrayIndex] = fadeFactor;
            cachedChunk.scaleModes[arrayIndex] = decalProjector.scaleMode;
            cachedChunk.renderingLayerMasks[arrayIndex] = RenderingLayerUtils.ToValidRenderingLayers(decalProjector.renderingLayerMask);

            cachedChunk.positions[arrayIndex] = decalProjector.transform.position;
            cachedChunk.rotation[arrayIndex] = decalProjector.transform.rotation;
            cachedChunk.scales[arrayIndex] = decalProjector.transform.lossyScale;
            cachedChunk.dirty[arrayIndex] = true;
        }

        public void DestroyDecalEntity(DecalEntity decalEntity)
        {
            if (!m_DecalEntityIndexer.IsValid(decalEntity))
                return;

            var decalItem = m_DecalEntityIndexer.GetItem(decalEntity);
            m_DecalEntityIndexer.DestroyDecalEntity(decalEntity);

            int chunkIndex = decalItem.chunkIndex;
            int arrayIndex = decalItem.arrayIndex;

            DecalEntityChunk entityChunk = entityChunks[chunkIndex];
            DecalCachedChunk cachedChunk = cachedChunks[chunkIndex];
            DecalCulledChunk culledChunk = culledChunks[chunkIndex];
            DecalDrawCallChunk drawCallChunk = drawCallChunks[chunkIndex];

            int lastArrayIndex = entityChunk.count - 1;
            if (arrayIndex != lastArrayIndex)
                m_DecalEntityIndexer.UpdateIndex(entityChunk.decalEntities[lastArrayIndex], arrayIndex);

            entityChunk.RemoveAtSwapBack(arrayIndex);
            cachedChunk.RemoveAtSwapBack(arrayIndex);
            culledChunk.RemoveAtSwapBack(arrayIndex);
            drawCallChunk.RemoveAtSwapBack(arrayIndex);
        }

        public void Update()
        {
            using (new ProfilingScope(m_SortChunks))
            {
                for (int i = 0; i < chunkCount; ++i)
                {
                    if (entityChunks[i].material == null)
                        entityChunks[i].material = errorMaterial;
                }

                // Combine chunks into single array
                for (int i = 0; i < chunkCount; ++i)
                {
                    m_CombinedChunks[i] = new CombinedChunks()
                    {
                        entityChunk = entityChunks[i],
                        cachedChunk = cachedChunks[i],
                        culledChunk = culledChunks[i],
                        drawCallChunk = drawCallChunks[i],
                        previousChunkIndex = i,
                        valid = entityChunks[i].count != 0,
                    };
                }


                // Sort
                m_CombinedChunks.Sort((a, b) =>
                {
                    if (a.valid && !b.valid)
                        return -1;
                    if (!a.valid && b.valid)
                        return 1;

                    if (a.cachedChunk.drawOrder < b.cachedChunk.drawOrder)
                        return -1;
                    if (a.cachedChunk.drawOrder > b.cachedChunk.drawOrder)
                        return 1;
                    return a.entityChunk.material.GetHashCode().CompareTo(b.entityChunk.material.GetHashCode());
                });

                // Early out if nothing changed
                bool dirty = false;
                for (int i = 0; i < chunkCount; ++i)
                {
                    if (m_CombinedChunks[i].previousChunkIndex != i || !m_CombinedChunks[i].valid)
                    {
                        dirty = true;
                        break;
                    }
                }
                if (!dirty)
                    return;

                // Update chunks
                int count = 0;
                m_MaterialToChunkIndex.Clear();
                for (int i = 0; i < chunkCount; ++i)
                {
                    var combinedChunk = m_CombinedChunks[i];

                    // Destroy invalid chunk for cleanup
                    if (!m_CombinedChunks[i].valid)
                    {
                        combinedChunk.entityChunk.currentJobHandle.Complete();
                        combinedChunk.cachedChunk.currentJobHandle.Complete();
                        combinedChunk.culledChunk.currentJobHandle.Complete();
                        combinedChunk.drawCallChunk.currentJobHandle.Complete();

                        combinedChunk.entityChunk.Dispose();
                        combinedChunk.cachedChunk.Dispose();
                        combinedChunk.culledChunk.Dispose();
                        combinedChunk.drawCallChunk.Dispose();

                        continue;
                    }

                    entityChunks[i] = combinedChunk.entityChunk;
                    cachedChunks[i] = combinedChunk.cachedChunk;
                    culledChunks[i] = combinedChunk.culledChunk;
                    drawCallChunks[i] = combinedChunk.drawCallChunk;
                    if (!m_MaterialToChunkIndex.ContainsKey(entityChunks[i].material))
                        m_MaterialToChunkIndex.Add(entityChunks[i].material, i);
                    m_CombinedChunkRemmap[combinedChunk.previousChunkIndex] = i;
                    count++;
                }

                // In case some chunks where destroyed resize the arrays
                if (chunkCount > count)
                {
                    entityChunks.RemoveRange(count, chunkCount - count);
                    cachedChunks.RemoveRange(count, chunkCount - count);
                    culledChunks.RemoveRange(count, chunkCount - count);
                    drawCallChunks.RemoveRange(count, chunkCount - count);
                    m_CombinedChunks.RemoveRange(count, chunkCount - count);
                    chunkCount = count;
                }

                // Remap entities chunk index with new sorted ones
                m_DecalEntityIndexer.RemapChunkIndices(m_CombinedChunkRemmap);
            }
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_ErrorMaterial);
            CoreUtils.Destroy(m_DecalProjectorMesh);

            foreach (var entityChunk in entityChunks)
                entityChunk.currentJobHandle.Complete();
            foreach (var cachedChunk in cachedChunks)
                cachedChunk.currentJobHandle.Complete();
            foreach (var culledChunk in culledChunks)
                culledChunk.currentJobHandle.Complete();
            foreach (var drawCallChunk in drawCallChunks)
                drawCallChunk.currentJobHandle.Complete();

            foreach (var entityChunk in entityChunks)
                entityChunk.Dispose();
            foreach (var cachedChunk in cachedChunks)
                cachedChunk.Dispose();
            foreach (var culledChunk in culledChunks)
                culledChunk.Dispose();
            foreach (var drawCallChunk in drawCallChunks)
                drawCallChunk.Dispose();

            m_DecalEntityIndexer.Clear();
            m_MaterialToChunkIndex.Clear();
            entityChunks.Clear();
            cachedChunks.Clear();
            culledChunks.Clear();
            drawCallChunks.Clear();
            m_CombinedChunks.Clear();
            chunkCount = 0;
        }
    }
}
