using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.TestTools.Graphics.Allocations;

namespace UnityEngine.Rendering.Universal.Tests.Allocations
{
    /// <summary>
    /// Shared setup for URP allocation tests, so each project's fixture is just a list of entry points.
    ///
    /// The pass under test is recorded inside a real URP frame through
    /// <see cref="AllocationMeasurementFeature"/>, so the frame data, resource handles and culling
    /// results are the ones URP built. There is no image comparison, no reference image and no authored
    /// scene.
    ///
    /// Iterations are driven by calling <c>Camera.Render</c> rather than by yielding. A yielded frame
    /// only renders when something drives the game view, and on a CI agent nothing does. The URP image
    /// tests call <c>Camera.Render</c> for the same reason. Rendering on demand also keeps iterations
    /// deterministic and the tests synchronous.
    ///
    /// The measurement bodies live here rather than in the project fixtures because passes like
    /// <c>BloomPostProcessPass</c> are internal to URP, and this assembly is the one with access.
    /// </summary>
    public abstract class UrpAllocationTestBase
    {
        const int k_Width = 1920;
        const int k_Height = 1080;

        UniversalRendererData m_RendererData;
        AllocationMeasurementFeature m_Feature;
        Camera m_Camera;
        RenderTexture m_Target;
        VolumeStack m_VolumeStack;

        // Lives for the whole test, not just the measured window. The test framework evaluates captured
        // logs after the test body returns, so a watcher that stops earlier hands the failure back to
        // the framework's bare "unhandled log message" instead of the allocation report.
        AllocationLogWatcher m_Logs;

        // Renderer order on k_AssetName. Tests index by this, so the asset's renderers must match it.
        static readonly RenderingMode[] k_RenderingModes =
        {
            RenderingMode.Forward,
            RenderingMode.ForwardPlus,
            RenderingMode.Deferred,
            RenderingMode.DeferredPlus
        };

        const string k_AssetName = "AllocationTests_URP";

        UniversalRenderPipelineAsset m_Asset;
        RenderPipelineAsset m_RestoreGraphicsAsset;
        RenderPipelineAsset m_RestoreQualityAsset;
        bool m_SwappedAsset;

        /// <summary>
        /// Swaps to the URP asset the allocation tests own, once for the whole fixture. Swapping
        /// disposes and rebuilds the pipeline, so doing it per test would cost one rebuild each.
        /// </summary>
        [OneTimeSetUp]
        public void AllocationFixtureSetUp()
        {
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
                Assert.Ignore("URP is not the configured render pipeline.");

            m_Asset = FindOwnedAsset();
            if (m_Asset == null)
                return;

            Assert.That(m_Asset.rendererDataList.Length, Is.EqualTo(k_RenderingModes.Length),
                $"{k_AssetName} has {m_Asset.rendererDataList.Length} renderers, expected "
                + $"{k_RenderingModes.Length}, one per rendering mode.");

            if (ReferenceEquals(GraphicsSettings.defaultRenderPipeline, m_Asset))
                return;

            // Save and set both, as the other SRP tests that swap pipeline assets do.
            m_RestoreGraphicsAsset = GraphicsSettings.defaultRenderPipeline;
            m_RestoreQualityAsset = QualitySettings.renderPipeline;
            m_SwappedAsset = true;
            GraphicsSettings.defaultRenderPipeline = m_Asset;
            QualitySettings.renderPipeline = m_Asset;
        }

        // Matched on the asset's own name, not on what it supports: the point is to measure through the
        // one asset these tests own, so borrowing another test's asset is a failure rather than a
        // fallback. The quality level is where it is registered, which is also what puts it in a build.
        static UniversalRenderPipelineAsset FindOwnedAsset()
        {
            UniversalRenderPipelineAsset found = null;
            for (var level = 0; level < QualitySettings.count; level++)
            {
                if (QualitySettings.GetRenderPipelineAssetAt(level) is not UniversalRenderPipelineAsset asset
                    || asset.name != k_AssetName)
                {
                    continue;
                }

                Assert.That(found, Is.Null,
                    $"More than one quality level uses {k_AssetName}, so which one is measured would "
                    + "depend on ordering.");
                found = asset;
            }

            return found;
        }

