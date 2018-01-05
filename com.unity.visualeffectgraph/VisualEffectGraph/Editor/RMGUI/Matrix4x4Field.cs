using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;

namespace UnityEditor.VFX.UIElements
{
    class Matrix4x4Field : VFXControl<Matrix4x4>
    {
        LabeledField<FloatField, float>[,] m_FloatFields;
        public bool dynamicUpdate
        {
            get
            {
                return m_FloatFields[0, 0].control.dynamicUpdate;
            }
            set
            {
                for (int i = 0; i < m_FloatFields.GetLength(0); ++i)
                {
                    for (int j = 0; j < m_FloatFields.GetLength(1); ++j)
                    {
                        m_FloatFields[i, j].control.dynamicUpdate = value;
                    }
                }
            }
        }
        void CreateTextField()
        {
            m_FloatFields = new LabeledField<FloatField, float>[4, 4];


            for (int i = 0; i < m_FloatFields.GetLength(0); ++i)
            {
                for (int j = 0; j < m_FloatFields.GetLength(1); ++j)
                {
                    var newField = new LabeledField<FloatField, float>(string.Format("{0}{1}", i, j));
                    m_FloatFields[i, j] = newField;
                    newField.AddToClassList("fieldContainer");
                    newField.control.AddToClassList("fieldContainer");
                    newField.RegisterCallback<ChangeEvent<float>>(OnFloatValueChanged);
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

        public Matrix4x4Field()
        {
            CreateTextField();

            style.flexDirection = FlexDirection.Column;

            for (int i = 0; i < m_FloatFields.GetLength(0); ++i)
            {
                var line = new VisualElement() {name = "matrixLine"};
                line.style.flexDirection = FlexDirection.Row;

                for (int j = 0; j < m_FloatFields.GetLength(1); ++j)
                {
                    line.Add(m_FloatFields[i, j]);
                }

                Add(line);
            }
        }

        protected override void ValueToGUI()
        {
            Matrix4x4 value = this.value;
            for (int i = 0; i < m_FloatFields.GetLength(0); ++i)
            {
                for (int j = 0; j < m_FloatFields.GetLength(1); ++j)
                {
                    m_FloatFields[i, j].value = value[i, j];
                }
            }
        }
    }
}
