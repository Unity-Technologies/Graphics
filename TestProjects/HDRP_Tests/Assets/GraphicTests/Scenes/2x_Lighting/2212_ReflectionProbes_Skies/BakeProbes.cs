using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.Rendering;
#endif

public class BakeProbes : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f);
    #if UNITY_EDITOR
        var system = ScriptableBakedReflectionSystemSettings.system;
        system.BakeAllReflectionProbes();
    #endif
    }

}
