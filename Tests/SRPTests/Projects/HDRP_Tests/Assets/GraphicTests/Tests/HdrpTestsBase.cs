using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Contexts;

#if UNITY_EDITOR
using UnityEditor.TestTools.Graphics;
#endif

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    public abstract class HdrpTestsBase
    {
        private GpuResidentDrawerGlobalContext gpuResidentDrawerContext;
        private readonly GpuResidentDrawerContext requestedGRDContext;
        private GpuResidentDrawerContext? previousGRDContext;

        private GpuOcclusionCullingGlobalContext gpuOcclusionContext;
        private readonly GpuOcclusionCullingContext requestedOccContext;
        private GpuOcclusionCullingContext? previousOccContext;

        protected HdrpTestsBase(GpuResidentDrawerContext grdContext)
        {
            requestedGRDContext = grdContext;
            requestedOccContext = GpuOcclusionCullingGlobalContext.ChooseContext(grdContext);
        }

        [OneTimeSetUp]
        public virtual void OneTimeSetUp()
        {
            gpuResidentDrawerContext = GlobalContextManager.Get<GpuResidentDrawerGlobalContext>();
            gpuOcclusionContext = GlobalContextManager.Get<GpuOcclusionCullingGlobalContext>();

            if (gpuResidentDrawerContext == null || gpuOcclusionContext == null)
                throw new InvalidOperationException("Failed to register GRD or GpuOcclusion global context.");

            previousGRDContext = (GpuResidentDrawerContext?)gpuResidentDrawerContext.Current;
            previousOccContext = (GpuOcclusionCullingContext?)gpuOcclusionContext.Current;

            gpuResidentDrawerContext.Activate(requestedGRDContext);
            gpuOcclusionContext.Activate(requestedOccContext);

            SceneManager.LoadScene("GraphicsTestTransitionScene", LoadSceneMode.Single);

            #if UNITY_EDITOR
            GameViewSize.SetGameViewSize(1920, 1080);
            #endif
        }

        [UnityOneTimeSetUp]
        public virtual IEnumerator UnityOneTimeSetUp()
        {
            yield return GlobalResolutionSetter.SetResolutionWithRetry(1920, 1080, true);
        }

        [SetUp]
        public virtual void SetUpContext()
        {
            if (gpuResidentDrawerContext == null || gpuOcclusionContext == null)
                return;

            gpuResidentDrawerContext.Activate(requestedGRDContext);
            gpuOcclusionContext.Activate(requestedOccContext);

            GlobalContextManager.AssertContextIs<GpuResidentDrawerGlobalContext>(requestedGRDContext);
            GlobalContextManager.AssertContextIs<GpuOcclusionCullingGlobalContext>(requestedOccContext);
        }

        [TearDown]
        public virtual void TearDown()
        {
            Debug.ClearDeveloperConsole();

            #if UNITY_EDITOR
            ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
            #endif
        }

        [OneTimeTearDown]
        public virtual void OneTimeTearDown()
        {
            try
            {
                SceneManager.LoadScene("GraphicsTestTransitionScene", LoadSceneMode.Single);
            }
            finally
            {
                if (gpuResidentDrawerContext != null && previousGRDContext.HasValue)
                {
                    gpuResidentDrawerContext.Activate(previousGRDContext.Value);
                }

                if (gpuOcclusionContext != null && previousOccContext.HasValue)
                {
                    gpuOcclusionContext.Activate(previousOccContext.Value);
                }
            }
        }

#if ENABLE_VR
        [TearDown]
        public void TearDownXR()
        {
            XRGraphicsAutomatedTests.running = false;
        }
#endif
    }
}
