using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using NUnit.Framework;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEngine.GraphToolsFoundation.Overdrive.Tests.Types
{
    class CSharpTypeBasedMetadataTest
    {
        readonly TypeHandle m_Handle = new TypeHandle("__TYPEHANDLE");

        [SetUp]
        public void SetUp()
        {
        }

        TypeMetadata CreateMetadata(Type t)
        {
            return new TypeMetadata(m_Handle, t);
        }

        [Test]
        public void Test_TypeHandle()
        {
            //Arrange
            var typeMetadata = CreateMetadata(typeof(float));

            //Act
            TypeHandle handle = typeMetadata.TypeHandle;

            //Assert
            Assert.That(handle, Is.EqualTo(m_Handle));
        }

        [Test]
        public void Test_FriendlyName()
        {
            //Arrange
            var typeMetadata = CreateMetadata(typeof(float));

            //Act
            string friendlyName = typeMetadata.FriendlyName;

            //Assert
            Assert.That(friendlyName, Is.EqualTo("Float"));
        }

        class A
        {
        }

        class B : A
        {
        }

        [Test]
        public void Test_IsAssignableFrom_UsingMetadata_DelegatesToOtherMetadata()
        {
            //Arrange
            var typeAMetadata = CreateMetadata(typeof(A));
            var typeBMetadata = CreateMetadata(typeof(B));

            //Act
            bool isAssignableFrom = typeAMetadata.IsAssignableFrom(typeBMetadata);

            //Assert
            Assert.That(isAssignableFrom, Is.True);
        }

        [Test]
        public void Test_IsAssignableFrom_UsingType()
        {
            //Arrange
            var typeMetadata = CreateMetadata(typeof(IDictionary<int, float>));

            //Act
            bool isAssignableFrom = typeMetadata.IsAssignableFrom(typeof(Dictionary<int, float>));

            //Assert
            Assert.That(isAssignableFrom, Is.True);
        }

        [Test]
        public void Test_IsAssignableTo_UsingType()
        {
            //Arrange
            var typeMetadata = CreateMetadata(typeof(Dictionary<int, float>));

            //Act
            bool isAssignableTo = typeMetadata.IsAssignableTo(typeof(IDictionary<int, float>));

            //Assert
            Assert.That(isAssignableTo, Is.True);
        }

        [Test, Ignore("mb")]
        public void Test_IsAssignableTo_UsingGraph()
        {
            throw new NotImplementedException("Test_IsAssignableTo_UsingGraph");
            //            //Arrange
            //            var typeMetadata = CreateMetadata(typeof(VisualBehaviour));
            //
            //            //Act
            //            bool isAssignableTo = typeMetadata.IsAssignableTo(new Mock<IVSGraphModel>().Object);
            //
            //            //Assert
            //            Assert.That(isAssignableTo, Is.False);
        }

#pragma warning disable CS0414
        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        class TestMemberClass
        {
            public int this[int index] => index;

            public int PublicField = 1;
            public int PublicProperty => 2;

            protected int m_ProtectedField = 3;
            protected int ProtectedProperty => 4;

            int m_PrivateField = 5;
            int PrivateProperty => 6;

            public int BlacklistedField = 7;
            public int BlacklistedProperty => 8;

            [Obsolete]
            public int ObsoleteField = 9;
            [Obsolete]
            public int ObsoleteProperty => 10;

            [CompilerGenerated]
            public int CompilerGeneratedField = 11;
            [CompilerGenerated]
            public int CompilerGeneratedProperty => 12;
        }
#pragma warning restore CS0414
    }
}
