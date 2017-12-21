using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering
{
    [ExecuteInEditMode]
    public class FreeCamera : MonoBehaviour
    {
        public float m_LookSpeedController = 120f;
        public float m_LookSpeedMouse = 10.0f;
        public float m_MoveSpeed = 10.0f;
        public float m_Turbo = 10.0f;

        private static string kMouseX = "Mouse X";
        private static string kMouseY = "Mouse Y";
        private static string kRightStickX = "Controller Right Stick X";
        private static string kRightStickY = "Controller Right Stick Y";
        private static string kVertical = "Vertical";
        private static string kHorizontal = "Horizontal";

        void OnEnable()
        {
            RegisterInputs();
        }

        void RegisterInputs()
        {
#if UNITY_EDITOR
            List <InputManagerEntry> inputEntries = new List<InputManagerEntry>();

            // Add new bindings
            inputEntries.Add(new InputManagerEntry { name = kRightStickX, kind = InputManagerEntry.Kind.Axis, axis = InputManagerEntry.Axis.Fourth, sensitivity = 1.0f, gravity = 1.0f, deadZone = 0.2f });
            inputEntries.Add(new InputManagerEntry { name = kRightStickY, kind = InputManagerEntry.Kind.Axis, axis = InputManagerEntry.Axis.Fifth, sensitivity = 1.0f, gravity = 1.0f, deadZone = 0.2f, invert = true });

            InputRegistering.RegisterInputs(inputEntries);
#endif
        }

        void Update()
        {
            // If the debug menu is running, we don't want to conflict with its inputs.
            if(DebugMenuManager.instance.menuUI.isEnabled)
                return;

            float inputRotateAxisX = 0.0f;
            float inputRotateAxisY = 0.0f;
            if (Input.GetMouseButton(1))
            {
                inputRotateAxisX = Input.GetAxis(kMouseX) * m_LookSpeedMouse;
                inputRotateAxisY = Input.GetAxis(kMouseY) * m_LookSpeedMouse;
            }
            inputRotateAxisX += (Input.GetAxis(kRightStickX) * m_LookSpeedController * Time.deltaTime);
            inputRotateAxisY += (Input.GetAxis(kRightStickY) * m_LookSpeedController * Time.deltaTime);

            float inputVertical = Input.GetAxis(kVertical);
            float inputHorizontal = Input.GetAxis(kHorizontal);

            bool moved = inputRotateAxisX != 0.0f || inputRotateAxisY != 0.0f || inputVertical != 0.0f || inputHorizontal != 0.0f;
            if (moved)
            {
                float rotationX = transform.localEulerAngles.x;
                float newRotationY = transform.localEulerAngles.y + inputRotateAxisX;

                // Weird clamping code due to weird Euler angle mapping...
                float newRotationX = (rotationX - inputRotateAxisY);
                if (rotationX <= 90.0f && newRotationX >= 0.0f)
                    newRotationX = Mathf.Clamp(newRotationX, 0.0f, 90.0f);
                if (rotationX >= 270.0f)
                    newRotationX = Mathf.Clamp(newRotationX, 270.0f, 360.0f);

                transform.localRotation = Quaternion.Euler(newRotationX, newRotationY, transform.localEulerAngles.z);

                float moveSpeed = Time.deltaTime * m_MoveSpeed;
                if (Input.GetMouseButton(1))
                    moveSpeed *= Input.GetKey(KeyCode.LeftShift) ? m_Turbo : 1.0f;
                else
                    moveSpeed *= Input.GetAxis("Fire1") > 0.0f ? m_Turbo : 1.0f;
                transform.position += transform.forward * moveSpeed * inputVertical;
                transform.position += transform.right * moveSpeed * inputHorizontal;
            }
        }
    }
}
