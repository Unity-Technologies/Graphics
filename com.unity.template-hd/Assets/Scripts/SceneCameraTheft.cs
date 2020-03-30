using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Code by Alex Lovett ( HeliosDoubleSix ) ( Shadowood.uk )
/// Contact heliosdoublesix@gmail.com
/// </summary>
[ExecuteInEditMode, InitializeOnLoad]
#endif
public class SceneCameraTheft : MonoBehaviour
{
    public bool runInPlayMode = false;
    private Camera targetCamera;
    public GameObject targetObject;
#if UNITY_EDITOR
    private void Start()
    {
        EditorApplication.update -= Update;
        EditorApplication.update += Update;
    }

    public Camera sceneCamera = null;

    public void Update()
    {

        if (gameObject.GetComponent<Camera>() != null)
            targetObject = gameObject;
        targetCamera = targetObject.GetComponentInChildren<Camera>();
        if (targetObject!= null)
        {
            if (!gameObject.activeInHierarchy) return;
            if (!runInPlayMode && Application.isPlaying) return;
            if (sceneCamera == null && SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
            {
                sceneCamera = SceneView.lastActiveSceneView.camera;
            }

            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera.transform.localPosition != new Vector3()) targetCamera.transform.localPosition = new Vector3(0, 0, 0);
            if (targetCamera.transform.localRotation.eulerAngles != new Vector3()) targetCamera.transform.localRotation = new Quaternion();

            if (Application.isPlaying && !runInPlayMode)
            {
                EditorApplication.update -= Update;
                //transform.position = new Vector3(0,0,0);
                transform.localPosition = new Vector3(0, 0, 0);
                transform.rotation = new Quaternion();
                //Debug.Log("CopySceneView update disabled during playback" );
            }
            else
            {
                if (sceneCamera != null)
                {
                    //Debug.Log("CopySceneView woo:" + sceneCamera.transform.position );
                    transform.position = sceneCamera.transform.position;
                    transform.rotation = sceneCamera.transform.rotation;
                }
            }
        }
    }

    private void OnDisable()
    {
        EditorApplication.update -= Update;
        transform.localPosition = new Vector3(0, 0, 0);
        transform.rotation = new Quaternion();
    }

    private void OnEnable()
    {
        EditorApplication.update -= Update;
        EditorApplication.update += Update;
    }

    private void OnValidate()
    {
        if (gameObject.activeInHierarchy) Update();
    }
#else
	private void Start() {	
		transform.rotation = new Quaternion();
		transform.localPosition = new Vector3(0,0,0);
	}
#endif


}
