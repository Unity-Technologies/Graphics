using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using FsCheck;
using NUnit.Framework;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEngine.Rendering.Tests
{
    public class VolumeComponentArchetypePathAndTypeTests
    {
        [OneTimeSetUp]
        public static void SetupFixture()
        {
            ArbX.Register();
        }

        [Test]
        public void SkipObsoleteOrHiddenComponent()
        {
            bool Property(VolumeComponentType[] types)
            {
                var archetype = VolumeComponentArchetype.FromTypes(types);
                if (!archetype.GetOrAddPathAndType(out var extension))
                    return false;

                foreach (var componentPathAndType in extension.volumeComponentPathAndTypes)
                {
                    var customAttributes = componentPathAndType.type.AsType().GetCustomAttributes();
                    if (customAttributes.Any(attr => attr is HideInInspector or ObsoleteAttribute))
                        return false;
                }

                return true;
            }

            Prop.ForAll<VolumeComponentType[]>(Property).QuickCheckThrowOnFailure();
        }

        [Test]
        public void PathIsProvidedByMenuAttribute()
        {
            bool Property(VolumeComponentType[] types)
            {
                var archetype = VolumeComponentArchetype.FromTypes(types);
                if (!archetype.GetOrAddPathAndType(out var extension))
                    return false;

                foreach (var componentPathAndType in extension.volumeComponentPathAndTypes)
                {
                    var attr = componentPathAndType.type.AsType().GetCustomAttribute<VolumeComponentMenu>();
                    if (attr != null
                        && !string.IsNullOrEmpty(attr.menu)
                        && componentPathAndType.path != attr.menu)
                    {
                        return false;
                    }
                }

                return true;
            }

            Prop.ForAll<VolumeComponentType[]>(Property).QuickCheckThrowOnFailure();
        }

    }
}
