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
    class LineDragger : Manipulator
    {
        MultiOperatorEdit m_Root;
        VisualElement m_Line;

        public LineDragger(MultiOperatorEdit root, VisualElement item)
        {
            m_Root = root;
            m_Line = item;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        bool m_Dragging;
        Vector2 startPosition;


        object m_Ctx;

        void Release()
        {
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.ReleaseMouseCapture();
            m_Dragging = false;
        }

        protected void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0)
            {
                evt.StopPropagation();
                target.TakeMouseCapture();
                m_Dragging = true;
                startPosition = evt.mousePosition;
                target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                m_Ctx = m_Root.StartDragging(m_Line);
            }
        }

        protected void OnMouseUp(MouseUpEvent evt)
        {
            m_Root.EndDragging(m_Ctx, m_Line, evt.mousePosition.y - startPosition.y);
            evt.StopPropagation();
            Release();
        }

        protected void OnMouseMove(MouseMoveEvent evt)
        {
            evt.StopPropagation();

            m_Root.ItemDragging(m_Ctx, m_Line, evt.mousePosition.y - startPosition.y);
        }
    }

    class MultiOperatorEdit : VisualElement, IControlledElement<VFXOperatorController>
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


        class DraggingContext
        {
            public Rect[] originalPositions;
            public Rect myOriginalPosition;
            public int draggedIndex;
        }

        public object StartDragging(VisualElement item)
        {
            //Fix all item so that they can be animated and we can control their positions
            DraggingContext context = new DraggingContext();


            var children = m_OperandContainer.Children().ToArray();
            context.originalPositions = children.Select(t => t.layout).ToArray();
            context.draggedIndex = m_OperandContainer.IndexOf(item);
            context.myOriginalPosition = m_OperandContainer.layout;

            for (int i = 0; i < children.Length; ++i)
            {
                VisualElement child = children[i];
                Rect rect = context.originalPositions[i];
                child.style.positionType = PositionType.Absolute;
                child.style.positionLeft = rect.x;
                child.style.positionTop = rect.y;
                child.style.width = rect.width;
                child.style.height = rect.height;
            }

            item.BringToFront();

            m_OperandContainer.style.width = context.myOriginalPosition.width;
            m_OperandContainer.style.height = context.myOriginalPosition.height;

            return context;
        }

        public void EndDragging(object ctx, VisualElement item, float offset)
        {
            DraggingContext context = (DraggingContext)ctx;

            foreach (var child in m_OperandContainer.Children())
            {
                child.ResetPositionProperties();
            }
            m_OperandContainer.Insert(context.draggedIndex, item);
            m_OperandContainer.ResetPositionProperties();
        }

        public void ItemDragging(object ctx, VisualElement item, float offset)
        {
            DraggingContext context = (DraggingContext)ctx;

            item.style.positionTop = context.originalPositions[context.draggedIndex].y + offset;
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

        VisualElement m_OperandContainer;

        public MultiOperatorEdit()
        {
            RegisterCallback<ControllerChangedEvent>(OnChange);

            m_OperandContainer = new VisualElement() {name = "OperandContainer"};

            Add(m_OperandContainer);

            Add(new Button(OnAdd) {text = "Add"});
        }

        void OnAdd()
        {
            VFXEditableOperator op = model;

            op.AddOperand();
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
            VFXEditableOperator op = model;

            if (value != op.GetOperandName(index)) // test mandatory because TextField might send ChangeEvent anytime
                op.SetOperandName(index, value);
        }

        void OnChange(ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                SelfChange();
            }
        }

        void SelfChange()
        {
            VFXEditableOperator op = model;
            int count = op.operandCount;

            bool sizeChanged = false;

            while (m_OperandContainer.childCount < count)
            {
                m_OperandContainer.Add(new OperandInfo(this, op, m_OperandContainer.childCount));
                sizeChanged = true;
            }
            while (m_OperandContainer.childCount > count)
            {
                m_OperandContainer.ElementAt(m_OperandContainer.childCount - 1).RemoveFromHierarchy();
                sizeChanged = true;
            }

            for (int i = 0; i < count; ++i)
            {
                (m_OperandContainer.ElementAt(i) as OperandInfo).Set(op);
            }

            VFXOperatorUI opUI = GetFirstAncestorOfType<VFXOperatorUI>();
            if (opUI != null)
            {
                opUI.ForceRefreshLayout();
            }
        }

        class OperandInfo : VisualElement
        {
            public VFXStringField field;
            public Button type;
            public VisualElement draggingHandle;

            MultiOperatorEdit m_Owner;

            public int index;

            public OperandInfo(MultiOperatorEdit owner, VFXEditableOperator op, int index)
            {
                m_Owner = owner;
                draggingHandle = new VisualElement() { name = "DraggingHandle"};
                field = new VFXStringField("name");
                field.OnValueChanged = () => owner.OnChangeLabel(field.value, index);
                type = new Button();
                this.index = index;
                type.AddToClassList("PopupButton");
                type.AddManipulator(new DownClickable(() => owner.OnTypeMenu(type, index)));
                Set(op);

                Add(draggingHandle);
                draggingHandle.AddManipulator(new LineDragger(m_Owner, this));
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
