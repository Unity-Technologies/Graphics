using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    public class HDRenderLoopMenuItems
    {
        [UnityEditor.MenuItem("HDRenderLoop/Create Scene Settings")]
        static void CreateSceneSettings()
        {
            CommonSettings[] settings = Object.FindObjectsOfType(typeof(CommonSettings)) as CommonSettings[];

            if(settings.Length == 0)
            {
                GameObject go = new GameObject();
                go.name = "SceneSettings";
                go.AddComponent(typeof(CommonSettings));
            }
            else
            {
                Debug.LogWarning("SceneSettings has already been created.");
            }
        }
    }
}
