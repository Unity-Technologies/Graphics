using System.Collections;
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
        private string[]        m_RequiredInputButtons = { kEnableDebugBtn1, kEnableDebugBtn2, kDebugPreviousBtn, kDebugNextBtn };


        public enum DebugAction
        {
            EnableDebugMenu,
            PreviousDebugMenu,
            NextDebugMenu,
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
            public List<KeyCode[]>          keyTriggerList = new List<KeyCode[]>();
            public DebugActionRepeatMode    repeatMode = DebugActionRepeatMode.Never;
            public float                    repeatDelay = 0.0f;            
        }

        class DebugActionState
        {
            enum DebugActionKeyType
            {
                Button,
                Key
            }

            DebugActionKeyType  m_Type;
            string[]            m_PressedButtons;
            KeyCode[]           m_PressedKeys;
            bool[]              m_TriggerPressedUp = null;
            float               m_Timer;
            bool                m_RunningAction = false;
            bool                m_Actiontriggered = false;

            public bool runningAction { get { return m_RunningAction; } }
            public bool actionTriggered { get { return m_Actiontriggered; } }
            public float timer{ get { return m_Timer; } }

            private void Trigger(int triggerCount)
            {
                m_Actiontriggered = true;
                m_RunningAction = true;
                m_Timer = 0.0f;

                m_TriggerPressedUp = new bool[triggerCount];
                for (int i = 0; i < m_TriggerPressedUp.Length; ++i)
                    m_TriggerPressedUp[i] = false;
            }

            public void Trigger(string[] buttons)
            {
                m_Type = DebugActionKeyType.Button;
                m_PressedButtons = buttons;
                Trigger(buttons.Length);
            }

            public void Trigger(KeyCode[] keys)
            {
                m_Type = DebugActionKeyType.Key;
                m_PressedKeys = keys;
                Trigger(keys.Length);
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
                m_Actiontriggered = false;

                if (m_TriggerPressedUp != null)
                {
                    m_Timer += Time.deltaTime;

                    for(int i = 0 ; i < m_TriggerPressedUp.Length ; ++i)
                    {
                        if (m_Type == DebugActionKeyType.Button)
                            m_TriggerPressedUp[i] |= Input.GetButtonUp(m_PressedButtons[i]);
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

        bool                m_Valid = false;
        DebugActionDesc[]   m_DebugActions = null;
        DebugActionState[]  m_DebugActionStates = null;

        DebugActionManager()
        {
            m_Valid = Debugging.CheckRequiredInputButtonMapping(m_RequiredInputButtons);

            m_DebugActions = new DebugActionDesc[(int)DebugAction.DebugActionCount];
            m_DebugActionStates = new DebugActionState[(int)DebugAction.DebugActionCount];

            DebugActionDesc enableDebugMenu = new DebugActionDesc();
            enableDebugMenu.buttonTriggerList.Add(new[] { kEnableDebugBtn1, kEnableDebugBtn2 });
            enableDebugMenu.keyTriggerList.Add(new[] { KeyCode.LeftControl, KeyCode.Backspace });
            enableDebugMenu.repeatMode = DebugActionRepeatMode.Never;

            AddAction(DebugAction.EnableDebugMenu, enableDebugMenu);

            DebugActionDesc nextDebugMenu = new DebugActionDesc();
            nextDebugMenu.buttonTriggerList.Add(new[] { kDebugNextBtn });
            nextDebugMenu.repeatMode = DebugActionRepeatMode.Never;
            AddAction(DebugAction.NextDebugMenu, nextDebugMenu);

            DebugActionDesc previousDebugMenu = new DebugActionDesc();
            previousDebugMenu.buttonTriggerList.Add(new[] { kDebugPreviousBtn });
            previousDebugMenu.repeatMode = DebugActionRepeatMode.Never;
            AddAction(DebugAction.PreviousDebugMenu, previousDebugMenu);
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
                        state.Trigger(buttons);
                        break;
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
                        state.Trigger(keys);
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
            if (!m_Valid)
                return;

            for(int actionIndex = 0 ; actionIndex < m_DebugActions.Length ; ++actionIndex)
            {
                UpdateAction(actionIndex);
                SampleAction(actionIndex);
            }
        }

        public bool GetAction(DebugAction action)
        {
            return m_DebugActionStates[(int)action].actionTriggered;
        }
    }
}
