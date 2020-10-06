#if ENABLE_INPUT_SYSTEM && VFX_HAS_INPUT_SYSTEM_PACKAGE
    #define USE_INPUT_SYSTEM
    using UnityEngine.InputSystem;
#endif

using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/Input Mouse Binder")]
    [VFXBinder("Input/Mouse")]
    class VFXInputMouseBinder : VFXBinderBase
    {
        public string MouseLeftClickProperty { get { return (string)m_MouseLeftClickProperty; } set { m_MouseLeftClickProperty = value; } }
        public string MouseRightClickProperty { get { return (string)m_MouseRightClickProperty; } set { m_MouseRightClickProperty = value; } }

        [VFXPropertyBinding("System.Boolean"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_MouseLeftClickParameter")]
        protected ExposedProperty m_MouseLeftClickProperty = "LeftClick";

        [VFXPropertyBinding("System.Boolean"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_MouseRightClickParameter")]
        protected ExposedProperty m_MouseRightClickProperty = "RightClick";

        public string PositionProperty { get { return (string)m_PositionProperty; } set { m_PositionProperty = value; } }

        [VFXPropertyBinding("UnityEditor.VFX.Position", "UnityEngine.Vector3"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_PositionParameter")]
        protected ExposedProperty m_PositionProperty = "Position";

        public string VelocityProperty { get { return (string)m_VelocityProperty; } set { m_VelocityProperty = value; } }

        [VFXPropertyBinding("UnityEngine.Vector3"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_VelocityParameter")]
        protected ExposedProperty m_VelocityProperty = "Velocity";

        public Camera Target = null;
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
            return component.HasVector3(m_PositionProperty) &&
                (CheckLeftClick ? component.HasBool(m_MouseLeftClickProperty) : true) &&
                (CheckRightClick ? component.HasBool(m_MouseRightClickProperty) : true) &&
                (SetVelocity ? component.HasVector3(m_VelocityProperty) : true);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            Vector3 position = Vector3.zero;

            if (CheckLeftClick)
                component.SetBool(MouseLeftClickProperty, IsLeftClickPressed());
            if (CheckRightClick)
                component.SetBool(MouseRightClickProperty, IsRightClickPressed());

            if (Target != null)
            {
#if VFX_USE_PHYSICS
                if (UseRaycast) // Raycast version
                {
                    RaycastHit info;
                    Ray r = Target.ScreenPointToRay(GetMousePosition());
                    if (Physics.Raycast(r, out info, Distance))
                    {
                        position = info.point;
                    }
                    else // if not hit, consider not touched
                    {
                        Vector3 pos = GetMousePosition();
                        pos.z = Distance;
                        position = Target.ScreenToWorldPoint(pos);
                    }
                }
                else // Simple version
#endif
                {
                    Vector3 pos = GetMousePosition();
                    pos.z = Distance;
                    position = Target.ScreenToWorldPoint(pos);
                }
            }
            else
            {
                position = GetMousePosition();
            }

            component.SetVector3(m_PositionProperty, position);

            if (SetVelocity)
            {
                component.SetVector3(m_VelocityProperty, (position - m_PreviousPosition) / Time.deltaTime);
            }

            m_PreviousPosition = position;
        }

        bool IsRightClickPressed()
        {
#if USE_INPUT_SYSTEM
            return (Mouse.current != null) ? Mouse.current.rightButton.isPressed : false;
#else
            return Input.GetMouseButton(1);
#endif
        }

        bool IsLeftClickPressed()
        {
#if USE_INPUT_SYSTEM
            return (Mouse.current != null) ? Mouse.current.leftButton.isPressed : false;
#else
            return Input.GetMouseButton(0);
#endif
        }

        Vector2 GetMousePosition()
        {
#if USE_INPUT_SYSTEM
            return Pointer.current.position.ReadValue();
#else
            return Input.mousePosition;
#endif
        }

        public override string ToString()
        {
            return string.Format("Mouse: '{0}' -> {1}", m_PositionProperty, Target == null ? "(null)" : Target.name);
        }
    }
}
