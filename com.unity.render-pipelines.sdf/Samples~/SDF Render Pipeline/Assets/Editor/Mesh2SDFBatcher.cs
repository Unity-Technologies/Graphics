using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class Mesh2SDFBatcher : EditorWindow
{
    string inputModelDirectory = string.Empty;
    string outputSDFDirectory = string.Empty;
    float voxelSize = 0.5f;
    bool sampleRandomPoints = false;
    bool smoothNormals = true;

    [MenuItem("SDF/Mesh To SDF Batch Export")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        Mesh2SDFBatcher window = (Mesh2SDFBatcher)EditorWindow.GetWindow(typeof(Mesh2SDFBatcher));
        window.Show();
    }

    string[] GetMeshAssetPaths(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return null;

        string[] filePaths = Directory.GetFiles(directoryPath, "*.obj");
        if (filePaths == null || filePaths.Length <= 0)
            return null;

        for(int i = 0; i < filePaths.Length; ++i)
        {
            filePaths[i] = filePaths[i].Replace("\\", "/");
            filePaths[i] = filePaths[i].Replace(Application.dataPath, "Assets");
        }

        return filePaths;
    }

    bool ConvertMeshToSDF(string meshFilePath, string outputDirectory)
    {
        if (string.IsNullOrEmpty(meshFilePath))
            return false;

        int meshNameStartIndex = meshFilePath.LastIndexOf('/')+1;
        string meshName = meshFilePath.Substring(meshNameStartIndex, meshFilePath.Length - meshNameStartIndex);
        meshName = meshName.Replace(".obj", "");

        GameObject assetObject = AssetDatabase.LoadAssetAtPath<GameObject>(meshFilePath);

        if (assetObject == null)
            return false;

        GameObject instance =  Object.Instantiate(assetObject);
        instance.transform.position = new Vector3(0, 0, 0);

        MeshFilter selectedMeshFilter = instance.GetComponentInChildren<MeshFilter>();
        if (selectedMeshFilter == null)
            return false;

        MeshToSDFProcessorSettings settings = new MeshToSDFProcessorSettings();
        settings.outputFilePath = string.Format("{0}/{1}.sdf", outputDirectory, meshName);
        if (settings.outputFilePath != null && settings.outputFilePath.Length > 0)
        {
            settings.assetName = meshName;
            settings.voxelSize = voxelSize;
            settings.sampleRandomPoints = sampleRandomPoints;
            settings.smoothNormals = smoothNormals;
            settings.voxelMaterial = null;
            settings.closestPointMaterial = null;

            if (MeshToSDFProcessor.Convert(settings, selectedMeshFilter))
                Debug.Log(string.Format("Created SDF asset \"{0}\" sucessfully!", meshName));
            else
                Debug.LogError(string.Format("Failed to created SDF asset \"{0}\"!", meshName));
        }

        Object.DestroyImmediate(instance, true);

        return true;
    }

    void OnGUI()
    {
        GUILayout.Label("Base Settings", EditorStyles.boldLabel);

        voxelSize = EditorGUILayout.FloatField("Voxel Size", voxelSize);
        sampleRandomPoints = EditorGUILayout.Toggle("Sample Random Points", sampleRandomPoints);
        smoothNormals = EditorGUILayout.Toggle("Smooth Normals", smoothNormals);

        GUILayout.Label("File Settings", EditorStyles.boldLabel);

        if (GUILayout.Button("Select Mesh Directory"))
        {
            inputModelDirectory = EditorUtility.OpenFolderPanel("Mesh Directory", inputModelDirectory, "");
        }

        if (!string.IsNullOrEmpty(inputModelDirectory))
        {
            EditorGUILayout.TextField("Mesh Directory:", inputModelDirectory);
        }

        if (GUILayout.Button("Select SDF Directory"))
        {
            outputSDFDirectory = EditorUtility.OpenFolderPanel("SDF Directory", outputSDFDirectory, "");
        }

        if (!string.IsNullOrEmpty(outputSDFDirectory))
        {
            EditorGUILayout.TextField("SDF Directory:", outputSDFDirectory);
        }

        if (Directory.Exists(inputModelDirectory) && Directory.Exists(outputSDFDirectory))
        {
            GUILayout.Label("Export", EditorStyles.boldLabel);
            if (GUILayout.Button("Generate SDF Assets"))
            {
                string[] meshFilePaths = GetMeshAssetPaths(inputModelDirectory);
                if(meshFilePaths.Length <= 0)
                {
                    string msgError = string.Format("The directory \"{0}\" does not contain any mesh OBJ files to convert.", inputModelDirectory);
                    Debug.LogError(msgError);
                    EditorUtility.DisplayDialog("Mesh To SDF", msgError, "OK");
                    return;
                }

                Debug.Log("Commencing Mesh To SDF Batch Export");
                for (int i = 0; i < meshFilePaths.Length; ++i)
                {
                    Debug.Log(string.Format("{0}/{1} Exporting {2} ...", i + 1, meshFilePaths.Length, meshFilePaths[i]));
                    ConvertMeshToSDF(meshFilePaths[i], outputSDFDirectory);
                }
                Debug.Log("Mesh To SDF Batch Export Is Complete");

                EditorUtility.DisplayDialog("Mesh To SDF", "The Mesh To SDF Batch Export is complete.", "OK");
            }
        }
    }
}
