using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Misc
{
    [TestFixture]
    public class CapabilitiesTests
    {
        class TestCapabilities1 : Capabilities
        {
            public static readonly TestCapabilities1 Fooable;
            public static readonly TestCapabilities1 Barable;

            static TestCapabilities1()
            {
                Fooable = new TestCapabilities1(nameof(Fooable));
                Barable = new TestCapabilities1(nameof(Barable));
            }

            TestCapabilities1(string name)
                : base(name, nameof(TestCapabilities1))
            {
            }
        }

        class TestCapabilities2 : Capabilities
        {
            public static readonly TestCapabilities2 Fooable;
            public static readonly TestCapabilities2 Barable;

            static TestCapabilities2()
            {
                Fooable = new TestCapabilities2(nameof(Fooable));
                Barable = new TestCapabilities2(nameof(Barable));
            }

            TestCapabilities2(string name)
                : base(name, nameof(TestCapabilities2))
            {
            }
        }

        [Test]
        public void GtfCapabilitiesIDsAreAttributedCorrectly()
        {
            const int baseGtfCapsCount = 10;

            var ids = new HashSet<int>
            {
                Capabilities.Selectable.Id,
                Capabilities.Deletable.Id,
                Capabilities.Droppable.Id,
                Capabilities.Copiable.Id,
                Capabilities.Renamable.Id,
                Capabilities.Movable.Id,
                Capabilities.Resizable.Id,
                Capabilities.Collapsible.Id,
                Capabilities.Ascendable.Id,
                Capabilities.NeedsContainer.Id
            };

            // Make sure all IDs are different. Actual numerical value is inconsequential.
            Assert.AreEqual(baseGtfCapsCount, ids.Count);
        }

        [Test]
        public void GtfCapabilitiesNamesAreAttributedCorrectly()
        {
            // Just one is enough.
            Assert.AreEqual(".Selectable", Capabilities.Selectable.Name);

            Assert.AreEqual(Capabilities.Selectable, Capabilities.Get(".Selectable"));
        }

        [Test]
        public void ExtendedCapabilitiesWithIdenticalNamesDontConflict()
        {
            var tc1 = TestCapabilities1.Barable;
            var tc2 = TestCapabilities2.Barable;

            Assert.AreNotEqual(tc1.Id, tc2.Id);
        }
    }
}
