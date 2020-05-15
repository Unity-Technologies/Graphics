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
        readonly Dictionary<ShaderInput, BlackboardRow> m_InputRows;
        readonly BlackboardSection m_PropertySection;
        readonly BlackboardSection m_KeywordSection;

        public const int k_PropertySectionIndex = 0;
        public const int k_KeywordSectionIndex = 1;

        public Blackboard blackboard { get; private set; }
        Label m_PathLabel;
        TextField m_PathLabelTextField;
        bool m_EditPathCancelled = false;
        List<Node> m_SelectedNodes = new List<Node>();

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
            m_InputRows = new Dictionary<ShaderInput, BlackboardRow>();

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
            gm.AddItem(new GUIContent($"Virtual Texture"), false, () => AddInputRow(new VirtualTextureShaderProperty(), true));
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
                var keyword = ShaderKeyword.CreateBuiltInKeyword(builtinKeywordDescriptor);
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

        public void HandleGraphChanges(bool wasUndoRedoPerformed)
        {
            var selection = new List<ISelectable>();
            if(blackboard.selection != null)
                selection.Concat(blackboard.selection);

            foreach (var inputGuid in m_Graph.removedInputs)
            {
                BlackboardRow row;
                if (m_InputRows.TryGetValue(inputGuid, out row))
                {
                    row.RemoveFromHierarchy();
                    m_InputRows.Remove(inputGuid);
                }
            }

            // This tries to maintain the selection the user had before the undo/redo was performed,
            // if the user hasn't added or removed any inputs
            if (wasUndoRedoPerformed)
            {
                oldSelectionPersistenceData.Clear();
                foreach (var item in selection)
                {
                    if (item is BlackboardFieldView blackboardFieldView)
                    {
                        var guid = blackboardFieldView.shaderInput.referenceName;
                        oldSelectionPersistenceData.Add(guid, blackboardFieldView.viewDataKey);
                    }
                }
            }

            foreach (var input in m_Graph.addedInputs)
            {
                AddInputRow(input, index: m_Graph.GetGraphInputIndex(input));
            }

            if (m_Graph.movedInputs.Any())
            {
                foreach (var row in m_InputRows.Values)
                    row.RemoveFromHierarchy();

                foreach (var property in m_Graph.properties)
                    m_PropertySection.Add(m_InputRows[property]);

                foreach (var keyword in m_Graph.keywords)
                    m_KeywordSection.Add(m_InputRows[keyword]);
            }
        }

        // A map from shaderInput reference names to the viewDataKey of the blackboardFieldView that used to represent them
        // This data is used to re-select the shaderInputs in the blackboard after an undo/redo is performed
        Dictionary<string, string> oldSelectionPersistenceData { get; set; } = new Dictionary<string, string>();

        void AddInputRow(ShaderInput input, bool create = false, int index = -1)
        {
            if (m_InputRows.ContainsKey(input))
                return;

            if (create)
            {
                m_Graph.SanitizeGraphInputName(input);
                input.generatePropertyBlock = input.isExposable;
            }

            BlackboardFieldView field = null;
            BlackboardRow row = null;

            switch(input)
            {
                case AbstractShaderProperty property:
                {
                    var icon = (m_Graph.isSubGraph || (property.isExposable && property.generatePropertyBlock)) ? exposedIcon : null;
                    field = new BlackboardFieldView(m_Graph, property, icon, property.displayName, property.propertyType.ToString()) { userData = property };
                    field.RegisterCallback<AttachToPanelEvent>(UpdateSelectionAfterUndoRedo);
                    row = new BlackboardRow(field, null);

                    if (index < 0 || index > m_InputRows.Count)
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

                    string typeText = keyword.keywordType.ToString()  + " Keyword";
                    typeText = keyword.isBuiltIn ? "Built-in " + typeText : typeText;

                    field = new BlackboardFieldView(m_Graph, keyword, icon, keyword.displayName, typeText) { userData = keyword };
                    field.RegisterCallback<AttachToPanelEvent>(UpdateSelectionAfterUndoRedo);
                    row = new BlackboardRow(field, null);

                    if (index < 0 || index > m_InputRows.Count)
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

            field.RegisterCallback<MouseEnterEvent>(evt => OnMouseHover(evt, input));
            field.RegisterCallback<MouseLeaveEvent>(evt => OnMouseHover(evt, input));
            field.RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);

            // Removing the expand button from the blackboard, its added by default
            var expandButton = row.Q<Button>("expandButton");
            expandButton.RemoveFromHierarchy();

            m_InputRows[input] = row;

            if (!create)
            {
                m_InputRows[input].expanded = SessionState.GetBool($"Unity.ShaderGraph.Input.{input.objectId}.isExpanded", false);
            }
            else
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

        void UpdateSelectionAfterUndoRedo(AttachToPanelEvent evt)
        {
            var newFieldView = evt.target as BlackboardFieldView;
            // If this field view represents a value that was previously selected
            if (oldSelectionPersistenceData.TryGetValue(newFieldView?.shaderInput.referenceName, out var oldViewDataKey))
            {
                // ViewDataKey is how UIElements handles UI state persistence,
                // This selects the newly added field view
                newFieldView.viewDataKey = oldViewDataKey;
            }
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

        public BlackboardRow GetBlackboardRow(ShaderInput input)
        {
            return m_InputRows[input];
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
                            if (propertyNode.property == input)
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
                            if (keywordNode.keyword == input)
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
