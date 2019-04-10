using UnityEngine.Experimental.VFX;

namespace UnityEngine.Experimental.VFX.Utility
{
    [AddComponentMenu("VFX/Utilities/Parameters/VFX Input Mouse Binder")]
    [VFXBinder("Input/Mouse")]
    public class VFXInputMouseBinder : VFXBinderBase
    {
        public string MouseLeftClickParameter { get { return (string)m_MouseLeftClickParameter; } set { m_MouseLeftClickParameter = value; } }
        public string MouseRightClickParameter { get { return (string)m_MouseRightClickParameter; } set { m_MouseRightClickParameter = value; } }

        [VFXParameterBinding("System.Boolean"), SerializeField]
        protected ExposedParameter m_MouseLeftClickParameter = "LeftClick";

        [VFXParameterBinding("System.Boolean"), SerializeField]
        protected ExposedParameter m_MouseRightClickParameter = "RightClick";

        public string PositionParameter { get { return (string)m_PositionParameter; } set { m_PositionParameter = value; } }

        [VFXParameterBinding("UnityEditor.VFX.Position", "UnityEngine.Vector3"), SerializeField]
        protected ExposedParameter m_PositionParameter = "Position";

        public string VelocityParameter { get { return (string)m_VelocityParameter; } set { m_VelocityParameter = value; } }

        [VFXParameterBinding("UnityEngine.Vector3"), SerializeField]
        protected ExposedParameter m_VelocityParameter = "Velocity";

        public Camera Target;
        public float Distance = 10.0f;
#if VFX_USE_PHYSICS
        public bool UseRaycast = false;
#endif
        public bool SetVelocity = false;
        public bool CheckLeftClick = true;
        public bool CheckRightClick = false;

        Vector3 m_PreviousPosition;

        public override bool IsValid(VisualEffect component)
        {
            return component.HasVector3(m_PositionParameter) &&
                (CheckLeftClick ? component.HasBool(m_MouseLeftClickParameter) : true) &&
                (CheckRightClick ? component.HasBool(m_MouseRightClickParameter) : true) &&
                (SetVelocity ? component.HasVector3(m_VelocityParameter) : true);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            Vector3 position = Vector3.zero;

            if (CheckLeftClick)
                component.SetBool(MouseLeftClickParameter, Input.GetMouseButton(0));
            if (CheckRightClick)
                component.SetBool(MouseRightClickParameter, Input.GetMouseButton(1));

            if (Target != null)
            {
#if VFX_USE_PHYSICS
                if (UseRaycast) // Raycast version
                {
                    RaycastHit info;
                    Ray r = Target.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(r, out info, Distance))
                    {
                        position = info.point;
                    }
                    else // if not hit, consider not touched
                    {
                        Vector3 pos = Input.mousePosition;
                        pos.z = Distance;
                        position = Target.ScreenToWorldPoint(pos);
                    }
                }
                else // Simple version
#endif
                {
                    Vector3 pos = Input.mousePosition;
                    pos.z = Distance;
                    position = Target.ScreenToWorldPoint(pos);
                }
            }
            else
            {
                position = Input.mousePosition;
            }

            component.SetVector3(m_PositionParameter, position);

            if (SetVelocity)
            {
                component.SetVector3(m_VelocityParameter, (position - m_PreviousPosition) / Time.deltaTime);
            }

            m_PreviousPosition = position;
        }

        public override string ToString()
        {
            return string.Format("Mouse: '{0}' -> {1}", m_PositionParameter, Target == null ? "(null)" : Target.name);
        }
    }
}
