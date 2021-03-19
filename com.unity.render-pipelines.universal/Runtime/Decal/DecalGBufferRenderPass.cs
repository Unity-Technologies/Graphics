using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Experimental.Rendering;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering.Universal.Internal;

public class DecalDrawGBufferSystem
{
    private DecalEntityManager m_EntityManager;
    private Mesh m_DecalMesh;
    private Matrix4x4[] m_WorldToDecals;
    private Matrix4x4[] m_NormalToDecals;
    private int[] m_DecalLayerMasks;
    private ProfilingSampler m_Sampler;

    public DecalDrawGBufferSystem(DecalEntityManager entityManager)
    {
        m_EntityManager = entityManager;
        m_DecalMesh = CoreUtils.CreateCubeMesh(new Vector4(-0.5f, -0.5f, -0.5f, 1.0f), new Vector4(0.5f, 0.5f, 0.5f, 1.0f));

        m_WorldToDecals = new Matrix4x4[250];
        m_NormalToDecals = new Matrix4x4[250];
        m_DecalLayerMasks = new int[250];

        m_Sampler = new ProfilingSampler("DecalDrawGBufferSystem.Execute");
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
        if (decalCachedChunk.passIndexGBuffer == -1)
            return;

        decalCachedChunk.currentJobHandle.Complete();
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
            cmd.DrawMeshInstanced(m_DecalMesh, 0, decalEntityChunk.material, decalCachedChunk.passIndexGBuffer, m_WorldToDecals, subCall.end - subCall.start, decalCachedChunk.propertyBlock);
        }
    }
}

namespace UnityEngine.Rendering.Universal
{
    public class DecalGBufferRenderPass : ScriptableRenderPass
    {
        private static string s_DBufferDepthName = "DBufferDepth";

        private FilteringSettings m_FilteringSettings;
        private ProfilingSampler m_ProfilingSampler;
        private List<ShaderTagId> m_ShaderTagIdList;
        private DecalDrawGBufferSystem m_DrawSystem;
        private DecalScreenSpaceSettings m_Settings;
        DeferredLights m_DeferredLights;

        public DecalGBufferRenderPass(string profilerTag, DecalScreenSpaceSettings settings, DecalDrawGBufferSystem decalDrawFowardEmissiveSystem)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingGbuffer;
            //ConfigureInput(ScriptableRenderPassInput.Depth); // Require depth

            m_DrawSystem = decalDrawFowardEmissiveSystem;
            m_Settings = settings;
            m_ProfilingSampler = new ProfilingSampler(profilerTag);
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);

            m_ShaderTagIdList = new List<ShaderTagId>();
            m_ShaderTagIdList.Add(new ShaderTagId("DecalGBufferMesh"));
        }

        internal void Setup(DeferredLights deferredLights)
        {
            m_DeferredLights = deferredLights;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // depth
            {
                var depthDesc = renderingData.cameraData.cameraTargetDescriptor;
                depthDesc.graphicsFormat = GraphicsFormat.DepthAuto;
                depthDesc.depthBufferBits = 24;

                cmd.GetTemporaryRT(Shader.PropertyToID(s_DBufferDepthName), depthDesc);
            }

            var rts = new RenderTargetIdentifier[m_DeferredLights.GbufferAttachments.Length];
            for (int i = 0; i < m_DeferredLights.GbufferAttachments.Length; ++i)
                rts[i] = m_DeferredLights.GbufferAttachments[i].Identifier();

            ConfigureTarget(rts, m_DeferredLights.DepthAttachmentIdentifier);

            //ConfigureClear(ClearFlag.Depth, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                cmd.SetGlobalTexture("_CameraDepthTexture", Shader.PropertyToID("_CameraDepthAttachment"));

                m_DrawSystem?.Execute(cmd);

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(Shader.PropertyToID(s_DBufferDepthName));
        }
    }
}
