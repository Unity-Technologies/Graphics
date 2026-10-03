using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Contexts;
using UnityEngine.TestTools.Graphics.Platforms;
using Object = UnityEngine.Object;
#if OCULUS_SDK || OPENXR_SDK
using UnityEngine.XR;
#endif

namespace Unity.Rendering.Universal.Tests
{
    public class UniversalGraphicsTestBase
    {
        protected readonly RenderGraphGlobalContext renderGraphContext;
        protected readonly RenderGraphContext requestedRGContext;
        protected readonly RenderGraphContext previousRGContext;

        protected readonly GpuResidentDrawerGlobalContext gpuResidentDrawerContext;
        protected readonly GpuResidentDrawerContext requestedGRDContext;
        protected readonly GpuResidentDrawerContext previousGRDContext;

        public UniversalGraphicsTestBase(RenderGraphContext rgContext)
            : this(rgContext, GpuResidentDrawerContext.None)
        {
            requestedGRDContext = previousGRDContext;

            GraphicsTestLogger.DebugLog($"RenderGraphContext: {requestedRGContext}");
            GraphicsTestLogger.DebugLog($"GpuResidentDrawerContext: {requestedGRDContext}");
        }

        public UniversalGraphicsTestBase(
            RenderGraphContext rgContext,
            GpuResidentDrawerContext grdContext
        )
        {
            requestedRGContext = rgContext;
            requestedGRDContext = grdContext;

            // Register context
            renderGraphContext =
                GlobalContextManager.RegisterGlobalContext(typeof(RenderGraphGlobalContext))
                as RenderGraphGlobalContext;

            gpuResidentDrawerContext =
                GlobalContextManager.RegisterGlobalContext(typeof(GpuResidentDrawerGlobalContext))
                as GpuResidentDrawerGlobalContext;

            // Cache previous state to avoid state leak
            previousRGContext = (RenderGraphContext)renderGraphContext.Context;
            previousGRDContext = (GpuResidentDrawerContext)gpuResidentDrawerContext.Context;

            // Activate new context
            renderGraphContext.ActivateContext(requestedRGContext);
            gpuResidentDrawerContext.ActivateContext(requestedGRDContext);
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            SceneManager.LoadScene("GraphicsTestTransitionScene", LoadSceneMode.Single);
        }

        [UnityOneTimeSetUp]
        public IEnumerator OneTimeSetup()
        {
            // Necessary because of inconsistencies on some platforms
            yield return SetResolutionWithRetry(1920, 1080, true);

            yield return TestContentLoader.WaitForContentLoadAsync(TimeSpan.FromSeconds(240));
        }

        static int s_SettledWidth;
        static int s_SettledHeight;

        // Copy of GlobalResolutionSetter.SetResolutionWithRetry from com.unity.testframework.graphics,
        // which local.gtf.references pins to 9.0.0-exp.43 here - that predates the retry, and moving the
        // pin far enough to get it also takes the 9.0.0-exp.57 API breaks. Keep this in step with the
        // framework and delete it once the pin moves. UUM-150918

        /// <summary>
        /// Sets the screen resolution with retry and verification logic.
        /// </summary>
        /// <param name="width">Target width</param>
        /// <param name="height">Target height</param>
        /// <param name="fullscreen">Fullscreen mode</param>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        /// <returns>
        /// Coroutine enumerator
        /// </returns>
        public static IEnumerator SetResolutionWithRetry(int width, int height, bool fullscreen, int maxRetries = 3)
        {
            if (Application.isEditor)
            {
                GraphicsTestLogger.Log("Running test in Editor, skipping Player resolution change");
                yield break;
            }

#if UNITY_WEBGL
            GraphicsTestLogger.Log("Running test on WebGL platform, skipping Player resolution change");
            yield break;
#else
            // Check if resolution is already correct
            if (Screen.width == width && Screen.height == height)
            {
                if (s_SettledWidth == width && s_SettledHeight == height)
                {
                    GraphicsTestLogger.Log($"Resolution already set to {width}x{height}, skipping resolution change entirely");
                    yield break;
                }

                // Applied elsewhere (e.g. plain Screen.SetResolution in OneTimeSetUp); may still be in flight.
                GraphicsTestLogger.Log($"Resolution already reports {width}x{height}, waiting for it to settle");
                yield return WaitForModeSwitchToSettle(Application.platform, () => Time.realtimeSinceStartup);
                s_SettledWidth = width;
                s_SettledHeight = height;
                yield break;
            }

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                int framesToWait = attempt switch
                {
                    1 => 60,
                    2 => 600,
                    _ => 6000
                };

                GraphicsTestLogger.Log(
                    $"Attempting to set resolution to {width}x{height} " +
                    $"(Attempt {attempt}/{maxRetries}, waiting up to {framesToWait} frame(s) for it to apply)");

                Screen.SetResolution(width, height, fullscreen);

                for (int frame = 0; frame <= framesToWait; frame++)
                {
                    if (Screen.width == width && Screen.height == height)
                    {
                        GraphicsTestLogger.Log(
                            $"Resolution successfully set to {width}x{height} " +
                            $"on attempt {attempt} after {frame} frame(s)");
                        yield return WaitForModeSwitchToSettle(Application.platform, () => Time.realtimeSinceStartup);
                        s_SettledWidth = width;
                        s_SettledHeight = height;
                        yield break;
                    }

                    yield return null;
                }

                GraphicsTestLogger.Log(
                    $"Resolution not applied after attempt {attempt}. " +
                    $"Current: {Screen.width}x{Screen.height}, Expected: {width}x{height}");
            }