        [OneTimeTearDown]
        public void AllocationFixtureTearDown()
        {
            if (!m_SwappedAsset)
                return;

            GraphicsSettings.defaultRenderPipeline = m_RestoreGraphicsAsset;
            QualitySettings.renderPipeline = m_RestoreQualityAsset;
            m_RestoreGraphicsAsset = null;
            m_RestoreQualityAsset = null;
            m_SwappedAsset = false;
        }

        [SetUp]
        public void AllocationTestSetUp()
        {
            m_Logs = new AllocationLogWatcher();
            m_Logs.Start();

            var asset = m_Asset ?? (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            Assert.That(asset.rendererDataList.Length, Is.GreaterThan(0), $"{asset.name} has no renderers.");
            m_RendererData = asset.rendererDataList[0] as UniversalRendererData;
            Assert.That(m_RendererData, Is.Not.Null, $"{asset.name} renderer 0 is not a UniversalRendererData.");

            m_Feature = ScriptableObject.CreateInstance<AllocationMeasurementFeature>();
            m_RendererData.rendererFeatures.Add(m_Feature);
            m_RendererData.SetDirty();

            var gameObject = new GameObject("URP_Allocations_Camera") { hideFlags = HideFlags.HideAndDontSave };
            m_Camera = gameObject.AddComponent<Camera>();
            m_Camera.clearFlags = CameraClearFlags.SolidColor;

            // Render to our own target rather than the backbuffer. The backbuffer brings in the game
            // view, whose size does not match the camera on a scaled display, and the overlay blit then
            // errors on the attachment dimensions. A fixed target also keeps the measurement the same
            // size everywhere.
            m_Target = new RenderTexture(k_Width, k_Height, 24, RenderTextureFormat.DefaultHDR)
            {
                name = "URP_Allocations_Target"
            };
            m_Camera.targetTexture = m_Target;

            // The camera would otherwise render the asset's default renderer, which is not always the
            // one holding the measurement feature.
            m_Camera.GetUniversalAdditionalCameraData().SetRenderer(0);

            // One render so RenderPipelineManager constructs URP. Its constructor initialises Blitter,
            // RTHandles, the shader global keywords and the VolumeManager. Constructing an instance by
            // hand would leave the manager to build a second one, and the measurement has to run inside
            // the manager's instance.
            m_Camera.Render();

            Assert.That(RenderPipelineManager.currentPipeline, Is.TypeOf<UniversalRenderPipeline>(),
                "Rendering the camera did not construct URP, so its global state is unavailable.");
        }

        [TearDown]
        public void AllocationTestTearDown()
        {
            if (m_VolumeStack is { isValid: true })
            {
                // DestroyStack, not Dispose: the manager asserts its created-stack list is empty when it
                // deinitialises, so the stack has to be unregistered as well as disposed.
                VolumeManager.instance.DestroyStack(m_VolumeStack);
            }

            m_VolumeStack = null;

            if (m_Camera != null)
            {
                m_Camera.targetTexture = null;
                Object.DestroyImmediate(m_Camera.gameObject);
                m_Camera = null;
            }

            if (m_Target != null)
            {
                m_Target.Release();
                Object.DestroyImmediate(m_Target);
                m_Target = null;
            }

            if (m_Feature != null)
            {
                m_RendererData.rendererFeatures.Remove(m_Feature);
                m_RendererData.SetDirty();
                Object.DestroyImmediate(m_Feature);
                m_Feature = null;
            }

            if (m_Logs != null)
            {
                var logs = m_Logs;
                m_Logs = null;
                logs.Dispose();

                // The watcher suppresses the framework's error-log gate for the whole test, so it has to
                // re-raise at the same scope. Otherwise an error logged in setup, or in a test that runs
                // its own detector, is swallowed where the framework used to fail.
                Assert.That(logs.hasErrors, Is.False,
                    "An error was logged during this test.\n" + logs.Describe());
            }
        }

        // Created on demand, and re-created when invalid: selecting a renderer on another URP asset
        // disposes the pipeline, and the VolumeManager disposes every stack with it.
        VolumeStack volumeStack
        {
            get
            {
                if (m_VolumeStack is not { isValid: true })
                    m_VolumeStack = VolumeManager.instance.CreateStack();

                return m_VolumeStack;
            }
        }

        /// <summary>
        /// Points the camera at the renderer this project authors for <paramref name="renderingMode"/>.
        ///
        /// All four renderers live on one URP asset the allocation tests own, so this is an index change
        /// rather than an asset change and costs no pipeline rebuild. The asset being authored and
        /// registered on a quality level is what makes it measurable in a player: URP strips shader
        /// variants per URP asset paired with each of its renderers as authored, so a mode nobody
        /// authored asks for variants nobody built.
        /// </summary>
        void UseRendererAuthoredFor(RenderingMode renderingMode)
        {
            Assert.That(m_Asset, Is.Not.Null,
                $"No quality level in this project uses {k_AssetName}. Measuring a rendering mode needs "
                + "the asset these tests own, registered on a quality level so the build analyses the "
                + "modes they measure.");

            var index = Array.IndexOf(k_RenderingModes, renderingMode);
            Assert.That(index, Is.GreaterThanOrEqualTo(0),
                $"{renderingMode} is not a mode {k_AssetName} authors, so add a renderer for it rather "
                + "than borrowing one from another test's asset.");

            var rendererData = m_Asset.rendererDataList[index] as UniversalRendererData;
            Assert.That(rendererData, Is.Not.Null,
                $"{k_AssetName} renderer {index} is not a UniversalRendererData.");
            Assert.That(rendererData.renderingMode, Is.EqualTo(renderingMode),
                $"{k_AssetName} renderer {index} is {rendererData.renderingMode} rather than "
                + $"{renderingMode}, so its renderers are no longer in the order the tests index by.");

            if (!ReferenceEquals(rendererData, m_RendererData))
            {
                m_RendererData.rendererFeatures.Remove(m_Feature);
                m_RendererData.SetDirty();

                m_RendererData = rendererData;
                m_RendererData.rendererFeatures.Add(m_Feature);
                m_RendererData.SetDirty();
            }

            m_Camera.GetUniversalAdditionalCameraData().SetRenderer(index);

            // Build the renderer before anything is measured.
            m_Camera.Render();
        }

        /// <summary>
        /// Records a test-owned pass that allocates and checks the detector catches it. This is what
        /// proves the measured window covers pass recording rather than only a bare delegate.
        /// </summary>
        protected void MeasureDetectionThroughARecordedPass()
        {
            var pass = new AllocatingTestPass();
            var run = new RenderAllocationRun(
                "detection proof",
                "pass allocating while recording",
                pass.RecordRenderGraph)
            {
                warmUpConvergenceRuns = 0,
                measuredRenders = 4
            };

            Drive(run);

            Assert.That(run.result.counts, Is.Not.Empty, "The detector measured no renders.");
            Assert.That(run.result.counts[0], Is.GreaterThan(0),
                "A pass that allocates while recording went undetected, so a real allocation in a URP "
                + "pass would go undetected too.");
        }

        /// <summary>
        /// Measures a whole frame that actually has work to do: geometry, a shadow-casting directional
        /// light, additional lights and an active post-processing stack. An empty frame skips most of
        /// URP, so a clean result from one says very little.
        /// </summary>
        protected IEnumerator MeasurePopulatedFrame(RenderingMode renderingMode, float renderScale = 1f)
            => MeasurePopulatedFrame(renderingMode, renderScale, DebugSceneOverrideMode.None);

        IEnumerator MeasurePopulatedFrame(
            RenderingMode renderingMode,
            float renderScale,
            DebugSceneOverrideMode sceneOverrideMode)
        {
            UseRendererAuthoredFor(renderingMode);

            // Debug views are global state on a singleton, so they are restored below rather than left
            // for the next test to inherit.
            var debugSettings = UniversalRenderPipelineDebugDisplaySettings.Instance.renderingSettings;
            var previousSceneOverride = debugSettings.sceneOverrideMode;
            debugSettings.sceneOverrideMode = sceneOverrideMode;

            // Render scale is not one of the settings the build-time stripper keys variants on, so it
            // can be set here rather than needing a renderer authored for it.
            var previousRenderScale = m_Asset.renderScale;
            m_Asset.renderScale = renderScale;

            // Occlusion culling without baked data, as the scene the image suite ignores for this has.
#pragma warning disable CS0618
            m_Camera.useOcclusionCulling = true;
#pragma warning restore CS0618

            var scene = new GameObject("URP_Allocations_Scene") { hideFlags = HideFlags.HideAndDontSave };
            Material material = null;
            VolumeProfile profile = null;
            try
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                var mesh = BuildCubeMesh();

                for (var i = 0; i < 200; i++)
                {
                    var cube = new GameObject($"Cube{i}");
                    cube.transform.SetParent(scene.transform);
                    cube.transform.position = new Vector3(i % 20 - 10f, i / 20 % 10 - 5f, 4f + i % 7);
                    cube.AddComponent<MeshFilter>().sharedMesh = mesh;
                    cube.AddComponent<MeshRenderer>().sharedMaterial = material;
                }

                var sunObject = new GameObject("Sun");
                sunObject.transform.SetParent(scene.transform);
                sunObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                var sun = sunObject.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.Soft;

                for (var i = 0; i < 16; i++)
                {
                    var lightObject = new GameObject($"Point{i}");
                    lightObject.transform.SetParent(scene.transform);
                    lightObject.transform.position = new Vector3(i % 4 - 2f, i / 4 - 2f, 5f);
                    var light = lightObject.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.range = 8f;
                    light.shadows = LightShadows.Hard;
                }

                var cameraData = m_Camera.GetUniversalAdditionalCameraData();
                cameraData.renderPostProcessing = true;
                cameraData.renderShadows = true;

                var volumeObject = new GameObject("Volume");
                volumeObject.transform.SetParent(scene.transform);
                var volume = volumeObject.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 1000f;
                profile = BuildPostProcessProfile();
                volume.sharedProfile = profile;

                Assert.That(material.shader, Is.Not.Null, "The Lit shader was not found, so nothing renders.");

                // Observe the frame from inside it. A frame with no visible renderers or lights measures
                // clean while exercising almost none of URP, which is the same vacuous pass the
                // per-pass tests guard against.
                m_ObservedVisibleLights = 0;
                m_Feature.measuredWork = ObserveFrameContents;

                var detector = new AllocationDetector { logWatcher = m_Logs };
                AllocationResult result = null;
                try
                {
                    var frames = detector.RunIncremental(
                        $"URP populated frame, {renderingMode}",
                        Describe($"200 cubes, shadowed directional light, 16 shadowed point lights, bloom, {renderingMode}, render scale {renderScale}, scene override {sceneOverrideMode}"),
                        m_Camera.Render,
                        measured => result = measured);

                    while (frames.MoveNext())
                        yield return null;
                }
                finally
                {
                    m_Feature.measuredWork = null;
                }

                Assert.That(result, Is.Not.Null, "The detector produced no result.");

                Assert.That(m_ObservedVisibleLights, Is.GreaterThan(1),
                    "Culling found at most one light, so the frame did not exercise the additional light "
                    + "paths and a clean result says little.");

                Assert.That(result.passed, Is.True, result.Report());
            }
            finally
            {
                Object.DestroyImmediate(scene);
                DestroyProfile(profile);
                if (material != null)
                    Object.DestroyImmediate(material);

                m_Asset.renderScale = previousRenderScale;
                debugSettings.sceneOverrideMode = previousSceneOverride;
            }
        }

