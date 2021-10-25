using System;
using FsCheck;
using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    class VolumeComponentTypeTests
    {
        [OneTimeSetUp]
        public static void SetupFixture()
        {
            ArbX.Register();
        }

        [Test]
        public void OnlyAcceptsVolumeComponentTypes()
        {
            Prop.ForAll(new ArbX.ArbitraryType(), type =>
            {
                var success = VolumeComponentType.FromType(type, out var volumeType);
                var isVolumeType = type?.IsSubclassOf(typeof(VolumeComponent));

                var mustBeValid = isVolumeType == true;
                var isValid = success && volumeType.AsType() == type;
                var isInvalid = !success && volumeType == default;

                return mustBeValid && isValid || !mustBeValid && isInvalid;
            })
                .QuickCheckThrowOnFailure();
        }

        [Test]
        public void Equality()
        {
            Prop.ForAll((VolumeComponentType l, VolumeComponentType r) =>
            {
                var expectsAreEqual = l.AsType() == r.AsType();
                var areEqual = l == r;
                return expectsAreEqual && areEqual || !expectsAreEqual && !areEqual;
            }).QuickCheckThrowOnFailure();
        }
    }
}
