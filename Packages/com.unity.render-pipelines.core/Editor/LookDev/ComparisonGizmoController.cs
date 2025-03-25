using System;
using UnityEngine.UIElements;
using UnityEngine;

namespace UnityEditor.Rendering.LookDev
{
    //TODO: clamps to always have both node on screen
    class ComparisonGizmoController : Manipulator
    {
        const float k_DragPadding = 0.05f;
        const float k_ReferenceScale = 1080f;

        ComparisonGizmoState m_State;
        SwitchableCameraController m_Switcher;

        enum Selected
        {
            None,
            NodeFirstView,
            NodeSecondView,
            PlaneSeparator,
            Fader
        }
        Selected m_Selected;

        Vector2 m_SavedRelativePositionOnMouseDown;
        bool m_IsDragging;

        bool isDragging
        {
            get => m_IsDragging;
            set
            {
                //As in scene view, stop dragging as first button is release in case of multiple button down
                if (value ^ m_IsDragging)
                {
                    if (value)
                    {
                        target.RegisterCallback<MouseMoveEvent>(OnMouseDrag);
                        target.CaptureMouse();
                    }
                    else
                    {
                        target.ReleaseMouse();
                        target.UnregisterCallback<MouseMoveEvent>(OnMouseDrag);
                    }
                    m_IsDragging = value;
                }
            }
        }

        public ComparisonGizmoController(SwitchableCameraController switcher)
        {
            m_Switcher = switcher;
        }

        public void UpdateGizmoState(ComparisonGizmoState state)
        {
            m_State = state;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<WheelEvent>(OnScrollWheel);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<WheelEvent>(OnScrollWheel);
        }

        void OnScrollWheel(WheelEvent evt)
        {
            if (LookDev.currentContext.layout.viewLayout != Layout.CustomSplit)
                return;
            if (GetViewFromComposition(evt.localMousePosition) == ViewIndex.Second)
                m_Switcher.SwitchUntilNextWheelEvent();
            //let event be propagated to views
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            if (LookDev.currentContext.layout.viewLayout != Layout.CustomSplit)
                return;

            Rect displayRect = target.contentRect;
            SelectGizmoZone(GetNormalizedCoordinates(evt.localMousePosition, displayRect));
            if (m_Selected != Selected.None)
            {
                m_SavedRelativePositionOnMouseDown = GetNormalizedCoordinates(evt.localMousePosition, displayRect) - m_State.center;
                isDragging = true;
                //We do not want to move camera and gizmo at the same time.
                evt.StopImmediatePropagation();
            }
            else
            {
                //else let event be propagated to views
                if (GetViewFromComposition(evt.localMousePosition) == ViewIndex.Second)
                    m_Switcher.SwitchUntilNextEndOfDrag();
            }
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (LookDev.currentContext.layout.viewLayout != Layout.CustomSplit
                || m_Selected == Selected.None)
                return;

            // deadzone in fader gizmo
            if (m_Selected == Selected.Fader && Mathf.Abs(m_State.blendFactor) < ComparisonGizmoState.circleRadiusSelected / (m_State.length - ComparisonGizmoState.circleRadius))
                m_State.blendFactor = 0f;

            m_Selected = Selected.None;
            isDragging = false;
            //We do not want to move camera and gizmo at the same time.
            evt.StopImmediatePropagation();

            LookDev.SaveConfig();
        }

        void OnMouseDrag(MouseMoveEvent evt)
        {
            if (m_Selected == Selected.None)
                return;

            switch (m_Selected)
            {
                case Selected.PlaneSeparator: OnDragPlaneSeparator(evt); break;
                case Selected.NodeFirstView:
                case Selected.NodeSecondView: OnDragPlaneNodeExtremity(evt); break;
                case Selected.Fader: OnDragFader(evt); break;
                default: throw new ArgumentException("Unknown kind of Selected");
            }
        }

        void OnDragPlaneSeparator(MouseMoveEvent evt)
        {
            //TODO: handle case when resizing window (clamping)
            Vector2 newPosition = GetNormalizedCoordinates(evt.localMousePosition, target.contentRect) - m_SavedRelativePositionOnMouseDown;

            // We clamp the center of the gizmo to the border of the screen in order to avoid being able to put it out of the screen.
            // The safe band is here to ensure that you always see at least part of the gizmo in order to be able to grab it again.
            //Vector2 extends = GetNormalizedCoordinates(new Vector2(displayRect.width, displayRect.height), displayRect);
            //newPosition.x = Mathf.Clamp(newPosition.x, -extends.x + k_DragPadding, extends.x - k_DragPadding);
            //newPosition.y = Mathf.Clamp(newPosition.y, -extends.y + k_DragPadding, extends.y - k_DragPadding);

            m_State.Update(newPosition, m_State.length, m_State.angle);
            //We do not want to move camera and gizmo at the same time.
            evt.StopImmediatePropagation();
        }

