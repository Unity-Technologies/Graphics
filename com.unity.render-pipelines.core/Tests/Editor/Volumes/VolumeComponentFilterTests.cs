using System;
using System.Linq;
using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    using TSet = VolumeComponentTestDataSet;

    class VolumeComponentFilterTests
    {
        static class Properties
        {
            public static bool EverythingFilterAccepts(
                 VolumeComponentType type
                )
            {
                var filter = new EverythingVolumeComponentFilter();
                return filter.IsAccepted(type);
            }

            public static bool IsExplicitlySupportedFilterAccepts(
                VolumeComponentType subject,
                VolumeComponentType target
                )
            {
                var filter = IsExplicitlySupportedVolumeComponentFilter.FromType(target.AsType());
                var isAccepted = filter.IsAccepted(subject);
                var expected = IsSupportedOn.IsExplicitlySupportedBy(subject.AsType(), target.AsType());
                return isAccepted == expected;
            }

            public static bool IsSupportedFilterAccepts(
                VolumeComponentType subject,
                VolumeComponentType target
                )
            {
                var filter = IsSupportedVolumeComponentFilter.FromType(target.AsType());
                var isAccepted = filter.IsAccepted(subject);
                var expected = IsSupportedOn.IsSupportedBy(subject.AsType(), target.AsType());
                return isAccepted == expected;
            }

            public static bool IsVisibleFilterAccepts(
                VolumeComponentType subject,
                bool isVisible
            )
            {
                var filter = IsVisibleVolumeComponentFilter.FromIsVisible(isVisible);
                var isAccepted = filter.IsAccepted(subject);
                var isVisibleValue = IsVisibleVolumeComponentFilter.IsVisible(subject);
                var expected = isVisible && isVisibleValue || !isVisible && !isVisibleValue;
                return isAccepted == expected;
            }

            public static bool IsVisibleIffNotObsoleteNotHideInInspector(
                VolumeComponentType subject
                )
            {
                var isVisible = IsVisibleVolumeComponentFilter.IsVisible(subject);
                var expected = subject.AsType().GetCustomAttributes(true)
                    .All(attr => attr is not ObsoleteAttribute and not HideInInspector);
                return isVisible == expected;
            }
        }

        [Test]
        public void EverythingFilterAcceptsProperty(
            [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))]
            VolumeComponentType type
        ) => Assert.True(Properties.EverythingFilterAccepts(type));

        [Test]
        public void IsExplicitlySupportedFilterAcceptsProperty(
            [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))]
            VolumeComponentType subject,
            [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))]
            VolumeComponentType target
        ) => Assert.True(Properties.IsExplicitlySupportedFilterAccepts(subject, target));

        [Test]
        public void IsSupportedFilterAcceptsProperty(
            [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))]
            VolumeComponentType subject,
            [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))]
            VolumeComponentType target
        ) => Assert.True(Properties.IsSupportedFilterAccepts(subject, target));

        [Test]
        public void IsVisibleFilterAcceptsProperty(
            [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))]
            VolumeComponentType subject,
            [Values] bool isVisible
        ) => Assert.True(Properties.IsVisibleFilterAccepts(subject, isVisible));

        [Test]
        public void IsVisibleIffNotObsoleteNotHideInInspectorProperty(
            [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))]
            VolumeComponentType subject
        ) => Assert.True(Properties.IsVisibleIffNotObsoleteNotHideInInspector(subject));
    }
}
