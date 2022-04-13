using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using Action = System.Action;

using FloatField = UnityEditor.VFX.UI.VFXLabeledField<UnityEditor.UIElements.FloatField, float>;
namespace UnityEditor.VFX.UI
{
    abstract class VFXVectorNField<T> : VFXControl<T>
    {
        FloatField[] m_Fields;
        VisualElement[] m_FieldParents;
        VisualElement[] m_TooltipHolders;

        protected abstract int componentCount { get; }
        public virtual string GetComponentName(int i)
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

        public override void SetEnabled(bool value)
        {
            for (int i = 0; i < componentCount; ++i)
            {
                m_Fields[i].SetEnabled(value);
                if (value)
                {
                    m_TooltipHolders[i].RemoveFromHierarchy();
                }
                else
                {
                    m_FieldParents[i].Add(m_TooltipHolders[i]);
                }
            }
        }

        void ValueDragFinished()
        {
            if (onValueDragFinished != null)
                onValueDragFinished();
        }

        void ValueDragStarted()
        {
            if (onValueDragStarted != null)
                onValueDragStarted();
        }

        public Action onValueDragFinished;
        public Action onValueDragStarted;

        void CreateTextField()
        {
            m_Fields = new FloatField[componentCount];
            m_FieldParents = new VisualElement[componentCount];
            m_TooltipHolders = new VisualElement[componentCount];

            for (int i = 0; i < m_Fields.Length; ++i)
            {
                m_Fields[i] = new FloatField(GetComponentName(i));
                m_Fields[i].control.AddToClassList("fieldContainer");
                m_Fields[i].AddToClassList("fieldContainer");
                m_Fields[i].RegisterCallback<ChangeEvent<float>, int>(OnValueChanged, i);


                m_Fields[i].onValueDragFinished = t => ValueDragFinished();
                m_Fields[i].onValueDragStarted = t => ValueDragStarted();

                m_FieldParents[i] = new VisualElement { name = "FieldParent" };
                m_FieldParents[i].Add(m_Fields[i]);
                m_FieldParents[i].style.flexGrow = 1;
                m_TooltipHolders[i] = new VisualElement { name = "TooltipHolder" };
                m_TooltipHolders[i].style.position = UnityEngine.UIElements.Position.Absolute;
                m_TooltipHolders[i].style.top = 0;
                m_TooltipHolders[i].style.left = 0;
                m_TooltipHolders[i].style.right = 0;
                m_TooltipHolders[i].style.bottom = 0;
                Add(m_FieldParents[i]);
            }

            m_Fields[0].label.AddToClassList("first");
        }

        public override bool indeterminate
        {
            get
            {
                return m_Fields[0].indeterminate;
            }
            set
            {
                foreach (var field in m_Fields)
                {
                    field.indeterminate = value;
                }
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

        public VFXVectorNField()
        {
            CreateTextField();

            style.flexDirection = FlexDirection.Row;
        }

        protected override void ValueToGUI(bool force)
        {
            T value = this.value;
            for (int i = 0; i < m_Fields.Length; ++i)
            {
                float componentValue = GetValueComponent(ref value, i);
                if (!m_Fields[i].control.HasFocus() || force)
                {
                    m_Fields[i].SetValueWithoutNotify(componentValue);
                }
                m_TooltipHolders[i].tooltip = componentValue.ToString();
            }
        }
    }
    class VFXVector3Field : VFXVectorNField<Vector3>
    {
        protected override int componentCount { get { return 3; } }
        protected override void SetValueComponent(ref Vector3 value, int i, float componentValue)
        {
            switch (i)
            {
                case 0:
                    value.x = componentValue;
                    break;
                case 1:
                    value.y = componentValue;
                    break;
                default:
                    value.z = componentValue;
                    break;
            }
        }

        protected override float GetValueComponent(ref Vector3 value, int i)
        {
            switch (i)
            {
                case 0:
                    return value.x;
                case 1:
                    return value.y;
                default:
                    return value.z;
            }
        }
    }
}
