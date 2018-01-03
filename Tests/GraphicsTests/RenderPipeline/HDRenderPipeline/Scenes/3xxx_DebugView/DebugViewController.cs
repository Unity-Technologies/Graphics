using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering;

public class DebugViewController : MonoBehaviour
{
    [ContextMenu("Do something")]
    public void DoSomething()
    {
        DebugMenuEditor debugMenuWindow = EditorWindow.GetWindow<DebugMenuEditor>();

        DebugMenuManager debugMenuManager = DebugMenuManager.instance;

        //debugMenuManager.SetDebugMenuState(DebugMenuState.)
    }
}
