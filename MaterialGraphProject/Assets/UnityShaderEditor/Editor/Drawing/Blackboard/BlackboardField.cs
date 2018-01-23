using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardField : GraphElement
    {
        private VisualElement m_ContentItem;
        private Image m_Icon;
        private Label m_TextLabel;
        private TextField m_TextField;
        private Label m_TypeLabel;
        private bool m_EditTitleCancelled = false;
        SelectionDropper m_SelectionDropper;

        public string text
        {
            get { return m_TextLabel.text; }
            set { m_TextLabel.text = value; }
        }

        public string typeText
        {
            get { return m_TypeLabel.text; }
            set { m_TypeLabel.text = value; }
        }

        public Texture icon
        {
            get { return m_Icon.image; }
            set
            {
                m_Icon.image = value;

                if (value == null)
                {
                    AddToClassList("noIcon");
                    m_Icon.visible = false;
                }
                else
                {
                    RemoveFromClassList("noIcon");
                    m_Icon.visible = true;
                }
            }
        }

        public BlackboardField()
            : this(null, "", "") { }

        public BlackboardField(Texture icon, string text, string typeText)
        {
            var tpl = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UnityShaderEditor/Editor/Resources/UXML/GraphView/BlackboardField.uxml");
            VisualElement mainContainer = tpl.CloneTree(null);

            mainContainer.AddToClassList("mainContainer");
            mainContainer.pickingMode = PickingMode.Ignore;

            m_ContentItem = mainContainer.Q("contentItem");

            m_TextLabel = mainContainer.Q<Label>("textLabel");
            m_Icon = mainContainer.Q<Image>("iconItem");
            m_TypeLabel = mainContainer.Q<Label>("typeLabel");

            m_TextField = mainContainer.Q<TextField>("textField");
            m_TextField.visible = false;
            m_TextField.RegisterCallback<FocusOutEvent>(e => { OnEditTextFinished(); });
            m_TextField.RegisterCallback<KeyDownEvent>(OnTextFieldKeyPressed);

            Add(mainContainer);

            RegisterCallback<MouseDownEvent>(OnMouseDownEvent);

            capabilities |= Capabilities.Selectable | Capabilities.Droppable | Capabilities.Deletable;

            ClearClassList();
            AddToClassList("sgblackboardField");

            this.text = text;
            this.icon = icon;
            this.typeText = typeText;

            m_SelectionDropper = new SelectionDropper(Handler);
            typeof(SelectionDropper).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(f => f.Name == "m_Active").SetValue(m_SelectionDropper, false);
            this.AddManipulator(m_SelectionDropper);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.button == 1)
            {
                var gm = new GenericMenu();

                gm.AddItem(new GUIContent("Rename"), false, RenameGo);

                gm.ShowAsContext();
                evt.StopPropagation();
                evt.PreventDefault();
            }
        }

        void Handler(IMGUIEvent evt, List<ISelectable> selection, IDropTarget dropTarget)
        {
        }

        private void OnTextFieldKeyPressed(KeyDownEvent e)
        {
            switch (e.keyCode)
            {
                case KeyCode.Escape:
                    m_EditTitleCancelled = true;
                    m_TextField.Blur();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    m_TextField.Blur();
                    break;
                default:
                    break;
            }
        }

        private void OnEditTextFinished()
        {
            m_ContentItem.visible = true;
            m_TextField.visible = false;

            if (!m_EditTitleCancelled && (text != m_TextField.text))
            {
                Blackboard blackboard = GetFirstAncestorOfType<Blackboard>();

                if (blackboard.editTextRequested != null)
                {
                    blackboard.editTextRequested(blackboard, this, m_TextField.text);
                }
                else
                {
                    text = m_TextField.text;
                }
            }

            m_EditTitleCancelled = false;
        }

        private void OnMouseDownEvent(MouseDownEvent e)
        {
            if ((e.clickCount == 2) && e.button == (int)MouseButton.LeftMouse)
            {
                RenameGo();
                e.PreventDefault();
            }
        }

        internal void RenameGo()
        {
            m_TextField.text = text;
            m_TextField.visible = true;
            m_ContentItem.visible = false;
            m_TextField.Focus();
            m_TextField.SelectAll();
        }
    }
}
