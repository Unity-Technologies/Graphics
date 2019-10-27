using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/Input Touch Binder")]
    [VFXBinder("Input/Touch")]
    class VFXInputTouchBinder : VFXBinderBase
    {
        public string TouchEnabledProperty { get { return (string)m_TouchEnabledProperty; } set { m_TouchEnabledProperty = value; } }

        [VFXPropertyBinding("System.Boolean"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_TouchEnabledParameter")]
        protected ExposedProperty m_TouchEnabledProperty = "TouchEnabled";

        public string Parameter { get { return (string)m_Parameter; } set { m_Parameter = value; } }

        [VFXPropertyBinding("UnityEditor.VFX.Position", "UnityEngine.Vector3"), SerializeField]
        protected ExposedProperty m_Parameter = "Position";

        public string VelocityParameter { get { return (string)m_VelocityParameter; } set { m_VelocityParameter = value; } }

        [VFXPropertyBinding("UnityEngine.Vector3"), SerializeField]
        protected ExposedProperty m_VelocityParameter = "Velocity";


        public int TouchIndex = 0;
        public Camera Target = null;
        public float Distance = 10.0f;
#if VFX_USE_PHYSICS
        public bool UseRaycast = false;
#endif
        public bool SetVelocity = false;

        Vector3 m_PreviousPosition;
        bool m_PreviousTouch;

        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasVector3(m_Parameter) && component.HasBool(m_TouchEnabledProperty) && (SetVelocity ? component.HasVector3(m_VelocityParameter) : true);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            Vector3 position = Vector3.zero;
            bool touch = false;

            if (Input.touchCount > TouchIndex)
            {
                Touch t = Input.GetTouch(TouchIndex);
#if VFX_USE_PHYSICS
                if (UseRaycast) // Raycast version
                {
                    RaycastHit info;
                    Ray r = Target.ScreenPointToRay(t.position);
                    if (Physics.Raycast(r, out info, Distance))
                    {
                        touch = true;
                        position = info.point;
                    }
                    else // if not hit, consider not touched
                    {
                        component.SetBool(m_TouchEnabledParameter, false);
                        component.SetVector3(m_Parameter, Vector3.zero);
                        return;
                    }
                }
                else // Simple version
#endif
                {
                    touch = true;
                    Vector3 pos = t.position;
                    pos.z = Distance;
                    position = Target.ScreenToWorldPoint(pos);
                }

                component.SetBool(m_TouchEnabledProperty, true);
                component.SetVector3(m_Parameter, position);
            }
            else // Not touched at all
            {
                touch = false;
                component.SetBool(m_TouchEnabledProperty, false);
                component.SetVector3(m_Parameter, Vector3.zero);
            }

            if (SetVelocity)
            {
                if (m_PreviousTouch)
                    component.SetVector3(m_VelocityParameter, (position - m_PreviousPosition) / Time.deltaTime);
                else
                    component.SetVector3(m_VelocityParameter, Vector3.zero);
            }

            m_PreviousTouch = touch;
            m_PreviousPosition = position;
        }

        public override string ToString()
        {
            return string.Format("Touch #{2} : '{0}' -> {1}", m_Parameter, Target == null ? "(null)" : Target.name, TouchIndex);
        }
    }
}
