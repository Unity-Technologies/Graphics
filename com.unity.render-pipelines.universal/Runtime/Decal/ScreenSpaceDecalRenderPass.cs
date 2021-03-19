using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Experimental.Rendering;
using UnityEngine;
using Unity.Collections;

public class DecalDrawScreenSpaceSystem
{
    private DecalEntityManager m_EntityManager;
    private Mesh m_DecalMesh;
    private Matrix4x4[] m_WorldToDecals;
    private Matrix4x4[] m_NormalToDecals;
    private int[] m_DecalLayerMasks;
    private ProfilingSampler m_Sampler;

    public DecalDrawScreenSpaceSystem(DecalEntityManager entityManager)
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
        if (decalCachedChunk.passIndexScreenSpace == -1)
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
            cmd.DrawMeshInstanced(m_DecalMesh, 0, decalEntityChunk.material, decalCachedChunk.passIndexScreenSpace, m_WorldToDecals, subCall.end - subCall.start, decalCachedChunk.propertyBlock);
        }
    }
}

namespace UnityEngine.Rendering.Universal
{
    public class ScreenSpaceDecalRenderPass : ScriptableRenderPass
    {
        private FilteringSettings m_FilteringSettings;
        private ProfilingSampler m_ProfilingSampler;
        private List<ShaderTagId> m_ShaderTagIdList;
        private DecalDrawScreenSpaceSystem m_DrawSystem;
        private DecalScreenSpaceSettings m_Settings;

        public ScreenSpaceDecalRenderPass(string profilerTag, DecalScreenSpaceSettings settings, DecalDrawScreenSpaceSystem decalDrawFowardEmissiveSystem)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques + 1;
            ConfigureInput(ScriptableRenderPassInput.Depth); // Require depth

            m_DrawSystem = decalDrawFowardEmissiveSystem;
            m_Settings = settings;
            m_ProfilingSampler = new ProfilingSampler(profilerTag);
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);

            m_ShaderTagIdList = new List<ShaderTagId>();
            m_ShaderTagIdList.Add(new ShaderTagId("DecalScreenSpaceMesh"));
        }

        private static readonly int s_ProjectionParams2ID = Shader.PropertyToID("_ProjectionParams2");
        private static readonly int s_CameraViewProjectionsID = Shader.PropertyToID("_CameraViewProjections");
        private static readonly int s_CameraViewTopLeftCornerID = Shader.PropertyToID("_CameraViewTopLeftCorner");
        private static readonly int s_CameraViewXExtentID = Shader.PropertyToID("_CameraViewXExtent");
        private static readonly int s_CameraViewYExtentID = Shader.PropertyToID("_CameraViewYExtent");
        private static readonly int s_CameraViewZExtentID = Shader.PropertyToID("_CameraViewZExtent");

        private Matrix4x4[] m_CameraViewProjections = new Matrix4x4[2];
        private Vector4[] m_CameraTopLeftCorner = new Vector4[2];
        private Vector4[] m_CameraXExtent = new Vector4[2];
        private Vector4[] m_CameraYExtent = new Vector4[2];
        private Vector4[] m_CameraZExtent = new Vector4[2];

        private const string k_OrthographicCameraKeyword = "_ORTHOGRAPHIC";

        private void SetupNormalReconstructProperties(CommandBuffer cmd, ref RenderingData renderingData)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            int eyeCount = renderingData.cameraData.xr.enabled && renderingData.cameraData.xr.singlePassEnabled ? 2 : 1;
#else
            int eyeCount = 1;
#endif
            for (int eyeIndex = 0; eyeIndex < eyeCount; eyeIndex++)
            {
                Matrix4x4 view = renderingData.cameraData.GetViewMatrix(eyeIndex);
                Matrix4x4 proj = renderingData.cameraData.GetProjectionMatrix(eyeIndex);
                m_CameraViewProjections[eyeIndex] = proj * view;

                // camera view space without translation, used by SSAO.hlsl ReconstructViewPos() to calculate view vector.
                Matrix4x4 cview = view;
                cview.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                Matrix4x4 cviewProj = proj * cview;
                Matrix4x4 cviewProjInv = cviewProj.inverse;

                Vector4 topLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1, 1, -1, 1));
                Vector4 topRightCorner = cviewProjInv.MultiplyPoint(new Vector4(1, 1, -1, 1));
                Vector4 bottomLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1, -1, -1, 1));
                Vector4 farCentre = cviewProjInv.MultiplyPoint(new Vector4(0, 0, 1, 1));
                m_CameraTopLeftCorner[eyeIndex] = topLeftCorner;
                m_CameraXExtent[eyeIndex] = topRightCorner - topLeftCorner;
                m_CameraYExtent[eyeIndex] = bottomLeftCorner - topLeftCorner;
                m_CameraZExtent[eyeIndex] = farCentre;
            }

            cmd.SetGlobalVector(s_ProjectionParams2ID, new Vector4(1.0f / renderingData.cameraData.camera.nearClipPlane, 0.0f, 0.0f, 0.0f));
            cmd.SetGlobalMatrixArray(s_CameraViewProjectionsID, m_CameraViewProjections);
            cmd.SetGlobalVectorArray(s_CameraViewTopLeftCornerID, m_CameraTopLeftCorner);
            cmd.SetGlobalVectorArray(s_CameraViewXExtentID, m_CameraXExtent);
            cmd.SetGlobalVectorArray(s_CameraViewYExtentID, m_CameraYExtent);
            cmd.SetGlobalVectorArray(s_CameraViewZExtentID, m_CameraZExtent);

            // Update keywords
            CoreUtils.SetKeyword(cmd, k_OrthographicCameraKeyword, renderingData.cameraData.camera.orthographic);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            SortingCriteria sortingCriteria = SortingCriteria.CommonTransparent;// renderingData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                CoreUtils.SetKeyword(cmd, "DECALS_NORMAL_BLEND_LOW", m_Settings.blend == DecalNormalBlend.NormalLow);
                CoreUtils.SetKeyword(cmd, "DECALS_NORMAL_BLEND_MEDIUM", m_Settings.blend == DecalNormalBlend.NormalMedium);
                CoreUtils.SetKeyword(cmd, "DECALS_NORMAL_BLEND_HIGH", m_Settings.blend == DecalNormalBlend.NormalHigh);

                float width = renderingData.cameraData.pixelWidth;
                float height = renderingData.cameraData.pixelHeight;
                cmd.SetGlobalVector("_ScreenSize", new Vector4(width, height, 1f / width, 1f / height));

                cmd.SetGlobalVector("_SourceSize", new Vector4(width, height, 1f / width, 1f / height));
                SetupNormalReconstructProperties(cmd, ref renderingData);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                m_DrawSystem?.Execute(cmd);

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
            {
                throw new System.ArgumentNullException("cmd");
            }

            CoreUtils.SetKeyword(cmd, "DECALS_NORMAL_BLEND_LOW", false);
            CoreUtils.SetKeyword(cmd, "DECALS_NORMAL_BLEND_MEDIUM", false);
            CoreUtils.SetKeyword(cmd, "DECALS_NORMAL_BLEND_HIGH", false);
        }
    }
}
