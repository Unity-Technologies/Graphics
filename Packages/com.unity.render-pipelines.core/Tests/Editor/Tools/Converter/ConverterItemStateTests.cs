using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Rendering.Converter;

namespace UnityEditor.Rendering.Tests
{
    [TestFixture]
    internal class ConverterItemStateTests
    {
        private class MockConverterItem : IRenderPipelineConverterItem
        {
            public string name { get; set; }
            public string info { get; set; }
            public bool isEnabled { get; set; } = true;
            public string isDisabledMessage { get; set; }
            public Texture2D icon => null;
            public void OnClicked() { }
        }

        private class MockFolderItem : IFolderRenderPipelineConverterItem
        {
            public string name { get; set; }
            public string info { get; set; }
            public bool isEnabled { get; set; } = true;
            public string isDisabledMessage { get; set; }
            public IList<IRenderPipelineConverterItem> children { get; set; } = new List<IRenderPipelineConverterItem>();
            public Texture2D icon => null;
            public void OnClicked() { }
        }

        [TestFixture]
        internal class CheckboxStateHelperTests
        {
            [Test]
            public void CalculateCheckboxState_NoEnabledChildren_ReturnsUnchecked()
            {
                var child1 = new ConverterItemState
                {
                    item = new MockConverterItem { isEnabled = false }
                };
                var child2 = new ConverterItemState
                {
                    item = new MockConverterItem { isEnabled = false }
                };
                child1.SetSelectedWithoutNotify(true);
                child2.SetSelectedWithoutNotify(true);

                var childItems = new List<IConverterItemState> { child1, child2 };

                var (isChecked, isIndeterminate) = CheckboxStateHelper.CalculateCheckboxState(childItems);

                Assert.IsFalse(isChecked);
                Assert.IsFalse(isIndeterminate);
            }

            [Test]
            public void CalculateCheckboxState_NoneSelected_ReturnsUnchecked()
            {
                var child1 = new ConverterItemState
                {
                    item = new MockConverterItem { isEnabled = true }
                };
                var child2 = new ConverterItemState
                {
                    item = new MockConverterItem { isEnabled = true }
                };
                child1.SetSelectedWithoutNotify(false);
                child2.SetSelectedWithoutNotify(false);

                var childItems = new List<IConverterItemState> { child1, child2 };

                var (isChecked, isIndeterminate) = CheckboxStateHelper.CalculateCheckboxState(childItems);

                Assert.IsFalse(isChecked);
                Assert.IsFalse(isIndeterminate);
            }

            [Test]
            public void CalculateCheckboxState_AllSelected_ReturnsChecked()
            {
                var child1 = new ConverterItemState
                {
                    item = new MockConverterItem { isEnabled = true }
                };
                var child2 = new ConverterItemState
                {
                    item = new MockConverterItem { isEnabled = true }
                };
                child1.SetSelectedWithoutNotify(true);
                child2.SetSelectedWithoutNotify(true);

                var childItems = new List<IConverterItemState> { child1, child2 };

                var (isChecked, isIndeterminate) = CheckboxStateHelper.CalculateCheckboxState(childItems);

                Assert.IsTrue(isChecked);
                Assert.IsFalse(isIndeterminate);
            }

            [Test]
            public void CalculateCheckboxState_SomeSelected_ReturnsIndeterminate()
            {
                var child1 = new ConverterItemState
                {
                    item = new MockConverterItem { isEnabled = true }
                };
                var child2 = new ConverterItemState
                {
                    item = new MockConverterItem { isEnabled = true }
                };
                var child3 = new ConverterItemState
                {
                    item = new MockConverterItem { isEnabled = true }
                };
                child1.SetSelectedWithoutNotify(true);
                child2.SetSelectedWithoutNotify(false);
                child3.SetSelectedWithoutNotify(true);

                var childItems = new List<IConverterItemState> { child1, child2, child3 };

                var (isChecked, isIndeterminate) = CheckboxStateHelper.CalculateCheckboxState(childItems);

                Assert.IsFalse(isChecked);
                Assert.IsTrue(isIndeterminate);
            }

            [Test]
            public void CalculateCheckboxState_MixedEnabledAndDisabled_OnlyCountsEnabled()
            {
                var child1 = new ConverterItemState
                {
                    item = new MockConverterItem { isEnabled = true }
                };
                var child2 = new ConverterItemState
                {
                    item = new MockConverterItem { isEnabled = false }
                };
                var child3 = new ConverterItemState
                {
                    item = new MockConverterItem { isEnabled = true }
                };
                child1.SetSelectedWithoutNotify(true);
                child2.SetSelectedWithoutNotify(true); // Disabled but selected (shouldn't count)
                child3.SetSelectedWithoutNotify(true);

                var childItems = new List<IConverterItemState> { child1, child2, child3 };

                var (isChecked, isIndeterminate) = CheckboxStateHelper.CalculateCheckboxState(childItems);

                Assert.IsTrue(isChecked); // All enabled children are selected
                Assert.IsFalse(isIndeterminate);
            }

