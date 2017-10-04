using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering
{
    public class Debugging : MonoBehaviour
    {
        private static List<string> m_DebugMessages = new List<string>();


        public static void PushDebugMessage(string message)
        {
            m_DebugMessages.Add(message);
        }

        static public bool CheckRequiredInputButtonMapping(string[] values)
        {
            bool inputsOk = true;
            foreach (string value in values)
            {
                try
                {
                    Input.GetButton(value);
                }
                catch
                {
                    Debug.LogError(string.Format("Required input button mapping missing: {0}.", value));
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


        void OnGUI()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(10.0f);
                using (new GUILayout.VerticalScope())
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
}
