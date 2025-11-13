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
        protected readonly GpuResidentDrawerGlobalContext gpuResidentDrawerContext;
        protected readonly GpuResidentDrawerContext requestedGRDContext;
        protected readonly GpuResidentDrawerContext previousGRDContext;

        public UniversalGraphicsTestBase(GpuResidentDrawerContext grdContext)
        {
            requestedGRDContext = grdContext;

            gpuResidentDrawerContext =
                GlobalContextManager.RegisterGlobalContext(typeof(GpuResidentDrawerGlobalContext))
                as GpuResidentDrawerGlobalContext;

            previousGRDContext = (GpuResidentDrawerContext)gpuResidentDrawerContext.Context;

            // Activate new context
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
            yield return TestContentLoader.WaitForContentLoadAsync(TimeSpan.FromSeconds(240));
        }

        [SetUp]
        public void SetUpContext()
        {
            gpuResidentDrawerContext.ActivateContext(requestedGRDContext);

            GlobalContextManager.AssertContextIs<GpuResidentDrawerGlobalContext, GpuResidentDrawerContext>(requestedGRDContext);
        }

        [TearDown]
        public void TearDown()
        {
            Debug.ClearDeveloperConsole();
#if ENABLE_VR
            XRGraphicsAutomatedTests.running = false;
#endif
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            SceneManager.LoadScene("GraphicsTestTransitionScene", LoadSceneMode.Single);

            gpuResidentDrawerContext.ActivateContext(previousGRDContext);

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
                    GpuResidentDrawerContext.GRDDisabled
                );

                if (GraphicsTestPlatform.Current.IsEditorPlatform)
                {
                    yield return new TestFixtureData(
                        GpuResidentDrawerContext.GRDEnabled
                    );
                }
            }
        }
    }
}
