using UnityEngine;

namespace UnityEngine.Experimental.VoxelizedShadowMaps
{
    public class DirectionalVxShadowMapResources : ScriptableObject
    {
        [HideInInspector] public Vector3 Position;
        [HideInInspector] public Quaternion Rotation;
        [HideInInspector] public float VolumeScale;
        [HideInInspector] public int VoxelResolution;
        [HideInInspector] public int MaxScale;
        [HideInInspector] public Matrix4x4 WorldToShadowMatrix;
        [HideInInspector] public uint[] Data;
    }
}
