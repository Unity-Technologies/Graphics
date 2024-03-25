using UnityEngine;
using UnityEngine.UIElements;

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
                    var newField = new FloatField($"{i}{j}");
                    m_FloatFields[i, j] = newField;
                    newField.AddToClassList("fieldContainer");
                    newField.AddToClassList("fieldContainer");
                    newField.RegisterCallback<ChangeEvent<float>>(OnFloatValueChanged);
                    var label = newField.Q<Label>();
                    label.RegisterCallback<PointerCaptureEvent>(ValueDragStarted);
                    label.RegisterCallback<PointerCaptureOutEvent>(ValueDragFinished);
                }
            }
        }

        public override bool indeterminate
        {
            get
            {
                return m_FloatFields[0, 0].showMixedValue;
            }
            set
            {
                for (int i = 0; i < m_FloatFields.GetLength(0); ++i)
                {
                    for (int j = 0; j < m_FloatFields.GetLength(1); ++j)
                    {
                        m_FloatFields[i, j].showMixedValue = value;
                    }
                }
            }
        }

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

        public VFXMatrix4x4Field(string label)
        {
            CreateTextField();

            if (!string.IsNullOrEmpty(label))
            {
                var labelElement = new Label(label);
                labelElement.AddToClassList("label");
                Add(labelElement);
            }

            var matrixContainer = new VisualElement { name = "matrixContainer" };
            for (int i = 0; i < m_FloatFields.GetLength(0); ++i)
            {
                var line = new VisualElement { name = "matrixLine" };
                for (int j = 0; j < m_FloatFields.GetLength(1); ++j)
                {
                    line.Add(m_FloatFields[i, j]);
                }

                matrixContainer.Add(line);
            }
            Add(matrixContainer);
        }

        protected override void ValueToGUI(bool force)
        {
            Matrix4x4 value = this.value;
            for (int i = 0; i < m_FloatFields.GetLength(0); ++i)
            {
                for (int j = 0; j < m_FloatFields.GetLength(1); ++j)
                {
                    if (!m_FloatFields[i, j].HasFocus() || force)
                    {
                        m_FloatFields[i, j].value = value[i, j];
                    }
                }
            }
        }
    }
}
