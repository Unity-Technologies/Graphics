using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SDFFilter))]
[CanEditMultipleObjects]
public class SDFFilterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SDFFilter sdfFilter = target as SDFFilter;
        VoxelField vf = sdfFilter.VoxelField;

        EditorGUILayout.LabelField("Name:");
        EditorGUILayout.TextField(vf.m_Name);
        EditorGUILayout.IntField("ID:", vf.m_Id);
        EditorGUILayout.LabelField("Voxel Count Per Axis:");
        EditorGUILayout.IntField("X:", vf.m_VoxelCountX);
        EditorGUILayout.IntField("Y:", vf.m_VoxelCountY);
        EditorGUILayout.IntField("Z:", vf.m_VoxelCountZ);

        EditorGUILayout.FloatField("Voxel Size:", vf.VoxelSize);
        EditorGUILayout.Vector3Field("Mesh Center:", vf.MeshBounds.center);
        EditorGUILayout.Vector3Field("Mesh Size:", vf.MeshBounds.size);
        EditorGUILayout.IntField("Field Entries:", vf.m_Field.Length);

    }
}