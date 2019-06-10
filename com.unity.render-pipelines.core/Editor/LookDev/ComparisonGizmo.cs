using System;
using UnityEngine.UIElements;
using UnityEngine;

namespace UnityEditor.Rendering.LookDev
{
    //TODO: clamps to always have both node on screen
    public class ComparisonGizmo : IDisposable
    {
        const float k_DragPadding = 0.05f;
        const float k_ReferenceScale = 1080f;

        ComparisonGizmoState state;
        IViewDisplayer displayer;

        enum Selected
        {
            None,
            NodeFirstView,
            NodeSecondView,
            PlaneSeparator,
            Fader
        }
        Selected selected;

        Vector2 savedRelativePositionOnMouseDown;

        public ComparisonGizmo(ComparisonGizmoState state, IViewDisplayer displayer)
        {
            this.state = state;
            this.displayer = displayer;
            displayer.OnMouseEventInView += Update;
        }

        void IDisposable.Dispose()
            => displayer.OnMouseEventInView -= Update;

        void Update(IMouseEvent mouseEvent)
        {
            //[TODO: handle or drop CustomCircular]
            if (LookDev.currentContext.layout.viewLayout == Layout.CustomSplit)
            {
                if (mouseEvent is MouseDownEvent)
                {
                    Rect displayRect = displayer.GetRect(ViewCompositionIndex.Composite);
                    SelectGizmoZone(GetNormalizedCoordinates(mouseEvent.localMousePosition, displayRect));
                    if (selected != Selected.None)
                    {
                        savedRelativePositionOnMouseDown = GetNormalizedCoordinates(mouseEvent.localMousePosition, displayRect) - state.center;
                        (mouseEvent as MouseDownEvent).StopImmediatePropagation();
                    }
                }
                else if (mouseEvent is MouseUpEvent)
                {
                    if (selected == Selected.Fader && Mathf.Abs(state.blendFactor) < ComparisonGizmoState.circleRadiusSelected / (state.length - ComparisonGizmoState.circleRadius))
                        state.blendFactor = 0f;
                    if (selected != Selected.None)
                        (mouseEvent as MouseUpEvent).StopImmediatePropagation();
                    selected = Selected.None;
                }
                else if (mouseEvent is MouseMoveEvent && selected != Selected.None)
                {
                    Rect displayRect = displayer.GetRect(ViewCompositionIndex.Composite);
                    switch (selected)
                    {
                        case Selected.PlaneSeparator: //dragging the gizmo
                            //TODO: handle case when resizing window (clamping)
                            Vector2 newPosition = GetNormalizedCoordinates(mouseEvent.localMousePosition, displayRect) - savedRelativePositionOnMouseDown;

                            // We clamp the center of the gizmo to the border of the screen in order to avoid being able to put it out of the screen.
                            // The safe band is here to ensure that you always see at least part of the gizmo in order to be able to grab it again.
                            //Vector2 extends = GetNormalizedCoordinates(new Vector2(displayRect.width, displayRect.height), displayRect);
                            //newPosition.x = Mathf.Clamp(newPosition.x, -extends.x + k_DragPadding, extends.x - k_DragPadding);
                            //newPosition.y = Mathf.Clamp(newPosition.y, -extends.y + k_DragPadding, extends.y - k_DragPadding);

                            state.Update(newPosition, state.length, state.angle);
                            break;

                        case Selected.NodeFirstView: //rotating the gizmo from A end
                        case Selected.NodeSecondView: //rotating the gizmo from B end
                            Vector2 normalizedCoord = GetNormalizedCoordinates(mouseEvent.localMousePosition, displayRect);
                            Vector2 basePoint, newPoint;
                            float angleSnapping = Mathf.Deg2Rad * 45.0f * 0.5f;

                            newPoint = normalizedCoord;
                            basePoint = selected == Selected.NodeFirstView ? state.point2 : state.point1;

                            // Snap to a multiple of "angleSnapping"
                            if ((mouseEvent.modifiers & EventModifiers.Shift) != 0)
                            {
                                Vector3 verticalPlane = new Vector3(-1.0f, 0.0f, basePoint.x);
                                float side = Vector3.Dot(new Vector3(normalizedCoord.x, normalizedCoord.y, 1.0f), verticalPlane);

                                float angle = Mathf.Deg2Rad * Vector2.Angle(new Vector2(0.0f, 1.0f), normalizedCoord - basePoint);
                                if (side > 0.0f)
                                    angle = 2.0f * Mathf.PI - angle;
                                angle = (int)(angle / angleSnapping) * angleSnapping;
                                Vector2 dir = normalizedCoord - basePoint;
                                float length = dir.magnitude; // we want to keep the length of the gizmo where it should be given the mouse position
                                newPoint = basePoint + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * length;
                            }

                            if (selected == Selected.NodeFirstView)
                                state.Update(newPoint, basePoint);
                            else
                                state.Update(basePoint, newPoint);
                            break;

                        case Selected.Fader:
                            Vector2 mousePosition = GetNormalizedCoordinates(mouseEvent.localMousePosition, displayRect);
                            float distanceToOrthoPlane = -Vector3.Dot(new Vector3(mousePosition.x, mousePosition.y, 1.0f), state.planeOrtho) / state.blendFactorMaxGizmoDistance;
                            state.blendFactor = Mathf.Clamp(distanceToOrthoPlane, -1.0f, 1.0f);
                            break;
                    }
                    if (selected != Selected.None)
                        LookDev.SaveConfig();

                    (mouseEvent as MouseMoveEvent).StopImmediatePropagation();
                }
                //let event be propagated elsewhere as we do not catch it for manipulation
            }
        }

