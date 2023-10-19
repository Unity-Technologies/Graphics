using NUnit.Framework;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class VolumeSystemTests
    {
        private void AssertInterpolatedValues(int count, DiffusionProfileSettingsParameter compC, DiffusionProfileSettings[] profiles)
        {
            Assert.AreEqual(count, compC.accumulatedCount);

            for (var i = 1; i < compC.accumulatedCount; i++)
            {
                Assert.AreEqual(profiles[i - 1], compC.value[i]);
            }

            for (var i = compC.accumulatedCount; i < compC.value.Length; i++)
            {
                Assert.IsNull(compC.value[i]);
            }
        }

        [Test]
        public void DiffusionProfileOverride_InterpAccumulation()
        {
            var profiles = new DiffusionProfileSettings[] {
                ScriptableObject.CreateInstance<DiffusionProfileSettings>(),
                ScriptableObject.CreateInstance<DiffusionProfileSettings>(),
                ScriptableObject.CreateInstance<DiffusionProfileSettings>(),
                ScriptableObject.CreateInstance<DiffusionProfileSettings>(),
                ScriptableObject.CreateInstance<DiffusionProfileSettings>(),
                ScriptableObject.CreateInstance<DiffusionProfileSettings>(),
                ScriptableObject.CreateInstance<DiffusionProfileSettings>(),
                ScriptableObject.CreateInstance<DiffusionProfileSettings>(),
            };

            var compC = new DiffusionProfileSettingsParameter(null);
            var compB = new DiffusionProfileSettingsParameter(new DiffusionProfileSettings[] {
                profiles[4], profiles[5], profiles[6], profiles[7], profiles[0],
            });
            var compA = new DiffusionProfileSettingsParameter(new DiffusionProfileSettings[] {
                profiles[0], profiles[1], profiles[2], profiles[3], null,
            });

            // All interpolation result have the default profile as first element

            // 1. Test basic interpolation + duplicate element + null
            compC.Interp(compB.value, compA.value, 0.0f);
            AssertInterpolatedValues(1 + 4 + 4, compC, profiles);

            // 2. Test interpolation after manually setting source and dest value
            compB.value = new DiffusionProfileSettings[] {
                profiles[0], profiles[4],
            };
            compC.value = new DiffusionProfileSettings[] {
                profiles[5], profiles[6], profiles[7],
            };

            compC.Interp(compB.value, compA.value, 0.0f);
            AssertInterpolatedValues(1 + 4 + 1, compC, profiles);

            // 3. Test when source is dest and is interpolation result
            compC.Interp(compC.value, compC.value, 0.0f);
            AssertInterpolatedValues(1 + 5, compC, profiles);

            foreach (var profile in profiles)
            {
                ScriptableObject.DestroyImmediate(profile);
            }
        }
    }
}
