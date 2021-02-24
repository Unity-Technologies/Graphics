using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools.Graphics;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

[CreateAssetMenu(fileName = "New Test", menuName = "Shader Graph Test Asset")]
public class ShaderGraphTestAsset : ScriptableObject
{
    [System.Serializable]
    public class MaterialTest
    {
        public int hash;
        public Material material;
        public bool enabled = true;
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(MaterialTest))]
    public class HashTaggedMaterialDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            SerializedProperty material = property.FindPropertyRelative("material");
            SerializedProperty hash = property.FindPropertyRelative("hash");
            SerializedProperty enabled = property.FindPropertyRelative("enabled");

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(new Rect(position.x+ position.width*0.1f, position.y, position.width * 0.6f, position.height), material);
            enabled.boolValue = EditorGUI.Toggle(new Rect(position.x , position.y, position.width * 0.1f, position.height),"", enabled.boolValue);
            EditorGUI.LabelField(new Rect(position.x + position.width * 2f / 3f+ position.width * 0.16f, position.y, position.width * 1f / 3f, position.height), hash.intValue.ToString());
            if (GUI.Button(new Rect(position.x + position.width * 0.7f, position.y, position.width * 0.12f, position.height), "Update Hash"))
            {
                if (material.objectReferenceValue != null)
                {
                    hash.intValue = material.objectReferenceValue.GetHashCode() + Mathf.FloorToInt(Random.value * int.MaxValue);
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                if (material.objectReferenceValue != null)
                {
                    hash.intValue = material.objectReferenceValue.GetHashCode() + Mathf.FloorToInt(Random.value * int.MaxValue);
                }
            }
            EditorGUI.EndProperty();
        }
    }
#endif

    public List<MaterialTest> testMaterial;
    public Mesh customMesh;
    public bool isCameraPerspective;
    public ImageComparisonSettings settings;

}
