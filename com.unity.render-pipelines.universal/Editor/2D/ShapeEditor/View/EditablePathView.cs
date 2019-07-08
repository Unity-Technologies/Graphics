using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.Rendering.Universal.Path2D.GUIFramework;

namespace UnityEditor.Experimental.Rendering.Universal.Path2D
{
    internal class EditablePathView : IEditablePathView
    {
        const float kSnappingDistance = 15f;
        const string kDeleteCommandName = "Delete";
        const string kSoftDeleteCommandName = "SoftDelete";
        public IEditablePathController controller { get; set; }
        private Control m_PointControl;
        private Control m_EdgeControl;
        private Control m_LeftTangentControl;
        private Control m_RightTangentControl;
        private GUIAction m_MovePointAction;
        private GUIAction m_MoveEdgeAction;
        private GUIAction m_CreatePointAction;
        private GUIAction m_RemovePointAction1;
        private GUIAction m_RemovePointAction2;
        private GUIAction m_MoveLeftTangentAction;
        private GUIAction m_MoveRightTangentAction;
        private IDrawer m_Drawer;

        public EditablePathView() : this(new Drawer()) { }
        
        public EditablePathView(IDrawer drawer)
        {
            m_Drawer = drawer;

            m_PointControl = new GenericControl("Point")
            {
                count = GetPointCount,
                distance = (guiState, i) =>
                {
                    var position = GetPoint(i).position;
                    return guiState.DistanceToCircle(position, guiState.GetHandleSize(position) * 10f);
                },
                position = (i) => { return GetPoint(i).position; },
                forward = (i) => { return GetForward(); },
                up = (i) => { return GetUp(); },
                right = (i) => { return GetRight(); },
                onRepaint = DrawPoint
            };

            m_EdgeControl = new GenericControl("Edge")
            {
                onEndLayout = (guiState) => { controller.AddClosestPath(m_EdgeControl.layoutData.distance); },
                count = GetEdgeCount,
                distance = DistanceToEdge,
                position = (i) => { return GetPoint(i).position; },
                forward = (i) => { return GetForward(); },
                up = (i) => { return GetUp(); },
                right = (i) => { return GetRight(); },
                onRepaint = DrawEdge
            };

            m_LeftTangentControl = new GenericControl("LeftTangent")
            {
                count = () =>
                {
                    if (GetShapeType() != ShapeType.Spline)
                        return 0;

                    return GetPointCount();
                },
                distance = (guiState, i) =>
                {
                    if (!IsSelected(i) || IsOpenEnded() && i == 0)
                        return float.MaxValue;

                    var position = GetLeftTangent(i);
                    return guiState.DistanceToCircle(position, guiState.GetHandleSize(position) * 10f);
                },
                position = (i) => { return GetLeftTangent(i); },
                forward = (i) => { return GetForward(); },
                up = (i) => { return GetUp(); },
                right = (i) => { return GetRight(); },
                onRepaint = (guiState, control, i) =>
                {
                    if (!IsSelected(i) || IsOpenEnded() && i == 0)
                        return;

                    var position = GetPoint(i).position;
                    var leftTangent = GetLeftTangent(i);
                    
                    m_Drawer.DrawTangent(position, leftTangent);
                }
            };

            m_RightTangentControl = new GenericControl("RightTangent")
            {
                count = () =>
                {
                    if (GetShapeType() != ShapeType.Spline)
                        return 0;
                        
                    return GetPointCount();
                },
                distance = (guiState, i) =>
                {
                    if (!IsSelected(i) || IsOpenEnded() && i == GetPointCount()-1)
                        return float.MaxValue;

                    var position = GetRightTangent(i);
                    return guiState.DistanceToCircle(position, guiState.GetHandleSize(position) * 10f);
                },
                position = (i) => { return GetRightTangent(i); },
                forward = (i) => { return GetForward(); },
                up = (i) => { return GetUp(); },
                right = (i) => { return GetRight(); },
                onRepaint = (guiState, control, i) =>
                {
                    if (!IsSelected(i) || IsOpenEnded() && i == GetPointCount()-1)
                        return;
                    
                    var position = GetPoint(i).position;
                    var rightTangent = GetRightTangent(i);

                    m_Drawer.DrawTangent(position, rightTangent);
                }
            };

            m_CreatePointAction = new CreatePointAction(m_PointControl, m_EdgeControl)
            {
                enable = (guiState, action) => { return !guiState.isShiftDown && controller.closestEditablePath == controller.editablePath; },
                enableRepaint = EnableCreatePointRepaint,
                repaintOnMouseMove = (guiState, action) => { return true; },
                guiToWorld = GUIToWorld,
                onCreatePoint = (index, position) =>
                {
                    controller.RegisterUndo("Create Point");
                    controller.CreatePoint(index, position);
                },
                onPreRepaint = (guiState, action) =>
                {
                    if (GetPointCount() > 0)
                    {
                        var position = ClosestPointInEdge(guiState, guiState.mousePosition, m_EdgeControl.layoutData.index);
                        m_Drawer.DrawCreatePointPreview(position);
                    }
                }
            };

            Action<IGUIState> removePoints = (guiState) =>
            {
                controller.RegisterUndo("Remove Point");
                controller.RemoveSelectedPoints();
                guiState.changed = true;
            };

            m_RemovePointAction1 = new CommandAction(kDeleteCommandName)
            {
                enable = (guiState, action) => { return GetSelectedPointCount() > 0; },
                onCommand = removePoints
            };

            m_RemovePointAction2 = new CommandAction(kSoftDeleteCommandName)
            {
                enable = (guiState, action) => { return GetSelectedPointCount() > 0; },
                onCommand = removePoints
            };

            m_MovePointAction = new SliderAction(m_PointControl)
            {
                onClick = (guiState, control) =>
                {
                    var index = control.layoutData.index;

                    if (!guiState.isActionKeyDown && !IsSelected(index))
                        controller.ClearSelection();
                    
                    controller.SelectPoint(index, true);
                    guiState.changed = true;
                },
                onSliderBegin = (guiState, control, position) =>
                {
                    controller.RegisterUndo("Move Point");
                },
                onSliderChanged = (guiState, control, position) =>
                {
                    var index = control.hotLayoutData.index;
                    var delta = SnapIfNeeded(position) - GetPoint(index).position;

                    controller.MoveSelectedPoints(delta);
                }
            };

            m_MoveEdgeAction = new SliderAction(m_EdgeControl)
            {
                enable = (guiState, action) => { return guiState.isShiftDown; },
                onSliderBegin = (guiState, control, position) =>
                {
                    controller.RegisterUndo("Move Edge");
                },
                onSliderChanged = (guiState, control, position) =>
                {
                    var index = control.hotLayoutData.index;
                    var delta = position -  GetPoint(index).position;
                    
                    controller.MoveEdge(index, delta);
                }
            };

            var cachedRightTangent = Vector3.zero;
            var cachedLeftTangent = Vector3.zero;
            
            m_MoveLeftTangentAction = new SliderAction(m_LeftTangentControl)
            {
                onSliderBegin = (guiState, control, position) =>
                {
                    controller.RegisterUndo("Move Tangent");
                    cachedRightTangent = GetPoint(control.hotLayoutData.index).rightTangent;
                },
                onSliderChanged = (guiState, control, position) =>
                {
                    var index = control.hotLayoutData.index;
                    var setToLinear = guiState.nearestControl == m_PointControl.ID && m_PointControl.layoutData.index == index;

                    controller.SetLeftTangent(index, position, setToLinear, guiState.isShiftDown, cachedRightTangent);
                    
                },
                onSliderEnd = (guiState, control, position) =>
                {
                    controller.editablePath.UpdateTangentMode(control.hotLayoutData.index);
                    guiState.changed = true;
                }
            };

            m_MoveRightTangentAction = new SliderAction(m_RightTangentControl)
            {
                onSliderBegin = (guiState, control, position) =>
                {
                    controller.RegisterUndo("Move Tangent");
                    cachedLeftTangent = GetPoint(control.hotLayoutData.index).leftTangent;
                },
                onSliderChanged = (guiState, control, position) =>
                {
                    var index = control.hotLayoutData.index;
                    var setToLinear = guiState.nearestControl == m_PointControl.ID && m_PointControl.layoutData.index == index;

                    controller.SetRightTangent(index, position, setToLinear, guiState.isShiftDown, cachedLeftTangent);
                },
                onSliderEnd = (guiState, control, position) =>
                {
                    controller.editablePath.UpdateTangentMode(control.hotLayoutData.index);
                    guiState.changed = true;
                }
            };
        }