        void OnDragPlaneNodeExtremity(MouseMoveEvent evt)
        {
            Vector2 normalizedCoord = GetNormalizedCoordinates(evt.localMousePosition, target.contentRect);
            Vector2 basePoint, newPoint;
            float angleSnapping = Mathf.Deg2Rad * 45.0f * 0.5f;

            newPoint = normalizedCoord;
            basePoint = m_Selected == Selected.NodeFirstView ? m_State.point2 : m_State.point1;

            // Snap to a multiple of "angleSnapping"
            if ((evt.modifiers & EventModifiers.Shift) != 0)
            {
                Vector3 verticalPlane = new Vector3(-1.0f, 0.0f, basePoint.x);
                float side = Vector3.Dot(new Vector3(normalizedCoord.x, normalizedCoord.y, 1.0f), verticalPlane);

                float angle = Mathf.Deg2Rad * Vector2.Angle(new Vector2(0.0f, 1.0f), normalizedCoord - basePoint);
                if (side > 0.0f)
                    angle = 2.0f * Mathf.PI - angle;
                angle = (int)(angle / angleSnapping) * angleSnapping;
                Vector2 dir = normalizedCoord - basePoint;
                float length = dir.magnitude;
                newPoint = basePoint + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * length;
            }

            if (m_Selected == Selected.NodeFirstView)
                m_State.Update(newPoint, basePoint);
            else
                m_State.Update(basePoint, newPoint);
            //We do not want to move camera and gizmo at the same time.
            evt.StopImmediatePropagation();
        }

        void OnDragFader(MouseMoveEvent evt)
        {
            Vector2 mousePosition = GetNormalizedCoordinates(evt.localMousePosition, target.contentRect);
            float distanceToOrthoPlane = -Vector3.Dot(new Vector3(mousePosition.x, mousePosition.y, 1.0f), m_State.planeOrtho) / m_State.blendFactorMaxGizmoDistance;
            m_State.blendFactor = Mathf.Clamp(distanceToOrthoPlane, -1.0f, 1.0f);
            //We do not want to move camera and gizmo at the same time.
            evt.StopImmediatePropagation();
        }

        void SelectGizmoZone(Vector2 normalizedMousePosition)
        {
            //TODO: Optimize
            Vector3 normalizedMousePositionZ1 = new Vector3(normalizedMousePosition.x, normalizedMousePosition.y, 1.0f);
            float distanceToPlane = Vector3.Dot(normalizedMousePositionZ1, m_State.plane);
            float absDistanceToPlane = Mathf.Abs(distanceToPlane);
            float distanceFromCenter = Vector2.Distance(normalizedMousePosition, m_State.center);
            float distanceToOrtho = Vector3.Dot(normalizedMousePositionZ1, m_State.planeOrtho);
            float side = (distanceToOrtho > 0.0f) ? 1.0f : -1.0f;
            Vector2 orthoPlaneNormal = new Vector2(m_State.planeOrtho.x, m_State.planeOrtho.y);

            Selected selected = Selected.None;
            if (absDistanceToPlane < ComparisonGizmoState.circleRadiusSelected && (distanceFromCenter < (m_State.length + ComparisonGizmoState.circleRadiusSelected)))
            {
                if (absDistanceToPlane < ComparisonGizmoState.thicknessSelected)
                    selected = Selected.PlaneSeparator;

                Vector2 circleCenter = m_State.center + m_State.length * side * orthoPlaneNormal;
                float d = Vector2.Distance(normalizedMousePosition, circleCenter);
                if (d <= ComparisonGizmoState.circleRadiusSelected)
                    selected = side > 0.0f ? Selected.NodeFirstView : Selected.NodeSecondView;

                float maxBlendCircleDistanceToCenter = m_State.blendFactorMaxGizmoDistance;
                float blendCircleDistanceToCenter = m_State.blendFactor * maxBlendCircleDistanceToCenter;
                Vector2 blendCircleCenter = m_State.center - orthoPlaneNormal * blendCircleDistanceToCenter;
                float blendCircleSelectionRadius = Mathf.Lerp(ComparisonGizmoState.blendFactorCircleRadius, ComparisonGizmoState.blendFactorCircleRadiusSelected, Mathf.Clamp((maxBlendCircleDistanceToCenter - Mathf.Abs(blendCircleDistanceToCenter)) / (ComparisonGizmoState.blendFactorCircleRadiusSelected - ComparisonGizmoState.blendFactorCircleRadius), 0.0f, 1.0f));
                if ((normalizedMousePosition - blendCircleCenter).magnitude < blendCircleSelectionRadius)
                    selected = Selected.Fader;
            }

            m_Selected = selected;
        }

        //normalize in [-1,1]^2 for a 1080^2. Can be above 1 for higher than 1080.
        internal static Vector2 GetNormalizedCoordinates(Vector2 localMousePosition, Rect rect)
            => new Vector2(
                (2f * localMousePosition.x - rect.width) / k_ReferenceScale,
                (-2f * localMousePosition.y + rect.height) / k_ReferenceScale);

        ViewIndex GetViewFromComposition(Vector2 localCoordinate)
        {
            Vector2 normalizedLocalCoordinate = GetNormalizedCoordinates(localCoordinate, target.contentRect);
            return Vector3.Dot(new Vector3(normalizedLocalCoordinate.x, normalizedLocalCoordinate.y, 1), m_State.plane) >= 0
                ? ViewIndex.First
                : ViewIndex.Second;
        }
    }
}
