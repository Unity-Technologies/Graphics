using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityEngine.Experimental.Rendering
{
    // This is the class that handles rendering the debug menu in the editor (as opposed to the runtime version in the player)
    public class DebugMenuEditor : EditorWindow
    {

        [MenuItem("HDRenderPipeline/Debug Menu")]
        static void DisplayDebugMenu()
        {
            var window = EditorWindow.GetWindow<DebugMenuEditor>("Debug Menu");
            window.Show();
        }

        DebugMenuManager m_DebugMenu = null;

        DebugMenuEditor()
        {
        }

        void OnEnable()
        {
            m_DebugMenu = FindObjectOfType<DebugMenuManager>();
        }

        void OnGUI()
        {
            if (m_DebugMenu == null)
                return;

            // Contrary to the menu in the player, here we always render the menu wether it's enabled or not. This is a separate window so user can manage it however they want.
            int debugMenuCount = m_DebugMenu.menuCount;
            int activeMenuIndex = m_DebugMenu.activeMenuIndex;
            using (new EditorGUILayout.HorizontalScope())
            {
                for(int i = 0 ; i < debugMenuCount ; ++i)
                {
                    GUIStyle style = EditorStyles.miniButtonMid;
                    if (i == 0)
                        style = EditorStyles.miniButtonLeft;
                    if (i == debugMenuCount - 1)
                        style = EditorStyles.miniButtonRight;
                    if (GUILayout.Toggle(i == activeMenuIndex, new GUIContent(m_DebugMenu.GetDebugMenu(i).name), style))
                        activeMenuIndex = i;
                }
            }

            m_DebugMenu.activeMenuIndex = activeMenuIndex;
        }
    }

}