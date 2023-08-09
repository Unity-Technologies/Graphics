#if ENABLE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM_PACKAGE
#define USE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
#endif

using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    internal enum DebugAction
    {
        EnableDebugMenu,
        PreviousDebugPanel,
        NextDebugPanel,
        Action,
        MakePersistent,
        MoveVertical,
        MoveHorizontal,
        Multiplier,
        ResetAll,
        DebugActionCount
    }

    enum DebugActionRepeatMode
    {
        Never,
        Delay
    }

    public sealed partial class DebugManager
    {
        const string kEnableDebugBtn1 = "Enable Debug Button 1";
        const string kEnableDebugBtn2 = "Enable Debug Button 2";
        const string kDebugPreviousBtn = "Debug Previous";
        const string kDebugNextBtn = "Debug Next";
        const string kValidateBtn = "Debug Validate";
        const string kPersistentBtn = "Debug Persistent";
        const string kDPadVertical = "Debug Vertical";
        const string kDPadHorizontal = "Debug Horizontal";
        const string kMultiplierBtn = "Debug Multiplier";
        const string kResetBtn = "Debug Reset";
        const string kEnableDebug = "Enable Debug";

        DebugActionDesc[] m_DebugActions;
        DebugActionState[] m_DebugActionStates;

#if USE_INPUT_SYSTEM
        InputActionMap debugActionMap = new InputActionMap("Debug Menu");
#endif

        void RegisterActions()
        {
            m_DebugActions = new DebugActionDesc[(int)DebugAction.DebugActionCount];
            m_DebugActionStates = new DebugActionState[(int)DebugAction.DebugActionCount];

            var enableDebugMenu = new DebugActionDesc();
#if USE_INPUT_SYSTEM
            enableDebugMenu.buttonAction = debugActionMap.FindAction(kEnableDebug);
#else
            enableDebugMenu.buttonTriggerList.Add(new[] { kEnableDebugBtn1, kEnableDebugBtn2 });
            enableDebugMenu.keyTriggerList.Add(new[] { KeyCode.LeftControl, KeyCode.Backspace });
#endif
            enableDebugMenu.repeatMode = DebugActionRepeatMode.Never;
            AddAction(DebugAction.EnableDebugMenu, enableDebugMenu);

            var resetDebugMenu = new DebugActionDesc();
#if USE_INPUT_SYSTEM
            resetDebugMenu.buttonAction = debugActionMap.FindAction(kResetBtn);
#else
            resetDebugMenu.keyTriggerList.Add(new[] { KeyCode.LeftAlt, KeyCode.Backspace });
            resetDebugMenu.buttonTriggerList.Add(new[] { kResetBtn, kEnableDebugBtn2 });
#endif
            resetDebugMenu.repeatMode = DebugActionRepeatMode.Never;
            AddAction(DebugAction.ResetAll, resetDebugMenu);

            var nextDebugPanel = new DebugActionDesc();
#if USE_INPUT_SYSTEM
            nextDebugPanel.buttonAction = debugActionMap.FindAction(kDebugNextBtn);
#else
            nextDebugPanel.buttonTriggerList.Add(new[] { kDebugNextBtn });
#endif
            nextDebugPanel.repeatMode = DebugActionRepeatMode.Never;
            AddAction(DebugAction.NextDebugPanel, nextDebugPanel);

            var previousDebugPanel = new DebugActionDesc();
#if USE_INPUT_SYSTEM
            previousDebugPanel.buttonAction = debugActionMap.FindAction(kDebugPreviousBtn);
#else
            previousDebugPanel.buttonTriggerList.Add(new[] { kDebugPreviousBtn });
#endif
            previousDebugPanel.repeatMode = DebugActionRepeatMode.Never;
            AddAction(DebugAction.PreviousDebugPanel, previousDebugPanel);

            var validate = new DebugActionDesc();
#if USE_INPUT_SYSTEM
            validate.buttonAction = debugActionMap.FindAction(kValidateBtn);
#else
            validate.buttonTriggerList.Add(new[] { kValidateBtn });
#endif
            validate.repeatMode = DebugActionRepeatMode.Never;
            AddAction(DebugAction.Action, validate);

            var persistent = new DebugActionDesc();
#if USE_INPUT_SYSTEM
            persistent.buttonAction = debugActionMap.FindAction(kPersistentBtn);
#else
            persistent.buttonTriggerList.Add(new[] { kPersistentBtn });
#endif
            persistent.repeatMode = DebugActionRepeatMode.Never;
            AddAction(DebugAction.MakePersistent, persistent);

            var multiplier = new DebugActionDesc();
#if USE_INPUT_SYSTEM
            multiplier.buttonAction = debugActionMap.FindAction(kMultiplierBtn);
#else
            multiplier.buttonTriggerList.Add(new[] { kMultiplierBtn });
#endif
            multiplier.repeatMode = DebugActionRepeatMode.Delay;
            validate.repeatDelay = 0f;

            AddAction(DebugAction.Multiplier, multiplier);

            var moveVertical = new DebugActionDesc();
#if USE_INPUT_SYSTEM
            moveVertical.buttonAction = debugActionMap.FindAction(kDPadVertical);
#else
            moveVertical.axisTrigger = kDPadVertical;
#endif
            moveVertical.repeatMode = DebugActionRepeatMode.Delay;
            moveVertical.repeatDelay = 0.16f;
            AddAction(DebugAction.MoveVertical, moveVertical);

            var moveHorizontal = new DebugActionDesc();
#if USE_INPUT_SYSTEM
            moveHorizontal.buttonAction = debugActionMap.FindAction(kDPadHorizontal);
#else
            moveHorizontal.axisTrigger = kDPadHorizontal;
#endif
            moveHorizontal.repeatMode = DebugActionRepeatMode.Delay;
            moveHorizontal.repeatDelay = 0.16f;
            AddAction(DebugAction.MoveHorizontal, moveHorizontal);
        }

        internal void EnableInputActions()
        {
#if USE_INPUT_SYSTEM
            foreach (var action in debugActionMap)
                action.Enable();
#endif
        }

        void AddAction(DebugAction action, DebugActionDesc desc)
        {
            int index = (int)action;
            m_DebugActions[index] = desc;
            m_DebugActionStates[index] = new DebugActionState();
        }

        void SampleAction(int actionIndex)
        {
            var desc = m_DebugActions[actionIndex];
            var state = m_DebugActionStates[actionIndex];

            // Disable all input events if we're using the new input system
#if USE_INPUT_SYSTEM
            if (state.runningAction == false)
            {
                if (desc.buttonAction != null)
                {
                    var value = desc.buttonAction.ReadValue<float>();
                    if (!Mathf.Approximately(value, 0))
                        state.TriggerWithButton(desc.buttonAction, value);
                }
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            //bool canSampleAction = (state.actionTriggered == false) || (desc.repeatMode == DebugActionRepeatMode.Delay && state.timer > desc.repeatDelay);
            if (state.runningAction == false)
            {
                // Check button triggers
                for (int buttonListIndex = 0; buttonListIndex < desc.buttonTriggerList.Count; ++buttonListIndex)
                {
                    var buttons = desc.buttonTriggerList[buttonListIndex];
                    bool allButtonPressed = true;

                    try
                    {
                        foreach (var button in buttons)
                        {
                            allButtonPressed = Input.GetButton(button);
                            if (!allButtonPressed)
                                break;
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Exception thrown if the input mapping gets removed while in play mode (UUM-37148)
                        allButtonPressed = false;
                    }

                    if (allButtonPressed)
                    {
                        state.TriggerWithButton(buttons, 1f);
                        break;
                    }
                }

                // Check axis triggers
                if (desc.axisTrigger != "")
                {
                    try
                    {
                        float axisValue = Input.GetAxis(desc.axisTrigger);

                        if (axisValue != 0f)
                            state.TriggerWithAxis(desc.axisTrigger, axisValue);
                    }
                    catch (ArgumentException)
                    {
                        // Exception thrown if the input mapping gets removed while in play mode (UUM-37148)
                    }
                }

                // Check key triggers
                for (int keyListIndex = 0; keyListIndex < desc.keyTriggerList.Count; ++keyListIndex)
                {
                    bool allKeyPressed = true;

                    var keys = desc.keyTriggerList[keyListIndex];

                    try
                    {
                        foreach (var key in keys)
                        {
                            allKeyPressed = Input.GetKey(key);
                            if (!allKeyPressed)
                                break;
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Exception thrown if the input mapping gets removed while in play mode (UUM-37148)
                        allKeyPressed = false;
                    }

                    if (allKeyPressed)
                    {
                        state.TriggerWithKey(keys, 1f);
                        break;
                    }
                }
            }

#endif
        }

        void UpdateAction(int actionIndex)
        {
            var desc = m_DebugActions[actionIndex];
            var state = m_DebugActionStates[actionIndex];

            if (state.runningAction)
                state.Update(desc);
        }

        internal void UpdateActions()
        {
            for (int actionIndex = 0; actionIndex < m_DebugActions.Length; ++actionIndex)
            {
                UpdateAction(actionIndex);
                SampleAction(actionIndex);
            }
        }

        internal float GetAction(DebugAction action)
        {
            return m_DebugActionStates[(int)action].actionState;
        }

        internal bool GetActionToggleDebugMenuWithTouch()
        {
#if USE_INPUT_SYSTEM
            if (!EnhancedTouchSupport.enabled)
                return false;

            var touches = InputSystem.EnhancedTouch.Touch.activeTouches;
            var touchCount = touches.Count;
            InputSystem.TouchPhase? expectedTouchPhase = null;
#else
            var touches = Input.touches;
            var touchCount = Input.touchCount;
            TouchPhase? expectedTouchPhase = TouchPhase.Began;
#endif
            if (touchCount == 3)
            {
                foreach (var touch in touches)
                {
                    // Gesture: 3-finger double-tap
                    if ((!expectedTouchPhase.HasValue || touch.phase == expectedTouchPhase.Value) && touch.tapCount == 2)
                        return true;
                }
            }

            return false;
        }

        internal bool GetActionReleaseScrollTarget()
        {
#if USE_INPUT_SYSTEM
            bool mouseWheelActive = Mouse.current != null && Mouse.current.scroll.ReadValue() != Vector2.zero;
            bool touchSupported = Touchscreen.current != null;
#else
            bool mouseWheelActive = Input.mouseScrollDelta != Vector2.zero;
            bool touchSupported = Input.touchSupported;
#endif
            return mouseWheelActive || touchSupported; // Touchscreens have general problems with scrolling, so it's disabled.
        }

        void RegisterInputs()
        {
#if UNITY_EDITOR && !USE_INPUT_SYSTEM
            var inputEntries = new List<InputManagerEntry>
            {
                new InputManagerEntry { name = kEnableDebugBtn1,  kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "left ctrl",   altBtnPositive = "joystick button 8" },
                new InputManagerEntry { name = kEnableDebugBtn2,  kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "backspace",   altBtnPositive = "joystick button 9" },
                new InputManagerEntry { name = kResetBtn,         kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "left alt",    altBtnPositive = "joystick button 1" },
                new InputManagerEntry { name = kDebugNextBtn,     kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "page down",   altBtnPositive = "joystick button 5" },
                new InputManagerEntry { name = kDebugPreviousBtn, kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "page up",     altBtnPositive = "joystick button 4" },
                new InputManagerEntry { name = kValidateBtn,      kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "return",      altBtnPositive = "joystick button 0" },
                new InputManagerEntry { name = kPersistentBtn,    kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "right shift", altBtnPositive = "joystick button 2" },
                new InputManagerEntry { name = kMultiplierBtn,    kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "left shift",  altBtnPositive = "joystick button 3" },
                new InputManagerEntry { name = kDPadHorizontal,   kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "right",       btnNegative = "left", gravity = 1000f, deadZone = 0.001f, sensitivity = 1000f },
                new InputManagerEntry { name = kDPadVertical,     kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "up",          btnNegative = "down", gravity = 1000f, deadZone = 0.001f, sensitivity = 1000f },
                new InputManagerEntry { name = kDPadVertical,     kind = InputManagerEntry.Kind.Axis, axis = InputManagerEntry.Axis.Seventh, btnPositive = "up",    btnNegative = "down", gravity = 1000f, deadZone = 0.001f, sensitivity = 1000f },
                new InputManagerEntry { name = kDPadHorizontal,   kind = InputManagerEntry.Kind.Axis, axis = InputManagerEntry.Axis.Sixth,   btnPositive = "right", btnNegative = "left", gravity = 1000f, deadZone = 0.001f, sensitivity = 1000f },
            };

            InputRegistering.RegisterInputs(inputEntries);
#endif

#if USE_INPUT_SYSTEM
            // Register input system actions
            var enableAction = debugActionMap.AddAction(kEnableDebug, type: InputActionType.Button);
            enableAction.AddCompositeBinding("ButtonWithOneModifier")
                .With("Modifier", "<Gamepad>/rightStickPress")
                .With("Button", "<Gamepad>/leftStickPress")
                .With("Modifier", "<Keyboard>/leftCtrl")
                .With("Button", "<Keyboard>/backspace");

            var resetAction = debugActionMap.AddAction(kResetBtn, type: InputActionType.Button);
            resetAction.AddCompositeBinding("ButtonWithOneModifier")
                .With("Modifier", "<Gamepad>/rightStickPress")
                .With("Button", "<Gamepad>/b")
                .With("Modifier", "<Keyboard>/leftAlt")
                .With("Button", "<Keyboard>/backspace");

            var next = debugActionMap.AddAction(kDebugNextBtn, type: InputActionType.Button);
            next.AddBinding("<Keyboard>/pageDown");
            next.AddBinding("<Gamepad>/rightShoulder");

            var previous = debugActionMap.AddAction(kDebugPreviousBtn, type: InputActionType.Button);
            previous.AddBinding("<Keyboard>/pageUp");
            previous.AddBinding("<Gamepad>/leftShoulder");

            var validateAction = debugActionMap.AddAction(kValidateBtn, type: InputActionType.Button);
            validateAction.AddBinding("<Keyboard>/enter");
            validateAction.AddBinding("<Gamepad>/a");

            var persistentAction = debugActionMap.AddAction(kPersistentBtn, type: InputActionType.Button);
            persistentAction.AddBinding("<Keyboard>/rightShift");
            persistentAction.AddBinding("<Gamepad>/x");

            var multiplierAction = debugActionMap.AddAction(kMultiplierBtn, type: InputActionType.Value);
            multiplierAction.AddBinding("<Keyboard>/leftShift");
            multiplierAction.AddBinding("<Gamepad>/y");

            var moveVerticalAction = debugActionMap.AddAction(kDPadVertical);
            moveVerticalAction.AddCompositeBinding("1DAxis")
                .With("Positive", "<Gamepad>/dpad/up")
                .With("Negative", "<Gamepad>/dpad/down")
                .With("Positive", "<Keyboard>/upArrow")
                .With("Negative", "<Keyboard>/downArrow");

            var moveHorizontalAction = debugActionMap.AddAction(kDPadHorizontal);
            moveHorizontalAction.AddCompositeBinding("1DAxis")
                .With("Positive", "<Gamepad>/dpad/right")
                .With("Negative", "<Gamepad>/dpad/left")
                .With("Positive", "<Keyboard>/rightArrow")
                .With("Negative", "<Keyboard>/leftArrow");
#endif
        }
    }

    class DebugActionDesc
    {
#if USE_INPUT_SYSTEM
        public InputAction buttonAction = null;
#else
        public string axisTrigger = "";
        public List<string[]> buttonTriggerList = new List<string[]>();
        public List<KeyCode[]> keyTriggerList = new List<KeyCode[]>();
#endif
        public DebugActionRepeatMode repeatMode = DebugActionRepeatMode.Never;
        public float repeatDelay;
    }

    class DebugActionState
    {
        enum DebugActionKeyType
        {
            Button,
            Axis,
            Key
        }

        DebugActionKeyType m_Type;
#if USE_INPUT_SYSTEM
        InputAction inputAction;
#else
        string[] m_PressedButtons;
        string m_PressedAxis = "";
        KeyCode[] m_PressedKeys;
#endif
        bool[] m_TriggerPressedUp;
        float m_Timer;

        internal bool runningAction { get; private set; }
        internal float actionState { get; private set; }

        void Trigger(int triggerCount, float state)
        {
            actionState = state;
            runningAction = true;
            m_Timer = 0f;

            m_TriggerPressedUp = new bool[triggerCount];
            for (int i = 0; i < m_TriggerPressedUp.Length; ++i)
                m_TriggerPressedUp[i] = false;
        }

#if USE_INPUT_SYSTEM
        public void TriggerWithButton(InputAction action, float state)
        {
            inputAction = action;
            Trigger(action.bindings.Count, state);
        }

#else
        public void TriggerWithButton(string[] buttons, float state)
        {
            m_Type = DebugActionKeyType.Button;
            m_PressedButtons = buttons;
            m_PressedAxis = "";
            Trigger(buttons.Length, state);
        }

        public void TriggerWithAxis(string axis, float state)
        {
            m_Type = DebugActionKeyType.Axis;
            m_PressedAxis = axis;
            Trigger(1, state);
        }

        public void TriggerWithKey(KeyCode[] keys, float state)
        {
            m_Type = DebugActionKeyType.Key;
            m_PressedKeys = keys;
            m_PressedAxis = "";
            Trigger(keys.Length, state);
        }

#endif

        void Reset()
        {
            runningAction = false;
            m_Timer = 0f;
            m_TriggerPressedUp = null;
        }

        public void Update(DebugActionDesc desc)
        {
            // Always reset this so that the action can only be caught once until repeat/reset
            actionState = 0f;

            if (m_TriggerPressedUp != null)
            {
                m_Timer += Time.deltaTime;

                for (int i = 0; i < m_TriggerPressedUp.Length; ++i)
                {
#if USE_INPUT_SYSTEM
                    if (inputAction != null)
                        m_TriggerPressedUp[i] |= Mathf.Approximately(inputAction.ReadValue<float>(), 0f);
#else
                    if (m_Type == DebugActionKeyType.Button)
                        m_TriggerPressedUp[i] |= Input.GetButtonUp(m_PressedButtons[i]);
                    else if (m_Type == DebugActionKeyType.Axis)
                        m_TriggerPressedUp[i] |= Mathf.Approximately(Input.GetAxis(m_PressedAxis), 0f);
                    else
                        m_TriggerPressedUp[i] |= Input.GetKeyUp(m_PressedKeys[i]);
#endif
                }

                bool allTriggerUp = true;
                foreach (bool value in m_TriggerPressedUp)
                    allTriggerUp &= value;

                if (allTriggerUp || (m_Timer > desc.repeatDelay && desc.repeatMode == DebugActionRepeatMode.Delay))
                    Reset();
            }
        }
    }
}
