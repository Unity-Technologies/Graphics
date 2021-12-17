using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.UIElements;
using UnityEditor.VFX.UIElements;
using VFXVector3Field = UnityEditor.VFX.UI.VFXVector3Field;
using System;

namespace UnityEditor.VFX.UI
{
    class SpaceablePropertyRM<T> : PropertyRM<T>
    {
        static readonly bool s_UseDropDownMenu = true;
        static readonly bool s_UseHovering = true;

        void OnMouseHover(EventBase evt)
        {
            if (m_Button == null || !m_Button.enabledSelf)
                return;

            if (evt.eventTypeId == MouseEnterEvent.TypeId())
                m_Button.AddToClassList("hovered");
            else
                m_Button.RemoveFromClassList("hovered");
        }

        public SpaceablePropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_Button = new VisualElement() { name = "spacebutton" };
            m_Button.AddManipulator(new Clickable(OnButtonClick));
            Add(m_Button);
            AddToClassList("spaceablepropertyrm");

            if (s_UseHovering)
            {
                RegisterCallback<MouseEnterEvent>(OnMouseHover);
                RegisterCallback<MouseLeaveEvent>(OnMouseHover);
            }
        }

        public override float GetPreferredControlWidth()
        {
            return 40;
        }

        public override float GetPreferredLabelWidth()
        {
            return base.GetPreferredLabelWidth() + spaceButtonWidth;
        }

        private VFXCoordinateSpace space
        {
            get
            {
                return m_Provider.space;
            }

            set
            {
                m_Provider.space = value;
            }
        }

        void ChangeSpace(object val)
        {
            space = (VFXCoordinateSpace)val;
        }

        void OnButtonClick()
        {
            var values = (VFXCoordinateSpace[])Enum.GetValues(space.GetType());

            if (s_UseDropDownMenu)
            {
                var menu = new GenericMenu();
                foreach (var spaceOption in values)
                {
                    menu.AddItem(
                        new GUIContent(ObjectNames.NicifyVariableName(spaceOption.ToString())),
                        spaceOption == space,
                        ChangeSpace,
                        spaceOption);
                }
                menu.DropDown(m_Button.worldBound);
            }
            else
            {
                var spaceCount = values.Length;
                var index = Array.IndexOf(values, space);
                var nextIndex = (index + 1) % spaceCount;
                space = values[nextIndex];
            }
        }

        public override void UpdateGUI(bool force)
        {
            foreach (string name in Enum.GetNames(typeof(VFXCoordinateSpace)))
            {
                if (space.ToString() != name)
                    m_Button.RemoveFromClassList("space" + name);
            }

            m_Button.AddToClassList("space" + space.ToString());
        }

        VisualElement m_Button;

        protected override void UpdateEnabled()
        {
            m_Button.SetEnabled(!m_Provider.IsSpaceInherited());
        }

        protected override void UpdateIndeterminate()
        {
        }

        private float spaceButtonWidth
        {
            get { return m_Button != null ? m_Button.layout.width + m_Button.resolvedStyle.marginLeft + m_Button.resolvedStyle.marginRight : 28; }
        }

        public override float effectiveLabelWidth
        {
            get
            {
                return m_labelWidth - spaceButtonWidth;
            }
        }

        public override bool showsEverything { get { return false; } }
    }

    abstract class Vector3SpaceablePropertyRM<T> : SpaceablePropertyRM<T>
    {
        public Vector3SpaceablePropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_VectorField = new VFXLabeledField<VFXVector3Field, Vector3>(m_Label);
            m_VectorField.RegisterCallback<ChangeEvent<Vector3>>(OnValueChanged);
            m_VectorField.AddToClassList("fieldContainer");

            m_VectorField.control.onValueDragFinished = ValueDragFinished;
            m_VectorField.control.onValueDragStarted = ValueDragStarted;
            Add(m_VectorField);
        }

        protected void ValueDragFinished()
        {
            m_Provider.EndLiveModification();
            hasChangeDelayed = false;
            NotifyValueChanged();
        }

        protected void ValueDragStarted()
        {
            m_Provider.StartLiveModification();
        }

        public override float GetPreferredControlWidth()
        {
            return 140;
        }

        public abstract void OnValueChanged(ChangeEvent<Vector3> e);

        protected VFXLabeledField<VFXVector3Field, Vector3> m_VectorField;

        protected override void UpdateEnabled()
        {
            base.UpdateEnabled();
            m_VectorField.control.SetEnabled(propertyEnabled);
        }

        protected override void UpdateIndeterminate()
        {
            base.UpdateEnabled();
            m_VectorField.visible = !indeterminate;
        }

        public override bool showsEverything { get { return true; } }
    }

    class VectorPropertyRM : Vector3SpaceablePropertyRM<Vector>
    {
        public VectorPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override void OnValueChanged(ChangeEvent<Vector3> e)
        {
            Vector3 newValue = m_VectorField.value;
            if (newValue != m_Value.vector)
            {
                m_Value.vector = newValue;
                NotifyValueChanged();
            }
        }

        public override void UpdateGUI(bool force)
        {
            base.UpdateGUI(force);
            m_VectorField.SetValueWithoutNotify(m_Value.vector);
        }
    }

    class PositionPropertyRM : Vector3SpaceablePropertyRM<Position>
    {
        public PositionPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override void OnValueChanged(ChangeEvent<Vector3> e)
        {
            Vector3 newValue = m_VectorField.value;
            if (newValue != m_Value.position)
            {
                m_Value.position = newValue;
                NotifyValueChanged();
            }
        }

        public override void UpdateGUI(bool force)
        {
            base.UpdateGUI(force);
            m_VectorField.SetValueWithoutNotify(m_Value.position);
        }
    }

    class DirectionPropertyRM : Vector3SpaceablePropertyRM<DirectionType>
    {
        public DirectionPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override void OnValueChanged(ChangeEvent<Vector3> e)
        {
            Vector3 newValue = m_VectorField.value;
            if (newValue != m_Value.direction)
            {
                m_Value.direction = newValue;
                NotifyValueChanged();
            }
        }

        public override void UpdateGUI(bool force)
        {
            base.UpdateGUI(force);
            m_VectorField.SetValueWithoutNotify(m_Value.direction);
        }
    }
}
