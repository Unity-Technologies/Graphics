using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(VFXParameterBinder))]
public class VFXParameterBinderEditor : Editor
{
    ReorderableList m_List;
    SerializedProperty m_Elements;

    GenericMenu m_Menu;
    Editor m_ElementEditor;
    public override void OnInspectorGUI()
    {
        if (m_Menu == null) BuildMenu();
        if (m_Elements == null) m_Elements = serializedObject.FindProperty("m_Bindings");
        if (m_List == null)
        {
            m_List = new ReorderableList(serializedObject, m_Elements, false, true, true, true);
            m_List.drawHeaderCallback = DrawHeader;
            m_List.drawElementCallback = DrawElement;
            m_List.onRemoveCallback = RemoveElement;
            m_List.onAddCallback = AddElement;
            m_List.onSelectCallback = SelectElement;
        }

        EditorGUILayout.Space();
        m_List.DoLayoutList();
        EditorGUILayout.Space();

        if (m_ElementEditor != null)
        {
            m_ElementEditor.DrawDefaultInspector();
        }
    }

    public void BuildMenu()
    {
        m_Menu = new GenericMenu();

        List<Type> relevantTypes = new List<Type>();

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type t in assembly.GetTypes())
            {
                if (t.BaseType == typeof(VFXBindingBase))
                    relevantTypes.Add(t);
            }
        }
        foreach (Type type in relevantTypes)
            m_Menu.AddItem(new GUIContent(type.ToString()), false, AddBinding, type);
    }

    public void AddBinding(object type)
    {
        Type t = type as Type;
        var obj = (m_SerializedObject.targetObject as VFXParameterBinder).gameObject;
        Undo.AddComponent(obj, t);
    }

    public void SelectElement(ReorderableList list)
    {
        UpdateSelection(list.index);
    }

    public void UpdateSelection(int selected)
    {
        if (selected >= 0)
            CreateCachedEditor(m_Elements.GetArrayElementAtIndex(selected).objectReferenceValue, typeof(Editor), ref m_ElementEditor);
        else
            m_ElementEditor = null;
    }

    public void AddElement(ReorderableList list)
    {
        m_Menu.ShowAsContext();
    }

    public void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        var element = (m_Elements.GetArrayElementAtIndex(index).objectReferenceValue).ToString();
        GUI.Label(rect, new GUIContent(element));
    }

    public void RemoveElement(ReorderableList list)
    {
        int index = m_List.index;
        var element = m_Elements.GetArrayElementAtIndex(index).objectReferenceValue;
        Undo.DestroyObjectImmediate(element);
        m_Elements.DeleteArrayElementAtIndex(index);
        UpdateSelection(m_List.index);
    }

    public void DrawHeader(Rect rect)
    {
        GUI.Label(rect, "Parameter Bindings");
    }
}
