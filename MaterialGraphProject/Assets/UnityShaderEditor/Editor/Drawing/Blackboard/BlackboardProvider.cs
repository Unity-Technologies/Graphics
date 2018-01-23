using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Inspector;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardProvider
    {
        readonly string m_AssetName;
        readonly AbstractMaterialGraph m_Graph;
        readonly Texture2D m_ExposedIcon;
        readonly Dictionary<Guid, BlackboardRow> m_PropertyRows;
        readonly BlackboardSection m_Section;
        public Blackboard blackboard { get; private set; }
        public Action updateAssetRequested { get; set; }

        public BlackboardProvider(string assetName, AbstractMaterialGraph graph)
        {
            m_AssetName = assetName;
            m_Graph = graph;
            m_ExposedIcon = Resources.Load("GraphView/Nodes/BlackboardFieldExposed") as Texture2D;
            m_PropertyRows = new Dictionary<Guid, BlackboardRow>();

            blackboard = new Blackboard()
            {
                scrollable = true,
                title = assetName,
                editTextRequested = EditTextRequested,
                addItemRequested = AddItemRequested,
                moveItemRequested = MoveItemRequested
            };

            m_Section = new BlackboardSection { headerVisible = false };
            m_Section.Add(new Button(OnClickSave) { text = "Save" });
            foreach (var property in graph.properties)
                AddProperty(property);
            blackboard.Add(m_Section);
        }

        void MoveItemRequested(Blackboard blackboard, int i, VisualElement visualElement)
        {
            Debug.Log(i);
        }

        void AddItemRequested(Blackboard blackboard)
        {
            var gm = new GenericMenu();
            gm.AddItem(new GUIContent("Float"), false, () => AddProperty(new FloatShaderProperty(), true));
            gm.AddItem(new GUIContent("Vector2"), false, () => AddProperty(new Vector2ShaderProperty(), true));
            gm.AddItem(new GUIContent("Vector3"), false, () => AddProperty(new Vector3ShaderProperty(), true));
            gm.AddItem(new GUIContent("Vector4"), false, () => AddProperty(new Vector4ShaderProperty(), true));
            gm.AddItem(new GUIContent("Color"), false, () => AddProperty(new ColorShaderProperty(), true));
            gm.AddItem(new GUIContent("Texture"), false, () => AddProperty(new TextureShaderProperty(), true));
            gm.AddItem(new GUIContent("Cubemap"), false, () => AddProperty(new CubemapShaderProperty(), true));
            gm.ShowAsContext();
        }

        void EditTextRequested(Blackboard blackboard, VisualElement visualElement, string newText)
        {
            var field = (BlackboardField)visualElement;
            var property = (IShaderProperty)field.userData;
            if (newText != property.displayName)
            {
                property.displayName = newText;
                field.text = newText;
                DirtyNodes();
            }
        }

        void OnClickSave()
        {
            if (updateAssetRequested != null)
                updateAssetRequested();
        }

        public void HandleGraphChanges()
        {
            foreach (var propertyGuid in m_Graph.removedProperties)
            {
                BlackboardRow row;
                if (m_PropertyRows.TryGetValue(propertyGuid, out row))
                {
                    row.RemoveFromHierarchy();
                    m_PropertyRows.Remove(propertyGuid);
                }
            }

            foreach (var property in m_Graph.addedProperties)
                AddProperty(property);
        }

        void AddProperty(IShaderProperty property, bool focus = false)
        {
            if (m_PropertyRows.ContainsKey(property.guid))
                throw new ArgumentException("Property already exists");
            var field = new BlackboardField(m_ExposedIcon, property.displayName, property.propertyType.ToString()) { userData = property };
            var row = new BlackboardRow(field, new BlackboardFieldPropertyView(m_Graph, property));
            m_Section.Add(row);
            m_PropertyRows[property.guid] = row;

            if (focus)
                field.RenameGo();
        }

        void DirtyNodes()
        {
            foreach (var node in m_Graph.GetNodes<PropertyNode>())
                node.Dirty(ModificationScope.Node);
        }
    }
}
