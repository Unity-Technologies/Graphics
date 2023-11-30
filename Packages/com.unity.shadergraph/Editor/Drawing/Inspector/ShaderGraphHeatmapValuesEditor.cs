using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Colors;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [CustomEditor(typeof(ShaderGraphHeatmapValues))]
    class ShaderGraphHeatmapValuesEditor : Editor
    {
        const string k_TemplatePath = "Packages/com.unity.shadergraph/Editor/Resources/UXML/HeatmapValuesEditor.uxml";
        const string k_StylePath = "Packages/com.unity.shadergraph/Editor/Resources/Styles/HeatmapValuesEditor.uss";

        const string k_ColorsListName = "colors-list";
        const string k_NodeListName = "nodes-list";
        const string k_SubgraphListName = "subgraph-list";
        const string k_NodeTitleColumnName = "node";
        const string k_SubgraphColumnName = "subgraph";
        const string k_HeatValueColumnName = "value";

        const string k_HeatFieldUssClassName = "sg-heatmap__heat-field";
        const string k_SubgraphPickerUssClassName = "sg-heatmap__subgraph-picker";
        const string k_NodeLabelUssClassName = "sg-heatmap__node-label";

        const string k_RefreshNodesHintName = "refresh-nodes-hint";
        const string k_RefreshNodesButtonName = "refresh-nodes-button";
        const string k_HelpBoxHiddenUssModifier = "sg-heatmap__help-box--hidden";

        ShaderGraphHeatmapValues HeatmapValuesTarget => target as ShaderGraphHeatmapValues;

        MultiColumnListView m_NodesListView;
        MultiColumnListView m_SubgraphListView;
        VisualElement m_RefreshNodesHint;

        internal static void UpdateShaderGraphWindows()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<MaterialGraphEditWindow>())
            {
                var graphEditorView = window.graphEditorView;
                if (graphEditorView == null)
                {
                    continue;
                }

                var colorManager = graphEditorView.colorManager;
                if (colorManager.activeProviderName != HeatmapColors.Title)
                {
                    continue;
                }

                var nodeList = graphEditorView.Query<MaterialNodeView>().ToList();
                colorManager.UpdateNodeViews(nodeList);
            }
        }

        static Dictionary<string, string> s_NodeTypeNamesToTitles;
        static Dictionary<string, string> NodeTypeNamesToTitles
        {
            get
            {
                if (s_NodeTypeNamesToTitles is null)
                {
                    s_NodeTypeNamesToTitles = new Dictionary<string, string>();
                    foreach (var knownNodeType in NodeClassCache.knownNodeTypes)
                    {
                        var node = (AbstractMaterialNode) Activator.CreateInstance(knownNodeType);
                        s_NodeTypeNamesToTitles[knownNodeType.Name] = node.name;
                    }
                }

                return s_NodeTypeNamesToTitles;
            }
        }

        static string GetTitleForNode(string nodeTypeName)
        {
            return string.IsNullOrEmpty(nodeTypeName)
                ? string.Empty
                : NodeTypeNamesToTitles.GetValueOrDefault(nodeTypeName, nodeTypeName);
        }

        static void ConfigureNodeLabelColumn(Column column, HeatmapEntries entries)
        {
            column.makeCell = () =>
            {
                var ve = new Label();
                ve.AddToClassList(k_NodeLabelUssClassName);
                return ve;
            };
            column.bindCell = (v, i) =>
            {
                var label = (Label) v;
                label.text = GetTitleForNode(entries.Entries[i].m_NodeName);
            };
        }

        void ConfigureSubgraphPickerColumn(Column column)
        {
            var serializedEntries = serializedObject.FindProperty("m_Subgraphs");
            var entries = HeatmapValuesTarget.Subgraphs;

            column.makeCell = () =>
            {
                var ve = new ObjectField
                {
                    tooltip = "Subgraph asset.",
                    objectType = typeof(SubGraphAsset),
                    allowSceneObjects = false,
                };

                ve.AddToClassList(k_SubgraphPickerUssClassName);
                return ve;
            };

            column.bindCell = (v, i) =>
            {
                var objectField = (ObjectField) v;
                objectField.RegisterCallback<ChangeEvent<UnityEngine.Object>, int>(OnSubgraphChanged, i);

                if (string.IsNullOrEmpty(entries.Entries[i].m_NodeName))
                {
                    return;
                }

                var assetPath = AssetDatabase.GUIDToAssetPath(entries.Entries[i].m_NodeName);
                var asset = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(assetPath);
                objectField.SetValueWithoutNotify(asset);
            };

            column.unbindCell = (v, i) =>
            {
                var objectField = (ObjectField) v;
                objectField.UnregisterCallback<ChangeEvent<UnityEngine.Object>, int>(OnSubgraphChanged);
            };

            return;

            void OnSubgraphChanged(ChangeEvent<UnityEngine.Object> changeEvent, int index)
            {
                if (changeEvent.newValue is not SubGraphAsset subgraph)
                {
                    return;
                }

                serializedEntries
                    .FindPropertyRelative("m_Entries")
                    .GetArrayElementAtIndex(index)
                    .FindPropertyRelative("m_NodeName")
                    .stringValue = subgraph.assetGuid;

                ApplyChanges();
            }
        }

        void ConfigureCategoryColumn(Column c, SerializedProperty serializedEntries, HeatmapEntries entries)
        {
            c.makeCell = () =>
            {
                var ve = new IntegerField
                {
                    tooltip = "Category assigned to this node, which determines its color.",
                    isDelayed = true,
                };

                ve.AddToClassList(k_HeatFieldUssClassName);
                return ve;
            };
            c.bindCell = (v, i) =>
            {
                var intField = (IntegerField) v;
                intField.SetValueWithoutNotify(entries.Entries[i].m_Category);
                intField.RegisterCallback<ChangeEvent<int>, int>(OnHeatChanged, i);
            };
            c.unbindCell = (v, i) =>
            {
                var intField = (IntegerField) v;
                intField.UnregisterCallback<ChangeEvent<int>, int>(OnHeatChanged);
            };

            return;

            void OnHeatChanged(ChangeEvent<int> changeEvent, int index)
            {
                serializedEntries
                    .FindPropertyRelative("m_Entries")
                    .GetArrayElementAtIndex(index)
                    .FindPropertyRelative("m_Category")
                    .intValue = changeEvent.newValue;

                ApplyChanges();
            }
        }

        void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        void OnUndoRedo()
        {
            // Target might not be what we expect if our inspector isn't focused.
            if (HeatmapValuesTarget != null)
            {
                m_RefreshNodesHint.EnableInClassList(k_HelpBoxHiddenUssModifier, HeatmapValuesTarget.ContainsAllApplicableNodes());
            }

            UpdateShaderGraphWindows();
        }

        void RefreshNodeListFromProject()
        {
            Undo.RecordObject(target, $"Refresh Node List in {target.name}");

            HeatmapValuesTarget.PopulateNodesFromProject();
            serializedObject.Update();

            m_NodesListView.RefreshItems();
            m_RefreshNodesHint.EnableInClassList(k_HelpBoxHiddenUssModifier, true);

            UpdateShaderGraphWindows();
        }

        void ApplyChanges()
        {
            serializedObject.ApplyModifiedProperties();
            UpdateShaderGraphWindows();
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TemplatePath).CloneTree(root);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(k_StylePath);
            root.styleSheets.Add(styleSheet);

            var colorsListView = root.Q<ListView>(k_ColorsListName);
            colorsListView.bindingPath = serializedObject.FindProperty(nameof(ShaderGraphHeatmapValues.m_Colors)).propertyPath;
            colorsListView.makeItem = () => new ColorField {showAlpha = false, hdr = false};
            colorsListView.bindItem = (v, i) =>
            {
                var colorField = (ColorField) v;
                colorField.label = $"Category {i}";
                colorField.tooltip = $"Color for category {i}.";
                colorField.RegisterValueChangedCallback(_ => ApplyChanges());

                // Assigning bindingPath makes itemsSource into a list of SerializedProperties, so we can bind directly.
                colorField.BindProperty((SerializedProperty) colorsListView.itemsSource[i]);
            };

            colorsListView.Bind(serializedObject);

            var nodeEntries = HeatmapValuesTarget.Nodes;
            m_NodesListView = root.Q<MultiColumnListView>(k_NodeListName);

            m_RefreshNodesHint = root.Q<HelpBox>(k_RefreshNodesHintName);
            m_RefreshNodesHint.EnableInClassList(k_HelpBoxHiddenUssModifier, HeatmapValuesTarget.ContainsAllApplicableNodes());
            m_RefreshNodesHint.Q<Button>(k_RefreshNodesButtonName).clicked += RefreshNodeListFromProject;

            var nodesSerializedProperty = serializedObject.FindProperty("m_Nodes");
            ConfigureNodeLabelColumn(m_NodesListView.columns[k_NodeTitleColumnName], nodeEntries);
            ConfigureCategoryColumn(m_NodesListView.columns[k_HeatValueColumnName], nodesSerializedProperty, nodeEntries);

            m_NodesListView.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Refresh Node List", _ => RefreshNodeListFromProject());
            }));

            // MultiColumnListView doesn't support binding as of writing, but we can still track property changes
            // to reflect undo, redo, reset, etc.
            m_NodesListView.TrackPropertyValue(nodesSerializedProperty, _ => { m_NodesListView.RefreshItems(); });
            m_NodesListView.itemsSource = nodeEntries.Entries;

            var subgraphEntries = HeatmapValuesTarget.Subgraphs;
            m_SubgraphListView = root.Q<MultiColumnListView>(k_SubgraphListName);

            var subgraphsSerializedProperty = serializedObject.FindProperty("m_Subgraphs");
            ConfigureSubgraphPickerColumn(m_SubgraphListView.columns[k_SubgraphColumnName]);
            ConfigureCategoryColumn(m_SubgraphListView.columns[k_HeatValueColumnName], subgraphsSerializedProperty, subgraphEntries);

            m_SubgraphListView.TrackPropertyValue(subgraphsSerializedProperty, _ => { m_SubgraphListView.RefreshItems(); });
            m_SubgraphListView.itemsSource = subgraphEntries.Entries;

            return root;
        }
    }
}
