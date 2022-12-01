using UnityEngine;
using UnityEngine.UIElements;


using Action = System.Action;

using FloatField = UnityEditor.VFX.UI.VFXLabeledField<UnityEngine.UIElements.FloatField, float>;

namespace UnityEditor.VFX.UI
{
    class VFXMatrix4x4Field : VFXControl<Matrix4x4>
    {
        FloatField[,] m_FloatFields;
        void CreateTextField()
        {
            m_FloatFields = new FloatField[4, 4];


            for (int i = 0; i < m_FloatFields.GetLength(0); ++i)
            {
                for (int j = 0; j < m_FloatFields.GetLength(1); ++j)
                {
                    var newField = new FloatField(string.Format("{0}{1}", i, j));
                    m_FloatFields[i, j] = newField;
                    newField.AddToClassList("fieldContainer");
                    newField.control.AddToClassList("fieldContainer");
                    newField.RegisterCallback<ChangeEvent<float>>(OnFloatValueChanged);

                    newField.SetOnValueDragFinished(t => ValueDragFinished());
                    newField.SetOnValueDragStarted(t => ValueDragStarted());
                }
            }
        }

        public override bool indeterminate
        {
            get
            {
                return m_FloatFields[0, 0].indeterminate;
            }
            set
            {
                for (int i = 0; i < m_FloatFields.GetLength(0); ++i)
                {
                    for (int j = 0; j < m_FloatFields.GetLength(1); ++j)
                    {
                        m_FloatFields[i, j].indeterminate = value;
                    }
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

        void OnFloatValueChanged(ChangeEvent<float> e)
        {
            Matrix4x4 newValue = value;

            int i = 0;
            int j = 0;
            bool found = false;
            for (; i < m_FloatFields.GetLength(0); ++i)
            {
                j = 0;
                for (; j < m_FloatFields.GetLength(1); ++j)
                {
                    if (m_FloatFields[i, j] == e.target)
                    {
                        found = true;
                        break;
                    }
                }
                if (found)
                    break;
            }

            if (i < m_FloatFields.GetLength(0) && j < m_FloatFields.GetLength(1))
            {
                newValue[i, j] = e.newValue;
                SetValueAndNotify(newValue);
            }
        }

        public override void SetEnabled(bool value)
        {
            for (int i = 0; i < m_FloatFields.GetLength(0); ++i)
            {
                for (int j = 0; j < m_FloatFields.GetLength(1); ++j)
                {
                    m_FloatFields[i, j].SetEnabled(value);
                }
            }
        }

        public VFXMatrix4x4Field()
        {
            CreateTextField();

            style.flexDirection = FlexDirection.Column;

            for (int i = 0; i < m_FloatFields.GetLength(0); ++i)
            {
                var line = new VisualElement() { name = "matrixLine" };
                line.style.flexDirection = FlexDirection.Row;

                for (int j = 0; j < m_FloatFields.GetLength(1); ++j)
                {
                    line.Add(m_FloatFields[i, j]);
                }

                Add(line);
            }
        }

        protected override void ValueToGUI(bool force)
        {
            Matrix4x4 value = this.value;
            for (int i = 0; i < m_FloatFields.GetLength(0); ++i)
            {
                for (int j = 0; j < m_FloatFields.GetLength(1); ++j)
                {
                    if (!m_FloatFields[i, j].control.HasFocus() || force)
                    {
                        m_FloatFields[i, j].value = value[i, j];
                    }
                }
            }
        }
    }
}
