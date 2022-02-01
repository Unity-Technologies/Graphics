using System;
using NUnit.Framework;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEngine.GraphToolsFoundation.Overdrive.Tests.Types
{
    class TypeHandleTests
    {
        [Test]
        public void Test_TypeHandleSerializationOfCustomType_Unknown()
        {
            //Arrange-Act
            var th = typeof(Unknown).GenerateTypeHandle();

            //Assert
            Assert.That(th, Is.EqualTo(TypeHandle.Unknown));
        }

        class A {}

        [Test]
        public void Test_TypeHandleDeserializationOfRegularType()
        {
            //Arrange
            TypeHandle th = typeof(A).GenerateTypeHandle();

            //Act
            Type deserializedTypeHandle = th.Resolve();

            //Assert
            Assert.That(deserializedTypeHandle, Is.EqualTo(typeof(A)));
        }
    }
}
