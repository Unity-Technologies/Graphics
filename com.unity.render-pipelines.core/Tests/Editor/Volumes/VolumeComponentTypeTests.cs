using System;
using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    using TSet = VolumeComponentTestDataSet;

    class VolumeComponentTypeTests
    {
        static class Properties
        {
            [Test(ExpectedResult = true)]
            public static bool OnlyAcceptsVolumeComponentTypes(
                [ValueSource(typeof(TSet), nameof(TSet.types))] Type type
                )
            {
                var success = VolumeComponentType.FromType(type, out var volumeType);
                var isVolumeType = type?.IsSubclassOf(typeof(VolumeComponent));

                var mustBeValid = isVolumeType == true;
                var isValid = success && volumeType.AsType() == type;
                var isInvalid = !success && volumeType == default;

                return mustBeValid && isValid || !mustBeValid && isInvalid;
            }

            [Test(ExpectedResult = true)]
            public static bool CastToType(
                [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))] VolumeComponentType type
                )
            {
                return type.AsType() == (Type)type;
            }
        }
    }
}
