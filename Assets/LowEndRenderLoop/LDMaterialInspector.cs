using UnityEngine;
using UnityEditor;

public class LDMaterialInspector : MaterialEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Material materialTarget = target as Material;
    }
}
