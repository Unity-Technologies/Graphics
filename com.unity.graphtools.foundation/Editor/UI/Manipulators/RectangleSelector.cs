using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Manipulator to select elements by drawing a rectangle around them.
    /// </summary>
    public class RectangleSelector : MouseManipulator
    {
        static readonly List<ModelView> k_OnMouseUpAllUIs = new List<ModelView>();

        readonly RectangleSelect m_Rectangle;
        bool m_Active;

        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleSelector"/> class.
        /// </summary>
        public RectangleSelector()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Command });
            }
            else
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Control });
            }
            m_Rectangle = new RectangleSelect();
            m_Rectangle.style.position = Position.Absolute;
            m_Rectangle.style.top = 0f;
            m_Rectangle.style.left = 0f;
            m_Rectangle.style.bottom = 0f;
            m_Rectangle.style.right = 0f;
            m_Active = false;
        }

        // get the axis aligned bound
        Rect ComputeAxisAlignedBound(Rect position, Matrix4x4 transform)
        {
            Vector3 min = transform.MultiplyPoint3x4(position.min);
            Vector3 max = transform.MultiplyPoint3x4(position.max);
            return Rect.MinMaxRect(Math.Min(min.x, max.x), Math.Min(min.y, max.y), Math.Max(min.x, max.x), Math.Max(min.y, max.y));
        }

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            var graphView = target as GraphView;
            if (graphView == null)
            {
                throw new InvalidOperationException("Manipulator can only be added to a GraphView");
            }

            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);
        }

        protected void OnMouseCaptureOutEvent(MouseCaptureOutEvent e)
        {
            if (m_Active)
            {
                m_Rectangle.RemoveFromHierarchy();
                m_Active = false;
            }
        }

        protected void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (e.target != target)
            {
                return;
            }

            var graphView = target as GraphView;
            if (graphView == null)
                return;

            if (CanStartManipulation(e))
            {
                if (!e.actionKey)
                {
                    graphView.Dispatch(new ClearSelectionCommand());
                }

                graphView.Add(m_Rectangle);

                m_Rectangle.Start = e.localMousePosition;
                m_Rectangle.End = m_Rectangle.Start;

                m_Active = true;
                target.CaptureMouse(); // We want to receive events even when mouse is not over ourself.
                e.StopImmediatePropagation();
            }
        }

        protected void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active)
                return;

            var graphView = target as GraphView;
            if (graphView == null)
                return;

            if (!CanStopManipulation(e))
                return;

            graphView.Remove(m_Rectangle);

            m_Rectangle.End = e.localMousePosition;

            var selectionRect = new Rect()
            {
                min = new Vector2(Math.Min(m_Rectangle.Start.x, m_Rectangle.End.x), Math.Min(m_Rectangle.Start.y, m_Rectangle.End.y)),
                max = new Vector2(Math.Max(m_Rectangle.Start.x, m_Rectangle.End.x), Math.Max(m_Rectangle.Start.y, m_Rectangle.End.y))
            };

            selectionRect = ComputeAxisAlignedBound(selectionRect, graphView.ViewTransform.matrix.inverse);

            // a copy is necessary because Add To selection might cause a SendElementToFront which will change the order.
            List<ModelView> newSelection = new List<ModelView>();
            graphView.GraphModel?.GraphElementModels
                .Where(ge => ge.IsSelectable())
                .GetAllViewsInList(graphView, null, k_OnMouseUpAllUIs);
            foreach (var child in k_OnMouseUpAllUIs)
            {
                var localSelRect = graphView.ContentViewContainer.ChangeCoordinatesTo(child, selectionRect);
                if (child.Overlaps(localSelRect))
                {
                    newSelection.Add(child);
                }
            }
            k_OnMouseUpAllUIs.Clear();

            var mode = e.actionKey ? SelectElementsCommand.SelectionMode.Toggle : SelectElementsCommand.SelectionMode.Add;
            var newSelectedModels = newSelection.Select(elem => elem.Model).OfType<IGraphElementModel>().ToList();
            graphView.Dispatch(new SelectElementsCommand(mode, newSelectedModels));

            m_Active = false;
            target.ReleaseMouse();
            e.StopPropagation();
        }

        protected void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active)
                return;

            m_Rectangle.End = e.localMousePosition;
            e.StopPropagation();
        }

        class RectangleSelect : VisualElement
        {
            static readonly CustomStyleProperty<Color> k_FillColorProperty = new CustomStyleProperty<Color>("--fill-color");
            static readonly CustomStyleProperty<Color> k_BorderColorProperty = new CustomStyleProperty<Color>("--border-color");

            Vector2 m_End;
            Vector2 m_Start;

            static Color DefaultFillColor
            {
                get
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        return new Color(146 / 255f, 189 / 255f, 255 / 255f, 0.11f);
                    }

                    return new Color(146 / 255f, 189 / 255f, 255 / 255f, 0.32f);
                }
            }

            static Color DefaultBorderColor
            {
                get
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        return new Color(146 / 255f, 189 / 255f, 255 / 255f, 0.38f);
                    }

                    return new Color(255 / 255f, 255 / 255f, 255 / 255f, 0.67f);
                }
            }

            Color FillColor { get; set; } = DefaultFillColor;
            Color BorderColor { get; set; } = DefaultBorderColor;

            public Vector2 Start
            {
                get => m_Start;
                set
                {
                    m_Start = value;
                    MarkDirtyRepaint();
                }
            }

            public Vector2 End
            {
                get => m_End;
                set
                {
                    m_End = value;
                    MarkDirtyRepaint();
                }
            }

            public RectangleSelect()
            {
                generateVisualContent += OnGenerateVisualContent;
                RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            }

            void OnCustomStyleResolved(CustomStyleResolvedEvent e)
            {
                ICustomStyle styles = e.customStyle;

                if (styles.TryGetValue(k_BorderColorProperty, out var borderColor))
                    BorderColor = borderColor;

                if (styles.TryGetValue(k_FillColorProperty, out var fillColor))
                    FillColor = fillColor;
            }

            void OnGenerateVisualContent(MeshGenerationContext mgc)
            {
                // Avoid drawing useless information
                if (Start == End)
                    return;

                var r = new Rect
                {
                    min = new Vector2(Math.Min(Start.x, End.x), Math.Min(Start.y, End.y)),
                    max = new Vector2(Math.Max(Start.x, End.x), Math.Max(Start.y, End.y))
                };

                GraphViewStaticBridge.SolidRectangle(mgc, r, FillColor, ContextType.Editor);
                GraphViewStaticBridge.Border(mgc, r, BorderColor, ContextType.Editor);
            }
        }
    }
}
