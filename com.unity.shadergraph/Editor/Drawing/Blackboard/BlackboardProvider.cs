using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Views.Blackboard
{
    class BlackboardProvider
    {
        readonly GraphData m_Graph;
        public static readonly Texture2D exposedIcon = Resources.Load<Texture2D>("GraphView/Nodes/BlackboardFieldExposed");
        readonly Dictionary<ShaderInput, BlackboardRow> m_InputRows;
        readonly SGBlackboardSection m_PropertySection;
        readonly SGBlackboardSection m_KeywordSection;

        public const int k_PropertySectionIndex = 0;
        public const int k_KeywordSectionIndex = 1;
        const string k_styleName = "Blackboard";

        public SGBlackboard blackboard { get; private set; }
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

        public BlackboardProvider(GraphData graph, GraphView associatedGraphView)
        {
            m_Graph = graph;
            m_InputRows = new Dictionary<ShaderInput, BlackboardRow>();

            blackboard = new SGBlackboard(associatedGraphView)
            {
                subTitle = FormatPath(graph.path),
                addItemRequested = AddItemRequested,
                moveItemRequested = MoveItemRequested
            };

            // These make sure that the drag indicators are disabled whenever a drag action is cancelled without completing a drop
            blackboard.RegisterCallback<MouseUpEvent>(evt =>
            {
                m_PropertySection.OnDragActionCanceled();
                m_KeywordSection.OnDragActionCanceled();
            });

            blackboard.RegisterCallback<DragExitedEvent>(evt =>
            {
                m_PropertySection.OnDragActionCanceled();
                m_KeywordSection.OnDragActionCanceled();
            });


            m_PathLabel = blackboard.hierarchy.ElementAt(0).Q<Label>("subTitleLabel");
            m_PathLabel.RegisterCallback<MouseDownEvent>(OnMouseDownEvent);

            m_PathLabelTextField = new TextField { visible = false };
            m_PathLabelTextField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(e => { OnEditPathTextFinished(); });
            m_PathLabelTextField.Q("unity-text-input").RegisterCallback<KeyDownEvent>(OnPathTextFieldKeyPressed);
            blackboard.hierarchy.Add(m_PathLabelTextField);

            m_PropertySection = new SGBlackboardSection { title = "Properties" };
            foreach (var property in graph.properties)
                AddInputRow(property);
            blackboard.Add(m_PropertySection);

            m_KeywordSection = new SGBlackboardSection { title = "Keywords" };
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

        void MoveItemRequested(SGBlackboard blackboard, int newIndex, VisualElement visualElement)
        {
            var input = visualElement.userData as ShaderInput;
            if (input == null)
                return;

            m_Graph.owner.RegisterCompleteObjectUndo("Move Graph Input");
            switch (input)
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

        void AddItemRequested(SGBlackboard blackboard)
        {
            var gm = new GenericMenu();
            AddPropertyItems(gm);
            AddKeywordItems(gm);
            gm.ShowAsContext();
        }

        void AddPropertyItems(GenericMenu gm)
        {
            var shaderInputTypes = TypeCache.GetTypesWithAttribute<BlackboardInputInfo>().ToList();

            // Sort the ShaderInput by priority using the BlackboardInputInfo attribute
            shaderInputTypes.Sort((s1, s2) => {
                var info1 = Attribute.GetCustomAttribute(s1, typeof(BlackboardInputInfo)) as BlackboardInputInfo;
                var info2 = Attribute.GetCustomAttribute(s2, typeof(BlackboardInputInfo)) as BlackboardInputInfo;

                if (info1.priority == info2.priority)
                    return (info1.name ?? s1.Name).CompareTo(info2.name ?? s2.Name);
                else
                    return info1.priority.CompareTo(info2.priority);
            });

            foreach (var t in shaderInputTypes)
            {
                if (t.IsAbstract)
                    continue;

                var info = Attribute.GetCustomAttribute(t, typeof(BlackboardInputInfo)) as BlackboardInputInfo;
                string name = info?.name ?? ObjectNames.NicifyVariableName(t.Name.Replace("ShaderProperty", ""));
                ShaderInput si = Activator.CreateInstance(t, true) as ShaderInput;
                gm.AddItem(new GUIContent(name), false, () => AddInputRow(si, true));
                //QUICK FIX TO DEAL WITH DEPRECATED COLOR PROPERTY
                if (ShaderGraphPreferences.allowDeprecatedBehaviors && si is ColorShaderProperty csp)
                {
                    gm.AddItem(new GUIContent($"Color (Deprecated)"), false, () => AddInputRow(new ColorShaderProperty(ColorShaderProperty.deprecatedVersion), true));
                }
            }
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
            if (m_Graph.keywords.Where(x => x.referenceName == keyword.referenceName).Any())
            {
                gm.AddDisabledItem(new GUIContent($"Keyword/{keyword.displayName}"));
            }
            else
            {
                gm.AddItem(new GUIContent($"Keyword/{keyword.displayName}"), false, () => AddInputRow(m_Graph.AddCopyOfShaderInput(keyword)));
            }
        }

        public void HandleGraphChanges(bool wasUndoRedoPerformed)
        {
            var selection = new List<ISelectable>();
            if (blackboard.selection != null)
            {
                selection.AddRange(blackboard.selection);
            }

            foreach (var shaderInput in m_Graph.removedInputs)
            {
                BlackboardRow row;
                if (m_InputRows.TryGetValue(shaderInput, out row))
                {
                    row.RemoveFromHierarchy();
                    m_InputRows.Remove(shaderInput);
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

        void AddInputRow(ShaderInput input, bool addToGraph = false, int index = -1)
        {
            if (m_InputRows.ContainsKey(input))
                return;

            if (addToGraph)
            {
                m_Graph.owner.RegisterCompleteObjectUndo("Create Graph Input");

                // this pathway is mostly used for adding newly inputs to the graph
                // so this is setting up the default state for those inputs
                // here we flag it exposed, if the input type is exposable
                input.generatePropertyBlock = input.isExposable;

                m_Graph.AddGraphInput(input);       // TODO: index after currently selected property
            }

            BlackboardFieldView field = null;
            BlackboardRow row = null;

            switch (input)
            {
                case AbstractShaderProperty property:
                {
                    var icon = (m_Graph.isSubGraph || property.isExposed) ? exposedIcon : null;
                    field = new BlackboardFieldView(m_Graph, property, icon, property.displayName, property.GetPropertyTypeString()) { userData = property };
                    field.RegisterCallback<AttachToPanelEvent>(UpdateSelectionAfterUndoRedo);
                    property.onBeforeVersionChange += (_) => m_Graph.owner.RegisterCompleteObjectUndo($"Change {property.displayName} Version");
                    void UpdateField()
                    {
                        field.typeText = property.GetPropertyTypeString();
                        field.InspectorUpdateTrigger();
                    }

                    property.onAfterVersionChange += UpdateField;
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
                    var icon = (m_Graph.isSubGraph || keyword.isExposed) ? exposedIcon : null;

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
            // These callbacks are used for the property dragging scroll behavior
            field.RegisterCallback<DragEnterEvent>(evt => blackboard.ShowScrollBoundaryRegions());
            field.RegisterCallback<DragExitedEvent>(evt => blackboard.HideScrollBoundaryRegions());

            // Removing the expand button from the blackboard, its added by default
            var expandButton = row.Q<Button>("expandButton");
            expandButton.RemoveFromHierarchy();

            m_InputRows[input] = row;

            if (!addToGraph)
            {
                m_InputRows[input].expanded = SessionState.GetBool($"Unity.ShaderGraph.Input.{input.objectId}.isExpanded", false);
            }
            else
            {
                row.expanded = true;
                field.OpenTextEditor();
                if (input as ShaderKeyword != null)
                {
                    m_Graph.OnKeywordChangedNoValidate();
                }
            }
        }

        void UpdateSelectionAfterUndoRedo(AttachToPanelEvent evt)
        {
            var newFieldView = evt.target as BlackboardFieldView;
            // If this field view represents a value that was previously selected
            var refName = newFieldView?.shaderInput?.referenceName;
            if (refName != null && oldSelectionPersistenceData.TryGetValue(refName, out var oldViewDataKey))
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
            if (m_InputRows.ContainsKey(input))
                return m_InputRows[input];
            else
                return null;
        }

        void OnMouseHover(EventBase evt, ShaderInput input)
        {
            var graphView = blackboard.GetFirstAncestorOfType<MaterialGraphView>();
            if (evt.eventTypeId == MouseEnterEvent.TypeId())
            {
                foreach (var node in graphView.nodes.ToList())
                {
                    if (input is AbstractShaderProperty property)
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
                    else if (input is ShaderKeyword keyword)
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
