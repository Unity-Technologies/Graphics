#if ENABLE_UIELEMENTS_MODULE && (UNITY_EDITOR || DEVELOPMENT_BUILD)
#define ENABLE_RENDERING_DEBUGGER_UI
#endif
#if ENABLE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM_PACKAGE
#define USE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Input support for Rendering Debugger using Input System Package

namespace UnityEngine.Rendering
{
    public sealed partial class DebugManager
    {
        const string k_EnableDebug = "Enable Debug";
        const string k_ResetBtn = "Debug Reset";
        const string k_DebugPreviousBtn = "Debug Previous";
        const string k_DebugNextBtn = "Debug Next";
        const string k_PersistentBtn = "Debug Persistent";
        const string k_DPadHorizontal = "Debug Horizontal";
        const string k_MultiplierBtn = "Debug Multiplier";

#if USE_INPUT_SYSTEM
        const string k_AnyTouch = "Any Touch";

        readonly InputActionMap m_DebugMenuEnableActions = new InputActionMap("Debug Menu Enable Actions");
        readonly InputActionMap m_DebugMenuActions = new InputActionMap("Debug Menu Actions");
        InputAction m_MultiplierAction;

        internal void EnableInputCallbacks()
        {
            m_DebugMenuEnableActions.Enable();
        }

        internal void DisableInputCallbacks()
        {
            m_DebugMenuEnableActions.Disable();
        }

        void ToggleRuntimeUI() => displayRuntimeUI = !displayRuntimeUI;

        void RegisterDebugInputs()
        {
            var enableAction = m_DebugMenuEnableActions.AddAction(k_EnableDebug, type: InputActionType.Button);
            enableAction.AddCompositeBinding("ButtonWithOneModifier")
                .With("Modifier", "<Gamepad>/rightStickPress")
                .With("Button", "<Gamepad>/leftStickPress")
                .With("Modifier", "<Keyboard>/leftCtrl")
                .With("Button", "<Keyboard>/backspace");
            enableAction.performed += _ => ToggleRuntimeUI();

            var anyTouchAction = m_DebugMenuEnableActions.AddAction(k_AnyTouch, type: InputActionType.Button);
            anyTouchAction.AddBinding("<Touchscreen>/touch2/tap"); // touch2 means "third finger"
            anyTouchAction.performed += ctx =>
            {
                // We want a three-finger double-tap to toggle the debug UI. However, it's a bit tricky to perform a
                // three-finger tap consistently. Therefore, to be slightly error-tolerant, we actually check for a
                // "double-tap action on the third tracked finger".
                var pressControl = ctx.control as InputSystem.Controls.ButtonControl;
                var touchControl = pressControl?.parent as InputSystem.Controls.TouchControl;
                int tapCount = touchControl?.tapCount?.ReadValue() ?? 0;
                if (tapCount == 2)
                    ToggleRuntimeUI();
            };

#if ENABLE_RENDERING_DEBUGGER_UI
            var resetAction = m_DebugMenuActions.AddAction(k_ResetBtn, type: InputActionType.Button);
            resetAction.AddCompositeBinding("ButtonWithOneModifier")
                .With("Modifier", "<Gamepad>/rightStickPress")
                .With("Button", "<Gamepad>/b")
                .With("Modifier", "<Keyboard>/leftAlt")
                .With("Button", "<Keyboard>/backspace");
            resetAction.performed += _ => { Reset(); };

            var next = m_DebugMenuActions.AddAction(k_DebugNextBtn, type: InputActionType.Button);
            next.AddBinding("<Keyboard>/pageDown");
            next.AddBinding("<Gamepad>/rightShoulder");
            next.performed += _ => { m_RuntimeDebugWindow.SelectNextPanel(); };

            var previous = m_DebugMenuActions.AddAction(k_DebugPreviousBtn, type: InputActionType.Button);
            previous.AddBinding("<Keyboard>/pageUp");
            previous.AddBinding("<Gamepad>/leftShoulder");
            previous.performed += _ => { m_RuntimeDebugWindow.SelectPreviousPanel(); };

            var persistentAction = m_DebugMenuActions.AddAction(k_PersistentBtn, type: InputActionType.Button);
            persistentAction.AddBinding("<Keyboard>/rightShift");
            persistentAction.AddBinding("<Gamepad>/x");
            persistentAction.performed += _ => { TogglePersistent(); };

            m_MultiplierAction = m_DebugMenuActions.AddAction(k_MultiplierBtn, type: InputActionType.Value);
            m_MultiplierAction.AddBinding("<Keyboard>/leftShift");
            m_MultiplierAction.AddBinding("<Gamepad>/y");

            var moveHorizontalAction = m_DebugMenuActions.AddAction(k_DPadHorizontal);
            moveHorizontalAction.AddCompositeBinding("1DAxis")
                .With("Positive", "<Gamepad>/dpad/right")
                .With("Negative", "<Gamepad>/dpad/left")
                .With("Positive", "<Keyboard>/rightArrow")
                .With("Negative", "<Keyboard>/leftArrow");
            moveHorizontalAction.performed += ctx =>
            {
                bool multiplierPressed = m_MultiplierAction.IsPressed();
                bool increment = ctx.ReadValue<float>() > 0.0f;
                if (increment)
                {
                    selectedWidget?.OnIncrement(multiplierPressed);
                }
                else
                {
                    selectedWidget?.OnDecrement(multiplierPressed);
                }
            };
#endif
        }
#endif
    }
}
