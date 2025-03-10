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
    [TestFixture(RenderGraphContext.CompatibilityMode)]
    [TestFixture(RenderGraphContext.RenderGraphMode)]
    public class UniversalGraphicsTestBase
    {
        protected readonly RenderGraphGlobalContext renderGraphContext;
        protected readonly RenderGraphContext requestedRGContext;
        protected readonly RenderGraphContext previousRGContext;

        public UniversalGraphicsTestBase(RenderGraphContext rgContext)
        {
            requestedRGContext = rgContext;

            // Register context
            renderGraphContext =
                GlobalContextManager.RegisterGlobalContext(typeof(RenderGraphGlobalContext))
                as RenderGraphGlobalContext;

            // Cache previous state to avoid state leak
            previousRGContext = (RenderGraphContext)renderGraphContext.Context;

            // Activate new context
            renderGraphContext.ActivateContext(requestedRGContext);
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
            renderGraphContext.ActivateContext(previousRGContext);
            GlobalContextManager.UnregisterGlobalContext(typeof(RenderGraphGlobalContext));
            SceneManager.LoadScene("GraphicsTestTransitionScene", LoadSceneMode.Single);
        }
    }
}