        static void DestroyProfile(VolumeProfile profile)
        {
            if (profile == null)
                return;

            // The components are separate objects owned by the profile, so destroying it is not enough.
            for (var i = profile.components.Count - 1; i >= 0; i--)
                Object.DestroyImmediate(profile.components[i]);

            Object.DestroyImmediate(profile);
        }

        int m_ObservedVisibleLights;

        // Runs inside the measured window, so it must not allocate: field writes and struct reads only.
        void ObserveFrameContents(RenderGraph renderGraph, ContextContainer frameData)
        {
            m_ObservedVisibleLights = frameData.Get<UniversalLightData>().visibleLights.Length;
        }

        static VolumeProfile BuildPostProcessProfile()
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.hideFlags = HideFlags.HideAndDontSave;

            var bloom = profile.Add<Bloom>();
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 1f;

            var tonemapping = profile.Add<Tonemapping>();
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.ACES;

            return profile;
        }

        static Mesh BuildCubeMesh()
        {
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mesh = primitive.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(primitive);
            return mesh;
        }

        /// <summary>
        /// Measures a populated frame with a debug view active. A debug view makes DrawObjectsPass
        /// build debug renderer lists, and DebugHandler.CreateDebugRenderSetups allocates a
        /// DebugRenderSetup per frame while recording.
        /// </summary>
        protected IEnumerator MeasureFrameWithOverdrawDebugView()
        {
            SkipUnlessDebugShadersAreAvailable();
            return MeasurePopulatedFrame(RenderingMode.Forward, 1f, DebugSceneOverrideMode.Overdraw);
        }

