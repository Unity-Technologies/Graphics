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
using BranchNew = UnityEditor.VFX.Operator.BranchNew;

namespace UnityEditor.VFX.UI
{
    class VFXUniformOperatorEdit<T,U> : VisualElement, IControlledElement<T> where U : VFXOperatorDynamicOperand, IVFXOperatorUniform where T : VFXUniformOperatorController<U>
    {
        Label m_TypePopup;
        public VFXUniformOperatorEdit()
        {
            this.AddStyleSheetPathWithSkinVariant("VFXControls");
            AddToClassList("VFXUniformOperatorEdit");
            m_TypePopup = new Label();
            m_TypePopup.AddToClassList("PopupButton");
            m_TypePopup.AddManipulator(new DownClickable(() => OnTypeMenu()));

            Add(m_TypePopup);

            RegisterCallback<ControllerChangedEvent>(OnChange);
        }

        void OnTypeMenu()
        {
            var op = controller.model;
            GenericMenu menu = new GenericMenu();
            var selectedType = op.GetOperandType();
            foreach (var type in op.validTypes)
            {
                menu.AddItem(EditorGUIUtility.TrTextContent(type.UserFriendlyName()), selectedType == type, OnChangeType, type);
            }
            menu.DropDown(m_TypePopup.worldBound);
        }

        void OnChangeType(object type)
        {
            var op = controller.model;

            op.SetOperandType((Type)type);
        }

        T m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public T controller
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
        void OnChange(ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                m_TypePopup.text = controller.model.GetOperandType().UserFriendlyName();
            }
        }
    }
    class VFXMultiOperatorEdit<T, U> : VFXReorderableList, IControlledElement<T> where U : VFXOperatorNumericNew, IVFXOperatorNumericUnifiedNew where T : VFXUnifiedOperatorControllerBase<U>
    {
        T m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public T controller
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

        public VFXMultiOperatorEdit()
        {
            RegisterCallback<ControllerChangedEvent>(OnChange);
        }

        int m_CurrentIndex = -1;
        void OnTypeMenu(Label button, int index)
        {
            var op = controller.model;
            GenericMenu menu = new GenericMenu();
            var selectedType = op.GetOperandType(index);
            foreach (var type in op.validTypes)
            {
                menu.AddItem(EditorGUIUtility.TrTextContent(type.UserFriendlyName()), selectedType == type, OnChangeType, type);
            }
            m_CurrentIndex = index;
            menu.DropDown(button.worldBound);
        }

        void OnChangeType(object type)
        {
            var op = controller.model;

            op.SetOperandType(m_CurrentIndex, (Type)type);
        }

        void OnChange(ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                SelfChange();
            }
        }

        protected bool m_SelfChanging;

        void SelfChange()
        {
            m_SelfChanging = true;
            var op = controller.model;
            int count = op.operandCount;

            bool sizeChanged = false;

            while (itemCount < count)
            {
                OperandInfoBase item = CreateOperandInfo(itemCount);
                item.Set(op);
                AddItem(item);
                sizeChanged = true;
            }
            while (itemCount > count)
            {
                RemoveItemAt(itemCount - 1);
                sizeChanged = true;
            }

            for (int i = 0; i < count; ++i)
            {
                OperandInfoBase operand = ItemAt(i) as OperandInfoBase;
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

        protected virtual OperandInfoBase CreateOperandInfo(int index)
        {
            return new OperandInfoBase(this, controller.model, index);
        }

        protected class OperandInfoBase : VisualElement
        {
            Label type;
            public VFXMultiOperatorEdit<T, U> m_Owner;

            public int index;

            public OperandInfoBase(VFXMultiOperatorEdit<T, U> owner, U op, int index)
            {
                this.AddStyleSheetPathWithSkinVariant("VFXControls");
                m_Owner = owner;
                type = new Label();
                this.index = index;
                type.AddToClassList("PopupButton");
                type.AddManipulator(new DownClickable(OnTypeMenu));

                Add(type);
            }

            void OnTypeMenu()
            {
                m_Owner.OnTypeMenu(type, index);
            }

            public virtual void Set(U op)
            {
                type.text = op.GetOperandType(index).UserFriendlyName();
            }
        }
    }

    class VFXUnifiedOperatorEdit : VFXMultiOperatorEdit<VFXUnifiedOperatorController, VFXOperatorNumericUnifiedNew>
    {
        public VFXUnifiedOperatorEdit()
        {
            toolbar = false;
            reorderable = false;
        }

        protected override OperandInfoBase CreateOperandInfo(int index)
        {
            return new OperandInfo(this, controller.model, index);
        }

        class OperandInfo : OperandInfoBase
        {
            Label label;

            public OperandInfo(VFXUnifiedOperatorEdit owner, VFXOperatorNumericUnifiedNew op, int index) : base(owner, op, index)
            {
                label = new Label();

                Insert(0, label);
            }

            public override void Set(VFXOperatorNumericUnifiedNew op)
            {
                base.Set(op);
                label.text = op.GetInputSlot(index).name;
            }
        }
    }
    class VFXCascadedOperatorEdit : VFXMultiOperatorEdit<VFXCascadedOperatorController, VFXOperatorNumericCascadedUnifiedNew>
    {
        protected override void ElementMoved(int movedIndex, int targetIndex)
        {
            base.ElementMoved(movedIndex, targetIndex);
            controller.model.OperandMoved(movedIndex, targetIndex);
        }

        public override void OnAdd()
        {
            controller.model.AddOperand();
        }

        public override bool CanRemove()
        {
            return controller.CanRemove();
        }

        public override void OnRemove(int index)
        {
            controller.RemoveOperand(index);
        }

        void OnChangeLabel(string value, int index)
        {
            if (!m_SelfChanging)
            {
                var op = controller.model;

                if (value != op.GetOperandName(index)) // test mandatory because TextField might send ChangeEvent anytime
                    op.SetOperandName(index, value);
            }
        }

        protected override OperandInfoBase CreateOperandInfo(int index)
        {
            return new OperandInfo(this, controller.model, index);
        }

        class OperandInfo : OperandInfoBase
        {
            VFXStringField field;

            public OperandInfo(VFXCascadedOperatorEdit owner, VFXOperatorNumericCascadedUnifiedNew op, int index) : base(owner, op, index)
            {
                field = new VFXStringField("");
                field.OnValueChanged = OnChangeLabel;

                Insert(0, field);
            }

            void OnChangeLabel()
            {
                (m_Owner as VFXCascadedOperatorEdit).OnChangeLabel(field.value, index);
            }

            public override void Set(VFXOperatorNumericCascadedUnifiedNew op)
            {
                base.Set(op);
                field.value = op.GetOperandName(index);
            }
        }
    }
}
