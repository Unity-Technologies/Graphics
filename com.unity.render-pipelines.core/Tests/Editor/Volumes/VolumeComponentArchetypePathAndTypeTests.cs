using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    using TSet = VolumeComponentTestDataSet;

    public class VolumeComponentArchetypePathAndTypeTests
    {
        static class Properties
        {
            public static bool SkipObsoleteOrHiddenComponent(
                VolumeComponentType[] types
            )
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

            public static bool PathIsProvidedByMenuAttribute(
                VolumeComponentType[] types
            )
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
        }

        [Test]
        public void SkipObsoleteOrHiddenComponentProperty(
            [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypesArray))]
            VolumeComponentType[] types
        ) => Assert.True(Properties.SkipObsoleteOrHiddenComponent(types));

        [Test]
        public void PathIsProvidedByMenuAttributeProperty(
            [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypesArray))]
            VolumeComponentType[] types
        ) => Assert.True(Properties.PathIsProvidedByMenuAttribute(types));
    }
}
