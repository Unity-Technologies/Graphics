using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A badge that displays an error message.
    /// </summary>
    public class ErrorBadge : Badge
    {
        public static readonly string ussName = "error-badge";
        public new static readonly string ussClassName = "ge-error-badge";
        public static readonly string iconUssClassName = ussClassName.WithUssElement("icon");
        public static readonly string tipUssClassName = ussClassName.WithUssElement("tip");
        public static readonly string textUssClassName = ussClassName.WithUssElement("text");
        public static readonly string hasErrorUssClassName = "ge-has-error-badge";

        public static readonly string hiddenModifierUssClassName = ussClassName.WithUssModifier("hidden");
        public static readonly string arrowHiddenModifierUssClassName = ussClassName.WithUssModifier("tip-hidden");

        public static readonly string sideTopModifierUssClassName = ussClassName.WithUssModifier("top");
        public static readonly string sideRightModifierUssClassName = ussClassName.WithUssModifier("right");
        public static readonly string sideBottomModifierUssClassName = ussClassName.WithUssModifier("bottom");
        public static readonly string sideLeftModifierUssClassName = ussClassName.WithUssModifier("left");

        static readonly string defaultCommonStylePath = "ErrorBadgeCommon.uss";
#if UNITY_2022_2_OR_NEWER
        static readonly string defaultStylePath = "ErrorBadge222.vuss";
#else
        static readonly string defaultStylePath = "ErrorBadge203.uss";
#endif
        protected Image m_TipElement;
        protected Image m_IconElement;
        protected Label m_TextElement;

        protected string m_BadgeType;

        protected string VisualStyle
        {
            set
            {
                if (m_BadgeType != value)
                {
                    RemoveFromClassList(ussClassName.WithUssModifier(m_BadgeType));

                    m_BadgeType = value;

                    AddToClassList(ussClassName.WithUssModifier(m_BadgeType));
                }
            }
        }

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
            base.BuildElementUI();
            name = "error-badge";

            m_TipElement = new Image { name = "tip" };
            Add(m_TipElement);
            m_TipElement.AddToClassList(tipUssClassName);

            m_IconElement = new Image { name = "icon" };
            Add(m_IconElement);
            m_IconElement.AddToClassList(iconUssClassName);

            m_TextElement = new Label { name = "text" };
            m_TextElement.AddToClassList(textUssClassName);
            m_TextElement.EnableInClassList(hiddenModifierUssClassName, true);
            Add(m_TextElement);

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
        }

        /// <inheritdoc />
        protected override void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            HideText();
            base.OnDetachedFromPanel(evt);
        }

        /// <inheritdoc />
        protected override void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (Attacher != null)
                PerformTipLayout();

            base.OnGeometryChanged(evt);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
            this.AddStylesheet(defaultCommonStylePath);
            this.AddStylesheet(defaultStylePath);

            //we need to add the style sheet to the Text element as well since it will be parented elsewhere
            m_TextElement.AddStylesheet(defaultCommonStylePath);
            m_TextElement.AddStylesheet(defaultStylePath);
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            if (Model is IErrorBadgeModel errorBadgeModel)
            {
                if (m_TextElement != null)
                {
                    m_TextElement.text = errorBadgeModel.ErrorMessage;
                }

                VisualStyle = errorBadgeModel.ErrorType.ToString().ToLower();
            }
        }

        /// <inheritdoc />
        protected override void Attach()
        {
            base.Attach();
            m_Target?.AddToClassList(hasErrorUssClassName);
        }

        /// <inheritdoc />
        protected override void Detach()
        {
            m_Target?.RemoveFromClassList(hasErrorUssClassName);
            base.Detach();
        }

        protected void ShowText()
        {
            if (m_TextElement?.hierarchy.parent != null && m_TextElement.ClassListContains(hiddenModifierUssClassName))
                m_TextElement?.EnableInClassList(hiddenModifierUssClassName, false);
        }

        protected void HideText()
        {
            if (m_TextElement?.hierarchy.parent != null && !m_TextElement.ClassListContains(hiddenModifierUssClassName))
                m_TextElement.EnableInClassList(hiddenModifierUssClassName, true);
        }

        void OnMouseEnter(MouseEnterEvent evt)
        {
            //we make sure we sit on top of whatever siblings we have
            BringToFront();
            ShowText();
        }

        void OnMouseLeave(MouseLeaveEvent evt)
        {
            HideText();
        }

        void PerformTipLayout()
        {
#if UNITY_2022_2_OR_NEWER
            RemoveFromClassList(arrowHiddenModifierUssClassName);

            RemoveFromClassList(sideTopModifierUssClassName);
            RemoveFromClassList(sideRightModifierUssClassName);
            RemoveFromClassList(sideBottomModifierUssClassName);
            RemoveFromClassList(sideLeftModifierUssClassName);

            switch (Alignment)
            {
                case SpriteAlignment.TopCenter:
                    AddToClassList(sideTopModifierUssClassName);
                    break;
                case SpriteAlignment.LeftCenter:
                    AddToClassList(sideLeftModifierUssClassName);
                    break;
                case SpriteAlignment.RightCenter:
                    AddToClassList(sideRightModifierUssClassName);
                    break;
                case SpriteAlignment.BottomCenter:
                    AddToClassList(sideBottomModifierUssClassName);
                    break;
                default:
                    AddToClassList(arrowHiddenModifierUssClassName);
                    break;
            }
#else
            float contentWidth = resolvedStyle.width;

            float arrowWidth = 0;
            float arrowLength = 0;

            if (m_TipElement != null)
            {
                arrowWidth = m_TipElement.resolvedStyle.width;
                arrowLength = m_TipElement.resolvedStyle.height;
            }

            float iconSize = 0f;
            if (m_IconElement != null)
            {
                iconSize = m_IconElement.GetComputedStyleWidth() == StyleKeyword.Auto ? contentWidth - arrowLength : m_IconElement.GetComputedStyleWidth().value.value;
            }

            float arrowOffset = Mathf.Floor((iconSize - arrowWidth) * 0.5f);

            Rect iconRect = new Rect(0, 0, iconSize, iconSize);
            float iconOffset = Mathf.Floor((contentWidth - iconSize) * 0.5f);

            Rect tipRect = new Rect(0, 0, arrowWidth, arrowLength);

            int tipAngle = 0;
            Vector2 tipTranslate = Vector2.zero;
            bool tipVisible = true;

            switch (Alignment)
            {
                case SpriteAlignment.TopCenter:
                    iconRect.x = iconOffset;
                    iconRect.y = 0;
                    tipRect.x = iconOffset + arrowOffset;
                    tipRect.y = iconRect.height;
                    tipTranslate = new Vector2(arrowWidth - 4, arrowLength - 5);
                    tipAngle = 180;
                    break;
                case SpriteAlignment.LeftCenter:
                    iconRect.y = iconOffset;
                    tipRect.x = iconRect.width;
                    tipRect.y = iconOffset + arrowOffset;
                    tipTranslate = new Vector2(arrowLength - 6,  +4);
                    tipAngle = 90;
                    break;
                case SpriteAlignment.RightCenter:
                    iconRect.y = iconOffset;
                    iconRect.x += arrowLength;
                    tipRect.y = iconOffset + arrowOffset;
                    tipTranslate = new Vector2(-1, arrowWidth - 3);
                    tipAngle = 270;
                    break;
                case SpriteAlignment.BottomCenter:
                    iconRect.x = iconOffset;
                    iconRect.y = arrowLength;
                    tipRect.x = iconOffset + arrowOffset;
                    tipTranslate = new Vector2(4, .5f);
                    tipAngle = 0;
                    break;
                default:
                    tipVisible = false;
                    break;
            }

            if (m_TipElement != null)
            {
                m_TipElement.transform.rotation = Quaternion.Euler(new Vector3(0, 0, tipAngle));
                m_TipElement.transform.position = new Vector3(tipTranslate.x, tipTranslate.y, 0);
            }

            m_IconElement?.SetLayout(iconRect);

            if (m_TipElement != null)
            {
                m_TipElement.SetLayout(tipRect);

                if (m_TipElement.visible != tipVisible)
                {
                    if (tipVisible)
                        m_TipElement.style.visibility = StyleKeyword.Null;
                    else
                        m_TipElement.style.visibility = Visibility.Hidden;
                }
            }

            if (m_TextElement != null)
            {
                if (m_TextElement.parent == this)
                {
                    m_TextElement.style.position = Position.Absolute;
                    m_TextElement.style.left = iconRect.xMax;
                    m_TextElement.style.top = iconRect.y;
                }
            }
#endif
        }
    }
}