            [Test]
            public void CalculateSelectionState_ReturnsNullForIndeterminate()
            {
                var child1 = new ConverterItemState
                {
                    item = new MockConverterItem { isEnabled = true }
                };
                var child2 = new ConverterItemState
                {
                    item = new MockConverterItem { isEnabled = true }
                };
                child1.SetSelectedWithoutNotify(true);
                child2.SetSelectedWithoutNotify(false);

                var childItems = new List<IConverterItemState> { child1, child2 };

                var selectionState = CheckboxStateHelper.CalculateSelectionState(childItems);

                Assert.IsNull(selectionState);
            }
        }

        [TestFixture]
        internal class CompareToTests
        {
            [Test]
            public void CompareTo_LeafToFolder_ReturnsPositive()
            {
                var leaf = new ConverterItemState
                {
                    item = new MockConverterItem { name = "A" }
                };
                var folder = new FolderItemState
                {
                    item = new MockFolderItem { name = "B" }
                };

                var result = leaf.CompareTo(folder);

                Assert.Greater(result, 0, "Leaf should sort after folder");
            }

            [Test]
            public void CompareTo_FolderToLeaf_ReturnsNegative()
            {
                var folder = new FolderItemState
                {
                    item = new MockFolderItem { name = "B" }
                };
                var leaf = new ConverterItemState
                {
                    item = new MockConverterItem { name = "A" }
                };

                var result = folder.CompareTo(leaf);

                Assert.Less(result, 0, "Folder should sort before leaf");
            }

            [Test]
            public void CompareTo_LeafToLeaf_UsesNameComparison()
            {
                var leafA = new ConverterItemState
                {
                    item = new MockConverterItem { name = "Alpha" }
                };
                var leafB = new ConverterItemState
                {
                    item = new MockConverterItem { name = "Beta" }
                };

                var result = leafA.CompareTo(leafB);

                Assert.Less(result, 0, "Alpha should sort before Beta");
            }

            [Test]
            public void CompareTo_FolderToFolder_UsesNameComparison()
            {
                var folderA = new FolderItemState
                {
                    item = new MockFolderItem { name = "Alpha" }
                };
                var folderB = new FolderItemState
                {
                    item = new MockFolderItem { name = "Beta" }
                };

                var result = folderA.CompareTo(folderB);

                Assert.Less(result, 0, "Alpha should sort before Beta");
            }

            [Test]
            public void CompareTo_EnsuresFolderFirstOrdering()
            {
                var items = new List<IConverterItemState>
                {
                    new ConverterItemState { item = new MockConverterItem { name = "Leaf1" } },
                    new FolderItemState { item = new MockFolderItem { name = "FolderZ" } },
                    new ConverterItemState { item = new MockConverterItem { name = "Leaf2" } },
                    new FolderItemState { item = new MockFolderItem { name = "FolderA" } }
                };

                items.Sort((a, b) => a.CompareTo(b));

                Assert.IsTrue(items[0].IsFolder(), "First item should be a folder");
                Assert.IsTrue(items[1].IsFolder(), "Second item should be a folder");
                Assert.IsFalse(items[2].IsFolder(), "Third item should be a leaf");
                Assert.IsFalse(items[3].IsFolder(), "Fourth item should be a leaf");
                Assert.AreEqual("FolderA", items[0].item.name, "Folders should be sorted alphabetically");
                Assert.AreEqual("FolderZ", items[1].item.name);
                Assert.AreEqual("Leaf1", items[2].item.name, "Leaves should be sorted alphabetically");
                Assert.AreEqual("Leaf2", items[3].item.name);
            }
        }

        [TestFixture]
        internal class FolderItemStateTests
        {
            [Test]
            public void SetSelectedWithoutNotify_PropagatesToChildren()
            {
                var child1 = new ConverterItemState
                {
                    item = new MockConverterItem { isEnabled = true }
                };
                var child2 = new ConverterItemState
                {
                    item = new MockConverterItem { isEnabled = true }
                };
                var folder = new FolderItemState
                {
                    item = new MockFolderItem { isEnabled = true }
                };
                folder.children.Add(child1);
                folder.children.Add(child2);

                folder.SetSelectedWithoutNotify(true);

                Assert.IsTrue(child1.isSelected ?? false);
                Assert.IsTrue(child2.isSelected ?? false);
            }

            [Test]
            public void OnChildSelectionChanged_UpdatesParentToIndeterminate()
            {
                var child1 = new ConverterItemState
                {
                    item = new MockConverterItem { isEnabled = true }
                };
                var child2 = new ConverterItemState
                {
                    item = new MockConverterItem { isEnabled = true }
                };
                var folder = new FolderItemState
                {
                    item = new MockFolderItem { isEnabled = true }
                };
                folder.children.Add(child1);
                folder.children.Add(child2);
                folder.SubscribeToChildren();

                child1.SetSelectedWithoutNotify(true);
                child2.SetSelectedWithoutNotify(false);

                // Trigger child selection changed
                child1.isSelected = true;

                Assert.IsNull(folder.isSelected, "Parent should be indeterminate (null) when children have mixed selection");
            }
        }
    }
}
