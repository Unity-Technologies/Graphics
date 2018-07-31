using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;


[ExecuteInEditMode]
public class TestRenderPipeline : RenderPipeline
{
    Culler                          m_Culler = new Culler();
    RenderersCullingResult          m_Result = new RenderersCullingResult();
    LightCullingResult              m_LightResult = new LightCullingResult();
    ReflectionProbeCullingResult    m_ProbeResult = new ReflectionProbeCullingResult();

    TestRenderPipelineAsset m_Asset;
    public TestRenderPipeline(TestRenderPipelineAsset asset)
    {
        m_Asset = asset;

        List<DebugUI.Widget> widgets = new List<DebugUI.Widget>();
        widgets.AddRange(new DebugUI.Widget[] { new DebugUI.BoolField { displayName = "Enable New Culling", getter = () => m_Asset.useNewCulling, setter = value => m_Asset.useNewCulling = value } });
        var panel = DebugManager.instance.GetPanel("Culling", true);
        panel.children.Add(widgets.ToArray());
    }

    public override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
    {
        base.Render(renderContext, cameras);

        foreach (var camera in cameras)
        {
            var cmd = CommandBufferPool.Get("");

            renderContext.SetupCameraProperties(camera);

            CullingParameters cullingParameters = new CullingParameters();
            ScriptableCulling.FillCullingParameters(camera, ref cullingParameters);
            if (camera.useOcclusionCulling)
                cullingParameters.parameters.cullingFlags |= CullFlag.OcclusionCull;

            //OcclusionCullingData occlusionCulling = new OcclusionCullingData();

            //Culling.BuildOcclusionCullingData(cullingParameters, occlusionCulling);

            //// Moche
            //cullingParameters.occlusionCulling = occlusionCulling;

            CullResults oldResult = new CullResults();
            ScriptableCullingParameters oldCullingParameters = new ScriptableCullingParameters();

            Light dirLight = null;

            if (m_Asset.useNewCulling)
            {
                m_Culler.CullRenderers(cullingParameters, m_Result);
                m_Culler.CullLights(cullingParameters, m_LightResult);
                m_Culler.CullReflectionProbes(cullingParameters, m_ProbeResult);


                Culling.PrepareRendererScene(m_Result, m_LightResult, m_ProbeResult);

                // Get directional light
                foreach (var light in m_LightResult.visibleShadowCastingLights)
                {
                    if (light.lightType == LightType.Directional)
                    {
                        dirLight = light.light;
                    }

                    break;
                }

                if (dirLight == null)
                {
                    foreach (var light in m_LightResult.visibleLights)
                    {
                        if (light.lightType == LightType.Directional)
                        {
                            dirLight = light.light;
                        }
                        break;
                    }
                }
            }
            else
            {
                CullResults.GetCullingParameters(camera, false, out oldCullingParameters);
                if (camera.useOcclusionCulling)
                    oldCullingParameters.cullingFlags |= CullFlag.OcclusionCull;

                CullResults.Cull(ref oldCullingParameters, renderContext, ref oldResult);

                foreach (var light in oldResult.visibleLights)
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

            if (m_Asset.useNewCulling)
            {
                RendererListSettings[] rendererListSettings = {
                                                            new RendererListSettings(renderQueueMin: (int)RenderQueue.Geometry, renderQueueMax: (int)RenderQueue.GeometryLast, shaderPassNames: passNames),
                                                            new RendererListSettings(renderQueueMin: (int)RenderQueue.Transparent, renderQueueMax: (int)RenderQueue.Transparent + 100, shaderPassNames: passNames),
                                                        };

                RendererList.PrepareRendererLists(m_Result, rendererListSettings, rendererLists);
            }

            int texID = Shader.PropertyToID("_CameraDepthBuffer");
            cmd.GetTemporaryRT(texID, camera.pixelWidth, camera.pixelHeight, 1, FilterMode.Point, RenderTextureFormat.Depth);

            CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget, texID);
            cmd.ClearRenderTarget(true, true, Color.grey);

            if (m_Asset.useNewCulling)
            {
                // Opaque
                cmd.DrawRenderers(rendererLists[0], new DrawRendererSettings_New());
                // Transparent
                cmd.DrawRenderers(rendererLists[1], new DrawRendererSettings_New());
            }
            else
            {
                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var drawSettings = new DrawRendererSettings(camera, new ShaderPassName("Forward"))
                {
                    rendererConfiguration = 0,
                    sorting = { flags = SortFlags.CommonOpaque }
                };
                var filterSettings = new FilterRenderersSettings(true)
                {
                    renderQueueRange = new RenderQueueRange()
                    {
                        max = (int)RenderQueue.GeometryLast,
                        min = (int)RenderQueue.Geometry
                    }
                };
                var filterSettingsTransparent = new FilterRenderersSettings(true)
                {
                    renderQueueRange = new RenderQueueRange()
                    {
                        max = (int)RenderQueue.Transparent + 100,
                        min = (int)RenderQueue.Transparent
                    }
                };
                renderContext.DrawRenderers(oldResult.visibleRenderers, ref drawSettings, filterSettings);
                renderContext.DrawRenderers(oldResult.visibleRenderers, ref drawSettings, filterSettingsTransparent);
            }

            renderContext.ExecuteCommandBuffer(cmd);
            renderContext.Submit();
            CommandBufferPool.Release(cmd);
        }

        renderContext.Submit();
    }

    public override void Dispose()
    {
        DebugManager.instance.RemovePanel("Culling");
    }
}
