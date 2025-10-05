using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

#if UNITY_EDITOR
using UnityEditor.Rendering;
#endif

namespace UnityEngine.Rendering.Tests
{
    // TODO: Move this class to the Tests/Editor folder once the "IsolatedPackagesVerified" CI test correctly resolves all dependencies.
    // Currently, the URP package fails to locate the RenderGraphTestsCore class when it's placed under Tests/Editor,
    // unless the Core package is explicitly added to the "testables" list in the project manifest — which is not a sustainable solution.
    internal class RenderGraphTestsCore
    {
        // For RG Record/Hash/Compile testing, use m_RenderGraph
        protected RenderGraph m_RenderGraph;

        protected RenderPipelineAsset m_OldDefaultRenderPipeline;
        protected RenderPipelineAsset m_OldQualityRenderPipeline;

        // For RG Execute/Submit testing with rendering, use m_RenderGraphTestPipeline and m_RenderGraph in its recordRenderGraphBody
        protected RenderGraphTestPipelineAsset m_RenderGraphTestPipeline;
        protected RenderGraphTestGlobalSettings m_RenderGraphTestGlobalSettings;

        protected GameObject m_GameObject;

        // We need a camera to execute the render graph and a game object to attach a camera
        protected Camera m_Camera;

        // For the testing of the following RG steps: Execute and Submit (native) with camera rendering, use this custom RenderGraph render pipeline
        // through a camera render call to test the RG with a real ScriptableRenderContext
        internal class RenderGraphTestPipelineAsset : RenderPipelineAsset<RenderGraphTestPipelineInstance>
        {
            public Action<ScriptableRenderContext, Camera, CommandBuffer> recordRenderGraphBody;

            public RenderGraph renderGraph;

            public RenderTextureUVOriginStrategy renderTextureUVOriginStrategy;

            public bool invalidContextForTesting;

            protected override RenderPipeline CreatePipeline()
            {
                return new RenderGraphTestPipelineInstance(this);
            }

            // Called only once per UTR
            void OnEnable()
            {
                renderGraph = new();
            }
        }

        internal class RenderGraphTestPipelineInstance : RenderPipeline
        {
            RenderGraphTestPipelineAsset asset;

            // Having the RG at this level allows us to handle RG framework within Render() for easier testing
            RenderGraph m_RenderGraph;

            public RenderGraphTestPipelineInstance(RenderGraphTestPipelineAsset asset)
            {
                this.asset = asset;
                this.m_RenderGraph = asset.renderGraph;
            }

            protected override void Render(ScriptableRenderContext renderContext, List<Camera> cameras)
            {
                foreach (var camera in cameras)
                {
                    if (!camera.enabled)
                        continue;

                    var cmd = CommandBufferPool.Get();

                    RenderGraphParameters rgParams = new()
                    {
                        commandBuffer = cmd,
                        scriptableRenderContext = renderContext,
                        currentFrameIndex = Time.frameCount,
                        invalidContextForTesting = asset.invalidContextForTesting,
                        renderTextureUVOriginStrategy = asset.renderTextureUVOriginStrategy
                    };

                    try
                    {
                        // Necessary to reinitialize the state, since we have many tests which do not use this system but directly adding
                        // passes to the graph (to test the compilation) and destroying them without executing them with camera.Render()
                        // (e.g GraphicsPassWriteWaitOnAsyncPipe). So the state becomes RecordGraph because of the Builder.Destroy logic.
                        m_RenderGraph.RenderGraphState = RenderGraphState.Idle;

                        m_RenderGraph.BeginRecording(rgParams);

                        asset.recordRenderGraphBody?.Invoke(renderContext, camera, cmd);

                        m_RenderGraph.EndRecordingAndExecute();
                    }
                    catch (Exception e)
                    {
                        if (m_RenderGraph.ResetGraphAndLogException(e))
                            throw;
                        return;
                    }

                    if (rgParams.invalidContextForTesting == false)
                    {
                        renderContext.ExecuteCommandBuffer(cmd);
                    }

                    CommandBufferPool.Release(cmd);
                }

                renderContext.Submit();
            }
        }

        [SupportedOnRenderPipeline(typeof(RenderGraphTestPipelineAsset))]
        [System.ComponentModel.DisplayName("RenderGraphTest")]
        internal class RenderGraphTestGlobalSettings : RenderPipelineGlobalSettings<RenderGraphTestGlobalSettings, RenderGraphTestPipelineInstance>
        {
            [SerializeField] RenderPipelineGraphicsSettingsContainer m_Settings = new();

            protected override List<IRenderPipelineGraphicsSettings> settingsList => m_Settings.settingsList;
        }

        [OneTimeSetUp]
        public void Setup()
        {
            // Setting default global settings to the custom RG render pipeline type, no quality settings so we can rely on the default RP
            m_RenderGraphTestGlobalSettings = ScriptableObject.CreateInstance<RenderGraphTestGlobalSettings>();
#if UNITY_EDITOR
            EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<RenderGraphTestPipelineInstance>(m_RenderGraphTestGlobalSettings);
#endif
            // Saving old render pipelines to set them back after testing
            m_OldDefaultRenderPipeline = GraphicsSettings.defaultRenderPipeline;
            m_OldQualityRenderPipeline = QualitySettings.renderPipeline;

            // Setting the custom RG render pipeline
            m_RenderGraphTestPipeline = ScriptableObject.CreateInstance<RenderGraphTestPipelineAsset>();
            GraphicsSettings.defaultRenderPipeline = m_RenderGraphTestPipeline;
            QualitySettings.renderPipeline = m_RenderGraphTestPipeline;

            // Getting the RG from the custom asset pipeline
            m_RenderGraph = m_RenderGraphTestPipeline.renderGraph;
            m_RenderGraph.nativeRenderPassesEnabled = true;

            // Necessary to disable it for the Unit Tests, as the caller is not the same.
            RenderGraph.RenderGraphExceptionMessages.enableCaller = false;

            // We need a real ScriptableRenderContext and a camera to execute the Render Graph
            m_GameObject = new GameObject("testGameObject")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            m_GameObject.tag = "MainCamera";
            m_Camera = m_GameObject.AddComponent<Camera>();
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            GraphicsSettings.defaultRenderPipeline = m_OldDefaultRenderPipeline;
            m_OldDefaultRenderPipeline = null;

            QualitySettings.renderPipeline = m_OldQualityRenderPipeline;
            m_OldQualityRenderPipeline = null;

            m_RenderGraph.Cleanup();

            Object.DestroyImmediate(m_RenderGraphTestPipeline);

#if UNITY_EDITOR
            EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<RenderGraphTestPipelineInstance>(null);
#endif
            Object.DestroyImmediate(m_RenderGraphTestGlobalSettings);

            GameObject.DestroyImmediate(m_GameObject);
            m_GameObject = null;
            m_Camera = null;
        }

        [TearDown]
        public void CleanupRenderGraph()
        {
            m_RenderGraphTestPipeline.invalidContextForTesting = false;
            // Cleaning all Render Graph resources and data structures
            // Nothing remains, Render Graph in next test will start from scratch
            m_RenderGraph.CleanupResourcesAndGraph();
        }
    }
}
