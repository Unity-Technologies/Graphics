using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    class ObsoleteShadowPropertiesTests
    {
        UniversalRenderPipelineAsset m_Asset;
        bool m_InitializedVolumeManager;
        VolumeStack m_PreviousStack;
        VolumeStack m_Stack;
        ShadowsVolumeOverride m_Shadows;
        int m_SetterWarningCount;

        [SetUp]
        public void SetUp()
        {
            m_Asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            m_SetterWarningCount = 0;
            Application.logMessageReceived += CountSetterWarnings;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= CountSetterWarnings;

            if (m_Stack != null)
            {
                VolumeManager.instance.stack = m_PreviousStack;
                VolumeManager.instance.DestroyStack(m_Stack);
                m_Stack = null;
            }

            if (m_InitializedVolumeManager)
            {
                VolumeManager.instance.Deinitialize();
                m_InitializedVolumeManager = false;
            }

            Object.DestroyImmediate(m_Asset);
        }

        void UseTestVolumeStack()
        {
            if (!VolumeManager.instance.isInitialized)
            {
                VolumeManager.instance.baseComponentTypeArray = new[] { typeof(ShadowsVolumeOverride) };
                VolumeManager.instance.InitializeInternal();
                m_InitializedVolumeManager = true;
            }

            m_PreviousStack = VolumeManager.instance.stack;
            m_Stack = VolumeManager.instance.CreateStack();
            VolumeManager.instance.stack = m_Stack;

            m_Shadows = m_Stack.GetComponent<ShadowsVolumeOverride>();
            if (m_Shadows == null)
                Assert.Ignore("The volume manager was initialized by a pipeline without the Shadows volume override");
            m_Shadows.maxShadowDistance.value = 33f;
            m_Shadows.cascadeCount.value = 3;
            m_Shadows.cascadeSplit0.value = 0.125f;
            m_Shadows.cascadeSplit1.value = 0.25f;
            m_Shadows.cascadeSplit2.value = 0.5f;
            m_Shadows.lastCascadeBorder.value = 0.375f;
        }

        void CountSetterWarnings(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Warning && condition.Contains("ShadowsVolumeOverride"))
                m_SetterWarningCount++;
        }

#pragma warning disable 618 // Obsolete warning
        [Test]
        [Description("The obsolete shadow properties of the URP asset return the values of the Shadows volume override in the current volume stack.")]
        public void Getters_ReturnTheCurrentVolumeStackValues()
        {
            UseTestVolumeStack();

            Assert.AreEqual(33f, m_Asset.shadowDistance);
            Assert.AreEqual(3, m_Asset.shadowCascadeCount);
            Assert.AreEqual(0.125f, m_Asset.cascade2Split);
            Assert.AreEqual(new Vector2(0.125f, 0.25f), m_Asset.cascade3Split);
            Assert.AreEqual(new Vector3(0.125f, 0.25f, 0.5f), m_Asset.cascade4Split);
            Assert.AreEqual(0.375f, m_Asset.cascadeBorder);
        }

        [Test]
        [Description("Before the volume manager is initialized, the obsolete shadow properties return the values serialized in the URP asset.")]
        public void Getters_WithoutVolumeManager_ReturnTheSerializedValues()
        {
            if (VolumeManager.instance.isInitialized)
                Assert.Ignore("The volume manager is already initialized by the active render pipeline");

            m_Asset.m_ObsoleteShadowDistance = 20f;
            m_Asset.m_ObsoleteShadowCascadeCount = 2;
            m_Asset.m_ObsoleteCascade2Split = 0.3f;
            m_Asset.m_ObsoleteCascade3Split = new Vector2(0.2f, 0.4f);
            m_Asset.m_ObsoleteCascade4Split = new Vector3(0.1f, 0.2f, 0.4f);
            m_Asset.m_ObsoleteCascadeBorder = 0.15f;

            Assert.AreEqual(20f, m_Asset.shadowDistance);
            Assert.AreEqual(2, m_Asset.shadowCascadeCount);
            Assert.AreEqual(0.3f, m_Asset.cascade2Split);
            Assert.AreEqual(new Vector2(0.2f, 0.4f), m_Asset.cascade3Split);
            Assert.AreEqual(new Vector3(0.1f, 0.2f, 0.4f), m_Asset.cascade4Split);
            Assert.AreEqual(0.15f, m_Asset.cascadeBorder);
        }

        [Test]
        [Description("Setting an obsolete shadow property of the URP asset changes nothing and warns at most once per session.")]
        public void Setters_HaveNoEffectAndWarnAtMostOnce()
        {
            UseTestVolumeStack();

            m_Asset.shadowDistance = 1f;
            m_Asset.shadowCascadeCount = 4;
            m_Asset.cascade2Split = 0.9f;
            m_Asset.cascade3Split = new Vector2(0.8f, 0.9f);
            m_Asset.cascade4Split = new Vector3(0.7f, 0.8f, 0.9f);
            m_Asset.cascadeBorder = 0.9f;

            Assert.AreEqual(33f, m_Asset.shadowDistance);
            Assert.AreEqual(3, m_Asset.shadowCascadeCount);
            Assert.AreEqual(new Vector3(0.125f, 0.25f, 0.5f), m_Asset.cascade4Split);
            Assert.AreEqual(0.375f, m_Asset.cascadeBorder);
            Assert.AreEqual(50f, m_Asset.m_ObsoleteShadowDistance);
            Assert.LessOrEqual(m_SetterWarningCount, 1);

            m_SetterWarningCount = 0;
            m_Asset.shadowDistance = 2f;
            Assert.AreEqual(0, m_SetterWarningCount);
        }
#pragma warning restore 618 // Obsolete warning
    }
}