        /// <summary>
        /// As <see cref="MeasureFrameWithOverdrawDebugView"/>, but the wireframe modes build two debug
        /// render setups a frame rather than one.
        /// </summary>
        protected IEnumerator MeasureFrameWithSolidWireframeDebugView()
        {
            SkipUnlessDebugShadersAreAvailable();
            return MeasurePopulatedFrame(RenderingMode.Forward, 1f, DebugSceneOverrideMode.SolidWireframe);
        }

        /// <summary>
        /// A build without debug display variants logs a missing variant for every debug draw, and the
        /// logging is what a measurement would then be reporting. Gated on the setting that strips them
        /// rather than on a platform list, so a platform that keeps them stays covered.
        /// </summary>
        static void SkipUnlessDebugShadersAreAvailable()
        {
            // The setting describes what a player build strips. An editor compiles the variant on
            // demand, so a debug view is drawable there whatever it says.
            if (Application.isEditor)
                return;

            if (GraphicsSettings.TryGetRenderPipelineSettings<ShaderStrippingSetting>(out var stripping)
                && stripping.stripRuntimeDebugShaders)
            {
                Assert.Ignore("This build strips runtime debug shaders, so a debug view cannot be drawn "
                    + "here and the measurement would be of the missing-variant logging instead.");
            }
        }