        void SelectGizmoZone(Vector2 normalizedMousePosition)
        {
            //TODO: Optimize
            Vector3 normalizedMousePositionZ1 = new Vector3(normalizedMousePosition.x, normalizedMousePosition.y, 1.0f);
            float distanceToPlane = Vector3.Dot(normalizedMousePositionZ1, state.plane);
            float absDistanceToPlane = Mathf.Abs(distanceToPlane);
            float distanceFromCenter = Vector2.Distance(normalizedMousePosition, state.center);
            float distanceToOrtho = Vector3.Dot(normalizedMousePositionZ1, state.planeOrtho);
            float side = (distanceToOrtho > 0.0f) ? 1.0f : -1.0f;
            Vector2 orthoPlaneNormal = new Vector2(state.planeOrtho.x, state.planeOrtho.y);

            Selected selected = Selected.None;
            if (absDistanceToPlane < ComparisonGizmoState.circleRadiusSelected && (distanceFromCenter < (state.length + ComparisonGizmoState.circleRadiusSelected)))
            {
                if (absDistanceToPlane < ComparisonGizmoState.thicknessSelected)
                    selected = Selected.PlaneSeparator;

                Vector2 circleCenter = state.center + side * orthoPlaneNormal * state.length;
                float d = Vector2.Distance(normalizedMousePosition, circleCenter);
                if (d <= ComparisonGizmoState.circleRadiusSelected)
                    selected = side > 0.0f ? Selected.NodeFirstView : Selected.NodeSecondView;

                float maxBlendCircleDistanceToCenter = state.blendFactorMaxGizmoDistance;
                float blendCircleDistanceToCenter = state.blendFactor * maxBlendCircleDistanceToCenter;
                Vector2 blendCircleCenter = state.center - orthoPlaneNormal * blendCircleDistanceToCenter;
                float blendCircleSelectionRadius = Mathf.Lerp(ComparisonGizmoState.blendFactorCircleRadius, ComparisonGizmoState.blendFactorCircleRadiusSelected, Mathf.Clamp((maxBlendCircleDistanceToCenter - Mathf.Abs(blendCircleDistanceToCenter)) / (ComparisonGizmoState.blendFactorCircleRadiusSelected - ComparisonGizmoState.blendFactorCircleRadius), 0.0f, 1.0f));
                if ((normalizedMousePosition - blendCircleCenter).magnitude < blendCircleSelectionRadius)
                    selected = Selected.Fader;
            }
            
            this.selected = selected;
        }

        //normalize in [-1,1]^2 for a 1080^2. Can be above 1 for higher than 1080.
        internal static Vector2 GetNormalizedCoordinates(Vector2 localMousePosition, Rect rect)
            => new Vector2(
                (2f * localMousePosition.x - rect.width) / k_ReferenceScale,
                (-2f * localMousePosition.y + rect.height) / k_ReferenceScale);
    }
}
