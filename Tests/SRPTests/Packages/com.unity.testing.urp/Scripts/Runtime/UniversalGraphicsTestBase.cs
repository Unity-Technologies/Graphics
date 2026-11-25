using System;
using System.Collections;
using System.Collections.Generic;
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
#if UNITY_EDITOR
using UnityEditor.TestTools.Graphics;
#endif

namespace Unity.Rendering.Universal.Tests
{
    // The mode of URP's global constant buffer that a fixture runs.
    public enum GlobalConstantBufferContext
    {
        TransientCB,
        PersistentCB,
    }

    [FailOnConstantBufferPropertyWarnings]
    public class UniversalGraphicsTestBase
    {
        protected readonly GpuResidentDrawerGlobalContext gpuResidentDrawerContext;
        protected readonly GpuResidentDrawerContext requestedGRDContext;
        protected readonly GlobalConstantBufferContext requestedCBContext;
        protected readonly Dictionary<IGPUResidentRenderPipeline, GPUResidentDrawerMode> previousDrawerModes;
        bool m_PreviousUseGlobalConstantBuffer;

        // A fixture that names no mode runs the transient one, so it keeps its name.
        public UniversalGraphicsTestBase(GpuResidentDrawerContext grdContext)
            : this(grdContext, GlobalConstantBufferContext.TransientCB) { }

        public UniversalGraphicsTestBase(GpuResidentDrawerContext grdContext, GlobalConstantBufferContext cbContext)
        {
            requestedGRDContext = grdContext;
            requestedCBContext = cbContext;

            gpuResidentDrawerContext = GlobalContextManager.Get<GpuResidentDrawerGlobalContext>();

            previousDrawerModes = gpuResidentDrawerContext.CaptureDrawerModes();

            gpuResidentDrawerContext.Activate(requestedGRDContext);
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Standard resolution for backbuffer capture is 1080p
            Screen.SetResolution(1920, 1080, true);

            #if UNITY_EDITOR
            GameViewSize.SetGameViewSize(1920, 1080);
            #endif

            SceneManager.LoadScene("GraphicsTestTransitionScene", LoadSceneMode.Single);
        }

        [UnityOneTimeSetUp]
        public IEnumerator OneTimeSetup()
        {
            // URP reads the mode from the setting. Switched before the first test's outer actions, which read it too.
            var cbSettings = GraphicsSettings.GetRenderPipelineSettings<RenderingCapabilitiesSettings>();
            Assert.That(cbSettings, Is.Not.Null, "The fixture runs its mode through the Use Persistent Global Constant Buffer setting.");
            m_PreviousUseGlobalConstantBuffer = cbSettings.useGlobalConstantBuffer;
            cbSettings.useGlobalConstantBuffer = requestedCBContext == GlobalConstantBufferContext.PersistentCB;

            // Necessary because of inconsistencies on some platforms
            yield return GlobalResolutionSetter.SetResolutionWithRetry(1920, 1080, true);

            yield return TestContentLoader.WaitForContentLoadAsync(TimeSpan.FromSeconds(240));
        }

        [SetUp]
        public void SetUpContext()
        {
            gpuResidentDrawerContext.Activate(requestedGRDContext);

            GlobalContextManager.AssertContextIs<GpuResidentDrawerGlobalContext>(requestedGRDContext);
        }

        [TearDown]
        public void TearDown()
        {
            Debug.ClearDeveloperConsole();
#if ENABLE_VR
            XRGraphicsAutomatedTests.running = false;
#endif

            // Ignored tests load no scene, so the drawer state reflects an unrelated quality level
            if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Skipped)
                return;

            Assert.That(
                gpuResidentDrawerContext.IsDrawerCreated,
                Is.EqualTo(requestedGRDContext == GpuResidentDrawerContext.GRDEnabled),
                () =>
                {
                    gpuResidentDrawerContext.IsDrawerSupportedByCurrentPipeline(out var unsupportedReason);
                    return $"GPU Resident Drawer instance did not match the {requestedGRDContext} fixture while the scene rendered." +
                        (string.IsNullOrEmpty(unsupportedReason) ? string.Empty : " " + unsupportedReason);
                }
            );

            Assert.That(
                UniversalRenderPipeline.useGlobalConstantBuffer,
                Is.EqualTo(requestedCBContext == GlobalConstantBufferContext.PersistentCB),
                () => $"URP's global constant buffer did not render in the {requestedCBContext} mode of the fixture."
            );
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            SceneManager.LoadScene("GraphicsTestTransitionScene", LoadSceneMode.Single);

            gpuResidentDrawerContext.RestoreDrawerModes(previousDrawerModes);
            var cbSettings = GraphicsSettings.GetRenderPipelineSettings<RenderingCapabilitiesSettings>();
            if (cbSettings != null)
                cbSettings.useGlobalConstantBuffer = m_PreviousUseGlobalConstantBuffer;
        }
    }

    public class UniversalTestFixtureData
    {
        // Each fixture runs in both modes of URP's global constant buffer, from the same build, except on OpenGL ES 3 outside
        // WebGL and OpenGL Core, which keep loose uniforms. GRDEnabled with PersistentCB is "modern URP".
        public static IEnumerable FixtureParams
        {
            get
            {
                yield return new TestFixtureData(
                    GpuResidentDrawerContext.GRDDisabled
                );

                if (UniversalRenderPipeline.IsGlobalConstantBufferSupported())
                {
                    yield return new TestFixtureData(
                        GpuResidentDrawerContext.GRDDisabled,
                        GlobalConstantBufferContext.PersistentCB
                    );
                }

                if (GraphicsTestPlatform.Current.IsEditorPlatform)
                {
                    yield return new TestFixtureData(
                        GpuResidentDrawerContext.GRDEnabled
                    );

                    if (UniversalRenderPipeline.IsGlobalConstantBufferSupported())
                    {
                        yield return new TestFixtureData(
                            GpuResidentDrawerContext.GRDEnabled,
                            GlobalConstantBufferContext.PersistentCB
                        );
                    }
                }
            }
        }

        // For the fixtures that don't take the GPU Resident Drawer contexts, like URP 2D's.
        public static IEnumerable GlobalConstantBufferFixtureParams
        {
            get
            {
                yield return new TestFixtureData();

                if (UniversalRenderPipeline.IsGlobalConstantBufferSupported())
                    yield return new TestFixtureData(GlobalConstantBufferContext.PersistentCB);
            }
        }
    }
}
