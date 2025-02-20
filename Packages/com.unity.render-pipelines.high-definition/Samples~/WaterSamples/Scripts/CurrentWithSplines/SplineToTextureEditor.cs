#if UNITY_EDITOR
using static UnityEngine.GraphicsBuffer;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SplineToTexture))]
public class SplineToTextureEditor : Editor
{
    private const string splinePackageName = "com.unity.splines";
    private static ListRequest listRequest;
    private static AddRequest addRequest;

    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();

        var baker = (target as SplineToTexture);

        if (GUILayout.Button("Check and Install Spline Package"))
            CheckAndInstallSplinePackage();

        if (GUILayout.Button("Save Current Map On Disk"))
            baker.OpenDialogAndSaveCurrentMap();

        if (GUILayout.Button("Save Height Map On Disk"))
            baker.OpenDialogAndSaveHeightMap();

        if (GUILayout.Button("Apply Transform"))
            baker.ApplyScaleAndPosition();

    }

    private static void CheckAndInstallSplinePackage()
    {
        listRequest = Client.List(true); // Fetches the list of all packages
        EditorApplication.update += OnListRequestProgress;
    }

    private static void OnListRequestProgress()
    {
        if (listRequest.IsCompleted)
        {
            EditorApplication.update -= OnListRequestProgress;

            if (listRequest.Status == StatusCode.Success)
            {
                bool isSplineInstalled = false;
                foreach (var package in listRequest.Result)
                {
                    if (package.name == splinePackageName)
                    {
                        isSplineInstalled = true;
                        Debug.Log($"Spline package is already installed (version: {package.version}).");
                        break;
                    }
                }

                if (!isSplineInstalled)
                {
                    Debug.Log("Spline package is not installed. Installing...");
                    addRequest = Client.Add(splinePackageName);
                    EditorApplication.update += OnAddRequestProgress;
                }
            }
            else if (listRequest.Status >= StatusCode.Failure)
            {
                Debug.LogError($"Failed to list packages: {listRequest.Error.message}");
            }
        }
    }

    private static void OnAddRequestProgress()
    {
        if (addRequest.IsCompleted)
        {
            EditorApplication.update -= OnAddRequestProgress;

            if (addRequest.Status == StatusCode.Success)
            {
                Debug.Log($"Successfully installed {addRequest.Result.displayName} (version: {addRequest.Result.version}).");
            }
            else if (addRequest.Status >= StatusCode.Failure)
            {
                Debug.LogError($"Failed to install package: {addRequest.Error.message}");
            }
        }
    }
}
#endif