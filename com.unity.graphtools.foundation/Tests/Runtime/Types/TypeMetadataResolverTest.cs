using System;
using NUnit.Framework;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEngine.GraphToolsFoundation.Overdrive.Tests.Types
{
    class TypeMetadataResolverTest
    {
        static readonly TypeHandle k_IntHandle = new TypeHandle("__INT");
        static readonly TypeHandle k_FloatHandle = new TypeHandle("__FLOAT");
        static readonly TypeHandle k_DoubleHandle = new TypeHandle("__DOUBLE");

        [Test]
        public void Should_CreateNewMetadata_OnEveryDifferentTypeHandle()
        {
            //Arrange
            var resolver = new TypeMetadataResolver();

            //Act
            var intMetadata = resolver.Resolve(k_IntHandle);
            var intMetadata2 = resolver.Resolve(k_IntHandle);
            var floatMetadata = resolver.Resolve(k_FloatHandle);
            var floatMetadata2 = resolver.Resolve(k_FloatHandle);
            var doubleMetadata = resolver.Resolve(k_DoubleHandle);
            var doubleMetadata2 = resolver.Resolve(k_DoubleHandle);

            //Assert
            Assert.That(intMetadata, Is.SameAs(intMetadata2));
            Assert.That(floatMetadata, Is.SameAs(floatMetadata2));
            Assert.That(doubleMetadata, Is.SameAs(doubleMetadata2));
        }
    }
}
