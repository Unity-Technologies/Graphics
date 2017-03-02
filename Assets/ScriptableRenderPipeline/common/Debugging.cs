using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Debugging : MonoBehaviour 
{

    private static bool m_DebugControlEnabled = false;
    public static bool debugControlEnabled { get { return m_DebugControlEnabled;  } }

    private float m_DebugControlEnabledMsgTime = 3.0f;
    private float m_DebugControlEnabledMsgTimer = 0.0f;

    private bool m_DebugKeyUp1 = false;
    private bool m_DebugKeyUp2 = false;
    private bool m_CanReceiveInput = true;

    private static List<string> m_DebugMessages = new List<string>();

    private static string kEnableDebugBtn1 = "Enable Debug Button 1";
    private static string kEnableDebugBtn2 = "Enable Debug Button 2";
    private string[] m_RequiredInputButtons = { kEnableDebugBtn1, kEnableDebugBtn2 };
    private bool m_Valid = true;

    public static void PushDebugMessage(string message)
    {
        m_DebugMessages.Add(message);
    }

    static public bool CheckRequiredInputButtonMapping(string[] values)
    {
        bool inputsOk = true;
        foreach(string value in values)
        {
            try
            {
                Input.GetButton(value);
            }
            catch
            {
                Debug.LogWarning(string.Format("Required input button mapping missing: {0}.", value));
                inputsOk = false;
            }
        }

        return inputsOk;
    }

    static public bool CheckRequiredInputAxisMapping(string[] values)
    {
        bool inputsOk = true;
        foreach (string value in values)
        {
            try
            {
                Input.GetAxis(value);
            }
            catch
            {
                Debug.LogWarning(string.Format("Required input axis mapping missing: {0}.", value));
                inputsOk = false;
            }
        }

        return inputsOk;
    }

    void OnEnable()
    {
        m_Valid = CheckRequiredInputButtonMapping(m_RequiredInputButtons);
    }

    void Update()
    {
        if(m_Valid)
        {
            m_DebugControlEnabledMsgTimer += Time.deltaTime;

            bool enableDebug = Input.GetButton(kEnableDebugBtn1) && Input.GetButton(kEnableDebugBtn2) || Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.Backspace);

            if (m_CanReceiveInput && enableDebug)
            {
                m_DebugControlEnabled = !m_DebugControlEnabled;
                m_DebugControlEnabledMsgTimer = 0.0f;
                m_CanReceiveInput = false;
                m_DebugKeyUp1 = false;
                m_DebugKeyUp2 = false;
            }

            if (Input.GetButtonUp(kEnableDebugBtn1))
            {
                m_DebugKeyUp1 = true;
            }
            if (Input.GetButtonUp(kEnableDebugBtn2))
            {
                m_DebugKeyUp2 = true;
            }

            // For keyboard you want to be able to keep ctrl pressed.
            if (Input.GetKeyUp(KeyCode.Backspace))
            {
                m_DebugKeyUp1 = m_DebugKeyUp2 = true;
            }

            m_CanReceiveInput = m_DebugKeyUp1 && m_DebugKeyUp2;

            if (m_DebugControlEnabledMsgTimer < m_DebugControlEnabledMsgTime)
            {
                if (m_DebugControlEnabled)
                    PushDebugMessage("Debug Controls Enabled");
                else
                    PushDebugMessage("Debug Controls Disabled");
            }
        }
    }

    void OnGUI()
    {
        using(new GUILayout.HorizontalScope())
        {
            GUILayout.Space(10.0f);
            using(new GUILayout.VerticalScope())
            {
                GUILayout.Space(10.0f);
                for (int i = 0; i < m_DebugMessages.Count; ++i)
                {
                    GUILayout.Label(m_DebugMessages[i]);
                }
            }
        }

        // Make sure to clear only after all relevant events have occured.
        if (Event.current.type == EventType.Repaint)
            m_DebugMessages.Clear();
    }
}
