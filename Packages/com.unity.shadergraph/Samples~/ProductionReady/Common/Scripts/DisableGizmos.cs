using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class PRSDisableGizmos : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
#if UNITY_EDITOR
        SceneView view = SceneView.lastActiveSceneView;
        if (view != null)
        {
            view.drawGizmos = false;
        }
#endif
    }

}
