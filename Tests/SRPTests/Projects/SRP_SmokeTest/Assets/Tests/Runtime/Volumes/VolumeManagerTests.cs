using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    [TestFixture("Local")]
    [TestFixture("Global")]
    class VolumeManagerTests
    {
        readonly LayerMask k_defaultLayer = 1;
        VolumeProfile m_VolumeProfile;
        readonly List<GameObject> m_Objects = new();
        readonly bool m_IsGlobal;

        VolumeManager volumeManager { get; set; }
        VolumeStack stack => volumeManager.stack;
        GameObject camera { get; set; }

        public VolumeManagerTests(string volumeType)
        {
            m_IsGlobal = volumeType switch
            {
                "Global" => true,
                "Local" => false,
                _ => throw new ArgumentException(volumeType)
            };
        }

        [SetUp]
        public void Setup()
        {
            m_VolumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();

            var volumeComponent = m_VolumeProfile.Add<TestVolume>();
            volumeComponent.param.Override(TestVolume.k_OverrideValue);

            volumeManager = new VolumeManager();
            volumeManager.Initialize();
            camera = new GameObject("Camera", typeof(Camera));
            m_Objects.Add(camera);
        }

        [TearDown]
        public void TearDown()
        {
            CoreUtils.Destroy(m_VolumeProfile);

            foreach (var go in m_Objects)
                CoreUtils.Destroy(go);

            volumeManager.Deinitialize();
        }

        static IEnumerable TestCaseSources()
        {
            yield return new TestCaseData(
                    new Action<VolumeManager, Volume>((vm, v) => vm.Unregister(v)),
                    new Action<VolumeManager, Volume>((vm, v) => vm.Register(v)))
                .SetName("Parameter evaluation is correct when volume is unregistered and registered");

            yield return new TestCaseData(
                    new Action<VolumeManager, Volume>((vm, v) => v.enabled = false),
                    new Action<VolumeManager, Volume>((vm, v) => v.enabled = true))
                .SetName("Parameter evaluation is correct when volume is disabled and enabled");

            yield return new TestCaseData(
                    new Action<VolumeManager, Volume>((vm, v) => v.profileRef.components[0].SetAllOverridesTo(false)),
                    new Action<VolumeManager, Volume>((vm, v) => v.profileRef.components[0].SetAllOverridesTo(true)))
                .SetName("Parameter evaluation is correct when overrides are disabled and enabled");
        }

        Volume CreateVolume(string name)
        {
            var volumeGameObject = new GameObject(name, typeof(Volume));
            if (!m_IsGlobal)
                volumeGameObject.AddComponent<BoxCollider>();
            var volume = volumeGameObject.GetComponent<Volume>();
            volume.isGlobal = m_IsGlobal;

            m_Objects.Add(volume.gameObject);

            return volume;
        }

        [TestCaseSource(nameof(TestCaseSources))]
        public void ParameterEvaluationTest(Action<VolumeManager, Volume> disableAction, Action<VolumeManager, Volume> enableAction)
        {
            var volume = CreateVolume("Volume");
            volume.sharedProfile = m_VolumeProfile;
            volumeManager.Register(volume);

            volumeManager.Update(camera.transform, k_defaultLayer);
            Assert.AreEqual(true, stack.requiresReset); // Local volume present - stack reset needed
            Assert.AreEqual(TestVolume.k_OverrideValue, stack.GetComponent<TestVolume>().param.value);

            disableAction.Invoke(volumeManager, volume);
            volumeManager.Update(camera.transform, k_defaultLayer);

            Assert.AreEqual(TestVolume.k_DefaultValue, stack.GetComponent<TestVolume>().param.value); // Value still resets to default

            enableAction.Invoke(volumeManager, volume);
            volumeManager.Update(camera.transform, k_defaultLayer);

            Assert.AreEqual(true, stack.requiresReset); // Local volume is back - stack reset needed
            Assert.AreEqual(TestVolume.k_OverrideValue, stack.GetComponent<TestVolume>().param.value); // Value overridden again
        }

        [Test]
        public void ParameterOverrideTest()
        {
            var volume = CreateVolume("Volume");
            volume.priority = 0f;
            volume.sharedProfile = m_VolumeProfile;
            volumeManager.Register(volume);

            volumeManager.Update(camera.transform, k_defaultLayer);
            Assert.AreEqual(TestVolume.k_OverrideValue, stack.GetComponent<TestVolume>().param.value);

            const float PriorityOverrideValue = 999.0f;
            var priorityVolumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            var priorityVolumeComponent = priorityVolumeProfile.Add<TestVolume>();
            priorityVolumeComponent.param.Override(PriorityOverrideValue);

            var volume1 = CreateVolume("Volume Priority 1");
            volume1.priority = 1f;
            volume1.sharedProfile = priorityVolumeProfile;
            volumeManager.Register(volume1);

            volumeManager.Update(camera.transform, k_defaultLayer);
            Assert.AreEqual(PriorityOverrideValue, stack.GetComponent<TestVolume>().param.value);

            volume.priority = 2f; // Raise priority of the original volume to be higher
            volumeManager.SetLayerDirty(volume.gameObject.layer); // Mark dirty to apply new priority (normally done by Volume.Update())

            volumeManager.Update(camera.transform, k_defaultLayer);
            Assert.AreEqual(TestVolume.k_OverrideValue, stack.GetComponent<TestVolume>().param.value);

            CoreUtils.Destroy(priorityVolumeProfile);
        }
    }

    class VolumeManagerDefaultProfileTests
    {
        VolumeProfile m_VolumeProfile1;
        VolumeProfile m_VolumeProfile2;
        VolumeProfile m_VolumeProfile3;
        VolumeManager volumeManager { get; set; }
        VolumeStack stack => volumeManager.stack;
        TestVolume GetDefaultState() => volumeManager.GetVolumeComponentDefaultState(typeof(TestVolume)) as TestVolume;

        [SetUp]
        public void Setup()
        {
            m_VolumeProfile1 = ScriptableObject.CreateInstance<VolumeProfile>();
            m_VolumeProfile1.Add<TestVolume>().param.Override(TestVolume.k_OverrideValue);

            m_VolumeProfile2 = ScriptableObject.CreateInstance<VolumeProfile>();
            m_VolumeProfile2.Add<TestVolume>().param.Override(TestVolume.k_OverrideValue2);

            m_VolumeProfile3 = ScriptableObject.CreateInstance<VolumeProfile>();
            m_VolumeProfile3.Add<TestVolume>().param.Override(TestVolume.k_OverrideValue3);
        }

        [TearDown]
        public void TearDown()
        {
            CoreUtils.Destroy(m_VolumeProfile1);
            CoreUtils.Destroy(m_VolumeProfile2);
            CoreUtils.Destroy(m_VolumeProfile3);
            volumeManager.Deinitialize();
        }

        [Test]
        public void ParameterIsCorrectByDefault()
        {
            volumeManager = new VolumeManager();
            volumeManager.Initialize();

            LayerMask defaultLayer = 1;
            var camera = new GameObject("Camera", typeof(Camera));
            Assert.AreEqual(true, stack.requiresReset); // Initially, reset is required
            Assert.AreEqual(TestVolume.k_DefaultValue, stack.GetComponent<TestVolume>().param.value); // Default value retrievable without calling Update()
            volumeManager.Update(camera.transform, defaultLayer);
            Assert.AreEqual(false, stack.requiresReset); // No volumes - no stack reset needed
            Assert.AreEqual(TestVolume.k_DefaultValue, stack.GetComponent<TestVolume>().param.value);
        }

        [Test]
        public void DefaultProfilesAreAppliedToDefaultState()
        {
            volumeManager = new VolumeManager();
            volumeManager.Initialize(m_VolumeProfile1, null);

            Assert.AreEqual(TestVolume.k_OverrideValue, GetDefaultState().param.value);

            volumeManager.SetQualityDefaultProfile(m_VolumeProfile2);
            Assert.AreEqual(TestVolume.k_OverrideValue2, GetDefaultState().param.value);

            volumeManager.SetCustomDefaultProfiles(new List<VolumeProfile> { m_VolumeProfile3 });
            Assert.AreEqual(TestVolume.k_OverrideValue3, GetDefaultState().param.value);

            volumeManager.SetGlobalDefaultProfile(null);
            Assert.AreEqual(TestVolume.k_OverrideValue3, GetDefaultState().param.value);

            volumeManager.SetQualityDefaultProfile(null);
            Assert.AreEqual(TestVolume.k_OverrideValue3, GetDefaultState().param.value);

            volumeManager.SetCustomDefaultProfiles(null);
            Assert.AreEqual(TestVolume.k_DefaultValue, GetDefaultState().param.value);
        }

        [Test]
        public void CustomDefaultProfilesAreAppliedInOrder()
        {
            volumeManager = new VolumeManager();
            volumeManager.Initialize();

            volumeManager.SetCustomDefaultProfiles(new List<VolumeProfile> { m_VolumeProfile1, m_VolumeProfile2 });
            Assert.AreEqual(TestVolume.k_OverrideValue2, GetDefaultState().param.value);

            volumeManager.SetCustomDefaultProfiles(new List<VolumeProfile> { m_VolumeProfile2, m_VolumeProfile1 });
            Assert.AreEqual(TestVolume.k_OverrideValue, GetDefaultState().param.value);
        }
    }
}
