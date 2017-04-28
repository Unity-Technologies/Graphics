using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class Old : MonoBehaviour {

    void Start () 
    {
        var computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/VFXShaders/VFXFillIndirectArgs.compute"); //HACK : issue with VFXManager Settings...
        if (computeShader)
        {
            VFXComponent.SetIndirectCompute(computeShader);
        }
        else
        {
            Debug.LogErrorFormat("Unable to retrieve VFXFillIndirectArgs");
        }
        
    }

    // Update is called once per frame
    void Update ()
    {
        
    }
}
