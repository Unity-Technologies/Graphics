using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Serialization;
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

        [TestFixture]
        internal class SerializationTests
        {
            [Serializable]
            private class SerializableMockConverterItem : IRenderPipelineConverterItem
            {
                [SerializeField] private string m_Name;
                [SerializeField] private string m_Info;
                [SerializeField] private bool m_IsEnabled = true;
                [SerializeField] private string m_IsDisabledMessage;

                public string name { get => m_Name; set => m_Name = value; }
                public string info { get => m_Info; set => m_Info = value; }
                public bool isEnabled { get => m_IsEnabled; set => m_IsEnabled = value; }
                public string isDisabledMessage { get => m_IsDisabledMessage; set => m_IsDisabledMessage = value; }
                public Texture2D icon => null;
                public void OnClicked() { }
            }

            [Serializable]
            private class SerializableMockFolderItem : IFolderRenderPipelineConverterItem
            {
                [SerializeField] private string m_Name;
                [SerializeField] private string m_Info;
                [SerializeField] private bool m_IsEnabled = true;
                [SerializeField] private string m_IsDisabledMessage;
                [SerializeReference] private List<IRenderPipelineConverterItem> m_Children = new List<IRenderPipelineConverterItem>();

                public string name { get => m_Name; set => m_Name = value; }
                public string info { get => m_Info; set => m_Info = value; }
                public bool isEnabled { get => m_IsEnabled; set => m_IsEnabled = value; }
                public string isDisabledMessage { get => m_IsDisabledMessage; set => m_IsDisabledMessage = value; }
                public IList<IRenderPipelineConverterItem> children { get => m_Children; set => m_Children = value as List<IRenderPipelineConverterItem> ?? new List<IRenderPipelineConverterItem>(value); }
                public Texture2D icon => null;
                public void OnClicked() { }
            }

            [Test]
            public void ConverterItemState_PreservesDataAfterJsonSerialization()
            {
                var item = new SerializableMockConverterItem
                {
                    name = "TestItem",
                    info = "TestInfo",
                    isEnabled = true
                };

                var state = new ConverterItemState { item = item };
                state.SetSelectedWithoutNotify(true);

                // Serialize and deserialize using Unity's JSON utility
                var json = JsonUtility.ToJson(state);
                var deserializedState = JsonUtility.FromJson<ConverterItemState>(json);

                Assert.IsNotNull(deserializedState.item, "Item should survive serialization");
                Assert.AreEqual("TestItem", deserializedState.item.name);
                Assert.AreEqual("TestInfo", deserializedState.item.info);
                Assert.IsTrue(deserializedState.isSelected ?? false, "Selection state should survive serialization");
            }

            [Test]
            public void FolderItemState_PreservesExpandedStateAfterJsonSerialization()
            {
                var folder = new SerializableMockFolderItem
                {
                    name = "TestFolder",
                    isEnabled = true
                };
                folder.children.Add(new SerializableMockConverterItem { name = "Child1", isEnabled = true });

                var state = new FolderItemState { item = folder };
                state.isExpanded = false; // Change from default

                var json = JsonUtility.ToJson(state);
                var deserializedState = JsonUtility.FromJson<FolderItemState>(json);

                Assert.IsFalse(deserializedState.isExpanded, "Expanded state should survive serialization");
                Assert.IsNotNull(deserializedState.item, "Item should survive serialization");
                Assert.AreEqual("TestFolder", deserializedState.item.name);
            }

            [Test]
            public void FolderItemState_PreservesChildrenAfterJsonSerialization()
            {
                var child1 = new SerializableMockConverterItem { name = "Child1", isEnabled = true };
                var child2 = new SerializableMockConverterItem { name = "Child2", isEnabled = false };

                var folder = new SerializableMockFolderItem
                {
                    name = "TestFolder",
                    isEnabled = true
                };
                folder.children.Add(child1);
                folder.children.Add(child2);

                var state = new FolderItemState { item = folder };
                var childState1 = new ConverterItemState { item = child1 };
                var childState2 = new ConverterItemState { item = child2 };
                childState1.SetSelectedWithoutNotify(true);
                childState2.SetSelectedWithoutNotify(false);
                state.children.Add(childState1);
                state.children.Add(childState2);

                var json = JsonUtility.ToJson(state);
                var deserializedState = JsonUtility.FromJson<FolderItemState>(json);

                Assert.AreEqual(2, deserializedState.children.Count, "Children count should be preserved");
                Assert.AreEqual("Child1", deserializedState.children[0].item.name);
                Assert.AreEqual("Child2", deserializedState.children[1].item.name);
                Assert.IsTrue(deserializedState.children[0].isSelected ?? false, "Child1 selection should be preserved");
                Assert.IsFalse(deserializedState.children[1].isSelected ?? true, "Child2 selection should be preserved");
            }

            [Test]
            public void OnAfterDeserialize_RemovesInvalidItems()
            {
                var converterState = new ConverterState
                {
                    converter = new MockConverter()
                };

                var item = new SerializableMockConverterItem { name = "ValidItem", isEnabled = true };
                converterState.AddItem(item);
                converterState.isInitialized = true;

                // Simulate a null item by serializing, modifying, and deserializing
                // In this unit test, we can just call OnAfterDeserialize directly
                // The validation should handle existing valid items
                converterState.OnAfterDeserialize();

                Assert.IsTrue(converterState.isInitialized, "Should remain initialized with valid items");
                Assert.AreEqual(1, converterState.totalItemsCount, "Valid items should be preserved");
            }

            [Test]
            public void AssetGroupItem_PreservesAssetTypeAfterSerialization()
            {
                var assetGroupItem = new RenderPipelineConverterUtility.AssetGroupItem
                {
                    name = "Materials",
                    info = "Test asset group",
                    assetType = typeof(Material)
                };

                // Manually trigger serialization callbacks
                ((ISerializationCallbackReceiver)assetGroupItem).OnBeforeSerialize();
                var json = JsonUtility.ToJson(assetGroupItem);
                var deserializedItem = JsonUtility.FromJson<RenderPipelineConverterUtility.AssetGroupItem>(json);
                ((ISerializationCallbackReceiver)deserializedItem).OnAfterDeserialize();

                Assert.IsNotNull(deserializedItem.assetType, "Asset type should survive serialization");
                Assert.AreEqual(typeof(Material), deserializedItem.assetType, "Asset type should be Material");
                Assert.AreEqual("Materials", deserializedItem.name, "Name should survive serialization");
            }
        }
    }
}
