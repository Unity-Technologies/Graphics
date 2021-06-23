using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Mesh2SDFConverterTool : EditorWindow
{
    //string myString = "Hello World";
    //bool groupEnabled;
    //bool myBool = true;
    //float myFloat = 1.23f;

    string sdfAssetName = "No Name";
    MeshFilter selectedMeshFilter = null;
    float voxelSize = 0.5f;
    Material voxelMaterial;
    Material closestPointMaterial;

    [MenuItem("SDF/Mesh To SDF")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        Mesh2SDFConverterTool window = (Mesh2SDFConverterTool)EditorWindow.GetWindow(typeof(Mesh2SDFConverterTool));
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Base Settings", EditorStyles.boldLabel);

        sdfAssetName = EditorGUILayout.TextField("Name", sdfAssetName);
        selectedMeshFilter = EditorGUILayout.ObjectField("Mesh", selectedMeshFilter, typeof(MeshFilter), true) as MeshFilter;
        voxelSize = EditorGUILayout.FloatField("Voxel Size", voxelSize);

        GUILayout.Label("Debug Settings", EditorStyles.boldLabel);
        voxelMaterial = EditorGUILayout.ObjectField("Voxel Material", voxelMaterial, typeof(Material), true) as Material;
        closestPointMaterial = EditorGUILayout.ObjectField("Closest Point On Mesh Material", closestPointMaterial, typeof(Material), true) as Material;

        if(GUILayout.Button("Generate SDF Asset"))
        {
            if (selectedMeshFilter == null)
                return;

            MeshToSDFProcessorSettings settings = new MeshToSDFProcessorSettings();
            settings.assetName = sdfAssetName;
            settings.voxelSize = voxelSize;
            settings.voxelMaterial = voxelMaterial;
            settings.closestPointMaterial = closestPointMaterial;

            if(MeshToSDFProcessor.Convert(settings, selectedMeshFilter))
                Debug.Log(string.Format("Created SDF asset \"{0}\" sucessfully!", sdfAssetName));
            else
                Debug.LogError(string.Format("Failed to created SDF asset \"{0}\"!", sdfAssetName));
        }
    }
}
