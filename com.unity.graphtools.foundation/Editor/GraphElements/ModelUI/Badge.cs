using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// An element that displays text and is attached to another element. Used to display annotations on nodes.
    /// </summary>
    public abstract class Badge : GraphElement
    {
        static CustomStyleProperty<int> s_DistanceProperty = new CustomStyleProperty<int>("--distance");
        static readonly int kDefaultDistanceValue = 6;

        protected VisualElement m_Target;
        protected VisualElement m_OriginalParent;
        protected int m_Distance;

        protected Attacher Attacher { get; private set; }
        protected SpriteAlignment Alignment { get; private set; }
        public IBadgeModel BadgeModel => Model as IBadgeModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="Badge"/> class.
        /// </summary>
        protected Badge()
        {
            m_Distance = kDefaultDistanceValue;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        protected virtual void Attach()
        {
            var visualElement = BadgeModel?.ParentModel?.GetUI<GraphElement>(View);
            if (visualElement != null)
            {
                AttachTo(visualElement, SpriteAlignment.RightCenter);
            }
        }

        protected void AttachTo(VisualElement target, SpriteAlignment alignment)
        {
            if (target == m_Target)
                return;

            Detach();
            m_Target = target;
            Alignment = alignment;
            target.RegisterCallback<DetachFromPanelEvent>(OnTargetDetachedFromPanel);
            CreateAttacher();
        }

        protected virtual void Detach()
        {
            if (m_Target != null)
            {
                m_Target.UnregisterCallback<DetachFromPanelEvent>(OnTargetDetachedFromPanel);
                m_Target = null;
            }

            ReleaseAttacher();
            m_OriginalParent = null;
        }

        protected void OnTargetDetachedFromPanel(DetachFromPanelEvent evt)
        {
            ReleaseAttacher();
            if (m_Target != null)
            {
                m_OriginalParent = hierarchy.parent;
                RemoveFromHierarchy();

                m_Target.UnregisterCallback<DetachFromPanelEvent>(OnTargetDetachedFromPanel);
                m_Target.RegisterCallback<AttachToPanelEvent>(OnTargetAttachedToPanel);
            }
        }

        protected void OnTargetAttachedToPanel(AttachToPanelEvent evt)
        {
            if (m_Target != null)
            {
                m_Target.RegisterCallback<DetachFromPanelEvent>(OnTargetDetachedFromPanel);

                //we re-add ourselves to the hierarchy
                m_OriginalParent?.hierarchy.Add(this);
                ReleaseAttacher();
                // the attacher will complain if reattaching too quickly when the hierarchy just entered the panel
                // ie. when switching back to the vs window tab
                schedule.Execute(CreateAttacher).StartingIn(0);
            }
        }

        protected void ReleaseAttacher()
        {
            if (Attacher != null)
            {
                Attacher.Detach();
                Attacher = null;
            }
        }

        protected void CreateAttacher()
        {
            Attacher = new Attacher(this, m_Target, Alignment) { Distance = m_Distance };
        }

        protected new void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            if (e.customStyle.TryGetValue(s_DistanceProperty, out var dist))
                m_Distance = dist;
        }

        protected void OnAttachToPanel(AttachToPanelEvent evt)
        {
            Attach();
        }

        protected void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            ReleaseAttacher();
        }
    }
}
