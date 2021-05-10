using System;
using UnityEngine.UI;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for enumerator widget.
    /// </summary>
    public class DebugUIHandlerEnumField : DebugUIHandlerWidget
    {
        /// <summary>Text displayed for the "next" button.</summary>
        public Text nextButtonText;
        /// <summary>Text displayed for the "previous" button.</summary>
        public Text previousButtonText;
        /// <summary>Name of the enum field.</summary>
        public Text nameLabel;
        /// <summary>Value of the enum field.</summary>
        public Text valueLabel;

        internal protected DebugUI.EnumField m_Field;

        internal override void SetWidget(DebugUI.Widget widget)
        {
            base.SetWidget(widget);
            m_Field = CastWidget<DebugUI.EnumField>();
            nameLabel.text = m_Field.displayName;
            UpdateValueLabel();
        }

        /// <summary>
        /// OnSelection implementation.
        /// </summary>
        /// <param name="fromNext">True if the selection wrapped around.</param>
        /// <param name="previous">Previous widget.</param>
        /// <returns>State of the widget.</returns>
        public override bool OnSelection(bool fromNext, DebugUIHandlerWidget previous)
        {
            if (nextButtonText != null)
                nextButtonText.color = colorSelected;
            if (previousButtonText != null)
                previousButtonText.color = colorSelected;
            nameLabel.color = colorSelected;
            valueLabel.color = colorSelected;
            return true;
        }

        /// <summary>
        /// OnDeselection implementation.
        /// </summary>
        public override void OnDeselection()
        {
            if (nextButtonText != null)
                nextButtonText.color = colorDefault;
            if (previousButtonText != null)
                previousButtonText.color = colorDefault;
            nameLabel.color = colorDefault;
            valueLabel.color = colorDefault;
        }

        /// <summary>
        /// OnAction implementation.
        /// </summary>
        public override void OnAction()
        {
            OnIncrement(false);
        }

        /// <summary>
        /// OnIncrement implementation.
        /// </summary>
        /// <param name="fast">True if incrementing fast.</param>
        public override void OnIncrement(bool fast)
        {
            if (m_Field.enumValues.Length == 0)
                return;

            var array = m_Field.enumValues;
            int index = m_Field.currentIndex;

            if (index == array.Length - 1)
            {
                index = 0;
            }
            else
            {
                if (fast)
                {
                    //check if quickSeparators have not been constructed
                    //it is the case when not constructed with autoenum
                    var separators = m_Field.quickSeparators;
                    if (separators == null)
                    {
                        m_Field.InitQuickSeparators();
                        separators = m_Field.quickSeparators;
                    }

                    int idxSup = 0;
                    for (; idxSup < separators.Length && index + 1 > separators[idxSup]; ++idxSup) ;
                    if (idxSup == separators.Length)
                    {
                        index = 0;
                    }
                    else
                    {
                        index = separators[idxSup];
                    }
                }
                else
                {
                    index += 1;
                }
            }

            m_Field.SetValue(array[index]);
            m_Field.currentIndex = index;
            UpdateValueLabel();
        }

        /// <summary>
        /// OnDecrement implementation.
        /// </summary>
        /// <param name="fast">Trye if decrementing fast.</param>
        public override void OnDecrement(bool fast)
        {
            if (m_Field.enumValues.Length == 0)
                return;

            var array = m_Field.enumValues;
            int index = m_Field.currentIndex;

            if (index == 0)
            {
                if (fast)
                {
                    //check if quickSeparators have not been constructed
                    //it is thecase when not constructed with autoenum
                    var separators = m_Field.quickSeparators;
                    if (separators == null)
                    {
                        m_Field.InitQuickSeparators();
                        separators = m_Field.quickSeparators;
                    }

                    index = separators[separators.Length - 1];
                }
                else
                {
                    index = array.Length - 1;
                }
            }
            else
            {
                if (fast)
                {
                    //check if quickSeparators have not been constructed
                    //it is the case when not constructed with autoenum
                    var separators = m_Field.quickSeparators;
                    if (separators == null)
                    {
                        m_Field.InitQuickSeparators();
                        separators = m_Field.quickSeparators;
                    }

                    int idxInf = separators.Length - 1;
                    for (; idxInf > 0 && index <= separators[idxInf]; --idxInf) ;
                    index = separators[idxInf];
                }
                else
                {
                    index -= 1;
                }
            }

            m_Field.SetValue(array[index]);
            m_Field.currentIndex = index;
            UpdateValueLabel();
        }

        /// <summary>
        /// Update the label of the widget.
        /// </summary>
        protected virtual void UpdateValueLabel()
        {
            int index = m_Field.currentIndex;

            // Fallback just in case, we may be handling sub/sectionned enums here
            if (index < 0)
                index = 0;

            string text = m_Field.enumNames[index].text;

            // The UI implementation is tight with space, so let's just truncate the string here if too long.
            const int maxLength = 26;
            if (text.Length > maxLength)
            {
                text = text.Substring(0, maxLength - 3) + "...";
            }

            valueLabel.text = text;
        }
    }
}
