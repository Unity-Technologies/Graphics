using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[ExecuteInEditMode]
public class AlignSceneView : MonoBehaviour
{

    // Start is called before the first frame update
    void Awake()
    {
         AlignCamera(transform);
    }


    private static void AlignCamera(Transform target)
    {
#if UNITY_EDITOR
            SceneView view = SceneView.lastActiveSceneView;
            if (view == null) return;
            Camera sceneCam = view.camera;
            if(sceneCam == null) return;
            sceneCam.transform.position = target.position;
            sceneCam.transform.rotation = target.rotation;
            view.AlignViewToObject(sceneCam.transform);
#endif
    }
    
}