            throw new InvalidOperationException(
                $"Failed to set resolution to {width}x{height} after {maxRetries} attempts. " +
                $"Current resolution: {Screen.width}x{Screen.height}. " +
                "Graphics tests require this resolution for valid image comparison."
            );
#endif
        }

        internal static IEnumerator WaitForModeSwitchToSettle(RuntimePlatform platform, Func<float> now)
        {
            const float settleSeconds = 5f;

            // macOS only: the fullscreen transition disrupts a frame well after Screen.width reports the
            // new size, failing the first back buffer capture of a run. No other platform has shown this,
            // and holding everywhere would cost every player run 5 seconds.
            if (platform != RuntimePlatform.OSXPlayer)
                yield break;

            float start = now();
            while (now() - start < settleSeconds)
                yield return null;
        }

        [SetUp]
        public void SetUpContext()
        {
            renderGraphContext.ActivateContext(requestedRGContext);
            gpuResidentDrawerContext.ActivateContext(requestedGRDContext);

            GlobalContextManager.AssertContextIs<RenderGraphGlobalContext, RenderGraphContext>(requestedRGContext);
            GlobalContextManager.AssertContextIs<GpuResidentDrawerGlobalContext, GpuResidentDrawerContext>(requestedGRDContext);
        }

        [TearDown]
        public void TearDown()
        {
            GlobalContextManager.AssertContextIs<RenderGraphGlobalContext, RenderGraphContext>(requestedRGContext);

            Debug.ClearDeveloperConsole();
#if ENABLE_VR
            XRGraphicsAutomatedTests.running = false;
#endif
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            SceneManager.LoadScene("GraphicsTestTransitionScene", LoadSceneMode.Single);

            renderGraphContext.ActivateContext(previousRGContext);
            gpuResidentDrawerContext.ActivateContext(previousGRDContext);

            GlobalContextManager.UnregisterGlobalContext(typeof(RenderGraphGlobalContext));
            GlobalContextManager.UnregisterGlobalContext(typeof(GpuResidentDrawerGlobalContext));
        }
    }

    public class UniversalTestFixtureData
    {
        public static IEnumerable FixtureParams
        {
            get
            {
                if (RuntimeSettings.urpCompatibilityMode)
                {
                    yield return new TestFixtureData(
                        RenderGraphContext.CompatibilityMode,
                        GpuResidentDrawerContext.GpuResidentDrawerDisabled
                    );
                }

                yield return new TestFixtureData(
                    RenderGraphContext.RenderGraphMode,
                    GpuResidentDrawerContext.GpuResidentDrawerDisabled
                );

                if (GraphicsTestPlatform.Current.IsEditorPlatform)
                {
                    if (RuntimeSettings.urpCompatibilityMode)
                    {
                        yield return new TestFixtureData(
                            RenderGraphContext.CompatibilityMode,
                            GpuResidentDrawerContext.GpuResidentDrawerInstancedDrawing
                        );
                    }

                    yield return new TestFixtureData(
                        RenderGraphContext.RenderGraphMode,
                        GpuResidentDrawerContext.GpuResidentDrawerInstancedDrawing
                    );
                }
            }
        }
    }
}
