using System;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class MotionBlurPostProcessPass : PostProcessPass
    {
        public const string k_TargetName = "_MotionBlurTarget";

        Material m_Material;
        bool m_IsValid;

        public MotionBlurPostProcessPass(Shader shader)
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing - 1;
            this.profilingSampler = null;

            m_Material = PostProcessUtils.LoadShader(shader, passName);
            m_IsValid = m_Material != null;
        }

        public override void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            m_IsValid = false;
        }

        private class MotionBlurPassData
        {
            internal TextureHandle sourceTexture;
            internal Material material;
            internal int passIndex;
            internal Camera camera;
            internal Experimental.Rendering.XRPass xr;
            internal float intensity;
            internal float clamp;
            internal bool enableAlphaOutput;
        }
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if(!m_IsValid)
                return;

            var motionBlur = volumeStack.GetComponent<MotionBlur>();

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            var sourceTexture = resourceData.cameraColor;
            var destinationTexture = PostProcessUtils.CreateCompatibleTexture(renderGraph, sourceTexture, k_TargetName, true, FilterMode.Bilinear);

            TextureHandle motionVectorColor = resourceData.motionVectorColor;
            TextureHandle cameraDepthTexture = resourceData.cameraDepthTexture;

            var mode = motionBlur.mode.value;
            int passIndex = (int)motionBlur.quality.value;
            passIndex += (mode == MotionBlurMode.CameraAndObjects) ? ShaderPass.k_CameraAndObjectMotionBlurLow : ShaderPass.k_CameraMotionBlurLow;

            using (var builder = renderGraph.AddRasterRenderPass<MotionBlurPassData>("Motion Blur", out var passData, ProfilingSampler.Get(URPProfileId.RG_MotionBlur)))
            {
                builder.SetRenderAttachment(destinationTexture, 0, AccessFlags.Write);
                passData.sourceTexture = sourceTexture;
                builder.UseTexture(sourceTexture, AccessFlags.Read);

                if (mode == MotionBlurMode.CameraAndObjects)
                {
                    Debug.Assert(ScriptableRenderer.current.SupportsMotionVectors(), "Current renderer does not support motion vectors.");
                    Debug.Assert(motionVectorColor.IsValid(), "Motion vectors are invalid. Per-object motion blur requires a motion vector texture.");

                    builder.UseTexture(motionVectorColor, AccessFlags.Read);
                }

                Debug.Assert(cameraDepthTexture.IsValid(), "Camera depth texture is invalid. Per-camera motion blur requires a depth texture.");
                builder.UseTexture(cameraDepthTexture, AccessFlags.Read);
                passData.material = m_Material;
                passData.passIndex = passIndex;
                passData.camera = cameraData.camera;
                passData.xr = cameraData.xr;
                passData.enableAlphaOutput = cameraData.isAlphaOutputEnabled;
                passData.intensity = motionBlur.intensity.value;
                passData.clamp = motionBlur.clamp.value;
                builder.SetRenderFunc(static (MotionBlurPassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    UpdateMotionBlurMatrices(data.material, data.camera, data.xr);

                    var sourceSize = PostProcessUtils.CalcShaderSourceSize(sourceTextureHdl);
                    data.material.SetVector(ShaderConstants._SourceSize, sourceSize);
                    data.material.SetFloat(ShaderConstants._Intensity, data.intensity);
                    data.material.SetFloat(ShaderConstants._Clamp, data.clamp);
                    CoreUtils.SetKeyword(data.material, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, data.enableAlphaOutput);

                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, data.material, data.passIndex);
                });
            }

            resourceData.cameraColor = destinationTexture;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UpdateMotionBlurMatrices(Material material, Camera camera, Experimental.Rendering.XRPass xr)
        {
            MotionVectorsPersistentData motionData = null;

            if(camera.TryGetComponent<UniversalAdditionalCameraData>(out var additionalCameraData))
                motionData = additionalCameraData.motionVectorsPersistentData;

            if (motionData == null)
                return;

#if ENABLE_VR && ENABLE_XR_MODULE
            if (xr.enabled && xr.singlePassEnabled)
            {
                // pass maximum of 2 matrices per pass. Need to access into the matrix array
                var viewStartIndex = xr.viewCount * xr.multipassId;
                // Using motionData.stagingMatrixStereo as staging buffer to avoid allocation
                Array.Copy(motionData.previousViewProjectionStereo, viewStartIndex, motionData.stagingMatrixStereo, 0, xr.viewCount);
                material.SetMatrixArray(ShaderConstants._PrevViewProjMStereo, motionData.stagingMatrixStereo);
                Array.Copy(motionData.viewProjectionStereo, viewStartIndex, motionData.stagingMatrixStereo, 0, xr.viewCount);
                material.SetMatrixArray(ShaderConstants._ViewProjMStereo, motionData.stagingMatrixStereo);
            }
            else
#endif
            {
                int viewProjMIdx = 0;
#if ENABLE_VR && ENABLE_XR_MODULE
                if (xr.enabled)
                    viewProjMIdx = xr.multipassId * xr.viewCount;
#endif

                // TODO: These should be part of URP main matrix set. For now, we set them here for motion vector rendering.
                material.SetMatrix(ShaderConstants._PrevViewProjM, motionData.previousViewProjectionStereo[viewProjMIdx]);
                material.SetMatrix(ShaderConstants._ViewProjM, motionData.viewProjectionStereo[viewProjMIdx]);
            }
        }


        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        public static class ShaderConstants
        {
            public static readonly int _ViewProjM = Shader.PropertyToID("_ViewProjM");
            public static readonly int _PrevViewProjM = Shader.PropertyToID("_PrevViewProjM");
            public static readonly int _ViewProjMStereo = Shader.PropertyToID("_ViewProjMStereo");
            public static readonly int _PrevViewProjMStereo = Shader.PropertyToID("_PrevViewProjMStereo");

            public static readonly int _Intensity = Shader.PropertyToID("_Intensity");
            public static readonly int _Clamp = Shader.PropertyToID("_Clamp");
            public static readonly int _SourceSize = Shader.PropertyToID("_SourceSize");
        }

        public static class ShaderPass
        {
            public const int k_CameraMotionBlurLow = 0;
            public const int k_CameraMotionBlurMedium = 1;
            public const int k_CameraMotionBlurHigh = 2;
            public const int k_CameraAndObjectMotionBlurLow = 3;
            public const int k_CameraAndObjectMotionBlurMedium = 4;
            public const int k_CameraAndObjectMotionBlurHigh = 5;
        }
    }
}
