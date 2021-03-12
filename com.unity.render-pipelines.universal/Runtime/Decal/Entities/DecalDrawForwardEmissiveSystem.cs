using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class DecalDrawFowardEmissiveSystem
{
    private DecalEntityManager m_EntityManager;
    private Mesh m_DecalMesh;
    private Matrix4x4[] m_WorldToDecals;
    private Matrix4x4[] m_NormalToDecals;
    private int[] m_DecalLayerMasks;
    private ProfilingSampler m_Sampler;

    public DecalDrawFowardEmissiveSystem(DecalEntityManager entityManager)
    {
        m_EntityManager = entityManager;
        m_DecalMesh = CoreUtils.CreateCubeMesh(new Vector4(-0.5f, -0.5f, -0.5f, 1.0f), new Vector4(0.5f, 0.5f, 0.5f, 1.0f));

        m_WorldToDecals = new Matrix4x4[250];
        m_NormalToDecals = new Matrix4x4[250];
        m_DecalLayerMasks = new int[250];

        m_Sampler = new ProfilingSampler("DecalDrawFowardEmissiveSystem.Execute");
    }

    public void Execute(CommandBuffer cmd)
    {
        // On build for some reason mesh dereferences
        if (m_DecalMesh == null)
            m_DecalMesh = CoreUtils.CreateCubeMesh(new Vector4(-0.5f, -0.5f, -0.5f, 1.0f), new Vector4(0.5f, 0.5f, 0.5f, 1.0f));

        using (new ProfilingScope(cmd, m_Sampler))
        {
            for (int i = 0; i < m_EntityManager.chunkCount; ++i)
            {
                Execute(
                    m_EntityManager.entityChunks[i],
                    m_EntityManager.cachedChunks[i],
                    m_EntityManager.drawCallChunks[i],
                    m_EntityManager.entityChunks[i].count,
                    cmd);
            }
        }
    }

    private void Execute(DecalEntityChunk decalEntityChunk, DecalCachedChunk decalCachedChunk, DecalDrawCallChunk decalDrawCallChunk, int count, CommandBuffer cmd)
    {
        if (decalCachedChunk.passIndexEmissive == -1)
            return;

        decalDrawCallChunk.currentJobHandle.Complete();

        int subCallCount = decalDrawCallChunk.subCallCount;
        for (int i = 0; i < subCallCount; ++i)
        {
            var subCall = decalDrawCallChunk.subCalls[i];

            var decalToWorldSlice = decalDrawCallChunk.decalToWorlds.Reinterpret<Matrix4x4>();
            NativeArray<Matrix4x4>.Copy(decalToWorldSlice, subCall.start, m_WorldToDecals, 0, subCall.count);

            var normalToWorldSlice = decalDrawCallChunk.normalToDecals.Reinterpret<Matrix4x4>();
            NativeArray<Matrix4x4>.Copy(normalToWorldSlice, subCall.start, m_NormalToDecals, 0, subCall.count);

            decalCachedChunk.propertyBlock.SetMatrixArray("_NormalToWorld", m_NormalToDecals);
            //decalBatch.propertyBlock.SetFloatArray(MaterialProperties.kDecalLayerMaskFromDecal, m_DecalLayerMasks[batchIndex]);
            cmd.DrawMeshInstanced(m_DecalMesh, 0, decalEntityChunk.material, decalCachedChunk.passIndexEmissive, m_WorldToDecals, subCall.end - subCall.start, decalCachedChunk.propertyBlock);
        }
    }
}
