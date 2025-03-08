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

        public UniversalGraphicsTestBase()
        {
            // Register context
            renderGraphContext =
                GlobalContextManager.RegisterGlobalContext(typeof(RenderGraphGlobalContext))
                as RenderGraphGlobalContext;

            requestedRGContext = RenderGraphGraphicsAutomatedTests.enabled
                ? RenderGraphContext.RenderGraphEnabled
                : RenderGraphContext.RenderGraphCompatibility;

            GraphicsTestLogger.Log(
                $"RenderGraphGlobalContext registered with context {requestedRGContext}"
            );
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

            Assert.That(
                GlobalContextManager.GetGlobalContext<RenderGraphGlobalContext>()?.Context,
                Is.EqualTo((int)requestedRGContext),
                $"Setup: Expected {requestedRGContext} but was {(RenderGraphContext)GlobalContextManager.GetGlobalContext<RenderGraphGlobalContext>()?.Context}"
            );
        }

        [TearDown]
        public void TearDown()
        {
            Assert.That(
                GlobalContextManager.GetGlobalContext<RenderGraphGlobalContext>()?.Context,
                Is.EqualTo((int)requestedRGContext),
                $"Teardown: Expected {requestedRGContext} but was {(RenderGraphContext)GlobalContextManager.GetGlobalContext<RenderGraphGlobalContext>()?.Context}"
            );

            Debug.ClearDeveloperConsole();
#if ENABLE_VR
            XRGraphicsAutomatedTests.running = false;
#endif
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            GlobalContextManager.UnregisterGlobalContext(typeof(RenderGraphGlobalContext));
        }
    }
}
