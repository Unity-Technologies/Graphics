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
            yield return TestContentLoader.WaitForContentLoadAsync(TimeSpan.FromSeconds(30));
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
                yield return new TestFixtureData(
                    RenderGraphContext.CompatibilityMode,
                    GpuResidentDrawerContext.GpuResidentDrawerDisabled
                );

                yield return new TestFixtureData(
                    RenderGraphContext.RenderGraphMode,
                    GpuResidentDrawerContext.GpuResidentDrawerDisabled
                );

                if (GraphicsTestPlatform.Current.IsEditorPlatform)
                {
                    yield return new TestFixtureData(
                        RenderGraphContext.CompatibilityMode,
                        GpuResidentDrawerContext.GpuResidentDrawerInstancedDrawing
                    );

                    yield return new TestFixtureData(
                        RenderGraphContext.RenderGraphMode,
                        GpuResidentDrawerContext.GpuResidentDrawerInstancedDrawing
                    );
                }
            }
        }
    }
}