        public void Install(GUISystem guiSystem)
        {
            guiSystem.AddControl(m_EdgeControl);
            guiSystem.AddControl(m_PointControl);
            guiSystem.AddControl(m_LeftTangentControl);
            guiSystem.AddControl(m_RightTangentControl);
            guiSystem.AddAction(m_CreatePointAction);
            guiSystem.AddAction(m_RemovePointAction1);
            guiSystem.AddAction(m_RemovePointAction2);
            guiSystem.AddAction(m_MovePointAction);
            guiSystem.AddAction(m_MoveEdgeAction);
            guiSystem.AddAction(m_MoveLeftTangentAction);
            guiSystem.AddAction(m_MoveRightTangentAction);
        }

        public void Uninstall(GUISystem guiSystem)
        {
            guiSystem.RemoveControl(m_EdgeControl);
            guiSystem.RemoveControl(m_PointControl);
            guiSystem.RemoveControl(m_LeftTangentControl);
            guiSystem.RemoveControl(m_RightTangentControl);
            guiSystem.RemoveAction(m_CreatePointAction);
            guiSystem.RemoveAction(m_RemovePointAction1);
            guiSystem.RemoveAction(m_RemovePointAction2);
            guiSystem.RemoveAction(m_MovePointAction);
            guiSystem.RemoveAction(m_MoveEdgeAction);
            guiSystem.RemoveAction(m_MoveLeftTangentAction);
            guiSystem.RemoveAction(m_MoveRightTangentAction);
        }

