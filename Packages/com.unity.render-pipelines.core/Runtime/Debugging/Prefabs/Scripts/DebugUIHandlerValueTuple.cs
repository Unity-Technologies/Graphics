using System;
using System.Collections;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for value tuple widget.
    /// </summary>
    public class DebugUIHandlerValueTuple : DebugUIHandlerWidget
    {
        /// <summary>Name of the value field.</summary>
        public Text nameLabel;
        /// <summary>Value of the value field.</summary>
        public Text valueLabel;

        protected internal DebugUI.ValueTuple m_Field;
        protected internal Text[] valueElements;

        const float k_XOffset = 230f;
        float m_Timer;
        static readonly Color k_ZeroColor = Color.gray;

        /// <summary>
        /// OnEnable implementation.
        /// </summary>
        protected override void OnEnable()
        {
            m_Timer = 0f;
        }

        /// <summary>
        /// OnSelection implementation.
        /// </summary>
        /// <param name="fromNext">True if the selection wrapped around.</param>
        /// <param name="previous">Previous widget.</param>
        /// <returns>True if the selection is allowed.</returns>
        public override bool OnSelection(bool fromNext, DebugUIHandlerWidget previous)
        {
            nameLabel.color = colorSelected;
            return true;
        }

        /// <summary>
        /// OnDeselection implementation.
        /// </summary>
        public override void OnDeselection()
        {
            nameLabel.color = colorDefault;
        }

        internal override void SetWidget(DebugUI.Widget widget)
        {
            m_Widget = widget;
            m_Field = CastWidget<DebugUI.ValueTuple>();
            nameLabel.text = m_Field.displayName;

            Debug.Assert(m_Field.numElements > 0);
            int numElements = m_Field.numElements;
            valueElements = new Text[numElements];
            valueElements[0] = valueLabel;
            float columnOffset = k_XOffset / (float)numElements;
            for (int index = 1; index < numElements; ++index)
            {
                var valueElement = Instantiate(valueLabel.gameObject, transform);
                valueElement.AddComponent<LayoutElement>().ignoreLayout = true;
                var rectTransform = valueElement.transform as RectTransform;
                var originalTransform = nameLabel.transform as RectTransform;
                rectTransform.anchorMax = rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.sizeDelta = new Vector2(100, 26);
                Vector3 pos = originalTransform.anchoredPosition;
                pos.x += (index + 1) * columnOffset + 200f;
                rectTransform.anchoredPosition = pos;
                rectTransform.pivot = new Vector2(0, 1);
                valueElements[index] = valueElement.GetComponent<Text>();
            }
        }

        internal virtual void UpdateValueLabels()
        {
            for (int index = 0; index < m_Field.numElements; ++index)
            {
                if (index < valueElements.Length && valueElements[index] != null)
                {
                    var value = m_Field.values[index].GetValue();
                    valueElements[index].text = m_Field.values[index].FormatString(value);
                    // De-emphasize zero values by switching to dark gray color
                    if (value is float)
                        valueElements[index].color = (float)value == 0f ? k_ZeroColor : colorDefault;
                }
            }
        }

        void Update()
        {
            if (m_Timer >= m_Field.refreshRate)
            {
                UpdateValueLabels();
                m_Timer -= m_Field.refreshRate;
            }

            m_Timer += Time.deltaTime;
        }
    }
}
