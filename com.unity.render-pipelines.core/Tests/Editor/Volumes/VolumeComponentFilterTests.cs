using System;
using System.Linq;
using FsCheck;
using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    class VolumeComponentFilterTests
    {
        [OneTimeSetUp]
        public static void SetupFixture()
        {
            ArbX.Register();
        }

        [Test]
        public void EverythingFilterAccepts()
        {
            bool Property(VolumeComponentType type)
            {
                var filter = new EverythingVolumeComponentFilter();
                return filter.IsAccepted(type);
            }

            Prop.ForAll<VolumeComponentType>(Property).QuickCheckThrowOnFailure();
        }

        [Test]
        public void IsExplicitlySupportedFilterAccepts()
        {
            bool Property(VolumeComponentType subject, VolumeComponentType target)
            {
                var filter = IsExplicitlySupportedVolumeComponentFilter.FromType(target.AsType());
                var isAccepted = filter.IsAccepted(subject);
                var expected = IsSupportedOn.IsExplicitlySupportedBy(subject.AsType(), target.AsType());
                return isAccepted == expected;
            }

            Prop.ForAll<VolumeComponentType, VolumeComponentType>(Property).QuickCheckThrowOnFailure();
        }

        [Test]
        public void IsSupportedFilterAccepts()
        {
            bool Property(VolumeComponentType subject, VolumeComponentType target)
            {
                var filter = IsSupportedVolumeComponentFilter.FromType(target.AsType());
                var isAccepted = filter.IsAccepted(subject);
                var expected = IsSupportedOn.IsSupportedBy(subject.AsType(), target.AsType());
                return isAccepted == expected;
            }

            Prop.ForAll<VolumeComponentType, VolumeComponentType>(Property).QuickCheckThrowOnFailure();
        }

        [Test]
        public void IsVisibleFilterAccepts()
        {
            bool Property(VolumeComponentType subject, bool isVisible)
            {
                var filter = IsVisibleVolumeComponentFilter.FromIsVisible(isVisible);
                var isAccepted = filter.IsAccepted(subject);
                var isVisibleValue = IsVisibleVolumeComponentFilter.IsVisible(subject);
                var expected = isVisible && isVisibleValue || !isVisible && !isVisibleValue;
                return isAccepted == expected;
            }

            Prop.ForAll<VolumeComponentType, bool>(Property).QuickCheckThrowOnFailure();
        }

        [Test]
        public void IsVisibleIffNotObsoleteNotHideInInspector()
        {
            bool Property(VolumeComponentType subject)
            {
                var isVisible = IsVisibleVolumeComponentFilter.IsVisible(subject);
                var expected = subject.AsType().GetCustomAttributes(true)
                    .All(attr => attr is not ObsoleteAttribute and not HideInInspector);
                return isVisible == expected;
            }

            Prop.ForAll<VolumeComponentType>(Property).QuickCheckThrowOnFailure();
        }
    }
}
