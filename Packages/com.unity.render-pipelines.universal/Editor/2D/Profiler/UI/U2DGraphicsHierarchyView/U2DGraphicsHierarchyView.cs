using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.Graphics.Profiler.UI
{
    [UxmlElement]
    [Serializable]
    partial class U2DGraphicsHierarchyView : VisualElement
    {
        const string k_UXML = "Packages/com.unity.render-pipelines.universal/Editor/2D/Profiler/UI/U2DGraphicsHierarchyView/U2DGraphicsHierarchyView.uxml";
        MultiColumnTreeView m_Table;
        List<TreeViewItemData<U2DGraphicsHierarchyNodeData>> m_Data = new();
        Label m_NoDataLabel;

        public U2DGraphicsHierarchyView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UXML);
            visualTree.CloneTree(this);
            m_Table = this.Q<MultiColumnTreeView>();
            m_Table.selectedIndicesChanged += OnSelectionChanged;
            m_NoDataLabel = this.Q<Label>("noDataLabel");
            SetupTable();
            ShowTable();
        }

        void OnSelectionChanged(IEnumerable<int> obj)
        {
            var cellData = m_Table.GetItemDataForIndex<U2DGraphicsHierarchyNodeData>(m_Table.selectedIndex);
            if (cellData != null)
            {
                var unityObject = EditorUtility.EntityIdToObject(cellData.entityId);
                if (unityObject != null)
                    Selection.activeObject = unityObject;
            }
        }

        void SetupTable()
        {
            if(EditorGUIUtility.isProSkin)
                m_Table.AddToClassList("dark");
            else
                m_Table.AddToClassList("light");

            m_Table.sortingMode = ColumnSortingMode.Custom;
            m_Table.columnSortingChanged += OnColumnSortingChanged;

            for(int i = 0; i < m_Table.columns.Count; ++i)
            {
                var column = m_Table.columns[i];

                if (column.name == "Name")
                {
                    column.bindCell = (element, i) =>
                    {
                        var label = element.Q<Label>();
                        var itemData = m_Table.GetItemDataForIndex<U2DGraphicsHierarchyNodeData>(i);
                        BindLabelToDataSource(label, column.bindingPath, itemData);
                        SetNameColumnCellIcon(element, itemData);
                    };
                }
                else
                {
                    column.bindCell = (element, i) =>
                    {
                        var itemData = m_Table.GetItemDataForIndex<U2DGraphicsHierarchyNodeData>(i);
                        var label = element.Q<Label>();
                        BindLabelToDataSource(label, column.bindingPath, itemData);
                    };
                }

                column.unbindCell = (element, _) =>
                {
                    var label = element.Q<Label>();
                    label.SetBinding("text", null);
                };

                column.makeCell = () =>
                {
                    var ve = new VisualElement();
                    ve.AddToClassList("cell");
                    var icon = new VisualElement(){name = "Icon"};
                    icon.AddToClassList("cell-icon");
                    var label = new Label();
                    label.AddToClassList("cell-label");
                    ve.Add(icon);
                    ve.Add(label);
                    return ve;
                };

                column.comparison = (a,b) =>
                {
                    return 0;
                };
            }
        }

        void OnColumnSortingChanged()
        {
            if(m_Table.sortedColumns != null)
            {
                List<TreeViewItemData<U2DGraphicsHierarchyNodeData>> sortedData = new();
                foreach(var child in m_Data)
                {
                    List<TreeViewItemData<U2DGraphicsHierarchyNodeData>> children = new();
                    foreach(var c in child.children)
                    {
                        children.Add(c);
                    }
                    children.Sort(SortData);
                    sortedData.Add(new TreeViewItemData<U2DGraphicsHierarchyNodeData>(child.id, child.data, children));
                }
                sortedData.Sort(SortData);
                m_Data.Clear();
                m_Data = sortedData;

                m_Table.Clear();
                HashSet<int> expandedIds = new();
                foreach(var d in m_Data)
                {
                    if(m_Table.IsExpanded(d.id))
                        expandedIds.Add(d.id);
                }
                m_Table.SetRootItems(m_Data);
                m_Table.Rebuild();
                foreach(var id in expandedIds)
                {
                    m_Table.ExpandItem(id);
                }
                ShowTable();
            }
        }

        int SortData(TreeViewItemData<U2DGraphicsHierarchyNodeData> a, TreeViewItemData<U2DGraphicsHierarchyNodeData> b)
        {
            using (var enumerator = m_Table.sortedColumns.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    int result = U2DGraphicsHierarchyNodeData.Compare(a.data, b.data, enumerator.Current.column.bindingPath);
                    if (result != 0)
                        return result * (enumerator.Current.direction == SortDirection.Ascending ? 1 : -1);
                }
            }

            return U2DGraphicsHierarchyNodeData.Compare(a.data, b.data, null);
        }

        void BindLabelToDataSource(Label label, string path, U2DGraphicsHierarchyNodeData cellData)
        {
            label.SetBinding("text", new DataBinding
            {
                dataSourcePath = new PropertyPath(path),
                bindingMode = BindingMode.ToTarget,
                dataSource = cellData
            });
        }

        void SetNameColumnCellIcon(VisualElement ele, U2DGraphicsHierarchyNodeData data)
        {
            var icon = ele.Q("Icon");
            icon.RemoveFromClassList("gameObject-icon");
            icon.RemoveFromClassList("shadowCaster-icon");
            icon.RemoveFromClassList("light-icon");
            icon.RemoveFromClassList("category-icon");
            if(data.icon?.Length > 0)
                icon.AddToClassList(data.icon);
        }

        public void SetData(List<U2DGraphicsHierarchyNodeData>[] categorizedData)
        {
            m_Table.Clear();
            HashSet<int> expandedIds = new();
            foreach(var item in m_Data)
            {
                if(m_Table.IsExpanded(item.id))
                    expandedIds.Add(item.id);
            }
            m_Data.Clear();

            if (categorizedData == null || categorizedData.Length < 2)
            {
                ShowTable();
                return;
            }

            int rootId = 1;
            int childId = 1000;

            // Shadow root node (index 0)
            var shadowList = categorizedData[0];
            if (shadowList != null && shadowList.Count > 0)
            {
                var shadowRoot = new U2DGraphicsHierarchyNodeData("Shadow", EntityId.None, 0, 0, rootId++, "category-icon");
                var shadowChildren = new List<TreeViewItemData<U2DGraphicsHierarchyNodeData>>();

                foreach (var node in shadowList)
                {
                    node.icon = "shadowCaster-icon";
                    shadowChildren.Add(new TreeViewItemData<U2DGraphicsHierarchyNodeData>(childId++, node));
                    shadowRoot.vertexCount += node.vertexCount;
                    shadowRoot.triangleCount += node.triangleCount;
                }

                if (m_Table.sortedColumns != null)
                {
                    shadowChildren.Sort(SortData);
                }
                m_Data.Add(new TreeViewItemData<U2DGraphicsHierarchyNodeData>(shadowRoot.id, shadowRoot, shadowChildren));
            }

            // Light root node (index 1)
            var lightList = categorizedData[1];
            if (lightList != null && lightList.Count > 0)
            {
                var lightRoot = new U2DGraphicsHierarchyNodeData("Light", EntityId.None, 0, 0, rootId++, "category-icon");
                var lightChildren = new List<TreeViewItemData<U2DGraphicsHierarchyNodeData>>();

                foreach (var node in lightList)
                {
                    node.icon = "light-icon";
                    lightChildren.Add(new TreeViewItemData<U2DGraphicsHierarchyNodeData>(childId++, node));
                    lightRoot.vertexCount += node.vertexCount;
                    lightRoot.triangleCount += node.triangleCount;
                }
                if (m_Table.sortedColumns != null)
                {
                    lightChildren.Sort(SortData);
                }
                m_Data.Add(new TreeViewItemData<U2DGraphicsHierarchyNodeData>(lightRoot.id, lightRoot, lightChildren));
            }
            if (m_Table.sortedColumns != null)
            {
                m_Data.Sort(SortData);
            }
            m_Table.SetRootItems(m_Data);
            m_Table.Rebuild();
            foreach(var id in expandedIds)
            {
                m_Table.ExpandItem(id);
            }
            ShowTable();
        }

        void ShowTable()
        {
            if(m_Data.Count == 0)
            {
                m_Table.style.display = DisplayStyle.None;
                m_NoDataLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_Table.style.display = DisplayStyle.Flex;
                m_NoDataLabel.style.display = DisplayStyle.None;
            }
        }
    }
}
