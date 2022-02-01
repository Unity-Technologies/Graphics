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

    public class DynamicTypeRelations
    {
        [Test]
        public void DynamicAPI()
        {
            var relations = new DynamicTypeRelation();
            relations.RegisterRelation(typeof(__A), typeof(__B));

            Assert.True(relations.HasRelations(typeof(__A)));
            Assert.False(relations.HasRelations(typeof(__B)));

            Assert.True(relations.AreRelated(typeof(__A), typeof(__B)), "A relates to B");
            Assert.False(relations.AreRelated(typeof(__A), typeof(__C)), "A do not relate to C");
            Assert.False(relations.AreRelated(typeof(__B), typeof(__C)), "B do not relate to C");
        }

        [Test]
        public void GenericAPI()
        {
            var relations = new DynamicTypeRelation();
            relations.RegisterRelation(typeof(__A), typeof(__B));

            Assert.True(relations.HasRelations<__A>());
            Assert.False(relations.HasRelations<__B>());

            Assert.True(relations.AreRelated<Tests.__A, Tests.__B>(), "A relates to B");
            Assert.False(relations.AreRelated<Tests.__A, Tests.__C>(), "A do not relate to C");
            Assert.False(relations.AreRelated<Tests.__B, Tests.__C>(), "B do not relate to C");
        }
    }
}
