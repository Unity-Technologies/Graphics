using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Jobs;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class DecalUtilities
{
    public enum MaterialDecalPass
    {
        DBufferProjector = 0,
        DecalProjectorForwardEmissive = 1,
        DBufferMesh = 2,
        DecalMeshForwardEmissive = 3,
    };

    private static readonly string[] s_MaterialDecalPassNames = Enum.GetNames(typeof(MaterialDecalPass));

    public static string GetDecalPassName(MaterialDecalPass decalPass)
    {
        return s_MaterialDecalPassNames[(int)decalPass];
    }
}

public class DecalEntityIndexer
{
    public struct DecalEntityItem
    {
        public int index;
        public int version;
        public int chunk;
    }

    private List<DecalEntityItem> entities = new List<DecalEntityItem>();
    private Queue<int> unused = new Queue<int>();

    public bool IsValid(DecalEntity decalEntity)
    {
        if (entities.Count <= decalEntity.Index)
            return false;

        return entities[decalEntity.Index].version == decalEntity.Version;
    }

    public DecalEntity CreateDecalEntity(int index, int chunk)
    {
        // Reuse
        if (unused.Count != 0)
        {
            int itemIndex = unused.Dequeue();
            int newVersion = entities[itemIndex].version + 1;

            entities[itemIndex] = new DecalEntityItem()
            {
                index = index,
                chunk = chunk,
                version = newVersion,
            };

            return new DecalEntity()
            {
                Index = itemIndex,
                Version = newVersion,
            };
        }

        // Create new one
        {
            int itemIndex = entities.Count;
            int version = 1;

            entities.Add(new DecalEntityItem()
            {
                index = index,
                chunk = chunk,
                version = version,
            });


            return new DecalEntity()
            {
                Index = itemIndex,
                Version = version,
            };
        }
    }

    public void DestroyDecalEntity(DecalEntity decalEntity)
    {
        Assert.IsTrue(IsValid(decalEntity));
        unused.Enqueue(decalEntity.Index);

        // Update version that everything that points to it will have oudated version
        var item = entities[decalEntity.Index];
        item.version++;
        entities[decalEntity.Index] = item;
    }

    public DecalEntityItem GetItem(DecalEntity decalEntity)
    {
        Assert.IsTrue(IsValid(decalEntity));
        return entities[decalEntity.Index];
    }

    public void UpdateIndex(DecalEntity decalEntity, int newIndex)
    {
        Assert.IsTrue(IsValid(decalEntity));
        var item = entities[decalEntity.Index];
        item.index = newIndex;
        entities[decalEntity.Index] = item;
    }

    public void RemapChunkIndices(List<int> remaper)
    {
        for (int i = 0; i < entities.Count; ++i)
        {
            int newChunkIndex = remaper[entities[i].chunk];
            var item = entities[i];
            item.chunk = newChunkIndex;
            entities[i] = item;
        }
    }

    public void Clear()
    {
        entities.Clear();
        unused.Clear();
    }
}

public struct DecalEntity
{
    public int Index;
    public int Version;

    public bool valid { get => Version != 0; }
}

public class DecalEntityChunk : DecalChunk
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
        ResizeNativeArray(ref decalEntities, newCapacity);
        ResizeNativeArray(ref transformAccessArray, decalProjectors, newCapacity);
        ResizeArray(ref decalProjectors, newCapacity);
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

public class DecalEntityManager : IDisposable
{
    public List<DecalEntityChunk> entityChunks = new List<DecalEntityChunk>();
    public List<DecalCachedChunk> cachedChunks = new List<DecalCachedChunk>();
    public List<DecalCulledChunk> culledChunks = new List<DecalCulledChunk>();
    public List<DecalDrawCallChunk> drawCallChunks = new List<DecalDrawCallChunk>();
    public int chunkCount;

    private ProfilingSampler m_AddDecalSampler;
    private ProfilingSampler m_ResizeChunks;
    private ProfilingSampler m_SortChunks;
    private DecalEntityIndexer decalEntityIndexer = new DecalEntityIndexer();

    private Dictionary<Material, int> m_MaterialToChunkIndex = new Dictionary<Material, int>();

