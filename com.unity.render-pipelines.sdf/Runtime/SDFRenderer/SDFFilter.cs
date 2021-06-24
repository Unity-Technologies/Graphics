using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDFFilter : MonoBehaviour
{
    [SerializeField]
    private VoxelField m_VoxelField;

    public VoxelField VoxelField
    {
        get
        {
            return m_VoxelField;
        }
    }

    public bool InitializeFromFile(string assetPath)
    {
        return VoxelFieldIO.Read(assetPath, out m_VoxelField);
    }
}
