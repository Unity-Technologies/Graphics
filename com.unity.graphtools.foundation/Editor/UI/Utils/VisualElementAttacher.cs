using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Attach an element to a target. Whenever the target position changes, the element will follow the target.
    /// </summary>
    public class Attacher
    {
        List<VisualElement> m_WatchedObjects;
        Vector2 m_Offset;
        SpriteAlignment m_Alignment;
        float m_Distance;

        public VisualElement Target { get; }
        public VisualElement Element { get; }

        public SpriteAlignment Alignment
        {
            get => m_Alignment;
            set
            {
                if (m_Alignment != value)
                {
                    m_Alignment = value;
                    if (IsAttached)
                    {
                        AlignOnTarget();
                    }
                }
            }
        }

        public Vector2 Offset
        {
            get => m_Offset;
            set
            {
                if (m_Offset != value)
                {
                    m_Offset = value;
                    if (IsAttached)
                    {
                        AlignOnTarget();
                    }
                }
            }
        }

        public float Distance
        {
            get => m_Distance;
            set
            {
                if (m_Distance != value)
                {
                    m_Distance = value;
                    if (IsAttached)
                    {
                        AlignOnTarget();
                    }
                }
            }
        }

        bool IsAttached => Target != null && Element != null && m_WatchedObjects != null && m_WatchedObjects.Count > 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="Attacher"/> class.
        /// </summary>
        /// <param name="anchored">The element that will follow the target.</param>
        /// <param name="target">The target to follow.</param>
        /// <param name="alignment">How to align <paramref name="anchored"/> relative to <paramref name="target"/>.</param>
        public Attacher(VisualElement anchored, VisualElement target, SpriteAlignment alignment)
        {
            Distance = 6.0f;
            Target = target;
            Element = anchored;
            Alignment = alignment;

            Reattach();
        }

        public void Detach()
        {
            UnregisterCallbacks();
        }

        public void Reattach()
        {
            RegisterCallbacks();
            AlignOnTarget();
        }

        void RegisterCallbacks()
        {
            UnregisterCallbacks();

            VisualElement commonAncestor = Target.FindCommonAncestor(Element);

            if (commonAncestor == Target)
            {
                Debug.Log("Attacher: Target is already parent of anchored element.");
            }
            else if (commonAncestor == Element)
            {
                Debug.Log("Attacher: An element can't be anchored to one of its descendants");
            }
            else if (commonAncestor == null)
            {
                Debug.Log("Attacher: The element and its target must be in the same visual tree hierarchy");
            }
            else
            {
                if (m_WatchedObjects == null)
                    m_WatchedObjects = new List<VisualElement>();

                VisualElement v = Target;

                while (v != commonAncestor)
                {
                    m_WatchedObjects.Add(v);
                    v.RegisterCallback<GeometryChangedEvent>(OnTargetLayout);
                    v = v.hierarchy.parent;
                }

                v = Element;

                while (v != commonAncestor)
                {
                    m_WatchedObjects.Add(v);
                    v.RegisterCallback<GeometryChangedEvent>(OnTargetLayout);
                    v = v.hierarchy.parent;
                }
            }
        }

        void UnregisterCallbacks()
        {
            if (m_WatchedObjects == null || m_WatchedObjects.Count == 0)
                return;

            foreach (VisualElement v in m_WatchedObjects)
            {
                v.UnregisterCallback<GeometryChangedEvent>(OnTargetLayout);
            }

            m_WatchedObjects.Clear();
        }

        void OnTargetLayout(GeometryChangedEvent evt)
        {
            AlignOnTarget();
        }

        void AlignOnTarget()
        {
            Rect currentRect = new Rect(Element.resolvedStyle.left, Element.resolvedStyle.top, Element.resolvedStyle.width, Element.resolvedStyle.height);
            Rect targetRect = Target.GetRect();
            targetRect = Target.ChangeCoordinatesTo(Element.hierarchy.parent, targetRect);

            float centerY = 0;
            //align Vertically
            switch (Alignment)
            {
                case SpriteAlignment.TopLeft:
                case SpriteAlignment.TopCenter:
                case SpriteAlignment.TopRight:
                    centerY = targetRect.y - currentRect.height * 0.5f - Distance;
                    break;
                case SpriteAlignment.LeftCenter:
                case SpriteAlignment.RightCenter:
                case SpriteAlignment.Center:
                    centerY = targetRect.center.y;
                    break;
                case SpriteAlignment.BottomLeft:
                case SpriteAlignment.BottomCenter:
                case SpriteAlignment.BottomRight:
                    centerY = targetRect.yMax + currentRect.height * 0.5f + Distance;
                    break;
            }

            float centerX = 0;
            //alignHorizontally
            switch (Alignment)
            {
                case SpriteAlignment.TopLeft:
                case SpriteAlignment.LeftCenter:
                case SpriteAlignment.BottomLeft:
                    centerX = targetRect.x - currentRect.width * 0.5f - Distance;
                    break;
                case SpriteAlignment.TopCenter:
                case SpriteAlignment.Center:
                case SpriteAlignment.BottomCenter:
                    centerX = targetRect.center.x;
                    break;
                case SpriteAlignment.TopRight:
                case SpriteAlignment.RightCenter:
                case SpriteAlignment.BottomRight:
                    centerX = targetRect.xMax + currentRect.width * 0.5f + Distance;
                    break;
            }

            currentRect.center = new Vector2(centerX, centerY) + Offset;

            //we don't want the layout to be overwritten before styling has been applied
            if (currentRect.width > 0)
            {
                Element.SetLayout(currentRect);
            }
            else
            {
                Element.style.left = currentRect.xMin;
                Element.style.top = currentRect.yMin;
            }
        }
    }
}
