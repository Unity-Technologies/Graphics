using System;
using System.Linq;
using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    using TSet = VolumeComponentTestDataSet;

    class SomeObject {}

    partial class EqualityTests
    {

        static readonly (string name, VolumeComponentType type)[] k_NameAndVolumeComponentType = Enumerable.Range(0, 49)
            .RandomInitState(52089765)
            .Select(_ => (TestDataGenerationUtilities.RandomString(), TSet.volumeComponentTypes.RandomElement()))
            .ToArray();

        static partial class Properties
        {
            [Test(ExpectedResult = true)]
            public static bool IsSupportedVolumeComponentFilterEquality(
                [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))]VolumeComponentType l,
                [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))]VolumeComponentType r
            )
            {
                var l2 = IsSupportedVolumeComponentFilter.FromType(l.AsType());
                var r2 = IsSupportedVolumeComponentFilter.FromType(r.AsType());

                var expectsAreEquals = l.AsType() == r.AsType();
                                var areEquals = l2 == r2;
                var areEquals2 = l2.Equals(r2);
                var areEquals3 = l2.Equals((object)r2);
                var areNotEquals4 = l2 != r2;

                // The hashcode must be the same for identical values
                var hashCodeEquals = expectsAreEquals && l2.GetHashCode() == r2.GetHashCode()
                    || !expectsAreEquals;

                // Check equal to itself
                var isEqual = l2 == l2 && l2.GetHashCode() == l2.GetHashCode();

                var nullableTest = false;
                r2 = null;
                nullableTest = !l2.Equals(r2);
                var result = areEquals == areEquals2
                    && areEquals == areEquals3
                    && areEquals != areNotEquals4
                    && hashCodeEquals
                    && areEquals == expectsAreEquals
                    && isEqual
                    && !l2.Equals((object)null)
                    && !l2.Equals((object)new SomeObject())
                    && l2.Equals((object)l2);
                return result;
            }
        }
        static partial class Properties
        {
            [Test(ExpectedResult = true)]
            public static bool IsExplicitlySupportedVolumeComponentFilterEquality(
                [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))]VolumeComponentType l,
                [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))]VolumeComponentType r
            )
            {
                var l2 = IsExplicitlySupportedVolumeComponentFilter.FromType(l.AsType());
                var r2 = IsExplicitlySupportedVolumeComponentFilter.FromType(r.AsType());

                var expectsAreEquals = l.AsType() == r.AsType();
                                var areEquals = l2 == r2;
                var areEquals2 = l2.Equals(r2);
                var areEquals3 = l2.Equals((object)r2);
                var areNotEquals4 = l2 != r2;

                // The hashcode must be the same for identical values
                var hashCodeEquals = expectsAreEquals && l2.GetHashCode() == r2.GetHashCode()
                    || !expectsAreEquals;

                // Check equal to itself
                var isEqual = l2 == l2 && l2.GetHashCode() == l2.GetHashCode();

                var nullableTest = false;
                r2 = null;
                nullableTest = !l2.Equals(r2);
                var result = areEquals == areEquals2
                    && areEquals == areEquals3
                    && areEquals != areNotEquals4
                    && hashCodeEquals
                    && areEquals == expectsAreEquals
                    && isEqual
                    && !l2.Equals((object)null)
                    && !l2.Equals((object)new SomeObject())
                    && l2.Equals((object)l2);
                return result;
            }
        }
        static partial class Properties
        {
            [Test(ExpectedResult = true)]
            public static bool VolumeComponentTypeEquality(
                [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))]VolumeComponentType l,
                [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))]VolumeComponentType r
            )
            {
                var l2 = l;
                var r2 = r;

                var expectsAreEquals = l.AsType() == r.AsType();
                                var areEquals = l2 == r2;
                var areEquals2 = l2.Equals(r2);
                var areEquals3 = l2.Equals((object)r2);
                var areNotEquals4 = l2 != r2;

                // The hashcode must be the same for identical values
                var hashCodeEquals = expectsAreEquals && l2.GetHashCode() == r2.GetHashCode()
                    || !expectsAreEquals;

                // Check equal to itself
                var isEqual = l2 == l2 && l2.GetHashCode() == l2.GetHashCode();

                var nullableTest = false;
                var result = areEquals == areEquals2
                    && areEquals == areEquals3
                    && areEquals != areNotEquals4
                    && hashCodeEquals
                    && areEquals == expectsAreEquals
                    && isEqual
                    && !l2.Equals((object)null)
                    && !l2.Equals((object)new SomeObject())
                    && l2.Equals((object)l2);
                return result;
            }
        }
        static partial class Properties
        {
            [Test(ExpectedResult = true)]
            public static bool IsVisibleVolumeComponentFilterEquality(
                [Values]bool l,
                [Values]bool r
            )
            {
                var l2 = IsVisibleVolumeComponentFilter.FromIsVisible(l);
                var r2 = IsVisibleVolumeComponentFilter.FromIsVisible(r);

                var expectsAreEquals = l == r;
                                var areEquals = l2 == r2;
                var areEquals2 = l2.Equals(r2);
                var areEquals3 = l2.Equals((object)r2);
                var areNotEquals4 = l2 != r2;

                // The hashcode must be the same for identical values
                var hashCodeEquals = expectsAreEquals && l2.GetHashCode() == r2.GetHashCode()
                    || !expectsAreEquals;

                // Check equal to itself
                var isEqual = l2 == l2 && l2.GetHashCode() == l2.GetHashCode();

                var nullableTest = false;
                r2 = null;
                nullableTest = !l2.Equals(r2);
                var result = areEquals == areEquals2
                    && areEquals == areEquals3
                    && areEquals != areNotEquals4
                    && hashCodeEquals
                    && areEquals == expectsAreEquals
                    && isEqual
                    && !l2.Equals((object)null)
                    && !l2.Equals((object)new SomeObject())
                    && l2.Equals((object)l2);
                return result;
            }
        }
        static partial class Properties
        {
            [Test(ExpectedResult = true)]
            public static bool VolumeComponentArchetypeEquality(
                [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypesArray))]VolumeComponentType[] l,
                [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypesArray))]VolumeComponentType[] r
            )
            {
                var l2 = VolumeComponentArchetype.FromTypes(l);
                var r2 = VolumeComponentArchetype.FromTypes(r);

                var expectsAreEquals = l.ToHashSet().SetEquals(r.ToHashSet());
                                var areEquals = l2 == r2;
                var areEquals2 = l2.Equals(r2);
                var areEquals3 = l2.Equals((object)r2);
                var areNotEquals4 = l2 != r2;

                // The hashcode must be the same for identical values
                var hashCodeEquals = expectsAreEquals && l2.GetHashCode() == r2.GetHashCode()
                    || !expectsAreEquals;

                // Check equal to itself
                var isEqual = l2 == l2 && l2.GetHashCode() == l2.GetHashCode();

                var nullableTest = false;
                r2 = null;
                nullableTest = !l2.Equals(r2);
                var result = areEquals == areEquals2
                    && areEquals == areEquals3
                    && areEquals != areNotEquals4
                    && hashCodeEquals
                    && areEquals == expectsAreEquals
                    && isEqual
                    && !l2.Equals((object)null)
                    && !l2.Equals((object)new SomeObject())
                    && l2.Equals((object)l2);
                return result;
            }
        }
        static partial class Properties
        {
            [Test(ExpectedResult = true)]
            public static bool VolumeComponentArchetypeTreeProviderPathNodeEquality(
                [ValueSource(nameof(k_NameAndVolumeComponentType))](string name, VolumeComponentType type) l,
                [ValueSource(nameof(k_NameAndVolumeComponentType))](string name, VolumeComponentType type) r
            )
            {
                var l2 = new VolumeComponentArchetypeTreeProvider.PathNode(l.name, l.type);
                var r2 = new VolumeComponentArchetypeTreeProvider.PathNode(r.name, r.type);

                var expectsAreEquals = (l.name == r.name && l.type == r.type);
                                var areEquals = l2 == r2;
                var areEquals2 = l2.Equals(r2);
                var areEquals3 = l2.Equals((object)r2);
                var areNotEquals4 = l2 != r2;

                // The hashcode must be the same for identical values
                var hashCodeEquals = expectsAreEquals && l2.GetHashCode() == r2.GetHashCode()
                    || !expectsAreEquals;

                // Check equal to itself
                var isEqual = l2 == l2 && l2.GetHashCode() == l2.GetHashCode();

                var nullableTest = false;
                r2 = null;
                nullableTest = !l2.Equals(r2);
                var result = areEquals == areEquals2
                    && areEquals == areEquals3
                    && areEquals != areNotEquals4
                    && hashCodeEquals
                    && areEquals == expectsAreEquals
                    && isEqual
                    && !l2.Equals((object)null)
                    && !l2.Equals((object)new SomeObject())
                    && l2.Equals((object)l2);
                return result;
            }
        }

        [Test]
        public void EverythingVolumeComponentFilterEquality()
        {
            var l2 = new EverythingVolumeComponentFilter();
            var r2 = new EverythingVolumeComponentFilter();

            var expectsAreEquals = true;
                            var areEquals = l2 == r2;
                var areEquals2 = l2.Equals(r2);
                var areEquals3 = l2.Equals((object)r2);
                var areNotEquals4 = l2 != r2;

                // The hashcode must be the same for identical values
                var hashCodeEquals = expectsAreEquals && l2.GetHashCode() == r2.GetHashCode()
                    || !expectsAreEquals;

                // Check equal to itself
                var isEqual = l2 == l2 && l2.GetHashCode() == l2.GetHashCode();

                var nullableTest = false;
                r2 = null;
                nullableTest = !l2.Equals(r2);
                var result = areEquals == areEquals2
                    && areEquals == areEquals3
                    && areEquals != areNotEquals4
                    && hashCodeEquals
                    && areEquals == expectsAreEquals
                    && isEqual
                    && !l2.Equals((object)null)
                    && !l2.Equals((object)new SomeObject())
                    && l2.Equals((object)l2);
            Assert.True(result);
        }
    }
}

