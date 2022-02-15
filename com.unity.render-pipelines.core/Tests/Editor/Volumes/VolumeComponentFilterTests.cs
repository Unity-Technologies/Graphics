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
            [Test(ExpectedResult = true)]
            public static bool EverythingFilterAccepts(
                [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))] VolumeComponentType type
                )
            {
                var filter = new EverythingVolumeComponentFilter();
                return filter.IsAccepted(type);
            }

            [Test(ExpectedResult = true)]
            public static bool IsExplicitlySupportedFilterAccepts(
                [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))] VolumeComponentType subject,
                [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))] VolumeComponentType target
                )
            {
                var filter = IsExplicitlySupportedVolumeComponentFilter.FromType(target.AsType());
                var isAccepted = filter.IsAccepted(subject);
                var expected = IsSupportedOn.IsExplicitlySupportedBy(subject.AsType(), target.AsType());
                return isAccepted == expected;
            }

            [Test(ExpectedResult = true)]
            public static bool IsSupportedFilterAccepts(
                [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))] VolumeComponentType subject,
                [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))] VolumeComponentType target
                )
            {
                var filter = IsSupportedVolumeComponentFilter.FromType(target.AsType());
                var isAccepted = filter.IsAccepted(subject);
                var expected = IsSupportedOn.IsSupportedBy(subject.AsType(), target.AsType());
                return isAccepted == expected;
            }

            [Test(ExpectedResult = true)]
            public static bool IsVisibleFilterAccepts(
                [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))] VolumeComponentType subject,
                [Values] bool isVisible
            )
            {
                var filter = IsVisibleVolumeComponentFilter.FromIsVisible(isVisible);
                var isAccepted = filter.IsAccepted(subject);
                var isVisibleValue = IsVisibleVolumeComponentFilter.IsVisible(subject);
                var expected = isVisible && isVisibleValue || !isVisible && !isVisibleValue;
                return isAccepted == expected;
            }

            [Test(ExpectedResult = true)]
            public static bool IsVisibleIffNotObsoleteNotHideInInspector(
                [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))]  VolumeComponentType subject
                )
            {
                var isVisible = IsVisibleVolumeComponentFilter.IsVisible(subject);
                var expected = subject.AsType().GetCustomAttributes(true)
                    .All(attr => attr is not ObsoleteAttribute and not HideInInspector);
                return isVisible == expected;
            }
        }
    }
}
