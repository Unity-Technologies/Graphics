using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Rendering.Converter;

namespace UnityEditor.Rendering.Tests
{
    [TestFixture]
    internal class ConverterStateTests
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

        private class MockConverter : IRenderPipelineConverter
        {
            public bool isEnabled => true;
            public string isDisabledMessage => string.Empty;
            public void Scan(System.Action<List<IRenderPipelineConverterItem>> onScanFinish) { }
            public Status Convert(IRenderPipelineConverterItem item, out string message)
            {
                message = string.Empty;
                return Status.Success;
            }
        }

        [TestFixture]
        internal class FilterTreeNodeTests
        {
            [Test]
            public void ApplyFilter_FiltersByDisplayFilter()
            {
                var converterState = new ConverterState
                {
                    converter = new MockConverter()
                };

                var item1 = new MockConverterItem { name = "Item1", isEnabled = true };
                var item2 = new MockConverterItem { name = "Item2", isEnabled = true };

                converterState.AddItem(item1);
                converterState.AddItem(item2);

                // Set one item to have a warning status
                var allLeafItems = new List<ConverterItemState>(converterState.GetAllLeafItems());
                allLeafItems[0].conversionResult = (Status.Warning, "Warning message");
                allLeafItems[1].conversionResult = (Status.Success, "Success message");

                // Filter to only show warnings
                converterState.currentFilter = DisplayFilter.Warnings;
                converterState.ApplyFilter();

                Assert.AreEqual(1, ((IList<TreeViewItemData<TreeNodeData>>)converterState.filteredItemsTree).Count, "Should only show items with warnings");
            }

            [Test]
            public void ApplyFilter_FolderWithNoVisibleChildren_IsExcluded()
            {
                var converterState = new ConverterState
                {
                    converter = new MockConverter()
                };

                var folder = new MockFolderItem { name = "Folder", isEnabled = true };
                var child = new MockConverterItem { name = "Child", isEnabled = true };
                folder.children.Add(child);

                converterState.AddItem(folder);

                // Mark the child as success
                var allLeafItems = new List<ConverterItemState>(converterState.GetAllLeafItems());
                allLeafItems[0].conversionResult = (Status.Success, "Success");

                // Filter to only show errors (folder should be excluded)
                converterState.currentFilter = DisplayFilter.Errors;
                converterState.ApplyFilter();

                Assert.AreEqual(0, ((IList<TreeViewItemData<TreeNodeData>>)converterState.filteredItemsTree).Count, "Folder with no visible children should be excluded");
            }

            [Test]
            public void ApplyFilter_FolderWithVisibleChildren_IsIncluded()
            {
                var converterState = new ConverterState
                {
                    converter = new MockConverter()
                };

                var folder = new MockFolderItem { name = "Folder", isEnabled = true };
                var child1 = new MockConverterItem { name = "Child1", isEnabled = true };
                var child2 = new MockConverterItem { name = "Child2", isEnabled = true };
                folder.children.Add(child1);
                folder.children.Add(child2);

                converterState.AddItem(folder);

                // Mark children with different statuses
                var allLeafItems = new List<ConverterItemState>(converterState.GetAllLeafItems());
                allLeafItems[0].conversionResult = (Status.Error, "Error");
                allLeafItems[1].conversionResult = (Status.Success, "Success");

                // Filter to only show errors
                converterState.currentFilter = DisplayFilter.Errors;
                converterState.ApplyFilter();

                var filteredTree = (IList<TreeViewItemData<TreeNodeData>>)converterState.filteredItemsTree;
                Assert.AreEqual(1, filteredTree.Count, "Folder should be included");
                var folderNode = filteredTree[0];
                Assert.IsTrue(folderNode.data.isFolder);
                var folderChildren = new List<TreeViewItemData<TreeNodeData>>(folderNode.children);
                Assert.AreEqual(1, folderChildren.Count, "Folder should have 1 visible child");
            }

