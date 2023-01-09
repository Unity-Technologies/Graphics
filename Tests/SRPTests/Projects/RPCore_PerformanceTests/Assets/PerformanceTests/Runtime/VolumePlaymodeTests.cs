using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace PerformanceTests.Runtime
{
    [TestFixture]
    public abstract class VolumeTestBase
    {
        protected readonly LayerMask k_defaultLayer = 1;
        protected VolumeProfile m_VolumeProfile;

        protected List<GameObject> m_Objects = new();

        protected List<Type> m_CustomVolumeTypeList = new();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_VolumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();

            const int numCustomVolumeTypes = 50;
            for (int i = 0; i < numCustomVolumeTypes; i++)
            {
                Type t = Type.GetType($"CustomVolume{i+1}");
                Debug.Assert(t != null);
                m_CustomVolumeTypeList.Add(t);
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            CoreUtils.Destroy(m_VolumeProfile);
        }

        protected void CreateVolumes(bool isGlobal, int numVolumes)
        {
            for (int i = 0; i < numVolumes; i++)
            {
                var volumeGO = new GameObject($"Volume {i + 1}", typeof(Volume), typeof(BoxCollider));

                var volume = volumeGO.GetComponent<Volume>();
                volume.sharedProfile = m_VolumeProfile;
                volume.isGlobal = isGlobal;

                m_Objects.Add(volumeGO);
            }
        }

        protected void Cleanup()
        {
            foreach (var go in m_Objects)
                CoreUtils.Destroy(go);
            m_Objects.Clear();
        }

        protected IEnumerator RunMeasurement()
        {
            var camera = new GameObject("Camera", typeof(Camera));
            m_Objects.Add(camera);

            var sampleGroups = new[]
            {
                new SampleGroup("VolumeManager.Update", SampleUnit.Millisecond),
                new SampleGroup("VolumeManager.ReplaceData", SampleUnit.Millisecond)
            };

            const int numWarmupIterations = 5;
            for (int i = 0; i < numWarmupIterations; i++)
                yield return null;

            using (Measure.ProfilerMarkers(sampleGroups))
            {
                const int numMeasureIterations = 20;
                for (int i = 0; i < numMeasureIterations; i++)
                {
                    VolumeManager.instance.Update(camera.transform, k_defaultLayer);
                    yield return null;
                }
                // This is needed after last yield for some reason, otherwise last sample is missing
                VolumeManager.instance.Update(camera.transform, k_defaultLayer);
            }
        }
    }

    [TestFixture]
    public class VolumeUpdateTests : VolumeTestBase
    {
        static IEnumerable<TestCaseData> UpdateWithOverridesCases()
        {
            bool[] isGlobalCases = {true, false};
            int[] numVolumesCases = {0, 1, 10, 100};
            int[] numParametersCases = {1, 100};

            foreach (bool isGlobal in isGlobalCases)
            foreach (int numVolumes in numVolumesCases)
            foreach (int numParameters in numParametersCases)
                yield return new TestCaseData(isGlobal, numVolumes, numParameters)
                    .SetName(isGlobal
                        ? "{1} global volumes each with {2} parameter overrides"
                        : "{1} local volumes each with {2} parameter overrides")
                    .Returns(null);
        }

        [UnityTest, Performance, TestCaseSource(nameof(UpdateWithOverridesCases))]
        public IEnumerator UpdateWithParameterOverrides(bool isGlobal, int numVolumes, int numParameters)
        {
            CreateVolumes(isGlobal, numVolumes);

            // Setup profile
            m_VolumeProfile.components.Clear();
            switch (numParameters)
            {
                case 1:
                {
                    var volume = m_VolumeProfile.Add<CustomVolume1>();
                    volume.param1.overrideState = true;
                    break;
                }
                case 100:
                {
                    int numTypes = 10;
                    for (int i = 0; i < numTypes; i++)
                    {
                        var volume = m_VolumeProfile.Add(m_CustomVolumeTypeList[i]);
                        volume.SetAllOverridesTo(true);
                    }

                    break;
                }
                default:
                    throw new NotImplementedException();
            }

            yield return RunMeasurement();

            Cleanup();
        }

        static IEnumerable<TestCaseData> UpdateInactivesCases()
        {
            int[] numVolumesCases = {1, 10, 100};
            int[] numInactiveComponentsCases = {1, 10, 50};

            foreach (int numVolumes in numVolumesCases)
            foreach (int numInactiveComponents in numInactiveComponentsCases)
                yield return new TestCaseData(numInactiveComponents, numVolumes)
                    .SetName("{1} local volumes each with {0} inactive VolumeComponents")
                    .Returns(null);
        }

        [UnityTest, Performance, TestCaseSource(nameof(UpdateInactivesCases))]
        public IEnumerator UpdateWithInactiveComponents(int numInactiveComponents, int numVolumes)
        {
            CreateVolumes(isGlobal: false, numVolumes);

            // Setup profile
            m_VolumeProfile.components.Clear();
            for (int i = 0; i < numInactiveComponents; i++)
            {
                var volume = m_VolumeProfile.Add(m_CustomVolumeTypeList[i]);
                Debug.Assert(volume != null);
                volume.active = false;
            }

            yield return RunMeasurement();

            Cleanup();
        }
    }

    [TestFixture]
    public class VolumeParameterInterpolationTests : VolumeTestBase
    {
        const int k_NumVolumes = 100;

        static TestCaseData MakeTestCaseWithName(Type type, int numVolumes)
            => new TestCaseData(type, numVolumes)
                .SetName("{1} local volumes each with 10 {0} overrides")
                .Returns(null);

        static IEnumerable<TestCaseData> TestCaseDatas()
        {
            Type[] volumeComponentType =
            {
                typeof(CustomVolumeFloatParams),
                typeof(CustomVolumeIntParams),
                typeof(CustomVolumeColorParams),
                typeof(CustomVolumeVector4Params),
                typeof(CustomVolumeFloatRangeParams),
                typeof(CustomVolumeAnimationCurveParams),
            };

            foreach (Type type in volumeComponentType)
                yield return MakeTestCaseWithName(type, k_NumVolumes);
        }

        [UnityTest, Performance, TestCaseSource(nameof(TestCaseDatas))]
        public IEnumerator VolumeParameterInterpolation(Type volumeComponentType, int numVolumes)
        {
            CreateVolumes(isGlobal: false, numVolumes);

            // Setup profile
            m_VolumeProfile.components.Clear();
            var volume = m_VolumeProfile.Add(volumeComponentType);
            volume.SetAllOverridesTo(true);

            yield return RunMeasurement();

            Cleanup();
        }
    }

    public class VolumeInitTests
    {
        [Test, Description("Call VolumeManager constructor"), Performance]
        public void VolumeManagerConstructor()
        {
            Measure.Method(() => { new VolumeManager(); })
                .WarmupCount(5)
                .MeasurementCount(50)
                .Run();
        }
    }
}
