using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A badge that displays an error message.
    /// </summary>
    public class ErrorBadge : Badge
    {
        public new static readonly string ussClassName = "ge-error-badge";
        public static readonly string iconUssClassName = ussClassName.WithUssElement("icon");
        public static readonly string tipUssClassName = ussClassName.WithUssElement("tip");
        public static readonly string textUssClassName = ussClassName.WithUssElement("text");
        public static readonly string hasErrorUssClassName = "ge-has-error-badge";

        static readonly string defaultStylePath = "ErrorBadge.uss";

        protected Image m_TipElement;
        protected Image m_IconElement;
        protected Label m_TextElement;

        protected Attacher m_TextAttacher;

        protected int m_CurrentTipAngle;

        protected string m_BadgeType;

        public string VisualStyle
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

            LoadTemplate();

            VisualStyle = "error";
        }

        void LoadTemplate()
        {
            name = "error-badge";

            m_TipElement = new Image { name = "tip" };
            Add(m_TipElement);
            m_TipElement.AddToClassList(tipUssClassName);

            m_IconElement = new Image { name = "icon" };
            Add(m_IconElement);
            m_IconElement.AddToClassList(iconUssClassName);

            m_TextElement = new Label { name = "text" };
            m_TextElement.AddToClassList(textUssClassName);
            //we need to add the style sheet to the Text element as well since it will be parented elsewhere
            m_TextElement.AddStylesheet(defaultStylePath);
            m_TextElement.RegisterCallback<GeometryChangedEvent>(evt => ComputeTextSize());
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
            this.AddStylesheet(defaultStylePath);
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

        void ComputeTextSize()
        {
            if (m_TextElement != null)
            {
                float maxWidth = m_TextElement.resolvedStyle.maxWidth == StyleKeyword.None ? float.NaN : m_TextElement.resolvedStyle.maxWidth.value;
                Vector2 newSize = m_TextElement.DoMeasure(maxWidth, MeasureMode.AtMost,
                    0, MeasureMode.Undefined);

                m_TextElement.style.width = newSize.x +
                    m_TextElement.resolvedStyle.marginLeft +
                    m_TextElement.resolvedStyle.marginRight +
                    m_TextElement.resolvedStyle.borderLeftWidth +
                    m_TextElement.resolvedStyle.borderRightWidth +
                    m_TextElement.resolvedStyle.paddingLeft +
                    m_TextElement.resolvedStyle.paddingRight;

                float height = newSize.y +
                    m_TextElement.resolvedStyle.marginTop +
                    m_TextElement.resolvedStyle.marginBottom +
                    m_TextElement.resolvedStyle.borderTopWidth +
                    m_TextElement.resolvedStyle.borderBottomWidth +
                    m_TextElement.resolvedStyle.paddingTop +
                    m_TextElement.resolvedStyle.paddingBottom;

                m_TextElement.style.height = height;

                if (m_TextAttacher != null)
                {
                    m_TextAttacher.Offset = new Vector2(0, height);
                }

                PerformTipLayout();
            }
        }

        protected void ShowText()
        {
            if (m_TextElement != null && m_TextElement.hierarchy.parent == null)
            {
                VisualElement textParent = this;

                if (GraphView != null)
                {
                    textParent = GraphView;
                }

                textParent.Add(m_TextElement);

                if (textParent != this)
                {
                    if (m_TextAttacher == null)
                    {
                        m_TextAttacher = new Attacher(m_TextElement, m_IconElement, SpriteAlignment.TopRight);
                    }
                    else
                    {
                        m_TextAttacher.Reattach();
                    }
                }
                m_TextAttacher.Distance = 0;
                m_TextElement.ResetPositionProperties();

                ComputeTextSize();
            }
        }

        protected void HideText()
        {
            if (m_TextElement?.hierarchy.parent != null)
            {
                m_TextAttacher?.Detach();
                m_TextElement.RemoveFromHierarchy();
            }
        }

        /// <inheritdoc />
#if UNITY_2022_1_OR_NEWER
        [EventInterest(typeof(GeometryChangedEvent), typeof(DetachFromPanelEvent),
            typeof(MouseEnterEvent), typeof(MouseLeaveEvent))]
#endif
        protected override void ExecuteDefaultAction(EventBase evt)
        {
            if (evt.eventTypeId == GeometryChangedEvent.TypeId())
            {
                if (Attacher != null)
                    PerformTipLayout();
            }
            else if (evt.eventTypeId == DetachFromPanelEvent.TypeId())
            {
                HideText();
            }
            else if (evt.eventTypeId == MouseEnterEvent.TypeId())
            {
                //we make sure we sit on top of whatever siblings we have
                BringToFront();
                ShowText();
            }
            else if (evt.eventTypeId == MouseLeaveEvent.TypeId())
            {
                HideText();
            }

            base.ExecuteDefaultAction(evt);
        }

        void PerformTipLayout()
        {
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
                    tipTranslate = new Vector2(arrowWidth, arrowLength);
                    tipAngle = 180;
                    break;
                case SpriteAlignment.LeftCenter:
                    iconRect.y = iconOffset;
                    tipRect.x = iconRect.width;
                    tipRect.y = iconOffset + arrowOffset;
                    tipTranslate = new Vector2(arrowLength, 0);
                    tipAngle = 90;
                    break;
                case SpriteAlignment.RightCenter:
                    iconRect.y = iconOffset;
                    iconRect.x += arrowLength;
                    tipRect.y = iconOffset + arrowOffset;
                    tipTranslate = new Vector2(0, arrowWidth);
                    tipAngle = 270;
                    break;
                case SpriteAlignment.BottomCenter:
                    iconRect.x = iconOffset;
                    iconRect.y = arrowLength;
                    tipRect.x = iconOffset + arrowOffset;
                    tipTranslate = new Vector2(0, 0);
                    tipAngle = 0;
                    break;
                default:
                    tipVisible = false;
                    break;
            }

            if (tipAngle != m_CurrentTipAngle)
            {
                if (m_TipElement != null)
                {
                    m_TipElement.transform.rotation = Quaternion.Euler(new Vector3(0, 0, tipAngle));
                    m_TipElement.transform.position = new Vector3(tipTranslate.x, tipTranslate.y, 0);
                }
                m_CurrentTipAngle = tipAngle;
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
        }
    }
}
