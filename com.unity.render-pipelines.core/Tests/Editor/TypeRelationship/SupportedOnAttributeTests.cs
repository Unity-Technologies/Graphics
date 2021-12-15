using NUnit.Framework;
using UnityEditor.Experimental.GraphView;

namespace UnityEngine.Rendering.Tests
{
    class SupportedOnAttributeTests
    {
        class TestA { }

        class TestB { }

        [Test]
        public void RegisterStaticRelationSetStaticValues()
        {
            // No static registration performed
            Assert.False(IsSupportedOn.IsExplicitlySupportedBy<TestA, TestB>());
            Assert.False(IsSupportedOn.HasExplicitSupport<TestA>());

            IsSupportedOn.RegisterStaticRelation(typeof(TestA), typeof(TestB));

            Assert.True(IsSupportedOn.IsExplicitlySupportedBy<TestA, TestB>());
            Assert.True(IsSupportedOn.HasExplicitSupport<TestA>());
        }
    }
}
