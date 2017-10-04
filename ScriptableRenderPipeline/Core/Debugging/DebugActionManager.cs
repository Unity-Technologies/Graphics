using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering
{
    public class DebugActionManager
    {
        static private DebugActionManager s_Instance = null;
        static public DebugActionManager instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new DebugActionManager();

                return s_Instance;
            }
        }

        private static string   kEnableDebugBtn1 = "Enable Debug Button 1";
        private static string   kEnableDebugBtn2 = "Enable Debug Button 2";
        private static string   kDebugPreviousBtn = "Debug Previous";
        private static string   kDebugNextBtn = "Debug Next";
        private static string   kValidateBtn = "Debug Validate";
        private static string   kPersistentBtn = "Debug Persistent";
        private static string   kDPadVertical = "Debug Vertical";
        private static string   kDPadHorizontal = "Debug Horizontal";


        public enum DebugAction
        {
            EnableDebugMenu,
            PreviousDebugPanel,
            NextDebugPanel,
            Validate,
            MakePersistent,
            MoveVertical,
            MoveHorizontal,
            DebugActionCount
        }

        enum DebugActionRepeatMode
        {
            Never,
            Delay
        }

        class DebugActionDesc
        {
            public List<string[]>           buttonTriggerList = new List<string[]>();
            public string                   axisTrigger = "";
            public List<KeyCode[]>          keyTriggerList = new List<KeyCode[]>();
            public DebugActionRepeatMode    repeatMode = DebugActionRepeatMode.Never;
            public float                    repeatDelay = 0.0f;            
        }

        class DebugActionState
        {
            enum DebugActionKeyType
            {
                Button,
                Axis,
                Key
            }

            DebugActionKeyType  m_Type;
            string[]            m_PressedButtons;
            string              m_PressedAxis = "";
            KeyCode[]           m_PressedKeys;
            bool[]              m_TriggerPressedUp = null;
            float               m_Timer;
            bool                m_RunningAction = false;
            float                m_ActionState = 0.0f;

            public bool runningAction { get { return m_RunningAction; } }
            public float actionState { get { return m_ActionState; } }
            public float timer{ get { return m_Timer; } }

            private void Trigger(int triggerCount, float state)
            {
                m_ActionState = state;
                m_RunningAction = true;
                m_Timer = 0.0f;

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

            private void Reset()
            {
                m_RunningAction = false;
                m_Timer = 0.0f;
                m_TriggerPressedUp = null;
            }

            public void Update(DebugActionDesc desc)
            {
                // Always reset this so that the action can only be caught once until repeat/reset
                m_ActionState = 0.0f;

                if (m_TriggerPressedUp != null)
                {
                    m_Timer += Time.deltaTime;

                    for(int i = 0 ; i < m_TriggerPressedUp.Length ; ++i)
                    {
                        if (m_Type == DebugActionKeyType.Button)
                            m_TriggerPressedUp[i] |= Input.GetButtonUp(m_PressedButtons[i]);
                        else if(m_Type == DebugActionKeyType.Axis)
                            m_TriggerPressedUp[i] |= (Input.GetAxis(m_PressedAxis) == 0.0f);
                        else
                            m_TriggerPressedUp[i] |= Input.GetKeyUp(m_PressedKeys[i]);
                    }

                    bool allTriggerUp = true;
                    foreach (bool value in m_TriggerPressedUp)
                        allTriggerUp &= value;

                    if(allTriggerUp || (m_Timer > desc.repeatDelay && desc.repeatMode == DebugActionRepeatMode.Delay))
                    {
                        Reset();
                    }
                }
            }
        }

        DebugActionDesc[]   m_DebugActions = null;
        DebugActionState[]  m_DebugActionStates = null;

        void RegisterActions()
        {
            m_DebugActions = new DebugActionDesc[(int)DebugAction.DebugActionCount];
            m_DebugActionStates = new DebugActionState[(int)DebugAction.DebugActionCount];

            DebugActionDesc enableDebugMenu = new DebugActionDesc();
            enableDebugMenu.buttonTriggerList.Add(new[] { kEnableDebugBtn1, kEnableDebugBtn2 });
            enableDebugMenu.keyTriggerList.Add(new[] { KeyCode.LeftControl, KeyCode.Backspace });
            enableDebugMenu.repeatMode = DebugActionRepeatMode.Never;
            AddAction(DebugAction.EnableDebugMenu, enableDebugMenu);

            DebugActionDesc nextDebugPanel = new DebugActionDesc();
            nextDebugPanel.buttonTriggerList.Add(new[] { kDebugNextBtn });
            nextDebugPanel.repeatMode = DebugActionRepeatMode.Never;
            AddAction(DebugAction.NextDebugPanel, nextDebugPanel);

            DebugActionDesc previousDebugPanel = new DebugActionDesc();
            previousDebugPanel.buttonTriggerList.Add(new[] { kDebugPreviousBtn });
            previousDebugPanel.repeatMode = DebugActionRepeatMode.Never;
            AddAction(DebugAction.PreviousDebugPanel, previousDebugPanel);

            DebugActionDesc validate = new DebugActionDesc();
            validate.buttonTriggerList.Add(new[] { kValidateBtn });
            validate.repeatMode = DebugActionRepeatMode.Delay;
            validate.repeatDelay = 0.25f;
            AddAction(DebugAction.Validate, validate);

            DebugActionDesc persistent = new DebugActionDesc();
            persistent.buttonTriggerList.Add(new[] { kPersistentBtn });
            persistent.repeatMode = DebugActionRepeatMode.Never;
            AddAction(DebugAction.MakePersistent, persistent);

            AddAction(DebugAction.MoveVertical, new DebugActionDesc { axisTrigger = kDPadVertical, repeatMode = DebugActionRepeatMode.Delay, repeatDelay = 0.4f });
            AddAction(DebugAction.MoveHorizontal, new DebugActionDesc { axisTrigger = kDPadHorizontal, repeatMode = DebugActionRepeatMode.Delay, repeatDelay = 0.4f });
        }

        DebugActionManager()
        {
            RegisterInputs();
            RegisterActions();
        }

        void AddAction(DebugAction action, DebugActionDesc desc)
        {
            int index = (int)action;
            m_DebugActions[index] = desc;
            m_DebugActionStates[index] = new DebugActionState();
        }

        void SampleAction(int actionIndex)
        {
            DebugActionDesc desc = m_DebugActions[actionIndex];
            DebugActionState state = m_DebugActionStates[actionIndex];

            //bool canSampleAction = (state.actionTriggered == false) || (desc.repeatMode == DebugActionRepeatMode.Delay && state.timer > desc.repeatDelay);
            if(state.runningAction == false)
            {
                // Check button triggers
                for (int buttonListIndex = 0; buttonListIndex < desc.buttonTriggerList.Count; ++buttonListIndex)
                {
                    string[] buttons = desc.buttonTriggerList[buttonListIndex];
                    bool allButtonPressed = true;
                    foreach (string button in buttons)
                    {
                        allButtonPressed = allButtonPressed && Input.GetButton(button);
                        if (!allButtonPressed)
                            break;
                    }

                    if (allButtonPressed)
                    {
                        state.TriggerWithButton(buttons, 1.0f);
                        break;
                    }
                }

                // Check axis triggers
                if(desc.axisTrigger != "")
                {
                    float axisValue = Input.GetAxis(desc.axisTrigger);
                    if(axisValue != 0.0f)
                    {
                        state.TriggerWithAxis(desc.axisTrigger, axisValue);
                    }
                }

                // Check key triggers
                for (int keyListIndex = 0; keyListIndex < desc.keyTriggerList.Count; ++keyListIndex)
                {
                    KeyCode[] keys = desc.keyTriggerList[keyListIndex];
                    bool allKeyPressed = true;
                    foreach (KeyCode key in keys)
                    {
                        allKeyPressed = allKeyPressed && Input.GetKey(key);
                        if (!allKeyPressed)
                            break;
                    }

                    if (allKeyPressed)
                    {
                        state.TriggerWithKey(keys, 1.0f);
                        break;
                    }
                }
            }
        }

        void UpdateAction(int actionIndex)
        {
            DebugActionDesc desc = m_DebugActions[actionIndex];
            DebugActionState state = m_DebugActionStates[actionIndex];

            if(state.runningAction)
            {
                state.Update(desc);
            }
        }

        public void Update()
        {
            for(int actionIndex = 0 ; actionIndex < m_DebugActions.Length ; ++actionIndex)
            {
                UpdateAction(actionIndex);
                SampleAction(actionIndex);
            }
        }

        public float GetAction(DebugAction action)
        {
            return m_DebugActionStates[(int)action].actionState;
        }

        void RegisterInputs()
        {
#if UNITY_EDITOR
            List <InputManagerEntry > inputEntries = new List<InputManagerEntry>();

            // Add new bindings
            inputEntries.Add(new InputManagerEntry { name = kEnableDebugBtn1, kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "left ctrl", altBtnPositive = "joystick button 8" });
            inputEntries.Add(new InputManagerEntry { name = kEnableDebugBtn2, kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "backspace", altBtnPositive = "joystick button 9" });
            
            inputEntries.Add(new InputManagerEntry { name = kDebugNextBtn, kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "page down", altBtnPositive = "joystick button 5" });
            inputEntries.Add(new InputManagerEntry { name = kDebugPreviousBtn, kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "page up", altBtnPositive = "joystick button 4" });

            inputEntries.Add(new InputManagerEntry { name = kDPadHorizontal, kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "right", btnNegative = "left", gravity = 1000.0f, deadZone = 0.001f, sensitivity = 1000.0f });
            inputEntries.Add(new InputManagerEntry { name = kDPadHorizontal, kind = InputManagerEntry.Kind.Axis, axis = InputManagerEntry.Axis.Sixth, btnPositive = "right", btnNegative = "left", gravity = 1000.0f, deadZone = 0.001f, sensitivity = 1000.0f });
            inputEntries.Add(new InputManagerEntry { name = kDPadVertical, kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "up", btnNegative = "down", gravity = 1000.0f, deadZone = 0.001f, sensitivity = 1000.0f });
            inputEntries.Add(new InputManagerEntry { name = kDPadVertical, kind = InputManagerEntry.Kind.Axis, axis = InputManagerEntry.Axis.Seventh, btnPositive = "up", btnNegative = "down", gravity = 1000.0f, deadZone = 0.001f, sensitivity = 1000.0f });
            
            inputEntries.Add(new InputManagerEntry { name = kValidateBtn, kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "return", altBtnPositive = "joystick button 0" });
            inputEntries.Add(new InputManagerEntry { name = kPersistentBtn, kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "right shift", altBtnPositive = "joystick button 2" });

            InputRegistering.RegisterInputs(inputEntries);
#endif
        }
    }
}
