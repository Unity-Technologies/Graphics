using System.Linq;
using NUnit.Framework;

namespace UnityEngine.GraphToolsFoundation.Overdrive.Tests
{
    [TestFixture]
    public class EnumerationTests
    {
        class BaseColors : Enumeration
        {
            public static readonly BaseColors Red = new BaseColors(0, nameof(Red));
            public static readonly BaseColors Green = new BaseColors(1, nameof(Green));
            // ReSharper disable once UnusedMember.Local
            public static readonly BaseColors Blue = new BaseColors(2, nameof(Blue));

            protected BaseColors(int id, string name)
                : base(id, name)
            {
            }
        }

        class MoreColors : BaseColors
        {
            public static readonly MoreColors Yellow = new MoreColors(3, nameof(Yellow));
            // ReSharper disable once UnusedMember.Local
            public static readonly MoreColors Cyan = new MoreColors(4, nameof(Cyan));

            protected MoreColors(int id, string name)
                : base(id, name)
            {
            }
        }

        class Shapes : Enumeration
        {
            public static readonly Shapes Circle = new Shapes(0, nameof(Circle));
            // ReSharper disable once UnusedMember.Local
            public static readonly Shapes Triangle = new Shapes(1, nameof(Triangle));

            protected Shapes(int id, string name)
                : base(id, name)
            {
            }
        }

        [Test]
        public void GetDeclaredEnumValuesWorks()
        {
            Assert.AreEqual(3, Enumeration.GetDeclared<BaseColors>().Count());
            Assert.AreEqual(2, Enumeration.GetDeclared<MoreColors>().Count());
        }

        [Test]
        public void GetAllEnumValuesWorks()
        {
            Assert.AreEqual(5, Enumeration.GetAll<MoreColors, BaseColors>().Count());
        }

        [Test]
        public void EqualToSelf()
        {
#pragma warning disable 1718
            // ReSharper disable once EqualExpressionComparison
            Assert.IsTrue(BaseColors.Red == BaseColors.Red);
            // ReSharper disable once EqualExpressionComparison
            Assert.IsTrue(MoreColors.Yellow == MoreColors.Yellow);
#pragma warning restore 1718
        }

        [Test]
        public void DifferentIDAreNotEquals()
        {
            Assert.IsFalse(BaseColors.Red == BaseColors.Green);
            Assert.IsFalse(BaseColors.Red == MoreColors.Yellow);
            Assert.IsFalse(MoreColors.Yellow == BaseColors.Red);
        }

        [Test]
        public void DifferentTypesAreNotEquals()
        {
            Assert.IsFalse(Shapes.Circle == BaseColors.Red);
        }

        [Test]
        public void NullIsNotEqual()
        {
            Assert.IsFalse(null == BaseColors.Red);
            Assert.IsFalse(BaseColors.Red == null);
        }
    }
}
