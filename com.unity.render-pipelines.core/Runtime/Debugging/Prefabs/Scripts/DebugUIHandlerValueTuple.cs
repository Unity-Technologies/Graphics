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

        const float xDecal = 60f;
        float m_Timer;

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
            foreach (var elem in valueElements)
                elem.color = colorSelected;
            return true;
        }

        /// <summary>
        /// OnDeselection implementation.
        /// </summary>
        public override void OnDeselection()
        {
            nameLabel.color = colorDefault;
            foreach (var elem in valueElements)
                elem.color = colorDefault;
        }

        internal override void SetWidget(DebugUI.Widget widget)
        {
            m_Widget = widget;
            m_Field = CastWidget<DebugUI.ValueTuple>();
            nameLabel.text = m_Field.displayName;

            int numElements = m_Field.numElements;
            valueElements = new Text[numElements];
            valueElements[0] = valueLabel;
            for (int index = 1; index < numElements; ++index)
            {
                var valueElement = Instantiate(valueLabel, transform);
                Vector3 pos = valueElement.transform.position;
                pos.x += index * xDecal;
                valueElement.transform.position = pos;
                valueElements[index] = valueElement.GetComponent<Text>();
            }
        }

        internal virtual void UpdateValueLabels()
        {
            for (int index = 0; index < m_Field.numElements; ++index)
            {
                if (index < valueElements.Length && valueElements[index] != null)
                    valueElements[index].text = m_Field.values[index].GetValueString();
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