        private ControlPoint GetPoint(int index)
        {
            return controller.editablePath.GetPoint(index);
        }

        private int GetPointCount()
        {
            return controller.editablePath.pointCount;
        }

        private int GetEdgeCount()
        {
            if (controller.editablePath.isOpenEnded)
                return controller.editablePath.pointCount - 1;

            return controller.editablePath.pointCount;
        }

        private int GetSelectedPointCount()
        {
            return controller.editablePath.selection.Count;
        }

        private bool IsSelected(int index)
        {
            return controller.editablePath.selection.Contains(index);
        }

        private Vector3 GetForward()
        {
            return controller.editablePath.forward;
        }

        private Vector3 GetUp()
        {
            return controller.editablePath.up;
        }

        private Vector3 GetRight()
        {
            return controller.editablePath.right;
        }

        private Matrix4x4 GetLocalToWorldMatrix()
        {
            return controller.editablePath.localToWorldMatrix;
        }

        private ShapeType GetShapeType()
        {
            return controller.editablePath.shapeType;
        }

        private bool IsOpenEnded()
        {
            return controller.editablePath.isOpenEnded;
        }

        private Vector3 GetLeftTangent(int index)
        {
            return controller.editablePath.CalculateLeftTangent(index);
        }

        private Vector3 GetRightTangent(int index)
        {
            return controller.editablePath.CalculateRightTangent(index);
        }

        private int NextIndex(int index)
        {
            return EditablePathUtility.Mod(index + 1, GetPointCount());
        }

        private ControlPoint NextControlPoint(int index)
        {
            return GetPoint(NextIndex(index));
        }

        private int PrevIndex(int index)
        {
            return EditablePathUtility.Mod(index - 1, GetPointCount());
        }

        private ControlPoint PrevControlPoint(int index)
        {
            return GetPoint(PrevIndex(index));
        }

