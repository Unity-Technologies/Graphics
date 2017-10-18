using UnityEngine;
using UnityEditor;

using System.Reflection;

using Type = System.Type;

public static class GUIGradientField
{
    #region Initial Setup

    private static MethodInfo s_miGradientField1;

    static GUIGradientField()
    {
        // Get our grubby hands on hidden "GradientField" :)
        Type tyEditorGUILayout = typeof(EditorGUILayout);
        s_miGradientField1 = tyEditorGUILayout.GetMethod("GradientField", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(string), GradientWrapper.s_tyGradient, typeof(GUILayoutOption[]) }, null);
    }

    #endregion

    public static GradientWrapper GradientField(string label, GradientWrapper gradient, params GUILayoutOption[] options)
    {
        if (gradient == null)
            gradient = new GradientWrapper();

        gradient.GradientData = s_miGradientField1.Invoke(null, new object[] { label, gradient.GradientData, options });

        return gradient;
    }

    public static GradientWrapper GradientField(GradientWrapper gradient, params GUILayoutOption[] options)
    {
        if (gradient == null)
            gradient = new GradientWrapper();

        gradient.GradientData = s_miGradientField1.Invoke(null, new object[] { gradient.GradientData, options });

        return gradient;
    }
}
