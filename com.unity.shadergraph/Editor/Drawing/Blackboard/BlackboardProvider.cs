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
        readonly Dictionary<Guid, BlackboardRow> m_InputRows;
        readonly BlackboardSection m_PropertySection;
        readonly BlackboardSection m_KeywordSection;
        public Blackboard blackboard { get; private set; }
        Label m_PathLabel;
        TextField m_PathLabelTextField;
        bool m_EditPathCancelled = false;
        List<Node> m_SelectedNodes = new List<Node>();

        Dictionary<ShaderInput, bool> m_ExpandedInputs = new Dictionary<ShaderInput, bool>();

        public Dictionary<ShaderInput, bool> expandedInputs
        {
            get { return m_ExpandedInputs; }
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
            m_InputRows = new Dictionary<Guid, BlackboardRow>();

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

            m_PropertySection = new BlackboardSection { title = "Properties", headerVisible = true };
            m_KeywordSection = new BlackboardSection { title = "Keywords", headerVisible = true };
            foreach (var property in graph.inputs)
                CreateInput(property);
            blackboard.Add(m_PropertySection);
            blackboard.Add(m_KeywordSection);
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
            var input = visualElement.userData as ShaderInput;
            if (input == null)
                return;
            m_Graph.owner.RegisterCompleteObjectUndo("Move Graph Input");
            m_Graph.MoveGraphInput(input, newIndex);
        }

        void AddItemRequested(Blackboard blackboard)
        {
            var gm = new GenericMenu();
            gm.AddItem(new GUIContent("Vector1"), false, () => CreateInput(new Vector1ShaderProperty(), true));
            gm.AddItem(new GUIContent("Vector2"), false, () => CreateInput(new Vector2ShaderProperty(), true));
            gm.AddItem(new GUIContent("Vector3"), false, () => CreateInput(new Vector3ShaderProperty(), true));
            gm.AddItem(new GUIContent("Vector4"), false, () => CreateInput(new Vector4ShaderProperty(), true));
            gm.AddItem(new GUIContent("Color"), false, () => CreateInput(new ColorShaderProperty(), true));
            gm.AddItem(new GUIContent("Texture2D"), false, () => CreateInput(new TextureShaderProperty(), true));
            gm.AddItem(new GUIContent("Texture2D Array"), false, () => CreateInput(new Texture2DArrayShaderProperty(), true));
            gm.AddItem(new GUIContent("Texture3D"), false, () => CreateInput(new Texture3DShaderProperty(), true));
            gm.AddItem(new GUIContent("Cubemap"), false, () => CreateInput(new CubemapShaderProperty(), true));
            gm.AddItem(new GUIContent("Boolean"), false, () => CreateInput(new BooleanShaderProperty(), true));
            gm.AddItem(new GUIContent("Matrix2x2"), false, () => CreateInput(new Matrix2ShaderProperty(), true));
            gm.AddItem(new GUIContent("Matrix3x3"), false, () => CreateInput(new Matrix3ShaderProperty(), true));
            gm.AddItem(new GUIContent("Matrix4x4"), false, () => CreateInput(new Matrix4ShaderProperty(), true));
            gm.AddItem(new GUIContent("SamplerState"), false, () => CreateInput(new SamplerStateShaderProperty(), true));
            gm.AddItem(new GUIContent("Gradient"), false, () => CreateInput(new GradientShaderProperty(), true));
            gm.AddItem(new GUIContent("Keyword"), false, () => CreateInput(new ShaderKeyword(), true));
            gm.ShowAsContext();
        }

        void EditTextRequested(Blackboard blackboard, VisualElement visualElement, string newText)
        {
            var field = (BlackboardField)visualElement;
            var input = (ShaderInput)field.userData;
            if (!string.IsNullOrEmpty(newText) && newText != input.displayName)
            {
                m_Graph.owner.RegisterCompleteObjectUndo("Edit Graph Input Name");
                newText = m_Graph.SanitizeGraphInputName(newText, input.guid);
                input.displayName = newText;
                field.text = newText;
                DirtyNodes();
            }
        }

        public void HandleGraphChanges()
        {
            foreach (var propertyGuid in m_Graph.removedInputs)
            {
                BlackboardRow row;
                if (m_InputRows.TryGetValue(propertyGuid, out row))
                {
                    row.RemoveFromHierarchy();
                    m_InputRows.Remove(propertyGuid);
                }
            }

            foreach (var property in m_Graph.addedInputs)
                CreateInput(property, index: m_Graph.GetGraphInputIndex(property));

            foreach (var propertyDict in expandedInputs)
            {
                SessionState.SetBool(propertyDict.Key.guid.ToString(), propertyDict.Value);
            }

            if (m_Graph.movedInputs.Any())
            {
                foreach (var row in m_InputRows.Values)
                    row.RemoveFromHierarchy();

                foreach (var input in m_Graph.inputs)
                    AddInputRow(m_InputRows[input.guid]);
            }
            m_ExpandedInputs.Clear();
        }

        void CreateInput(ShaderInput input, bool create = false, int index = -1)
        {
            if (m_InputRows.ContainsKey(input.guid))
                return;

            if (create)
                input.displayName = m_Graph.SanitizeGraphInputName(input.displayName);

            BlackboardField field = null;
            BlackboardRow row = null;
            switch(input)
            {
                case AbstractShaderProperty property:
                    var properyIcon = (m_Graph.isSubGraph || (property.isExposable && property.generatePropertyBlock)) ? exposedIcon : null;
                    field = new BlackboardField(properyIcon, property.displayName, property.propertyType.ToString()) { userData = property };
                    var propertyView = new BlackboardFieldPropertyView(field, m_Graph, property);
                    row = new BlackboardRow(field, propertyView);
                    break;
                case ShaderKeyword keyword:
                    field = new BlackboardField(null, keyword.displayName, keyword.keywordType.ToString()) { userData = keyword };
                    var keywordView = new BlackboardFieldKeywordView(field, m_Graph, keyword);
                    row = new BlackboardRow(field, keywordView);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var pill = row.Q<Pill>();
            pill.RegisterCallback<MouseEnterEvent>(evt => OnMouseHover(evt, input));
            pill.RegisterCallback<MouseLeaveEvent>(evt => OnMouseHover(evt, input));
            pill.RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);

            var expandButton = row.Q<Button>("expandButton");
            expandButton.RegisterCallback<MouseDownEvent>(evt => OnExpanded(evt, input), TrickleDown.TrickleDown);

            row.userData = input;
            if (index < 0)
                index = m_InputRows.Count;
            if (index == m_InputRows.Count)
                AddInputRow(row);
            else
                InsertInputRow(row, index);
            m_InputRows[input.guid] = row;

            m_InputRows[input.guid].expanded = SessionState.GetBool(input.guid.ToString(), true);

            if (create)
            {
                row.expanded = true;
                m_Graph.owner.RegisterCompleteObjectUndo("Create Graph Input");
                m_Graph.AddGraphInput(input);
                field.OpenTextEditor();
            }
        }

        private void AddInputRow(BlackboardRow row)
        {
            switch(row.userData)
            {
                case AbstractShaderProperty property:
                    m_PropertySection.Add(row);
                    break;
                case ShaderKeyword keyword:
                    m_KeywordSection.Add(row);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void InsertInputRow(BlackboardRow row, int index)
        {
            switch(row.userData)
            {
                case AbstractShaderProperty property:
                    m_PropertySection.Insert(index, row);
                    break;
                case ShaderKeyword keyword:
                    m_KeywordSection.Insert(index, row);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void OnExpanded(MouseDownEvent evt, ShaderInput input)
        {
            m_ExpandedInputs[input] = !m_InputRows[input.guid].expanded;
        }

        void DirtyNodes()
        {
            foreach (var node in m_Graph.GetNodes<GraphInputNode>())
            {
                node.OnEnable();
                node.Dirty(ModificationScope.Node);
            }
        }

        public BlackboardRow GetBlackboardRow(Guid guid)
        {
            return m_InputRows[guid];
        }

        void OnMouseHover(EventBase evt, ShaderInput input)
        {
            var graphView = blackboard.GetFirstAncestorOfType<MaterialGraphView>();
            if (evt.eventTypeId == MouseEnterEvent.TypeId())
            {
                foreach (var node in graphView.nodes.ToList())
                {
                    if(input is AbstractShaderProperty property)
                    {
                        if (node.userData is GraphInputNode propertyNode)
                        {
                            if (propertyNode.graphInputGuid == input.guid)
                            {
                                m_SelectedNodes.Add(node);
                                node.AddToClassList("hovered");
                            }
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
