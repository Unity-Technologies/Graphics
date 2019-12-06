using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;
using Random = System.Random;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardProvider
    {
        public static readonly Texture2D exposedIcon = Resources.Load<Texture2D>("GraphView/Nodes/BlackboardFieldExposed");

        readonly GraphData m_Graph;

        // readonly Dictionary<Guid, BlackboardRow> m_InputRows;

        readonly BlackboardSection m_PropertySection;
        readonly BlackboardSection m_KeywordSection;

        public const int k_PropertySectionIndex = 0;
        public const int k_KeywordSectionIndex = 1;

        public Blackboard blackboard { get; private set; }
        Label m_PathLabel;
        TextField m_PathLabelTextField;
        bool m_EditPathCancelled = false;
        List<Node> m_SelectedNodes = new List<Node>();

        Dictionary<Guid, bool> m_ExpandedInputs = new Dictionary<Guid, bool>();

        public Dictionary<Guid, bool> expandedInputs
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
        }

        // TODO: move
        void CreateNewCategory(string categoryHeader)
        {
            InputCategory inputCategory = new InputCategory();
            inputCategory.header = categoryHeader;

            m_Graph.categories.Add(inputCategory);
        }

        void AddCategoryOnBlackboard(InputCategory category)
        {
            if (category.blackboardSection == null)
                category.CreateBlackboardSection(m_Graph);

            blackboard.Add(category.blackboardSection);
        }

        void BlackboardClear()
        {
            blackboard.Clear();
        }

        // TODO: z
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

            m_Graph.MoveWithinCategory(input, newIndex);
        }

        void AddItemRequested(Blackboard blackboard)
        {
            var gm = new GenericMenu();

            gm.AddItem(new GUIContent($"Category"), false, () => CreateNewCategory("new category " + UnityEngine.Random.value.ToString()));
            gm.AddSeparator($"");

//            AddPropertyItems(gm);
//            AddKeywordItems(gm);

            gm.ShowAsContext();
        }

        void AddPropertyItems(GenericMenu gm)
        {
            gm.AddItem(new GUIContent($"Vector1"), false, () => CreateShaderInput(new Vector1ShaderProperty()));
            gm.AddItem(new GUIContent($"Vector2"), false, () => CreateShaderInput(new Vector2ShaderProperty()));
            gm.AddItem(new GUIContent($"Vector3"), false, () => CreateShaderInput(new Vector3ShaderProperty()));
            gm.AddItem(new GUIContent($"Vector4"), false, () => CreateShaderInput(new Vector4ShaderProperty()));
            gm.AddItem(new GUIContent($"Color"), false, () => CreateShaderInput(new ColorShaderProperty()));
            gm.AddItem(new GUIContent($"Texture2D"), false, () => CreateShaderInput(new Texture2DShaderProperty()));
            gm.AddItem(new GUIContent($"Texture2D Array"), false, () => CreateShaderInput(new Texture2DArrayShaderProperty()));
            gm.AddItem(new GUIContent($"Texture3D"), false, () => CreateShaderInput(new Texture3DShaderProperty()));
            gm.AddItem(new GUIContent($"Cubemap"), false, () => CreateShaderInput(new CubemapShaderProperty()));
            gm.AddItem(new GUIContent($"Boolean"), false, () => CreateShaderInput(new BooleanShaderProperty()));
            gm.AddItem(new GUIContent($"Matrix2x2"), false, () => CreateShaderInput(new Matrix2ShaderProperty()));
            gm.AddItem(new GUIContent($"Matrix3x3"), false, () => CreateShaderInput(new Matrix3ShaderProperty()));
            gm.AddItem(new GUIContent($"Matrix4x4"), false, () => CreateShaderInput(new Matrix4ShaderProperty()));
            gm.AddItem(new GUIContent($"SamplerState"), false, () => CreateShaderInput(new SamplerStateShaderProperty()));
            gm.AddItem(new GUIContent($"Gradient"), false, () => CreateShaderInput(new GradientShaderProperty()));
            gm.AddSeparator($"/");
        }

        void AddKeywordItems(GenericMenu gm)
        {
            gm.AddItem(new GUIContent($"Keyword/Boolean"), false, () => CreateShaderInput(new ShaderKeyword(KeywordType.Boolean)));
            gm.AddItem(new GUIContent($"Keyword/Enum"), false, () => CreateShaderInput(new ShaderKeyword(KeywordType.Enum)));
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
                gm.AddItem(new GUIContent($"Keyword/{keyword.displayName}"), false, () => CreateShaderInput(keyword.Copy()));
            }
        }

        void EditTextRequested(Blackboard blackboard, VisualElement visualElement, string newText)
        {
            Debug.Log("I'mma edit " + visualElement);

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
            // Simply remove and then add the categories only
            BlackboardClear();
            foreach (var category in m_Graph.categories)
            {
                AddCategoryOnBlackboard(category);

                if (m_Graph.alteredCategories.Count() > 0 && m_Graph.alteredCategories.Contains(category))
                    category.blackboardSection.RefreshSection();
            }

            foreach (var expandedInput in expandedInputs)
            {
                SessionState.SetBool($"Unity.ShaderGraph.Input.{expandedInput.Key}.isExpanded", expandedInput.Value);
            }

            m_ExpandedInputs.Clear();
        }

        // TODO: z these below need to be moved / modified
        void CreateShaderInput(ShaderInput input)
        {
            m_Graph.SanitizeGraphInputName(input);
            input.generatePropertyBlock = input.isExposable;

//            AddInputRow(input);

            m_Graph.owner.RegisterCompleteObjectUndo("Create Graph Input");
            m_Graph.AddGraphInput(input);

// TODO: z Where should this stuff go?...
// <<<<<<< HEAD
//                     break;
//                 }
//                 case ShaderKeyword keyword:
//                 {
//                     var icon = (m_Graph.isSubGraph || (keyword.isExposable && keyword.generatePropertyBlock)) ? exposedIcon : null;

//                     string typeText = keyword.keywordType.ToString()  + " Keyword";
//                     typeText = keyword.isBuiltIn ? "Built-in " + typeText : typeText;

//                     field = new BlackboardField(icon, keyword.displayName, typeText) { userData = keyword };
//                     var keywordView = new BlackboardFieldKeywordView(field, m_Graph, keyword);
//                     row = new BlackboardRow(field, keywordView);

//                     if (index < 0 || index > m_InputRows.Count)
//                         index = m_InputRows.Count;
// =======
// >>>>>>> e7b6a7272e... hackweek start to blackboard section

// //            row.expanded = true;
// //            field.OpenTextEditor();

// <<<<<<< HEAD
//                     break;
//                 }
//                 default:
//                     throw new ArgumentOutOfRangeException();
//             }

//             if(field == null || row == null)
//                 return;

//             var pill = row.Q<Pill>();
//             pill.RegisterCallback<MouseEnterEvent>(evt => OnMouseHover(evt, input));
//             pill.RegisterCallback<MouseLeaveEvent>(evt => OnMouseHover(evt, input));
//             pill.RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);

//             var expandButton = row.Q<Button>("expandButton");
//             expandButton.RegisterCallback<MouseDownEvent>(evt => OnExpanded(evt, input), TrickleDown.TrickleDown);

//             m_InputRows[input.guid] = row;

//             if (!create)
//             {
//                 m_InputRows[input.guid].expanded = SessionState.GetBool($"Unity.ShaderGraph.Input.{input.guid.ToString()}.isExpanded", false);
//             }
//             else
//             {
//                 row.expanded = true;
//                 m_ExpandedInputs[input.guid] = true;
//                 m_Graph.owner.RegisterCompleteObjectUndo("Create Graph Input");
//                 m_Graph.AddGraphInput(input);
//                 field.OpenTextEditor();

//                 if(input as ShaderKeyword != null)
//                 {
//                     m_Graph.OnKeywordChangedNoValidate();
//                 }

            if(input as ShaderKeyword != null)
            {
                m_Graph.OnKeywordChangedNoValidate();
            }
        }

        void OnExpanded(MouseDownEvent evt, ShaderInput input)
        {
            m_ExpandedInputs[input.guid] = !m_InputRows[input.guid].expanded;
        }

        // TODO: z (needed for mouse hover, doing this for the categories themselve would also be nice)
//        void OnExpanded(MouseDownEvent evt, ShaderInput input)
//        {
//            m_ExpandedInputs[input] = !m_InputRows[input.guid].expanded;
//        }

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

        // TODO: z
//        public BlackboardRow GetBlackboardRow(Guid guid)
//        {
//            return m_InputRows[guid];
//        }

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
