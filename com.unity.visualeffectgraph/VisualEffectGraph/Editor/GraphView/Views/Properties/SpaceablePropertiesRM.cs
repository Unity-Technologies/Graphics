using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Experimental.UIElements;
using UnityEditor.VFX.UIElements;
using Vector3Field = UnityEditor.VFX.UIElements.Vector3Field;

namespace UnityEditor.VFX.UI
{
    class SpaceablePropertyRM<T> : PropertyRM<T> where T : ISpaceable
    {
        public SpaceablePropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_Button = new VisualElement() {name = "spacebutton"};
            m_Button.AddManipulator(new Clickable(OnButtonClick));
            Add(m_Button);
            AddToClassList("spaceablepropertyrm");
        }

        public override float GetPreferredControlWidth()
        {
            return 40;
        }

        void OnButtonClick()
        {
            m_Value.space = (CoordinateSpace)((int)(m_Value.space + 1) % CoordinateSpaceInfo.SpaceCount);
            NotifyValueChanged();
        }

        public override void UpdateGUI()
        {
            foreach (string name in System.Enum.GetNames(typeof(CoordinateSpace)))
            {
                m_Button.RemoveFromClassList("space" + name);
            }

            if (m_Value != null)
            {
                m_Button.AddToClassList("space" + m_Value.space.ToString());
            }
        }

        VisualElement m_Button;
        protected override void UpdateEnabled()
        {
            m_Button.SetEnabled(propertyEnabled);
        }

        public override float effectiveLabelWidth
        {
            get
            {
                return m_labelWidth - (m_Button != null ? m_Button.style.width : 16);
            }
        }

        public override bool showsEverything { get { return false; } }
    }

    abstract class Vector3SpaceablePropertyRM<T> : SpaceablePropertyRM<T> where T : ISpaceable
    {
        public Vector3SpaceablePropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_VectorField = new LabeledField<Vector3Field, Vector3>(m_Label);
            m_VectorField.RegisterCallback<ChangeEvent<Vector3>>(OnValueChanged);
            m_VectorField.AddToClassList("fieldContainer");

            Add(m_VectorField);
        }

        public override float GetPreferredControlWidth()
        {
            return 195;
        }

        public abstract void OnValueChanged(ChangeEvent<Vector3> e);

        protected LabeledField<Vector3Field, Vector3> m_VectorField;

        protected override void UpdateEnabled()
        {
            base.UpdateEnabled();
            m_VectorField.SetEnabled(propertyEnabled);
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

        public override void UpdateGUI()
        {
            base.UpdateGUI();
            m_VectorField.value = m_Value.vector;
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

        public override void UpdateGUI()
        {
            base.UpdateGUI();
            m_VectorField.value = m_Value.position;
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

        public override void UpdateGUI()
        {
            base.UpdateGUI();
            m_VectorField.value = m_Value.direction;
        }
    }
}
