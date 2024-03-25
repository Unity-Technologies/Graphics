using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    abstract class VFXVectorNField<T> : VFXControl<T>
    {
        FloatField[] m_Fields;

        protected abstract int componentCount { get; }

        private string GetComponentName(int i)
        {
            switch (i)
            {
                case 0:
                    return "x";
                case 1:
                    return "y";
                case 2:
                    return "z";
                case 3:
                    return "w";
                default:
                    return "a";
            }
        }

        public override void SetEnabled(bool v)
        {
            for (int i = 0; i < componentCount; ++i)
            {
                m_Fields[i].SetEnabled(v);
            }
        }

        void CreateTextField()
        {
            m_Fields = new FloatField[componentCount];

            for (int i = 0; i < m_Fields.Length; ++i)
            {
                var newField = new FloatField(GetComponentName(i));
                m_Fields[i] = newField;
                m_Fields[i].AddToClassList("fieldContainer");
                m_Fields[i].RegisterCallback<ChangeEvent<float>, int>(OnValueChanged, i);
                m_Fields[i].RegisterCallback<FocusOutEvent>(OnLostFocus);

                var label = newField.Q<Label>();
                label.RegisterCallback<PointerCaptureEvent>(ValueDragStarted);
                label.RegisterCallback<PointerCaptureOutEvent>(ValueDragFinished);

                Add(m_Fields[i]);
            }
        }

        private void OnLostFocus(FocusOutEvent focusOutEvent)
        {
            // Filter value if needed (range attribute for instance)
            this.ValueToGUI(true);
        }

        public override bool indeterminate
        {
            get => m_Fields[0].showMixedValue;
            set
            {
                foreach (var field in m_Fields)
                {
                    field.showMixedValue = value;
                }
                ValueToGUI(true);
            }
        }

        protected abstract void SetValueComponent(ref T value, int i, float componentValue);
        protected abstract float GetValueComponent(ref T value, int i);

        void OnValueChanged(ChangeEvent<float> e, int component)
        {
            T newValue = value;
            SetValueComponent(ref newValue, component, m_Fields[component].value);
            SetValueAndNotify(newValue);
        }

        protected VFXVectorNField()
        {
            CreateTextField();

            style.flexDirection = FlexDirection.Row;
        }

        protected override void ValueToGUI(bool force)
        {
            T v = this.value;
            for (int i = 0; i < m_Fields.Length; ++i)
            {
                float componentValue = GetValueComponent(ref v, i);
                if (!m_Fields[i].HasFocus() || force)
                {
                    m_Fields[i].SetValueWithoutNotify(componentValue);
                }
            }
        }
    }
    class VFXVector3Field : VFXVectorNField<Vector3>
    {
        protected override int componentCount => 3;

        protected override void SetValueComponent(ref Vector3 v, int i, float componentValue)
        {
            switch (i)
            {
                case 0:
                    v.x = componentValue;
                    break;
                case 1:
                    v.y = componentValue;
                    break;
                default:
                    v.z = componentValue;
                    break;
            }
        }

        protected override float GetValueComponent(ref Vector3 v, int i)
        {
            switch (i)
            {
                case 0:
                    return v.x;
                case 1:
                    return v.y;
                default:
                    return v.z;
            }
        }
    }
}
