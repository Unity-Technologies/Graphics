using System;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class PaniniProjectionPostProcessPass : PostProcessPass
    {
        public const string k_TargetName = "CameraColorPaniniProjection";

        Material m_Material;
        bool m_IsValid;

        public PaniniProjectionPostProcessPass(Shader shader)
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing - 1;
            this.profilingSampler = new ProfilingSampler("Blit Panini Projection");

            m_Material = PostProcessUtils.LoadShader(shader, passName);
            m_IsValid = m_Material != null;
        }

        public override void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            m_IsValid = false;
        }

        private class PaniniProjectionPassData
        {
            internal Material material;
            internal TextureHandle sourceTexture;
            internal Vector4 paniniParams;
            internal bool isPaniniGeneric;
        }
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if(!m_IsValid)
                return;

            var paniniProjection = volumeStack.GetComponent<PaniniProjection>();

            if(!paniniProjection.IsActive())
                return;

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (cameraData.isSceneViewCamera)
                return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            var sourceTexture = resourceData.cameraColor;
            var destinationTexture = PostProcessUtils.CreateCompatibleTexture(renderGraph, sourceTexture, k_TargetName, true, FilterMode.Bilinear);

            Camera camera = cameraData.camera;

            // Use source width/height for aspect ratio which can be different from camera aspect. (e.g. viewport)
            var desc = sourceTexture.GetDescriptor(renderGraph);
            float distance = paniniProjection.distance.value;
            var viewExtents = CalcViewExtents(camera.fieldOfView, desc.width, desc.height);
            var cropExtents = CalcCropExtents(camera.fieldOfView, distance, desc.width, desc.height);

            float scaleX = cropExtents.x / viewExtents.x;
            float scaleY = cropExtents.y / viewExtents.y;
            float scaleF = Mathf.Min(scaleX, scaleY);

            float paniniD = distance;
            float paniniS = Mathf.Lerp(1f, Mathf.Clamp01(scaleF), paniniProjection.cropToFit.value);

            using (var builder = renderGraph.AddRasterRenderPass<PaniniProjectionPassData>(passName, out var passData, profilingSampler))
            {
                builder.AllowGlobalStateModification(true);
                builder.SetRenderAttachment(destinationTexture, 0, AccessFlags.Write);
                passData.sourceTexture = sourceTexture;
                builder.UseTexture(sourceTexture, AccessFlags.Read);
                passData.material = m_Material;
                passData.paniniParams = new Vector4(viewExtents.x, viewExtents.y, paniniD, paniniS);
                passData.isPaniniGeneric = 1f - Mathf.Abs(paniniD) > float.Epsilon;

                builder.SetRenderFunc(static (PaniniProjectionPassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    data.material.SetVector(ShaderConstants._Params, data.paniniParams);
                    data.material.EnableKeyword(data.isPaniniGeneric ? ShaderKeywordStrings.PaniniGeneric : ShaderKeywordStrings.PaniniUnitDistance);

                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, data.material, 0);
                });
            }

            resourceData.cameraColor = destinationTexture;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 CalcViewExtents(float fieldOfView, int width, int height)
        {
            float fovY = fieldOfView * Mathf.Deg2Rad;
            float aspect = width / (float)height;

            float viewExtY = Mathf.Tan(0.5f * fovY);
            float viewExtX = aspect * viewExtY;

            return new Vector2(viewExtX, viewExtY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 CalcCropExtents(float fieldOfView, float d, int width, int height)
        {
            // given
            //    S----------- E--X-------
            //    |    `  ~.  /,´
            //    |-- ---    Q
            //    |        ,/    `
            //  1 |      ,´/       `
            //    |    ,´ /         ´
            //    |  ,´  /           ´
            //    |,`   /             ,
            //    O    /
            //    |   /               ,
            //  d |  /
            //    | /                ,
            //    |/                .
            //    P
            //    |              ´
            //    |         , ´
            //    +-    ´
            //
            // have X
            // want to find E

            float viewDist = 1f + d;

            var projPos = CalcViewExtents(fieldOfView, width, height);
            var projHyp = Mathf.Sqrt(projPos.x * projPos.x + 1f);

            float cylDistMinusD = 1f / projHyp;
            float cylDist = cylDistMinusD + d;
            var cylPos = projPos * cylDistMinusD;

            return cylPos * (viewDist / cylDist);
        }

        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        public static class ShaderConstants
        {
            public static readonly int _Params = Shader.PropertyToID("_Params");
        }
    }
}
