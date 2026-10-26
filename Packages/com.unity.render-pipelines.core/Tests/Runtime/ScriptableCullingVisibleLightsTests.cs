using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;

namespace UnityEngine.Rendering.Tests
{
    // Lightweight coverage of visible-light sorting for all scriptable render pipelines
    [TestFixture]
    sealed class ScriptableCullingVisibleLightsTests
    {
        // Marker handed to SubmitRenderRequest so the pipeline culls for the test camera without rendering
        sealed class CullCaptureRequest
        {
        }

        sealed class CullingCaptureRenderPipelineAsset : RenderPipelineAsset<CullingCaptureRenderPipeline>
        {
            [System.NonSerialized] public int maximumVisibleLights = -1;
            [System.NonSerialized] public bool hasCapturedLights;
            [System.NonSerialized] public NativeArray<VisibleLight> capturedLights;

            protected override RenderPipeline CreatePipeline()
                => new CullingCaptureRenderPipeline(this);
        }

        sealed class CullingCaptureRenderPipeline : RenderPipeline
        {
            readonly CullingCaptureRenderPipelineAsset m_Asset;

            public CullingCaptureRenderPipeline(CullingCaptureRenderPipelineAsset asset)
            {
                m_Asset = asset;
            }

            protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
            {
            }

            protected override void ProcessRenderRequests<RequestData>(ScriptableRenderContext context, Camera camera, RequestData renderRequest)
            {
                if (!(renderRequest is CullCaptureRequest) || !camera.TryGetCullingParameters(out var cullingParameters))
                    return;

                cullingParameters.maximumVisibleLights = m_Asset.maximumVisibleLights;
                var cullingResults = context.Cull(ref cullingParameters);
                m_Asset.capturedLights = new NativeArray<VisibleLight>(cullingResults.visibleLights, Allocator.Persistent);
                m_Asset.hasCapturedLights = true;
            }
        }

        const int k_TestLayer = 21;
        const int k_TestLayerMask = 1 << k_TestLayer;

        GameObject m_Root;
        Camera m_Camera;
        CullingCaptureRenderPipelineAsset m_Asset;
        RenderPipelineAsset m_PreviousQualityPipeline;
        List<(Light light, int cullingMask)> m_ExistingLightMasks;
        List<Light> m_LightsSortedByDistance;

        [SetUp]
        public void Setup()
        {
            m_PreviousQualityPipeline = QualitySettings.renderPipeline;

            // Existing lights commonly affect every layer, so using a dedicated GameObject layer alone
            // does not stop them from appearing in the culling results or consuming the light budget.
            m_ExistingLightMasks = new List<(Light, int)>();
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                if ((light.cullingMask & k_TestLayerMask) == 0)
                    continue;

                m_ExistingLightMasks.Add((light, light.cullingMask));
                light.cullingMask &= ~k_TestLayerMask;
            }

            m_Root = new GameObject("ScriptableCullingVisibleLightsTests");
            m_Camera = new GameObject("Camera").AddComponent<Camera>();
            m_Camera.transform.SetParent(m_Root.transform);
            m_Camera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            m_Camera.fieldOfView = 60f;
            m_Camera.aspect = 16f / 9f;
            m_Camera.cullingMask = k_TestLayerMask;

            m_Asset = ScriptableObject.CreateInstance<CullingCaptureRenderPipelineAsset>();
            QualitySettings.renderPipeline = m_Asset;
        }

        [TearDown]
        public void Cleanup()
        {
            QualitySettings.renderPipeline = m_PreviousQualityPipeline;

            foreach (var (light, cullingMask) in m_ExistingLightMasks)
            {
                if (light != null)
                    light.cullingMask = cullingMask;
            }

            if (m_Asset != null && m_Asset.hasCapturedLights)
                m_Asset.capturedLights.Dispose();
            Object.DestroyImmediate(m_Root);
            Object.DestroyImmediate(m_Asset);
        }

