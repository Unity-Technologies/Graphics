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
using UnityEngine.TestTools.Graphics.Platforms;
using Object = UnityEngine.Object;
using UnityEngine.TestTools.Graphics.Contexts;
#if OCULUS_SDK || OPENXR_SDK
using UnityEngine.XR;
#endif

namespace Unity.Rendering.Universal.Tests
{
    [TestFixture(RenderGraphContext.CompatibilityMode)]
    [TestFixture(RenderGraphContext.RenderGraphMode)]
    public class UniversalGraphicsTestBase
    {
        protected readonly RenderGraphGlobalContext renderGraphContext;
        protected readonly RenderGraphContext requestedRGContext;
        protected readonly RenderGraphContext previousRGContext;

        protected readonly GpuResidentDrawerGlobalContext gpuResidentDrawerContext;
        protected readonly GpuResidentDrawerContext requestedGRDContext;
        protected readonly GpuResidentDrawerContext previousGRDContext;

        protected readonly StereoRenderingGlobalContext stereoRenderingContext;
        protected readonly StereoRenderingContext requestedXRContext;
        protected readonly StereoRenderingContext previousXRContext;

        public UniversalGraphicsTestBase(RenderGraphContext rgContext)
            : this(rgContext, GpuResidentDrawerContext.None, StereoRenderingContext.None)
        {
            requestedGRDContext = previousGRDContext;
            requestedXRContext = previousXRContext;

            GraphicsTestLogger.DebugLog($"RenderGraphContext: {requestedRGContext}");
            GraphicsTestLogger.DebugLog($"GpuResidentDrawerContext: {requestedGRDContext}");
            GraphicsTestLogger.DebugLog($"StereoRenderingContext: {requestedXRContext}");
        }

        public UniversalGraphicsTestBase(
            RenderGraphContext rgContext,
            GpuResidentDrawerContext grdContext,
            StereoRenderingContext xrContext
        )
        {
            requestedRGContext = rgContext;
            requestedGRDContext = grdContext;
            requestedXRContext = xrContext;

            // Register context
            renderGraphContext =
                GlobalContextManager.RegisterGlobalContext(typeof(RenderGraphGlobalContext))
                as RenderGraphGlobalContext;

            gpuResidentDrawerContext =
                GlobalContextManager.RegisterGlobalContext(typeof(GpuResidentDrawerGlobalContext))
                as GpuResidentDrawerGlobalContext;

            stereoRenderingContext =
                GlobalContextManager.RegisterGlobalContext(typeof(StereoRenderingGlobalContext))
                as StereoRenderingGlobalContext;

            // Cache previous state to avoid state leak
            previousRGContext = (RenderGraphContext)renderGraphContext.Context;
            previousGRDContext = (GpuResidentDrawerContext)gpuResidentDrawerContext.Context;
            previousXRContext = (StereoRenderingContext)stereoRenderingContext.Context;

            // Activate new context
            renderGraphContext.ActivateContext(requestedRGContext);
            gpuResidentDrawerContext.ActivateContext(requestedGRDContext);
            stereoRenderingContext.ActivateContext(requestedXRContext);
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            SceneManager.LoadScene("GraphicsTestTransitionScene", LoadSceneMode.Single);
        }

        [UnitySetUp]
        public IEnumerator OneTimeSetup()
        {
            yield return TestContentLoader.WaitForContentLoadAsync(TimeSpan.FromSeconds(30));
        }

        [SetUp]
        public void SetUpContext()
        {
            renderGraphContext.ActivateContext(requestedRGContext);
            gpuResidentDrawerContext.ActivateContext(requestedGRDContext);
            stereoRenderingContext.ActivateContext(requestedXRContext);

            Assert.That(
                GlobalContextManager.GetGlobalContext<RenderGraphGlobalContext>()?.Context,
                Is.EqualTo((int)requestedRGContext),
                $"Expected {requestedRGContext} but was {(RenderGraphContext)GlobalContextManager.GetGlobalContext<RenderGraphGlobalContext>()?.Context}"
            );

            Assert.That(
                GlobalContextManager.GetGlobalContext<GpuResidentDrawerGlobalContext>()?.Context,
                Is.EqualTo((int)requestedGRDContext),
                $"Expected {requestedGRDContext} but was {(GpuResidentDrawerContext)GlobalContextManager.GetGlobalContext<GpuResidentDrawerGlobalContext>()?.Context}"
            );

            Assert.That(
                GlobalContextManager.GetGlobalContext<StereoRenderingGlobalContext>()?.Context,
                Is.EqualTo((int)requestedXRContext),
                $"Expected {requestedXRContext} but was {(StereoRenderingContext)GlobalContextManager.GetGlobalContext<StereoRenderingGlobalContext>()?.Context}"            );
        }

        [TearDown]
        public void TearDown()
        {
            Assert.That(
                GlobalContextManager.GetGlobalContext<RenderGraphGlobalContext>()?.Context,
                Is.EqualTo((int)requestedRGContext),
                $"Expected {requestedRGContext} but was {(RenderGraphContext)GlobalContextManager.GetGlobalContext<RenderGraphGlobalContext>()?.Context}"
            );

            Assert.That(
                GlobalContextManager.GetGlobalContext<StereoRenderingGlobalContext>()?.Context,
                Is.EqualTo((int)requestedXRContext),
                $"Expected {requestedXRContext} but was {(StereoRenderingContext)GlobalContextManager.GetGlobalContext<StereoRenderingGlobalContext>()?.Context}"
            );

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
            stereoRenderingContext.ActivateContext(previousXRContext);
            
            GlobalContextManager.UnregisterGlobalContext(typeof(RenderGraphGlobalContext));
            GlobalContextManager.UnregisterGlobalContext(typeof(GpuResidentDrawerGlobalContext));
            GlobalContextManager.UnregisterGlobalContext(typeof(StereoRenderingGlobalContext));
        }
    }
}
