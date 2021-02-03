using System;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    public class ProbeVolumeBrush
    {
        const double k_EditorTargetFramerateHigh = .03;

        public float Radius = 0.5f;

        double m_LastUpdate = 0.0;
        bool m_Hovering = false;
        Vector3 m_Position;
        bool m_Applying = false;
        Vector3 m_LastApplyPosition;

        public event Action<Vector3> OnApply;
        public event Action OnStopApplying;

        public void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;

            if (SceneViewInUse(e))
            {
                // Force exit the current brush if user's mouse left
                // the SceneView while a brush was still in use.
                StopIfApplying();
                return;
            }

            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            HandleUtility.AddDefaultControl(controlID);

            switch( e.GetTypeForControl(controlID) )
            {
                case EventType.MouseMove:
                    if (EditorApplication.timeSinceStartup - m_LastUpdate >= GetTargetFramerate())
                    {
                        m_LastUpdate = EditorApplication.timeSinceStartup;
                        UpdateBrush(e.mousePosition);
                    }
                    break;

                case EventType.MouseDown:
                case EventType.MouseDrag:
                    if (EditorApplication.timeSinceStartup - m_LastUpdate >= GetTargetFramerate())
                    {
                        m_LastUpdate = EditorApplication.timeSinceStartup;
                        UpdateBrush(e.mousePosition);
                        ApplyBrush();
                    }
                    break;

                case EventType.MouseUp:
                    if (m_Applying)
                    {
                        StopIfApplying();
                        UpdateBrush(e.mousePosition);
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

        /// <summary>
        /// Get framerate according to the brush target
        /// </summary>
        /// <returns>framerate</returns>
        static double GetTargetFramerate()
        {
            return k_EditorTargetFramerateHigh;
        }

        /// <summary>
        /// Update the current brush object and weights with the current mouse position.
        /// </summary>
        /// <param name="mousePosition">current mouse position (from Event)</param>
        void UpdateBrush(Vector2 mousePosition)
        {
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(mousePosition);
            OnBrushMove(mouseRay);
        }

        /// <summary>
        /// Calculate the weights for this ray.
        /// </summary>
        /// <param name="mouseRay">The ray used to calculate weights</param>
        /// <returns>true if mouseRay hits the target, false otherwise</returns>
        void OnBrushMove(Ray mouseRay)
        {
            m_Hovering = Physics.Raycast(mouseRay, out var hit, float.MaxValue, ~0, QueryTriggerInteraction.Ignore);
            if (m_Hovering)
                m_Position = hit.point;
        }

        /// <summary>
        /// Apply brush to current brush target
        /// </summary>
        void ApplyBrush()
        {
            if (m_Hovering)
            {
                bool needsApplying;
                if (!m_Applying)
                {
                    m_Applying = true;
                    needsApplying = true;
                }
                else
                {
                    var sqrMoveDistance = Vector3.SqrMagnitude(m_Position - m_LastApplyPosition);
                    var minStep = Radius * 0.25f;
                    needsApplying = sqrMoveDistance >= (minStep * minStep);
                }

                if (needsApplying)
                {
                    OnApply?.Invoke(m_Position);
                    m_LastApplyPosition = m_Position;
                }
            }
        }

        void DrawGizmo(SceneView sceneView)
        {
            if (m_Hovering)
                Handles.DrawWireDisc(m_Position, (sceneView.camera.transform.position - m_Position).normalized, Radius);
        }

        public void StopIfApplying()
        {
            if (m_Applying)
            {
                OnStopApplying?.Invoke();
                m_Applying = false;
            }
        }
    }
}
