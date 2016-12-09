using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShaderLOD : MonoBehaviour {

	void OnGUI()
    {
        if (GUILayout.Button("200"))
        {
            Shader.globalMaximumLOD = 200;
        }
        if (GUILayout.Button("400"))
        {
            Shader.globalMaximumLOD = 400;
        }
    }
}
