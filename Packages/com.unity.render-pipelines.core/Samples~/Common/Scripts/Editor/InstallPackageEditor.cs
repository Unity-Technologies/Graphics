#if UNITY_EDITOR
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InstallPackage))]
public class InstallPackageEditor : Editor
{

    private static ListRequest listRequest;
    private static AddRequest addRequest;
    private static InstallPackage mb = null;
    public override void OnInspectorGUI()
    {
        //base.DrawDefaultInspector();

        mb = (target as InstallPackage);

        if (GUILayout.Button($"Check and Install {mb.packageName} Package"))
            CheckAndInstallSplinePackage();

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
                bool isPackageInstalled = false;
                foreach (var package in listRequest.Result)
                {
                    if (package.name == mb.packageName)
                    {
                        isPackageInstalled = true;
                        Debug.Log($"Package is already installed (version: {package.version}).");
                        break;
                    }
                }

                if (!isPackageInstalled)
                {
                    Debug.Log($"Package {mb.packageName} is not installed. Installing...");
                    addRequest = Client.Add(mb.packageName);
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