    private struct CombinedChunks
    {
        public DecalEntityChunk entityChunk;
        public DecalCachedChunk cachedChunk;
        public DecalCulledChunk culledChunk;
        public DecalDrawCallChunk drawCallChunk;
        public int previousChunkIndex;
    }
    private List<CombinedChunks> m_CombinedChunks = new List<CombinedChunks>();
    private List<int> m_CombinedChunkRemmap = new List<int>();

    public DecalEntityManager()
    {
        m_AddDecalSampler = new ProfilingSampler("DecalEntityManager.CreateDecalEntity");
        m_ResizeChunks = new ProfilingSampler("DecalEntityManager.ResizeChunks");
        m_SortChunks = new ProfilingSampler("DecalEntityManager.SortChunks");
    }

    public bool IsValid(DecalEntity decalEntity)
    {
        return decalEntityIndexer.IsValid(decalEntity);
    }

    // TODO
    /*public DecalEntity FindDecalEntity(DecalProjector decalProjector)
    {
        if (m_MaterialToChunkIndex.TryGetValue(decalProjector.material, out int chunkIndex))
        {
            DecalEntityChunk entityChunk = entityChunks[chunkIndex];
            for (int i = 0; i < entityChunk.count; ++i)
            {
                if (entityChunk.decalProjectors[i] == decalProjector)
                    return entityChunk.decalEntities[i];
            }
        }

        return new DecalEntity();
    }*/

