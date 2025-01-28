using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.HighDefinition
{
    public class RenderPipelineManagerCallbackTests
    {
        public class TestCaseSetup
        {
            public struct ExpectedTriggeredTimes
            {
                public uint beginCameraRender { get; set; }
                public uint endCameraRender { get; set; }
                public uint beginContextRender { get; set; }
                public uint endContextRender { get; set; }

                public ExpectedTriggeredTimes(uint beginCameraRender, uint endCameraRender, uint beginContextRender, uint endContextRender)
                {
                    this.beginCameraRender = beginCameraRender;
                    this.endCameraRender = endCameraRender;
                    this.beginContextRender = beginContextRender;
                    this.endContextRender = endContextRender;
                }
            }

            public string name { get; set; }
            public bool isRenderRequest { get; set; }
            public uint renderCountTimes { get; set; }
            public ExpectedTriggeredTimes expectedTriggeredTimes { get; set; }

            public Action<Camera, HDAdditionalCameraData, RenderTexture> setUpAction { get; set; }
            public Action<Camera, HDAdditionalCameraData, RenderTexture> tearDownAction { get; set; }

            public override string ToString()
            {
                return $"[{name}] RenderCountTimes: {renderCountTimes}, ExpectedTriggeredTimes: {{ BeginCameraRender: {expectedTriggeredTimes.beginCameraRender}, EndCameraRender: {expectedTriggeredTimes.endCameraRender}, BeginContextRender: {expectedTriggeredTimes.beginContextRender}, EndContextRender: {expectedTriggeredTimes.endContextRender} }}";
            }
        }

        private Camera m_Camera;
        private HDAdditionalCameraData m_AdditionalCameraData;
        private RenderTexture m_RT;

        [SetUp]
        public void Setup()
        {
            var go = new GameObject($"{nameof(RenderPipelineManagerCallbackTests)}_Main");
            m_Camera = go.AddComponent<Camera>();
            m_AdditionalCameraData = go.AddComponent<HDAdditionalCameraData>();

            // Avoid that the camera renders outside the submit render request
            m_Camera.enabled = false;

            m_RT = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
            Debug.Log($"{m_RT.depth} - {m_RT.depthStencilFormat} - {m_RT.depthBuffer}");
            m_RT.Create();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Camera.gameObject);
            m_RT.Release();
        }

        void SendRequest()
        {
            RenderPipeline.StandardRequest request = new();

            if (RenderPipeline.SupportsRenderRequest(m_Camera, request))
            {
                request.destination = m_RT;
                RenderPipeline.SubmitRenderRequest(m_Camera, request);
            }
        }

        public static IEnumerable TestCaseSourceProvider
        {
            get
            {

                void AdditionalCameraDataOncustomRender(ScriptableRenderContext arg1, HDCamera arg2)
                {
                }

                TestCaseSetup[] testCasesSetup =
                {
                    new TestCaseSetup()
                    {
                        name = "Standard Render Requests",
                        isRenderRequest = true,
                        renderCountTimes = 1,
                        expectedTriggeredTimes = new (1u, 1u, 1u, 1u),
                        setUpAction = null,
                        tearDownAction = null
                    },
                    new TestCaseSetup()
                    {
                        name = "Multiple Standard Render Requests",
                        isRenderRequest = true,
                        renderCountTimes = 5,
                        expectedTriggeredTimes = new (5u, 5u, 5u, 5u),
                        setUpAction = null,
                        tearDownAction = null
                    },
                    new TestCaseSetup()
                    {
                        name = "Camera Render",
                        isRenderRequest = false,
                        renderCountTimes = 1,
                        expectedTriggeredTimes = new (1u, 1u, 1u, 1u),
                        setUpAction = null,
                        tearDownAction = null
                    },
                    new TestCaseSetup()
                    {
                        name = "Multiple Camera Render",
                        isRenderRequest = false,
                        renderCountTimes = 10,
                        expectedTriggeredTimes = new (10u, 10u, 10u, 10u),
                        setUpAction = null,
                        tearDownAction = null
                    },
                    new TestCaseSetup()
                    {
                        name = "Camera Render to Target Texture",
                        isRenderRequest = false,
                        renderCountTimes = 1,
                        expectedTriggeredTimes = new (1u, 1u, 1u, 1u),
                        setUpAction = (cam, _, rt) => cam.targetTexture = rt,
                        tearDownAction = (cam, _, rt) => cam.targetTexture = null,
                    },
                    new TestCaseSetup()
                    {
                        name = "Multiple Camera Render to Target Texture",
                        isRenderRequest = false,
                        renderCountTimes = 10,
                        expectedTriggeredTimes = new (10u, 10u, 10u, 10u),
                        setUpAction = (cam, _, rt) => cam.targetTexture = rt,
                        tearDownAction = (cam, _, rt) => cam.targetTexture = null,
                    },
                    new TestCaseSetup()
                    {
                        name = "Camera Render with Custom Render",
                        isRenderRequest = false,
                        renderCountTimes = 1,
                        expectedTriggeredTimes = new (1u, 1u, 1u, 1u),
                        setUpAction = (cam, additionalCam, _) => additionalCam.customRender += AdditionalCameraDataOncustomRender,
                        tearDownAction = (cam, additionalCam, _) => additionalCam.customRender -= AdditionalCameraDataOncustomRender,
                    },
                    new TestCaseSetup()
                    {
                        name = "Multiple Camera Render with Custom Render",
                        isRenderRequest = false,
                        renderCountTimes = 10,
                        expectedTriggeredTimes = new (10u, 10u, 10u, 10u),
                        setUpAction = (cam, additionalCam, _) => additionalCam.customRender += AdditionalCameraDataOncustomRender,
                        tearDownAction = (cam, additionalCam, _) => additionalCam.customRender -= AdditionalCameraDataOncustomRender,
                    },
                    new TestCaseSetup()
                    {
                        name = "Camera Render with Custom Render And FullscreenPassthrough",
                        isRenderRequest = false,
                        renderCountTimes = 1,
                        // Fullscreen passthrough don't trigger begin/end camera rendering
                        expectedTriggeredTimes = new (0u, 0u, 1u, 1u),
                        setUpAction = (cam, additionalCam, _) =>
                        {
                            additionalCam.fullscreenPassthrough = true;
                            additionalCam.customRender += AdditionalCameraDataOncustomRender;
                        },
                        tearDownAction = (cam, additionalCam, _) =>
                        {
                            additionalCam.fullscreenPassthrough = false;
                            additionalCam.customRender -= AdditionalCameraDataOncustomRender;
                        },
                    },
                    new TestCaseSetup()
                    {
                        name = "Multiple Camera Render with Custom Render And FullscreenPassthrough",
                        isRenderRequest = false,
                        renderCountTimes = 10,
                        // Fullscreen passthrough don't trigger begin/end camera rendering
                        expectedTriggeredTimes = new (0u, 0u, 10u, 10u),
                        setUpAction = (cam, additionalCam, _) =>
                        {
                            additionalCam.fullscreenPassthrough = true;
                            additionalCam.customRender += AdditionalCameraDataOncustomRender;
                        },
                        tearDownAction = (cam, additionalCam, _) =>
                        {
                            additionalCam.fullscreenPassthrough = false;
                            additionalCam.customRender -= AdditionalCameraDataOncustomRender;
                        },
                    },
                };

                foreach (var i in testCasesSetup)
                {
                    yield return new TestCaseData(i)
                        .SetName(i.ToString())
                        .Returns(null);
                }
            }
        }

        private const int k_WarmUpRenderCount = 10;

        [UnityTest, TestCaseSource(nameof(TestCaseSourceProvider))]
        public IEnumerator Execute(TestCaseSetup test)
        {
            // Skip a few frames for the Render Pipeline to Stabilize
            for (int i = 0; i < k_WarmUpRenderCount; i++)
            {
                m_Camera.Render();
                yield return new WaitForEndOfFrame();
            }

            // Subscribe to RP manager callbacks
            uint beginCameraCalledTimes = 0u;
            void ActionBeginRendering(ScriptableRenderContext context, Camera camera)
            {
                if (camera == m_Camera)
                    beginCameraCalledTimes++;
            }

            uint beginContextCalledTimes = 0u;
            void ActionBeginContext(ScriptableRenderContext context, List<Camera> cameras)
            {
                if (cameras.Contains(m_Camera))
                    beginContextCalledTimes++;
            }


            uint endCameraCalledTimes = 0u;
            void ActionEndRendering(ScriptableRenderContext context, Camera camera)
            {
                if (camera == m_Camera)
                    endCameraCalledTimes++;
            }

            uint endContextCalledTimes = 0u;
            void ActionEndContext(ScriptableRenderContext context, List<Camera> cameras)
            {
                if (cameras.Contains(m_Camera))
                    endContextCalledTimes++;
            }

            RenderPipelineManager.beginContextRendering += ActionBeginContext;
            RenderPipelineManager.beginCameraRendering += ActionBeginRendering;
            RenderPipelineManager.endCameraRendering += ActionEndRendering;
            RenderPipelineManager.endContextRendering += ActionEndContext;

            test.setUpAction?.Invoke(m_Camera, m_AdditionalCameraData, m_RT);

            for (int i = 0; i < test.renderCountTimes; ++i)
            {
                if (test.isRenderRequest)
                {
                    SendRequest();
                }
                else
                {
                    m_Camera.Render();
                    yield return new WaitForEndOfFrame();
                }
            }

            test.tearDownAction?.Invoke(m_Camera, m_AdditionalCameraData, m_RT);

            // Unsubscribe to RP Manager callbacks
            RenderPipelineManager.beginCameraRendering -= ActionBeginRendering;
            RenderPipelineManager.beginContextRendering -= ActionBeginContext;
            RenderPipelineManager.endCameraRendering -= ActionEndRendering;
            RenderPipelineManager.endContextRendering -= ActionEndContext;

            Assert.AreEqual(test.expectedTriggeredTimes.beginCameraRender, beginCameraCalledTimes, "Begin Camera Render Count mismatch");
            Assert.AreEqual(test.expectedTriggeredTimes.endCameraRender, endCameraCalledTimes, "End Camera Render Count mismatch");
            Assert.AreEqual(test.expectedTriggeredTimes.beginContextRender, beginContextCalledTimes, "Begin Context Render Count mismatch");
            Assert.AreEqual(test.expectedTriggeredTimes.endContextRender, endContextCalledTimes, "End Context Render Count mismatch");

            yield return null;
        }
    }
}
