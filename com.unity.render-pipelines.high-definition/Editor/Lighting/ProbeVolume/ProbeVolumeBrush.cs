using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    public class ProbeVolumeBrush
    {
        const double k_EditorTargetFramerateHigh = .03;

        double m_LastBrushUpdate = 0.0;
        bool m_ApplyingBrush = false;

        bool m_HasHit = false;
        Vector3 m_WorldPosition;

        public void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;

            if (SceneViewInUse(e))
            {
                // Force exit the current brush if user's mouse left
                // the SceneView while a brush was still in use.
                if (m_ApplyingBrush)
                    OnFinishApplyingBrush();
                return;
            }

            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            HandleUtility.AddDefaultControl(controlID);

            switch( e.GetTypeForControl(controlID) )
            {
                case EventType.MouseMove:
                    // Handles:
                    //		OnBrushEnter
                    //		OnBrushExit
                    //		OnBrushMove
                    if( EditorApplication.timeSinceStartup - m_LastBrushUpdate > GetTargetFramerate() )
                    {
                        m_LastBrushUpdate = EditorApplication.timeSinceStartup;
                        UpdateBrush(e.mousePosition, Event.current.control, Event.current.shift && Event.current.type != EventType.ScrollWheel);
                    }
                    break;

                case EventType.MouseDown:
                case EventType.MouseDrag:
                    // Handles:
                    //		OnBrushBeginApply
                    //		OnBrushApply
                    //		OnBrushFinishApply
                    if( EditorApplication.timeSinceStartup - m_LastBrushUpdate > GetTargetFramerate() )
                    {
                        m_LastBrushUpdate = EditorApplication.timeSinceStartup;
                        UpdateBrush(e.mousePosition, Event.current.control, Event.current.shift && Event.current.type != EventType.ScrollWheel);
                        ApplyBrush(Event.current.control, Event.current.shift && Event.current.type != EventType.ScrollWheel);
                    }
                    break;

                case EventType.MouseUp:
                    if(m_ApplyingBrush)
                    {
                        OnFinishApplyingBrush();
                        UpdateBrush(e.mousePosition, Event.current.control, Event.current.shift && Event.current.type != EventType.ScrollWheel);
                    }
                    break;
            }

            DrawGizmo(sceneView);
        }

        /// <summary>
        /// Returns true if the event is one that should consume the mouse or keyboard.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        static bool SceneViewInUse(Event e)
        {
            return 	e.alt
                    || Tools.current == Tool.View
                    || GUIUtility.hotControl > 0
                    || (e.isMouse ? e.button > 1 : false)
                    || Tools.viewTool == ViewTool.FPS
                    || Tools.viewTool == ViewTool.Orbit;
        }

        private void OnFinishApplyingBrush()
        {
            Debug.Log("Finish");
            // PolySceneUtility.PopGIWorkflowMode();
            // firstGameObject = null;
            m_ApplyingBrush = false;
            // mode.OnBrushFinishApply(brushTarget, brushSettings);
            // FinalizeAndResetHovering();
            // m_IgnoreDrag.Clear();
        }

        /// <summary>
        /// Get framerate according to the brush target
        /// </summary>
        /// <param name="target">The brush target</param>
        /// <returns>framerate</returns>
        static double GetTargetFramerate()
        {
            // if (Util.IsValid(target) && target.vertexCount > 24000)
            //     return k_EditorTargetFrameLow;

            return k_EditorTargetFramerateHigh;
        }

        /// <summary>
        /// Update the current brush object and weights with the current mouse position.
        /// </summary>
        /// <param name="mousePosition">current mouse position (from Event)</param>
        /// <param name="isDrag">optional, is dragging the mouse cursor</param>
        /// <param name="overridenGO">optional, provides an already selected gameobject (used in unit tests only)</param>
        /// <param name="overridenRay"> optional, provides a ray already created (used in unit tests only)</param>
        internal void UpdateBrush(Vector2 mousePosition, bool isUserHoldingControl = false, bool isUserHoldingShift = false, bool isDrag = false, GameObject overridenGO = null, Ray? overridenRay = null)
        {
            Ray mouseRay = overridenRay != null ? (Ray)overridenRay :  HandleUtility.GUIPointToWorldRay(mousePosition);
            // if the mouse hover picked up a valid editable, raycast against that.  otherwise
            // raycast all meshes in selection
            DoMeshRaycast(mouseRay);

            // OnBrushMove();

            SceneView.RepaintAll();
        }

        /// <summary>
        /// Calculate the weights for this ray.
        /// </summary>
        /// <param name="mouseRay">The ray used to calculate weights</param>
        /// <param name="target">The object on which to calculate the weights</param>
        /// <returns>true if mouseRay hits the target, false otherwise</returns>
        void DoMeshRaycast(Ray mouseRay)
        {
            m_HasHit = Physics.Raycast(mouseRay, out var hit, float.MaxValue, ~0, QueryTriggerInteraction.Ignore);
            if (m_HasHit)
                m_WorldPosition = hit.point;
        }

        /// <summary>
        /// Apply brush to current brush target
        /// </summary>
        /// <param name="isUserHoldingControl"></param>
        /// <param name="isUserHoldingShift"></param>
        internal void ApplyBrush(bool isUserHoldingControl, bool isUserHoldingShift)
        {
            if (m_HasHit)
            {
                m_ApplyingBrush = true;
                Debug.Log("Painted!");
            }
        }

        void DrawGizmo(SceneView sceneView)
        {
            if (m_HasHit)
                Handles.DrawWireDisc(m_WorldPosition, (sceneView.camera.transform.position - m_WorldPosition).normalized, 1f);
        }

        internal void Cancel()
        {
            if (m_ApplyingBrush)
                OnFinishApplyingBrush();
        }
    }
}
