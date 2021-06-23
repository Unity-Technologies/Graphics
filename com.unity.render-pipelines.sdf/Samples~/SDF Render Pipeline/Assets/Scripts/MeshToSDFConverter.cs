using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshToSDFConverter : MonoBehaviour
{
    public MeshFilter selectedMeshFilter = null;
    public float voxelSize = 0.5f;
    public Material voxelMaterial;
    public Material closestPointMaterial;

    public int startX = 0;
    public int startY = 0;
    public int startZ = 0;

    public bool convert = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(convert)
        {
            convert = false;
            if (selectedMeshFilter == null)
                return;

            MeshToSDFProcessorSettings settings = new MeshToSDFProcessorSettings();
            settings.voxelSize = voxelSize;
            settings.voxelMaterial = voxelMaterial;
            settings.closestPointMaterial = closestPointMaterial;

            settings.startX = startX;
            settings.startY = startY;
            settings.startZ = startZ;

            MeshToSDFProcessor.Convert(settings, selectedMeshFilter);

        }
    }
}
