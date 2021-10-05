using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class GlobalVFXProperty : MonoBehaviour
{

    public string globalVarName = "_VFXGlobalColor";
    public Color color = Color.white;

    // Update is called once per frame
    void Update()
    {
        Shader.SetGlobalColor(globalVarName, color);
    }
}
