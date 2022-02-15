using System;
using System.Linq;
using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    using TSet = VolumeComponentTestDataSet;

    class VolumeComponentDatabaseTests
    {
        static class Properties
        {
            [Test(ExpectedResult = true)]
            public static bool ContainsRequiredTypes(
                [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypesArray))] VolumeComponentType[] types
                )
            {
                var database = VolumeComponentDatabase.FromTypes(types);
                using (HashSetPool<VolumeComponentType>.Get(out var set))
                {
                    set.UnionWith(types);
                    return set.SetEquals(database.componentTypes);
                }
            }
        }

        [Test]
        public void HasAllComponentsInMemory()
        {
            var database = VolumeComponentDatabase.memoryDatabase;

            var componentTypes = CoreUtils.GetAllTypesDerivedFrom<VolumeComponent>()
                .Where(t => !t.IsAbstract).Select(VolumeComponentType.FromTypeUnsafe).ToHashSet();

            Assert.True(database.componentTypes.ToHashSet().SetEquals(componentTypes));
        }

        [Test]
        public void CallStaticInitOnComponents()
        {
            var initCount = VolumeComponentWithStaticInit.initCount;
            VolumeComponentDatabase.StaticInitializeComponents(new[]
            {
                VolumeComponentType.FromTypeUnsafe(typeof(VolumeComponentWithStaticInit))
            });
            Assert.AreEqual(initCount + 1, VolumeComponentWithStaticInit.initCount);
        }

        class VolumeComponentWithStaticInit : VolumeComponent
        {
            public static int initCount;
            static void Init()
            {
                ++initCount;
            }
        }

    }
}
