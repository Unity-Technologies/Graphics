using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardProvider
    {
        readonly AbstractMaterialGraph m_Graph;
        readonly Texture2D m_ExposedIcon;
        readonly Dictionary<Guid, BlackboardRow> m_PropertyRows;
        readonly BlackboardSection m_Section;
        WindowDraggable m_WindowDraggable;
        ResizeBorderFrame m_ResizeBorderFrame;
        public Blackboard blackboard { get; private set; }

        public Action onDragFinished
        {
            get { return m_WindowDraggable.OnDragFinished; }
            set { m_WindowDraggable.OnDragFinished = value; }
        }

        public Action onResizeFinished
        {
            get { return m_ResizeBorderFrame.OnResizeFinished; }
            set { m_ResizeBorderFrame.OnResizeFinished = value; }
        }

        public BlackboardProvider(string assetName, AbstractMaterialGraph graph)
        {
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

            m_WindowDraggable = new WindowDraggable(blackboard.shadow.Children().First().Q("header"));
            blackboard.AddManipulator(m_WindowDraggable);

            m_ResizeBorderFrame = new ResizeBorderFrame(blackboard) { name = "resizeBorderFrame" };
            blackboard.shadow.Add(m_ResizeBorderFrame);

            m_Section = new BlackboardSection { headerVisible = false };
            foreach (var property in graph.properties)
                AddProperty(property);
            blackboard.Add(m_Section);
        }

        void MoveItemRequested(Blackboard blackboard, int newIndex, VisualElement visualElement)
        {
            var property = visualElement.userData as IShaderProperty;
            if (property == null)
                return;
            m_Graph.owner.RegisterCompleteObjectUndo("Move Property");
            m_Graph.MoveShaderProperty(property, newIndex);
        }

        void AddItemRequested(Blackboard blackboard)
        {
            var gm = new GenericMenu();
            gm.AddItem(new GUIContent("Vector1"), false, () => AddProperty(new Vector1ShaderProperty(), true));
            gm.AddItem(new GUIContent("Vector2"), false, () => AddProperty(new Vector2ShaderProperty(), true));
            gm.AddItem(new GUIContent("Vector3"), false, () => AddProperty(new Vector3ShaderProperty(), true));
            gm.AddItem(new GUIContent("Vector4"), false, () => AddProperty(new Vector4ShaderProperty(), true));
            gm.AddItem(new GUIContent("Color"), false, () => AddProperty(new ColorShaderProperty(), true));
            gm.AddItem(new GUIContent("Texture"), false, () => AddProperty(new TextureShaderProperty(), true));
            gm.AddItem(new GUIContent("Cubemap"), false, () => AddProperty(new CubemapShaderProperty(), true));
            gm.AddItem(new GUIContent("Boolean"), false, () => AddProperty(new BooleanShaderProperty(), true));
            gm.ShowAsContext();
        }

        void EditTextRequested(Blackboard blackboard, VisualElement visualElement, string newText)
        {
            var field = (BlackboardField)visualElement;
            var property = (IShaderProperty)field.userData;
            if (newText != property.displayName)
            {
                m_Graph.owner.RegisterCompleteObjectUndo("Edit Property Name");
                property.displayName = newText;
                field.text = newText;
                DirtyNodes();
            }
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
                AddProperty(property, index: m_Graph.GetShaderPropertyIndex(property));

            if (m_Graph.movedProperties.Any())
            {
                foreach (var row in m_PropertyRows.Values)
                    row.RemoveFromHierarchy();

                foreach (var property in m_Graph.properties)
                    m_Section.Add(m_PropertyRows[property.guid]);
            }
        }

        void AddProperty(IShaderProperty property, bool create = false, int index = -1)
        {
            if (m_PropertyRows.ContainsKey(property.guid))
                return;
            var field = new BlackboardField(m_ExposedIcon, property.displayName, property.propertyType.ToString()) { userData = property };
            var row = new BlackboardRow(field, new BlackboardFieldPropertyView(m_Graph, property));
            row.userData = property;
            if (index < 0)
                index = m_PropertyRows.Count;
            if (index == m_PropertyRows.Count)
                m_Section.Add(row);
            else
                m_Section.Insert(index, row);
            m_PropertyRows[property.guid] = row;

            if (create)
            {
                field.OpenTextEditor();
                row.expanded = true;
                m_Graph.owner.RegisterCompleteObjectUndo("Create Property");
                m_Graph.AddShaderProperty(property);
            }
        }

        void DirtyNodes()
        {
            foreach (var node in m_Graph.GetNodes<PropertyNode>())
            {
                node.OnEnable();
                node.Dirty(ModificationScope.Node);
            }
        }
    }
}
