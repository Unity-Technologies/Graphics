using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    [SupportedOn(typeof(__B))]
    class __A { }
    class __B { }
    class __C { }


    public class IsSupportedOn
    {
        [Test]
        public void BasicUsage()
        {
            Assert.True(IsSupportedOn<__A, __B>.Value, "A supports explicitly B");
            Assert.False(IsSupportedOn<__A, __C>.Value, "A don't supports explicitly C");
            Assert.True(IsSupportedOn<__B, __C>.Value, "B supports implicitly C");
        }
    }

    public class DynamicTypeRelations
    {
        [Test]
        public void DynamicAPI()
        {
            var relations = new DynamicTypeRelation();
            relations.RegisterRelation(typeof(__A), typeof(__B));

            Assert.True(relations.IsRelated(typeof(__A), typeof(__B)), "A supports explicitly B");
            Assert.False(relations.IsRelated(typeof(__A), typeof(__C)), "A don't supports explicitly C");
            Assert.True(relations.IsRelated(typeof(__B), typeof(__C)), "B supports implicitly C");
        }

        [Test]
        public void GenericAPI()
        {
            var relations = new DynamicTypeRelation();
            relations.RegisterRelation(typeof(__A), typeof(__B));

            Assert.True(relations.IsRelated<Tests.__A, Tests.__B>(), "A supports explicitly B");
            Assert.False(relations.IsRelated<Tests.__A, Tests.__C>(), "A don't supports explicitly C");
            Assert.True(relations.IsRelated<Tests.__B, Tests.__C>(), "B supports implicitly C");
        }
    }
}
