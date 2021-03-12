using System;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    public class MaskVolumeBrush
    {
        const float k_MaxTimeSpentPerEvent = 0.05f;
        const float k_Stepping = 0.5f;
        const int k_RaycastBufferSize = 128;

        public float Radius = 0.5f;

        public bool MeshCollidersOnly = true;
        public LayerMask PhysicsLayerMask = ~0;

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

            switch (e.GetTypeForControl(controlID))
            {
                case EventType.MouseMove:
                    StopIfApplying();
                    UpdateBrush(e.mousePosition);
                    break;

                case EventType.MouseDown:
                case EventType.MouseDrag:
                    UpdateBrush(e.mousePosition);
                    ApplyBrush();
                    break;

                case EventType.MouseUp:
                    StopIfApplying();
                    UpdateBrush(e.mousePosition);
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
        /// Update the brush state and position.
        /// </summary>
        /// <param name="mousePosition">current mouse position (from Event)</param>
        void UpdateBrush(Vector2 mousePosition)
        {
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(mousePosition);
            m_Hovering = Raycast(mouseRay, out var hitPosition);
            if (m_Hovering)
                m_Position = hitPosition;
        }

        RaycastHit[] raycastBuffer;
        bool Raycast(Ray ray, out Vector3 hitPosition)
        {
            if (MeshCollidersOnly)
            {
                if (raycastBuffer == null)
                    raycastBuffer = new RaycastHit[k_RaycastBufferSize];
                var hitCount = Physics.RaycastNonAlloc(ray, raycastBuffer, float.MaxValue, PhysicsLayerMask, QueryTriggerInteraction.Ignore);

                var minDistance = float.MaxValue;
                var nearestPosition = Vector3.zero;

                for (int i = 0; i < hitCount; i++)
                {
                    var hit = raycastBuffer[i];
                    if (hit.collider is MeshCollider && hit.distance < minDistance)
                    {
                        minDistance = hit.distance;
                        nearestPosition = hit.point;
                    }
                }

                if (minDistance != float.MaxValue)
                {
                    hitPosition = nearestPosition;
                    return true;
                }
            }
            else
            {
                if (Physics.Raycast(ray, out var hit, float.MaxValue, PhysicsLayerMask, QueryTriggerInteraction.Ignore))
                {
                    hitPosition = hit.point;
                    return true;
                }
            }

            hitPosition = default;
            return false;
        }

        /// <summary>
        /// Apply brush.
        /// </summary>
        void ApplyBrush()
        {
            if (m_Hovering)
            {
                if (!m_Applying)
                {
                    m_Applying = true;
                    OnApply?.Invoke(m_Position);
                    m_LastApplyPosition = m_Position;
                }
                else
                {
                    var moveDistance = Vector3.Distance(m_Position, m_LastApplyPosition);

                    // If mouse moved too far due to low framerate or high movement speed, fill the gap with more stamps
                    var maxStep = Radius * k_Stepping;
                    var steps = (int) (moveDistance / maxStep);

                    var maxTime = Time.realtimeSinceStartup + k_MaxTimeSpentPerEvent;
                    var startPosition = m_LastApplyPosition;
                    for (int i = 1; i <= steps; i++)
                    {
                        m_LastApplyPosition = Vector3.Lerp(startPosition, m_Position, (float) i / steps);
                        OnApply?.Invoke(m_LastApplyPosition);
                        if (Time.realtimeSinceStartup > maxTime)
                            break;
                    }
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
