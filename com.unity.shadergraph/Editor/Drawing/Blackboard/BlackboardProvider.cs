using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Internal;
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

            m_PropertySection = new BlackboardSection { title = "Properties" };
            foreach (var property in graph.properties)
                AddInputRow(property);
            blackboard.Add(m_PropertySection);

            m_KeywordSection = new BlackboardSection { title = "Keywords" };
            foreach (var keyword in graph.keywords)
                AddInputRow(keyword);
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
            switch(input)
            {
                case AbstractShaderProperty property:
                    m_Graph.MoveProperty(property, newIndex);
                    break;
                case ShaderKeyword keyword:
                    m_Graph.MoveKeyword(keyword, newIndex);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void AddItemRequested(Blackboard blackboard)
        {
            var gm = new GenericMenu();
            AddPropertyItems(gm);
            AddKeywordItems(gm);
            gm.ShowAsContext();
        }

        void AddPropertyItems(GenericMenu gm)
        {
            gm.AddItem(new GUIContent($"Vector1"), false, () => AddInputRow(new Vector1ShaderProperty(), true));
            gm.AddItem(new GUIContent($"Vector2"), false, () => AddInputRow(new Vector2ShaderProperty(), true));
            gm.AddItem(new GUIContent($"Vector3"), false, () => AddInputRow(new Vector3ShaderProperty(), true));
            gm.AddItem(new GUIContent($"Vector4"), false, () => AddInputRow(new Vector4ShaderProperty(), true));
            gm.AddItem(new GUIContent($"Color"), false, () => AddInputRow(new ColorShaderProperty(), true));
            gm.AddItem(new GUIContent($"Texture2D"), false, () => AddInputRow(new Texture2DShaderProperty(), true));
            gm.AddItem(new GUIContent($"Texture2D Array"), false, () => AddInputRow(new Texture2DArrayShaderProperty(), true));
            gm.AddItem(new GUIContent($"Texture3D"), false, () => AddInputRow(new Texture3DShaderProperty(), true));
            gm.AddItem(new GUIContent($"Cubemap"), false, () => AddInputRow(new CubemapShaderProperty(), true));
            gm.AddItem(new GUIContent($"Boolean"), false, () => AddInputRow(new BooleanShaderProperty(), true));
            gm.AddItem(new GUIContent($"Matrix2x2"), false, () => AddInputRow(new Matrix2ShaderProperty(), true));
            gm.AddItem(new GUIContent($"Matrix3x3"), false, () => AddInputRow(new Matrix3ShaderProperty(), true));
            gm.AddItem(new GUIContent($"Matrix4x4"), false, () => AddInputRow(new Matrix4ShaderProperty(), true));
            gm.AddItem(new GUIContent($"SamplerState"), false, () => AddInputRow(new SamplerStateShaderProperty(), true));
            gm.AddItem(new GUIContent($"Gradient"), false, () => AddInputRow(new GradientShaderProperty(), true));
            gm.AddSeparator($"/");
        }

        void AddKeywordItems(GenericMenu gm)
        {
            gm.AddItem(new GUIContent($"Keyword/Boolean"), false, () => AddInputRow(new ShaderKeyword(KeywordType.Boolean), true));
            gm.AddItem(new GUIContent($"Keyword/Enum"), false, () => AddInputRow(new ShaderKeyword(KeywordType.Enum), true));
            gm.AddSeparator($"Keyword/");
            foreach (var builtinKeywordDescriptor in KeywordUtil.GetBuiltinKeywordDescriptors())
            {
                var keyword = ShaderKeyword.Create(builtinKeywordDescriptor);
                AddBuiltinKeyword(gm, keyword);
            }
        }

        void AddBuiltinKeyword(GenericMenu gm, ShaderKeyword keyword)
        {
            if(m_Graph.keywords.Where(x => x.referenceName == keyword.referenceName).Any())
            {
                gm.AddDisabledItem(new GUIContent($"Keyword/{keyword.displayName}"));
            }
            else
            {
                gm.AddItem(new GUIContent($"Keyword/{keyword.displayName}"), false, () => AddInputRow(keyword.Copy(), true));
            }
        }

        void EditTextRequested(Blackboard blackboard, VisualElement visualElement, string newText)
        {
            var field = (BlackboardField)visualElement;
            var input = (ShaderInput)field.userData;
            if (!string.IsNullOrEmpty(newText) && newText != input.displayName)
            {
                m_Graph.owner.RegisterCompleteObjectUndo("Edit Graph Input Name");
                input.displayName = newText;
                m_Graph.SanitizeGraphInputName(input);
                field.text = input.displayName;
                DirtyNodes();
            }
        }

        public void HandleGraphChanges()
        {
            foreach (var inputGuid in m_Graph.removedInputs)
            {
                BlackboardRow row;
                if (m_InputRows.TryGetValue(inputGuid, out row))
                {
                    row.RemoveFromHierarchy();
                    m_InputRows.Remove(inputGuid);
                }
            }

            foreach (var input in m_Graph.addedInputs)
                AddInputRow(input, index: m_Graph.GetGraphInputIndex(input));

            foreach (var expandedInput in expandedInputs)
            {
                SessionState.SetBool(expandedInput.Key.guid.ToString(), expandedInput.Value);
            }

            if (m_Graph.movedInputs.Any())
            {
                foreach (var row in m_InputRows.Values)
                    row.RemoveFromHierarchy();

                foreach (var property in m_Graph.properties)
                    m_PropertySection.Add(m_InputRows[property.guid]);

                foreach (var keyword in m_Graph.keywords)
                    m_KeywordSection.Add(m_InputRows[keyword.guid]);
            }
            m_ExpandedInputs.Clear();
        }

        void AddInputRow(ShaderInput input, bool create = false, int index = -1)
        {
            if (m_InputRows.ContainsKey(input.guid))
                return;

            if (create)
            {
                m_Graph.SanitizeGraphInputName(input);
                input.generatePropertyBlock = input.isExposable;
            }

            BlackboardField field = null;
            BlackboardRow row = null;

            switch(input)
            {
                case AbstractShaderProperty property:
                {
                    var icon = (m_Graph.isSubGraph || (property.isExposable && property.generatePropertyBlock)) ? exposedIcon : null;
                    field = new BlackboardField(icon, property.displayName, property.propertyType.ToString()) { userData = property };
                    var propertyView = new BlackboardFieldPropertyView(field, m_Graph, property);
                    row = new BlackboardRow(field, propertyView) { userData = input };
                    if (index < 0)
                        index = m_InputRows.Count;
                    if (index == m_InputRows.Count)
                        m_PropertySection.Add(row);
                    else
                        m_PropertySection.Insert(index, row);
                    break;
                }
                case ShaderKeyword keyword:
                {
                    var icon = (m_Graph.isSubGraph || (keyword.isExposable && keyword.generatePropertyBlock)) ? exposedIcon : null;
                    var typeText = keyword.isEditable ? keyword.keywordType.ToString() : "Built-in Keyword";
                    field = new BlackboardField(icon, keyword.displayName, typeText) { userData = keyword };
                    var keywordView = new BlackboardFieldKeywordView(field, m_Graph, keyword);
                    row = new BlackboardRow(field, keywordView);
                    if (index < 0)
                        index = m_InputRows.Count;
                    if (index == m_InputRows.Count)
                        m_KeywordSection.Add(row);
                    else
                        m_KeywordSection.Insert(index, row);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if(field == null || row == null)
                return;

            var pill = row.Q<Pill>();
            pill.RegisterCallback<MouseEnterEvent>(evt => OnMouseHover(evt, input));
            pill.RegisterCallback<MouseLeaveEvent>(evt => OnMouseHover(evt, input));
            pill.RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);

            var expandButton = row.Q<Button>("expandButton");
            expandButton.RegisterCallback<MouseDownEvent>(evt => OnExpanded(evt, input), TrickleDown.TrickleDown);

            m_InputRows[input.guid] = row;
            m_InputRows[input.guid].expanded = SessionState.GetBool(input.guid.ToString(), true);

            if (create)
            {
                row.expanded = true;
                m_Graph.owner.RegisterCompleteObjectUndo("Create Graph Input");
                m_Graph.AddGraphInput(input);
                field.OpenTextEditor();

                if(input as ShaderKeyword != null)
                {
                    m_Graph.OnKeywordChangedNoValidate();
                }
            }
        }

        void OnExpanded(MouseDownEvent evt, ShaderInput input)
        {
            m_ExpandedInputs[input] = !m_InputRows[input.guid].expanded;
        }

        void DirtyNodes()
        {
            foreach (var node in m_Graph.GetNodes<PropertyNode>())
            {
                node.OnEnable();
                node.Dirty(ModificationScope.Node);
            }
            foreach (var node in m_Graph.GetNodes<KeywordNode>())
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
                        if (node.userData is PropertyNode propertyNode)
                        {
                            if (propertyNode.propertyGuid == input.guid)
                            {
                                m_SelectedNodes.Add(node);
                                node.AddToClassList("hovered");
                            }
                        }
                    }
                    else if(input is ShaderKeyword keyword)
                    {
                        if (node.userData is KeywordNode keywordNode)
                        {
                            if (keywordNode.keywordGuid == input.guid)
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
