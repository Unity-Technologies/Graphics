using System;
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
        public void EverythingAcceptsEverything()
        {
            bool Property(VolumeComponentType type)
            {
                var filter = new EverythingVolumeComponentFilter();
                return filter.IsAccepted(type);
            }

            Prop.ForAll<VolumeComponentType>(Property).QuickCheckThrowOnFailure();
        }

        [Test]
        public void IsExplicitlySupportedAccepts()
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
        public void IsSupportedAccepts()
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
    }
}
