using System;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using UnityEngine.Tests;

namespace UnityEngine.Rendering.Tests
{
    class VolumeComponentDatabaseTests
    {
        class VolumeComponentWithStaticInit : VolumeComponent
        {
            public static int initCount;
            static void Init()
            {
                ++initCount;
            }
        }

        [Test]
        public void ContainsRequiredTypes()
        {
            bool Property(VolumeComponentType[] types)
            {
                var database = VolumeComponentDatabase.FromTypes(types);
                using (HashSetPool<VolumeComponentType>.Get(out var set))
                {
                    set.UnionWith(types);
                    return set.SetEquals(database.componentTypes);
                }
            }

            Prop.ForAll<VolumeComponentType[]>(Property).ContextualQuickCheckThrowOnFailure();
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
            VolumeComponentDatabase.StaticInitializeComponents(new []
            {
                VolumeComponentType.FromTypeUnsafe(typeof(VolumeComponentWithStaticInit))
            });
            Assert.AreEqual(initCount + 1, VolumeComponentWithStaticInit.initCount);
        }
    }
}