        Light CreateLight(Vector3 position, LightType type = LightType.Point)
        {
            var go = new GameObject(type + " Light " + position);
            go.layer = k_TestLayer;
            go.transform.SetParent(m_Root.transform);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = type;
            light.range = 1f;
            light.cullingMask = k_TestLayerMask;
            return light;
        }

        void CreateTestLights()
        {
            // interleave some off frustum lights to make sure light sorting is correct..
            CreateLight(new Vector3(6f, 0f, 0.5f));
            var visible = new List<Light>
            {
                CreateLight(new Vector3(0f, 0f, 20f)),
                CreateLight(new Vector3(0f, 0f, 4f)),
            };
            CreateLight(new Vector3(-6f, 0f, 0.5f));
            visible.Add(CreateLight(new Vector3(0f, 0f, 12f)));
            visible.Add(CreateLight(new Vector3(0f, 0f, 8f)));
            visible.Add(CreateLight(new Vector3(0f, 0f, 16f)));
            // distinguish distance sorting from depth sorting
            visible.Add(CreateLight(new Vector3(0f, 3.5f, 7.5f)));

            var cameraPosition = m_Camera.transform.position;
            visible.Sort((a, b) =>
                (a.transform.position - cameraPosition).sqrMagnitude.CompareTo(
                    (b.transform.position - cameraPosition).sqrMagnitude));
            m_LightsSortedByDistance = visible;
        }

        List<Light> CaptureVisibleLights(int maximumVisibleLights)
        {
            m_Asset.maximumVisibleLights = maximumVisibleLights;
            if (m_Asset.hasCapturedLights)
            {
                m_Asset.capturedLights.Dispose();
                m_Asset.hasCapturedLights = false;
            }

            RenderPipeline.SubmitRenderRequest(m_Camera, new CullCaptureRequest());

            Assert.IsTrue(m_Asset.hasCapturedLights, "The culling capture pipeline did not process the render request.");

            var lights = new List<Light>(m_Asset.capturedLights.Length);
            foreach (var visibleLight in m_Asset.capturedLights)
                lights.Add(visibleLight.light);
            return lights;
        }

        static List<Light> FilterByType(List<Light> lights, LightType type)
        {
            var filtered = new List<Light>(lights.Count);
            foreach (var light in lights)
            {
                if (light.type == type)
                    filtered.Add(light);
            }
            return filtered;
        }

        [Test]
        public void VisibleLights_AreSortedByDistanceToCamera_AndExcludeLightsOutsideFrustum()
        {
            CreateTestLights();

            var capturedLights = CaptureVisibleLights(32);

            CollectionAssert.AreEqual(m_LightsSortedByDistance, capturedLights,
                "Expected all in-frustum lights sorted nearest to farthest, and the off-frustum lights excluded despite being closer to the camera.");
        }

        [Test]
        public void VisibleLights_OverBudget_KeepNearestLightsInFrustum()
        {
            CreateTestLights();

            var capturedLights = CaptureVisibleLights(3);

            CollectionAssert.AreEqual(m_LightsSortedByDistance.GetRange(0, 3), capturedLights,
                "Expected the visible light list truncated to the nearest in-frustum lights.");
        }

        [Test]
        public void VisibleLights_DirectionalLight_HasPriorityOverNearerLocalLights()
        {
            CreateTestLights();
            var directionalLight = CreateLight(new Vector3(0f, 30f, 0f), LightType.Directional);

            var capturedLights = CaptureVisibleLights(3);

            CollectionAssert.Contains(capturedLights, directionalLight,
                "Expected the directional light to always survive truncation.");
            CollectionAssert.AreEqual(m_LightsSortedByDistance.GetRange(0, 2), FilterByType(capturedLights, LightType.Point),
                "Expected the directional light to consume budget first, leaving slots for only the nearest local lights.");
        }
    }
}
