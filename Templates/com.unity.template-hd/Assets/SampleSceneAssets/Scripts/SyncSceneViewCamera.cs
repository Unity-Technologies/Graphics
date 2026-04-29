#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[ExecuteInEditMode]
public class SyncSceneViewCamera : MonoBehaviour
{

    void Awake()
    {
        AlignCamera(transform);
    }


    private static void AlignCamera(Transform target)
    {
#if UNITY_EDITOR
            SceneView view = SceneView.lastActiveSceneView;
            if (view == null)
                return;

            Camera sceneCam = view.camera;
            if(sceneCam == null)
                return;

            sceneCam.transform.position = target.position;
            sceneCam.transform.rotation = target.rotation;
            view.AlignViewToObject(sceneCam.transform);
#endif
    }

}



