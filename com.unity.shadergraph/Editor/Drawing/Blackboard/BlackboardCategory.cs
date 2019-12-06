using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Graphs;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class InputCategory
    {
        [SerializeField]
        string m_Header = "";

        public string header
        {
            get { return m_Header; }
            set { m_Header = value;  }
        }

        [SerializeField]
        bool m_Expanded = true;

        public bool expanded
        {
            get { return m_Expanded; }
        }

        [NonSerialized]
        GraphData m_Graph;

        [NonSerialized]
        public List<ShaderInput> m_Inputs = new List<ShaderInput>();

        public List<ShaderInput> inputs
        {
            get { return m_Inputs; }
        }

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializedInputs = new List<SerializationHelper.JSONSerializedElement>();

        #region ShaderInputs

        public void AddShaderInput(ShaderInput input, int index = -1)
        {
            if (index < 0)
                m_Inputs.Add(input);
            else
                m_Inputs.Insert(index, input);
        }

        public void RemoveShaderInput(ShaderInput input)
        {
            m_Inputs.Remove(input);
            RefreshBlackboardSectionDisplay();
        }

        public void RemoveShaderInputByGuid(Guid guid)
        {
            m_Inputs.RemoveAll(x => x.guid == guid);
            RefreshBlackboardSectionDisplay();
        }

        // True if the input was moved
        public bool MoveShaderInput(ShaderInput input, int newIndex)
        {
            if (newIndex > m_Inputs.Count || newIndex < 0)
                throw new ArgumentException("New index is not within keywords list.");

            var currentIndex = m_Inputs.IndexOf(input);
            if (currentIndex == -1)
                throw new ArgumentException("Input is not in Input Category.");

            if (newIndex == currentIndex)
                return false;

            m_Inputs.RemoveAt(currentIndex);
            if (newIndex > currentIndex)
                newIndex--;

            if (newIndex == m_Inputs.Count)
                m_Inputs.Add(input);
            else
                m_Inputs.Insert(newIndex, input);

            RefreshBlackboardSectionDisplay();

            return true;
        }

        public void RefreshBlackboardSectionDisplay()
        {
            m_BlackboardSection.Clear();
            foreach (ShaderInput input in m_Inputs)
            {
                AddDisplayedInputRow(input);
            }
        }

        #endregion

        // TODO: z do we want something like this for serialized things? Check how other serialized things work
//        public InputCategory(string title, GraphData graphData, bool displayed = true)
//        {
//            m_Header = title;
//            m_Expanded = displayed;
//
//            CreateBlackboardSection();
//        }


        [NonSerialized]
        BlackboardSection m_BlackboardSection;

        public BlackboardSection blackboardSection
        {
            get
            {
                return m_BlackboardSection;
            }
        }


        public void CreateBlackboardSection(GraphData graph)
        {
            m_Graph = graph;

            m_BlackboardSection = new BlackboardSection();
            m_BlackboardSection.title = m_Header;

            m_BlackboardSection.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));

            foreach (ShaderInput input in inputs)
            {
                AddDisplayedInputRow(input);
            }
        }

        // TODO: z probably shouldn't go here?
        void CreateShaderInput(ShaderInput input)
        {
            inputs.Add(input);
            AddDisplayedInputRow(input);

            m_Graph.SanitizeGraphInputName(input);
            input.generatePropertyBlock = input.isExposable;

            m_Graph.owner.RegisterCompleteObjectUndo("Create Graph Input");
            m_Graph.AddGraphInput(input);

            if (input as ShaderKeyword != null)
            {
                m_Graph.OnKeywordChangedNoValidate();
            }
        }

        void AddDisplayedInputRow(ShaderInput input)
        {
            // TODO: z double check that things cannot be added twice
//            if (m_InputRows.ContainsKey(input.guid))
//                return;

            BlackboardField field = null;
            BlackboardRow row = null;

            switch(input)
            {
                case AbstractShaderProperty property:
                {
                    var icon = (m_Graph.isSubGraph || (property.isExposable && property.generatePropertyBlock)) ? BlackboardProvider.exposedIcon : null;
                    field = new BlackboardField(icon, property.displayName, property.propertyType.ToString()) { userData = property };
                    var propertyView = new BlackboardFieldPropertyView(field, m_Graph, property);
                    row = new BlackboardRow(field, propertyView) { userData = input };

                    break;
                }
                case ShaderKeyword keyword:
                {
                    var icon = (m_Graph.isSubGraph || (keyword.isExposable && keyword.generatePropertyBlock)) ? BlackboardProvider.exposedIcon : null;
                    var typeText = KeywordUtil.IsBuiltinKeyword(keyword) ? "Built-in Keyword" : keyword.keywordType.ToString();
                    field = new BlackboardField(icon, keyword.displayName, typeText) { userData = keyword };
                    var keywordView = new BlackboardFieldKeywordView(field, m_Graph, keyword);
                    row = new BlackboardRow(field, keywordView);

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            m_BlackboardSection.Add(row);

            // TODO: z
//            var pill = row.Q<Pill>();
//            pill.RegisterCallback<MouseEnterEvent>(evt => OnMouseHover(evt, input));
//            pill.RegisterCallback<MouseLeaveEvent>(evt => OnMouseHover(evt, input));
//            pill.RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
//
//            var expandButton = row.Q<Button>("expandButton");
//            expandButton.RegisterCallback<MouseDownEvent>(evt => OnExpanded(evt, input), TrickleDown.TrickleDown);
//
//            m_InputRows[input.guid] = row;
//            m_InputRows[input.guid].expanded = SessionState.GetBool(input.guid.ToString(), true);
        }

        #region Serialization

        public void OnBeforeSerialize()
        {
            // TODO: does making the ShaderInput cause Property or Keyword specific serialized fields to get lost? probably?
            m_SerializedInputs = SerializationHelper.Serialize<ShaderInput>(m_Inputs);
        }

        public void OnAfterDeserialize()
        {
            m_Inputs = SerializationHelper.Deserialize<ShaderInput>(m_SerializedInputs, GraphUtil.GetLegacyTypeRemapping());
        }

        #endregion


        #region DropdownMenu
        void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Rename", (a) => OpenTextEditor(), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Delete", (a) => RemoveSelf(), DropdownMenuAction.AlwaysEnabled);

            evt.menu.AppendSeparator("/");

            AddPropertyItems(evt.menu);
        }

        void OpenTextEditor()
        {
            Debug.Log("OpenTextEditor()");
        }

        void RemoveSelf()
        {
            m_Graph.categories.Remove(this);
        }

        void AddPropertyItems(DropdownMenu menu)
        {
            menu.AppendAction($"Vector1", (a) => CreateShaderInput(new Vector1ShaderProperty()), DropdownMenuAction.AlwaysEnabled);
            menu.AppendAction($"Vector2", (a) => CreateShaderInput(new Vector2ShaderProperty()), DropdownMenuAction.AlwaysEnabled);
            menu.AppendAction($"Vector3", (a) => CreateShaderInput(new Vector3ShaderProperty()), DropdownMenuAction.AlwaysEnabled);
            menu.AppendAction($"Vector4", (a) => CreateShaderInput(new Vector4ShaderProperty()), DropdownMenuAction.AlwaysEnabled);
            menu.AppendAction($"Color", (a) => CreateShaderInput(new ColorShaderProperty()), DropdownMenuAction.AlwaysEnabled);
            menu.AppendAction($"Texture2D", (a) => CreateShaderInput(new Texture2DShaderProperty()), DropdownMenuAction.AlwaysEnabled);
            menu.AppendAction($"Texture2D Array", (a) => CreateShaderInput(new Texture2DArrayShaderProperty()), DropdownMenuAction.AlwaysEnabled);
            menu.AppendAction($"Texture3D", (a) => CreateShaderInput(new Texture3DShaderProperty()), DropdownMenuAction.AlwaysEnabled);
            menu.AppendAction($"Cubemap", (a) => CreateShaderInput(new CubemapShaderProperty()), DropdownMenuAction.AlwaysEnabled);
            menu.AppendAction($"Boolean", (a) => CreateShaderInput(new BooleanShaderProperty()), DropdownMenuAction.AlwaysEnabled);
            menu.AppendAction($"Matrix2x2", (a) => CreateShaderInput(new Matrix2ShaderProperty()), DropdownMenuAction.AlwaysEnabled);
            menu.AppendAction($"Matrix3x3", (a) => CreateShaderInput(new Matrix3ShaderProperty()), DropdownMenuAction.AlwaysEnabled);
            menu.AppendAction($"Matrix4x4", (a) => CreateShaderInput(new Matrix4ShaderProperty()), DropdownMenuAction.AlwaysEnabled);
            menu.AppendAction($"SamplerState", (a) => CreateShaderInput(new SamplerStateShaderProperty()), DropdownMenuAction.AlwaysEnabled);
            menu.AppendAction($"Gradient", (a) => CreateShaderInput(new GradientShaderProperty()), DropdownMenuAction.AlwaysEnabled);
        }
        #endregion

    }
}