        /// <summary>
        /// Measures <c>BloomPostProcessPass.RecordRenderGraph</c>.
        /// </summary>
        protected void MeasureBloomPass()
        {
            var shaders = FindShaderResources();
            Assert.That(shaders?.bloomPS, Is.Not.Null,
                "No bloom shader was found. URP ships PostProcessData, so this is a lookup failure "
                + "rather than a project without post-processing.");

            // Bloom reads the descriptor of the camera colour texture, which only exists as a
            // graph-managed texture when the frame renders through an intermediate. Without this URP
            // may render straight to the camera target, and whether it does is platform-dependent.
            m_Camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            var stack = volumeStack;
            var bloom = stack.GetComponent<Bloom>();
            bloom.intensity.value = 1f;
            bloom.threshold.value = 0.5f;

            using var pass = new BloomPostProcessPass(shaders.bloomPS)
            {
                volumeStackOverride = stack
            };

            // RecordRenderGraph returns early when the pass is invalid or the effect is inactive, and a
            // no-op measures zero allocations and passes. Bloom publishes its result handle, so observe
            // that to prove the pass actually recorded something.
            var recordedBloom = false;
            var hadCameraColor = false;

            var run = new RenderAllocationRun(
                $"{nameof(BloomPostProcessPass)}.{nameof(BloomPostProcessPass.RecordRenderGraph)}",
                Describe($"Bloom {bloom.filter.value}, {bloom.downscale.value}"),
                (renderGraph, frameData) =>
                {
                    var resourceData = frameData.Get<UniversalResourceData>();

                    // Reading the descriptor of an unmanaged handle throws inside the pass, where the
                    // render graph swallows it and the run stalls with nothing measured. Check instead.
                    if (!resourceData.cameraColor.IsValid())
                        return;

                    hadCameraColor = true;
                    pass.RecordRenderGraph(renderGraph, frameData);
                    recordedBloom |= resourceData.bloom.IsValid();
                });

            Drive(run);

            Assert.That(hadCameraColor, Is.True,
                "The frame had no graph-managed camera colour texture, so Bloom had no source to read. "
                + "The camera is rendering straight to its target instead of through an intermediate.");

            Assert.That(recordedBloom, Is.True,
                "Bloom never published a result texture, so the pass returned early and the measurement "
                + "covered nothing.");

            var result = run.result;
            Assert.That(result.passed, Is.True, result.Report());
        }

