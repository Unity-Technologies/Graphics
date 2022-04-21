#if ENABLE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM_PACKAGE
    #define USE_INPUT_SYSTEM
    using UnityEngine.InputSystem;
#endif

using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Utility Free Camera component.
    /// </summary>
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "Free-Camera" + Documentation.endURL)]
    public class FreeCamera : MonoBehaviour
    {
        const float k_MouseSensitivityMultiplier = 0.01f;

        /// <summary>
        /// Rotation speed when using a controller.
        /// </summary>
        public float m_LookSpeedController = 120f;
        /// <summary>
        /// Rotation speed when using the mouse.
        /// </summary>
        public float m_LookSpeedMouse = 4.0f;
        /// <summary>
        /// Movement speed.
        /// </summary>
        public float m_MoveSpeed = 10.0f;
        /// <summary>
        /// Value added to the speed when incrementing.
        /// </summary>
        public float m_MoveSpeedIncrement = 2.5f;
        /// <summary>
        /// Scale factor of the turbo mode.
        /// </summary>
        public float m_Turbo = 10.0f;

#if !USE_INPUT_SYSTEM
        private static string kMouseX = "Mouse X";
        private static string kMouseY = "Mouse Y";
        private static string kRightStickX = "Controller Right Stick X";
        private static string kRightStickY = "Controller Right Stick Y";
        private static string kVertical = "Vertical";
        private static string kHorizontal = "Horizontal";

        private static string kYAxis = "YAxis";
        private static string kSpeedAxis = "Speed Axis";
#endif

#if USE_INPUT_SYSTEM
        InputAction lookAction;
        InputAction moveAction;
        InputAction speedAction;
        InputAction yMoveAction;
#endif

        void OnEnable()
        {
            RegisterInputs();
        }

        void RegisterInputs()
        {
#if USE_INPUT_SYSTEM
            var map = new InputActionMap("Free Camera");

            lookAction = map.AddAction("look", binding: "<Mouse>/delta");
            moveAction = map.AddAction("move", binding: "<Gamepad>/leftStick");
            speedAction = map.AddAction("speed", binding: "<Gamepad>/dpad");
            yMoveAction = map.AddAction("yMove");

            lookAction.AddBinding("<Gamepad>/rightStick").WithProcessor("scaleVector2(x=15, y=15)");
            moveAction.AddCompositeBinding("Dpad")
                .With("Up", "<Keyboard>/w")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/s")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/a")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/d")
                .With("Right", "<Keyboard>/rightArrow");
            speedAction.AddCompositeBinding("Dpad")
                .With("Up", "<Keyboard>/home")
                .With("Down", "<Keyboard>/end");
            yMoveAction.AddCompositeBinding("Dpad")
                .With("Up", "<Keyboard>/pageUp")
                .With("Down", "<Keyboard>/pageDown")
                .With("Up", "<Keyboard>/e")
                .With("Down", "<Keyboard>/q")
                .With("Up", "<Gamepad>/rightshoulder")
                .With("Down", "<Gamepad>/leftshoulder");

            moveAction.Enable();
            lookAction.Enable();
            speedAction.Enable();
            yMoveAction.Enable();
#endif

#if UNITY_EDITOR && !USE_INPUT_SYSTEM
            List<InputManagerEntry> inputEntries = new List<InputManagerEntry>();

            // Add new bindings
            inputEntries.Add(new InputManagerEntry { name = kRightStickX, kind = InputManagerEntry.Kind.Axis, axis = InputManagerEntry.Axis.Fourth, sensitivity = 1.0f, gravity = 1.0f, deadZone = 0.2f });
            inputEntries.Add(new InputManagerEntry { name = kRightStickY, kind = InputManagerEntry.Kind.Axis, axis = InputManagerEntry.Axis.Fifth, sensitivity = 1.0f, gravity = 1.0f, deadZone = 0.2f, invert = true });

            inputEntries.Add(new InputManagerEntry { name = kYAxis, kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "page up", altBtnPositive = "joystick button 5", btnNegative = "page down", altBtnNegative = "joystick button 4", gravity = 1000.0f, deadZone = 0.001f, sensitivity = 1000.0f });
            inputEntries.Add(new InputManagerEntry { name = kYAxis, kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "q", btnNegative = "e", gravity = 1000.0f, deadZone = 0.001f, sensitivity = 1000.0f });

            inputEntries.Add(new InputManagerEntry { name = kSpeedAxis, kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "home", btnNegative = "end", gravity = 1000.0f, deadZone = 0.001f, sensitivity = 1000.0f });
            inputEntries.Add(new InputManagerEntry { name = kSpeedAxis, kind = InputManagerEntry.Kind.Axis, axis = InputManagerEntry.Axis.Seventh, gravity = 1000.0f, deadZone = 0.001f, sensitivity = 1000.0f });

            InputRegistering.RegisterInputs(inputEntries);
#endif
        }

        float inputRotateAxisX, inputRotateAxisY;
        float inputChangeSpeed;
        float inputVertical, inputHorizontal, inputYAxis;
        bool leftShiftBoost, leftShift, fire1;

        void UpdateInputs()
        {
            inputRotateAxisX = 0.0f;
            inputRotateAxisY = 0.0f;
            leftShiftBoost = false;
            fire1 = false;

#if USE_INPUT_SYSTEM
            var lookDelta = lookAction.ReadValue<Vector2>();
            inputRotateAxisX = lookDelta.x * m_LookSpeedMouse * k_MouseSensitivityMultiplier;
            inputRotateAxisY = lookDelta.y * m_LookSpeedMouse * k_MouseSensitivityMultiplier;

            leftShift = Keyboard.current.leftShiftKey.isPressed;
            fire1 = Mouse.current?.leftButton?.isPressed == true || Gamepad.current?.xButton?.isPressed == true;

            inputChangeSpeed = speedAction.ReadValue<Vector2>().y;

            var moveDelta = moveAction.ReadValue<Vector2>();
            inputVertical = moveDelta.y;
            inputHorizontal = moveDelta.x;
            inputYAxis = yMoveAction.ReadValue<Vector2>().y;
#else
            if (Input.GetMouseButton(1))
            {
                leftShiftBoost = true;
                inputRotateAxisX = Input.GetAxis(kMouseX) * m_LookSpeedMouse;
                inputRotateAxisY = Input.GetAxis(kMouseY) * m_LookSpeedMouse;
            }
            inputRotateAxisX += (Input.GetAxis(kRightStickX) * m_LookSpeedController * k_MouseSensitivityMultiplier);
            inputRotateAxisY += (Input.GetAxis(kRightStickY) * m_LookSpeedController * k_MouseSensitivityMultiplier);

            leftShift = Input.GetKey(KeyCode.LeftShift);
            fire1 = Input.GetAxis("Fire1") > 0.0f;

            inputChangeSpeed = Input.GetAxis(kSpeedAxis);

            inputVertical = Input.GetAxis(kVertical);
            inputHorizontal = Input.GetAxis(kHorizontal);
            inputYAxis = Input.GetAxis(kYAxis);
#endif
        }

        void Update()
        {
            // If the debug menu is running, we don't want to conflict with its inputs.
            if (DebugManager.instance.displayRuntimeUI)
                return;

            UpdateInputs();

            if (inputChangeSpeed != 0.0f)
            {
                m_MoveSpeed += inputChangeSpeed * m_MoveSpeedIncrement;
                if (m_MoveSpeed < m_MoveSpeedIncrement) m_MoveSpeed = m_MoveSpeedIncrement;
            }

            bool moved = inputRotateAxisX != 0.0f || inputRotateAxisY != 0.0f || inputVertical != 0.0f || inputHorizontal != 0.0f || inputYAxis != 0.0f;
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
                if (fire1 || leftShiftBoost && leftShift)
                    moveSpeed *= m_Turbo;
                transform.position += transform.forward * moveSpeed * inputVertical;
                transform.position += transform.right * moveSpeed * inputHorizontal;
                transform.position += Vector3.up * moveSpeed * inputYAxis;
            }
        }
    }
}
