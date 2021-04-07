using System;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    public class MaskVolumeBrush
    {
        const float k_MaxTimeSpentPerEvent = 0.05f;
        const float k_Stepping = 0.5f;
        const int k_RaycastBufferSize = 128;

        public float OuterRadius = 0.5f;
        public float InnerRadius = 1f;
        public float NormalBias = 0f;

        public bool MeshCollidersOnly = true;
        public LayerMask PhysicsLayerMask = ~0;

        bool m_Hovering = false;
        Vector3 m_Position;
        Vector3 m_Normal;
        
        bool m_Applying = false;
        Vector3 m_LastApplyPosition;
        Vector3 m_LastApplyNormal;
        float m_LastApplyPressure;

        public delegate void Apply(Vector3 position, Vector3 normal, float pressure, bool control);
        
        public event Apply OnApply;
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
                    ApplyBrush(e.pressure, e.control);
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
            m_Hovering = Raycast(mouseRay, out var hitPosition, out var hitNormal);
            if (m_Hovering)
            {
                m_Position = hitPosition;
                m_Normal = hitNormal;
            }
        }

        RaycastHit[] raycastBuffer;
        bool Raycast(Ray ray, out Vector3 hitPosition, out Vector3 hitNormal)
        {
            if (MeshCollidersOnly)
            {
                if (raycastBuffer == null)
                    raycastBuffer = new RaycastHit[k_RaycastBufferSize];
                var hitCount = Physics.RaycastNonAlloc(ray, raycastBuffer, float.MaxValue, PhysicsLayerMask, QueryTriggerInteraction.Ignore);

                var minDistance = float.MaxValue;
                var nearestHit = -1;

                for (int i = 0; i < hitCount; i++)
                {
                    var hit = raycastBuffer[i];
                    if (hit.collider is MeshCollider && hit.distance < minDistance)
                    {
                        minDistance = hit.distance;
                        nearestHit = i;
                    }
                }

                if (nearestHit != -1)
                {
                    var hit = raycastBuffer[nearestHit];
                    hitPosition = hit.point;
                    hitNormal = hit.normal;
                    return true;
                }
            }
            else
            {
                if (Physics.Raycast(ray, out var hit, float.MaxValue, PhysicsLayerMask, QueryTriggerInteraction.Ignore))
                {
                    hitPosition = hit.point;
                    hitNormal = hit.normal;
                    return true;
                }
            }

            hitPosition = default;
            hitNormal = default;
            return false;
        }

        /// <summary>
        /// Apply brush.
        /// </summary>
        void ApplyBrush(float pressure, bool control)
        {
            if (m_Hovering)
            {
                if (!m_Applying)
                {
                    m_Applying = true;
                    OnApply?.Invoke(m_Position, m_Normal, pressure, control);
                    m_LastApplyPosition = m_Position;
                    m_LastApplyNormal = m_Normal;
                    m_LastApplyPressure = pressure;
                }
                else
                {
                    var moveDistance = Vector3.Distance(m_Position, m_LastApplyPosition);

                    // If mouse moved too far due to low framerate or high movement speed, fill the gap with more stamps
                    var maxStep = OuterRadius * k_Stepping;
                    var steps = (int) (moveDistance / maxStep);

                    var maxTime = Time.realtimeSinceStartup + k_MaxTimeSpentPerEvent;
                    var startPosition = m_LastApplyPosition;
                    var startNormal = m_LastApplyNormal;
                    var startPressure = m_LastApplyPressure;
                    for (int i = 1; i <= steps; i++)
                    {
                        var time = (float) i / steps;
                        m_LastApplyPosition = Vector3.Lerp(startPosition, m_Position, time);
                        m_LastApplyNormal = Vector3.Lerp(startNormal, m_Normal, time);
                        m_LastApplyPressure = Mathf.Lerp(startPressure, pressure, time);
                        OnApply?.Invoke(m_LastApplyPosition, m_LastApplyNormal, m_LastApplyPressure, control);
                        if (Time.realtimeSinceStartup > maxTime)
                            break;
                    }
                }
            }
        }

        void DrawGizmo(SceneView sceneView)
        {
            if (m_Hovering)
            {
                var oldHandleColor = Handles.color;
                
                var discNormal = (sceneView.camera.transform.position - m_Position).normalized;

                if (InnerRadius != 1f)
                {
                    Handles.color = new Color(1f, 1f, 1f, 0.5f);
                    Handles.DrawWireDisc(m_Position, discNormal, OuterRadius);
                }

                Handles.color = Color.white;
                Handles.DrawWireDisc(m_Position, discNormal, OuterRadius * InnerRadius);

                if (NormalBias != 0f)
                {
                    Handles.color = new Color(Mathf.Abs(m_Normal.x), Mathf.Abs(m_Normal.y), Mathf.Abs(m_Normal.z));
                    Handles.DrawLine(m_Position, m_Position + m_Normal * NormalBias);
                }
                
                Handles.color = oldHandleColor;
            }
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
