using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(MaterialParameterVariation))]
public class MaterialParameterVariationDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that
        // prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        // Draw label
        //position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Don't make child fields be indented
        var indent = EditorGUI.indentLevel;
        //EditorGUI.indentLevel = 0;

        float cellStart = 0f;
        float remainingWidth = position.width - cellStart;

        bool isMulti = property.FindPropertyRelative("multi").boolValue;
        MaterialParameterVariation.ParamType type = (MaterialParameterVariation.ParamType)property.FindPropertyRelative("paramType").enumValueIndex;
        float nonValueHeight = (type == MaterialParameterVariation.ParamType.Vector) ? position.height / 4f : position.height;

        Rect multiRect = new Rect(position.x, position.y, 25f, nonValueHeight);
        cellStart += multiRect.width;
        remainingWidth -= multiRect.width;
        Rect paramRect = new Rect(cellStart, position.y, remainingWidth / 5f, nonValueHeight);
        cellStart += paramRect.width;
        remainingWidth -= paramRect.width;
        Rect typeRect = new Rect(cellStart, position.y, 70, nonValueHeight);
        cellStart += typeRect.width;
        remainingWidth -= typeRect.width;
        Rect valueRect = new Rect();
        Rect maxRect = new Rect();
        Rect countRect = new Rect();
        if (!isMulti || (type == MaterialParameterVariation.ParamType.Texture))
        {
            valueRect = new Rect(cellStart, position.y, remainingWidth, position.height);
        }
        else
        {
            valueRect = new Rect(cellStart, position.y, remainingWidth / 3f, position.height);
            cellStart += valueRect.width;
            remainingWidth -= valueRect.width;
            maxRect = new Rect(cellStart, position.y, remainingWidth / 2f, position.height);
            cellStart += maxRect.width;
            remainingWidth -= maxRect.width;
            countRect = new Rect(cellStart, position.y, remainingWidth, nonValueHeight);
        }

        if (GUI.Button(multiRect, isMulti ? "∞" : "1"))
            property.FindPropertyRelative("multi").boolValue = !isMulti;

        EditorGUI.PropertyField(paramRect, property.FindPropertyRelative("parameter"), GUIContent.none);
        EditorGUI.PropertyField(typeRect, property.FindPropertyRelative("paramType"), GUIContent.none);

        switch (type)
        {
            case MaterialParameterVariation.ParamType.Float:
                EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("f_Value"), GUIContent.none);
                if (isMulti)
                    EditorGUI.PropertyField(maxRect, property.FindPropertyRelative("f_Value_Max"), GUIContent.none);
                break;
            case MaterialParameterVariation.ParamType.Bool:
                if (!isMulti)
                    EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("b_Value"), GUIContent.none);
                break;
            case MaterialParameterVariation.ParamType.Vector:
                //EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("v_Value"), GUIContent.none);
                DrawVector(valueRect, property.FindPropertyRelative("v_Value"));
                if (isMulti)
                    DrawVector(maxRect, property.FindPropertyRelative("v_Value_Max"));
                break;
            case MaterialParameterVariation.ParamType.Int:
                EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("i_Value"), GUIContent.none);
                if (isMulti)
                    EditorGUI.PropertyField(maxRect, property.FindPropertyRelative("i_Value_Max"), GUIContent.none);
                break;
            case MaterialParameterVariation.ParamType.Texture:
                EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("t_Value"), GUIContent.none);
                break;
            case MaterialParameterVariation.ParamType.Color:
                EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("c_Value"), GUIContent.none);
                if (isMulti)
                    EditorGUI.PropertyField(maxRect, property.FindPropertyRelative("c_Value_Max"), GUIContent.none);
                break;
        }
        if (isMulti && (type != MaterialParameterVariation.ParamType.Bool) && (type != MaterialParameterVariation.ParamType.Texture))
            EditorGUI.PropertyField(countRect, property.FindPropertyRelative("count"), GUIContent.none);

        // Set indent back to what it was
        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float mul = ((MaterialParameterVariation.ParamType)property.FindPropertyRelative("paramType").enumValueIndex == MaterialParameterVariation.ParamType.Vector) ? 4f : 1f;
        return base.GetPropertyHeight(property, label) * mul;
    }

    void DrawVector(Rect rect, SerializedProperty property)
    {
        Vector4 v = property.vector4Value;

        float labelwidth = 15f;

        Rect lx = new Rect(rect.x, rect.y, labelwidth, rect.height / 4f);
        Rect ly = new Rect(lx.x, rect.y + lx.height, labelwidth, lx.height);
        Rect lz = new Rect(lx.x, rect.y + 2f * lx.height, labelwidth, lx.height);
        Rect lw = new Rect(lx.x, rect.y + 3f * lx.height, labelwidth, lx.height);

        GUI.Label(lx, "X");
        GUI.Label(ly, "Y");
        GUI.Label(lz, "Z");
        GUI.Label(lw, "W");

        Rect rx = new Rect(rect.x, rect.y, rect.width, rect.height / 4f);
        Rect ry = new Rect(rx.x, rect.y + rx.height, rx.width, rect.height / 4f);
        Rect rz = new Rect(rx.x, rect.y + 2f * rx.height, rx.width, rect.height / 4f);
        Rect rw = new Rect(rx.x, rect.y + 3f * rx.height, rx.width, rect.height / 4f);

        v.x = EditorGUI.FloatField(rx, v.x);
        v.y = EditorGUI.FloatField(ry, v.y);
        v.z = EditorGUI.FloatField(rz, v.z);
        v.w = EditorGUI.FloatField(rw, v.w);

        property.vector4Value = v;
    }
}