    public DecalEntity CreateDecalEntity(DecalProjector decalProjector)
    {
        if (decalProjector.material == null)
            return new DecalEntity();

        using (new ProfilingScope(null, m_AddDecalSampler))
        {
            int chunkIndex = CreateChunkIndex(decalProjector.material);
            int entityIndex = entityChunks[chunkIndex].count;

            DecalEntity entity = decalEntityIndexer.CreateDecalEntity(entityIndex, chunkIndex);

            DecalEntityChunk entityChunk = entityChunks[chunkIndex];
            DecalCachedChunk cachedChunk = cachedChunks[chunkIndex];
            DecalCulledChunk culledChunk = culledChunks[chunkIndex];
            DecalDrawCallChunk drawCallChunk = drawCallChunks[chunkIndex];

            // Make sure we have space to add new entity
            if (entityChunks[chunkIndex].capacity == entityChunks[chunkIndex].count)
            {
                using (new ProfilingScope(null, m_ResizeChunks))
                {
                    int newCapacity = entityChunks[chunkIndex].capacity + entityChunks[chunkIndex].capacity;
                    newCapacity = math.max(250, newCapacity);

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
            entityChunks.Add(new DecalEntityChunk() { material = material });
            cachedChunks.Add(new DecalCachedChunk()
            {
                propertyBlock = new MaterialPropertyBlock(),
                drawDistance = 1000,
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

    public void UpdateDecalEntityData(DecalEntity decalEntity, DecalProjector decalProjector)
    {
        var decalItem = decalEntityIndexer.GetItem(decalEntity);

        int chunkIndex = decalItem.chunk;
        int i = decalItem.index;

        DecalCachedChunk cachedChunk = cachedChunks[chunkIndex];

        cachedChunk.sizeOffsets[i] = Matrix4x4.Translate(decalProjector.decalOffset) * Matrix4x4.Scale(decalProjector.decalSize);

        float drawDistance = decalProjector.drawDistance;
        float fadeScale = decalProjector.fadeScale;
        float startAngleFade = decalProjector.startAngleFade;
        float endAngleFade = decalProjector.endAngleFade;
        Vector4 uvScaleBias = decalProjector.uvScaleBias;
        bool affectsTransparency = decalProjector.affectsTransparency;
        int layerMask = decalProjector.gameObject.layer;
        ulong sceneLayerMask = decalProjector.gameObject.sceneCullingMask;
        float fadeFactor = decalProjector.fadeFactor;
        DecalLayerEnum decalLayerMask = decalProjector.decalLayerMask;

        // draw distance can't be more than global draw distance
        cachedChunk.drawDistances[i] = new Vector2(cachedChunk.drawDistance < drawDistance
            ? drawDistance
            : cachedChunk.drawDistance, fadeScale);
        // In the shader to remap from cosine -1 to 1 to new range 0..1  (with 0 - 0 degree and 1 - 180 degree)
        // we do 1.0 - (dot() * 0.5 + 0.5) => 0.5 * (1 - dot())
        // we actually square that to get smoother result => x = (0.5 - 0.5 * dot())^2
        // Do a remap in the shader. 1.0 - saturate((x - start) / (end - start))
        // After simplification => saturate(a + b * dot() * (dot() - 2.0))
        // a = 1.0 - (0.25 - start) / (end - start), y = - 0.25 / (end - start)
        if (startAngleFade == 180.0f) // angle fade is disabled
        {
            cachedChunk.angleFades[i] = new Vector2(0.0f, 0.0f);
        }
        else
        {
            float angleStart = startAngleFade / 180.0f;
            float angleEnd = endAngleFade / 180.0f;
            var range = Mathf.Max(0.0001f, angleEnd - angleStart);
            cachedChunk.angleFades[i] = new Vector2(1.0f - (0.25f - angleStart) / range, -0.25f / range);
        }
        cachedChunk.uvScaleBias[i] = uvScaleBias;
        cachedChunk.affectsTransparencies[i] = affectsTransparency;
        cachedChunk.layerMasks[i] = layerMask;
        cachedChunk.sceneLayerMasks[i] = sceneLayerMask;
        cachedChunk.fadeFactors[i] = fadeFactor;
        cachedChunk.decalLayerMasks[i] = decalLayerMask;
        cachedChunk.dirty[i] = true;
    }

    public void DestroyDecalEntity(DecalEntity decalEntity)
    {
        if (!decalEntityIndexer.IsValid(decalEntity))
            return;


        var decalItem = decalEntityIndexer.GetItem(decalEntity);
        decalEntityIndexer.DestroyDecalEntity(decalEntity);

        int chunkIndex = decalItem.chunk;
        int entityIndex = decalItem.index;

        DecalEntityChunk entityChunk = entityChunks[chunkIndex];
        DecalCachedChunk cachedChunk = cachedChunks[chunkIndex];
        DecalCulledChunk culledChunk = culledChunks[chunkIndex];
        DecalDrawCallChunk drawCallChunk = drawCallChunks[chunkIndex];

        // TODO clean this mess
        // Do swap back removing when it is not last
        if (entityIndex != entityChunk.count - 1)
        {
            decalEntityIndexer.UpdateIndex(entityChunk.decalEntities[entityChunk.count - 1], entityIndex);
        }


        entityChunk.RemoveAtSwapBack(entityIndex);
        cachedChunk.RemoveAtSwapBack(entityIndex);
        culledChunk.RemoveAtSwapBack(entityIndex);
        drawCallChunk.RemoveAtSwapBack(entityIndex);
    }

    public void Sort()
    {
        using (new ProfilingScope(null, m_SortChunks))
        {
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
                };
            }

            // Sort
            m_CombinedChunks.Sort((a, b) =>
            {
                if (a.cachedChunk.drawOrder < b.cachedChunk.drawOrder)
                    return -1;
                if (a.cachedChunk.drawOrder > b.cachedChunk.drawOrder)
                    return 1;
                return a.GetHashCode().CompareTo(b.GetHashCode());
            });

            // Early out if nothing changed
            bool dirty = false;
            for (int i = 0; i < chunkCount; ++i)
            {
                if (m_CombinedChunks[i].previousChunkIndex != i)
                {
                    dirty = true;
                    break;
                }
            }
            if (!dirty)
                return;

            // Update chunks
            m_MaterialToChunkIndex.Clear();
            for (int i = 0; i < chunkCount; ++i)
            {
                entityChunks[i] = m_CombinedChunks[i].entityChunk;
                cachedChunks[i] = m_CombinedChunks[i].cachedChunk;
                culledChunks[i] = m_CombinedChunks[i].culledChunk;
                drawCallChunks[i] = m_CombinedChunks[i].drawCallChunk;
                m_MaterialToChunkIndex.Add(entityChunks[i].material, i);
                m_CombinedChunkRemmap[m_CombinedChunks[i].previousChunkIndex] = i;
            }

            // Remap entities chunk index with new sorted ones
            decalEntityIndexer.RemapChunkIndices(m_CombinedChunkRemmap);
        }
    }

    public void Dispose()
    {
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

        decalEntityIndexer.Clear();
        m_MaterialToChunkIndex.Clear();
        entityChunks.Clear();
        cachedChunks.Clear();
        culledChunks.Clear();
        drawCallChunks.Clear();
        m_CombinedChunks.Clear();
        chunkCount = 0;
    }
}
