using System.Collections.Generic;
using UnityEngine;
using GUIFramework;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering.LWRP
{
    internal abstract class PolygonEditor
    {
        private Dictionary<UnityObject, GUISystem> m_GUISystems = new Dictionary<UnityObject, GUISystem>();
        private Dictionary<UnityObject, SerializedObject> m_SerializedObjects = new Dictionary<UnityObject, SerializedObject>();
        private Drawer m_Drawer = new Drawer();

        private GUISystem GetGUISystem(UnityObject target)
        {
            GUISystem guiSystem;

            m_GUISystems.TryGetValue(target, out guiSystem);

            if (guiSystem == null)
            {
                guiSystem = CreateSystem(target);
                m_GUISystems[target] = guiSystem;
            }

            return guiSystem;
        }

        private SerializedObject GetSerializedObject(UnityObject target)
        {
            SerializedObject serializedObject;

            m_SerializedObjects.TryGetValue(target, out serializedObject);

            if (serializedObject == null)
            {
                serializedObject = new SerializedObject(target);
                m_SerializedObjects[target] = serializedObject;
            }

            return serializedObject;
        }

        private GUISystem CreateSystem(UnityObject target)
        {
            var guiSystem = new GUISystem(new GUIState());

            GUIAction removePointAction = null;

            var pointControl = new GenericControl("Point")
            {
                count = () =>
                {
                    return GetPointCount(target);
                },
                distance = (guiState, i) =>
                {
                    var position = GetPointWorld(target, i);
                    return ShapeEditorUtility.DistanceToCircle(position, ShapeEditorUtility.GetHandleSize(position) * 10f);
                },
                position = (i) =>
                {
                    return GetPointWorld(target, i);
                },
                forward = (i) =>
                {
                    return GetForward(target);
                },
                up = (i) =>
                {
                    return GetUp(target);
                },
                right = (i) =>
                {
                    return GetRight(target);
                },
                onRepaint = (IGUIState guiState, Control control, int index) =>
                {
                    var position = GetPointWorld(target, index);

                    if (guiState.hotControl == control.actionID && control.hotLayoutData.index == index)
                        m_Drawer.DrawPointSelected(position);
                    else if (guiState.hotControl == 0 && guiState.nearestControl == control.ID && control.layoutData.index == index)
                    {
                        if (removePointAction.IsEnabled(guiState))
                            m_Drawer.DrawRemovePointPreview(position);
                        else
                            m_Drawer.DrawPointHovered(position);
                    }
                    else
                        m_Drawer.DrawPoint(position);
                }
            };

            var edgeControl = new GenericControl("Edge")
            {
                count = () =>
                {
                    return GetPointCount(target);
                },
                distance = (IGUIState guiState, int index) =>
                {
                    return ShapeEditorUtility.DistanceToSegment(GetPointWorld(target, index), NextControlPoint(target, index));
                },
                position = (i) =>
                {
                    return GetPointWorld(target, i);
                },
                forward = (i) =>
                {
                    return GetForward(target);
                },
                up = (i) =>
                {
                    return GetUp(target);
                },
                right = (i) =>
                {
                    return GetRight(target);
                },
                onRepaint = (IGUIState guiState, Control control, int index) =>
                {
                    var nextIndex = NextIndex(target, index);
                    var prevIndex = PrevIndex(target, index);

                    var isEndpointHovered = 
                        guiState.hotControl == 0 &&
                        guiState.nearestControl == pointControl.ID &&
                        (index == pointControl.layoutData.index || nextIndex == pointControl.layoutData.index);

                    var isPointHovered = 
                        guiState.hotControl == 0 &&
                        guiState.nearestControl == pointControl.ID &&
                        index == pointControl.layoutData.index;

                    var color = Color.white;

                    if(guiState.hotControl == 0 && guiState.nearestControl == control.ID && control.layoutData.index == index)
                        color = Color.yellow;
                    else if (removePointAction.IsEnabled(guiState) && isEndpointHovered)
                    {
                        if (isPointHovered)
                            m_Drawer.DrawDottedLine(GetPointWorld(target, prevIndex), GetPointWorld(target, nextIndex), 5f, color);

                        color = Color.red;
                    }
                    
                    m_Drawer.DrawLine(GetPointWorld(target, index), GetPointWorld(target, nextIndex), 5f, color);
                }
            };

            var createPointAction = new CreatePointAction(pointControl, edgeControl)
            {
                enable = (guiState, action) =>
                {
                    return guiState.isShiftDown;
                },
                enableRepaint = (IGUIState guiState, GUIAction action) =>
                {
                    return guiState.nearestControl != pointControl.ID && guiState.hotControl == 0;
                },
                repaintOnMouseMove = (guiState, action) =>
                {
                    return true;
                },
                guiToWorld = (mousePosition) =>
                {
                    return GUIToWorld(target, mousePosition);
                },
                onCreatePoint = (int index, Vector3 position) =>
                {
                    InsertPointWorld(target, index + 1, position);
                },
                onPreRepaint = (guiState, action) =>
                {
                    var position = ClosestPointInEdge(target, guiState.mousePosition, edgeControl.layoutData.index);

                    m_Drawer.DrawCreatePointPreview(position);
                }
            };

            removePointAction = new ClickAction(pointControl, 0)
            {
                enable = (guiState, action) =>
                {
                    return guiState.isActionKeyDown;
                },
                onClick = (GUIState, control) =>
                {
                    if (GetPointCount(target) > 3)
                        RemovePoint(target, control.layoutData.index);
                }
            };

            var movePointAction = new SliderAction(pointControl)
            {
                onSliderChanged = (guiState, control, position) =>
                {
                    var index = control.hotLayoutData.index;
                    var pointPosition = GetPointWorld(target, index);
                    pointPosition = position;
                    SetPointWorld(target, index, pointPosition);
                }
            };

            var moveEdgeAction = new SliderAction(edgeControl)
            {
                onSliderChanged = (guiState, control, position) =>
                {
                    var index = control.hotLayoutData.index;
                    var pointPosition = GetPointWorld(target, index);
                    var delta = position -  pointPosition;
                    pointPosition += delta;
                    SetPointWorld(target, index, pointPosition);
                    pointPosition = NextControlPoint(target, index);
                    pointPosition += delta;
                    SetPointWorld(target, NextIndex(target, index), pointPosition);
                }
            };

            guiSystem.AddControl(edgeControl);
            guiSystem.AddControl(pointControl);
            guiSystem.AddAction(createPointAction);
            guiSystem.AddAction(removePointAction);
            guiSystem.AddAction(movePointAction);
            guiSystem.AddAction(moveEdgeAction);

            return guiSystem;
        }

        private Vector3 ClosestPointInEdge(UnityObject target, Vector2 mousePosition, int index)
        {
            var p0 = GetPointWorld(target, index);
            var p1 = NextControlPoint(target, index);
            var mouseWorldPosition = GUIToWorld(target, mousePosition);

            var dir1 = (mouseWorldPosition - p0);
            var dir2 = (p1 - p0);
            
            return Mathf.Clamp01(Vector3.Dot(dir1, dir2.normalized) / dir2.magnitude) * dir2 + p0;
        }

        private int NextIndex(UnityObject target, int index)
        {
            return ShapeEditorUtility.Mod(index + 1, GetPointCount(target));
        }

        private Vector3 NextControlPoint(UnityObject target, int index)
        {
            return GetPointWorld(target, NextIndex(target, index));
        }

        private int PrevIndex(UnityObject target, int index)
        {
            return ShapeEditorUtility.Mod(index - 1, GetPointCount(target));
        }

        private Vector3 PrevControlPoint(UnityObject target, int index)
        {
            return GetPoint(target, PrevIndex(target, index));
        }

        public void OnGUI(UnityObject target)
        {
            GetSerializedObject(target).UpdateIfRequiredOrScript();
            GetGUISystem(target).OnGUI();
        }

        private Vector3 GetPointWorld(UnityObject target, int index)
        {
            return LocalToWorld(target, GetPoint(target, index));
        }

        private void SetPointWorld(UnityObject target, int index, Vector3 position)
        {
            SetPoint(target, index, WorldToLocal(target, position));
        }

        private void InsertPointWorld(UnityObject target, int index, Vector3 position)
        {
            InsertPoint(target, index, WorldToLocal(target, position));
        }

        private Vector3 GUIToWorld(UnityObject target, Vector2 position)
        {
            return ShapeEditorUtility.GUIToWorld(
                position,
                GetForward(target),
                GetLocalToWorldMatrix(target).MultiplyPoint3x4(Vector3.zero));
        }

        private Vector3 LocalToWorld(UnityObject target, Vector3 position)
        {
            return GetLocalToWorldMatrix(target).MultiplyPoint3x4(position);
        }

        private Vector3 WorldToLocal(UnityObject target, Vector3 position)
        {
            return GetWorldToLocalMatrix(target).MultiplyPoint3x4(position);
        }

        private Vector3 GetForward(UnityObject target)
        {
            var component = target as Component;
            return component.transform.forward;
        }

        private Vector3 GetUp(UnityObject target)
        {
            var component = target as Component;
            return component.transform.up;
        }

        private Vector3 GetRight(UnityObject target)
        {
            var component = target as Component;
            return component.transform.right;
        }

        private Matrix4x4 GetLocalToWorldMatrix(UnityObject target)
        {
            var component = target as Component;
            return component.transform.localToWorldMatrix;
        }

        private Matrix4x4 GetWorldToLocalMatrix(UnityObject target)
        {
            var component = target as Component;
            return component.transform.worldToLocalMatrix;
        }

        private bool IsOpenEnded()
        {
            return false;
        }

        private int GetPointCount(UnityObject target)
        {
            return GetPointCount(GetSerializedObject(target));
        }

        private Vector3 GetPoint(UnityObject target, int index)
        {
            return GetPoint(GetSerializedObject(target), index);
        }

        private void SetPoint(UnityObject target, int index, Vector3 position)
        {
            SetPoint(GetSerializedObject(target), index, position);
        }

        private void InsertPoint(UnityObject target, int index, Vector3 position)
        {
            InsertPoint(GetSerializedObject(target), index, position);
        }

        private void RemovePoint(UnityObject target, int index)
        {
            RemovePoint(GetSerializedObject(target), index);
        }

        protected abstract int GetPointCount(SerializedObject serializedObject);
        protected abstract Vector3 GetPoint(SerializedObject serializedObject, int index);
        protected abstract void SetPoint(SerializedObject serializedObject, int index, Vector3 position);
        protected abstract void InsertPoint(SerializedObject serializedObject, int index, Vector3 position);
        protected abstract void RemovePoint(SerializedObject serializedObject, int index);
    }
}
