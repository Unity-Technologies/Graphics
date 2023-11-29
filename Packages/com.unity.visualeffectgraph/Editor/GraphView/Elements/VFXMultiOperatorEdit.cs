using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXUniformOperatorEdit<T, U> : VisualElement, IControlledElement<T> where U : VFXOperatorDynamicOperand, IVFXOperatorUniform where T : VFXUniformOperatorController<U>
    {
        readonly PopupField<Type> m_PopupField;

        public VFXUniformOperatorEdit()
        {
            this.AddStyleSheetPathWithSkinVariant("VFXControls");
            AddToClassList("VFXUniformOperatorEdit");
            m_PopupField = new PopupField<Type>();
            m_PopupField.formatListItemCallback += x => x.UserFriendlyName();
            m_PopupField.formatSelectedValueCallback += x => x.UserFriendlyName();
            m_PopupField.RegisterValueChangedCallback(OnChangeType);
            Add(m_PopupField);
        }

        void OnChangeType(ChangeEvent<Type> evt)
        {
            controller.model.SetOperandType(evt.newValue);
        }

        T m_Controller;
        Controller IControlledElement.controller => m_Controller;

        public T controller
        {
            get => m_Controller;
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
                        m_PopupField.choices = controller.model.validTypes.ToList();
                    }
                }
            }
        }

        void IControlledElement.OnControllerChanged(ref ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                m_PopupField.value = controller.model.GetOperandType();
            }
        }
    }
    class VFXMultiOperatorEdit<T, U> : VFXReorderableList, IControlledElement<T> where U : VFXOperatorNumeric, IVFXOperatorNumericUnified where T : VFXUnifiedOperatorControllerBase<U>
    {
        T m_Controller;
        Controller IControlledElement.controller => m_Controller;

        public T controller
        {
            get => m_Controller;
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

        void OnTypeMenu(PopupField<Type> dropdown, int index)
        {
            var op = controller.model;
            var choices = new List<Type>();
            if (op is IVFXOperatorNumericUnifiedConstrained constraintInterface && constraintInterface.slotIndicesThatCanBeScalar.Contains(index))
            {
                var otherSlotWithConstraint = op.inputSlots.Where((t, i) => constraintInterface.slotIndicesThatMustHaveSameType.Contains(i) && !constraintInterface.slotIndicesThatCanBeScalar.Contains(i)).FirstOrDefault();

                foreach (var type in op.validTypes)
                {
                    if (otherSlotWithConstraint == null || otherSlotWithConstraint.property.type == type || VFXUnifiedConstraintOperatorController.GetMatchingScalar(otherSlotWithConstraint.property.type) == type)
                        choices.Add(type);
                }
            }
            else
            {
                foreach (var type in op.validTypes)
                {
                    choices.Add(type);
                }
            }
            dropdown.userData = index;
            dropdown.choices = choices;
            dropdown.value = op.GetOperandType(index);
            dropdown.formatListItemCallback += x => x.UserFriendlyName();
            dropdown.formatSelectedValueCallback += x => x.UserFriendlyName();
            dropdown.RegisterValueChangedCallback(OnChangeType);
        }

        void OnChangeType(ChangeEvent<Type> evt)
        {
            var type = evt.newValue;
            var currentIndex = (int)((VisualElement)evt.target).userData;
            var op = controller.model;
            op.SetOperandType(currentIndex, type);

            if (op is IVFXOperatorNumericUnifiedConstrained constraintInterface)
            {
                if (!constraintInterface.slotIndicesThatCanBeScalar.Contains(currentIndex))
                {
                    foreach (var index in constraintInterface.slotIndicesThatMustHaveSameType)
                    {
                        if (index != currentIndex && (!constraintInterface.slotIndicesThatCanBeScalar.Contains(index) || VFXUnifiedConstraintOperatorController.GetMatchingScalar(type) != op.GetOperandType(index)))
                        {
                            op.SetOperandType(index, type);
                        }
                    }
                }
            }
        }

        void IControlledElement.OnControllerChanged(ref ControllerChangedEvent e)
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


            while (itemCount < count)
            {
                OperandInfoBase item = CreateOperandInfo(itemCount);
                item.Set(op);
                AddItem(item);
            }
            while (itemCount > count)
            {
                RemoveItemAt(itemCount - 1);
            }

            for (int i = 0; i < count; ++i)
            {
                OperandInfoBase operand = ItemAt(i) as OperandInfoBase;
                operand.index = i; // The operand might have been changed by the drag
                operand.Set(op);
            }

            m_SelfChanging = false;
        }

        protected virtual OperandInfoBase CreateOperandInfo(int index)
        {
            return new OperandInfoBase(this, controller.model, index);
        }

        protected class OperandInfoBase : VisualElement
        {
            PopupField<Type> type;
            public VFXMultiOperatorEdit<T, U> m_Owner;

            public int index;

            public OperandInfoBase(VFXMultiOperatorEdit<T, U> owner, U op, int index)
            {
                this.AddStyleSheetPathWithSkinVariant("VFXControls");
                m_Owner = owner;
                type = new PopupField<Type>();
                this.index = index;
                m_Owner.OnTypeMenu(type, index);
                Add(type);
            }

            public virtual void Set(U op)
            {
            }
        }
    }

    class VFXUnifiedOperatorEdit : VFXMultiOperatorEdit<VFXUnifiedOperatorController, VFXOperatorNumericUnified>
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

            public OperandInfo(VFXUnifiedOperatorEdit owner, VFXOperatorNumericUnified op, int index) : base(owner, op, index)
            {
                label = new Label();

                Insert(0, label);
            }

            public override void Set(VFXOperatorNumericUnified op)
            {
                base.Set(op);
                label.text = op.GetInputSlot(index).name;
            }
        }
    }
    class VFXCascadedOperatorEdit : VFXMultiOperatorEdit<VFXCascadedOperatorController, VFXOperatorNumericCascadedUnified>
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
            TextField field;

            public OperandInfo(VFXCascadedOperatorEdit owner, VFXOperatorNumericCascadedUnified op, int index) : base(owner, op, index)
            {
                field = new TextField();
                field.Q("unity-text-input").RegisterCallback<BlurEvent>(OnChangeValue, TrickleDown.TrickleDown);
                field.Q("unity-text-input").RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

                Insert(0, field);
            }

            void OnKeyDown(KeyDownEvent e)
            {
                if (e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Return)
                {
                    OnChangeValue(e);
                }
            }

            void OnChangeValue(EventBase evt)
            {
                (m_Owner as VFXCascadedOperatorEdit).OnChangeLabel(field.value, index);
            }

            public override void Set(VFXOperatorNumericCascadedUnified op)
            {
                base.Set(op);
                field.value = op.GetOperandName(index);
            }
        }
    }
}
