using System;
using FsCheck;
using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    class VolumeComponentDatabaseTests
    {
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

            Prop.ForAll<VolumeComponentType[]>(Property).QuickCheckThrowOnFailure();
        }
    }
}
