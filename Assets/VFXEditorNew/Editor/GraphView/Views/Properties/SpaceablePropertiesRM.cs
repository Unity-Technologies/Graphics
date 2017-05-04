using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;
using UnityEditor.VFX;
using UnityEditor.VFX.UIElements;
using Object = UnityEngine.Object;
using Type = System.Type;

namespace UnityEditor.VFX.UI
{
    class SpaceablePropertyRM<T> : PropertyRM<T> where T : Spaceable
    {
        public SpaceablePropertyRM(IPropertyRMProvider presenter) : base(presenter)
        {
            m_Button = new VisualElement() {name = "spacebutton"};
            m_Button.AddManipulator(new Clickable(OnButtonClick));
            AddChild(m_Button);
            AddToClassList("spaceablepropertyrm");
        }

        void OnButtonClick()
        {
            m_Value.space = (CoordinateSpace)((int)(m_Value.space + 1) % (int)CoordinateSpace.SpaceCount);
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

        public override bool enabled
        {
            set
            {
                base.enabled = value;
                if (m_Button != null)
                    m_Button.enabled = value;
            }
        }

        public override float defaultLabelWidth
        {
            get
            {
                return 64;
            }
        }
    }

    abstract class Vector3SpaceablePropertyRM<T> : SpaceablePropertyRM<T> where T : Spaceable
    {
        public Vector3SpaceablePropertyRM(IPropertyRMProvider presenter) : base(presenter)
        {
            m_VectorField = new Vector3Field(m_Label);
            m_VectorField.onValueChanged = OnValueChanged;
            m_VectorField.AddToClassList("fieldContainer");

            AddChild(m_VectorField);
        }

        public abstract void OnValueChanged();

        protected Vector3Field m_VectorField;

        public override bool enabled
        {
            set
            {
                base.enabled = value;
                if (m_VectorField != null)
                    m_VectorField.enabled = value;
            }
        }
    }

    class VectorPropertyRM : Vector3SpaceablePropertyRM<Vector>
    {
        public VectorPropertyRM(IPropertyRMProvider presenter) : base(presenter)
        {
        }

        public override void OnValueChanged()
        {
            Vector3 newValue = m_VectorField.GetValue();
            if (newValue != m_Value.vector)
            {
                m_Value.vector = newValue;
                NotifyValueChanged();
            }
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();
            m_VectorField.SetValue(m_Value.vector);
        }
    }

    class PositionPropertyRM : Vector3SpaceablePropertyRM<Position>
    {
        public PositionPropertyRM(IPropertyRMProvider presenter) : base(presenter)
        {
        }

        public override void OnValueChanged()
        {
            Vector3 newValue = m_VectorField.GetValue();
            if (newValue != m_Value.position)
            {
                m_Value.position = newValue;
                NotifyValueChanged();
            }
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();
            m_VectorField.SetValue(m_Value.position);
        }
    }
}