        /// <summary>
        /// Renders until the run has warmed up and measured.
        /// </summary>
        void Drive(RenderAllocationRun run)
        {
            run.logWatcher = m_Logs;
            run.Begin();
            m_Feature.measuredWork = run.OnRender;

            // Renders only count when the pass is recorded, so a run that never gets recorded would
            // spin here. Bound it and say so instead.
            var budget = (run.warmUpMaxRenders + run.measuredRenders) * 4;
            try
            {
                while (run.wantsMoreRenders && budget-- > 0)
                    m_Camera.Render();
            }
            finally
            {
                m_Feature.measuredWork = null;
                run.Finish();
            }

            Assert.That(run.wantsMoreRenders, Is.False,
                "The measurement pass was not recorded often enough to finish the run, so it is not "
                + "reaching its injection point and nothing was measured.\n"
                + run.result.loggedMessages);
        }

        string Describe(string effect) =>
            $"{effect}, {k_Width}x{k_Height}, "
            + $"{GraphicsSettings.currentRenderPipeline?.name}/{m_RendererData.name}, "
            + $"{SystemInfo.graphicsDeviceType}, {SystemInfo.renderingThreadingMode}";

        // GraphicsSettings only exposes the shader resources while URP is the pipeline in use, and in a
        // player that lookup resolves from state captured before the pipeline exists. The renderer data
        // asset carries the same resources, so fall back to it.
        static PostProcessData.ShaderResources FindShaderResources()
        {
            if (GraphicsSettings.TryGetRenderPipelineSettings<PostProcessData.ShaderResources>(out var current)
                && current?.bloomPS != null)
            {
                return current;
            }

            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset asset)
                return null;

            foreach (var rendererData in asset.rendererDataList)
            {
                if (rendererData is UniversalRendererData universalRendererData
                    && universalRendererData.postProcessData?.shaders != null)
                {
                    return universalRendererData.postProcessData.shaders;
                }
            }

            return null;
        }

        class TestPassData
        {
            internal TextureHandle target;
        }

        // Unsafe pass: the shape the SurfaceCache runtime tests already prove records and executes, so
        // the only thing this pass puts under test is whether the allocation is detected.
        class AllocatingTestPass : ScriptableRenderPass
        {
            // Kept alive so the deliberate allocation cannot be optimised away.
            internal int[] sink;

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                sink = new int[1];

                var resourceData = frameData.Get<UniversalResourceData>();
                using var builder = renderGraph.AddUnsafePass<TestPassData>("AllocatingTestPass", out var passData);
                passData.target = resourceData.activeColorTexture;
                builder.UseTexture(passData.target, AccessFlags.Read);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((TestPassData data, UnsafeGraphContext context) => { });
            }
        }
    }
}
