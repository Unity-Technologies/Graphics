using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardProvider
    {
        readonly GraphData m_Graph;
        public static readonly Texture2D exposedIcon = Resources.Load<Texture2D>("GraphView/Nodes/BlackboardFieldExposed");
        readonly Dictionary<Guid, BlackboardRow> m_PropertyRows;
        readonly BlackboardSection m_Section;
        //WindowDraggable m_WindowDraggable;
        //ResizeBorderFrame m_ResizeBorderFrame;
        public Blackboard blackboard { get; private set; }
        Label m_PathLabel;
        TextField m_PathLabelTextField;
        bool m_EditPathCancelled = false;
        List<Node> m_SelectedNodes = new List<Node>();

        //public Action onDragFinished
        //{
        //    get { return m_WindowDraggable.OnDragFinished; }
        //    set { m_WindowDraggable.OnDragFinished = value; }
        //}

        //public Action onResizeFinished
        //{
        //    get { return m_ResizeBorderFrame.OnResizeFinished; }
        //    set { m_ResizeBorderFrame.OnResizeFinished = value; }
        //}

        Dictionary<AbstractShaderProperty, bool> m_ExpandedProperties = new Dictionary<AbstractShaderProperty, bool>();

        public Dictionary<AbstractShaderProperty, bool> expandedProperties
        {
            get { return m_ExpandedProperties; }
        }

        public string assetName
        {
            get { return blackboard.title; }
            set
            {
                blackboard.title = value;
            }
        }

        public BlackboardProvider(GraphData graph)
        {
            m_Graph = graph;
            m_PropertyRows = new Dictionary<Guid, BlackboardRow>();

            blackboard = new Blackboard()
            {
                scrollable = true,
                subTitle = FormatPath(graph.path),
                editTextRequested = EditTextRequested,
                addItemRequested = AddItemRequested,
                moveItemRequested = MoveItemRequested
            };

            m_PathLabel = blackboard.hierarchy.ElementAt(0).Q<Label>("subTitleLabel");
            m_PathLabel.RegisterCallback<MouseDownEvent>(OnMouseDownEvent);

            m_PathLabelTextField = new TextField { visible = false };
            m_PathLabelTextField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(e => { OnEditPathTextFinished(); });
            m_PathLabelTextField.Q("unity-text-input").RegisterCallback<KeyDownEvent>(OnPathTextFieldKeyPressed);
            blackboard.hierarchy.Add(m_PathLabelTextField);

            // m_WindowDraggable = new WindowDraggable(blackboard.shadow.Children().First().Q("header"));
            // blackboard.AddManipulator(m_WindowDraggable);

            // m_ResizeBorderFrame = new ResizeBorderFrame(blackboard) { name = "resizeBorderFrame" };
            // blackboard.shadow.Add(m_ResizeBorderFrame);

            m_Section = new BlackboardSection { headerVisible = false };
            foreach (var property in graph.properties)
                AddProperty(property);
            blackboard.Add(m_Section);
        }

        void OnDragUpdatedEvent(DragUpdatedEvent evt)
        {
            if (m_SelectedNodes.Any())
            {
                foreach (var node in m_SelectedNodes)
                {
                    node.RemoveFromClassList("hovered");
                }
                m_SelectedNodes.Clear();
            }
        }

        void OnMouseDownEvent(MouseDownEvent evt)
        {
            if (evt.clickCount == 2 && evt.button == (int)MouseButton.LeftMouse)
            {
                StartEditingPath();
                evt.PreventDefault();
            }
        }

        void StartEditingPath()
        {
            m_PathLabelTextField.visible = true;

            m_PathLabelTextField.value = m_PathLabel.text;
            m_PathLabelTextField.style.position = Position.Absolute;
            var rect = m_PathLabel.ChangeCoordinatesTo(blackboard, new Rect(Vector2.zero, m_PathLabel.layout.size));
            m_PathLabelTextField.style.left = rect.xMin;
            m_PathLabelTextField.style.top = rect.yMin;
            m_PathLabelTextField.style.width = rect.width;
            m_PathLabelTextField.style.fontSize = 11;
            m_PathLabelTextField.style.marginLeft = 0;
            m_PathLabelTextField.style.marginRight = 0;
            m_PathLabelTextField.style.marginTop = 0;
            m_PathLabelTextField.style.marginBottom = 0;

            m_PathLabel.visible = false;

            m_PathLabelTextField.Q("unity-text-input").Focus();
            m_PathLabelTextField.SelectAll();
        }

        void OnPathTextFieldKeyPressed(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    m_EditPathCancelled = true;
                    m_PathLabelTextField.Q("unity-text-input").Blur();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    m_PathLabelTextField.Q("unity-text-input").Blur();
                    break;
                default:
                    break;
            }
        }

        void OnEditPathTextFinished()
        {
            m_PathLabel.visible = true;
            m_PathLabelTextField.visible = false;

            var newPath = m_PathLabelTextField.text;
            if (!m_EditPathCancelled && (newPath != m_PathLabel.text))
            {
                newPath = SanitizePath(newPath);
            }

            m_Graph.path = newPath;
            m_PathLabel.text = FormatPath(newPath);
            m_EditPathCancelled = false;
        }

        static string FormatPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "—";
            return path;
        }

        static string SanitizePath(string path)
        {
            var splitString = path.Split('/');
            List<string> newStrings = new List<string>();
            foreach (string s in splitString)
            {
                var str = s.Trim();
                if (!string.IsNullOrEmpty(str))
                {
                    newStrings.Add(str);
                }
            }

            return string.Join("/", newStrings.ToArray());
        }

        void MoveItemRequested(Blackboard blackboard, int newIndex, VisualElement visualElement)
        {
            var property = visualElement.userData as AbstractShaderProperty;
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
            gm.AddItem(new GUIContent("Texture2D"), false, () => AddProperty(new TextureShaderProperty(), true));
            gm.AddItem(new GUIContent("Texture2D Array"), false, () => AddProperty(new Texture2DArrayShaderProperty(), true));
            gm.AddItem(new GUIContent("Texture3D"), false, () => AddProperty(new Texture3DShaderProperty(), true));
            gm.AddItem(new GUIContent("Cubemap"), false, () => AddProperty(new CubemapShaderProperty(), true));
            gm.AddItem(new GUIContent("Boolean"), false, () => AddProperty(new BooleanShaderProperty(), true));
            gm.AddItem(new GUIContent("Matrix2x2"), false, () => AddProperty(new Matrix2ShaderProperty(), true));
            gm.AddItem(new GUIContent("Matrix3x3"), false, () => AddProperty(new Matrix3ShaderProperty(), true));
            gm.AddItem(new GUIContent("Matrix4x4"), false, () => AddProperty(new Matrix4ShaderProperty(), true));
            gm.AddItem(new GUIContent("SamplerState"), false, () => AddProperty(new SamplerStateShaderProperty(), true));
            gm.AddItem(new GUIContent("Gradient"), false, () => AddProperty(new GradientShaderProperty(), true));
            gm.ShowAsContext();
        }

        void EditTextRequested(Blackboard blackboard, VisualElement visualElement, string newText)
        {
            var field = (BlackboardField)visualElement;
            var property = (AbstractShaderProperty)field.userData;
            if (!string.IsNullOrEmpty(newText) && newText != property.displayName)
            {
                m_Graph.owner.RegisterCompleteObjectUndo("Edit Property Name");
                newText = m_Graph.SanitizePropertyName(newText, property.guid);
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

            foreach (var propertyDict in expandedProperties)
            {
                SessionState.SetBool(propertyDict.Key.guid.ToString(), propertyDict.Value);
            }

            if (m_Graph.movedProperties.Any())
            {
                foreach (var row in m_PropertyRows.Values)
                    row.RemoveFromHierarchy();

                foreach (var property in m_Graph.properties)
                    m_Section.Add(m_PropertyRows[property.guid]);
            }
            m_ExpandedProperties.Clear();
        }

        void AddProperty(AbstractShaderProperty property, bool create = false, int index = -1)
        {
            if (m_PropertyRows.ContainsKey(property.guid))
                return;

            if (create)
                property.displayName = m_Graph.SanitizePropertyName(property.displayName);

            var icon = (m_Graph.isSubGraph || (property.isExposable && property.generatePropertyBlock)) ? exposedIcon : null;
            var field = new BlackboardField(icon, property.displayName, property.propertyType.ToString()) { userData = property };

            var propertyView = new BlackboardFieldPropertyView(field, m_Graph, property);
            var row = new BlackboardRow(field, propertyView);
            var pill = row.Q<Pill>();
            pill.RegisterCallback<MouseEnterEvent>(evt => OnMouseHover(evt, property));
            pill.RegisterCallback<MouseLeaveEvent>(evt => OnMouseHover(evt, property));
            pill.RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);

            var expandButton = row.Q<Button>("expandButton");
            expandButton.RegisterCallback<MouseDownEvent>(evt => OnExpanded(evt, property), TrickleDown.TrickleDown);

            row.userData = property;
            if (index < 0)
                index = m_PropertyRows.Count;
            if (index == m_PropertyRows.Count)
                m_Section.Add(row);
            else
                m_Section.Insert(index, row);
            m_PropertyRows[property.guid] = row;

            m_PropertyRows[property.guid].expanded = SessionState.GetBool(property.guid.ToString(), true);

            if (create)
            {
                row.expanded = true;
                m_Graph.owner.RegisterCompleteObjectUndo("Create Property");
                m_Graph.AddShaderProperty(property);
                field.OpenTextEditor();
            }
        }

        void OnExpanded(MouseDownEvent evt, AbstractShaderProperty property)
        {
            m_ExpandedProperties[property] = !m_PropertyRows[property.guid].expanded;
        }

        void DirtyNodes()
        {
            foreach (var node in m_Graph.GetNodes<PropertyNode>())
            {
                node.OnEnable();
                node.Dirty(ModificationScope.Node);
            }
        }

        public BlackboardRow GetBlackboardRow(Guid guid)
        {
            return m_PropertyRows[guid];
        }

        void OnMouseHover(EventBase evt, AbstractShaderProperty property)
        {
            var graphView = blackboard.GetFirstAncestorOfType<MaterialGraphView>();
            if (evt.eventTypeId == MouseEnterEvent.TypeId())
            {
                foreach (var node in graphView.nodes.ToList())
                {
                    if (node.userData is PropertyNode propertyNode)
                    {
                        if (propertyNode.propertyGuid == property.guid)
                        {
                            m_SelectedNodes.Add(node);
                            node.AddToClassList("hovered");
                        }
                    }
                }
            }
            else if (evt.eventTypeId == MouseLeaveEvent.TypeId() && m_SelectedNodes.Any())
            {
                foreach (var node in m_SelectedNodes)
                {
                    node.RemoveFromClassList("hovered");
                }
                m_SelectedNodes.Clear();
            }
        }
    }
}
