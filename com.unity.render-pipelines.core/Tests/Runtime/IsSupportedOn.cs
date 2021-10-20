using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    [SupportedOn(typeof(__B))]
    class __A { }
    class __B { }
    class __C { }


    public class IsSupportedOnTest
    {
        [Test]
        public void GenericAPI()
        {
            Assert.True(IsSupportedOn.HasExplicitSupport<__A>(), "A has explicit support.");
            Assert.False(IsSupportedOn.HasExplicitSupport<__B>(), "B has no explicit support.");

            Assert.True(IsSupportedOn.IsExplicitlySupportedBy<__A, __B>(), "A supports explicitly B.");
            Assert.True(IsSupportedOn.IsSupportedBy<__A, __B>(), "A supports B.");

            Assert.False(IsSupportedOn.IsExplicitlySupportedBy<__A, __C>(), "A don't supports explicitly B.");
            Assert.False(IsSupportedOn.IsSupportedBy<__A, __C>(), "A don't supports implicitly B.");

            Assert.False(IsSupportedOn.IsExplicitlySupportedBy<__B, __C>(), "B don't supports explicitly C.");
            Assert.True(IsSupportedOn.IsSupportedBy<__B, __C>(), "B supports implicitly C.");
        }

        [Test]
        public void DynamicAPI()
        {
            Assert.True(IsSupportedOn.IsExplicitlySupportedBy(typeof(__A), typeof(__B)), "A supports explicitly B.");
            Assert.True(IsSupportedOn.IsSupportedBy(typeof(__A), typeof(__B)), "A supports B.");
            Assert.True(IsSupportedOn.HasExplicitSupport(typeof(__A)), "A has explicit support.");

            Assert.False(IsSupportedOn.IsExplicitlySupportedBy(typeof(__A), typeof(__C)), "A don't supports explicitly B.");
            Assert.False(IsSupportedOn.IsSupportedBy(typeof(__A), typeof(__C)), "A don't supports implicitly B.");

            Assert.False(IsSupportedOn.IsExplicitlySupportedBy(typeof(__B), typeof(__C)), "B don't supports explicitly C.");
            Assert.True(IsSupportedOn.IsSupportedBy(typeof(__B), typeof(__C)), "B supports implicitly C.");
        }
    }
}
