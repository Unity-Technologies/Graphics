using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class CullerStatisticsDebug
{
    public CullerStatistics stats;

    public void RegisterCullingStatsDebug(List<DebugUI.Widget> widgets)
    {
        widgets.AddRange(
            new DebugUI.Widget[]
            {
                new DebugUI.Foldout
                {
                    displayName = "Renderers",
                    children =
                    {
                        new DebugUI.Value { displayName = "Tested Objects", getter = () => stats.renderers.culling.testedObjects },
                        new DebugUI.Value { displayName = "Visible Objects", getter = () => stats.renderers.culling.visibleObjects},
                        new DebugUI.Value { displayName = "Main thread Objects", getter = () => stats.renderers.mainThreadObjectCount},
                    }
                 },
            });
        widgets.AddRange(
            new DebugUI.Widget[]
            {
                new DebugUI.Foldout
                {
                    displayName = "Lights",
                    children =
                    {
                        new DebugUI.Value { displayName = "Tested Objects", getter = () => stats.lights.culling.testedObjects },
                        new DebugUI.Value { displayName = "Visible Objects", getter = () => stats.lights.culling.visibleObjects},
                    }
                 },
            });
        widgets.AddRange(
            new DebugUI.Widget[]
            {
                new DebugUI.Foldout
                {
                    displayName = "Reflection Probes",
                    children =
                    {
                        new DebugUI.Value { displayName = "Tested Objects", getter = () => stats.reflectionProbes.culling.testedObjects },
                        new DebugUI.Value { displayName = "Visible Objects", getter = () => stats.reflectionProbes.culling.visibleObjects},
                    }
                 },
            });
    }
}

[ExecuteInEditMode]
public class TestRenderPipeline : RenderPipeline
{
    Culler                          m_Culler = new Culler();
    RenderersCullingResult          m_Result = new RenderersCullingResult();
    static ShaderPassName[]         m_PassNames = { new ShaderPassName("Forward") };
    RendererListSettings[]          m_RendererListSettings = {
                                                            new RendererListSettings(renderQueueMin: (int)RenderQueue.Geometry, renderQueueMax: (int)RenderQueue.GeometryLast, shaderPassNames: m_PassNames),
                                                            new RendererListSettings(renderQueueMin: (int)RenderQueue.Transparent, renderQueueMax: (int)RenderQueue.Transparent + 100, shaderPassNames: m_PassNames),
                                                            };
    RendererList[]                  m_RendererLists = { new RendererList(), new RendererList() };
    LightCullingResult m_LightResult = new LightCullingResult();
    ReflectionProbeCullingResult    m_ProbeResult = new ReflectionProbeCullingResult();

    bool m_GatherStats = false;
    CullerStatisticsDebug m_Statistics = new CullerStatisticsDebug();

    TestRenderPipelineAsset m_Asset;
    public TestRenderPipeline(TestRenderPipelineAsset asset)
    {
        m_Asset = asset;

        List<DebugUI.Widget> widgets = new List<DebugUI.Widget>();
        widgets.AddRange(
            new DebugUI.Widget[]
            {
                new DebugUI.BoolField { displayName = "Enable New Culling", getter = () => m_Asset.useNewCulling, setter = value => m_Asset.useNewCulling = value },
                new DebugUI.BoolField { displayName = "Gather Statistics", getter = () => m_GatherStats, setter = value => m_GatherStats = value },
            });

        m_Statistics.RegisterCullingStatsDebug(widgets);

        var panel = DebugManager.instance.GetPanel("Culling", true);
        panel.flags |= DebugUI.Flags.EditorForceUpdate;
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

            cullingParameters.enableJobs = false;
            cullingParameters.extractLightProbes = true;
            cullingParameters.gatherStatistics = m_GatherStats;
            cullingParameters.testMask = CullingTestMask.CullingMask | CullingTestMask.LODMask;// | CullingTest.SceneMask;

            //if (camera.useOcclusionCulling)
            //    cullingParameters.parameters.cullingFlags |= CullFlag.OcclusionCull;

            CullResults oldResult = new CullResults();
            ScriptableCullingParameters oldCullingParameters = new ScriptableCullingParameters();

            Light dirLight = null;

            if (m_Asset.useNewCulling)
            {
                m_Culler.CullRenderers(cullingParameters, m_Result);

                cullingParameters.testMask = CullingTestMask.CullingMask;
                m_Culler.CullLights(cullingParameters, m_LightResult);
                m_Culler.CullReflectionProbes(cullingParameters, m_ProbeResult);

                m_Statistics.stats = m_Culler.GetStatistics();

                m_Culler.PreparePerObjectData(m_Result, m_LightResult, m_ProbeResult);

                if (dirLight == null)
                {
                    foreach (var light in m_LightResult.visibleLights)
                    {
                        if (light.lightType == LightType.Directional)
                        {
                            dirLight = light.light;
                            break;
                        }
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
            if (m_Asset.useNewCulling)
            {
                RendererList.PrepareRendererLists(m_Result, m_RendererListSettings, m_RendererLists);
            }

            int texID = Shader.PropertyToID("_CameraDepthBuffer");
            cmd.GetTemporaryRT(texID, camera.pixelWidth, camera.pixelHeight, 1, FilterMode.Point, RenderTextureFormat.Depth);

            CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget, texID);
            cmd.ClearRenderTarget(true, true, Color.grey);

            if (m_Asset.useNewCulling)
            {
                // Opaque
                cmd.DrawRenderers(m_RendererLists[0], new DrawRendererSettings_New());
                // Transparent
                cmd.DrawRenderers(m_RendererLists[1], new DrawRendererSettings_New());
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
