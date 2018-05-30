using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;


[ExecuteInEditMode]
public class TestRenderPipeline : RenderPipeline
{
    public override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
    {
        base.Render(renderContext, cameras);

        foreach (var camera in cameras)
        {
            var cmd = CommandBufferPool.Get("");

            renderContext.SetupCameraProperties(camera);

            CullingParameters cullingParameters = new CullingParameters();
            ScriptableCulling.FillCullingParameters(camera, ref cullingParameters);

            CullingResult result = new CullingResult();
            LightCullingResult lightResult = new LightCullingResult();
            Culling.CullScene(cullingParameters, result);
            Culling.CullLights(cullingParameters, lightResult);
            Culling.PrepareRendererScene(result, null, null);

            // Get directional light
            Light dirLight = null;
            foreach (var light in lightResult.visibleShadowCastingLights)
            {
                if(light.lightType == LightType.Directional)
                {
                    dirLight = light.light;
                }

                break;
            }

            if (dirLight == null)
            {
                foreach (var light in lightResult.visibleLights)
                {
                    if (light.lightType == LightType.Directional)
                    {
                        dirLight = light.light;
                    }
                    break;
                }
            }

            if (dirLight != null)
            {
                Vector3 foward = -dirLight.transform.forward;
                Vector4 color = dirLight.color * dirLight.intensity;
                cmd.SetGlobalVector("_LightDirection", new Vector4(foward.x, foward.y, foward.z, 0.0f));
                cmd.SetGlobalVector("_LightColor", color);
            }
            else
            {
                cmd.SetGlobalVector("_LightDirection", Vector4.zero);
                cmd.SetGlobalVector("_LightColor", Vector4.zero);
            }

            // Render Objects
            ShaderPassName[] passNames = { new ShaderPassName("Forward") };
            RendererList[] rendererLists = { new RendererList(), new RendererList() };
            RendererListSettings[] rendererListSettings = {
                                                            new RendererListSettings(renderQueueMin: (int)RenderQueue.Geometry, renderQueueMax: (int)RenderQueue.GeometryLast, shaderPassNames: passNames),
                                                            new RendererListSettings(renderQueueMin: (int)RenderQueue.Transparent, renderQueueMax: (int)RenderQueue.Transparent + 100, shaderPassNames: passNames),
                                                        };

            RendererList.PrepareRendererLists(result, rendererListSettings, rendererLists);

            int texID = Shader.PropertyToID("_CameraDepthBuffer");
            cmd.GetTemporaryRT(texID, camera.pixelWidth, camera.pixelHeight, 1, FilterMode.Point, RenderTextureFormat.Depth);

            CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget, texID);
            cmd.ClearRenderTarget(true, true, Color.grey);
            // Opaque
            cmd.DrawRenderers(rendererLists[0], new DrawRendererSettings_New());
            // Transparent
            cmd.DrawRenderers(rendererLists[1], new DrawRendererSettings_New());

            renderContext.ExecuteCommandBuffer(cmd);
            renderContext.Submit();
            CommandBufferPool.Release(cmd);
        }

        renderContext.Submit();
    }
}
