using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.UIElements;

using System;

namespace UnityEditor.VFX.UI
{
    class SpaceablePropertyRM<T> : PropertyRM<T>
    {
        static readonly bool s_UseDropDownMenu = true;

        Label m_Button;

        public SpaceablePropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            if (!string.IsNullOrEmpty(controller.name))
            {
                var label = new Label(ObjectNames.NicifyVariableName(controller.name));
                label.AddToClassList("label");
                Add(label);
            }
            m_Button = new Label("\u25bc") { name = "spacebutton" };
            m_Button.AddManipulator(new Clickable(OnButtonClick));
            Add(m_Button);
            AddToClassList("spaceablepropertyrm");
        }

        public override float GetPreferredControlWidth() => 40;

        private VFXSpace space
        {
            get => m_Provider.space;

            set => m_Provider.space = value;
        }

        void ChangeSpace(object val)
        {
            space = (VFXSpace)val;
        }

        void OnButtonClick()
        {
            var values = (VFXSpace[])Enum.GetValues(space.GetType());

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
            m_Button.RemoveFromClassList(VFXSpace.World.ToString());
            m_Button.RemoveFromClassList(VFXSpace.Local.ToString());
            m_Button.RemoveFromClassList(VFXSpace.None.ToString());
            m_Button.AddToClassList(space.ToString());
            m_Button.tooltip = $"{space.ToString()} Space";
        }

        protected override void UpdateEnabled()
        {
            m_Button.SetEnabled(!m_Provider.IsSpaceInherited());
        }

        protected override void UpdateIndeterminate()
        {
        }

        public override bool showsEverything => false;
    }

    abstract class Vector3SpaceablePropertyRM<T> : SpaceablePropertyRM<T>
    {
        public Vector3SpaceablePropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_VectorField = new VFXVector3Field();
            m_VectorField.RegisterCallback<ChangeEvent<Vector3>>(OnValueChanged);
            m_VectorField.AddToClassList("fieldContainer");
            m_VectorField.onValueDragFinished += ValueDragFinished;
            m_VectorField.onValueDragStarted += ValueDragStarted;

            Add(m_VectorField);
        }

        public override float GetPreferredControlWidth()
        {
            return 200;
        }

        public abstract void OnValueChanged(ChangeEvent<Vector3> e);

        protected VFXVector3Field m_VectorField;

        protected override void UpdateEnabled()
        {
            base.UpdateEnabled();
            m_VectorField.SetEnabled(propertyEnabled);
        }

        protected override void UpdateIndeterminate()
        {
            base.UpdateEnabled();
            m_VectorField.indeterminate = indeterminate;
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
