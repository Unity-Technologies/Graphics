using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A rounded box to display a text.
    /// </summary>
    public class Pill : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<Pill, UxmlTraits> {}

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlBoolAttributeDescription m_Highlighted = new UxmlBoolAttributeDescription { name = "highlighted" };
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((Pill)ve).Highlighted = m_Highlighted.GetValueFromBag(bag, cc);
                ((Pill)ve).Text = m_Text.GetValueFromBag(bag, cc);
            }
        }

        public static readonly string ussClassName = "ge-pill";
        public static readonly string highlightedModifierClassName = ussClassName.WithUssModifier("highlighted");
        public static readonly string hasIconModifierClassName = ussClassName.WithUssModifier("has-icon");

        protected Label m_TitleLabel;
        protected Image m_Icon;
        protected bool m_Highlighted;

        public bool Highlighted
        {
            set
            {
                if (m_Highlighted == value)
                {
                    return;
                }

                m_Highlighted = value;

                EnableInClassList(highlightedModifierClassName, m_Highlighted);
            }
        }

        public string Text
        {
            set => m_TitleLabel.text = value;
        }

        public void SetIcon(Texture icon)
        {
            if (m_Icon == null || m_Icon.image == icon)
                return;

            m_Icon.image = icon;

            UpdateIconVisibility();
        }

        void UpdateIconVisibility()
        {
            EnableInClassList(hasIconModifierClassName, m_Icon.image != null);

            if (m_Icon.image == null)
            {
                m_Icon.style.visibility = Visibility.Hidden;
            }
            else
            {
                m_Icon.style.visibility = StyleKeyword.Null;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Pill"/> class.
        /// </summary>
        public Pill()
        {
            this.AddStylesheet("Pill.uss");

            var tpl = GraphElementHelper.LoadUXML("Pill.uxml");
            VisualElement mainContainer = tpl.Instantiate();

            m_TitleLabel = mainContainer.SafeQ<Label>("title-label");
            m_Icon = mainContainer.SafeQ<Image>("icon");

            Add(mainContainer);

            AddToClassList(ussClassName);

            UpdateIconVisibility();
        }
    }
}
