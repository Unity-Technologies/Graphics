using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sample : MonoBehaviour
{
    public MeshFilter selectedMeshFilter = null;
    public float voxelSize = 0.5f;
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
            MeshToSDFProcessor.Convert(settings, selectedMeshFilter);

        }
    }
}