        private Vector3 ClosestPointInEdge(IGUIState guiState, Vector2 mousePosition, int index)
        {
            if (GetShapeType() == ShapeType.Polygon)
            {
                var p0 = GetPoint(index).position;
                var p1 = NextControlPoint(index).position;
                var mouseWorldPosition = GUIToWorld(guiState, mousePosition);

                var dir1 = (mouseWorldPosition - p0);
                var dir2 = (p1 - p0);
                
                return Mathf.Clamp01(Vector3.Dot(dir1, dir2.normalized) / dir2.magnitude) * dir2 + p0;
            }
            else if (GetShapeType() == ShapeType.Spline)
            {
                var nextIndex = NextIndex(index);
                float t;
                return BezierUtility.ClosestPointOnCurve(
                    GUIToWorld(guiState, mousePosition),
                    GetPoint(index).position,
                    GetPoint(nextIndex).position,
                    GetRightTangent(index),
                    GetLeftTangent(nextIndex),
                    out t);
            }

            return Vector3.zero;
        }

        private float DistanceToEdge(IGUIState guiState, int index)
        {
            if (GetShapeType() == ShapeType.Polygon)
            {
                return guiState.DistanceToSegment(GetPoint(index).position, NextControlPoint(index).position);
            }
            else if (GetShapeType() == ShapeType.Spline)
            {
                var closestPoint = ClosestPointInEdge(guiState, guiState.mousePosition, index);
                var closestPoint2 = HandleUtility.WorldToGUIPoint(closestPoint);

                return (closestPoint2 - guiState.mousePosition).magnitude;
            }

            return float.MaxValue;
        }

        private Vector3 GUIToWorld(IGUIState guiState, Vector2 position)
        {
            return guiState.GUIToWorld(position, GetForward(), GetLocalToWorldMatrix().MultiplyPoint3x4(Vector3.zero));
        }

        private void DrawPoint(IGUIState guiState, Control control, int index)
        {
            var position = GetPoint(index).position;

            if (guiState.hotControl == control.actionID && control.hotLayoutData.index == index || IsSelected(index))
                m_Drawer.DrawPointSelected(position);
            else if (guiState.hotControl == 0 && guiState.nearestControl == control.ID && control.layoutData.index == index)
                m_Drawer.DrawPointHovered(position);
            else
                m_Drawer.DrawPoint(position);
        }

        private void DrawEdge(IGUIState guiState, Control control, int index)
        {
            if (GetShapeType() == ShapeType.Polygon)
            {
                var nextIndex = NextIndex(index);
                var color = Color.white;

                if(guiState.nearestControl == control.ID && control.layoutData.index == index && guiState.hotControl == 0)
                    color = Color.yellow;
                
                m_Drawer.DrawLine(GetPoint(index).position, GetPoint(nextIndex).position, 5f, color);
            }
            else if (GetShapeType() == ShapeType.Spline)
            {
                var nextIndex = NextIndex(index);
                var color = Color.white;

                if(guiState.nearestControl == control.ID && control.layoutData.index == index && guiState.hotControl == 0)
                    color = Color.yellow;
                
                m_Drawer.DrawBezier(
                    GetPoint(index).position,
                    GetRightTangent(index),
                    GetLeftTangent(nextIndex),
                    GetPoint(nextIndex).position,
                    5f,
                    color);
            }
        }

        private bool EnableCreatePointRepaint(IGUIState guiState, GUIAction action)
        {
            return guiState.nearestControl != m_PointControl.ID &&
                    guiState.hotControl == 0  &&
                    (guiState.nearestControl != m_LeftTangentControl.ID) &&
                    (guiState.nearestControl != m_RightTangentControl.ID);
        }

        private Vector3 SnapIfNeeded(Vector3 position)
        {
            if (!controller.enableSnapping || controller.snapping == null)
                return position;
            
            var guiPosition = HandleUtility.WorldToGUIPoint(position);
            var snappedGuiPosition = HandleUtility.WorldToGUIPoint(controller.snapping.Snap(position));
            var sqrDistance = (guiPosition - snappedGuiPosition).sqrMagnitude;

            if (sqrDistance < kSnappingDistance * kSnappingDistance)
                position = controller.snapping.Snap(position);
            
            return position;
        }
    }
}
