using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.VFX;
using UnityEditor.VFX.UIElements;

using VFXEditableOperator = UnityEditor.VFX.VFXOperatorMultiplyNew;

namespace UnityEditor.VFX.UI
{
    class MultiOperatorEdit : VFXReorderableList, IControlledElement<VFXOperatorController>
    {
        VFXOperatorController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXOperatorController controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != value)
                {
                    if (m_Controller != null)
                    {
                        m_Controller.UnregisterHandler(this);
                    }
                    m_Controller = value;
                    if (m_Controller != null)
                    {
                        m_Controller.RegisterHandler(this);
                    }
                }
            }
        }

        VFXEditableOperator model
        {
            get
            {
                if (controller == null)
                    return null;

                return controller.model as VFXEditableOperator;
            }
        }


        protected override void ElementMoved(int movedIndex, int targetIndex)
        {
            base.ElementMoved(movedIndex, targetIndex);
            model.OperandMoved(movedIndex, targetIndex);
        }

        public MultiOperatorEdit()
        {
            RegisterCallback<ControllerChangedEvent>(OnChange);
        }

        public override void OnAdd()
        {
            model.AddOperand();
        }

        public override void OnRemove(int index)
        {
            model.RemoveOperand(index);
        }

        int m_CurrentIndex = -1;
        void OnTypeMenu(Button button, int index)
        {
            VFXEditableOperator op = model;
            GenericMenu menu = new GenericMenu();
            int selectedIndex = -1;
            VFXValueType selectedType = op.GetOperandType(index);
            int cpt = 0;
            foreach (var type in op.validTypes)
            {
                if (selectedType == type)
                    selectedIndex = cpt++;
                menu.AddItem(EditorGUIUtility.TrTextContent(type.ToString().Substring(1)), selectedType == type, OnChangeType, type);
            }
            m_CurrentIndex = index;
            menu.Popup(button.worldBound, selectedIndex);
        }

        void OnChangeType(object type)
        {
            VFXEditableOperator op = model;

            op.SetOperandType(m_CurrentIndex, (VFXValueType)type);
        }

        void OnChangeLabel(string value, int index)
        {
            if (!m_SelfChanging)
            {
                VFXEditableOperator op = model;

                if (value != op.GetOperandName(index)) // test mandatory because TextField might send ChangeEvent anytime
                    op.SetOperandName(index, value);
            }
        }

        void OnChange(ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                SelfChange();
            }
        }

        bool m_SelfChanging;

        void SelfChange()
        {
            m_SelfChanging = true;
            VFXEditableOperator op = model;
            int count = op.operandCount;

            bool sizeChanged = false;

            while (itemCount < count)
            {
                AddItem(new OperandInfo(this, op, itemCount));
                sizeChanged = true;
            }
            while (itemCount > count)
            {
                RemoveItemAt(itemCount - 1);
                sizeChanged = true;
            }

            for (int i = 0; i < count; ++i)
            {
                OperandInfo operand = ItemAt(i) as OperandInfo;
                operand.index = i; // The operand might have been changed by the drag
                operand.Set(op);
            }

            VFXOperatorUI opUI = GetFirstAncestorOfType<VFXOperatorUI>();
            if (opUI != null)
            {
                opUI.ForceRefreshLayout();
            }
            m_SelfChanging = false;
        }

        class OperandInfo : VisualElement
        {
            VFXStringField field;
            Button type;
            MultiOperatorEdit m_Owner;

            public int index;

            public OperandInfo(MultiOperatorEdit owner, VFXEditableOperator op, int index)
            {
                m_Owner = owner;
                field = new VFXStringField("name");
                field.OnValueChanged = () => owner.OnChangeLabel(field.value, index);
                type = new Button();
                this.index = index;
                type.AddToClassList("PopupButton");
                type.AddManipulator(new DownClickable(() => owner.OnTypeMenu(type, index)));
                Set(op);

                Add(field);
                Add(type);
            }

            public void Set(VFXEditableOperator op)
            {
                field.value = op.GetOperandName(index);
                type.text = op.GetOperandType(index).ToString().Substring(1);
            }
        }
    }


    class VFXOperatorUI : VFXStandaloneSlotContainerUI
    {
        VisualElement m_EditButton;

        public VFXOperatorUI()
        {
            m_Middle = new VisualElement();
            m_Middle.name = "middle";
            inputContainer.parent.Insert(1, m_Middle);

            m_EditButton = new VisualElement() {name = "edit"};
            m_EditButton.Add(new VisualElement() { name = "icon" });
            m_EditButton.AddManipulator(new Clickable(OnEdit));
        }

        VisualElement m_EditContainer;

        void OnEdit()
        {
            if (m_EditContainer != null)
            {
                if (m_EditContainer.parent != null)
                {
                    m_EditContainer.RemoveFromHierarchy();
                }
                else
                {
                    topContainer.Add(m_EditContainer);
                }
                ForceRefreshLayout();
            }
        }

        public void ForceRefreshLayout()
        {
            (panel as BaseVisualElementPanel).ValidateLayout();
            RefreshLayout();
        }

        VisualElement m_Middle;

        public new VFXOperatorController controller
        {
            get { return base.controller as VFXOperatorController; }
        }


        public override void GetPreferedWidths(ref float labelWidth, ref float controlWidth)
        {
            base.GetPreferedWidths(ref labelWidth, ref controlWidth);

            foreach (var port in GetPorts(true, false).Cast<VFXEditableDataAnchor>())
            {
                float portLabelWidth = port.GetPreferredLabelWidth() + 1;
                float portControlWidth = port.GetPreferredControlWidth();

                if (labelWidth < portLabelWidth)
                {
                    labelWidth = portLabelWidth;
                }
                if (controlWidth < portControlWidth)
                {
                    controlWidth = portControlWidth;
                }
            }
        }

        public override void ApplyWidths(float labelWidth, float controlWidth)
        {
            base.ApplyWidths(labelWidth, controlWidth);
            foreach (var port in GetPorts(true, false).Cast<VFXEditableDataAnchor>())
            {
                port.SetLabelWidth(labelWidth);
            }
        }

        public override void RefreshLayout()
        {
            if (!isEditable || m_EditContainer == null || m_EditContainer.parent == null)
            {
                bool changed = topContainer.style.height.value != 0;
                if (changed)
                {
                    topContainer.ResetPositionProperties();
                }
                base.RefreshLayout();
            }
            else
            {
                topContainer.style.height = m_EditContainer.layout.height;
                topContainer.Dirty(ChangeType.Layout);
            }
        }

        public bool isEditable
        {
            get
            {
                return controller != null && controller.model is VFXEditableOperator;
            }
        }

        protected override void SelfChange()
        {
            base.SelfChange();

            bool hasMiddle = inputContainer.childCount != 0;
            if (hasMiddle)
            {
                if (m_Middle.parent == null)
                {
                    inputContainer.parent.Insert(1, m_Middle);
                }
            }
            else if (m_Middle.parent != null)
            {
                m_Middle.RemoveFromHierarchy();
            }

            if (isEditable)
            {
                if (m_EditButton.parent == null)
                {
                    titleContainer.Insert(1, m_EditButton);
                }
                if (m_EditContainer == null)
                {
                    m_EditContainer = new MultiOperatorEdit();
                }
                (m_EditContainer as IControlledElement<VFXOperatorController>).controller = controller;
            }
            else
            {
                if (m_EditContainer != null && m_EditContainer.parent != null)
                {
                    m_EditContainer.RemoveFromHierarchy();
                }
                m_EditContainer = null;
                if (m_EditButton.parent != null)
                {
                    m_EditButton.RemoveFromHierarchy();
                }
            }
        }
    }
}
