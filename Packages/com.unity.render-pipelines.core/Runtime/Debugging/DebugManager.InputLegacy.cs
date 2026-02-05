#if ENABLE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM_PACKAGE
#define USE_INPUT_SYSTEM
#endif

// Input support for Rendering Debugger using legacy InputManager

#if !USE_INPUT_SYSTEM

using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    internal enum DebugAction
    {
        EnableDebugMenu,
        PreviousDebugPanel,
        NextDebugPanel,
        MakePersistent,
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
        const string k_EnableDebugBtn1 = "Enable Debug Button 1";
        const string k_EnableDebugBtn2 = "Enable Debug Button 2";

        DebugActionDesc[] m_DebugActions;
        DebugActionState[] m_DebugActionStates;

        void RegisterActions()
        {
            m_DebugActions = new DebugActionDesc[(int)DebugAction.DebugActionCount];
            m_DebugActionStates = new DebugActionState[(int)DebugAction.DebugActionCount];

            var enableDebugMenu = new DebugActionDesc();
            enableDebugMenu.buttonTriggerList.Add(new[] { k_EnableDebugBtn1, k_EnableDebugBtn2 });
            enableDebugMenu.keyTriggerList.Add(new[] { KeyCode.LeftControl, KeyCode.Backspace });
            enableDebugMenu.repeatMode = DebugActionRepeatMode.Never;
            AddAction(DebugAction.EnableDebugMenu, enableDebugMenu);

            var resetDebugMenu = new DebugActionDesc();
            resetDebugMenu.keyTriggerList.Add(new[] { KeyCode.LeftAlt, KeyCode.Backspace });
            resetDebugMenu.buttonTriggerList.Add(new[] { k_ResetBtn, k_EnableDebugBtn2 });
            resetDebugMenu.repeatMode = DebugActionRepeatMode.Never;
            AddAction(DebugAction.ResetAll, resetDebugMenu);

            var nextDebugPanel = new DebugActionDesc();
            nextDebugPanel.buttonTriggerList.Add(new[] { k_DebugNextBtn });
            nextDebugPanel.repeatMode = DebugActionRepeatMode.Never;
            AddAction(DebugAction.NextDebugPanel, nextDebugPanel);

            var previousDebugPanel = new DebugActionDesc();
            previousDebugPanel.buttonTriggerList.Add(new[] { k_DebugPreviousBtn });
            previousDebugPanel.repeatMode = DebugActionRepeatMode.Never;
            AddAction(DebugAction.PreviousDebugPanel, previousDebugPanel);

            var persistent = new DebugActionDesc();
            persistent.buttonTriggerList.Add(new[] { k_PersistentBtn });
            persistent.repeatMode = DebugActionRepeatMode.Never;
            AddAction(DebugAction.MakePersistent, persistent);

            var multiplier = new DebugActionDesc();
            multiplier.buttonTriggerList.Add(new[] { k_MultiplierBtn });
            multiplier.repeatMode = DebugActionRepeatMode.Delay;
            multiplier.repeatDelay = 0f;
            AddAction(DebugAction.Multiplier, multiplier);

            var moveHorizontal = new DebugActionDesc();
            moveHorizontal.axisTrigger = k_DPadHorizontal;
            moveHorizontal.repeatMode = DebugActionRepeatMode.Delay;
            moveHorizontal.repeatDelay = 0.16f;
            AddAction(DebugAction.MoveHorizontal, moveHorizontal);
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

#if ENABLE_LEGACY_INPUT_MANAGER
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
            var touchCount = Input.touchCount;
            TouchPhase expectedTouchPhase = TouchPhase.Began;
            if (touchCount == 3)
            {
                var touches = Input.touches; // Note: Causes an allocation
                foreach (var touch in touches)
                {
                    // Gesture: 3-finger double-tap
                    if (touch.phase == expectedTouchPhase && touch.tapCount == 2)
                        return true;
                }
            }

            return false;
        }

        void RegisterDebugInputs()
        {
#if UNITY_EDITOR
#pragma warning disable CS0618 // Type or member is obsolete
            var inputEntries = new List<InputManagerEntry>
            {
                new InputManagerEntry { name = k_EnableDebugBtn1,  kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "left ctrl",   altBtnPositive = "joystick button 8" },
                new InputManagerEntry { name = k_EnableDebugBtn2,  kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "backspace",   altBtnPositive = "joystick button 9" },
                new InputManagerEntry { name = k_ResetBtn,         kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "left alt",    altBtnPositive = "joystick button 1" },
                new InputManagerEntry { name = k_DebugNextBtn,     kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "page down",   altBtnPositive = "joystick button 5" },
                new InputManagerEntry { name = k_DebugPreviousBtn, kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "page up",     altBtnPositive = "joystick button 4" },
                new InputManagerEntry { name = k_PersistentBtn,    kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "right shift", altBtnPositive = "joystick button 2" },
                new InputManagerEntry { name = k_MultiplierBtn,    kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "left shift",  altBtnPositive = "joystick button 3" },
                new InputManagerEntry { name = k_DPadHorizontal,   kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "right",       btnNegative = "left", gravity = 1000f, deadZone = 0.001f, sensitivity = 1000f },
                new InputManagerEntry { name = k_DPadHorizontal,   kind = InputManagerEntry.Kind.Axis, axis = InputManagerEntry.Axis.Sixth,   btnPositive = "right", btnNegative = "left", gravity = 1000f, deadZone = 0.001f, sensitivity = 1000f },
            };

            InputRegistering.RegisterInputs(inputEntries);
#pragma warning restore CS0618 // Type or member is obsolete
#endif
            RegisterActions();
        }
    }

    class DebugActionDesc
    {
        public string axisTrigger = "";
        public List<string[]> buttonTriggerList = new List<string[]>();
        public List<KeyCode[]> keyTriggerList = new List<KeyCode[]>();
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
        string[] m_PressedButtons;
        string m_PressedAxis = "";
        KeyCode[] m_PressedKeys;
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
                    if (m_Type == DebugActionKeyType.Button)
                        m_TriggerPressedUp[i] |= Input.GetButtonUp(m_PressedButtons[i]);
                    else if (m_Type == DebugActionKeyType.Axis)
                        m_TriggerPressedUp[i] |= Mathf.Approximately(Input.GetAxis(m_PressedAxis), 0f);
                    else
                        m_TriggerPressedUp[i] |= Input.GetKeyUp(m_PressedKeys[i]);
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

#endif