            [Test]
            public void ApplyFilter_AllFilter_ShowsAllItems()
            {
                var converterState = new ConverterState
                {
                    converter = new MockConverter()
                };

                var item1 = new MockConverterItem { name = "Item1", isEnabled = true };
                var item2 = new MockConverterItem { name = "Item2", isEnabled = true };
                var item3 = new MockConverterItem { name = "Item3", isEnabled = true };

                converterState.AddItem(item1);
                converterState.AddItem(item2);
                converterState.AddItem(item3);

                var allLeafItems = new List<ConverterItemState>(converterState.GetAllLeafItems());
                allLeafItems[0].conversionResult = (Status.Error, "Error");
                allLeafItems[1].conversionResult = (Status.Warning, "Warning");
                allLeafItems[2].conversionResult = (Status.Success, "Success");

                converterState.currentFilter = DisplayFilter.All;
                converterState.ApplyFilter();

                Assert.AreEqual(3, ((IList<TreeViewItemData<TreeNodeData>>)converterState.filteredItemsTree).Count, "All filter should show all items");
            }

            [Test]
            public void ApplyFilter_NestedFolders_FiltersCorrectly()
            {
                var converterState = new ConverterState
                {
                    converter = new MockConverter()
                };

                var rootFolder = new MockFolderItem { name = "Root", isEnabled = true };
                var childFolder = new MockFolderItem { name = "Child Folder", isEnabled = true };
                var leaf1 = new MockConverterItem { name = "Leaf1", isEnabled = true };
                var leaf2 = new MockConverterItem { name = "Leaf2", isEnabled = true };

                childFolder.children.Add(leaf1);
                childFolder.children.Add(leaf2);
                rootFolder.children.Add(childFolder);

                converterState.AddItem(rootFolder);

                var allLeafItems = new List<ConverterItemState>(converterState.GetAllLeafItems());
                allLeafItems[0].conversionResult = (Status.Error, "Error");
                allLeafItems[1].conversionResult = (Status.Success, "Success");

                converterState.currentFilter = DisplayFilter.Errors;
                converterState.ApplyFilter();

                var filteredTree = (IList<TreeViewItemData<TreeNodeData>>)converterState.filteredItemsTree;
                Assert.AreEqual(1, filteredTree.Count, "Root folder should be included");
                var rootNode = filteredTree[0];
                var rootChildren = new List<TreeViewItemData<TreeNodeData>>(rootNode.children);
                Assert.AreEqual(1, rootChildren.Count, "Root should have 1 child folder");
                var childNode = rootChildren[0];
                var childChildren = new List<TreeViewItemData<TreeNodeData>>(childNode.children);
                Assert.AreEqual(1, childChildren.Count, "Child folder should have 1 visible leaf");
            }
        }

        [TestFixture]
        internal class TreeBuildingTests
        {
            [Test]
            public void BuildFullTree_SortsItemsWithFoldersFirst()
            {
                var converterState = new ConverterState
                {
                    converter = new MockConverter()
                };

                // Add in mixed order: leaf, folder, leaf, folder
                var leaf1 = new MockConverterItem { name = "Zebra", isEnabled = true };
                var folder1 = new MockFolderItem { name = "Apple", isEnabled = true };
                var leaf2 = new MockConverterItem { name = "Alpha", isEnabled = true };
                var folder2 = new MockFolderItem { name = "Zulu", isEnabled = true };

                converterState.AddItem(leaf1);
                converterState.AddItem(folder1);
                converterState.AddItem(leaf2);
                converterState.AddItem(folder2);

                converterState.currentFilter = DisplayFilter.All;
                converterState.ApplyFilter();

                var filteredTree = (IList<TreeViewItemData<TreeNodeData>>)converterState.filteredItemsTree;
                // Empty folders are filtered out, so only leaf items remain
                Assert.AreEqual(2, filteredTree.Count);

                // Both should be leaves, sorted alphabetically
                Assert.IsFalse(filteredTree[0].data.isFolder);
                Assert.AreEqual("Alpha", filteredTree[0].data.displayName);

                Assert.IsFalse(filteredTree[1].data.isFolder);
                Assert.AreEqual("Zebra", filteredTree[1].data.displayName);
            }
        }
    }
}
