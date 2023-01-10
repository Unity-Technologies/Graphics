using NUnit.Framework;
using System.Linq;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests
{
    partial class RenderingDebuggerTests
    {
        private DebugUI.Panel m_Panel;

        [SetUp]
        public void Setup()
        {
            m_Panel = DebugManager.instance.GetPanel("Tests", true);
        }

        [TearDown]
        public void TearDown()
        {
            DebugManager.instance.RemovePanel(m_Panel);
        }

        static TestCaseData[] s_TestCaseDatasGetItem =
        {
            new TestCaseData(new DebugUI.BoolField() { displayName = "element", flags = DebugUI.Flags.FrequentlyUsed }, DebugUI.Flags.FrequentlyUsed)
                .SetName("Given a widget with a flag, when looking for items with that flag, then the item is returned")
                .Returns(new string[] { "element" }),
            new TestCaseData(new DebugUI.BoolField() { displayName = "element" }, DebugUI.Flags.FrequentlyUsed)
                .SetName("Given a widget without flags, when looking for items with a flag, then nothing is returned")
                .Returns(new string[] { }),
            new TestCaseData(new DebugUI.Foldout()
                {
                    displayName = "foldout",
                    flags = DebugUI.Flags.FrequentlyUsed,
                    children = { new DebugUI.BoolField() { displayName = "element" } }
                }, DebugUI.Flags.FrequentlyUsed)
                .SetName("Given a container widget with children and a flag, when looking for items with a flag, the container is returned")
                .Returns(new string[] { "foldout" }),
             new TestCaseData(new DebugUI.Foldout()
                {
                    displayName = "foldout",
                    flags = DebugUI.Flags.FrequentlyUsed,
                    children = { new DebugUI.BoolField() { displayName = "element", flags = DebugUI.Flags.FrequentlyUsed, } }
                }, DebugUI.Flags.FrequentlyUsed)
                .SetName("Given a container and children with a flag, when looking for a flag, then only the container is returned")
                .Returns(new string[] { "foldout" }),
             new TestCaseData(new DebugUI.Foldout()
                {
                    displayName = "foldout",
                    children = { new DebugUI.BoolField() { displayName = "element", flags = DebugUI.Flags.FrequentlyUsed, }, new DebugUI.BoolField() { displayName = "element2", flags = DebugUI.Flags.FrequentlyUsed, } }
                }, DebugUI.Flags.FrequentlyUsed)
                .SetName("Given multiple children widgets with a flag, when looking for a flag the item is returned")
                .Returns(new string[] { "element", "element2" }),
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatasGetItem))]
        public string[] GetItemTestFlags(DebugUI.Widget widget, DebugUI.Flags flags)
        {
            m_Panel.children.Add(widget);

            var itemsFoundNames = DebugManager.instance
                .GetItemsFromContainer(flags, m_Panel)
                .Select(i => i.displayName).ToArray();

            return itemsFoundNames;
        }

        static TestCaseData[] s_TestCaseDatasGetItemQueryPath =
        {
            new TestCaseData(new DebugUI.BoolField() { displayName = "element", flags = DebugUI.Flags.FrequentlyUsed }, "Tests -> element")
                .SetName("Given a widget, when looking by it's query path, then the item is found")
                .Returns("element"),
            new TestCaseData(new DebugUI.BoolField() { displayName = "element" }, "Tests -> element2")
                .SetName("Given a query path that does not map to any widget, when looking for it, nothing is found")
                .Returns(string.Empty),
            new TestCaseData(new DebugUI.Foldout()
                {
                    displayName = "foldout",
                    flags = DebugUI.Flags.FrequentlyUsed,
                    children = { new DebugUI.BoolField() { displayName = "element" } }
                }, "Tests -> foldout -> element")
                .SetName("Given a query path for a child widget, when using that query path, then the child object is returned")
                .Returns("element"),
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatasGetItemQueryPath))]
        public string GetItemTestQueryPath(DebugUI.Widget widget, string queryPath)
        {
            m_Panel.children.Add(widget);

            var itemsFoundNames = DebugManager.instance
                .GetItem(queryPath)?
                .displayName ?? string.Empty;

            return itemsFoundNames;
        }
    }
}
