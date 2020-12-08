using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Experimental.Rendering;

public class BakeProbes : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f);
        var system = ScriptableBakedReflectionSystemSettings.system;
        system.BakeAllReflectionProbes();
    }

}
