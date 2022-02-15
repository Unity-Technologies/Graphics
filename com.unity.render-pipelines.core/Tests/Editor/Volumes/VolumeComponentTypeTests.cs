using System;
using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    using TSet = VolumeComponentTestDataSet;

    class VolumeComponentTypeTests
    {
        static class Properties
        {
            public static bool OnlyAcceptsVolumeComponentTypes(
                Type type
                )
            {
                var success = VolumeComponentType.FromType(type, out var volumeType);
                var isVolumeType = type?.IsSubclassOf(typeof(VolumeComponent));

                var mustBeValid = isVolumeType == true;
                var isValid = success && volumeType.AsType() == type;
                var isInvalid = !success && volumeType == default;

                return mustBeValid && isValid || !mustBeValid && isInvalid;
            }

            public static bool CastToType(
                VolumeComponentType type
                )
            {
                return type.AsType() == (Type)type;
            }
        }

        [Test]
        public void OnlyAcceptsVolumeComponentTypes(
            [ValueSource(typeof(TSet), nameof(TSet.types))]
            Type type
        ) => Assert.True(Properties.OnlyAcceptsVolumeComponentTypes(type));

        [Test]
        public void CastToType(
            [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))]
            VolumeComponentType type
        ) => Assert.True(Properties.CastToType(type));
    }
}
